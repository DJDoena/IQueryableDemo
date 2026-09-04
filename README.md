# QueryableTest

A small, hand-rolled demo that shows exactly what `IEnumerable<T>` and
`IQueryable<T>` do under the hood, without stepping into BCL/framework code.
Everything here re-implements the pieces LINQ normally hides, so you can set
breakpoints and watch the mechanics happen step by step.

## Project layout

Code is split into folders/namespaces by concern:
- `Models/` (`QueryableTest.Models`) - `Car.cs`, `Storage.cs`.
- `Enumerable/` (`QueryableTest.Enumerable`) - `CarEnumerable.cs`,
  `CarEnumerator.cs`, and the `IEnumerable<Car>` overload of
  `WhereExtensions.Where`.
- `Queryable/` (`QueryableTest.Queryable`) - `CarQueryable.cs`,
  `CarQueryProvider.cs`, and the `IQueryable<Car>` overload of
  `WhereExtensions.Where`. Note there are two distinct
  `WhereExtensions` classes (one per namespace above), each holding the
  overload relevant to its own execution model.
- `SqlQueryable/` (`QueryableTest.SqlQueryable`) - `CarSqlExpressionVisitor.cs`,
  `CarSqlQueryable.cs`, `CarSqlQueryProvider.cs`.
- Solution root (`QueryableTest` namespace) - `IOutputProvider.cs`,
  `OutputProvider.cs`, `Program.cs`.

## What it demonstrates

### 1. `IEnumerable<Car>` - in-memory, immediate-per-item execution
- `CarEnumerable` / `CarEnumerator` - a hand-written collection and iterator,
  standing in for `List<T>`'s built-in ones.
- `WhereExtensions.Where(IEnumerable<Car>, ...)` - a replacement for
  `Enumerable.Where` that filters items lazily (via `yield return`) using a
  plain compiled delegate (`Func<Car, bool>`). No expression trees are
  involved at all.

### 2. `IQueryable<Car>` - deferred, expression-tree-based execution
- `CarQueryable<T>` - a hand-written `IQueryable<T>` that only stores an
  `Expression` describing the query so far; nothing runs until it's enumerated.
- `CarQueryProvider` - the `IQueryProvider` that receives the accumulated
  `Expression` tree and interprets it manually (`Evaluate`), recognizing the
  `Where` method call node, compiling the predicate lambda, and filtering
  the source in-memory - the same outcome as the `IEnumerable` version, but
  built by walking an expression tree instead of calling a delegate directly.
- `WhereExtensions.Where(IQueryable<Car>, Expression<Func<Car, bool>>, IOutputProvider)`
  - builds a `MethodCallExpression` node (including the `IOutputProvider`
	argument, so the method's arity matches its 3-parameter signature) and
	asks the provider to wrap it in a new `IQueryable<Car>`, mirroring what
	`Queryable.Where` does internally. Even though this shadows the real
	`Queryable.Where`, it's a distinct 3-parameter overload, so it doesn't
	conflict with (or call) the built-in one.

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
  logging. `WhereExtensions.Where` also takes an `IOutputProvider`
  parameter on both the `IEnumerable<Car>` and `IQueryable<Car>` overloads;
  the `IQueryable` overload passes it through as a third argument on the
  `Expression.Call` node it builds, so `WhereMethodInfo`'s 3-parameter
  signature matches at runtime.
- `Storage.GetCarsQueryable` and `Program.cs` construct named `OutputProvider`
  instances (`"Provider"`, `"SqlProvider"`, `"Enumerable"`) and wire them
  through to the relevant constructors/methods.

## Running the demo

`Program.cs` runs all three approaches back-to-back against the same sample
`Car` data (`Storage`), logging each step through `IOutputProvider` instances
so the differences between immediate in-memory filtering, deferred
expression-tree evaluation, and SQL translation are visible in the output.
