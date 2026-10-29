namespace BananaCompiler.Parser;

public sealed record SymbolAssignment(
    string Name,
    string? DataType,
    string? Value,
    string Scope = "global",
    int Address = 0,
    int Size = 0)
{
    public string AddressHex => $"0x{Address:X4}";
}
