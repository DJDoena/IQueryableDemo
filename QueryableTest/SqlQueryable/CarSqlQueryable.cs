using System.Collections;
using System.Linq.Expressions;

namespace QueryableTest.SqlQueryable;

// ----- Fictional SQL-backed IQueryable<Car> -----
// Independent twin of CarQueryable<T>, wired up to CarSqlQueryProvider
// instead of CarQueryProvider, purely to demonstrate what a "real" ORM-style
// provider's IQueryable side looks like next to the in-memory one.
internal class CarSqlQueryable<T> : IQueryable<T>
{
    private readonly IOutputProvider _output;

    public CarSqlQueryable(CarSqlQueryProvider provider, IOutputProvider output)
    {
        this.Provider = provider;
        this.Expression = Expression.Constant(this);
        _output = output;
    }

    public CarSqlQueryable(CarSqlQueryProvider provider, Expression expression, IOutputProvider output)
    {
        this.Provider = provider;
        this.Expression = expression;
        _output = output;
    }

    public Type ElementType
    {
        get => typeof(T);
    }

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        _output.WriteLine("Enumeration started, translating expression tree to SQL...");

        var enumerable = this.Provider.Execute<IEnumerable<T>>(this.Expression);

        var enumerator = enumerable.GetEnumerator();

        return enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var enumerator = this.GetEnumerator();

        return enumerator;
    }
}
