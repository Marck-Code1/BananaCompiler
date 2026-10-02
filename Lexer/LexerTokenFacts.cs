using BananaCompiler.Models;

namespace BananaCompiler.Lexer;

internal static class LexerTokenFacts
{
    private static readonly HashSet<string> NumericTokens =
    [
        TokenKinds.Number,
        TokenKinds.RealNumber,
        TokenKinds.ScientificNumber
    ];

    public static bool IsNumeric(string? token) =>
        token is not null && NumericTokens.Contains(token);

    public static bool IsIdentifierStart(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
