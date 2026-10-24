using BananaCompiler.Lexer;
using BananaCompiler.Models;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class LexerDiagnosticTests
{
    private static List<Token> Tokenize(string source) =>
        new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")))
            .Tokenize(source, includeEndOfFile: true);

    [Theory]
    [InlineData("+.", "Operador aritmético no válido")]
    [InlineData("&", "Operador lógico no válido")]
    [InlineData("|", "Operador lógico no válido")]
    [InlineData("<.", "Operador relacional no válido")]
    [InlineData("1.", "Constante numérica no válida")]
    [InlineData("1E", "Constante numérica no válida")]
    [InlineData("1E+", "Constante numérica no válida")]
    [InlineData("1abc", "Identificador no válido")]
    [InlineData("\"hola", "Cadena sin terminar")]
    [InlineData("€", "Carácter no válido en el alfabeto")]
    public void LexicalErrorsHaveSpanishMessagesAndTheStartLocation(string malformed, string message)
    {
        List<Token> tokens = Tokenize($"start\n  {malformed}\nend");
        Token error = Assert.Single(tokens, token => token.Kind == TokenKinds.Error);

        Assert.Equal(message, error.Lexeme);
        Assert.Equal(2, error.Line);
        Assert.Equal(3, error.Column);
        Assert.Contains(tokens, token => token.Kind == TokenKinds.End);
        Assert.Equal(TokenKinds.EndOfFile, tokens[^1].Kind);
    }

    [Theory]
    [InlineData("1.")]
    [InlineData("1E")]
    [InlineData("1E+")]
    [InlineData("&")]
    [InlineData("|")]
    public void IncompleteLexemesAreNotLostAtEndOfFileOrBeforeAComment(string malformed)
    {
        foreach (string suffix in new[] { "", " # comentario", "\nwrite 1", "\r\nwrite 1" })
        {
            List<Token> tokens = Tokenize(malformed + suffix);
            Token error = Assert.Single(tokens, token => token.Kind == TokenKinds.Error);
            Assert.Equal(1, error.Line);
            Assert.Equal(1, error.Column);
            if (suffix.Contains("write"))
                Assert.Contains(tokens, token => token.Kind == TokenKinds.Write && token.Line == 2);
        }
    }

    [Fact]
    public void UnclosedStringDoesNotDiscardTheNextLine()
    {
        List<Token> tokens = Tokenize("start\n  write \"hola\n  x = 1\nend");

        Token error = Assert.Single(tokens, token => token.Kind == TokenKinds.Error);
        Assert.Equal("Cadena sin terminar", error.Lexeme);
        Assert.Equal(2, error.Line);
        Assert.Equal(9, error.Column);
        Assert.Contains(tokens, token => token.Kind == TokenKinds.Identifier && token.Lexeme == "x" && token.Line == 3);
    }

    [Fact]
    public void ValidOperatorsCommentsAndQuotedSymbolsDoNotGenerateLexicalErrors()
    {
        string source = "start\nx=1+2*3^2\ny=1E+2\nflag=!(x<10)&&true||false\ntext=\"# € & |\" # € comentario\nend";
        List<Token> tokens = Tokenize(source);

        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        Assert.Contains(tokens, token => token.Kind == TokenKinds.ScientificNumber && token.Lexeme == "1E+2");
        Assert.Contains(tokens, token => token.Kind == TokenKinds.String && token.Lexeme == "\"# € & |\"");
    }

    [Fact]
    public void EveryErrorStateInTheDatabaseHasAMessageAndAToken()
    {
        TransitionTableRow[] rows = SqliteTransitionTableSource.ReadRows(Path.Combine(AppContext.BaseDirectory, "data.db"))
            .Where(row => string.Equals(row.Token, TokenKinds.Error, StringComparison.OrdinalIgnoreCase)).ToArray();

        Assert.Equal(5, rows.Length);
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Message)));
        Assert.Equal(5, rows.Select(row => row.Message).Distinct().Count());
    }
}
