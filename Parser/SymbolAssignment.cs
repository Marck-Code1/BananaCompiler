namespace BananaCompiler.Parser;

/// <summary>
/// Entrada de la tabla de símbolos con ámbito y dirección de memoria simulada.
/// </summary>
public sealed record SymbolAssignment(
    string Name,
    string? DataType,
    string? Value,
    string Scope = "global",
    int Address = 0,
    int Size = 0)
{
    /// <summary>Dirección en formato hexadecimal estilo 0x1000.</summary>
    public string AddressHex => $"0x{Address:X4}";
}
