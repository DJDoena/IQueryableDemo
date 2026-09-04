using QueryableTest.Models;
using QueryableTest.Queryable;
using System.Linq.Expressions;

namespace QueryableTest.SqlQueryable;

// A fictional SQL-backed IQueryProvider. Instead of interpreting the
// expression tree in-memory item by item (like CarQueryProvider does), this
// provider builds an actual SQL statement string from the tree via
// CarSqlExpressionVisitor - the way a real ORM (e.g. EF Core) would - and
// would then send it to a database.
//
// IMPORTANT: there is no real database here, and the SQL string is NEVER
// parsed, executed, or otherwise consulted to produce a result. The in-memory
// filtering below exists purely so this example has something concrete to
// hand back to the caller (real Car instances instead of an empty sequence).
// The generated SQL and the returned result are intentionally NOT coupled -
// in a real ORM, the database itself would apply the WHERE clause, but here
// we fake that by separately re-compiling and re-running the same predicate
// in .NET. Changing CarSqlExpressionVisitor would only change the printed SQL
// text; it would have zero effect on which cars are actually returned.
internal class CarSqlQueryProvider : IQueryProvider
{
    private readonly IEnumerable<Car> _source;

    private readonly IOutputProvider _output;

    public CarSqlQueryProvider(IEnumerable<Car> source, IOutputProvider output)
    {
        _source = source;
        _output = output;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(CreateQuery)} called with expression: {expressionText}");

        var elementType = expression.Type.GetGenericArguments()[0];

        var queryableType = typeof(CarSqlQueryable<>).MakeGenericType(elementType);

        var wrapped = (IQueryable)Activator.CreateInstance(queryableType, this, expression, _output)!;

        return wrapped;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(CreateQuery)}<{typeof(TElement).Name}> called with expression: {expressionText}");

        return new CarSqlQueryable<TElement>(this, expression, _output);
    }

    public object? Execute(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(Execute)} called with expression: {expressionText}");

        var result = this.Evaluate(expression);

        return result;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(Execute)}<{typeof(TResult).Name}> called with expression: {expressionText}");

        var result = (TResult)this.Evaluate(expression)!;

        return result;
    }

    // Walks the expression tree looking for the Where(...) call, turns its
    // predicate into a SQL WHERE clause, and builds the full SELECT
    // statement. No database exists to actually run it against, so the SQL is
    // only ever printed, never executed. The real result is produced by a
    // completely separate, unrelated step further down (compiling the same
    // predicate and filtering _source in .NET) - the two are not wired
    // together in any way.
    private object Evaluate(Expression expression, bool isTopLevel = true)
    {
        switch (expression)
        {
            case ConstantExpression { Value: CarSqlQueryable<Car> }:
                {
                    // This is the legitimate base case when reached
                    // recursively from the Where case below (whereCall.Arguments[0]):
                    // it just means "start from the original data source".
                    //
                    // If, instead, this is the TOP-LEVEL call (isTopLevel is
                    // still true), it means Evaluate was invoked with only
                    // the bare root ConstantExpression and no Where(...)
                    // MethodCallExpression at all - i.e. no SQL could be
                    // generated. This typically happens when .Where(...) was
                    // called with an already-compiled Func<Car, bool> delegate
                    // instead of an Expression<Func<Car, bool>> lambda: the
                    // IEnumerable<Car> overload silently binds instead of the
                    // IQueryable<Car> one (both are applicable since
                    // CarSqlQueryable<T> : IQueryable<T> : IEnumerable<T>),
                    // so the expression tree never gets a Where node appended
                    // to it - exactly the kind of predicate EF Core also
                    // cannot translate to SQL. Unlike the in-memory
                    // CarQueryProvider (which has no SQL to generate and so
                    // has nothing to fail at), a SQL-backed provider throwing
                    // here mirrors EF Core's real behavior of raising an
                    // exception when a query cannot be translated, rather
                    // than silently falling back to client-side evaluation.
                    if (isTopLevel)
                    {
                        var errorMessage = "No SQL could be determined from this expression";

                        throw new Exception(errorMessage);
                    }

                    return _source;
                }
            case MethodCallExpression { Method.Name: "QueryableWhere" } whereCall:
                {
                    // Recurse first so nested/chained Where calls could each
                    // contribute their own AND'ed clause in a fuller example.
                    var sourceSoFar = (IEnumerable<Car>)this.Evaluate(whereCall.Arguments[0], isTopLevel: false);

                    var lambda = (LambdaExpression)StripQuotes(whereCall.Arguments[1]);

                    // ----- "Database" side: build and print the SQL, nothing else -----
                    var sqlWhereClause = CarSqlExpressionVisitor.Translate(lambda.Body);

                    var sqlStatement = $"SELECT * FROM Cars WHERE {sqlWhereClause}";

                    _output.WriteLine($"Generated SQL: {sqlStatement}");
                    _output.WriteLine("(no real database connected - the SQL above is never executed)");

                    // ----- ".NET" side: yield a real result, independently of the SQL above -----
                    // This does NOT parse or execute sqlStatement in any way. It
                    // reuses CarQueryProvider.GetWhereFilter to compile the same
                    // predicate expression the SQL was generated from and filter
                    // in-memory, purely so the example has actual Car results to
                    // enumerate. If CarSqlExpressionVisitor produced wrong or
                    // nonsensical SQL, the cars returned here would be completely
                    // unaffected - both providers share one filtering
                    // implementation instead of maintaining duplicate copies.
                    var whereFilter = CarQueryProvider.GetWhereFilter(sourceSoFar, lambda, _output);

                    return whereFilter;
                }
            default:
                {
                    throw new NotSupportedException($"Expression '{expression}' is not supported by {nameof(CarSqlQueryProvider)}.");
                }
        }
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        return expression;
    }
}
