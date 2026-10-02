using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class ScopedSymbolTableTests
{
    [Theory]
    [InlineData("if@1", "if@10")]
    [InlineData("if@1", "if@1")]
    public void SiblingScopesDoNotShareSymbolsEvenWithSimilarLabels(string first, string second)
    {
        var table = new ScopedSymbolTable();
        table.PushScope(first);
        table.Declare("x", SemanticTypes.Integer, "1");
        table.PopScope();
        table.PushScope(second);

        Assert.Null(table.Lookup("x"));
        table.Assign("x", SemanticTypes.String, "\"text\"");
        Assert.Equal(2, table.Entries.Count);
        Assert.Equal(SemanticTypes.String, table.Lookup("x")!.DataType);
    }

    [Fact]
    public void DeclarationShadowsOuterSymbolAndPopRestoresVisibility()
    {
        var table = new ScopedSymbolTable();
        table.Declare("x", SemanticTypes.Integer, "1");
        table.PushScope("while@3");
        table.Declare("x", SemanticTypes.Boolean, "true");
        table.PushScope("if@4");
        Assert.Equal(SemanticTypes.Boolean, table.Lookup("x")!.DataType);
        table.PopScope();
        table.PopScope();

        Assert.Equal(SemanticTypes.Integer, table.Lookup("x")!.DataType);
        Assert.Equal(2, table.Entries.Count);
    }

    [Fact]
    public void AssignmentUpdatesClosestVisibleSymbolWithoutRedeclaring()
    {
        var table = new ScopedSymbolTable();
        table.Declare("x", SemanticTypes.Integer, "1");
        table.PushScope("if@2");
        table.Assign("x", null, "2");

        SymbolAssignment symbol = Assert.Single(table.Entries);
        Assert.Equal("global", symbol.Scope);
        Assert.Equal("2", symbol.Value);
        Assert.Equal(SemanticTypes.Integer, symbol.DataType);
    }

    [Fact]
    public void UnknownAssignmentClearsPreviousValue()
    {
        var table = new ScopedSymbolTable();
        table.Assign("x", SemanticTypes.Integer, "1");
        table.Assign("x", null, null);

        Assert.Null(table.Lookup("x")!.Value);
        Assert.Equal(SemanticTypes.Integer, table.Lookup("x")!.DataType);
    }

    [Fact]
    public void DeclarationRejectsDuplicatesWithinTheSameScope()
    {
        var table = new ScopedSymbolTable();
        table.Declare("x");
        Assert.Throws<InvalidOperationException>(() => table.Declare("x"));
    }

    [Fact]
    public void GlobalScopeCannotBePoppedAndNamesAreCaseSensitive()
    {
        var table = new ScopedSymbolTable();
        table.PopScope();
        table.Declare("x");

        Assert.Equal("global", table.CurrentScope);
        Assert.NotNull(table.Lookup("x"));
        Assert.Null(table.Lookup("X"));
    }

    [Theory]
    [InlineData(SemanticTypes.Integer, "1", 4)]
    [InlineData(SemanticTypes.Decimal, "1.5", 8)]
    [InlineData(SemanticTypes.Exponential, "1e2", 16)]
    [InlineData(SemanticTypes.Boolean, "true", 1)]
    [InlineData(SemanticTypes.String, "\"abc\"", 8)]
    [InlineData(SemanticTypes.String, "\"\"", 2)]
    [InlineData(SemanticTypes.String, null, 2)]
    [InlineData(null, null, 0)]
    public void SizeOfUsesDocumentedSimulation(string? type, string? value, int expected)
    {
        Assert.Equal(expected, ScopedSymbolTable.SizeOf(type, value));
    }

    [Fact]
    public void FinalLayoutUsesUpdatedSizesWithoutOverlaps()
    {
        var table = new ScopedSymbolTable();
        table.Assign("a", SemanticTypes.Integer, "1");
        table.Assign("b", SemanticTypes.Integer, "2");
        table.Declare("unknown");
        table.Assign("flag", SemanticTypes.Boolean, "true");
        table.Assign("a", SemanticTypes.Decimal, "1.5");
        table.Assign("unknown", SemanticTypes.String, "\"a longer string\"");
        table.AssignAddresses();

        Assert.Equal(ScopedSymbolTable.BaseAddress, table.Entries[0].Address);
        for (int i = 1; i < table.Entries.Count; i++)
        {
            SymbolAssignment previous = table.Entries[i - 1];
            SymbolAssignment current = table.Entries[i];
            Assert.Equal(0, current.Address % 4);
            Assert.True(current.Address >= previous.Address + previous.Size);
        }

        SymbolAssignment[] firstLayout = table.Entries.ToArray();
        table.AssignAddresses();
        Assert.Equal(firstLayout, table.Entries.ToArray());
    }

    [Fact]
    public void UnknownTypesReceiveDistinctAlignedPlaceholderAddresses()
    {
        var table = new ScopedSymbolTable();
        table.Declare("a");
        table.Declare("b");
        table.AssignAddresses();

        Assert.Equal(0, table.Entries[0].Size);
        Assert.Equal(ScopedSymbolTable.BaseAddress + 4, table.Entries[1].Address);
    }
}
