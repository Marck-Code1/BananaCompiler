namespace BananaCompiler.Parser;

public static class SemanticTypes
{
    public const string Integer = "Entero";
    public const string Decimal = "Decimal";
    public const string Exponential = "Exponencial";
    public const string String = "string";
    public const string Boolean = "bool";
    public const string Invalid = "<inválido>";
}

public sealed record SemanticValue(
    string? DataType,
    string? Value = null,
    bool IsConstant = false)
{
    public bool IsUnknown => DataType is null;
    public bool IsInvalid => DataType == SemanticTypes.Invalid;

    public bool IsNumeric => DataType is
        SemanticTypes.Integer or
        SemanticTypes.Decimal or
        SemanticTypes.Exponential;

    public static SemanticValue Unknown { get; } =
        new SemanticValue(DataType: null, Value: null, IsConstant: false);

    public static SemanticValue Invalid { get; } =
        new SemanticValue(DataType: SemanticTypes.Invalid, Value: null, IsConstant: false);
}
