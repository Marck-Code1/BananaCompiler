namespace BananaCompiler.Parser;

public sealed class SemanticStack
{
    private readonly Stack<SemanticValue> values = [];

    public int Count => values.Count;

    public void Push(SemanticValue value) => values.Push(value);

    public SemanticValue Pop() => values.Pop();

    public SemanticValue Peek() => values.Peek();

    public void Restore(int depth)
    {
        if (depth < 0 || depth > values.Count)
            throw new ArgumentOutOfRangeException(nameof(depth));

        while (values.Count > depth)
            values.Pop();
    }
}
