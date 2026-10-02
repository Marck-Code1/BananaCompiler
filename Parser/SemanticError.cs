using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public sealed class SemanticError
{
    public SemanticError(Token token, string message)
    {
        Token = token;
        Message = message;
    }

    public Token Token { get; }
    public string Message { get; }

    public string Format() => $"{Message} Se encontró '{Token.Lexeme}'.";
}
