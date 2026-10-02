namespace BananaCompiler.Parser;

/// <summary>
/// Tabla de símbolos con una pila de ámbitos independientes.
/// Las rutas textuales solo sirven para mostrar el ámbito de declaración;
/// la búsqueda recorre los diccionarios activos del más interno al global.
/// Las entradas de ámbitos cerrados se conservan para el reporte.
/// <para>Las direcciones son una simulación didáctica: se calculan al finalizar
/// el análisis mediante <see cref="AssignAddresses"/>, con alineación a 4 bytes.
/// No representan reservas reales de memoria ni direcciones estables durante el parseo.</para>
/// </summary>
public sealed class ScopedSymbolTable
{
    public const int BaseAddress = 0x1000;
    private const int Alignment = 4;

    private readonly List<SymbolAssignment> entries = [];
    private readonly List<Scope> scopeStack = [new("global")];

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

    /// <summary>Tamaño en bytes para un tipo semántico.</summary>
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

    /// <summary>
    /// Declara en el ámbito actual, incluso si existe una variable externa con el mismo nombre.
    /// </summary>
    public SymbolAssignment Declare(string name, string? dataType = null, string? value = null)
    {
        Scope scope = scopeStack[^1];
        if (scope.Symbols.ContainsKey(name))
            throw new InvalidOperationException($"El símbolo '{name}' ya está declarado en el ámbito actual.");

        var entry = new SymbolAssignment(name, dataType, value, CurrentScope, Size: SizeOf(dataType, value));
        scope.Symbols.Add(name, entries.Count);
        entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Asigna a la variable visible más cercana, o la declara si no existe.
    /// Un tipo null conserva el tipo previo; un valor null sustituye el valor
    /// previo porque el nuevo valor no se conoce durante el análisis.
    /// </summary>
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
        return updated;
    }

    /// <summary>Entrada de un identificador visible, o null si no existe.</summary>
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

    /// <summary>
    /// Calcula un layout sin solapamientos a partir de los tamaños finales.
    /// Los tipos desconocidos tienen tamaño 0, con una reserva simulada mínima de 4 bytes.
    /// Las cadenas de valor desconocido solo tienen una estimación mínima de 2 bytes.
    /// </summary>
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
