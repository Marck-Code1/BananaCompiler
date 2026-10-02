using BananaCompiler.Lexer;
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
    private int position;
    private int loopDepth;
    private ParseError? bestError;
    private Token? assignmentToken;
    private bool assignmentExpressionIsSimple;
    private SyntaxNode? syntaxRoot;

    /// <summary>Raíz del árbol de sintaxis construido durante el parseo (S).</summary>
    public SyntaxNode? SyntaxRoot => syntaxRoot;

    /// <summary>Bitácora acumulada de la pila semántica (una línea por reducción).</summary>
    public IReadOnlyList<string> SemanticPileLog => semanticPileLog;

    /// <summary>Símbolos con ámbito y dirección simulada, en orden de memoria.</summary>
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

    // S -> INICIO
    // INICIO -> PR16 INS PR5
    private void ParseStartSymbol()
    {
        InContext("program", () =>
        {
            Token startToken = Current;
            Expect(TokenKinds.Start);
            SyntaxNode insNode = ParseInstructions([TokenKinds.End, TokenKinds.EndOfFile]);
            Token endToken = Current;
            Expect(TokenKinds.End);

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
                if (!InstructionStarts.Contains(CurrentKind))
                {
                    bestError = null;
                    RecordError("instruction", "instruction list");
                    AddBestError();
                    RecoverAfterError(followers);
                    continue;
                }

                bestError = null;
                string instructionStart = CurrentKind;

                try
                {
                    listNode.AddChild(ParseInstruction());
                }
                catch (ParseFailure)
                {
                    AddBestError();
                    // Nodo de error para que el árbol refleje la recuperación.
                    var errorNode = new SyntaxNode("ERROR", null) { Line = Current.Line, Column = Current.Column };
                    errorNode.AddLeaf(CurrentKind, Current);
                    listNode.AddChild(errorNode);
                    RecoverAfterInstructionError(followers, instructionStart);
                }
            }

            if (listNode.Children.Count == 0)
                listNode.AddChild(new SyntaxNode("ε", null) { Line = Current.Line, Column = Current.Column });
            return listNode;
        });
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
                            AddSemanticError(Current, "'break' solo puede utilizarse dentro de un ciclo.");

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
                (SemanticValue value, SyntaxNode expNode) = ParseExpressionWithTree();
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
            // read actualiza la variable visible o la declara, y borra su valor conocido.
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
        return InContext("if instruction", () =>
        {
            Token ifToken = Current;
            Expect(TokenKinds.If);
            (SemanticValue condValue, SyntaxNode condNode) = ParseExpressionWithTree();
            RequireBoolean(condValue, condNode.Token ?? ifToken, "if");
            Token thenToken = Current;
            Expect(TokenKinds.Then);
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
            SyntaxNode tailNode = ParseIfTail();

            var node = new SyntaxNode("IF", null) { Line = ifToken.Line, Column = ifToken.Column };
            node.AddLeaf(TokenKinds.If, ifToken);
            node.AddChild(condNode);
            node.AddLeaf(TokenKinds.Then, thenToken);
            node.AddChild(thenBranch);
            node.AddChild(tailNode);
            return node;
        });
    }

    // IF_T -> PR7 | PR4 INS PR7
    private SyntaxNode ParseIfTail()
    {
        return InContext("if tail", () =>
        {
            var node = new SyntaxNode("IF_T", null) { Line = Current.Line, Column = Current.Column };
            if (CurrentKind == TokenKinds.EndIf)
            {
                Token endIfToken = Current;
                Match(TokenKinds.EndIf);
                node.AddLeaf(TokenKinds.EndIf, endIfToken);
                return node;
            }

            if (CurrentKind != TokenKinds.Else)
                FailExpectedAny([TokenKinds.EndIf, TokenKinds.Else]);

            Token elseToken = Current;
            Match(TokenKinds.Else);
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
            Token tailEndToken = Current;
            Expect(TokenKinds.EndIf);
            node.AddLeaf(TokenKinds.EndIf, tailEndToken);
            return node;
        });
    }

    // WHILE -> PR20 EXP PR3 INS PR8
    private SyntaxNode ParseWhile()
    {
        return InContext("while instruction", () =>
        {
            Token whileToken = Current;
            Expect(TokenKinds.While);
            (SemanticValue condValue, SyntaxNode condNode) = ParseExpressionWithTree();
            RequireBoolean(condValue, condNode.Token ?? whileToken, "while");
            Token doToken = Current;
            Expect(TokenKinds.Do);
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

            Token endToken = Current;
            Expect(TokenKinds.EndWhile);

            var node = new SyntaxNode("WHILE", null) { Line = whileToken.Line, Column = whileToken.Column };
            node.AddLeaf(TokenKinds.While, whileToken);
            node.AddChild(condNode);
            node.AddLeaf(TokenKinds.Do, doToken);
            node.AddChild(body);
            node.AddLeaf(TokenKinds.EndWhile, endToken);
            return node;
        });
    }

    // FOR -> PR10 ID PR11 EXP PR18 EXP PR3 INS PR6
    private SyntaxNode ParseFor()
    {
        return InContext("for instruction", () =>
        {
            Token forToken = Current;
            Expect(TokenKinds.For);
            EnterScope($"for@{forToken.Line}:{forToken.Column}");
            try
            {
                Token idToken = Current;
                Expect(TokenKinds.Identifier);
                // La variable de control pertenece al ámbito del for.
                symbolTable.Declare(idToken.Lexeme);
                Token fromToken = Current;
                Expect(TokenKinds.From);
                Token fromExprToken = Current;
                (SemanticValue fromValue, SyntaxNode fromNode) = ParseExpressionWithTree();
                RequireNumeric(fromValue, fromExprToken, "for");
                Token toToken = Current;
                Expect(TokenKinds.To);
                Token toExprToken = Current;
                (SemanticValue toValue, SyntaxNode toNode) = ParseExpressionWithTree();
                RequireNumeric(toValue, toExprToken, "for");
                Token doToken = Current;
                Expect(TokenKinds.Do);
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

                Token endToken = Current;
                Expect(TokenKinds.EndFor);

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
        });
    }

    // DO -> PR3 INS PR5 PR20 EXP
    private SyntaxNode ParseDoWhile()
    {
        return InContext("do while instruction", () =>
        {
            Token doToken = Current;
            Expect(TokenKinds.Do);
            EnterScope($"do@{doToken.Line}:{doToken.Column}");
            loopDepth++;
            try
            {
                SyntaxNode body = ParseInstructions([TokenKinds.End, TokenKinds.EndOfFile]);

                Token endToken = Current;
                Expect(TokenKinds.End);
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
        });
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
            symbolTable.Assign(idToken.Lexeme, null, null);
            Token commaToken = Current;
            Expect(TokenKinds.Comma);
            Token argumentToken = Current;
            (SemanticValue argValue, SyntaxNode argNode) = ParseExpressionWithTree();
            RequireNumeric(argValue, argumentToken, "rand");

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
            RequireNumeric(firstValue, firstArgumentToken, "move");
            Token commaToken = Current;
            Expect(TokenKinds.Comma);
            Token secondArgumentToken = Current;
            (SemanticValue secondValue, SyntaxNode secondNode) = ParseExpressionWithTree();
            RequireNumeric(secondValue, secondArgumentToken, "move");

            var node = new SyntaxNode("MOVE", null) { Line = moveToken.Line, Column = moveToken.Column };
            node.AddLeaf(TokenKinds.Move, moveToken);
            node.AddChild(firstNode);
            node.AddLeaf(TokenKinds.Comma, commaToken);
            node.AddChild(secondNode);
            return node;
        });
    }

    // EXP -> EXP_OR (construye árbol + mantiene la pila semántica intacta)
    private (SemanticValue Value, SyntaxNode Node) ParseExpressionWithTree()
    {
        int stackDepth = semanticStack.Count;
        int pileDepth = semanticPile.Count;
        int logDepth = semanticPileLog.Count;
        int stepsDepth = semanticPile.Steps.Count;
        try
        {
            (SemanticValue result, SyntaxNode orNode) = InContext("expression", ParseOrExpressionWithTree);

            if (semanticStack.Count > stackDepth)
            {
                result = semanticStack.Pop();
                semanticStack.Restore(stackDepth);
            }

            // Vacía la pila didáctica y vuelca las reducciones al log global.
            for (int i = stepsDepth; i < semanticPile.Steps.Count; i++)
                semanticPileLog.Add($"L{orNode.Line}: {semanticPile.Steps[i]}");
            semanticPile.Restore(pileDepth);

            var expNode = new SyntaxNode("EXP", null) { Line = orNode.Line, Column = orNode.Column, Semantic = result };
            expNode.AddChild(orNode);
            return (result, expNode);
        }
        catch
        {
            semanticStack.Restore(stackDepth);
            semanticPile.Restore(pileDepth);
            while (semanticPileLog.Count > logDepth)
                semanticPileLog.RemoveAt(semanticPileLog.Count - 1);
            throw;
        }
    }

    // Compatibilidad: callers que solo necesitan el valor semántico.
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
            ReduceBinaryTraced(operatorToken, "El operador lógico requiere operandos booleanos.");
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
            ReduceBinaryTraced(operatorToken, "El operador lógico requiere operandos booleanos.");
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
            ReduceBinaryTraced(operatorToken, "El operador relacional no admite esos tipos de operandos.");
            tail.AddLeaf(operatorToken.Kind, operatorToken);
            tail.AddChild(rightNode);
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
            ReduceUnaryTraced(TokenKinds.Not, "El operador '!' requiere un operando booleano.", operatorToken);
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
            ReduceBinaryTraced(operatorToken, "El operador aritmético requiere operandos numéricos.");
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
            ReduceBinaryTraced(operatorToken, "El operador aritmético requiere operandos numéricos.");
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
            ReduceUnaryTraced(TokenKinds.Plus, "El signo unario requiere un operando numérico.", operatorToken);
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
            ReduceUnaryTraced(TokenKinds.Minus, "El signo unario requiere un operando numérico.", operatorToken);
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
            ReduceBinaryTraced(operatorToken, "El operador '^' requiere operandos numéricos.");
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
                    Token rightToken = Current;
                    Expect(TokenKinds.RightParen);
                    semanticStack.Push(parenthesized);
                    semanticPile.PushOperand($"({ShortLabel(innerNode)})");
                    node.AddLeaf(TokenKinds.LeftParen, leftToken);
                    node.AddChild(innerNode);
                    node.AddLeaf(TokenKinds.RightParen, rightToken);
                    break;
                default:
                    FailExpected("factor");
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
        if (assignmentToken is null || expression.IsInvalid)
            return;

        string? value = assignmentExpressionIsSimple && expression.IsConstant
            ? expression.Value
            : null;

        symbolTable.Assign(assignmentToken.Lexeme, expression.DataType, value);
    }

    private SemanticValue GetIdentifierValue(Token token)
    {
        SymbolAssignment? assignment = symbolTable.Lookup(token.Lexeme);
        if (assignment is null || string.IsNullOrWhiteSpace(assignment.DataType))
            return SemanticValue.Unknown;

        return new SemanticValue(assignment.DataType, assignment.Value, false);
    }

    private void ReduceUnaryTraced(string operatorKind, string message, Token operatorToken)
    {
        string operandLabel = semanticPile.Count > 0 ? semanticPile.Entries[^1] : "?";
        ReduceUnary(operatorKind, message, operatorToken);
        string resultType = semanticStack.Count > 0 ? semanticStack.Peek().DataType ?? "?" : "?";
        if (resultType == SemanticTypes.Invalid)
            semanticPile.Restore(Math.Max(0, semanticPile.Count - 1));
        else
            semanticPile.ReduceUnary(operatorToken.Lexeme, operandLabel, resultType);
    }

    private void ReduceBinaryTraced(Token operatorToken, string message)
    {
        string left = semanticPile.Count >= 2 ? semanticPile.Entries[^2] : "?";
        string right = semanticPile.Count >= 1 ? semanticPile.Entries[^1] : "?";
        ReduceBinary(operatorToken, message);
        string resultType = semanticStack.Count > 0 ? semanticStack.Peek().DataType ?? "?" : "?";
        if (resultType == SemanticTypes.Invalid)
            semanticPile.Restore(Math.Max(0, semanticPile.Count - 2));
        else
            semanticPile.ReduceBinary(operatorToken.Lexeme, left, right, resultType);
    }

    private void ReduceUnary(string operatorKind, string message, Token operatorToken)
    {
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
                AddSemanticError(operatorToken, message);
                semanticStack.Push(SemanticValue.Invalid);
                return;
            }

            semanticStack.Push(new SemanticValue(SemanticTypes.Boolean));
            return;
        }

        if (!CanBeNumeric(operand))
        {
            AddSemanticError(operatorToken, message);
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        string? value = operand.IsConstant && operand.Value is not null
            ? operatorKind == TokenKinds.Minus ? $"-{operand.Value}" : operand.Value
            : null;

        semanticStack.Push(new SemanticValue(operand.DataType, value, operand.IsConstant && value is not null));
    }

    private void ReduceBinary(Token operatorToken, string message)
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
                AddSemanticError(operatorToken, message);
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
                AddSemanticError(operatorToken, message);
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
            AddSemanticError(operatorToken, message);
            semanticStack.Push(SemanticValue.Invalid);
            return;
        }

        semanticStack.Push(new SemanticValue(SemanticTypes.Boolean));
    }

    private void RequireBoolean(SemanticValue value, Token token, string construct)
    {
        if (!value.IsInvalid && !CanBeBoolean(value))
            AddSemanticError(token, $"La condición de '{construct}' debe ser booleana.");
    }

    private void RequireNumeric(SemanticValue value, Token token, string construct)
    {
        if (!value.IsInvalid && !CanBeNumeric(value))
            AddSemanticError(token, $"El argumento de '{construct}' debe ser numérico.");
    }

    private void AddSemanticError(Token token, string message)
    {
        if (semanticErrors.Any(error =>
                error.Token.Line == token.Line &&
                error.Token.Column == token.Column &&
                error.Message == message))
            return;

        semanticErrors.Add(new SemanticError(token, message));
    }

    private static bool CanBeNumeric(SemanticValue value) => value.IsUnknown || value.IsNumeric;

    private static bool CanBeBoolean(SemanticValue value) =>
        value.IsUnknown || value.DataType == SemanticTypes.Boolean;

    private static bool CanBeCompared(SemanticValue left, SemanticValue right, string operatorKind)
    {
        if (operatorKind is TokenKinds.LessThan or TokenKinds.LessThanOrEqual or
            TokenKinds.GreaterThan or TokenKinds.GreaterThanOrEqual)
            return (left.IsUnknown || left.IsNumeric) &&
                   (right.IsUnknown || right.IsNumeric);

        if (left.IsUnknown || right.IsUnknown)
            return true;

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

    private void FailExpectedAny(IEnumerable<string> expectedKinds, string? contextOverride = null)
    {
        foreach (string expected in expectedKinds)
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

    private void RecoverAfterError(HashSet<string> followers, bool stopAtInstructionStart = true)
    {
        if (CurrentKind == TokenKinds.EndOfFile || followers.Contains(CurrentKind))
            return;

        int errorLine = Current.Line;
        position++;

        while (CurrentKind != TokenKinds.EndOfFile)
        {
            if (followers.Contains(CurrentKind))
                return;

            if (stopAtInstructionStart && Current.Line != errorLine && InstructionStarts.Contains(CurrentKind))
                return;

            if (BlockTerminators.Contains(CurrentKind))
            {
                position++;
                return;
            }

            position++;
        }
    }

    private void RecoverAfterInstructionError(HashSet<string> followers, string instructionStart)
    {
        HashSet<string> instructionTerminators = GetInstructionTerminators(instructionStart);
        HashSet<string> syncTokens = [.. followers, .. instructionTerminators];

        RecoverAfterError(syncTokens, instructionTerminators.Count == 0);

        if (instructionTerminators.Contains(CurrentKind) && !followers.Contains(CurrentKind))
            position++;
    }

    private static HashSet<string> GetInstructionTerminators(string instructionStart) => instructionStart switch
    {
        TokenKinds.If => [TokenKinds.Else, TokenKinds.EndIf],
        TokenKinds.While => [TokenKinds.EndWhile],
        TokenKinds.For => [TokenKinds.EndFor],
        TokenKinds.Do => [TokenKinds.End],
        _ => []
    };

    private static Token CreateSyntheticToken(string kind, int line, int column) => new(kind, "", line, column);
}
