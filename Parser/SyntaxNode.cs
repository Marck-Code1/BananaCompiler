namespace BananaCompiler.Parser;

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

    public string Kind { get; }

    public Models.Token? Token { get; }

    public List<SyntaxNode> Children { get; } = [];

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
        Children.Add(new SyntaxNode(token.IsSynthetic ? "MISSING" : kind, token));
        return this;
    }

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

    public string ToCompact()
    {
        if (IsLeaf)
            return Token is null ? Kind : $"{Kind}('{Token.Lexeme}')";
        return $"({Kind} {string.Join(" ", Children.Select(c => c.ToCompact()))})";
    }
}
