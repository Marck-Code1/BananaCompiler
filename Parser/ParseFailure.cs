namespace BananaCompiler.Parser;

public sealed class ParseFailure : Exception
{
    public ParseFailure()
        : base("Parse failed.")
    {
    }
}
