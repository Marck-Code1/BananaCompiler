namespace BananaCompiler.Parser;

public sealed class ParseResult
{
    private ParseResult(
        IReadOnlyList<ParseError> errors,
        IReadOnlyDictionary<string, SymbolAssignment> symbolAssignments,
        IReadOnlyList<SemanticError> semanticErrors,
        SyntaxNode? syntaxTree,
        IReadOnlyList<SymbolAssignment> scopedSymbols,
        IReadOnlyList<string> semanticPileLog)
    {
        Errors = errors;
        SymbolAssignments = symbolAssignments;
        SemanticErrors = semanticErrors;
        SyntaxTree = syntaxTree;
        ScopedSymbols = scopedSymbols;
        SemanticPileLog = semanticPileLog;
    }

    public bool Success => Errors.Count == 0 && SemanticErrors.Count == 0;
    public IReadOnlyList<ParseError> Errors { get; }
    public IReadOnlyList<SemanticError> SemanticErrors { get; }
    public IReadOnlyDictionary<string, SymbolAssignment> SymbolAssignments { get; }

    /// <summary>Raíz del árbol de sintaxis (S). Null solo si no se pudo construir.</summary>
    public SyntaxNode? SyntaxTree { get; }

    /// <summary>Símbolos con ámbito y dirección simulada, en orden de memoria.</summary>
    public IReadOnlyList<SymbolAssignment> ScopedSymbols { get; }

    /// <summary>Bitácora de reducciones de la pila semántica.</summary>
    public IReadOnlyList<string> SemanticPileLog { get; }

    public static ParseResult Ok(
        IReadOnlyDictionary<string, SymbolAssignment>? symbolAssignments = null,
        IEnumerable<SemanticError>? semanticErrors = null,
        SyntaxNode? syntaxTree = null,
        IEnumerable<SymbolAssignment>? scopedSymbols = null,
        IEnumerable<string>? semanticPileLog = null) =>
        new(
            [],
            symbolAssignments ?? new Dictionary<string, SymbolAssignment>(),
            semanticErrors?.ToList() ?? [],
            syntaxTree,
            scopedSymbols?.ToList() ?? [],
            semanticPileLog?.ToList() ?? []);

    public static ParseResult Fail(
        ParseError error,
        IReadOnlyDictionary<string, SymbolAssignment>? symbolAssignments = null,
        IEnumerable<SemanticError>? semanticErrors = null,
        SyntaxNode? syntaxTree = null,
        IEnumerable<SymbolAssignment>? scopedSymbols = null,
        IEnumerable<string>? semanticPileLog = null) =>
        new(
            [error],
            symbolAssignments ?? new Dictionary<string, SymbolAssignment>(),
            semanticErrors?.ToList() ?? [],
            syntaxTree,
            scopedSymbols?.ToList() ?? [],
            semanticPileLog?.ToList() ?? []);

    public static ParseResult Fail(
        IEnumerable<ParseError> errors,
        IReadOnlyDictionary<string, SymbolAssignment>? symbolAssignments = null,
        IEnumerable<SemanticError>? semanticErrors = null,
        SyntaxNode? syntaxTree = null,
        IEnumerable<SymbolAssignment>? scopedSymbols = null,
        IEnumerable<string>? semanticPileLog = null) =>
        new(
            errors.ToList(),
            symbolAssignments ?? new Dictionary<string, SymbolAssignment>(),
            semanticErrors?.ToList() ?? [],
            syntaxTree,
            scopedSymbols?.ToList() ?? [],
            semanticPileLog?.ToList() ?? []);
}
