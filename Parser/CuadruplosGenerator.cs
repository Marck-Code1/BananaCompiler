using System.Text;
using BananaCompiler.Models;

namespace BananaCompiler.Parser;

/// <summary>Cuádruplo clásico (Op, Arg1, Arg2, Resultado).</summary>
public sealed record Cuadruplo(string Op, string Arg1, string Arg2, string Resultado);

/// <summary>
/// Genera la secuencia de cuádruplos y el código 3D legible a partir del AST,
/// con temporales (T1, T2, …) y etiquetas de salto (L1, L2, …). Es una capa
/// puramente derivada del AST validado por el parser: no lo modifica.
/// </summary>
public static class CuadruplosGenerator
{
    /// <summary>Recorre el AST en orden de programa y devuelve los cuádruplos.</summary>
    public static IReadOnlyList<Cuadruplo> Generar(SyntaxNode? root)
    {
        if (root is null)
            return [];

        return new Generador().Ejecutar(root);
    }

    /// <summary>Renderiza los cuádruplos como código 3D, una instrucción por línea.</summary>
    public static string Codigo3D(IReadOnlyList<Cuadruplo> cuadruplos)
    {
        var writer = new StringBuilder();
        foreach (Cuadruplo quad in cuadruplos)
        {
            if (writer.Length > 0)
                writer.AppendLine();
            writer.Append(Linea3D(quad));
        }
        return writer.ToString();
    }

    private static string Linea3D(Cuadruplo quad) => quad.Op switch
    {
        "=" => $"{quad.Resultado} = {quad.Arg1}",
        "ifFalse" => $"ifFalse {quad.Arg1} goto {quad.Resultado}",
        "ifTrue" => $"ifTrue {quad.Arg1} goto {quad.Resultado}",
        "goto" => $"goto {quad.Resultado}",
        "label" => $"{quad.Resultado}:",
        "write" => $"write {quad.Arg1}",
        "read" => $"read {quad.Resultado}",
        "rand" => $"{quad.Resultado} = rand({quad.Arg1})",
        "move" => $"move {quad.Arg1}, {quad.Arg2}",
        "clear" => "clear",
        // Unarios (-, +, !): el operador va antes del operando en 3D.
        _ when quad.Arg2 == "" && quad.Resultado != "_" =>
            $"{quad.Resultado} = {quad.Op}{quad.Arg1}",
        _ when quad.Resultado == "_" && quad.Arg2 == "_" => $"{quad.Op} {quad.Arg1}",
        _ when quad.Resultado == "_" => $"{quad.Op} {quad.Arg1} {quad.Arg2}",
        _ => $"{quad.Resultado} = {quad.Arg1} {quad.Op} {quad.Arg2}",
    };

    private sealed class Generador
    {
        private readonly List<Cuadruplo> cuadruplos = [];
        private readonly Stack<string> finCiclos = [];
        private readonly HashSet<string> etiquetasUsadas = [];
        private int temporales;
        private int etiquetas;

        public IReadOnlyList<Cuadruplo> Ejecutar(SyntaxNode root)
        {
            Sentencia(root);
            return cuadruplos;
        }

        private string NuevaTemporal() => $"T{++temporales}";

        private string NuevaEtiqueta() => $"L{++etiquetas}";

        private void Emitir(string op, string arg1 = "_", string arg2 = "_", string resultado = "_")
        {
            cuadruplos.Add(new Cuadruplo(op, arg1, arg2, resultado));
            if ((op == "goto" || op == "ifFalse" || op == "ifTrue") && resultado != "_")
                etiquetasUsadas.Add(resultado);
        }

        // ---------------------------------------------------------------
        // Sentencias
        // ---------------------------------------------------------------

        private void Sentencia(SyntaxNode nodo)
        {
            switch (nodo.Kind)
            {
                case "S":
                case "INICIO":
                case "INS":
                    foreach (SyntaxNode hijo in nodo.Children)
                        Sentencia(hijo);
                    break;

                case "ε":
                    break;

                case "ASSIGN": // ID OPA EXP
                    Emitir("=", Expresion(nodo.Children[2]), "", Lexema(nodo, 0));
                    break;

                case "WRITE":
                    Emitir("write", Expresion(nodo.Children[1]));
                    break;

                case "READ":
                    Emitir("read", "_", "_", Lexema(nodo, 1));
                    break;

                case "IF":
                    IfConOptionalElse(nodo);
                    break;

                case "WHILE":
                    {
                        string etiquetaCondicion = NuevaEtiqueta();
                        string etiquetaFin = NuevaEtiqueta();
                        Emitir("label", "_", "_", etiquetaCondicion);
                        Emitir("ifFalse", Expresion(nodo.Children[1]), "_", etiquetaFin);
                        finCiclos.Push(etiquetaFin);
                        Sentencia(nodo.Children[3]);
                        finCiclos.Pop();
                        Emitir("goto", "_", "_", etiquetaCondicion);
                        Emitir("label", "_", "_", etiquetaFin);
                        break;
                    }

                case "FOR":
                    ForComoCiclo(nodo);
                    break;

                case "DO":
                    {
                        string etiquetaInicio = NuevaEtiqueta();
                        string etiquetaFin = NuevaEtiqueta();
                        Emitir("label", "_", "_", etiquetaInicio);
                        finCiclos.Push(etiquetaFin);
                        Sentencia(nodo.Children[1]);
                        finCiclos.Pop();
                        Emitir("ifTrue", Expresion(nodo.Children[4]), "_", etiquetaInicio);
                        if (etiquetasUsadas.Contains(etiquetaFin))
                            Emitir("label", "_", "_", etiquetaFin);
                        break;
                    }

                case "RAND":
                    Emitir("rand", Expresion(nodo.Children[3]), "_", Lexema(nodo, 1));
                    break;

                case "MOVE":
                    Emitir("move", Expresion(nodo.Children[1]), Expresion(nodo.Children[3]));
                    break;

                case "BREAK":
                    if (finCiclos.Count > 0)
                        Emitir("goto", "_", "_", finCiclos.Peek());
                    break;

                case "CLEAR":
                    Emitir("clear");
                    break;

                case "ERROR": // la recuperación no genera código: solo se marca
                    Emitir("error", ErrorTexto(nodo));
                    break;
            }
        }

