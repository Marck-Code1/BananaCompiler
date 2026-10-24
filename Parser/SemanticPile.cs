namespace BananaCompiler.Parser;

// Registra la traza de las reducciones; los atributos tipados están en SemanticStack.
public sealed class SemanticPile
{
    private readonly List<string> entries = [];
    private readonly List<string> steps = [];
    private readonly List<string> snapshots = [];

    public IReadOnlyList<string> Entries => entries;

    public IReadOnlyList<string> Steps => steps;

    public IReadOnlyList<string> Snapshots => snapshots;

    public int Count => entries.Count;

    // Un nivel mayor indica mayor precedencia; ver Docs/Grammar.md.
    public static int PrecedenceOf(string operatorKind) => operatorKind switch
    {
        Models.TokenKinds.Power or "^" => 7,
        Models.TokenKinds.Multiply or Models.TokenKinds.Divide or Models.TokenKinds.Modulo
            or "*" or "/" or "%" => 5,
        Models.TokenKinds.Plus or Models.TokenKinds.Minus or "+" or "-" => 4,
        Models.TokenKinds.Not or "!" => 3,
        Models.TokenKinds.LessThan or Models.TokenKinds.LessThanOrEqual or Models.TokenKinds.GreaterThan or Models.TokenKinds.GreaterThanOrEqual or Models.TokenKinds.Equal or Models.TokenKinds.NotEqual
            or "<" or "<=" or ">" or ">=" or "==" or "!=" => 2,
        Models.TokenKinds.And or "&&" => 1,
        Models.TokenKinds.Or or "||" => 0,
        _ => -1,
    };

    public static string AssociativityOf(string operatorKind) => operatorKind switch
    {
        Models.TokenKinds.Power or "^" => "derecha",
        Models.TokenKinds.Not or "!" => "derecha",
        _ => "izquierda",
    };

    public static string Describe(string operatorKind) =>
        $"op '{operatorKind}' nivel={PrecedenceOf(operatorKind)} assoc={AssociativityOf(operatorKind)}";

    public void PushOperand(string label)
    {
        entries.Add(label);
        Snapshot($"push {label}");
    }

    public void ReduceUnary(string op, string operand, string resultType)
    {
        string step = $"{op}{operand} => {resultType}  [{Describe(op)}]";
        steps.Add(step);
        if (entries.Count > 0)
            entries.RemoveAt(entries.Count - 1);
        entries.Add($"({op}{operand}:{resultType})");
        Snapshot(step);
    }

    public void ReduceBinary(string op, string left, string right, string resultType)
    {
        string step = $"{left} {op} {right} => {resultType}  [{Describe(op)}]";
        steps.Add(step);
        if (entries.Count >= 2)
            entries.RemoveRange(entries.Count - 2, 2);
        else
            entries.Clear();
        entries.Add($"({left} {op} {right}:{resultType})");
        Snapshot(step);
    }

    public string ToPostfix() => string.Join(" ", snapshots.Count == 0 ? [] : steps);

    public void Restore(int depth)
    {
        while (entries.Count > depth)
            entries.RemoveAt(entries.Count - 1);
    }

    public void RestoreTrace(int stepsDepth, int snapshotsDepth)
    {
        if (steps.Count > stepsDepth)
            steps.RemoveRange(stepsDepth, steps.Count - stepsDepth);
        if (snapshots.Count > snapshotsDepth)
            snapshots.RemoveRange(snapshotsDepth, snapshots.Count - snapshotsDepth);
    }

    public void Clear()
    {
        entries.Clear();
    }

    private void Snapshot(string what)
    {
        snapshots.Add($"[{entries.Count}] {string.Join(" | ", entries)}   <-- {what}");
    }
}
