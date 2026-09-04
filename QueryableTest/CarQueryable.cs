using System.Collections;
using System.Linq.Expressions;

namespace QueryableTest;

// ----- Hand-rolled IQueryable<Car> -----

internal class CarQueryable<T> : IQueryable<T>
{
    public CarQueryable(CarQueryProvider provider)
    {
        this.Provider = provider;

        // No expression was supplied, so this is the root of the query.
        // Represent "the data source" as a ConstantExpression wrapping this
        // instance, so later Where/Select calls have something to chain
        // MethodCallExpression nodes onto via Expression.Call(source.Expression, ...).
        this.Expression = Expression.Constant(this);
    }

    public CarQueryable(CarQueryProvider provider, Expression expression)
    {
        this.Provider = provider;
        this.Expression = expression;
    }

    public Type ElementType
    {
        // The element type LINQ operators need to build correctly-typed
        // Expression nodes (e.g. Queryable.Where<T> needs to know T is Car).
        get => typeof(T);
    }

    // The full expression tree built up so far. Nothing in here has executed;
    // it's purely a description of "what to do", inspected later by the
    // provider.
    public Expression Expression { get; }

    // The object responsible for turning Expression into an actual result.
    // LINQ's Where/Select/etc. extension methods call back into this Provider
    // (via CreateQuery) instead of running anything themselves.
    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        // This is the moment deferred execution ends: foreach (or ToList(),
        // Count(), etc.) calls GetEnumerator(), which asks the provider to
        // Execute the accumulated Expression tree and only then produces real
        // Car instances.
        Console.WriteLine("  [Provider] Enumeration started, executing expression tree...");

        var enumerable = this.Provider.Execute<IEnumerable<T>>(this.Expression);

        var enumerator = enumerable.GetEnumerator();

        return enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        // Non-generic forwarding, required because IQueryable<T> extends
        // IEnumerable<T> which extends IEnumerable.
        var enumerator = this.GetEnumerator();

        return enumerator;
    }
}