        // IF [if, EXP, then, INS, IF_T] | IF_T [endif] o [else, INS, endif]
        private void IfConOptionalElse(SyntaxNode nodo)
        {
            SyntaxNode cola = nodo.Children[4];
            bool tieneElse = cola.Children.Count > 1;

            string etiquetaSalto = NuevaEtiqueta();
            Emitir("ifFalse", Expresion(nodo.Children[1]), "_", etiquetaSalto);
            Sentencia(nodo.Children[3]);

            if (!tieneElse)
            {
                Emitir("label", "_", "_", etiquetaSalto);
                return;
            }

            string etiquetaFin = NuevaEtiqueta();
            Emitir("goto", "_", "_", etiquetaFin);
            Emitir("label", "_", "_", etiquetaSalto);
            Sentencia(cola.Children[1]);
            Emitir("label", "_", "_", etiquetaFin);
        }

        // for ID from EXP to EXP do INS endfor => asignación, condición,
        // cuerpo e incremento con saltos, como el clásico.
        private void ForComoCiclo(SyntaxNode nodo)
        {
            string variable = Lexema(nodo, 1);
            Emitir("=", Expresion(nodo.Children[3]), "", variable); // desde
            string limiteFinal = Expresion(nodo.Children[5]); // se evalúa una vez

            string etiquetaCondicion = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();
            Emitir("label", "_", "_", etiquetaCondicion);
            string comparacion = NuevaTemporal();
            Emitir(">", variable, limiteFinal, comparacion);
            Emitir("ifTrue", comparacion, "_", etiquetaFin);

            finCiclos.Push(etiquetaFin);
            Sentencia(nodo.Children[7]);
            finCiclos.Pop();

            string incremento = NuevaTemporal();
            Emitir("+", variable, "1", incremento);
            Emitir("=", incremento, "", variable);
            Emitir("goto", "_", "_", etiquetaCondicion);
            Emitir("label", "_", "_", etiquetaFin);
        }

        // ---------------------------------------------------------------
        // Expresiones: devuelven el operando (literal, ID o temporal) y
        // dejan los cuádruplos de cada operación emitidos.
        // ---------------------------------------------------------------

        private string Expresion(SyntaxNode nodo)
        {
            switch (nodo.Kind)
            {
                case "ε":
                    return string.Empty;

                case "MISSING":
                    return "?";

                case "EXP":
                    return Expresion(nodo.Children[0]);

                case "EXP_OR":
                case "EXP_AND":
                case "EXP_ARIT":
                case "TERMINO":
                case "POTENCIA":
                case "EXP_REL":
                    return Cadena(nodo);

                case "EXP_NOT":
                case "UNARIO":
                    {
                        string operando = Expresion(nodo.Children[^1]);
                        if (nodo.Children.Count <= 1)
                            return operando; // passthrough sin operador
                        string resultado = NuevaTemporal();
                        Emitir(Lexema(nodo, 0), operando, "", resultado);
                        return resultado;
                    }

                case "FACTOR":
                    if (nodo.Children.Count > 1 && nodo.Children[0].Kind == TokenKinds.LeftParen)
                        return Expresion(nodo.Children[1]);
                    if (nodo.Children.Count > 0 && nodo.Children[0].Kind == "BOOL")
                        return Expresion(nodo.Children[0]);
                    return nodo.Children.Count > 0 ? Lexema(nodo, 0) : "?";

                case "BOOL":
                    return nodo.Children.Count > 0 ? Lexema(nodo, 0) : "?";

                default:
                    return nodo.Token is not null ? nodo.Token.Lexeme
                        : nodo.Children.Count > 0 ? Expresion(nodo.Children[0])
                        : nodo.Kind;
            }
        }

        // Nodos binarios: [operando izquierdo, (operador, operando derecho)…].
        // Cada par produce un cuádruplo cuyo resultado alimenta al siguiente.
        private string Cadena(SyntaxNode nodo)
        {
            string operando = Expresion(nodo.Children[0]);

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
                string derecha = Expresion(pares[i + 1]);
                string resultado = NuevaTemporal();
                Emitir(Lexema(pares[i]), operando, derecha, resultado);
                operando = resultado;
            }

            return operando;
        }

        private static string Lexema(SyntaxNode padre, int indice) =>
            indice < padre.Children.Count ? Lexema(padre.Children[indice]) : "?";

        private static string Lexema(SyntaxNode nodo) =>
            nodo.Token?.Lexeme ?? (nodo.Kind == "MISSING" ? "?" : nodo.Kind);

        private static string ErrorTexto(SyntaxNode nodo) =>
            nodo.Children.Count == 0
                ? "<error>"
                : string.Join(" ", nodo.Children.Select(child => $"'{Lexema(child)}'"));
    }
}
