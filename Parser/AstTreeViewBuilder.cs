using System.Windows.Forms;

namespace BananaCompiler.Parser;

/// <summary>
/// Convierte el AST (<see cref="SyntaxNode"/>) en nodos <see cref="TreeNode"/>
/// para poblar un TreeView. Es una capa puramente visual: no modifica el AST
/// ni el parser; las etiquetas en español ("Declaración:", "Asignación:",
/// "Condición:", …) se calculan aquí y solo aquí.
/// </summary>
public static class AstTreeViewBuilder
{
    private static readonly Dictionary<string, string> Etiquetas = new(StringComparer.Ordinal)
    {
        ["S"] = "Programa:",
        ["INICIO"] = "Programa:",
        ["INS"] = "Secuencia:",
        ["ASSIGN"] = "Asignación:", // "Declaración:" en la primera aparición del ID.
        ["WRITE"] = "Escribir:",
        ["READ"] = "Leer:",
        ["IF"] = "Condicional (if):",
        ["IF_T"] = "Fin if / rama else:",
        ["WHILE"] = "Ciclo while:",
        ["FOR"] = "Ciclo for:",
        ["DO"] = "Ciclo do-while:",
        ["BREAK"] = "Romper ciclo:",
        ["CLEAR"] = "Limpiar pantalla:",
        ["RAND"] = "Aleatorio:",
        ["MOVE"] = "Mover:",
        ["EXP"] = "Expresión:",
        ["EXP_OR"] = "Expresión lógica:",
        ["EXP_AND"] = "Expresión lógica:",
        ["EXP_REL"] = "Condición:",
        ["EXP_NOT"] = "Negación:",
        ["EXP_ARIT"] = "Expresión aritmética:",
        ["TERMINO"] = "Término:",
        ["UNARIO"] = "Signo unario:",
        ["POTENCIA"] = "Potencia:",
        ["FACTOR"] = "Factor:",
        ["BOOL"] = "Booleano:",
        ["ERROR"] = "Error de recuperación:",
        ["ε"] = "ε (vacío)",
    };

    /// <summary>Construye la raíz del TreeView a partir de la raíz del AST.</summary>
    /// <returns><c>null</c> si el AST no fue construido.</returns>
    public static TreeNode? Build(SyntaxNode? root)
    {
        if (root is null)
            return null;

        return Convertir(root, new HashSet<string>(StringComparer.Ordinal), etiquetaForzada: null);
    }

    private static TreeNode Convertir(
        SyntaxNode nodo,
        HashSet<string> identificadoresVistos,
        string? etiquetaForzada)
    {
        var tree = new TreeNode(EtiquetaDeNodo(nodo, etiquetaForzada))
        {
            Tag = nodo,
            ToolTipText = $"{nodo.Line}:{nodo.Column} · {nodo.Kind}",
        };

        switch (nodo.Kind)
        {
            case "ASSIGN":
                ConvertirAsignacion(tree, nodo, identificadoresVistos);
                break;

            case "IF":
            case "WHILE":
                // Hijos: [palabra clave, EXP de la condición, resto…].
                Agregar(tree, nodo.Children[0], identificadoresVistos);
                tree.Nodes.Add(Convertir(nodo.Children[1], identificadoresVistos, "Condición:"));
                for (int i = 2; i < nodo.Children.Count; i++)
                    Agregar(tree, nodo.Children[i], identificadoresVistos);
                break;

            case "DO":
                // Hijos: [do, cuerpo, end, while, EXP de la condición].
                for (int i = 0; i < nodo.Children.Count - 1; i++)
                    Agregar(tree, nodo.Children[i], identificadoresVistos);
                tree.Nodes.Add(Convertir(nodo.Children[^1], identificadoresVistos, "Condición:"));
                break;

            case "FOR":
                ConvertirFor(tree, nodo, identificadoresVistos);
                break;

            case "EXP_OR":
            case "EXP_AND":
            case "EXP_ARIT":
            case "TERMINO":
            case "POTENCIA":
            case "EXP_REL":
                ConvertirBinario(tree, nodo, identificadoresVistos);
                break;

            default:
                foreach (SyntaxNode hijo in nodo.Children)
                    Agregar(tree, hijo, identificadoresVistos);
                break;
        }

        return tree;
    }

