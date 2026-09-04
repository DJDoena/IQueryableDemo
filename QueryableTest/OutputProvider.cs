namespace QueryableTest;

// Default IOutputProvider - writes to the console, prefixing every message
// with the name passed into the constructor (e.g. "Provider", "SqlProvider",
// "WhereDebug/IEnumerable"), mirroring the manual "  [Name] ..." prefixes the
// classes used to hard-code into every Console.WriteLine call.
internal class OutputProvider : IOutputProvider
{
    private readonly string _name;

    public OutputProvider(string name)
    {
        _name = name;
    }

    public void WriteLine(string message)
    {
        Console.WriteLine($"  [{_name}] {message}");
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }
}
