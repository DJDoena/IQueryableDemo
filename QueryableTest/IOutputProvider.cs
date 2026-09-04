namespace QueryableTest;

// Abstraction over Console.WriteLine so every class in this demo logs
// through the same small surface instead of calling Console directly.
// Each implementation carries its own "name" (e.g. "Provider", "SqlProvider"),
// supplied via its constructor, and prefixes every message with it - this is
// what previously showed up as a hand-typed "[Provider]"/"[SqlProvider]"
// literal at the start of every log line.
internal interface IOutputProvider
{
    void WriteLine(string message);

    void WriteLine();
}
