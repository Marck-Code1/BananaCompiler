using BananaCompiler.Models;

namespace BananaCompiler.Parser;

/// <summary>Genera triplos a partir del AST sin modificarlo.</summary>
public static class TriplosGenerator
{
    public static List<Triplo> Generar(SyntaxNode? root)
    {
        if (root is null)
            return [];

        var generador = new Generador();
        generador.Ejecutar(root);
        return generador.Triplos;
    }

    private sealed class Generador
    {
        public List<Triplo> Triplos { get; } = [];
        private readonly Stack<List<int>> saltosBreak = new();

        public void Ejecutar(SyntaxNode root) => Sentencia(root);

        private Triplo Emitir(string operador, string arg1 = "", string arg2 = "")
        {
            var triplo = new Triplo(Triplos.Count, operador, arg1, arg2);
            Triplos.Add(triplo);
            return triplo;
        }

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
                case "ASSIGN":
                    Emitir("=", Expresion(nodo.Children[2]), Lexema(nodo, 0));
                    break;
                case "WRITE":
                    Emitir("write", Expresion(nodo.Children[1]));
                    break;
                case "READ":
                    Emitir("read", Lexema(nodo, 1));
                    break;
                case "IF":
                    If(nodo);
                    break;
                case "WHILE":
                    While(nodo);
                    break;
                case "FOR":
                    For(nodo);
                    break;
                case "DO":
                    DoWhile(nodo);
                    break;
                case "RAND":
                    Emitir("rand", Expresion(nodo.Children[3]), Lexema(nodo, 1));
                    break;
                case "MOVE":
                    Emitir("move", Expresion(nodo.Children[1]), Expresion(nodo.Children[3]));
                    break;
                case "BREAK":
                    if (saltosBreak.Count > 0)
                        saltosBreak.Peek().Add(Emitir("goto").Indice);
                    break;
                case "CLEAR":
                    Emitir("clear");
                    break;
                case "ERROR":
                    Emitir("error", ErrorTexto(nodo));
                    break;
            }
        }

        private void If(SyntaxNode nodo)
        {
            SyntaxNode cola = nodo.Children[4];
            bool tieneElse = cola.Children.Count > 1;
            Triplo saltoFalso = Emitir("ifFalso", Expresion(nodo.Children[1]));
            Sentencia(nodo.Children[3]);

            if (!tieneElse)
            {
                saltoFalso.Arg2 = Emitir("Label").Indice.ToString();
                return;
            }

            Triplo saltoFin = Emitir("goto");
            saltoFalso.Arg2 = Emitir("Label").Indice.ToString();
            Sentencia(cola.Children[1]);
            saltoFin.Arg1 = Emitir("Label").Indice.ToString();
        }

        private void While(SyntaxNode nodo)
        {
            int inicio = Emitir("Label").Indice;
            Triplo saltoFin = Emitir("ifFalso", Expresion(nodo.Children[1]));
            var breaks = new List<int>();
            saltosBreak.Push(breaks);
            Sentencia(nodo.Children[3]);
            saltosBreak.Pop();
            Emitir("goto", inicio.ToString());
            int fin = Emitir("Label").Indice;
            saltoFin.Arg2 = fin.ToString();
            ResolverBreaks(breaks, fin);
        }

        private void For(SyntaxNode nodo)
        {
            string variable = Lexema(nodo, 1);
            Emitir("=", Expresion(nodo.Children[3]), variable);
            string limite = Expresion(nodo.Children[5]);
            int inicio = Emitir("Label").Indice;
            string comparacion = Referencia(Emitir(">", variable, limite).Indice);
            Triplo saltoFin = Emitir("ifTrue", comparacion);
            var breaks = new List<int>();
            saltosBreak.Push(breaks);
            Sentencia(nodo.Children[7]);
            saltosBreak.Pop();
            string incremento = Referencia(Emitir("+", variable, "1").Indice);
            Emitir("=", incremento, variable);
            Emitir("goto", inicio.ToString());
            int fin = Emitir("Label").Indice;
            saltoFin.Arg2 = fin.ToString();
            ResolverBreaks(breaks, fin);
        }

        private void DoWhile(SyntaxNode nodo)
        {
            int inicio = Emitir("Label").Indice;
            var breaks = new List<int>();
            saltosBreak.Push(breaks);
            Sentencia(nodo.Children[1]);
            saltosBreak.Pop();
            Emitir("ifTrue", Expresion(nodo.Children[4]), inicio.ToString());
            if (breaks.Count > 0)
            {
                int fin = Emitir("Label").Indice;
                ResolverBreaks(breaks, fin);
            }
        }

        private void ResolverBreaks(IEnumerable<int> indices, int destino)
        {
            foreach (int indice in indices)
                Triplos[indice].Arg1 = destino.ToString();
        }

        private static string Referencia(int indice) => $"({indice})";

        private string Expresion(SyntaxNode nodo)
        {
            switch (nodo.Kind)
            {
                case "ε": return "";
                case "MISSING": return "?";
                case "EXP": return Expresion(nodo.Children[0]);
                case "EXP_OR":
                case "EXP_AND":
                case "EXP_ARIT":
                case "TERMINO":
                case "POTENCIA":
                case "EXP_REL":
                    return Cadena(nodo);
                case "EXP_NOT":
                case "UNARIO":
                    if (nodo.Children.Count <= 1)
                        return Expresion(nodo.Children[^1]);
                    return Referencia(Emitir(Lexema(nodo, 0), Expresion(nodo.Children[^1])).Indice);
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
                        : nodo.Children.Count > 0 ? Expresion(nodo.Children[0]) : nodo.Kind;
            }
        }

        private string Cadena(SyntaxNode nodo)
        {
            string operando = Expresion(nodo.Children[0]);
            SyntaxNode? cola = nodo.Children.Skip(1).FirstOrDefault(h => h.Kind.EndsWith("_T", StringComparison.Ordinal));
            IReadOnlyList<SyntaxNode> pares = cola?.Children ?? nodo.Children.Skip(1).ToList();
            for (int i = 0; i + 1 < pares.Count; i += 2)
            {
                int indice = Emitir(Lexema(pares[i]), operando, Expresion(pares[i + 1])).Indice;
                operando = Referencia(indice);
            }
            return operando;
        }

        private static string Lexema(SyntaxNode padre, int indice) =>
            indice < padre.Children.Count ? Lexema(padre.Children[indice]) : "?";

        private static string Lexema(SyntaxNode nodo) =>
            nodo.Token?.Lexeme ?? (nodo.Kind == "MISSING" ? "?" : nodo.Kind);

        private static string ErrorTexto(SyntaxNode nodo) => nodo.Children.Count == 0
            ? "<error>"
            : string.Join(" ", nodo.Children.Select(child => $"'{Lexema(child)}'"));
    }
}
