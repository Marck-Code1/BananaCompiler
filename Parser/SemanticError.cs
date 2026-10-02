using BananaCompiler.Models;

namespace BananaCompiler.Parser;

public sealed class SemanticError
{
    public SemanticError(Token token, string message, string code = "SEM_TYPE_MISMATCH")
    {
        Token = token;
        Message = message;
        Code = code;
    }

    public Token Token { get; }
    public string Message { get; }
    public string Code { get; }

    public string Format() => Message;
}
