using QueryableTest.Enumerable;
using QueryableTest.Queryable;

namespace QueryableTest.Models;

internal class Storage
{
    private readonly List<Car> _cars;

    public Storage()
    {
        _cars = [];
    }

    public void AddCar(Car car)
    {
        _cars.Add(car);
    }

    // IEnumerable: hand-rolled, no reliance on List<T>'s own enumerator.
    // Where(...) compiles to a delegate and pulls items one at a time through
    // CarEnumerator.MoveNext/Current - fully in-memory, step-debuggable.
    public IEnumerable<Car> GetCarsEnumerable()
    {
        return new CarEnumerable(_cars);
    }

    // IQueryable: hand-rolled provider. Where(...) does NOT run anything, it
    // just appends a MethodCallExpression onto the Expression tree. Only when
    // you enumerate does CarQueryProvider.Execute walk that tree and apply
    // the filter manually.
    public IQueryable<Car> GetCarsQueryable(IOutputProvider output)
    {
        var provider = new CarQueryProvider(new CarEnumerable(_cars), output);

        return new CarQueryable<Car>(provider, output);
    }
}
