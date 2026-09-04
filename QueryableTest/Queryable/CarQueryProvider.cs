using QueryableTest.Models;
using System.Linq.Expressions;

namespace QueryableTest.Queryable;

// A minimal IQueryProvider that interprets the expression tree itself instead
// of delegating to List<T>.AsQueryable()'s built-in provider. It only
// understands the pieces needed for this example: the root constant and a
// single Where(...) call, which is enough to see how the whole pipeline works.
internal class CarQueryProvider : IQueryProvider
{
    private readonly IEnumerable<Car> _source;

    private readonly IOutputProvider _output;

    public CarQueryProvider(IEnumerable<Car> source, IOutputProvider output)
    {
        _source = source;
        _output = output;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(CreateQuery)} called with expression: {expressionText}");

        var elementType = expression.Type.GetGenericArguments()[0];

        var queryableType = typeof(CarQueryable<>).MakeGenericType(elementType);

        var wrapped = (IQueryable)Activator.CreateInstance(queryableType, this, expression, _output)!;

        return wrapped;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var expressionText = expression.ToString();

        _output.WriteLine($"{nameof(CreateQuery)}<{typeof(TElement).Name}> called with expression: {expressionText}");

        return new CarQueryable<TElement>(this, expression, _output);
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

        _output.WriteLine($"Execute<{typeof(TResult).Name}> called with expression: {expressionText}");

        var result = (TResult)this.Evaluate(expression)!;

        return result;
    }

    // Walks the expression tree node by node. Step into this method with the
    // debugger to watch how a LINQ query gets unpacked and turned back into
    // real work.
    private object Evaluate(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression { Value: CarQueryable<Car> }:
                {
                    // Base case / recursion terminator: we've unwound all the way
                    // back to the root node, which just means "start from the
                    // original data source" - our custom CarEnumerable.
                    return _source;
                }
            case MethodCallExpression { Method.Name: "QueryableWhere" } whereCall:
                {
                    // whereCall.Arguments[0] is the expression for whatever came
                    // before this Where (either the root constant, or another
                    // operator) - recurse into it first to get its results.
                    var filteredSource = (IEnumerable<Car>)this.Evaluate(whereCall.Arguments[0]);

                    // whereCall.Arguments[1] is the predicate lambda, wrapped in a
                    // Quote node (UnaryExpression) by the compiler. Unwrap it,
                    // then Compile() turns the expression tree back into an
                    // actual, callable Func<Car, bool> delegate.
                    var lambda = (LambdaExpression)StripQuotes(whereCall.Arguments[1]);

                    // A real SQL-backed provider would use the predicate's
                    // expression tree (not the compiled delegate) to build a
                    // WHERE clause instead of filtering in-memory below - see
                    // CarSqlQueryProvider for a demonstration of that approach.
                    var whereFilter = GetWhereFilter(filteredSource, lambda, _output);

                    return whereFilter;
                }
            default:
                {
                    // Anything else (Select, OrderBy, Count, ...) isn't handled by
                    // this minimal example provider.
                    throw new NotSupportedException($"Expression '{expression}' is not supported by {nameof(CarQueryProvider)}.");
                }
        }
    }

    // Compiles a predicate LambdaExpression into a callable delegate and
    // filters cars with it. Shared by both CarQueryProvider (in-memory path)
    // and CarSqlQueryProvider (fictional SQL path) so there's a single,
    // reusable "run this predicate against real cars" implementation instead
    // of each provider maintaining its own copy.
    internal static IEnumerable<Car> GetWhereFilter(IEnumerable<Car> filteredSource, LambdaExpression lambda, IOutputProvider output)
    {
        var predicate = (Func<Car, bool>)lambda.Compile();

        var whereFilter = FilterManually(filteredSource, predicate, output);

        return whereFilter;
    }

    private static IEnumerable<Car> FilterManually(IEnumerable<Car> cars, Func<Car, bool> predicate, IOutputProvider output)
    {
        // A hand-written, lazily-evaluated filter (our own version of what
        // Enumerable.Where does internally). The yield return makes this a
        // state machine iterator: nothing here runs until the caller actually
        // pulls the next item, which is what "deferred execution" means in
        // practice.
        foreach (var car in cars)
        {
            var isMatch = predicate(car);

            output.WriteLine($"Testing predicate against {car.Color} car with {car.Doors} doors: {isMatch}");

            if (isMatch)
            {
                yield return car;
            }
        }
    }

    private static Expression StripQuotes(Expression expression)
    {
        // Lambdas passed to Queryable extension methods get wrapped in a
        // Quote (a UnaryExpression) so they can be represented as data inside
        // the expression tree instead of being compiled immediately. Unwrap
        // any nested Quotes to get back to the actual LambdaExpression.
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        return expression;
    }
}