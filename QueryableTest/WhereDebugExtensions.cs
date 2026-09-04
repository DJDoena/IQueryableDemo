using System.Linq.Expressions;
using System.Reflection;

namespace QueryableTest;

// Plain, hand-written replacements for the built-in Enumerable.Where /
// Queryable.Where extension methods, so you can see exactly what they do
// instead of stepping into framework/BCL code.
internal static class WhereDebugExtensions
{
    // ----- IEnumerable<Car> version -----
    // Mirrors Enumerable.Where: runs immediately (well, lazily via yield) and
    // in-memory, item by item, no expression trees involved at all.
    public static IEnumerable<Car> WhereDebug(this IEnumerable<Car> source, Func<Car, bool> predicate)
    {
        Console.WriteLine("  [WhereDebug/IEnumerable] Called - returning a lazy iterator, nothing enumerated yet.");
        return WhereDebugIterator(source, predicate);
    }

    private static IEnumerable<Car> WhereDebugIterator(IEnumerable<Car> source, Func<Car, bool> predicate)
    {
        foreach (var car in source)
        {
            Console.WriteLine($"  [WhereDebug/IEnumerable] Testing predicate against {car.Color} car with {car.Doors} doors");
            if (predicate(car))
            {
                yield return car;
            }
        }
    }

    // ----- IQueryable<Car> version -----
    // Mirrors Queryable.Where: does NOT run anything itself. It builds a
    // MethodCallExpression describing "call WhereDebug with this source and
    // this predicate", and asks the source's Provider to turn that into a new
    // IQueryable<Car>. The actual filtering only happens later, when someone
    // enumerates the result and CarQueryProvider.Evaluate interprets this
    // MethodCallExpression node.
    private static readonly MethodInfo WhereDebugMethodInfo =
        typeof(WhereDebugExtensions).GetMethod(nameof(WhereDebug), [typeof(IQueryable<Car>), typeof(Expression<Func<Car, bool>>)])!;

    public static IQueryable<Car> WhereDebug(this IQueryable<Car> source, Expression<Func<Car, bool>> predicate)
    {
        Console.WriteLine("  [WhereDebug/IQueryable] Called - building expression tree, nothing executed yet.");

        var callExpression = Expression.Call(
            null,
            WhereDebugMethodInfo,
            source.Expression,
            Expression.Quote(predicate));

        return source.Provider.CreateQuery<Car>(callExpression);
    }
}
