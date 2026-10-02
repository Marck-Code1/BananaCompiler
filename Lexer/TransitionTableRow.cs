namespace BananaCompiler.Lexer;

public class TransitionTableRow
{
    public int State { get; }
    public IReadOnlyDictionary<string, int> Transitions { get; }
    public string? Token { get; }
    public string? Message { get; }

    public TransitionTableRow(
        int state,
        IReadOnlyDictionary<string, int> transitions,
        string? token,
        string? message = null)
    {
        State = state;
        Transitions = transitions;
        Token = token;
        Message = message;
    }
}
