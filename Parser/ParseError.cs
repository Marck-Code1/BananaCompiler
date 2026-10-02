using BananaCompiler.Lexer;
using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public sealed class ParseError
{
    private readonly SortedSet<string> expected = [];
    private static readonly Dictionary<string, string> ContextLabels = new()
    {
        ["program"] = "programa",
        ["instruction list"] = "lista de instrucciones",
        ["instruction"] = "una instrucción",
        ["assignment"] = "asignación",
        ["write instruction"] = "instrucción write",
        ["read instruction"] = "instrucción read",
        ["if instruction"] = "instrucción if",
        ["if tail"] = "cierre de if",
        ["while instruction"] = "instrucción while",
        ["for instruction"] = "instrucción for",
        ["do while instruction"] = "instrucción do while",
        ["rand instruction"] = "instrucción rand",
        ["move instruction"] = "instrucción move",
        ["argument"] = "argumento",
        ["expression"] = "expresión",
        ["factor"] = "factor",
        ["oa"] = "operación aritmética",
        ["oa term"] = "término de operación aritmética",
        ["condition"] = "condición",
        ["condition base"] = "condición",
        ["relational expression"] = "expresión relacional",
        ["boolean"] = "booleano"
    };

    private static readonly Dictionary<string, string> ExpectedLabels = new()
    {
        [TokenKinds.EndOfFile] = "fin de archivo",
        [TokenKinds.Identifier] = "identificador",
        [TokenKinds.Number] = "número",
        [TokenKinds.RealNumber] = "número real",
        [TokenKinds.ScientificNumber] = "número exponencial",
        [TokenKinds.String] = "cadena",
        [TokenKinds.Plus] = "'+'",
        [TokenKinds.Minus] = "'-'",
        [TokenKinds.Multiply] = "'*'",
        [TokenKinds.Divide] = "'/'",
        [TokenKinds.Power] = "'^'",
        [TokenKinds.Modulo] = "'%'",
        [TokenKinds.LessThan] = "'<'",
        [TokenKinds.LessThanOrEqual] = "'<='",
        [TokenKinds.GreaterThan] = "'>'",
        [TokenKinds.GreaterThanOrEqual] = "'>='",
        [TokenKinds.Equal] = "'=='",
        [TokenKinds.NotEqual] = "'!='",
        [TokenKinds.And] = "'&&'",
        [TokenKinds.Or] = "'||'",
        [TokenKinds.Not] = "'!'",
        [TokenKinds.Assignment] = "'='",
        [TokenKinds.LeftParen] = "'('",
        [TokenKinds.RightParen] = "')'",
        [TokenKinds.Comma] = "','",
        [TokenKinds.Break] = "'break'",
        [TokenKinds.Clear] = "'clear'",
        [TokenKinds.Do] = "'do'",
        [TokenKinds.Else] = "'else'",
        [TokenKinds.End] = "'end'",
        [TokenKinds.EndFor] = "'endfor'",
        [TokenKinds.EndIf] = "'endif'",
        [TokenKinds.EndWhile] = "'endwhile'",
        [TokenKinds.False] = "'false'",
        [TokenKinds.For] = "'for'",
        [TokenKinds.From] = "'from'",
        [TokenKinds.If] = "'if'",
        [TokenKinds.Move] = "'move'",
        [TokenKinds.Rand] = "'rand'",
        [TokenKinds.Read] = "'read'",
        [TokenKinds.Start] = "'start'",
        [TokenKinds.Then] = "'then'",
        [TokenKinds.To] = "'to'",
        [TokenKinds.True] = "'true'",
        [TokenKinds.While] = "'while'",
        [TokenKinds.Write] = "'write'",
        ["instruction"] = "una instrucción",
        ["argument"] = "argumento",
        ["factor"] = "factor",
        ["expression"] = "expresión",
        ["condition"] = "condición",
        ["relational operator"] = "operador relacional",
        ["ID, CN, or CE5"] = "identificador, número o '('",
        ["PR19 or PR9"] = "'true' o 'false'"
    };

    public ParseError(Token token, string context)
    {
        Token = token;
        Context = context;
    }

    public Token Token { get; }
    public string Context { get; private set; }
    public IReadOnlyCollection<string> Expected => expected;

    public ParseError Clone()
    {
        ParseError clone = new(Token, Context);

        foreach (string value in expected)
            clone.expected.Add(value);

        return clone;
    }

    public void AddExpected(string kind, string context)
    {
        expected.Add(kind);

        if (!string.IsNullOrWhiteSpace(context))
            Context = context;
    }

    public string Format()
    {
        string expectedText = expected.Count == 0
            ? "sintaxis válida"
            : string.Join(", ", expected.Select(TranslateExpected));

        string found = Token.Kind == TokenKinds.EndOfFile
            ? "fin de archivo"
            : $"'{Token.Lexeme}'";

        string contextText = TranslateContext(Context);

        if (expected.SetEquals(["instruction"]) && Context == "instruction list")
            return $"Se esperaba {expectedText}; se encontró {found}.";

        if (expectedText == contextText)
            return $"Se esperaba {expectedText}; se encontró {found}.";

        return $"Se esperaba {expectedText} en {contextText}; se encontró {found}.";
    }

    private static string TranslateContext(string context) =>
        ContextLabels.TryGetValue(context, out string? label) ? label : context;

    private static string TranslateExpected(string value) =>
        ExpectedLabels.TryGetValue(value, out string? label) ? label : value;
}
