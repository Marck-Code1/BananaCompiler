using System.Text;
using BananaCompiler.Models;

namespace BananaCompiler.Lexer;

public sealed class LexicalAnalyzer
{
    private const int InitialState = 0;
    private const string EndColumn = "FDC";
    private const char CommentStart = '#';
    private const char StringDelimiter = '"';
    private const string InvalidCharacterMessage = "Carácter no válido en el alfabeto";
    private const string UnclosedStringMessage = "Cadena sin terminar";

    private readonly TransitionTable transitionTable;

    public LexicalAnalyzer(TransitionTable transitionTable)
    {
        this.transitionTable = transitionTable;
    }

    public List<Token> Tokenize(TextReader reader)
    {
        ScanContext context = new();
        int lineNumber = 1;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            ReadLine(context, line, lineNumber);
            lineNumber++;
        }

        return context.Tokens;
    }

    public List<Token> Tokenize(string source) => Tokenize(source, includeEndOfFile: false);

    /// <summary>La opción EOF incluye la posición exacta del final del texto para los diagnósticos.</summary>
    public List<Token> Tokenize(string source, bool includeEndOfFile)
    {
        using StringReader reader = new(source);
        List<Token> tokens = Tokenize(reader);
        if (includeEndOfFile)
        {
            int line = 1;
            int column = 1;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] is '\r' or '\n')
                {
                    if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n')
                        i++;
                    line++;
                    column = 1;
                }
                else
                    column++;
            }
            tokens.Add(new Token(TokenKinds.EndOfFile, "", line, column));
        }
        return tokens;
    }

    // Recorrido por líneas
    private void ReadLine(ScanContext context, string line, int lineNumber)
    {
        context.CurrentLine = lineNumber;
        ResetLine(context);

        for (int index = 0; index < line.Length; index++)
        {
            char symbol = line[index];
            int columnNumber = index + 1;

            if (symbol == CommentStart)
            {
                TryFinalizeCurrentLexeme(context);
                return;
            }

            if (symbol == StringDelimiter)
            {
                index = ReadString(context, line, index);
                continue;
            }

            ProcessSymbol(context, symbol, columnNumber, ref index);
        }

        TryFinalizeCurrentLexeme(context);
    }

    private void ProcessSymbol(ScanContext context, char symbol, int columnNumber, ref int index)
    {
        string column = GetColumn(symbol);
        int? nextState = transitionTable.GetTransition(column, context.CurrentState);

        if (nextState is null)
        {
            HandleMissingTransition(context, columnNumber, ref index);
            return;
        }

        if (ShouldCloseBeforeSymbol(context, symbol, column, nextState.Value))
        {
            TryFinalizeCurrentLexeme(context);
            index--;
            return;
        }

        if (column == EndColumn)
        {
            AddTokenIfLexemeExists(context, nextState.Value);
            return;
        }

        AppendSymbol(context, symbol, nextState.Value, columnNumber);
    }

    // Formas especiales de tokens
    private int ReadString(ScanContext context, string line, int startIndex)
    {
        TryFinalizeCurrentLexeme(context);

        ResetLexeme(context);
        context.LexemeStartColumn = startIndex + 1;
        context.Lexeme.Append(line[startIndex]);

        for (int index = startIndex + 1; index < line.Length; index++)
        {
            context.Lexeme.Append(line[index]);

            if (line[index] == StringDelimiter)
            {
                AddStringToken(context);
                return index;
            }
        }

        AddToken(context, TokenKinds.Error, UnclosedStringMessage);
        ResetLexeme(context);
        return line.Length - 1;
    }

    // Transiciones y límites de tokens
    private bool ShouldCloseBeforeSymbol(ScanContext context, char symbol, string column, int nextState)
    {
        if (context.Lexeme.Length == 0 ||
            transitionTable.IsErrorState(context.CurrentState) ||
            !transitionTable.IsErrorState(nextState) ||
            column == EndColumn)
            return false;

        if (!transitionTable.TryGetFinalTokenState(context.CurrentState, EndColumn, out int finalState))
            return false;

        // Una letra inmediatamente después de una constante numérica forma
        // una sola secuencia inválida (por ejemplo, 1identificador). No se
        // debe recuperar como CN seguido de ID.
        if (LexerTokenFacts.IsNumeric(transitionTable.GetToken(finalState)) &&
            LexerTokenFacts.IsIdentifierStart(symbol))
            return false;

        return transitionTable.CanStartToken(column, InitialState);
    }

    private void HandleMissingTransition(ScanContext context, int columnNumber, ref int index)
    {
        if (TryFinalizeCurrentLexeme(context))
        {
            index--;
            return;
        }

        context.LexemeStartColumn = columnNumber;
        AddToken(context, TokenKinds.Error, InvalidCharacterMessage);
        ResetLexeme(context);
    }

    private static string GetColumn(char symbol) =>
        (symbol == ' ' || symbol == '\t') ? EndColumn : ((int)symbol).ToString();

    // Ciclo de vida del lexema
    private static void ResetLine(ScanContext context)
    {
        ResetLexeme(context);
        context.LexemeStartColumn = 1;
    }

    private static void AppendSymbol(ScanContext context, char symbol, int nextState, int columnNumber)
    {
        if (context.Lexeme.Length == 0)
            context.LexemeStartColumn = columnNumber;

        context.Lexeme.Append(symbol);
        context.CurrentState = nextState;
    }

    private void AddTokenIfLexemeExists(ScanContext context, int state)
    {
        if (context.Lexeme.Length == 0)
            return;

        AddTokenForState(context, state);
    }

    private bool TryFinalizeCurrentLexeme(ScanContext context)
    {
        if (context.Lexeme.Length == 0)
            return false;

        if (!transitionTable.TryGetFinalTokenState(context.CurrentState, EndColumn, out int finalState))
            return false;

        AddTokenForState(context, finalState);
        return true;
    }

    // Emisión de tokens
    private void AddTokenForState(ScanContext context, int state)
    {
        string token = transitionTable.GetToken(state) ?? TokenKinds.Error;
        bool isError = transitionTable.IsErrorState(state);
        string data = isError
            ? transitionTable.GetMessage(state) ?? token
            : context.Lexeme.ToString();

        AddToken(context, isError ? TokenKinds.Error : token, data);

        ResetLexeme(context);
    }

    private void AddStringToken(ScanContext context)
    {
        AddToken(context, TokenKinds.String, context.Lexeme.ToString());
        ResetLexeme(context);
    }

    private static void AddToken(ScanContext context, string token, string data)
    {
        context.Tokens.Add(new Token(token, data, context.CurrentLine, context.LexemeStartColumn));
    }

    private static void ResetLexeme(ScanContext context)
    {
        context.Lexeme.Clear();
        context.CurrentState = InitialState;
    }

    // Estado mutable de una lectura
    private sealed class ScanContext
    {
        public List<Token> Tokens { get; } = [];
        public StringBuilder Lexeme { get; } = new();
        public int CurrentLine { get; set; } = 1;
        public int CurrentState { get; set; } = InitialState;
        public int LexemeStartColumn { get; set; } = 1;
    }
}
