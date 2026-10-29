using BananaCompiler.Models;

namespace BananaCompiler.Parser;

/// <summary>Genera código P de pila a partir del árbol sintáctico sin modificarlo.</summary>
public static class PCodeGenerator
{
    public static List<string> Generar(SyntaxNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new Generador().Ejecutar(root);
    }

    private sealed class Generador
    {
        private readonly List<string> codigo = [];
        private readonly Stack<string> destinosBreak = new();
        private int siguienteEtiqueta;
        private int siguienteLimiteFor;

        public List<string> Ejecutar(SyntaxNode root)
        {
            Sentencia(root);
            return codigo;
        }

        private string NuevaEtiqueta() => $"L{++siguienteEtiqueta}";

        private void Emitir(string instruccion) => codigo.Add(instruccion);

        private void Etiqueta(string etiqueta) => Emitir($"LAB {etiqueta}:");

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
                case "MISSING":
                    break;
                case "ASSIGN":
                    Expresion(nodo.Children[2]);
                    Emitir($"STO {Lexema(nodo, 0)}");
                    break;
                case "WRITE":
                    Expresion(nodo.Children[1]);
                    Emitir("OUT"); // Extensión de E/S del conjunto base de Código P.
                    break;
                case "READ":
                    Emitir($"INP {Lexema(nodo, 1)}"); // Extensión de E/S.
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
                case "BREAK":
                    if (destinosBreak.Count > 0)
                        Emitir($"JMP {destinosBreak.Peek()}");
                    break;
                case "RAND":
                    Expresion(nodo.Children[3]);
                    Emitir($"RND {Lexema(nodo, 1)}"); // Extensión: genera entero [1, límite].
                    break;
                case "MOVE":
                    Expresion(nodo.Children[1]);
                    Expresion(nodo.Children[3]);
                    Emitir("MOV"); // Extensión de la máquina didáctica Banana.
                    break;
                case "CLEAR":
                    Emitir("CLR"); // Extensión de la máquina didáctica Banana.
                    break;
                case "ERROR":
                    // Los nodos ERROR solo aparecen en árboles recuperados; no
                    // se inventa una instrucción ejecutable para código inválido.
                    break;
                default:
                    foreach (SyntaxNode hijo in nodo.Children)
                        Sentencia(hijo);
                    break;
            }
        }

        private void If(SyntaxNode nodo)
        {
            string etiquetaFalsa = NuevaEtiqueta();
            SyntaxNode cola = nodo.Children[4];
            bool tieneElse = cola.Children.Count > 1;
            Expresion(nodo.Children[1]);
            Emitir($"JPC {etiquetaFalsa}");
            Sentencia(nodo.Children[3]);

            if (!tieneElse)
            {
                Etiqueta(etiquetaFalsa);
                return;
            }

            string etiquetaFin = NuevaEtiqueta();
            Emitir($"JMP {etiquetaFin}");
            Etiqueta(etiquetaFalsa);
            Sentencia(cola.Children[1]);
            Etiqueta(etiquetaFin);
        }

        private void While(SyntaxNode nodo)
        {
            string etiquetaInicio = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();
            Etiqueta(etiquetaInicio);
            Expresion(nodo.Children[1]);
            Emitir($"JPC {etiquetaFin}");
            destinosBreak.Push(etiquetaFin);
            Sentencia(nodo.Children[3]);
            destinosBreak.Pop();
            Emitir($"JMP {etiquetaInicio}");
            Etiqueta(etiquetaFin);
        }

        private void For(SyntaxNode nodo)
        {
            string variable = Lexema(nodo, 1);
            string limite = $"$forLimit{++siguienteLimiteFor}";
            string etiquetaInicio = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();

            Expresion(nodo.Children[3]);
            Emitir($"STO {variable}");
            Expresion(nodo.Children[5]);
            Emitir($"STO {limite}");

            Etiqueta(etiquetaInicio);
            Emitir($"LOD {variable}");
            Emitir($"LOD {limite}");
            Emitir("LEQ");
            Emitir($"JPC {etiquetaFin}");
            destinosBreak.Push(etiquetaFin);
            Sentencia(nodo.Children[7]);
            destinosBreak.Pop();
            Emitir($"LOD {variable}");
            Emitir("LDC 1");
            Emitir("ADD");
            Emitir($"STO {variable}");
            Emitir($"JMP {etiquetaInicio}");
            Etiqueta(etiquetaFin);
        }

        private void DoWhile(SyntaxNode nodo)
        {
            string etiquetaInicio = NuevaEtiqueta();
            string etiquetaCondicion = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();
            Etiqueta(etiquetaInicio);
            destinosBreak.Push(etiquetaFin);
            Sentencia(nodo.Children[1]);
            destinosBreak.Pop();
            Etiqueta(etiquetaCondicion);
            Expresion(nodo.Children[4]);
            Emitir($"JPC {etiquetaFin}");
            Emitir($"JMP {etiquetaInicio}");
            Etiqueta(etiquetaFin);
        }

        private void Expresion(SyntaxNode nodo)
        {
            switch (nodo.Kind)
            {
                case "ε":
                case "MISSING":
                    return;
                case "EXP":
                    Expresion(nodo.Children[0]);
                    return;
                case "EXP_OR":
                    CadenaLogica(nodo, esOr: true);
                    return;
                case "EXP_AND":
                    CadenaLogica(nodo, esOr: false);
                    return;
                case "EXP_ARIT":
                case "TERMINO":
                case "POTENCIA":
                case "EXP_REL":
                    Cadena(nodo);
                    return;
                case "EXP_NOT":
                    if (nodo.Children.Count > 1)
                    {
                        Expresion(nodo.Children[^1]);
                        EmitirBooleanNot();
                    }
                    else
                    {
                        Expresion(nodo.Children[0]);
                    }
                    return;
                case "UNARIO":
                    if (nodo.Children.Count > 1 && Lexema(nodo, 0) == "-")
                    {
                        Emitir("LDC 0");
                        Expresion(nodo.Children[^1]);
                        Emitir("SUB");
                    }
                    else if (nodo.Children.Count > 1)
                    {
                        Expresion(nodo.Children[^1]); // + unario
                    }
                    else
                    {
                        Expresion(nodo.Children[0]);
                    }
                    return;
                case "FACTOR":
                    if (nodo.Children.Count > 1 && nodo.Children[0].Kind == TokenKinds.LeftParen)
                        Expresion(nodo.Children[1]);
                    else if (nodo.Children.Count > 0 && nodo.Children[0].Kind == "BOOL")
                        Expresion(nodo.Children[0]);
                    else if (nodo.Children.Count > 0)
                        Factor(nodo.Children[0]);
                    return;
                case "BOOL":
                    if (nodo.Children.Count > 0)
                        Factor(nodo.Children[0]);
                    return;
                default:
                    if (nodo.Token is not null)
                        Factor(nodo);
                    else if (nodo.Children.Count > 0)
                        Expresion(nodo.Children[0]);
                    return;
            }
        }

        private void Factor(SyntaxNode nodo)
        {
            string value = Lexema(nodo);
            if (nodo.Kind == TokenKinds.Identifier)
                Emitir($"LOD {value}");
            else if (nodo.Kind == TokenKinds.True)
                Emitir("LDC 1");
            else if (nodo.Kind == TokenKinds.False)
                Emitir("LDC 0");
            else
                Emitir($"LDC {value}");
        }

        private void Cadena(SyntaxNode nodo)
        {
            if (nodo.Children.Count == 0)
                return;
            Expresion(nodo.Children[0]);

            SyntaxNode? cola = nodo.Children.Skip(1)
                .FirstOrDefault(hijo => hijo.Kind.EndsWith("_T", StringComparison.Ordinal));
            IReadOnlyList<SyntaxNode> pares = cola?.Children ?? nodo.Children.Skip(1).ToList();
            for (int i = 0; i + 1 < pares.Count; i += 2)
            {
                SyntaxNode operador = pares[i];
                SyntaxNode derecha = pares[i + 1];
                if (operador.Kind == "ε")
                    continue;
                Expresion(derecha);
                Emitir(InstruccionBinaria(Lexema(operador)));
            }
        }

        private void CadenaLogica(SyntaxNode nodo, bool esOr)
        {
            if (nodo.Children.Count == 0)
                return;
            Expresion(nodo.Children[0]);
            SyntaxNode? cola = nodo.Children.Skip(1)
                .FirstOrDefault(hijo => hijo.Kind.EndsWith("_T", StringComparison.Ordinal));
            IReadOnlyList<SyntaxNode> pares = cola?.Children ?? nodo.Children.Skip(1).ToList();
            for (int i = 0; i + 1 < pares.Count; i += 2)
            {
                if (pares[i].Kind == "ε")
                    continue;
                EmitirBooleanBinary(Expresion, pares[i + 1], esOr);
            }
        }

        private void EmitirBooleanBinary(Action<SyntaxNode> compilarDerecha, SyntaxNode derecha, bool esOr)
        {
            string etiquetaEvaluarDerecha = NuevaEtiqueta();
            string etiquetaFalso = NuevaEtiqueta();
            string etiquetaVerdadero = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();

            if (esOr)
            {
                // JPC consume el operando: verdadero evita evaluar el derecho.
                Emitir($"JPC {etiquetaEvaluarDerecha}");
                Emitir("LDC 1");
                Emitir($"JMP {etiquetaFin}");
                Etiqueta(etiquetaEvaluarDerecha);
                compilarDerecha(derecha);
                Emitir($"JPC {etiquetaFalso}");
                Emitir("LDC 1");
                Emitir($"JMP {etiquetaFin}");
                Etiqueta(etiquetaFalso);
                Emitir("LDC 0");
            }
            else
            {
                Emitir($"JPC {etiquetaFalso}");
                compilarDerecha(derecha);
                Emitir($"JPC {etiquetaFalso}");
                Emitir("LDC 1");
                Emitir($"JMP {etiquetaFin}");
                Etiqueta(etiquetaFalso);
                Emitir("LDC 0");
            }
            Etiqueta(etiquetaFin);
        }

        private void EmitirBooleanNot()
        {
            string etiquetaFalso = NuevaEtiqueta();
            string etiquetaFin = NuevaEtiqueta();
            Emitir($"JPC {etiquetaFalso}");
            Emitir("LDC 0");
            Emitir($"JMP {etiquetaFin}");
            Etiqueta(etiquetaFalso);
            Emitir("LDC 1");
            Etiqueta(etiquetaFin);
        }

        private static string InstruccionBinaria(string operador) => operador switch
        {
            "+" => "ADD",
            "-" => "SUB",
            "*" => "MUL",
            "/" => "DIV",
            "%" => "MOD",
            "^" => "PWR",
            "==" => "EQU",
            "!=" => "NEQ",
            "<" => "LES",
            "<=" => "LEQ",
            ">" => "GTR",
            ">=" => "GEQ",
            _ => throw new InvalidOperationException($"Operador binario no soportado: '{operador}'."),
        };

        private static string Lexema(SyntaxNode padre, int indice) =>
            indice < padre.Children.Count ? Lexema(padre.Children[indice]) : "?";

        private static string Lexema(SyntaxNode nodo) =>
            nodo.Token?.Lexeme ?? (nodo.Kind == "MISSING" ? "?" : nodo.Kind);
    }
}
