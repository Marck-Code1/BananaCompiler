using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class DiagnosticRecoveryTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        List<Token> tokens = lexer.Tokenize(code, includeEndOfFile: true);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    private static IEnumerable<SyntaxNode> Nodes(SyntaxNode node)
    {
        yield return node;
        foreach (SyntaxNode child in node.Children)
            foreach (SyntaxNode descendant in Nodes(child))
                yield return descendant;
    }

    private static List<SyntaxNode> Nodes(ParseResult result)
    {
        Assert.NotNull(result.SyntaxTree);
        return Nodes(result.SyntaxTree).ToList();
    }

    [Theory]
    [InlineData("start\nx =\nwrite 1\ny = 2\nend", "SYN_MISSING_EXPRESSION")]
    [InlineData("start x = write 1 y = 2 end", "SYN_MISSING_EXPRESSION")]
    [InlineData("start x = 1 + write 1 y = 2 end", "SYN_MISSING_OPERAND")]
    [InlineData("start x = (1 + ) write 1 y = 2 end", "SYN_MISSING_OPERAND")]
    [InlineData("start , write 1 y = 2 end", "SYN_EXPECTED_TOKEN")]
    public void RecoveryPreservesFollowingInstructionsOnAnyLine(string source, string code)
    {
        ParseResult result = Parse(source);

        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Empty(result.SemanticErrors);
        Assert.Single(Nodes(result), node => node.Kind == "WRITE");
        Assert.Single(Nodes(result), node => node.Kind == "ASSIGN");
        Assert.Single(Nodes(result), node => node.Kind == "ERROR");
        SymbolAssignment y = Assert.Single(result.ScopedSymbols);
        Assert.Equal("y", y.Name);
        Assert.Equal("2", y.Value);
    }

    [Theory]
    [InlineData("start\nx = 1 +\ny = 2\nend", "SYN_MISSING_OPERAND", 2, 7)]
    [InlineData("start\nx =\ny = 2\nend", "SYN_MISSING_EXPRESSION", 2, 3)]
    public void NextAssignmentIsNotConsumedAsAMissingOperand(string source, string code, int line, int column)
    {
        ParseResult result = Parse(source);
        ParseError error = Assert.Single(result.Errors);

        Assert.Equal(code, error.Code);
        Assert.Equal(line, error.Token.Line);
        Assert.Equal(column, error.Token.Column);
        Assert.Equal("y", Assert.Single(result.ScopedSymbols).Name);
        Assert.Single(Nodes(result), node => node.Kind == "ASSIGN");
        Assert.Empty(result.SemanticPileLog);
    }

    [Theory]
    [InlineData("start\nwhile true do\nif true then\nx = 1\nendwhile\ny = 2\nend", "endif")]
    [InlineData("start\nif true then\nwhile true do\nx = 1\nendif\ny = 2\nend", "endwhile")]
    [InlineData("start\nif true then\nfor i from 1 to 2 do\nx = 1\nendif\ny = 2\nend", "endfor")]
    [InlineData("start\nif true then\nwhile true do\nx = 1\nelse\nz = 3\nendif\ny = 2\nend", "endwhile")]
    public void MissingInnerCloserLeavesOuterCloserIntact(string source, string expected)
    {
        ParseResult result = Parse(source);
        ParseError error = Assert.Single(result.Errors);

        Assert.Equal("SYN_MISSING_CLOSER", error.Code);
        Assert.Contains($"'{expected}'", error.Format());
        Assert.NotNull(error.RelatedToken);
        Assert.Equal(3, error.RelatedToken.Line);
        Assert.DoesNotContain("PR", error.Format());
        Assert.Empty(result.SemanticErrors);
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
        Assert.Single(Nodes(result), node => node.Kind == "MISSING");
    }

    [Fact]
    public void WrongCloserIsReportedOnceAndFollowingAssignmentRemainsGlobal()
    {
        ParseResult result = Parse("start\nif true then\nx = 1\nendwhile\ny = 2\nend");
        ParseError error = Assert.Single(result.Errors);

        Assert.Equal("SYN_MISMATCHED_CLOSER", error.Code);
        Assert.Contains("'endif'", error.Format());
        Assert.Contains("'if' iniciado en 2:1", error.Format());
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
    }

    [Fact]
    public void MissingThenPreservesBothBranchesAndTheirScopes()
    {
        ParseResult result = Parse("start\nif true\nx = 1\nelse\ny = 2\nendif\nz = 3\nend");

        Assert.Equal("SYN_MISSING_TOKEN", Assert.Single(result.Errors).Code);
        Assert.Empty(result.SemanticErrors);
        Assert.Contains("if@", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Scope);
        Assert.Contains("else@", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "z").Scope);
        Assert.Single(Nodes(result), node => node.Kind == "IF");
    }

    [Fact]
    public void DuplicateElseDoesNotDiscardTheMatchingEndif()
    {
        ParseResult result = Parse("start if true then x = 1 else y = 2 else z = 3 endif next = 4 end");

        Assert.Equal("SYN_DUPLICATE_ELSE", Assert.Single(result.Errors).Code);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "next").Scope);
        Assert.Equal(result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope,
            result.ScopedSymbols.Single(symbol => symbol.Name == "z").Scope);
    }

    [Fact]
    public void MissingDoWithNestedLoopDoesNotProduceFalseBreakError()
    {
        ParseResult result = Parse("start\nwhile true\nwhile true do\nx = 1\nendwhile\nbreak\nendwhile\ny = 2\nend");

        Assert.Equal("SYN_MISSING_TOKEN", Assert.Single(result.Errors).Code);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal(2, Nodes(result).Count(node => node.Kind == "WHILE"));
        Assert.Single(Nodes(result), node => node.Kind == "BREAK");
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
    }

    [Theory]
    [InlineData("start while true 99 while true do x = 1 endwhile break endwhile y = 2 end")]
    [InlineData("start if true 99 if true then x = 1 endif else z = 3 endif y = 2 end")]
    [InlineData("start for i 99 to 10 do for j from 1 to 2 do x = 1 endfor endfor y = 2 end")]
    public void SkippingMalformedHeadersBalancesNestedBlocks(string source)
    {
        ParseResult result = Parse(source);

        Assert.Single(result.Errors);
        Assert.Empty(result.SemanticErrors);
        SymbolAssignment y = Assert.Single(result.ScopedSymbols);
        Assert.Equal("y", y.Name);
        Assert.Equal("global", y.Scope);
        Assert.Single(Nodes(result), node => node.Kind == "ASSIGN");
    }

    [Fact]
    public void MalformedChildHeaderDoesNotConsumeOuterElse()
    {
        ParseResult result = Parse("start if true then while true 99 else y = 2 endif z = 3 end");

        Assert.Single(result.Errors);
        Assert.Contains("else@", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "z").Scope);
    }

    [Theory]
    [InlineData("rand x 10", 1, 0)]
    [InlineData("rand x, true", 0, 1)]
    [InlineData("rand x, (10", 1, 0)]
    [InlineData("rand x, 10 20", 1, 0)]
    [InlineData("read x = 10", 1, 0)]
    [InlineData("x = 10 20", 1, 0)]
    public void RejectedInstructionsDoNotOverwritePreviousValue(string instruction, int syntax, int semantic)
    {
        ParseResult result = Parse($"start x = 5 {instruction} y = 2 end");

        Assert.Equal(syntax, result.Errors.Count);
        Assert.Equal(semantic, result.SemanticErrors.Count);
        Assert.Equal("5", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Value);
        Assert.Equal("2", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Value);
    }

    [Fact]
    public void MalformedForDoesNotDeclareControlVariable()
    {
        ParseResult result = Parse("start for i 1 to 2 do write 1 endfor y = 3 end");

        Assert.Single(result.Errors);
        Assert.Equal("y", Assert.Single(result.ScopedSymbols).Name);
    }

    [Fact]
    public void RepairedParenthesisDoesNotGenerateCascadingTypeErrorsOrOverwriteValue()
    {
        ParseResult result = Parse("start x = 5 x = (1 + 2 write 1 y = 2 end");

        Assert.Equal("SYN_MISSING_TOKEN", Assert.Single(result.Errors).Code);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal("5", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Value);
        Assert.Single(Nodes(result), node => node.Kind == "WRITE");
    }

    [Theory]
    [InlineData("if\n  123\nthen endif", 3, 3)]
    [InlineData("while 123 do endwhile", 2, 7)]
    public void ConditionDiagnosticPointsAtTheExpression(string instruction, int line, int column)
    {
        ParseResult result = Parse($"start\n{instruction}\nend");
        SemanticError error = Assert.Single(result.SemanticErrors);

        Assert.Empty(result.Errors);
        Assert.Equal("SEM_CONDITION_TYPE", error.Code);
        Assert.Equal("123", error.Token.Lexeme);
        Assert.Equal(line, error.Token.Line);
        Assert.Equal(column, error.Token.Column);
        Assert.Contains("Entero", error.Format());
        Assert.DoesNotContain("Se encontró 'if'", error.Format());
    }

    [Theory]
    [InlineData("\"text\" + true", "+", "string", "bool")]
    [InlineData("1 && 2", "&&", "Entero", "Entero")]
    [InlineData("\"text\" < true", "<", "string", "bool")]
    public void OperatorDiagnosticIncludesOperatorAndBothTypes(string expression, string op, string left, string right)
    {
        ParseResult result = Parse($"start x = {expression} end");
        SemanticError error = Assert.Single(result.SemanticErrors);

        Assert.Equal(op, error.Token.Lexeme);
        Assert.Contains($"'{op}'", error.Format());
        Assert.Contains($"{left} y {right}", error.Format());
        Assert.DoesNotContain("Se encontró", error.Format());
    }

    [Fact]
    public void ArgumentDiagnosticsDistinguishTheirRoles()
    {
        ParseResult result = Parse("start for i from true to \"text\" do endfor move true, \"text\" end");

        Assert.Empty(result.Errors);
        Assert.Equal(4, result.SemanticErrors.Count);
        Assert.Contains("límite inicial", result.SemanticErrors[0].Message);
        Assert.Contains("límite final", result.SemanticErrors[1].Message);
        Assert.Contains("primer argumento", result.SemanticErrors[2].Message);
        Assert.Contains("segundo argumento", result.SemanticErrors[3].Message);
    }

    [Fact]
    public void UnifiedDiagnosticsAreOrderedBySourcePosition()
    {
        ParseResult result = Parse("start\nx = true + 1\ny =\nwrite 2\nend");

        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Equal(DiagnosticKind.Semantic, result.Diagnostics[0].Kind);
        Assert.Equal(2, result.Diagnostics[0].Token.Line);
        Assert.Equal(DiagnosticKind.Syntax, result.Diagnostics[1].Kind);
        Assert.Equal(3, result.Diagnostics[1].Token.Line);
        Assert.All(result.Diagnostics, diagnostic => Assert.False(string.IsNullOrWhiteSpace(diagnostic.Code)));
    }

    [Fact]
    public void IncompleteExpressionDiscardsItsPartialSemanticsButNotOtherInstructions()
    {
        ParseResult result = Parse("start x = (true + 1) * write 2 y = true + 1 z = 3 end");

        Assert.Single(result.Errors);
        SemanticError error = Assert.Single(result.SemanticErrors);
        Assert.True(error.Token.Column > result.Errors[0].Token.Column);
        Assert.Equal("z", Assert.Single(result.ScopedSymbols).Name);
        Assert.Empty(result.SemanticPileLog);
    }

    [Fact]
    public void IndependentSemanticErrorsAreNotSuppressedByInvalidParentResult()
    {
        ParseResult result = Parse("start x = (true + 1) + (false * 2) y = 3 end");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.SemanticErrors.Count);
        Assert.Equal("+", result.SemanticErrors[0].Token.Lexeme);
        Assert.Equal("*", result.SemanticErrors[1].Token.Lexeme);
    }

    [Fact]
    public void ParenthesizedReductionsAreLoggedOnce()
    {
        ParseResult result = Parse("start x = (1 + 2) * 3 end");

        Assert.True(result.Success);
        Assert.Equal(2, result.SemanticPileLog.Count);
        Assert.Single(result.SemanticPileLog, step => step.Contains("1 + 2 =>"));
    }

    [Fact]
    public void MissingProgramEndKeepsPartialTreeAndInstructions()
    {
        ParseResult result = Parse("start x = 1 y = 2");

        Assert.Equal("SYN_MISSING_CLOSER", Assert.Single(result.Errors).Code);
        Assert.Equal(2, Nodes(result).Count(node => node.Kind == "ASSIGN"));
        Assert.Single(Nodes(result), node => node.Kind == "MISSING");
    }

    [Theory]
    [InlineData("start\nx = 1\n\n", 4, 1)]
    [InlineData("start\r\nx = 1\r\n\r\n", 4, 1)]
    [InlineData("start\rx = 1\r\r", 4, 1)]
    [InlineData("start\nx = 1 # comment", 2, 16)]
    public void EndOfFileDiagnosticUsesRealSourceEnd(string source, int line, int column)
    {
        ParseResult result = Parse(source);
        Token eof = Assert.Single(result.Errors).Token;

        Assert.Equal(TokenKinds.EndOfFile, eof.Kind);
        Assert.Equal(line, eof.Line);
        Assert.Equal(column, eof.Column);
    }

    [Fact]
    public void RejectedDoWhileRollsBackNestedAssignmentsAndDeclarations()
    {
        ParseResult result = Parse("start x = 5 do x = 10 local = 1 end true y = 2 end");

        Assert.Single(result.Errors);
        Assert.Equal(2, result.ScopedSymbols.Count);
        Assert.Equal("5", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Value);
        Assert.DoesNotContain(result.ScopedSymbols, symbol => symbol.Name == "local");
        Assert.Equal("2", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Value);
    }

    [Fact]
    public void FailedChildInstructionDoesNotRollbackValidSiblingsInARecoveredBlock()
    {
        ParseResult result = Parse("start x = 5 if true then x = 10 rand x 99 local = 1 endif y = 2 end");

        Assert.Single(result.Errors);
        Assert.Empty(result.SemanticErrors);
        Assert.Equal("10", result.ScopedSymbols.Single(symbol => symbol.Name == "x").Value);
        Assert.Contains("if@", result.ScopedSymbols.Single(symbol => symbol.Name == "local").Scope);
        Assert.Equal("global", result.ScopedSymbols.Single(symbol => symbol.Name == "y").Scope);
    }

    [Fact]
    public void RejectedInstructionDoesNotPublishPartialReductionLogs()
    {
        ParseResult result = Parse("start x = 1 + 2 99 y = 3 + 4 end");

        Assert.Single(result.Errors);
        Assert.DoesNotContain(result.ScopedSymbols, symbol => symbol.Name == "x");
        Assert.Contains("3 + 4", Assert.Single(result.SemanticPileLog));
    }

    [Fact]
    public void MissingStartDoesNotHideLaterErrors()
    {
        ParseResult result = Parse("x = 1 y = true + 1 z = end");

        Assert.Equal(2, result.Errors.Count);
        Assert.Single(result.SemanticErrors);
        Assert.Equal("x", Assert.Single(result.ScopedSymbols).Name);
        Assert.NotNull(result.SyntaxTree);
    }

    [Fact]
    public void ChainedComparisonsAreRejectedWithoutCommittingAPartialAssignment()
    {
        ParseResult result = Parse("start x = 1 < 2 < 3 y = 4 end");

        Assert.Equal("SYN_CHAINED_COMPARISON", Assert.Single(result.Errors).Code);
        Assert.Equal("y", Assert.Single(result.ScopedSymbols).Name);
    }

    [Fact]
    public void LineBreaksRemainWhitespaceInValidExpressions()
    {
        ParseResult result = Parse("start x = 1 +\n2 y = 3 end");

        Assert.True(result.Success);
        Assert.Equal(2, result.ScopedSymbols.Count);
    }

    [Theory]
    [InlineData("InvalidInstructionStart.banana", 1, 0)]
    [InlineData("InvalidLogicalNumber.banana", 0, 1)]
    [InlineData("InvalidRelationalRight.banana", 1, 0)]
    [InlineData("MissingAssignmentOperator.banana", 1, 0)]
    [InlineData("MissingAssignmentValue.banana", 1, 0)]
    [InlineData("MissingDoWhileDo.banana", 1, 0)]
    [InlineData("MissingEndIf.banana", 1, 0)]
    [InlineData("MissingEndWhile.banana", 1, 0)]
    [InlineData("MissingExpressionTerm.banana", 1, 0)]
    [InlineData("MissingProgramEnd.banana", 1, 0)]
    [InlineData("MissingRightParenIf.banana", 1, 0)]
    [InlineData("MissingThenIf.banana", 1, 0)]
    [InlineData("MultipleErrors.banana", 2, 1)]
    public void ExistingSamplesHaveExactlyTheExpectedIndependentErrors(string filename, int syntax, int semantic)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Samples", "InvalidDiagnostics", filename);
        ParseResult result = Parse(File.ReadAllText(path));

        Assert.Equal(syntax, result.Errors.Count);
        Assert.Equal(semantic, result.SemanticErrors.Count);
        Assert.NotNull(result.SyntaxTree);
        Assert.All(result.Errors, error => Assert.DoesNotContain("factor", error.Format()));
    }

    [Fact]
    public void ShortMalformedTokenStreamsAlwaysRecoverWithoutExceptions()
    {
        var random = new Random(42);
        (string Kind, string Lexeme)[] vocabulary =
        [
            (TokenKinds.Identifier, "x"), (TokenKinds.Assignment, "="), (TokenKinds.Number, "1"),
            (TokenKinds.True, "true"), (TokenKinds.Plus, "+"), (TokenKinds.LeftParen, "("),
            (TokenKinds.RightParen, ")"), (TokenKinds.If, "if"), (TokenKinds.Then, "then"),
            (TokenKinds.Else, "else"), (TokenKinds.EndIf, "endif"), (TokenKinds.While, "while"),
            (TokenKinds.Do, "do"), (TokenKinds.EndWhile, "endwhile"), (TokenKinds.For, "for"),
            (TokenKinds.From, "from"), (TokenKinds.To, "to"), (TokenKinds.EndFor, "endfor"),
            (TokenKinds.End, "end"), (TokenKinds.Write, "write"), (TokenKinds.Break, "break")
        ];
        for (int sample = 0; sample < 250; sample++)
        {
            var tokens = new List<Token> { new(TokenKinds.Start, "start", 1, 1) };
            for (int i = 0, count = random.Next(1, 35); i < count; i++)
            {
                var token = vocabulary[random.Next(vocabulary.Length)];
                tokens.Add(new Token(token.Kind, token.Lexeme, 1, i + 2));
            }
            tokens.Add(new Token(TokenKinds.End, "end", 1, tokens.Count + 1));
            ParseResult result = new TopDownParser(tokens).Parse();
            Assert.True(result.Diagnostics.Count <= tokens.Count * 2);
        }
    }
}
