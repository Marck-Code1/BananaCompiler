using BananaCompiler.Models;

namespace BananaCompiler.Lexer;

public class TransitionTable
{
    private readonly Dictionary<(int State, string Column), int> transitions = [];
    private readonly Dictionary<int, string> tokens = [];
    private readonly Dictionary<int, string> messages = [];
    private readonly HashSet<int> errorStates = [];

    public TransitionTable(IEnumerable<TransitionTableRow> rows)
    {
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Token))
                tokens[row.State] = row.Token;

            if (!string.IsNullOrWhiteSpace(row.Message))
                messages[row.State] = row.Message;

            // ERROR identifica los estados de error generados dinámicamente.
            if (string.Equals(row.Token, TokenKinds.Error, StringComparison.OrdinalIgnoreCase))
                errorStates.Add(row.State);

            foreach (var transition in row.Transitions)
            {
                transitions[(row.State, transition.Key)] = transition.Value;
            }
        }
    }

    public int? GetTransition(string column, int state)
    {
        return transitions.TryGetValue((state, column), out int nextState) ? nextState : null;
    }

    public bool TryGetFinalTokenState(int state, string endColumn, out int finalState)
    {
        int? candidate = GetTransition(endColumn, state);
        if (candidate is null || GetToken(candidate.Value) is null)
        {
            finalState = default;
            return false;
        }

        finalState = candidate.Value;
        return true;
    }

    public bool CanStartToken(string column, int initialState)
    {
        int? nextState = GetTransition(column, initialState);
        return nextState is not null && !IsErrorState(nextState.Value);
    }

    public string? GetToken(int state)
    {
        return tokens.TryGetValue(state, out string? token) ? token : null;
    }

    public string? GetMessage(int state)
    {
        return messages.TryGetValue(state, out string? message) ? message : null;
    }

    public bool IsErrorState(int state) => errorStates.Contains(state);
}
