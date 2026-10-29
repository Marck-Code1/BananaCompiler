using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public sealed class TopDownParser
{
    public int isString;

    private readonly List<Token> tokens;
    private readonly Stack<string> contexts = [];
    private readonly List<ParseError> errors = [];
    private readonly List<SemanticError> semanticErrors = [];
    private readonly SemanticStack semanticStack = new();
    private readonly SemanticPile semanticPile = new();
    private readonly List<string> semanticPileLog = [];
    private readonly ScopedSymbolTable symbolTable = new();
    private readonly List<BlockContext> blocks = [];
    private int position;
    private int loopDepth;
    private int expressionDepth;
    private ParseError? bestError;
    private Token? assignmentToken;
    private bool assignmentExpressionIsSimple;
    private SyntaxNode? syntaxRoot;

    private sealed record BlockContext(Token Opening, HashSet<string> Boundaries);

    public SyntaxNode? SyntaxRoot => syntaxRoot;

    public IReadOnlyList<string> SemanticPileLog => semanticPileLog;

    public IReadOnlyList<SymbolAssignment> ScopedSymbols => symbolTable.Entries;

    private static readonly HashSet<string> InstructionStarts =
    [
        TokenKinds.Identifier,
        TokenKinds.Break,
        TokenKinds.Clear,
        TokenKinds.For,
        TokenKinds.If,
        TokenKinds.Move,
        TokenKinds.Rand,
        TokenKinds.Read,
        TokenKinds.While,
        TokenKinds.Write,
        TokenKinds.Do
    ];

    private static readonly HashSet<string> BlockTerminators =
    [
        TokenKinds.End,
        TokenKinds.Else,
        TokenKinds.EndIf,
        TokenKinds.EndWhile,
        TokenKinds.EndFor,
        TokenKinds.EndOfFile
    ];

    private static readonly HashSet<string> AdditiveOperators =
    [
        TokenKinds.Plus,
        TokenKinds.Minus
    ];

    private static readonly HashSet<string> MultiplicativeOperators =
    [
        TokenKinds.Multiply,
        TokenKinds.Divide,
        TokenKinds.Modulo
    ];

    private static readonly HashSet<string> RelationalOperators =
    [
        TokenKinds.LessThan,
        TokenKinds.LessThanOrEqual,
        TokenKinds.GreaterThan,
        TokenKinds.GreaterThanOrEqual,
        TokenKinds.Equal,
        TokenKinds.NotEqual
    ];

    public TopDownParser(IEnumerable<Token> sourceTokens)
    {
        tokens = sourceTokens.ToList();

        if (tokens.Count == 0)
        {
            tokens.Add(CreateSyntheticToken(TokenKinds.EndOfFile, 1, 1));
        }
        else if (tokens[^1].Kind != TokenKinds.EndOfFile)
        {
            Token last = tokens[^1];
            tokens.Add(CreateSyntheticToken(TokenKinds.EndOfFile, last.Line, last.Column + Math.Max(last.Lexeme.Length, 1)));
        }
    }

    public ParseResult Parse()
    {
        try
        {
            ParseStartSymbol();
            Expect(TokenKinds.EndOfFile);
        }
        catch (ParseFailure)
        {
            AddBestError();
        }

        symbolTable.AssignAddresses();
        return errors.Count == 0
            ? ParseResult.Ok(semanticErrors, syntaxRoot, symbolTable.Entries, semanticPileLog)
            : ParseResult.Fail(errors, semanticErrors, syntaxRoot, symbolTable.Entries, semanticPileLog);
    }

    private Token Current => tokens[Math.Min(position, tokens.Count - 1)];

    private string CurrentKind => Current.Kind;

    private string CurrentContext => contexts.Count == 0 ? "program" : contexts.Peek();

    private void InContext(string name, Action parse)
    {
        contexts.Push(name);
        try
        {
            parse();
        }
        finally
        {
            contexts.Pop();
        }
    }

    private T InContext<T>(string name, Func<T> parse)
    {
        contexts.Push(name);
        try
        {
            return parse();
        }
        finally
        {
            contexts.Pop();
        }
    }

    private void EnterScope(string scope) => symbolTable.PushScope(scope);

    private void ExitScope() => symbolTable.PopScope();

    private T InBlock<T>(Token opening, HashSet<string> boundaries, Func<T> parse)
    {
        blocks.Add(new BlockContext(opening, boundaries));
        try
        {
            return parse();
        }
        finally
        {
            blocks.RemoveAt(blocks.Count - 1);
        }
    }

    // S -> INICIO
    // INICIO -> PR16 INS PR5
    private void ParseStartSymbol()
    {
        InContext("program", () =>
        {
            Token startToken = ReadSeparator(TokenKinds.Start, Current);
            (SyntaxNode insNode, Token endToken) = InBlock(startToken, [TokenKinds.End], () =>
            {
                SyntaxNode instructions = ParseInstructions([TokenKinds.End]);
                return (instructions, ReadClosing(TokenKinds.End, startToken));
            });

            var inicio = new SyntaxNode("INICIO", null) { Line = startToken.Line, Column = startToken.Column };
            inicio.AddLeaf(TokenKinds.Start, startToken);
            inicio.AddChild(insNode);
            inicio.AddLeaf(TokenKinds.End, endToken);

            syntaxRoot = new SyntaxNode("S", null) { Line = startToken.Line, Column = startToken.Column };
            syntaxRoot.AddChild(inicio);
        });
    }

    // INS -> IN INS | ε
    private SyntaxNode ParseInstructions(HashSet<string> followers)
    {
        return InContext("instruction list", () =>
        {
            var listNode = new SyntaxNode("INS", null) { Line = Current.Line, Column = Current.Column };
            while (!followers.Contains(CurrentKind) && CurrentKind != TokenKinds.EndOfFile)
            {
                if (CurrentKind == TokenKinds.Else && blocks.Count > 1 &&
                    blocks[^1].Opening.Kind == TokenKinds.If && !IsOuterBoundary(CurrentKind))
                {
                    int duplicate = position;
                    AddDiagnostic(Current, "El bloque 'if' ya contiene una rama 'else'.",
                        "SYN_DUPLICATE_ELSE", blocks[^1].Opening);
                    position++;
                    listNode.AddChild(CreateErrorNode(duplicate, position));
                    continue;
                }
                // Un cierre externo no pertenece a este cuerpo. Se devuelve intacto
                // para que el bloque incompleto inserte su cierre y el padre lo lea.
                if (IsOuterBoundary(CurrentKind) || (blocks.Count > 1 && BlockTerminators.Contains(CurrentKind)))
                    break;

                if (!InstructionStarts.Contains(CurrentKind))
                {
                    bestError = null;
                    int invalidStart = position;
                    if (BlockTerminators.Contains(CurrentKind))
                        bestError = new ParseError(Current, CurrentContext,
                            $"El cierre {Diagnostic.DescribeToken(Current)} no corresponde a ningún bloque abierto.",
                            "SYN_UNEXPECTED_CLOSER");
                    else
                        RecordError("instruction", "instruction list");
                    AddBestError();
                    position++;
                    RecoverAfterError(followers);
                    listNode.AddChild(CreateErrorNode(invalidStart, position));
                    continue;
                }

                bestError = null;
                int instructionStart = position;
                int semanticErrorsBefore = semanticErrors.Count;
                int logDepth = semanticPileLog.Count;
                symbolTable.BeginTransaction();

                try
                {
                    listNode.AddChild(ParseInstruction());
                    symbolTable.CommitTransaction();
                }
                catch (ParseFailure)
                {
                    symbolTable.RollbackTransaction();
                    if (semanticErrors.Count > semanticErrorsBefore)
                        semanticErrors.RemoveRange(semanticErrorsBefore, semanticErrors.Count - semanticErrorsBefore);
                    if (semanticPileLog.Count > logDepth)
                        semanticPileLog.RemoveRange(logDepth, semanticPileLog.Count - logDepth);
                    AddBestError();
                    RecoverAfterInstructionError(followers, instructionStart);
                    listNode.AddChild(CreateErrorNode(instructionStart, position));
                }
                catch
                {
                    symbolTable.RollbackTransaction();
                    throw;
                }
            }

            if (listNode.Children.Count == 0)
                listNode.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
            return listNode;
        });
    }

    private SyntaxNode CreateErrorNode(int start, int end)
    {
        Token token = tokens[start];
        var node = new SyntaxNode("ERROR") { Line = token.Line, Column = token.Column };
        for (int i = start; i < end; i++)
            node.AddLeaf(tokens[i].Kind, tokens[i]);
        return node;
    }

    // IN -> ASSIGN | WRITE | READ | IF | WHILE | FOR | DO | RAND |
    //       MOVE | BREAK | CLEAR
    private SyntaxNode ParseInstruction()
    {
        return InContext("instruction", () =>
        {
            switch (CurrentKind)
            {
                case TokenKinds.Identifier:
                    return ParseAssignment();
                case TokenKinds.Write:
                    return ParseWrite();
                case TokenKinds.Read:
                    return ParseRead();
                case TokenKinds.If:
                    return ParseIf();
                case TokenKinds.While:
                    return ParseWhile();
                case TokenKinds.For:
                    return ParseFor();
                case TokenKinds.Do:
                    return ParseDoWhile();
                case TokenKinds.Rand:
                    return ParseRand();
                case TokenKinds.Move:
                    return ParseMove();
                case TokenKinds.Break:
                    {
                        Token breakToken = Current;
                        if (loopDepth == 0)
                            AddSemanticError(Current, "'break' solo puede utilizarse dentro de un ciclo.", "SEM_BREAK_OUTSIDE_LOOP");

                        Expect(TokenKinds.Break);
                        var node = new SyntaxNode("BREAK", null) { Line = breakToken.Line, Column = breakToken.Column };
                        node.AddLeaf(TokenKinds.Break, breakToken);
                        return node;
                    }
                case TokenKinds.Clear:
                    {
                        Token clearToken = Current;
                        Expect(TokenKinds.Clear);
                        var node = new SyntaxNode("CLEAR", null) { Line = clearToken.Line, Column = clearToken.Column };
                        node.AddLeaf(TokenKinds.Clear, clearToken);
                        return node;
                    }
                default:
                    FailExpected("instruction", "instruction list");
                    throw new ParseFailure(); // inalcanzable
            }
        });
    }

    // ASSIGN -> ID OPA EXP
    private SyntaxNode ParseAssignment()
    {
        return InContext("assignment", () =>
        {
            try
            {
                assignmentToken = Current;
                assignmentExpressionIsSimple = true;
                Token idToken = Current;
                Expect(TokenKinds.Identifier);
                Token assignToken = Current;
                Expect(TokenKinds.Assignment);
                int syntaxErrorsBefore = errors.Count;
                (SemanticValue value, SyntaxNode expNode) = ParseExpressionWithTree();
                CheckInstructionEnd();
                if (errors.Count == syntaxErrorsBefore)
                    CommitAssignment(value);

                var node = new SyntaxNode("ASSIGN", null) { Line = idToken.Line, Column = idToken.Column };
                node.AddLeaf(TokenKinds.Identifier, idToken);
                node.AddLeaf(TokenKinds.Assignment, assignToken);
                node.AddChild(expNode);
                return node;
            }
            finally
            {
                assignmentToken = null;
                assignmentExpressionIsSimple = false;
            }
        });
    }

    // WRITE -> PR21 EXP
    private SyntaxNode ParseWrite()
    {
        return InContext("write instruction", () =>
        {
            Token writeToken = Current;
            Expect(TokenKinds.Write);
            (_, SyntaxNode expNode) = ParseExpressionWithTree();
            CheckInstructionEnd();
            var node = new SyntaxNode("WRITE", null) { Line = writeToken.Line, Column = writeToken.Column };
            node.AddLeaf(TokenKinds.Write, writeToken);
            node.AddChild(expNode);
            return node;
        });
    }

    // READ -> PR15 ID
    private SyntaxNode ParseRead()
    {
        return InContext("read instruction", () =>
        {
            Token readToken = Current;
            Expect(TokenKinds.Read);
            Token idToken = Current;
            Expect(TokenKinds.Identifier);
            CheckInstructionEnd();
            // La entrada no determina un tipo: necesita un destino ya tipado.
            SemanticValue destination = GetIdentifierValue(idToken);
            if (!destination.IsInvalid)
                symbolTable.Assign(idToken.Lexeme, null, null);
            var node = new SyntaxNode("READ", null) { Line = readToken.Line, Column = readToken.Column };
            node.AddLeaf(TokenKinds.Read, readToken);
            node.AddLeaf(TokenKinds.Identifier, idToken);
            return node;
        });
    }

    // IF -> PR12 EXP PR17 INS IF_T
    private SyntaxNode ParseIf()
    {
        return InContext("if instruction", () => InBlock(Current, [TokenKinds.Else, TokenKinds.EndIf], () =>
        {
            Token ifToken = Current;
            Expect(TokenKinds.If);
            Token conditionToken = Current;
            (SemanticValue condValue, SyntaxNode condNode) = ParseExpressionWithTree();
            RequireBoolean(condValue, conditionToken, "if");
            Token thenToken = ReadSeparator(TokenKinds.Then, ifToken);
            string scope = $"if@{ifToken.Line}:{ifToken.Column}";
            EnterScope(scope);
            SyntaxNode thenBranch;
            try
            {
                thenBranch = ParseInstructions([TokenKinds.Else, TokenKinds.EndIf, TokenKinds.End, TokenKinds.EndOfFile]);
            }
            finally
            {
                ExitScope();
            }
            SyntaxNode tailNode = ParseIfTail(ifToken);

            var node = new SyntaxNode("IF", null) { Line = ifToken.Line, Column = ifToken.Column };
            node.AddLeaf(TokenKinds.If, ifToken);
            node.AddChild(condNode);
            node.AddLeaf(TokenKinds.Then, thenToken);
            node.AddChild(thenBranch);
            node.AddChild(tailNode);
            return node;
        }));
    }

    // IF_T -> PR7 | PR4 INS PR7
    private SyntaxNode ParseIfTail(Token ifToken)
    {
        return InContext("if tail", () =>
        {
            var node = new SyntaxNode("IF_T", null) { Line = Current.Line, Column = Current.Column };
            if (CurrentKind != TokenKinds.Else)
            {
                Token endIfToken = ReadClosing(TokenKinds.EndIf, ifToken);
                node.AddLeaf(TokenKinds.EndIf, endIfToken);
                return node;
            }

            Token elseToken = Current;
            Match(TokenKinds.Else);
            blocks[^1].Boundaries.Remove(TokenKinds.Else);
            node.AddLeaf(TokenKinds.Else, elseToken);
            EnterScope($"else@{elseToken.Line}:{elseToken.Column}");
            try
            {
                node.AddChild(ParseInstructions([TokenKinds.EndIf, TokenKinds.End, TokenKinds.EndOfFile]));
            }
            finally
            {
                ExitScope();
            }
            Token tailEndToken = ReadClosing(TokenKinds.EndIf, ifToken);
            node.AddLeaf(TokenKinds.EndIf, tailEndToken);
            return node;
        });
    }

    // WHILE -> PR20 EXP PR3 INS PR8
    private SyntaxNode ParseWhile()
    {
        return InContext("while instruction", () => InBlock(Current, [TokenKinds.EndWhile], () =>
        {
            Token whileToken = Current;
            Expect(TokenKinds.While);
            Token conditionToken = Current;
            (SemanticValue condValue, SyntaxNode condNode) = ParseExpressionWithTree();
            RequireBoolean(condValue, conditionToken, "while");
            Token doToken = ReadSeparator(TokenKinds.Do, whileToken);
            EnterScope($"while@{whileToken.Line}:{whileToken.Column}");
            SyntaxNode body;
            loopDepth++;
            try
            {
                body = ParseInstructions([TokenKinds.EndWhile, TokenKinds.End, TokenKinds.EndOfFile]);
            }
            finally
            {
                loopDepth--;
                ExitScope();
            }

            Token endToken = ReadClosing(TokenKinds.EndWhile, whileToken);

            var node = new SyntaxNode("WHILE", null) { Line = whileToken.Line, Column = whileToken.Column };
            node.AddLeaf(TokenKinds.While, whileToken);
            node.AddChild(condNode);
            node.AddLeaf(TokenKinds.Do, doToken);
            node.AddChild(body);
            node.AddLeaf(TokenKinds.EndWhile, endToken);
            return node;
        }));
    }

    // FOR -> PR10 ID PR11 EXP PR18 EXP PR3 INS PR6
    private SyntaxNode ParseFor()
    {
        return InContext("for instruction", () => InBlock(Current, [TokenKinds.EndFor], () =>
        {
            Token forToken = Current;
            Expect(TokenKinds.For);
            EnterScope($"for@{forToken.Line}:{forToken.Column}");
            try
            {
                Token idToken = Current;
                Expect(TokenKinds.Identifier);
                Token fromToken = Current;
                Expect(TokenKinds.From);
                int syntaxErrorsBefore = errors.Count;
                Token fromExprToken = Current;
                (SemanticValue fromValue, SyntaxNode fromNode) = ParseExpressionWithTree();
                bool validFrom = RequireNumeric(fromValue, fromExprToken, "El límite inicial de 'for'");
                Token toToken = Current;
                Expect(TokenKinds.To);
                Token toExprToken = Current;
                (SemanticValue toValue, SyntaxNode toNode) = ParseExpressionWithTree();
                bool validTo = RequireNumeric(toValue, toExprToken, "El límite final de 'for'");
                Token doToken = ReadSeparator(TokenKinds.Do, forToken);
                // El control es local y usa el tipo numérico común de los límites.
                // Una cabecera inválida conserva un enlace inválido para no usar
                // por accidente un homónimo externo ni generar errores en cascada.
                string? controlType = validFrom && validTo && errors.Count == syntaxErrorsBefore
                    ? GetNumericType(fromValue, toValue)
                    : SemanticTypes.Invalid;
                symbolTable.Declare(idToken.Lexeme, controlType);
                SyntaxNode body;
                loopDepth++;
                try
                {
                    body = ParseInstructions([TokenKinds.EndFor, TokenKinds.End, TokenKinds.EndOfFile]);
                }
                finally
                {
                    loopDepth--;
                }

                Token endToken = ReadClosing(TokenKinds.EndFor, forToken);

                var node = new SyntaxNode("FOR", null) { Line = forToken.Line, Column = forToken.Column };
                node.AddLeaf(TokenKinds.For, forToken);
                node.AddLeaf(TokenKinds.Identifier, idToken);
                node.AddLeaf(TokenKinds.From, fromToken);
                node.AddChild(fromNode);
                node.AddLeaf(TokenKinds.To, toToken);
                node.AddChild(toNode);
                node.AddLeaf(TokenKinds.Do, doToken);
                node.AddChild(body);
                node.AddLeaf(TokenKinds.EndFor, endToken);
                return node;
            }
            finally
            {
                ExitScope();
            }
        }));
    }

    // DO -> PR3 INS PR5 PR20 EXP
    private SyntaxNode ParseDoWhile()
    {
        return InContext("do while instruction", () => InBlock(Current, [TokenKinds.End], () =>
        {
            Token doToken = Current;
            Expect(TokenKinds.Do);
            EnterScope($"do@{doToken.Line}:{doToken.Column}");
            loopDepth++;
            try
            {
                SyntaxNode body = ParseInstructions([TokenKinds.End, TokenKinds.EndOfFile]);

                Token endToken = ReadClosing(TokenKinds.End, doToken);
                Token whileToken = Current;
                Expect(TokenKinds.While);
                Token condToken = Current;
                (SemanticValue condValue, SyntaxNode condNode) = ParseExpressionWithTree();
                RequireBoolean(condValue, condToken, "do while");

                var node = new SyntaxNode("DO", null) { Line = doToken.Line, Column = doToken.Column };
                node.AddLeaf(TokenKinds.Do, doToken);
                node.AddChild(body);
                node.AddLeaf(TokenKinds.End, endToken);
                node.AddLeaf(TokenKinds.While, whileToken);
                node.AddChild(condNode);
                return node;
            }
            finally
            {
                loopDepth--;
                ExitScope();
            }
        }));
    }

    // RAND -> PR14 ID CE7 EXP
    private SyntaxNode ParseRand()
    {
        return InContext("rand instruction", () =>
        {
            Token randToken = Current;
            Expect(TokenKinds.Rand);
            Token idToken = Current;
            Expect(TokenKinds.Identifier);
            Token commaToken = Current;
            Expect(TokenKinds.Comma);
            Token argumentToken = Current;
            int syntaxErrorsBefore = errors.Count;
            (SemanticValue argValue, SyntaxNode argNode) = ParseExpressionWithTree();
            CheckInstructionEnd();
            bool validArgument = RequireNumeric(argValue, argumentToken, "El argumento de 'rand'");
            if (validArgument && errors.Count == syntaxErrorsBefore)
                AssignCompatible(idToken, new SemanticValue(SemanticTypes.Integer), null);

            var node = new SyntaxNode("RAND", null) { Line = randToken.Line, Column = randToken.Column };
            node.AddLeaf(TokenKinds.Rand, randToken);
            node.AddLeaf(TokenKinds.Identifier, idToken);
            node.AddLeaf(TokenKinds.Comma, commaToken);
            node.AddChild(argNode);
            return node;
        });
    }

    // MOVE -> PR13 EXP CE7 EXP
    private SyntaxNode ParseMove()
    {
        return InContext("move instruction", () =>
        {
            Token moveToken = Current;
            Expect(TokenKinds.Move);
            Token firstArgumentToken = Current;
            (SemanticValue firstValue, SyntaxNode firstNode) = ParseExpressionWithTree();
            RequireNumeric(firstValue, firstArgumentToken, "El primer argumento de 'move'");
            Token commaToken = Current;
            Expect(TokenKinds.Comma);
            Token secondArgumentToken = Current;
            (SemanticValue secondValue, SyntaxNode secondNode) = ParseExpressionWithTree();
            CheckInstructionEnd();
            RequireNumeric(secondValue, secondArgumentToken, "El segundo argumento de 'move'");

            var node = new SyntaxNode("MOVE", null) { Line = moveToken.Line, Column = moveToken.Column };
            node.AddLeaf(TokenKinds.Move, moveToken);
            node.AddChild(firstNode);
            node.AddLeaf(TokenKinds.Comma, commaToken);
            node.AddChild(secondNode);
            return node;
        });
    }

    // EXP -> EXP_OR
    private (SemanticValue Value, SyntaxNode Node) ParseExpressionWithTree()
    {
        expressionDepth++;
        int stackDepth = semanticStack.Count;
        int pileDepth = semanticPile.Count;
        int logDepth = semanticPileLog.Count;
        int stepsDepth = semanticPile.Steps.Count;
        int snapshotsDepth = semanticPile.Snapshots.Count;
        int semanticErrorsBefore = semanticErrors.Count;
        int syntaxErrorsBefore = errors.Count;
        try
        {
            (SemanticValue result, SyntaxNode orNode) = InContext("expression", ParseOrExpressionWithTree);

            if (semanticStack.Count > stackDepth)
            {
                result = semanticStack.Pop();
                semanticStack.Restore(stackDepth);
            }

            if (errors.Count != syntaxErrorsBefore)
            {
                result = SemanticValue.Invalid;
                // Una reparación local también deja la expresión incompleta:
                // no publicar comprobaciones parciales de una estructura inválida.
                if (semanticErrors.Count > semanticErrorsBefore)
                    semanticErrors.RemoveRange(semanticErrorsBefore, semanticErrors.Count - semanticErrorsBefore);
            }

            // Las expresiones entre paréntesis contribuyen a la traza de su
            // expresión externa, pero no la publican dos veces.
            if (expressionDepth == 1)
            {
                if (!result.IsInvalid)
                    for (int i = stepsDepth; i < semanticPile.Steps.Count; i++)
                        semanticPileLog.Add($"L{orNode.Line}: {semanticPile.Steps[i]}");
                semanticPile.RestoreTrace(stepsDepth, snapshotsDepth);
            }
            semanticPile.Restore(pileDepth);

            var expNode = new SyntaxNode("EXP", null) { Line = orNode.Line, Column = orNode.Column, Semantic = result };
            expNode.AddChild(orNode);
            return (result, expNode);
        }
        catch (ParseFailure)
        {
            semanticStack.Restore(stackDepth);
            semanticPile.Restore(pileDepth);
            while (semanticPileLog.Count > logDepth)
                semanticPileLog.RemoveAt(semanticPileLog.Count - 1);
            semanticPile.RestoreTrace(stepsDepth, snapshotsDepth);
            if (semanticErrors.Count > semanticErrorsBefore)
                semanticErrors.RemoveRange(semanticErrorsBefore, semanticErrors.Count - semanticErrorsBefore);
            throw;
        }
        finally
        {
            expressionDepth--;
        }
    }

    private SemanticValue ParseExpression() => ParseExpressionWithTree().Value;

    // EXP_OR -> EXP_AND EXP_OR_T
    // EXP_OR_T -> OL2 EXP_AND EXP_OR_T | ε
    private (SemanticValue, SyntaxNode) ParseOrExpressionWithTree()
    {
        (SemanticValue _, SyntaxNode leftNode) = ParseAndExpressionWithTree();
        var node = new SyntaxNode("EXP_OR", null) { Line = leftNode.Line, Column = leftNode.Column };
        node.AddChild(leftNode);

        while (CurrentKind == TokenKinds.Or)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.Or);
            MarkAssignmentExpressionAsComplex();
            (_, SyntaxNode rightNode) = ParseAndExpressionWithTree();
            ReduceBinaryTraced(operatorToken);
            node.AddLeaf(TokenKinds.Or, operatorToken);
            node.AddChild(rightNode);
        }

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // EXP_AND -> EXP_REL EXP_AND_T
    // EXP_AND_T -> OL1 EXP_REL EXP_AND_T | ε
    private (SemanticValue, SyntaxNode) ParseAndExpressionWithTree()
    {
        (SemanticValue _, SyntaxNode leftNode) = ParseRelationalExpressionWithTree();
        var node = new SyntaxNode("EXP_AND", null) { Line = leftNode.Line, Column = leftNode.Column };
        node.AddChild(leftNode);

        while (CurrentKind == TokenKinds.And)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.And);
            MarkAssignmentExpressionAsComplex();
            (_, SyntaxNode rightNode) = ParseRelationalExpressionWithTree();
            ReduceBinaryTraced(operatorToken);
            node.AddLeaf(TokenKinds.And, operatorToken);
            node.AddChild(rightNode);
        }

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // EXP_REL -> EXP_NOT EXP_REL_T
    // EXP_REL_T -> OPR EXP_NOT | ε
    private (SemanticValue, SyntaxNode) ParseRelationalExpressionWithTree()
    {
        (SemanticValue _, SyntaxNode leftNode) = ParseNotExpressionWithTree();
        var node = new SyntaxNode("EXP_REL", null) { Line = leftNode.Line, Column = leftNode.Column };
        node.AddChild(leftNode);

        var tail = new SyntaxNode("EXP_REL_T", null) { Line = Current.Line, Column = Current.Column };
        if (RelationalOperators.Contains(CurrentKind))
        {
            Token operatorToken = Current;
            MarkAssignmentExpressionAsComplex();
            ExpectAny(RelationalOperators, "relational operator");
            (_, SyntaxNode rightNode) = ParseNotExpressionWithTree();
            ReduceBinaryTraced(operatorToken);
            tail.AddLeaf(operatorToken.Kind, operatorToken);
            tail.AddChild(rightNode);
            if (RelationalOperators.Contains(CurrentKind))
                FailDiagnostic(Current, "No se permiten comparaciones encadenadas; combine las comparaciones con '&&' o '||'.",
                    "SYN_CHAINED_COMPARISON");
        }
        else
        {
            tail.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
        }
        node.AddChild(tail);

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // EXP_NOT -> OL3 EXP_NOT | EXP_ARIT
    private (SemanticValue, SyntaxNode) ParseNotExpressionWithTree()
    {
        if (CurrentKind == TokenKinds.Not)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.Not);
            MarkAssignmentExpressionAsComplex();
            (_, SyntaxNode childNode) = ParseNotExpressionWithTree();
            ReduceUnaryTraced(operatorToken);
            var node = new SyntaxNode("EXP_NOT", null)
            {
                Line = operatorToken.Line,
                Column = operatorToken.Column,
                Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown,
            };
            node.AddLeaf(TokenKinds.Not, operatorToken);
            node.AddChild(childNode);
            return (node.Semantic!, node);
        }

        (SemanticValue value, SyntaxNode child) = ParseArithmeticExpressionWithTree();
        var passthrough = new SyntaxNode("EXP_NOT", null) { Line = child.Line, Column = child.Column, Semantic = value };
        passthrough.AddChild(child);
        return (value, passthrough);
    }

    // EXP_ARIT -> TERMINO EXP_ARIT_T
    // EXP_ARIT_T -> OA1 TERMINO EXP_ARIT_T |
    //               OA2 TERMINO EXP_ARIT_T | ε
    private (SemanticValue, SyntaxNode) ParseArithmeticExpressionWithTree()
    {
        (SemanticValue _, SyntaxNode leftNode) = ParseTermWithTree();
        var node = new SyntaxNode("EXP_ARIT", null) { Line = leftNode.Line, Column = leftNode.Column };
        node.AddChild(leftNode);

        var tail = new SyntaxNode("EXP_ARIT_T", null) { Line = Current.Line, Column = Current.Column };
        bool hasTail = false;
        while (AdditiveOperators.Contains(CurrentKind))
        {
            hasTail = true;
            Token operatorToken = Current;
            MarkAssignmentExpressionAsComplex();
            ExpectAny(AdditiveOperators, "additive operator");
            (_, SyntaxNode rightNode) = ParseTermWithTree();
            ReduceBinaryTraced(operatorToken);
            tail.AddLeaf(operatorToken.Kind, operatorToken);
            tail.AddChild(rightNode);
        }
        if (!hasTail)
            tail.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
        node.AddChild(tail);

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // TERMINO -> UNARIO TERMINO_T
    // TERMINO_T -> OA3 UNARIO TERMINO_T |
    //              OA4 UNARIO TERMINO_T |
    //              OA6 UNARIO TERMINO_T | ε
    private (SemanticValue, SyntaxNode) ParseTermWithTree()
    {
        (SemanticValue _, SyntaxNode leftNode) = ParseUnaryWithTree();
        var node = new SyntaxNode("TERMINO", null) { Line = leftNode.Line, Column = leftNode.Column };
        node.AddChild(leftNode);

        var tail = new SyntaxNode("TERMINO_T", null) { Line = Current.Line, Column = Current.Column };
        bool hasTail = false;
        while (MultiplicativeOperators.Contains(CurrentKind))
        {
            hasTail = true;
            Token operatorToken = Current;
            MarkAssignmentExpressionAsComplex();
            ExpectAny(MultiplicativeOperators, "multiplicative operator");
            (_, SyntaxNode rightNode) = ParseUnaryWithTree();
            ReduceBinaryTraced(operatorToken);
            tail.AddLeaf(operatorToken.Kind, operatorToken);
            tail.AddChild(rightNode);
        }
        if (!hasTail)
            tail.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
        node.AddChild(tail);

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // UNARIO -> OA1 UNARIO | OA2 UNARIO | POTENCIA
    private (SemanticValue, SyntaxNode) ParseUnaryWithTree()
    {
        if (CurrentKind == TokenKinds.Plus)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.Plus);
            (_, SyntaxNode childNode) = ParseUnaryWithTree();
            ReduceUnaryTraced(operatorToken);
            var node = new SyntaxNode("UNARIO", null)
            {
                Line = operatorToken.Line,
                Column = operatorToken.Column,
                Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown,
            };
            node.AddLeaf(TokenKinds.Plus, operatorToken);
            node.AddChild(childNode);
            return (node.Semantic!, node);
        }

        if (CurrentKind == TokenKinds.Minus)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.Minus);
            (_, SyntaxNode childNode) = ParseUnaryWithTree();
            ReduceUnaryTraced(operatorToken);
            var node = new SyntaxNode("UNARIO", null)
            {
                Line = operatorToken.Line,
                Column = operatorToken.Column,
                Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown,
            };
            node.AddLeaf(TokenKinds.Minus, operatorToken);
            node.AddChild(childNode);
            return (node.Semantic!, node);
        }

        (SemanticValue value, SyntaxNode child) = ParsePowerWithTree();
        var passthrough = new SyntaxNode("UNARIO", null) { Line = child.Line, Column = child.Column, Semantic = value };
        passthrough.AddChild(child);
        return (value, passthrough);
    }

    // POTENCIA -> FACTOR POTENCIA_T
    // POTENCIA_T -> OA5 UNARIO | ε
    private (SemanticValue, SyntaxNode) ParsePowerWithTree()
    {
        SyntaxNode factorNode = ParseFactorWithTree();
        var node = new SyntaxNode("POTENCIA", null) { Line = factorNode.Line, Column = factorNode.Column };
        node.AddChild(factorNode);

        var tail = new SyntaxNode("POTENCIA_T", null) { Line = Current.Line, Column = Current.Column };
        if (CurrentKind == TokenKinds.Power)
        {
            Token operatorToken = Current;
            Expect(TokenKinds.Power);
            MarkAssignmentExpressionAsComplex();
            (_, SyntaxNode exponentNode) = ParseUnaryWithTree();
            ReduceBinaryTraced(operatorToken);
            tail.AddLeaf(TokenKinds.Power, operatorToken);
            tail.AddChild(exponentNode);
        }
        else
        {
            tail.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
        }
        node.AddChild(tail);

        node.Semantic = semanticStack.Count > 0 ? semanticStack.Peek() : SemanticValue.Unknown;
        return (node.Semantic, node);
    }

    // FACTOR -> ID | CN | CNR | CNE | CAD | BOOL | CE5 EXP CE6
    private SyntaxNode ParseFactorWithTree()
    {
        return InContext("factor", () =>
        {
            Token factorToken = Current;
            // ID seguido de '=' inicia otra asignación, no es el operando
            // de la expresión incompleta anterior, incluso en la misma línea.
            if (CurrentKind == TokenKinds.Identifier && PeekKind(1) == TokenKinds.Assignment)
                FailMissingExpression();
            var node = new SyntaxNode("FACTOR", null) { Line = factorToken.Line, Column = factorToken.Column };

            switch (CurrentKind)
            {
                case TokenKinds.Identifier:
                    Expect(TokenKinds.Identifier);
                    semanticStack.Push(GetIdentifierValue(factorToken));
                    semanticPile.PushOperand(string.IsNullOrWhiteSpace(factorToken.Lexeme) ? "ID" : factorToken.Lexeme);
                    node.AddLeaf(TokenKinds.Identifier, factorToken);
                    break;
                case TokenKinds.Number:
                    Expect(TokenKinds.Number);
                    semanticStack.Push(new SemanticValue(
                        SemanticTypes.Integer,
                        factorToken.Lexeme,
                        true));
                    semanticPile.PushOperand(factorToken.Lexeme);
                    node.AddLeaf(TokenKinds.Number, factorToken);
                    break;
                case TokenKinds.RealNumber:
                    Expect(TokenKinds.RealNumber);
                    semanticStack.Push(new SemanticValue(
                        SemanticTypes.Decimal,
                        factorToken.Lexeme,
                        true));
                    semanticPile.PushOperand(factorToken.Lexeme);
                    node.AddLeaf(TokenKinds.RealNumber, factorToken);
                    break;
                case TokenKinds.ScientificNumber:
                    Expect(TokenKinds.ScientificNumber);
                    semanticStack.Push(new SemanticValue(
                        SemanticTypes.Exponential,
                        factorToken.Lexeme,
                        true));
                    semanticPile.PushOperand(factorToken.Lexeme);
                    node.AddLeaf(TokenKinds.ScientificNumber, factorToken);
                    break;
                case TokenKinds.String:
                    Expect(TokenKinds.String);
                    semanticStack.Push(new SemanticValue(SemanticTypes.String, factorToken.Lexeme, true));
                    semanticPile.PushOperand(string.IsNullOrWhiteSpace(factorToken.Lexeme) ? "CAD" : factorToken.Lexeme);
                    node.AddLeaf(TokenKinds.String, factorToken);
                    break;
                case TokenKinds.True:
                case TokenKinds.False:
                    SyntaxNode boolNode = ParseBooleanWithTree();
                    semanticStack.Push(new SemanticValue(SemanticTypes.Boolean, factorToken.Lexeme, true));
                    semanticPile.PushOperand(factorToken.Lexeme);
                    node.AddChild(boolNode);
                    break;
                case TokenKinds.LeftParen:
                    MarkAssignmentExpressionAsComplex();
                    Token leftToken = Current;
                    Expect(TokenKinds.LeftParen);
                    (SemanticValue parenthesized, SyntaxNode innerNode) = ParseExpressionWithTree();
                    Token rightToken = ReadRightParenthesis(leftToken);
                    semanticStack.Push(rightToken.IsSynthetic ? SemanticValue.Invalid : parenthesized);
                    semanticPile.PushOperand($"({ShortLabel(innerNode)})");
                    node.AddLeaf(TokenKinds.LeftParen, leftToken);
                    node.AddChild(innerNode);
                    node.AddLeaf(TokenKinds.RightParen, rightToken);
                    break;
                default:
                    FailMissingExpression();
                    break;
            }
            return node;
        });
    }

    // BOOL -> PR19 | PR9
    private SyntaxNode ParseBooleanWithTree()
    {
        return InContext("boolean", () =>
        {
            var node = new SyntaxNode("BOOL", null) { Line = Current.Line, Column = Current.Column };
            if (CurrentKind == TokenKinds.True || CurrentKind == TokenKinds.False)
            {
                Token token = Current;
                Match(token.Kind);
                node.AddLeaf(token.Kind, token);
                return node;
            }
            FailExpected("PR19 or PR9");
            throw new ParseFailure(); // inalcanzable
        });
    }

    private static string ShortLabel(SyntaxNode node)
    {
        string compact = node.ToCompact();
        return compact.Length > 32 ? compact[..32] + "…" : compact;
    }

    private void CommitAssignment(SemanticValue expression)
    {
        if (assignmentToken is null || expression.IsInvalid || expression.IsUnknown)
            return;

        string? value = assignmentExpressionIsSimple && expression.IsConstant
            ? expression.Value
            : null;

        AssignCompatible(assignmentToken, expression, value);
    }

    private void AssignCompatible(Token destination, SemanticValue source, string? value)
    {
        SymbolAssignment? existing = symbolTable.Lookup(destination.Lexeme);
        if (existing is null)
        {
            symbolTable.Declare(destination.Lexeme, source.DataType, value);
            return;
        }

        if (existing.DataType == SemanticTypes.Invalid)
            return;

        if (!CanAssign(existing.DataType, source.DataType))
        {
            AddSemanticError(destination,
                $"No se puede asignar {TypeName(source)} a la variable '{destination.Lexeme}' de tipo {existing.DataType ?? "desconocido"}.",
                "SEM_ASSIGNMENT_TYPE");
            return;
        }

        // La ampliación numérica no cambia el tipo fijado por la declaración.
        symbolTable.Assign(destination.Lexeme, null, value);
    }

    private static bool CanAssign(string? destination, string? source) =>
        destination is not null && source is not null &&
        (destination == source ||
         destination == SemanticTypes.Decimal && source == SemanticTypes.Integer ||
         destination == SemanticTypes.Exponential &&
             source is SemanticTypes.Integer or SemanticTypes.Decimal);

    private SemanticValue GetIdentifierValue(Token token)
    {
        SymbolAssignment? assignment = symbolTable.Lookup(token.Lexeme);
        if (assignment is null)
        {
            AddSemanticError(token,
                $"La variable '{token.Lexeme}' no está declarada en un ámbito visible. Inicialízala antes de utilizarla.",
                "SEM_UNDECLARED_VARIABLE");
            return SemanticValue.Invalid;
        }
        if (string.IsNullOrWhiteSpace(assignment.DataType))
        {
            AddSemanticError(token, $"No se conoce el tipo de la variable '{token.Lexeme}'.", "SEM_UNKNOWN_TYPE");
            return SemanticValue.Invalid;
        }

        return new SemanticValue(assignment.DataType, assignment.Value, false);
    }

    private void ReduceUnaryTraced(Token operatorToken)
    {
        string operandLabel = semanticPile.Count > 0 ? semanticPile.Entries[^1] : "?";
        ReduceUnary(operatorToken);
        string resultType = semanticStack.Count > 0 ? semanticStack.Peek().DataType ?? "?" : "?";
        if (resultType == SemanticTypes.Invalid)
            semanticPile.Restore(Math.Max(0, semanticPile.Count - 1));
        else
            semanticPile.ReduceUnary(operatorToken.Lexeme, operandLabel, resultType);
    }

    private void ReduceBinaryTraced(Token operatorToken)
    {
        string left = semanticPile.Count >= 2 ? semanticPile.Entries[^2] : "?";
        string right = semanticPile.Count >= 1 ? semanticPile.Entries[^1] : "?";
        ReduceBinary(operatorToken);
        string resultType = semanticStack.Count > 0 ? semanticStack.Peek().DataType ?? "?" : "?";
        if (resultType == SemanticTypes.Invalid)
            semanticPile.Restore(Math.Max(0, semanticPile.Count - 2));
        else
            semanticPile.ReduceBinary(operatorToken.Lexeme, left, right, resultType);
    }

    private void ReduceUnary(Token operatorToken)
    {
        string operatorKind = operatorToken.Kind;
        SemanticValue operand = semanticStack.Pop();

        if (operand.IsInvalid)
        {
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        if (operatorKind == TokenKinds.Not)
        {
            if (!CanBeBoolean(operand))
            {
                AddSemanticError(operatorToken,
                    $"El operador unario '{operatorToken.Lexeme}' requiere un operando booleano; recibió {TypeName(operand)}.");
                semanticStack.Push(SemanticValue.Invalid);
                return;
            }

            semanticStack.Push(new SemanticValue(SemanticTypes.Boolean));
            return;
        }

        if (!CanBeNumeric(operand))
        {
            AddSemanticError(operatorToken,
                $"El operador unario '{operatorToken.Lexeme}' requiere un operando numérico; recibió {TypeName(operand)}.");
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        string? value = operand.IsConstant && operand.Value is not null
            ? operatorKind == TokenKinds.Minus ? $"-{operand.Value}" : operand.Value
            : null;

        semanticStack.Push(new SemanticValue(operand.DataType, value, operand.IsConstant && value is not null));
    }

    private void ReduceBinary(Token operatorToken)
    {
        SemanticValue right = semanticStack.Pop();
        SemanticValue left = semanticStack.Pop();

        if (left.IsInvalid || right.IsInvalid)
        {
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        string operatorKind = operatorToken.Kind;

        if (operatorKind is TokenKinds.And or TokenKinds.Or)
        {
            if (!CanBeBoolean(left) || !CanBeBoolean(right))
            {
                AddSemanticError(operatorToken,
                    $"El operador '{operatorToken.Lexeme}' requiere operandos booleanos; recibió {TypeName(left)} y {TypeName(right)}.");
                semanticStack.Push(SemanticValue.Invalid);
                return;
            }

            semanticStack.Push(new SemanticValue(SemanticTypes.Boolean));
            return;
        }

        if (operatorKind is TokenKinds.Plus or TokenKinds.Minus or
            TokenKinds.Multiply or TokenKinds.Divide or TokenKinds.Modulo or TokenKinds.Power)
        {
            if (!CanBeNumeric(left) || !CanBeNumeric(right))
            {
                AddSemanticError(operatorToken,
                    $"El operador '{operatorToken.Lexeme}' requiere operandos numéricos; recibió {TypeName(left)} y {TypeName(right)}.");
                semanticStack.Push(SemanticValue.Invalid);
                return;
            }

            semanticStack.Push(new SemanticValue(
                GetNumericType(left, right),
                IsConstantBinary(left, right) ? $"{left.Value}{operatorToken.Lexeme}{right.Value}" : null,
                IsConstantBinary(left, right)));
            return;
        }

        if (!CanBeCompared(left, right, operatorKind))
        {
            AddSemanticError(operatorToken,
                $"El operador '{operatorToken.Lexeme}' no admite operandos {TypeName(left)} y {TypeName(right)}.");
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        semanticStack.Push(new SemanticValue(SemanticTypes.Boolean));
    }

    private void RequireBoolean(SemanticValue value, Token token, string construct)
    {
        if (!value.IsInvalid && !CanBeBoolean(value))
            AddSemanticError(token, $"La condición de '{construct}' debe ser booleana; se recibió {TypeName(value)}.",
                "SEM_CONDITION_TYPE");
    }

    private bool RequireNumeric(SemanticValue value, Token token, string description)
    {
        if (value.IsInvalid)
            return false;
        if (CanBeNumeric(value))
            return true;
        AddSemanticError(token, $"{description} debe ser numérico; se recibió {TypeName(value)}.", "SEM_ARGUMENT_TYPE");
        return false;
    }

    private static string TypeName(SemanticValue value) => value.DataType ?? "tipo desconocido";

    private void AddSemanticError(Token token, string message, string code = "SEM_TYPE_MISMATCH")
    {
        if (semanticErrors.Any(error =>
                error.Token.Line == token.Line &&
                error.Token.Column == token.Column &&
                error.Message == message))
            return;

        semanticErrors.Add(new SemanticError(token, message, code));
    }

    private static bool CanBeNumeric(SemanticValue value) => value.IsNumeric;

    private static bool CanBeBoolean(SemanticValue value) =>
        value.DataType == SemanticTypes.Boolean;

    private static bool CanBeCompared(SemanticValue left, SemanticValue right, string operatorKind)
    {
        if (operatorKind is TokenKinds.LessThan or TokenKinds.LessThanOrEqual or
            TokenKinds.GreaterThan or TokenKinds.GreaterThanOrEqual)
            return left.IsNumeric && right.IsNumeric;

        if (left.IsNumeric && right.IsNumeric)
            return true;

        return left.DataType == right.DataType &&
               left.DataType is SemanticTypes.Boolean or SemanticTypes.String;
    }

    private static string? GetNumericType(SemanticValue left, SemanticValue right)
    {
        if (left.DataType == SemanticTypes.Exponential || right.DataType == SemanticTypes.Exponential)
            return SemanticTypes.Exponential;

        if (left.DataType == SemanticTypes.Decimal || right.DataType == SemanticTypes.Decimal)
            return SemanticTypes.Decimal;

        if (left.DataType == SemanticTypes.Integer || right.DataType == SemanticTypes.Integer)
            return SemanticTypes.Integer;

        return null;
    }

    private static bool IsConstantBinary(SemanticValue left, SemanticValue right) =>
        left.IsConstant && right.IsConstant && left.Value is not null && right.Value is not null;

    private void MarkAssignmentExpressionAsComplex()
    {
        if (assignmentToken is not null)
            assignmentExpressionIsSimple = false;
    }

    private bool Match(string kind)
    {
        if (CurrentKind != kind)
            return false;

        position++;
        return true;
    }

    private void Expect(string kind)
    {
        if (Match(kind))
            return;

        FailExpected(kind);
    }

    private void ExpectAny(HashSet<string> kinds, string label)
    {
        if (kinds.Contains(CurrentKind))
        {
            position++;
            return;
        }

        FailExpected(label);
    }

    private void FailExpected(string expected, string? contextOverride = null)
    {
        RecordError(expected, contextOverride);
        throw new ParseFailure();
    }

    private void RecordError(string expected, string? contextOverride = null)
    {
        string context = contextOverride ?? CurrentContext;

        if (bestError == null || Current.Line > bestError.Token.Line || (Current.Line == bestError.Token.Line && Current.Column > bestError.Token.Column))
            bestError = new ParseError(Current, context);

        if (bestError.Token.Line == Current.Line && bestError.Token.Column == Current.Column)
            bestError.AddExpected(expected, context);
    }

    private void AddBestError()
    {
        ParseError error = bestError?.Clone() ?? new ParseError(Current, CurrentContext);

        if (!errors.Any(existing => existing.Token.Line == error.Token.Line && existing.Token.Column == error.Token.Column && existing.Format() == error.Format()))
            errors.Add(error);

        bestError = null;
    }

    private string PeekKind(int offset) => tokens[Math.Min(position + offset, tokens.Count - 1)].Kind;

    private bool IsInstructionBoundary() => InstructionStarts.Contains(CurrentKind) &&
        (CurrentKind != TokenKinds.Identifier || PeekKind(1) == TokenKinds.Assignment);

    private bool IsOuterBoundary(string kind) => blocks.Take(Math.Max(0, blocks.Count - 1))
        .Any(block => block.Boundaries.Contains(kind));

    private void CheckInstructionEnd()
    {
        if (!InstructionStarts.Contains(CurrentKind) && !BlockTerminators.Contains(CurrentKind))
            FailDiagnostic(Current, $"El token {Diagnostic.DescribeToken(Current)} no puede continuar esta instrucción; se esperaba un operador válido o el inicio de otra instrucción.",
                "SYN_UNEXPECTED_TOKEN");
    }

    private static string Keyword(string kind) => kind switch
    {
        TokenKinds.Start => "start",
        TokenKinds.If => "if",
        TokenKinds.While => "while",
        TokenKinds.For => "for",
        TokenKinds.Then => "then",
        TokenKinds.Do => "do",
        TokenKinds.End => "end",
        TokenKinds.EndIf => "endif",
        TokenKinds.EndWhile => "endwhile",
        TokenKinds.EndFor => "endfor",
        _ => kind
    };

    private Token MissingToken(string kind) => new(kind, "", Current.Line, Current.Column) { IsSynthetic = true };

    private Token ReadSeparator(string kind, Token opening)
    {
        Token token = Current;
        if (Match(kind))
            return token;
        if (!IsInstructionBoundary() && !BlockTerminators.Contains(CurrentKind))
        {
            FailExpected(kind);
            throw new ParseFailure();
        }

        AddDiagnostic(Current, $"Falta '{Keyword(kind)}' antes de {Diagnostic.DescribeToken(Current)}.",
            "SYN_MISSING_TOKEN", kind == TokenKinds.Start ? null : opening);
        return MissingToken(kind);
    }

    private Token ReadClosing(string kind, Token opening)
    {
        Token token = Current;
        if (Match(kind))
            return token;

        string origin = $"'{Keyword(opening.Kind)}' iniciado en {opening.Line}:{opening.Column}";
        bool missing = CurrentKind == TokenKinds.EndOfFile || IsOuterBoundary(CurrentKind);
        AddDiagnostic(Current, missing
                ? $"Falta '{Keyword(kind)}' para el {origin}, antes de {Diagnostic.DescribeToken(Current)}."
                : $"Se esperaba '{Keyword(kind)}' para el {origin}; se encontró {Diagnostic.DescribeToken(Current)}.",
            missing ? "SYN_MISSING_CLOSER" : "SYN_MISMATCHED_CLOSER", opening);
        Token recovered = MissingToken(kind);
        if (!missing && BlockTerminators.Contains(CurrentKind))
            position++;
        return recovered;
    }

    private Token ReadRightParenthesis(Token opening)
    {
        Token token = Current;
        if (Match(TokenKinds.RightParen))
            return token;
        if (BlockTerminators.Contains(CurrentKind) || IsInstructionBoundary() ||
            CurrentKind is TokenKinds.Then or TokenKinds.Do or TokenKinds.To or TokenKinds.Comma)
        {
            AddDiagnostic(Current, $"Falta ')' para el paréntesis abierto en {opening.Line}:{opening.Column}, antes de {Diagnostic.DescribeToken(Current)}.",
                "SYN_MISSING_TOKEN", opening);
            return MissingToken(TokenKinds.RightParen);
        }
        FailExpected(TokenKinds.RightParen);
        throw new ParseFailure();
    }

    private void FailMissingExpression()
    {
        Token previous = tokens[Math.Max(0, position - 1)];
        bool operand = AdditiveOperators.Contains(previous.Kind) || MultiplicativeOperators.Contains(previous.Kind) ||
            RelationalOperators.Contains(previous.Kind) || previous.Kind is TokenKinds.Power or TokenKinds.And or TokenKinds.Or or TokenKinds.Not;
        string description = operand ? "un operando" : "una expresión";
        FailDiagnostic(previous, $"Falta {description} después de {Diagnostic.DescribeToken(previous)}; se encontró {Diagnostic.DescribeToken(Current)}.",
            operand ? "SYN_MISSING_OPERAND" : "SYN_MISSING_EXPRESSION");
    }

    private void FailDiagnostic(Token token, string message, string code)
    {
        bestError = new ParseError(token, CurrentContext, message, code);
        throw new ParseFailure();
    }

    private void AddDiagnostic(Token token, string message, string code, Token? related = null)
    {
        errors.Add(new ParseError(token, CurrentContext, message, code, related));
    }

    private void RecoverAfterError(HashSet<string> followers)
    {
        while (CurrentKind != TokenKinds.EndOfFile)
        {
            if (followers.Contains(CurrentKind) || IsOuterBoundary(CurrentKind) || IsInstructionBoundary())
                return;
            if (BlockTerminators.Contains(CurrentKind))
                return;
            position++;
        }
    }

    private void RecoverAfterInstructionError(HashSet<string> followers, int instructionStart)
    {
        if (ClosingKind(tokens[instructionStart].Kind) is null)
        {
            if (position == instructionStart)
                position++;
            RecoverAfterError(followers);
            return;
        }

        // Se vuelve a recorrer solo la instrucción rechazada para equilibrar
        // sus bloques. El primer cierre de un bloque interno no cierra al externo.
        var pending = new List<(string Closer, bool AwaitingDo)>();
        position = instructionStart;
        while (CurrentKind != TokenKinds.EndOfFile)
        {
            string kind = CurrentKind;
            if (kind == TokenKinds.Do && pending.Count > 0 && pending[^1].AwaitingDo)
            {
                pending[^1] = (pending[^1].Closer, false);
                position++;
                continue;
            }
            string? closer = ClosingKind(kind);
            if (closer is not null)
            {
                pending.Add((closer, kind is TokenKinds.While or TokenKinds.For));
                position++;
                continue;
            }
            if (BlockTerminators.Contains(kind))
            {
                int match = pending.FindLastIndex(block => block.Closer == kind);
                if (match >= 0)
                {
                    pending.RemoveRange(match, pending.Count - match);
                    position++;
                    // 'end while EXP' es el cierre del do, no otro while.
                    if (kind == TokenKinds.End)
                    {
                        if (CurrentKind == TokenKinds.While)
                            position++;
                        while (CurrentKind != TokenKinds.EndOfFile && !BlockTerminators.Contains(CurrentKind) && !IsInstructionBoundary())
                            position++;
                    }
                    if (pending.Count == 0)
                        return;
                    continue;
                }
                if ((followers.Contains(kind) || blocks.Any(block => block.Boundaries.Contains(kind))) &&
                    (kind != TokenKinds.Else || !pending.Any(block => block.Closer == TokenKinds.EndIf)))
                    return;
            }
            position++;
        }
    }

    private static string? ClosingKind(string instructionStart) => instructionStart switch
    {
        TokenKinds.If => TokenKinds.EndIf,
        TokenKinds.While => TokenKinds.EndWhile,
        TokenKinds.For => TokenKinds.EndFor,
        TokenKinds.Do => TokenKinds.End,
        _ => null
    };

    private static Token CreateSyntheticToken(string kind, int line, int column) =>
        new(kind, "", line, column) { IsSynthetic = true };
}
