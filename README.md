# QueryableTest

A small, hand-rolled demo that shows exactly what `IEnumerable<T>` and
`IQueryable<T>` do under the hood, without stepping into BCL/framework code.
Everything here re-implements the pieces LINQ normally hides, so you can set
breakpoints and watch the mechanics happen step by step.

## What it demonstrates

### 1. `IEnumerable<Car>` - in-memory, immediate-per-item execution
- `CarEnumerable` / `CarEnumerator` - a hand-written collection and iterator,
  standing in for `List<T>`'s built-in ones.
- `WhereDebugExtensions.WhereDebug(IEnumerable<Car>, ...)` - a replacement for
  `Enumerable.Where` that filters items lazily (via `yield return`) using a
  plain compiled delegate (`Func<Car, bool>`). No expression trees are
  involved at all.

### 2. `IQueryable<Car>` - deferred, expression-tree-based execution
- `CarQueryable<T>` - a hand-written `IQueryable<T>` that only stores an
  `Expression` describing the query so far; nothing runs until it's enumerated.
- `CarQueryProvider` - the `IQueryProvider` that receives the accumulated
  `Expression` tree and interprets it manually (`Evaluate`), recognizing the
  `WhereDebug` method call node, compiling the predicate lambda, and filtering
  the source in-memory - the same outcome as the `IEnumerable` version, but
  built by walking an expression tree instead of calling a delegate directly.
- `WhereDebugExtensions.WhereDebug(IQueryable<Car>, Expression<Func<Car, bool>>)`
  - builds a `MethodCallExpression` node and asks the provider to wrap it in a
	new `IQueryable<Car>`, mirroring what `Queryable.Where` does internally.

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
  `CarQueryProvider.FilterManually` (the same helper the plain `IQueryable`
  path uses). This in-memory filtering only exists to yield a real result for
  the demo; it is a completely independent code path from the SQL text
  generation. Changing `CarSqlExpressionVisitor` would only change what gets
  printed - it would
  have zero effect on which cars are actually returned, exactly like a real
  ORM's "generate SQL" step is logically separate from "database executes it
  and returns rows".

## Running the demo

`Program.cs` runs all three approaches back-to-back against the same sample
`Car` data (`Storage`), logging each step (`Console.WriteLine`) so the
differences between immediate in-memory filtering, deferred expression-tree
evaluation, and SQL translation are visible in the output.
