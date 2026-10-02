namespace BananaCompiler.Parser;

/// <summary>
/// Tabla de símbolos con ámbitos (scopes) y simulación de memoria real.
///
/// <para><b>Ámbitos.</b> Pila de ámbitos: siempre existe "global". Cada
/// bloque (if / else / while / for / do) apila un ámbito del estilo
/// <c>while@3</c> (palabra + línea). La ruta completa
/// (<c>global &gt; while@3 &gt; if@5</c>) se guarda en cada símbolo como su
/// ámbito de declaración. La búsqueda es por visibilidad: un identificador
/// es visible si su ámbito es prefijo de la ruta actual; ante
/// sombreado (shadowing) gana la declaración más interna.</para>
///
/// <para><b>Memoria simulada.</b> Se simula un segmento de datos que empieza
/// en <see cref="BaseAddress"/> (0x1000). Cada variable recibe una dirección
/// (offset) al declararse, con alineación a 4 bytes —como haría un
/// compilador x86—. Tamaños: Entero=4, Decimal=8, Exponencial=16, bool=1,
/// string=2×caracteres+2 (UTF-16 + terminador), desconocido=0 (sin reserva
/// hasta conocer el tipo). Si el tipo cambia después, se conserva la
/// dirección original y solo se actualiza el tamaño (se documenta como
/// fragmentación, igual que una reasignación real).</para>
/// </summary>
public sealed class ScopedSymbolTable
{
    public const int BaseAddress = 0x1000;
    private const int Alignment = 4;

    private readonly List<SymbolAssignment> entries = [];
    private readonly List<string> scopeStack = ["global"];
    private int nextOffset;

    public IReadOnlyList<SymbolAssignment> Entries => entries;

    public string CurrentScope => string.Join(" > ", scopeStack);

    public void PushScope(string scope) => scopeStack.Add(scope);

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
    /// Declara o actualiza una variable en el ámbito visible actual.
    /// Si ya existe una entrada visible con el mismo nombre en un ámbito
    /// accesible, se actualiza (conservando dirección y ámbito de
    /// declaración). Si solo existe en un ámbito inaccesible (otra rama),
    /// se crea una entrada nueva que la sombrea.
    /// </summary>
    public SymbolAssignment DeclareOrUpdate(string name, string? dataType, string? value)
    {
        int index = FindVisibleIndex(name);
        if (index >= 0)
        {
            SymbolAssignment prev = entries[index];
            string? type = dataType ?? prev.DataType;
            int size = SizeOf(type, value ?? prev.Value) is int s && s != 0 ? s : prev.Size;
            var updated = prev with
            {
                DataType = type,
                Value = value ?? prev.Value,
                Size = size,
            };
            entries[index] = updated;
            return updated;
        }

        int address = BaseAddress + Align(nextOffset);
        int newSize = SizeOf(dataType, value);
        var entry = new SymbolAssignment(name, dataType, value, CurrentScope, address, newSize);
        entries.Add(entry);
        nextOffset = Align(nextOffset) + Math.Max(newSize, Alignment);
        return entry;
    }

    /// <summary>Tipo conocido de un identificador visible, o null si no existe.</summary>
    public SymbolAssignment? Lookup(string name)
    {
        int index = FindVisibleIndex(name);
        return index >= 0 ? entries[index] : null;
    }

    private int FindVisibleIndex(string name)
    {
        string current = CurrentScope;
        int best = -1;
        int bestScopeLen = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!string.Equals(entries[i].Name, name, StringComparison.Ordinal))
                continue;
            // Visible si su ámbito es prefijo de la ruta actual.
            if (!current.StartsWith(entries[i].Scope, StringComparison.Ordinal))
                continue;
            if (entries[i].Scope.Length > bestScopeLen)
            {
                bestScopeLen = entries[i].Scope.Length;
                best = i;
            }
        }
        return best;
    }
}
