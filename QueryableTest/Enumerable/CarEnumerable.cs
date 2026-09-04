using QueryableTest.Models;
using System.Collections;

namespace QueryableTest.Enumerable;

// ----- Hand-rolled IEnumerable<Car> -----

internal class CarEnumerable : IEnumerable<Car>
{
    private readonly IReadOnlyList<Car> _source;

    public CarEnumerable(IReadOnlyList<Car> source)
    {
        _source = source;
    }

    public IEnumerator<Car> GetEnumerator()
    {
        return new CarEnumerator(_source);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var enumerator = this.GetEnumerator();

        return enumerator;
    }
}