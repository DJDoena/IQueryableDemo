using System.Collections;

namespace QueryableTest;

internal class CarEnumerator : IEnumerator<Car>
{
    private int _index = -1;

    private readonly IReadOnlyList<Car> _source;

    public CarEnumerator(IReadOnlyList<Car> source)
    {
        _source = source;
    }

    public Car Current
    {
        get => _source[_index];
    }

    object IEnumerator.Current
    {
        get => this.Current;
    }

    public bool MoveNext()
    {
        _index++;
        return _index < _source.Count;
    }

    public void Reset()
    {
        _index = -1;
    }

    public void Dispose()
    {
    }
}