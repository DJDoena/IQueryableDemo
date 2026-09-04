# QueryableTest

A small, hand-rolled demo that shows exactly what `IEnumerable<T>` and
`IQueryable<T>` do under the hood, without stepping into BCL/framework code.
Everything here re-implements the pieces LINQ normally hides, so you can set
breakpoints and watch the mechanics happen step by step.

## Development notes

This project (code, comments, README, and this document) was built
iteratively in collaboration with **GitHub Copilot** (using the **Claude
Sonnet 5** model) integrated into **Visual Studio Professional 2026
(18.9.2)**, using its debugger-connected agent mode to inspect runtime state,
set breakpoints, and verify behavior while implementing and refining the
demo. See `chathistory.md` at the repository root for a chronological
summary of the full session - every request, code change, bug found, and
design decision made along the way.

## Project layout

Code is split into folders/namespaces by concern:
- `Models/` (`QueryableTest.Models`) - `Car.cs`, `Storage.cs`.
- `Enumerable/` (`QueryableTest.Enumerable`) - `CarEnumerable.cs`,
  `CarEnumerator.cs`, and `WhereExtensions.EnumerableWhere` (the
  `IEnumerable<Car>` overload).
- `Queryable/` (`QueryableTest.Queryable`) - `CarQueryable.cs`,
  `CarQueryProvider.cs`, and `WhereExtensions.QueryableWhere` (the
  `IQueryable<Car>` overload). Note there are two distinct
  `WhereExtensions` classes (one per namespace above), each holding the
  overload relevant to its own execution model - named `EnumerableWhere` and
  `QueryableWhere` respectively so it's explicit at the call site which one
  is being invoked.
- `SqlQueryable/` (`QueryableTest.SqlQueryable`) - `CarSqlExpressionVisitor.cs`,
  `CarSqlQueryable.cs`, `CarSqlQueryProvider.cs`.
- Solution root (`QueryableTest` namespace) - `IOutputProvider.cs`,
  `OutputProvider.cs`, `Program.cs`.

## What it demonstrates

### 1. `IEnumerable<Car>` - in-memory, immediate-per-item execution
- `CarEnumerable` / `CarEnumerator` - a hand-written collection and iterator,
  standing in for `List<T>`'s built-in ones.
- `WhereExtensions.EnumerableWhere(IEnumerable<Car>, ...)` - a replacement
  for `Enumerable.Where` that filters items lazily (via `yield return`) using
  a plain compiled delegate (`Func<Car, bool>`). No expression trees are
  involved at all.

### 2. `IQueryable<Car>` - deferred, expression-tree-based execution
- `CarQueryable<T>` - a hand-written `IQueryable<T>` that only stores an
  `Expression` describing the query so far; nothing runs until it's enumerated.
- `CarQueryProvider` - the `IQueryProvider` that receives the accumulated
  `Expression` tree and interprets it manually (`Evaluate`), recognizing the
  `Where` method call node, compiling the predicate lambda, and filtering
  the source in-memory - the same outcome as the `IEnumerable` version, but
  built by walking an expression tree instead of calling a delegate directly.
  (`CarSqlQueryProvider`'s equivalent method is also named `Evaluate`, so the
  two providers can be compared side by side.)