    // ASSIGN -> ID OPA EXP. La primera aparición de un identificador es una
    // declaración implícita (así lo maneja la tabla de símbolos); a partir de
    // ahí es una reasignación.
    private static void ConvertirAsignacion(TreeNode tree, SyntaxNode nodo, HashSet<string> vistas)
    {
        string? nombre = nodo.Children.Count > 0 ? nodo.Children[0].Token?.Lexeme : null;
        bool esDeclaracion = nombre is not null && vistas.Add(nombre);
        if (nombre is not null)
            tree.Text = $"{(esDeclaracion ? "Declaración:" : "Asignación:")} '{nombre}'";

        foreach (SyntaxNode hijo in nodo.Children)
            Agregar(tree, hijo, vistas);
    }

    // FOR -> ID from EXP to EXP do INS endfor. La variable de control se
    // declara explícitamente en la tabla de símbolos: se etiqueta "Declaración:".
    private static void ConvertirFor(TreeNode tree, SyntaxNode nodo, HashSet<string> vistas)
    {
        Agregar(tree, nodo.Children[0], vistas); // 'for'

        SyntaxNode control = nodo.Children[1];
        string? nombre = control.Token?.Lexeme;
        if (nombre is not null)
            vistas.Add(nombre);
        tree.Nodes.Add(Convertir(control, vistas, $"Declaración: '{nombre}'"));

        Agregar(tree, nodo.Children[2], vistas); // 'from'
        tree.Nodes.Add(Convertir(nodo.Children[3], vistas, "Límite inicial:"));
        Agregar(tree, nodo.Children[4], vistas); // 'to'
        tree.Nodes.Add(Convertir(nodo.Children[5], vistas, "Límite final:"));

        for (int i = 6; i < nodo.Children.Count; i++)
            Agregar(tree, nodo.Children[i], vistas);
    }

    // Nodos binarios: [operando izquierdo, (operador, operando derecho)…].
    // EXP_OR/EXP_AND guardan los pares directamente; EXP_ARIT/TERMINO/
    // POTENCIA/EXP_REL los delegan a su nodo cola *_T. Se muestran los
    // operadores como hijos propios y se omiten los nodos *_T intermedios.
    private static void ConvertirBinario(TreeNode tree, SyntaxNode nodo, HashSet<string> vistas)
    {
        Agregar(tree, nodo.Children[0], vistas); // operando izquierdo

        SyntaxNode? cola = null;
        for (int i = 1; i < nodo.Children.Count; i++)
        {
            if (nodo.Children[i].Kind.EndsWith("_T", StringComparison.Ordinal))
            {
                cola = nodo.Children[i];
                break;
            }
        }

        IReadOnlyList<SyntaxNode> pares = cola?.Children ?? nodo.Children.Skip(1).ToList();

        for (int i = 0; i + 1 < pares.Count; i += 2)
        {
            SyntaxNode operador = pares[i];
            var operadorNode = new TreeNode($"Operador: '{operador.Token?.Lexeme}'")
            {
                Tag = operador,
                ToolTipText = $"{operador.Line}:{operador.Column} · {operador.Kind}",
            };
            operadorNode.Nodes.Add(Convertir(pares[i + 1], vistas, etiquetaForzada: null));
            tree.Nodes.Add(operadorNode);
        }

        // ε explícito (producción vacía) u otros hijos residuales.
        int procesados = (pares.Count / 2) * 2;
        for (int i = procesados; i < pares.Count; i++)
            Agregar(tree, pares[i], vistas);
    }

    private static void Agregar(TreeNode tree, SyntaxNode hijo, HashSet<string> vistas) =>
        tree.Nodes.Add(Convertir(hijo, vistas, etiquetaForzada: null));

    private static string EtiquetaDeNodo(SyntaxNode nodo, string? etiquetaForzada)
    {
        if (nodo.Kind == "MISSING")
            return $"⚠ Token faltante '{nodo.Token?.Lexeme}' [{nodo.Line}:{nodo.Column}]";

        string texto = etiquetaForzada
            ?? (Etiquetas.TryGetValue(nodo.Kind, out string? etiqueta) ? etiqueta : DescribirHoja(nodo));

        // Los nodos EXP* publican su tipo inferido, igual que ToTreeString().
        string? tipo = nodo.Semantic?.DataType;
        return tipo is null ? texto : $"{texto} [{tipo}]";
    }

    private static string DescribirHoja(SyntaxNode nodo) =>
        nodo.Token is null
            ? nodo.Kind
            : $"'{nodo.Token.Lexeme}' ({nodo.Kind}) [{nodo.Line}:{nodo.Column}]";
}
