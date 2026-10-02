namespace BananaCompiler.Parser;

/// <summary>
/// Pila semántica didáctica / de trazabilidad (Semantic Pile).
///
/// <para>
/// NO sustituye a <see cref="SemanticStack"/> (que es la pila real de
/// atributos tipados usada en las reducciones). Esta clase existe para
/// <b>visualizar la jerarquía matemática</b>: cada operador tiene un nivel
/// de precedencia y una asociatividad (ver <see cref="PrecedenceOf"/>), y
/// cada reducción queda registrada como texto legible más una foto de la
/// pila. El parser crea una instancia por expresión y la vacía al terminar,
/// igual que hace con la pila real.
/// </para>
/// </summary>
public sealed class SemanticPile
{
    private readonly List<string> entries = [];
    private readonly List<string> steps = [];
    private readonly List<string> snapshots = [];

    /// <summary>Entradas actuales de la pila (operandos y resultados intermedios).</summary>
    public IReadOnlyList<string> Entries => entries;

    /// <summary>Reducciones registradas, en orden: "a + b =&gt; Entero".</summary>
    public IReadOnlyList<string> Steps => steps;

    /// <summary>Foto de la pila después de cada operación (para UI/depuración).</summary>
    public IReadOnlyList<string> Snapshots => snapshots;

    public int Count => entries.Count;

    // ---- Tabla de jerarquía (mayor número = mayor precedencia) ----
    // Refleja Docs/Grammar.md § "Tabla de precedencia resultante".
    // Acepta tanto el kind del token (OA1, OR1, …) como el lexema (+, <, …).
    public static int PrecedenceOf(string operatorKind) => operatorKind switch
    {
        Models.TokenKinds.Power or "^" => 7,                        // ^  derecha
        Models.TokenKinds.Multiply or Models.TokenKinds.Divide or Models.TokenKinds.Modulo
            or "*" or "/" or "%" => 5,                              // *, /, %
        Models.TokenKinds.Plus or Models.TokenKinds.Minus or "+" or "-" => 4, // + - binarios
        Models.TokenKinds.Not or "!" => 3,                         // !  derecha
        Models.TokenKinds.LessThan or Models.TokenKinds.LessThanOrEqual or Models.TokenKinds.GreaterThan or Models.TokenKinds.GreaterThanOrEqual or Models.TokenKinds.Equal or Models.TokenKinds.NotEqual
            or "<" or "<=" or ">" or ">=" or "==" or "!=" => 2,     // relacionales
        Models.TokenKinds.And or "&&" => 1,                        // &&
        Models.TokenKinds.Or or "||" => 0,                        // ||
        _ => -1,
    };

    public static string AssociativityOf(string operatorKind) => operatorKind switch
    {
        Models.TokenKinds.Power or "^" => "derecha",
        Models.TokenKinds.Not or "!" => "derecha",
        _ => "izquierda",
    };

    /// <summary>Descripción humana de la jerarquía, para reportes.</summary>
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

    /// <summary>Forma postfija (notación polaca inversa) acumulada hasta ahora.</summary>
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
