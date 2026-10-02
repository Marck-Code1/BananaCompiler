using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public enum DiagnosticKind
{
    Syntax,
    Semantic
}

/// <summary>Diagnóstico listo para mostrar, con categoría, código y ubicación de origen.</summary>
public sealed record Diagnostic(
    DiagnosticKind Kind,
    string Code,
    Token Token,
    string Message,
    Token? RelatedToken = null)
{
    public static string DescribeToken(Token token) => token.Kind == TokenKinds.EndOfFile
        ? "fin de archivo"
        : $"'{token.Lexeme}'";
}
