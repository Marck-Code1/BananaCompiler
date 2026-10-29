using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class DiagnosticDefinitionTests
{
    private static readonly (string Literal, string Type, int NumericRank)[] Values =
    [
        ("1", SemanticTypes.Integer, 0),
        ("1.5", SemanticTypes.Decimal, 1),
        ("1E+2", SemanticTypes.Exponential, 2),
        ("\"text\"", SemanticTypes.String, -1),
        ("true", SemanticTypes.Boolean, -1)
    ];

    private static ParseResult Parse(string source)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        List<Token> tokens = lexer.Tokenize(source, includeEndOfFile: true);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    [Theory]
    [InlineData("start read 1 end", "SYN_EXPECTED_TOKEN", "1", "identificador")]
    [InlineData("start x = write 1 end", "SYN_MISSING_EXPRESSION", "=", "una expresión")]
    [InlineData("start x = 1 + write 1 end", "SYN_MISSING_OPERAND", "+", "un operando")]
    [InlineData("start if true write 1 endif end", "SYN_MISSING_TOKEN", "write", "'then'")]
    [InlineData("start while true do end", "SYN_MISSING_CLOSER", "end", "'endwhile'")]
    [InlineData("start if true then endwhile end", "SYN_MISMATCHED_CLOSER", "endwhile", "'endif'")]
    [InlineData("start endif end", "SYN_UNEXPECTED_CLOSER", "endif", "ningún bloque abierto")]
    [InlineData("start if true then else else endif end", "SYN_DUPLICATE_ELSE", "else", "rama 'else'")]
    [InlineData("start x = 1 < 2 < 3 end", "SYN_CHAINED_COMPARISON", "<", "comparaciones encadenadas")]
    [InlineData("start x = 1 99 end", "SYN_UNEXPECTED_TOKEN", "99", "no puede continuar")]
    public void EverySyntaxCodeHasTheExpectedMeaningAndLocation(string source, string code, string lexeme, string detail)
    {
        ParseResult result = Parse(source);
        ParseError error = Assert.Single(result.Errors);

        Assert.False(result.Success);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal(code, error.Code);
        Assert.Equal(lexeme, error.Token.Lexeme);
        Assert.Equal(1, error.Token.Line);
        Assert.Equal(source.LastIndexOf(lexeme, StringComparison.Ordinal) + 1, error.Token.Column);
        Assert.Contains(detail, error.Format());
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticKind.Syntax, diagnostic.Kind);
        Assert.StartsWith("Sintáctico: ", diagnostic.Format());
        Assert.DoesNotContain(code, diagnostic.Format());
    }

    [Fact]
    public void AllBinaryOperatorsValidateTheCompleteTypeMatrix()
    {
        string[] arithmetic = ["+", "-", "*", "/", "%", "^"];
        string[] ordering = ["<", "<=", ">", ">="];
        string[] equality = ["==", "!="];
        string[] logical = ["&&", "||"];

        foreach (string op in arithmetic.Concat(ordering).Concat(equality).Concat(logical))
        foreach (var left in Values)
        foreach (var right in Values)
        {
            bool bothNumeric = left.NumericRank >= 0 && right.NumericRank >= 0;
            bool accepted = arithmetic.Contains(op) || ordering.Contains(op) ? bothNumeric :
                equality.Contains(op) ? bothNumeric || left.Type == right.Type :
                left.Type == SemanticTypes.Boolean && right.Type == SemanticTypes.Boolean;
            ParseResult result = Parse($"start\n  value = {left.Literal} {op} {right.Literal}\nend");

            Assert.Empty(result.Errors);
            Assert.True(result.Success == accepted, $"Combinación incorrecta: {left.Type} {op} {right.Type}");
            if (accepted)
            {
                Assert.Empty(result.SemanticErrors);
                string expectedType = arithmetic.Contains(op)
                    ? Values[Math.Max(left.NumericRank, right.NumericRank)].Type
                    : SemanticTypes.Boolean;
                Assert.Equal(expectedType, Assert.Single(result.ScopedSymbols).DataType);
            }
            else
            {
                SemanticError error = Assert.Single(result.SemanticErrors);
                Assert.Equal("SEM_TYPE_MISMATCH", error.Code);
                Assert.Equal(op, error.Token.Lexeme);
                Assert.Equal(2, error.Token.Line);
                Assert.Equal(12 + left.Literal.Length, error.Token.Column);
                Assert.Contains($"{left.Type} y {right.Type}", error.Message);
                Assert.Empty(result.ScopedSymbols);
                Assert.Empty(result.SemanticPileLog);
            }
        }
    }

    [Fact]
    public void AllUnaryOperatorsValidateEachType()
    {
        foreach (string op in new[] { "+", "-", "!" })
        foreach (var value in Values)
        {
            bool accepted = op == "!" ? value.Type == SemanticTypes.Boolean : value.NumericRank >= 0;
            ParseResult result = Parse($"start\n  x = {op}{value.Literal}\nend");

            Assert.Empty(result.Errors);
            Assert.True(result.Success == accepted, $"Combinación incorrecta: {op}{value.Type}");
            if (!accepted)
            {
                SemanticError error = Assert.Single(result.SemanticErrors);
                Assert.Equal("SEM_TYPE_MISMATCH", error.Code);
                Assert.Equal(op, error.Token.Lexeme);
                Assert.Equal(2, error.Token.Line);
                Assert.Equal(7, error.Token.Column);
                Assert.Contains(value.Type, error.Message);
                Assert.Empty(result.ScopedSymbols);
            }
        }
    }

    [Fact]
    public void AssignmentsValidateEverySourceAndDestinationType()
    {
        foreach (var destination in Values)
        foreach (var source in Values)
        {
            bool accepted = destination.Type == source.Type ||
                destination.NumericRank >= 0 && source.NumericRank >= 0 && source.NumericRank <= destination.NumericRank;
            ParseResult result = Parse($"start\n  x = {destination.Literal}\n  x = {source.Literal}\nend");

            Assert.Empty(result.Errors);
            Assert.True(result.Success == accepted, $"Asignación incorrecta: {destination.Type} <- {source.Type}");
            SymbolAssignment x = Assert.Single(result.ScopedSymbols);
            Assert.Equal(destination.Type, x.DataType);
            Assert.Equal(accepted ? source.Literal : destination.Literal, x.Value);
            if (!accepted)
            {
                SemanticError error = Assert.Single(result.SemanticErrors);
                Assert.Equal("SEM_ASSIGNMENT_TYPE", error.Code);
                Assert.Equal("x", error.Token.Lexeme);
                Assert.Equal(3, error.Token.Line);
                Assert.Equal(3, error.Token.Column);
                Assert.Contains(source.Type, error.Message);
                Assert.Contains(destination.Type, error.Message);
            }
        }
    }

    [Fact]
    public void EveryConditionRequiresBooleanAndPointsAtItsExpression()
    {
        foreach (var value in Values)
        foreach (string template in new[] { "if {0} then endif", "while {0} do endwhile", "do end while {0}" })
        {
            string source = $"start\n  {string.Format(template, value.Literal)}\nend";
            ParseResult result = Parse(source);

            Assert.Empty(result.Errors);
            Assert.Equal(value.Type == SemanticTypes.Boolean, result.Success);
            if (!result.Success)
            {
                SemanticError error = Assert.Single(result.SemanticErrors);
                Assert.Equal("SEM_CONDITION_TYPE", error.Code);
                Assert.Equal(value.Literal, error.Token.Lexeme);
                Assert.Equal(2, error.Token.Line);
                Assert.Equal(source.Split('\n')[1].IndexOf(value.Literal, StringComparison.Ordinal) + 1, error.Token.Column);
                Assert.Contains(value.Type, error.Message);
            }
        }
    }

    [Theory]
    [InlineData("move true, 1", "true", "primer argumento")]
    [InlineData("move 1, false", "false", "segundo argumento")]
    [InlineData("rand x, true", "true", "argumento de 'rand'")]
    [InlineData("for i from true to 2 do endfor", "true", "límite inicial")]
    [InlineData("for i from 1 to false do endfor", "false", "límite final")]
    public void ArgumentErrorsIdentifyTheSpecificArgument(string instruction, string lexeme, string role)
    {
        string source = $"start\n  {instruction}\nend";
        ParseResult result = Parse(source);
        SemanticError error = Assert.Single(result.SemanticErrors);

        Assert.Empty(result.Errors);
        Assert.Equal("SEM_ARGUMENT_TYPE", error.Code);
        Assert.Equal(lexeme, error.Token.Lexeme);
        Assert.Equal(2, error.Token.Line);
        Assert.Equal(source.Split('\n')[1].IndexOf(lexeme, StringComparison.Ordinal) + 1, error.Token.Column);
        Assert.Contains(role, error.Message);
    }

    [Fact]
    public void BreakErrorsAreContextualAndDoNotLeakAcrossClosedLoops()
    {
        ParseResult result = Parse("start\n  while true do break endwhile\n  break\nend");
        SemanticError error = Assert.Single(result.SemanticErrors);

        Assert.Empty(result.Errors);
        Assert.Equal("SEM_BREAK_OUTSIDE_LOOP", error.Code);
        Assert.Equal("break", error.Token.Lexeme);
        Assert.Equal(3, error.Token.Line);
        Assert.Equal(3, error.Token.Column);
    }
}
