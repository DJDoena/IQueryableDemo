using System.Collections;
using System.Linq.Expressions;

namespace QueryableTest;

// ----- Fictional SQL-backed IQueryable<Car> -----
// Independent twin of CarQueryable<T>, wired up to CarSqlQueryProvider
// instead of CarQueryProvider, purely to demonstrate what a "real" ORM-style
// provider's IQueryable side looks like next to the in-memory one.
internal class CarSqlQueryable<T> : IQueryable<T>
{
    public CarSqlQueryable(CarSqlQueryProvider provider)
    {
        this.Provider = provider;
        this.Expression = Expression.Constant(this);
    }

    public CarSqlQueryable(CarSqlQueryProvider provider, Expression expression)
    {
        this.Provider = provider;
        this.Expression = expression;
    }

    public Type ElementType
    {
        get => typeof(T);
    }

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        Console.WriteLine("  [SqlProvider] Enumeration started, translating expression tree to SQL...");

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
