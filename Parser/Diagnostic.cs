using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public enum DiagnosticKind
{
    Syntax,
    Semantic
}

public sealed record Diagnostic(
    DiagnosticKind Kind,
    string Code,
    Token Token,
    string Message,
    Token? RelatedToken = null)
{
    public string Format() => $"{(Kind == DiagnosticKind.Syntax ? "Sintáctico" : "Semántico")}: {Message}";

    public static string DescribeToken(Token token) => token.Kind == TokenKinds.EndOfFile
        ? "fin de archivo"
        : $"'{token.Lexeme}'";
}