- `WhereExtensions.QueryableWhere(IQueryable<Car>, Expression<Func<Car, bool>>, IOutputProvider)`
  - builds a `MethodCallExpression` node (including the `IOutputProvider`
	argument, so the method's arity matches its 3-parameter signature) and
	asks the provider to wrap it in a new `IQueryable<Car>`, mirroring what
	`Queryable.Where` does internally.

### 3. Fictional SQL translation - what a "real" ORM provider would do instead
- `CarSqlExpressionVisitor` - an `ExpressionVisitor` that walks a predicate's
  expression tree and turns it into a SQL `WHERE` clause fragment (e.g.
  `([Color] = 4)`), including translating the `ConsoleColor` enum down to the
  integer value a database column would actually store.
- `CarSqlQueryable<T>` / `CarSqlQueryProvider` - an independent `IQueryable`/
  `IQueryProvider` pair that, instead of filtering in-memory like
  `CarQueryProvider` does, builds a full fictional SQL statement (e.g.
  `SELECT * FROM Cars WHERE ([Color] = 4)`) and prints it, illustrating where
  and how a provider like Entity Framework Core would translate LINQ into SQL.
- **The generated SQL and the returned result are intentionally NOT coupled.**
  There is no real database, so the SQL string is never parsed or executed -
  it exists purely for illustration. To still have something concrete to
  enumerate, `CarSqlQueryProvider` compiles the very same predicate expression
  and filters an in-memory source in plain .NET, reusing
  `CarQueryProvider.GetWhereFilter` (the same helper the plain `IQueryable`
  path uses). This in-memory filtering only exists to yield a real result for
  the demo; it is a completely independent code path from the SQL text
  generation. Changing `CarSqlExpressionVisitor` would only change what gets
  printed - it would
  have zero effect on which cars are actually returned, exactly like a real
  ORM's "generate SQL" step is logically separate from "database executes it
  and returns rows".

### 4. `IOutputProvider` - pluggable, named logging instead of raw `Console.WriteLine`
- `IOutputProvider` - a small interface (`WriteLine(string)` / `WriteLine()`)
  that every class uses instead of calling `Console.WriteLine` directly.
- `OutputProvider` - the default implementation; takes a `name` in its
  constructor and prefixes every message with `  [name] ` (e.g.
  `new OutputProvider("Provider")`, `new OutputProvider("SqlProvider")`),
  reproducing the old hardcoded log prefixes but through an injectable
  abstraction.
- `CarQueryProvider`, `CarQueryable`, `CarSqlQueryProvider`, `CarSqlQueryable`
  all take an `IOutputProvider` via their constructors and use it for all
  logging. `WhereExtensions.EnumerableWhere`/`QueryableWhere` also take an
  `IOutputProvider` parameter; the `QueryableWhere` overload passes it
  through as a third argument on the `Expression.Call` node it builds, so
  `WhereMethodInfo`'s 3-parameter signature matches at runtime.
- `Storage.GetCarsQueryable` and `Program.cs` construct named `OutputProvider`
  instances (`"Provider"`, `"SqlProvider"`, `"Enumerable"`) and wire them
  through to the relevant constructors/methods.

### 5. Delegate vs. expression: why `Func<Car, bool>` breaks translation
- `CarQueryable<T>`/`CarSqlQueryable<T>` are both `IQueryable<T>` and
  (transitively) `IEnumerable<T>`, so both would be valid targets for an
  `IEnumerable<Car>`/`Func<Car, bool>`-style `Where` and an
  `IQueryable<Car>`/`Expression<Func<Car, bool>>`-style `Where` at the same
  time. Rather than relying on the compiler's overload resolution to quietly
  pick one (which it would do based on whether the argument you pass can
  only convert to a delegate, or can also convert to an expression tree),
  the two methods are named explicitly - `EnumerableWhere` and
  `QueryableWhere` - so it's always clear at the call site which execution
  model is being invoked, and what kind of argument (delegate vs. expression
  tree) it expects:
  - `EnumerableWhere(this IEnumerable<Car>, Func<Car, bool>, IOutputProvider)`
    only ever receives a compiled delegate - it has no expression tree to
    inspect, so it always filters immediately/lazily in-memory, item by item.
  - `QueryableWhere(this IQueryable<Car>, Expression<Func<Car, bool>>, IOutputProvider)`
    only ever receives an expression tree - the compiler converts a
    compatible inline lambda into one automatically, but a pre-compiled
    `Func<Car, bool>` (e.g. a local function like
    `bool blueCarFilter(Car c) => c.Color == ConsoleColor.Blue;`) **cannot**
    convert to `Expression<Func<Car, bool>>` implicitly, so calling
    `QueryableWhere` with it is a compile error - you'd have to explicitly
    call `EnumerableWhere(blueCarFilter, output)` instead, which is exactly
    what `Program.cs`'s "blue car filter" demo blocks do.
- `CarSqlQueryProvider.Evaluate(Expression, bool isTopLevel = true)` still
  distinguishes a legitimate recursive base-case hit (`isTopLevel: false`,
  reached from inside the `Where` `MethodCallExpression` case) from an
  invalid top-level one. Because `EnumerableWhere`/`QueryableWhere` now make
  the choice explicit rather than implicit, reaching the invalid top-level
  case in practice means someone called `.EnumerableWhere(...)` directly on
  an `IQueryable<Car>`/`CarSqlQueryable<Car>` (as the "blue car filter" demo
  does) instead of `.QueryableWhere(...)`. In that case it `throw`s, rather
  than silently falling back to client-side filtering: unlike the in-memory
  `CarQueryProvider` (which has no SQL to generate and so nothing to fail
  at), a SQL-backed provider raising an exception here mirrors real EF
  Core's behavior of throwing when a query cannot be translated to SQL,
  instead of quietly evaluating it client-side.
#### In real life: this is the EF Core "IEnumerable vs. IQueryable" trap
`IQueryable<T>.Where` is overloaded to accept `Expression<Func<T, bool>>`,
while `IEnumerable<T>.Where` (the LINQ-to-Objects extension method) only
accepts `Func<T, bool>`. Both extension methods are in scope at once on an
EF Core `DbSet<T>`/`IQueryable<T>`, exactly like `EnumerableWhere`/
`QueryableWhere` are both in scope here - except the BCL gives them the
*same* name (`Where`), so which one binds is entirely down to silent
overload resolution, not an explicit, differently-named method call like
in this demo. If you accidentally pass something that can only be a
delegate - e.g. you first cast/assign your `IQueryable<T>` to a plain
`IEnumerable<T>` variable, or you pass a pre-compiled `Func<T, bool>`
instead of writing an inline lambda - the compiler quietly binds to
`Enumerable.Where` instead of `Queryable.Where`. No SQL `WHERE` clause is
ever generated for that predicate: EF Core still executes the *rest* of
the query as SQL, materializes the *entire* table/result set into memory,
and only then applies your filter client-side, in .NET, over every row -
instead of at the database. This is the real-world version of what this
demo models with `CarSqlQueryProvider` throwing: it's silent (no compile
error, no exception, no warning by default), and its only symptoms are
"the query is way slower than expected" and "way more rows were fetched
from the database than necessary" - which is why EF Core added analyzer
warnings for exactly this mistake, and why this demo makes the
`EnumerableWhere`/`QueryableWhere` naming distinction explicit rather than
relying on identical-name overload resolution the way the BCL does.
- `Program.cs`'s `TestEnumerable()`/`TestQueryable()`/`TestSqlQueryable()`
  each include a "filter by 'blueCarFilter'" block using a local
  `bool blueCarFilter(Car c) => ...` function to demonstrate this side-by-side
  with the inline-lambda blocks: it filters correctly via `EnumerableWhere`
  in the `IEnumerable`/`IQueryable` sections (client-side, item by item, even
  though the source is an `IQueryable` in `TestQueryable()`), but throws when
  called via `EnumerableWhere` on the `CarSqlQueryable<Car>` in
  `TestSqlQueryable()`.

## Running the demo

`Program.cs` runs all three approaches back-to-back against the same sample
`Car` data (`Storage`), logging each step through `IOutputProvider` instances
so the differences between immediate in-memory filtering, deferred
expression-tree evaluation, and SQL translation are visible in the output.
