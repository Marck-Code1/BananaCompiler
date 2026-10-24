using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class SemanticRulesTests
{
    private static ParseResult Parse(string source)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        List<Token> tokens = lexer.Tokenize(source, includeEndOfFile: true);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    [Theory]
    [InlineData("write missing")]
    [InlineData("x = x + 1")]
    [InlineData("x = missing")]
    [InlineData("if missing then endif")]
    [InlineData("while missing < 2 do endwhile")]
    [InlineData("move missing, 1")]
    [InlineData("rand x, missing")]
    [InlineData("read missing")]
    public void MissingNamesAreErrorsWithoutCascadingTypeErrors(string instruction)
    {
        ParseResult result = Parse($"start {instruction} end");

        Assert.False(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal("SEM_UNDECLARED_VARIABLE", Assert.Single(result.SemanticErrors).Code);
        Assert.Empty(result.ScopedSymbols);
        Assert.Empty(result.SemanticPileLog);
    }

    [Fact]
    public void MissingNameDiagnosticPointsAtTheUse()
    {
        ParseResult result = Parse("start\n  write missing\nend");
        SemanticError error = Assert.Single(result.SemanticErrors);

        Assert.Equal("missing", error.Token.Lexeme);
        Assert.Equal(2, error.Token.Line);
        Assert.Equal(9, error.Token.Column);
        Assert.Contains("ámbito visible", error.Message);
    }

    [Theory]
    [InlineData("if true then local = 1 endif")]
    [InlineData("if false then else local = 1 endif")]
    [InlineData("while true do local = 1 endwhile")]
    [InlineData("do local = 1 end while true")]
    [InlineData("for local from 1 to 2 do endfor")]
    public void ClosedBlockNamesAreNotVisible(string block)
    {
        ParseResult result = Parse($"start {block} write local end");

        Assert.Equal("SEM_UNDECLARED_VARIABLE", Assert.Single(result.SemanticErrors).Code);
        Assert.Single(result.ScopedSymbols);
    }

    [Fact]
    public void ANewAssignmentCanReuseAClosedScopeName()
    {
        ParseResult result = Parse("start if true then x = 1 endif x = \"global\" write x end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
        Assert.Equal(SemanticTypes.String, result.ScopedSymbols.Single(symbol => symbol.Scope == "global").DataType);
    }

    [Theory]
    [InlineData("1", "\"text\"", SemanticTypes.Integer)]
    [InlineData("1", "1.5", SemanticTypes.Integer)]
    [InlineData("1", "1E+2", SemanticTypes.Integer)]
    [InlineData("1.5", "1E+2", SemanticTypes.Decimal)]
    [InlineData("true", "1", SemanticTypes.Boolean)]
    [InlineData("\"text\"", "false", SemanticTypes.String)]
    public void IncompatibleAssignmentPreservesTheOriginalSymbol(string initial, string next, string type)
    {
        ParseResult result = Parse($"start x = {initial} x = {next} write x end");

        Assert.Empty(result.Errors);
        SemanticError error = Assert.Single(result.SemanticErrors);
        Assert.Equal("SEM_ASSIGNMENT_TYPE", error.Code);
        Assert.Equal("x", error.Token.Lexeme);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(type, x.DataType);
        Assert.Equal(initial, x.Value);
    }

    [Theory]
    [InlineData("1.5", "2", SemanticTypes.Decimal)]
    [InlineData("1E+2", "2", SemanticTypes.Exponential)]
    [InlineData("1E+2", "2.5", SemanticTypes.Exponential)]
    [InlineData("1", "2", SemanticTypes.Integer)]
    [InlineData("true", "false", SemanticTypes.Boolean)]
    [InlineData("\"text\"", "\"new\"", SemanticTypes.String)]
    public void CompatibleAssignmentKeepsTheDestinationType(string initial, string next, string type)
    {
        ParseResult result = Parse($"start x = {initial} x = {next} end");

        Assert.True(result.Success);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(type, x.DataType);
        Assert.Equal(next, x.Value);
    }

    [Fact]
    public void VisibleOuterVariableKeepsItsTypeInsideBlocks()
    {
        ParseResult result = Parse("start x = 1 if true then x = \"text\" endif x = 2 end");

        Assert.Equal("SEM_ASSIGNMENT_TYPE", Assert.Single(result.SemanticErrors).Code);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal("global", x.Scope);
        Assert.Equal("2", x.Value);
    }

    [Theory]
    [InlineData("1", SemanticTypes.Integer)]
    [InlineData("1.5", SemanticTypes.Decimal)]
    [InlineData("1E+2", SemanticTypes.Exponential)]
    [InlineData("true", SemanticTypes.Boolean)]
    [InlineData("\"text\"", SemanticTypes.String)]
    public void ReadClearsTheValueButRetainsTheType(string initial, string type)
    {
        ParseResult result = Parse($"start x = {initial} if true then read x endif write x end");

        Assert.True(result.Success);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(type, x.DataType);
        Assert.Null(x.Value);
    }

    [Theory]
    [InlineData("", SemanticTypes.Integer)]
    [InlineData("x = 1", SemanticTypes.Integer)]
    [InlineData("x = 1.5", SemanticTypes.Decimal)]
    [InlineData("x = 1E+2", SemanticTypes.Exponential)]
    public void RandProducesAnIntegerAndAcceptsWiderDestinations(string initialization, string type)
    {
        ParseResult result = Parse($"start {initialization} rand x, 10 write x end");

        Assert.True(result.Success);
        SymbolAssignment x = Assert.Single(result.ScopedSymbols);
        Assert.Equal(type, x.DataType);
        Assert.Null(x.Value);
    }

    [Theory]
    [InlineData("\"text\"")]
    [InlineData("true")]
    public void RandRejectsNonNumericDestinations(string initial)
    {
        ParseResult result = Parse($"start x = {initial} rand x, 10 end");

        Assert.Equal("SEM_ASSIGNMENT_TYPE", Assert.Single(result.SemanticErrors).Code);
        Assert.Equal(initial, Assert.Single(result.ScopedSymbols).Value);
    }

    [Fact]
    public void InvalidRandArgumentDoesNotCreateADestination()
    {
        ParseResult result = Parse("start rand x, true y = 1 end");

        Assert.Equal("SEM_ARGUMENT_TYPE", Assert.Single(result.SemanticErrors).Code);
        Assert.Equal("y", Assert.Single(result.ScopedSymbols).Name);
    }

    [Theory]
    [InlineData("1", "2", SemanticTypes.Integer)]
    [InlineData("1", "2.5", SemanticTypes.Decimal)]
    [InlineData("1.5", "2", SemanticTypes.Decimal)]
    [InlineData("1E+2", "2.5", SemanticTypes.Exponential)]
    [InlineData("1.5", "2E+2", SemanticTypes.Exponential)]
    public void ForInfersItsLocalControlFromBothBounds(string from, string to, string type)
    {
        ParseResult result = Parse($"start for i from {from} to {to} do write i i = 1 endfor end");

        Assert.True(result.Success);
        SymbolAssignment control = Assert.Single(result.ScopedSymbols);
        Assert.Equal(type, control.DataType);
        Assert.Contains("for@", control.Scope);
    }

    [Fact]
    public void ForBoundsSeeOuterNamesBeforeIntroducingTheControl()
    {
        ParseResult result = Parse("start i = 1.5 for i from i to 3 do write i endfor write i end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
        Assert.All(result.ScopedSymbols, symbol => Assert.Equal(SemanticTypes.Decimal, symbol.DataType));
        Assert.Equal("1.5", result.ScopedSymbols.Single(symbol => symbol.Scope == "global").Value);
    }

    [Theory]
    [InlineData("true", "SEM_ARGUMENT_TYPE")]
    [InlineData("missing", "SEM_UNDECLARED_VARIABLE")]
    [InlineData("i", "SEM_UNDECLARED_VARIABLE")]
    public void InvalidForHeaderDoesNotCascadeOrUseAnOuterControl(string from, string code)
    {
        ParseResult result = Parse($"start for i from {from} to 2 do write i i = 1 endfor y = 3 end");

        Assert.Equal(code, Assert.Single(result.SemanticErrors).Code);
        Assert.Equal(SemanticTypes.Invalid, result.ScopedSymbols.Single(symbol => symbol.Name == "i").DataType);
        Assert.Equal("3", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Value);
    }

    [Fact]
    public void FailedFirstAssignmentDoesNotFixTheNameOrType()
    {
        ParseResult result = Parse("start x = missing x = \"valid\" write x end");

        Assert.Equal("SEM_UNDECLARED_VARIABLE", Assert.Single(result.SemanticErrors).Code);
        Assert.Equal(SemanticTypes.String, Assert.Single(result.ScopedSymbols).DataType);
    }

    [Fact]
    public void MissingSourceDoesNotOverwriteAnExistingDestination()
    {
        ParseResult result = Parse("start x = 1 x = missing y = 2 end");

        Assert.Equal("SEM_UNDECLARED_VARIABLE", Assert.Single(result.SemanticErrors).Code);
        Assert.Equal("1", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Value);
        Assert.Equal("2", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Value);
    }

    [Fact]
    public void InvalidForControlShadowsButDoesNotAlterAnOuterVariable()
    {
        ParseResult result = Parse("start i = \"outer\" for i from true to 2 do read i i = 1 endfor write i end");

        Assert.Equal("SEM_ARGUMENT_TYPE", Assert.Single(result.SemanticErrors).Code);
        SymbolAssignment outer = result.ScopedSymbols.Single(symbol => symbol.Scope == "global");
        Assert.Equal(SemanticTypes.String, outer.DataType);
        Assert.Equal("\"outer\"", outer.Value);
    }

    [Fact]
    public void ForControlHasAFixedTypeEvenBeforeItsFirstBodyAssignment()
    {
        ParseResult result = Parse("start for i from 1 to 2 do i = 1.5 write i endfor end");

        Assert.Equal("SEM_ASSIGNMENT_TYPE", Assert.Single(result.SemanticErrors).Code);
        SymbolAssignment control = Assert.Single(result.ScopedSymbols);
        Assert.Equal(SemanticTypes.Integer, control.DataType);
        Assert.Null(control.Value);
    }

    [Fact]
    public void ForControlValueIsNotAnExecutedLoopResult()
    {
        ParseResult result = Parse("start for i from 1 to 5 do write i endfor end");

        Assert.True(result.Success);
        Assert.Null(Assert.Single(result.ScopedSymbols).Value);
    }

    [Fact]
    public void RandKeepsIntegerResultTypeWithADecimalArgument()
    {
        ParseResult result = Parse("start rand x, 10.5 end");

        Assert.True(result.Success);
        Assert.Equal(SemanticTypes.Integer, Assert.Single(result.ScopedSymbols).DataType);
    }

    [Fact]
    public void IncompleteExpressionDiscardsItsMissingNameDiagnostic()
    {
        ParseResult result = Parse("start x = missing + write 1 y = 2 end");

        Assert.Single(result.Errors);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal("y", Assert.Single(result.ScopedSymbols).Name);
    }
}
