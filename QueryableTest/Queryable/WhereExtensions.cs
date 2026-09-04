using QueryableTest.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryableTest.Queryable;

// Plain, hand-written replacements for the built-in Enumerable.Where /
// Queryable.Where extension methods, so you can see exactly what they do
// instead of stepping into framework/BCL code.
internal static class WhereExtensions
{
    // ----- IQueryable<Car> version -----
    // Mirrors Queryable.Where: does NOT run anything itself. It builds a
    // MethodCallExpression describing "call Where with this source and
    // this predicate", and asks the source's Provider to turn that into a new
    // IQueryable<Car>. The actual filtering only happens later, when someone
    // enumerates the result and CarQueryProvider.Evaluate interprets this
    // MethodCallExpression node.
    private static readonly MethodInfo WhereMethodInfo =
        typeof(WhereExtensions).GetMethod(nameof(Where), [typeof(IQueryable<Car>), typeof(Expression<Func<Car, bool>>), typeof(IOutputProvider)])!;

    public static IQueryable<Car> Where(this IQueryable<Car> source
        , Expression<Func<Car, bool>> predicate
        , IOutputProvider output)
    {
        output.WriteLine("Called - building expression tree, nothing executed yet.");

        var callExpression = Expression.Call(
            null,
            WhereMethodInfo,
            source.Expression,
            Expression.Quote(predicate),
            Expression.Constant(output, typeof(IOutputProvider)));

        return source.Provider.CreateQuery<Car>(callExpression);
    }
}
