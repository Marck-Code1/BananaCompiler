namespace BananaCompiler.Parser;

public sealed class ScopedSymbolTable
{
    public const int BaseAddress = 0x1000;
    private const int Alignment = 4;

    // Conserva los símbolos de ámbitos cerrados para el reporte.
    private readonly List<SymbolAssignment> entries = [];
    private readonly List<Scope> scopeStack = [new("global")];
    private readonly Stack<int> transactionStarts = [];
    private readonly List<Action> undoActions = [];

    private sealed class Scope(string path)
    {
        public string Path { get; } = path;
        public Dictionary<string, int> Symbols { get; } = new(StringComparer.Ordinal);
    }

    public IReadOnlyList<SymbolAssignment> Entries => entries;

    public string CurrentScope => scopeStack[^1].Path;

    public void PushScope(string scope) => scopeStack.Add(new Scope($"{CurrentScope} > {scope}"));

    public void PopScope()
    {
        if (scopeStack.Count > 1)
            scopeStack.RemoveAt(scopeStack.Count - 1);
    }

    public static int SizeOf(string? dataType, string? value) => dataType switch
    {
        SemanticTypes.Integer => 4,
        SemanticTypes.Decimal => 8,
        SemanticTypes.Exponential => 16,
        SemanticTypes.Boolean => 1,
        SemanticTypes.String => value is null ? 2 : (Unquote(value).Length * 2) + 2,
        _ => 0,
    };

    private static string Unquote(string s) =>
        s.Length >= 2 && s.StartsWith('"') && s.EndsWith('"')
            ? s[1..^1]
            : s;

    private static int Align(int offset) =>
        (offset + Alignment - 1) / Alignment * Alignment;

    public SymbolAssignment Declare(string name, string? dataType = null, string? value = null)
    {
        Scope scope = scopeStack[^1];
        if (scope.Symbols.ContainsKey(name))
            throw new InvalidOperationException($"El símbolo '{name}' ya está declarado en el ámbito actual.");

        var entry = new SymbolAssignment(name, dataType, value, CurrentScope, Size: SizeOf(dataType, value));
        scope.Symbols.Add(name, entries.Count);
        entries.Add(entry);
        if (transactionStarts.Count > 0)
            undoActions.Add(() =>
            {
                scope.Symbols.Remove(name);
                entries.RemoveAt(entries.Count - 1);
            });
        return entry;
    }

    // Un tipo null conserva el anterior; un valor null invalida el valor conocido.
    public SymbolAssignment Assign(string name, string? dataType, string? value)
    {
        int index = FindVisibleIndex(name);
        if (index < 0)
            return Declare(name, dataType, value);

        SymbolAssignment previous = entries[index];
        string? type = dataType ?? previous.DataType;
        var updated = previous with
        {
            DataType = type,
            Value = value,
            Address = 0,
            Size = SizeOf(type, value),
        };
        entries[index] = updated;
        if (transactionStarts.Count > 0)
            undoActions.Add(() => entries[index] = previous);
        return updated;
    }

    // Cada instrucción confirma sus cambios o los revierte al fallar. Las
    // instrucciones anidadas conservan su registro hasta confirmar el bloque.
    // El registro contiene solo cambios realizados, no copias de toda la tabla.
    internal void BeginTransaction() => transactionStarts.Push(undoActions.Count);

    internal void CommitTransaction()
    {
        transactionStarts.Pop();
        if (transactionStarts.Count == 0)
            undoActions.Clear();
    }

    internal void RollbackTransaction()
    {
        int start = transactionStarts.Pop();
        for (int i = undoActions.Count - 1; i >= start; i--)
            undoActions[i]();
        undoActions.RemoveRange(start, undoActions.Count - start);
    }

    public SymbolAssignment? Lookup(string name)
    {
        int index = FindVisibleIndex(name);
        return index >= 0 ? entries[index] : null;
    }

    private int FindVisibleIndex(string name)
    {
        for (int i = scopeStack.Count - 1; i >= 0; i--)
        {
            if (scopeStack[i].Symbols.TryGetValue(name, out int index))
                return index;
        }
        return -1;
    }

    // Direcciones simuladas, calculadas al finalizar el análisis, no reservas reales.
    // Incluso los tipos desconocidos ocupan una reserva mínima de 4 bytes.
    public void AssignAddresses()
    {
        int offset = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            offset = Align(offset);
            SymbolAssignment entry = entries[i];
            entries[i] = entry with { Address = BaseAddress + offset };
            offset += Math.Max(entry.Size, Alignment);
        }
    }
}
