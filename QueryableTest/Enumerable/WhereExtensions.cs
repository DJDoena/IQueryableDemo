using QueryableTest.Models;

namespace QueryableTest.Enumerable;

internal static class WhereExtensions
{
    // ----- IEnumerable<Car> version -----
    // Mirrors Enumerable.Where: runs immediately (well, lazily via yield) and
    // in-memory, item by item, no expression trees involved at all.
    public static IEnumerable<Car> Where(this IEnumerable<Car> source
        , Func<Car, bool> predicate
        , IOutputProvider output)
    {
        output.WriteLine($"{nameof(Where)} called, returning a lazy iterator, nothing enumerated yet.");

        var enumerable = GetWhereIterator(source, predicate, output);

        return enumerable;
    }

    private static IEnumerable<Car> GetWhereIterator(IEnumerable<Car> source
        , Func<Car, bool> predicate
        , IOutputProvider output)
    {
        foreach (var car in source)
        {
            var isMatch = predicate(car);

            output.WriteLine($"Testing predicate against {car.Color} car with {car.Doors} doors: {isMatch}");

            if (isMatch)
            {
                yield return car;
            }
        }
    }
}
