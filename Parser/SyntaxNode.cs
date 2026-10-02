namespace BananaCompiler.Parser;

/// <summary>
/// Nodo del árbol de sintaxis (Syntax Tree).
/// Cada nodo corresponde a un símbolo gramatical (no terminal) o a un
/// terminal (token) cuando es hoja. Los nodos de expresión conservan además
/// el <see cref="SemanticValue"/> inferido durante la reducción, de modo que
/// el árbol sirve a la vez para diagnóstico sintáctico y semántico.
/// </summary>
public sealed class SyntaxNode
{
    public SyntaxNode(string kind, Models.Token? token = null)
    {
        Kind = kind;
        Token = token;
        if (token is not null)
        {
            Line = token.Line;
            Column = token.Column;
        }
    }

    /// <summary>Nombre gramatical: S, INICIO, INS, ASSIGN, EXP_OR, … o kind del token en hojas.</summary>
    public string Kind { get; }

    /// <summary>Token origen. Solo presente en hojas (terminales).</summary>
    public Models.Token? Token { get; }

    public List<SyntaxNode> Children { get; } = [];

    /// <summary>Tipo/valor inferido por la pila semántica (solo nodos EXP*).</summary>
    public SemanticValue? Semantic { get; set; }

    public int Line { get; set; }
    public int Column { get; set; }

    public bool IsLeaf => Children.Count == 0;

    public SyntaxNode AddChild(SyntaxNode child)
    {
        Children.Add(child);
        return this;
    }

    public SyntaxNode AddLeaf(string kind, Models.Token token)
    {
        Children.Add(new SyntaxNode(kind, token));
        return this;
    }

    /// <summary>Impresión indentada del árbol, útil para depuración y para el reporte.</summary>
    public string ToTreeString(string indent = "")
    {
        var writer = new System.Text.StringBuilder();
        AppendTo(writer, indent, true);
        return writer.ToString();
    }

    private void AppendTo(System.Text.StringBuilder writer, string indent, bool isRoot)
    {
        string label = Token is null ? Kind : $"{Kind} '{Token.Lexeme}' ({Line}:{Column})";
        if (Semantic is not null && Semantic.DataType is not null)
            label += $" [{Semantic.DataType}]";
        writer.Append(indent);
        if (!isRoot)
            writer.Append("+- ");
        writer.AppendLine(label);
        string childIndent = isRoot ? indent : indent + "|  ";
        foreach (SyntaxNode child in Children)
            child.AppendTo(writer, childIndent, false);
    }

    /// <summary>Representación compacta tipo Lisp: (EXP_OR left || right).</summary>
    public string ToCompact()
    {
        if (IsLeaf)
            return Token is null ? Kind : $"{Kind}('{Token.Lexeme}')";
        return $"({Kind} {string.Join(" ", Children.Select(c => c.ToCompact()))})";
    }
}
