using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class ParserSymbolTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    [Fact]
    public void BranchSymbolsRemainSeparateInTheResult()
    {
        ParseResult result = Parse("start if true then x = 1 else x = 2 endif end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
        Assert.All(result.ScopedSymbols, symbol => Assert.Equal("x", symbol.Name));
        Assert.NotEqual(result.ScopedSymbols[0].Scope, result.ScopedSymbols[1].Scope);
        Assert.NotEqual(result.ScopedSymbols[0].Address, result.ScopedSymbols[1].Address);
    }

    [Fact]
    public void BlocksOnTheSameLineHaveSeparateDeclarationsAndLabels()
    {
        ParseResult result = Parse("start if true then x = 1 endif if true then x = \"text\" endif end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
        Assert.NotEqual(result.ScopedSymbols[0].Scope, result.ScopedSymbols[1].Scope);
        Assert.Equal(SemanticTypes.Integer, result.ScopedSymbols[0].DataType);
        Assert.Equal(SemanticTypes.String, result.ScopedSymbols[1].DataType);
    }

    [Fact]
    public void ClosedScopeDoesNotLeakItsTypeIntoAnExpression()
    {
        ParseResult result = Parse("start if true then local = \"text\" endif y = local + 1 end");

        // El lenguaje sigue admitiendo identificadores de tipo desconocido;
        // lo importante es no recuperar el tipo string de un bloque cerrado.
        Assert.True(result.Success);
        Assert.Equal(SemanticTypes.Integer, result.ScopedSymbols.Single(symbol => symbol.Name == "y").DataType);
    }

    [Fact]
    public void ClosedScopeDoesNotSupplyThePreviousAssignmentType()
    {
        ParseResult result = Parse("start if true then x = \"text\" endif x = missing end");

        Assert.True(result.Success);
        SymbolAssignment global = result.ScopedSymbols.Single(symbol => symbol.Scope == "global");
        Assert.Equal("x", global.Name);
        Assert.Null(global.DataType);
        Assert.Null(global.Value);
    }

    [Theory]
    [InlineData("x = x + 1")]
    [InlineData("x = 1 + 2")]
    [InlineData("read x")]
    [InlineData("rand x, 10")]
    public void UnknownRuntimeValueReplacesPreviousConstant(string instruction)
    {
        ParseResult result = Parse($"start x = 1 {instruction} end");

        Assert.True(result.Success);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(SemanticTypes.Integer, x.DataType);
        Assert.Null(x.Value);
    }

    [Fact]
    public void InvalidAssignmentDoesNotOverwriteTheLastValidAssignment()
    {
        ParseResult result = Parse("start x = 1 x = true + 2 end");

        Assert.Single(result.SemanticErrors);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(SemanticTypes.Integer, x.DataType);
        Assert.Equal("1", x.Value);
    }

    [Fact]
    public void ForControlVariableIsLocalAndDoesNotOverwriteAnOuterVariable()
    {
        ParseResult result = Parse("start i = \"outer\" for i from 1 to 2 do i = 3 endfor write i end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
        SymbolAssignment outer = result.ScopedSymbols.Single(symbol => symbol.Scope == "global");
        Assert.Equal(SemanticTypes.String, outer.DataType);
        Assert.Equal("\"outer\"", outer.Value);
        Assert.Equal(SemanticTypes.Integer, result.ScopedSymbols.Single(symbol => symbol.Scope != "global").DataType);
    }

    [Fact]
    public void AssignmentInNestedBlockUpdatesVisibleOuterVariable()
    {
        ParseResult result = Parse("start x = 1 if true then x = 2 endif end");

        Assert.True(result.Success);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal("global", x.Scope);
        Assert.Equal("2", x.Value);
    }

    [Fact]
    public void DoWhileConditionCanSeeBodySymbolsButFollowingCodeCannot()
    {
        ParseResult result = Parse("start do local = \"text\" end while local y = local + 1 end");

        SemanticError error = Assert.Single(result.SemanticErrors);
        Assert.Contains("debe ser booleana", error.Message);
        Assert.Equal(SemanticTypes.Integer, result.ScopedSymbols.Single(symbol => symbol.Name == "y").DataType);
    }

    [Fact]
    public void FinalAddressesReflectTypeChanges()
    {
        ParseResult result = Parse("start a = 1 b = 2 a = 1.5 end");

        Assert.True(result.Success);
        SymbolAssignment a = result.ScopedSymbols.Single(symbol => symbol.Name == "a");
        SymbolAssignment b = result.ScopedSymbols.Single(symbol => symbol.Name == "b");
        Assert.Equal(8, a.Size);
        Assert.True(b.Address >= a.Address + a.Size);
    }

    [Fact]
    public void ExistingValidSamplesStillParseSuccessfully()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);
        foreach (string sample in samples)
        {
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló {Path.GetFileName(sample)}: " +
                string.Join("; ", result.Errors.Select(error => error.Format())
                    .Concat(result.SemanticErrors.Select(error => error.Format()))));
        }
    }

    [Fact]
    public void ExistingInvalidDiagnosticsStillReportErrorsWithoutThrowing()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "InvalidDiagnostics");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);
        foreach (string sample in samples)
        {
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.False(result.Success, $"No se detectaron errores en {Path.GetFileName(sample)}.");
            Assert.NotEmpty(result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format())));
            Assert.All(result.ScopedSymbols, symbol => Assert.True(symbol.Address >= ScopedSymbolTable.BaseAddress));
        }
    }
}
