using System.Text;
using BananaCompiler.Models;

namespace BananaCompiler.Parser;

/// <summary>
/// Genera la notación prefija (preorden) del AST: la raíz (operador) va antes
/// que sus operandos, p. ej. <c>a + b * c</c> → <c>+ a * b c</c>. Cada sentencia
/// completa ocupa una línea y los bloques anidados se indentan. Es una capa
/// puramente visual: no modifica el AST ni el parser.
/// </summary>
public static class NotacionesGenerator
{
    /// <summary>Devuelve la notación prefija de todo el programa, sentencia por línea.</summary>
    public static string Prefija(SyntaxNode? root)
    {
        if (root is null)
            return string.Empty;

        var writer = new StringBuilder();
        Sentencia(writer, root, nivel: 0);
        return writer.ToString();
    }

    // Recorre la estructura de sentencias (S, INICIO, INS e instrucciones) y
    // delega a Expresion() la notación prefija de cada EXP.
    private static void Sentencia(StringBuilder writer, SyntaxNode nodo, int nivel)
    {
        switch (nodo.Kind)
        {
            case "S":
            case "INICIO":
            case "INS":
                foreach (SyntaxNode hijo in nodo.Children)
                    Sentencia(writer, hijo, nivel);
                break;

            case "ε":
                break; // cuerpo vacío: no produce línea

            case "ASSIGN": // ID OPA EXP
                Linea(writer, nivel, $"{Lexema(nodo, 0)} = {Expresion(nodo, 2)}");
                break;

            case "WRITE": // PR21 EXP
                Linea(writer, nivel, $"write {Expresion(nodo, 1)}");
                break;

            case "READ": // PR15 ID
                Linea(writer, nivel, $"read {Lexema(nodo, 1)}");
                break;

            case "IF": // if EXP then INS IF_T
                Linea(writer, nivel, $"if {Expresion(nodo, 1)} then");
                Sentencia(writer, nodo.Children[3], nivel + 1);
                Sentencia(writer, nodo.Children[4], nivel);
                break;

            case "IF_T": // endif | else INS endif
                if (nodo.Children.Count > 1)
                {
                    Linea(writer, nivel, "else");
                    Sentencia(writer, nodo.Children[1], nivel + 1);
                }
                Linea(writer, nivel, "endif");
                break;

            case "WHILE": // while EXP do INS endwhile
                Linea(writer, nivel, $"while {Expresion(nodo, 1)} do");
                Sentencia(writer, nodo.Children[3], nivel + 1);
                Linea(writer, nivel, "endwhile");
                break;

            case "FOR": // for ID from EXP to EXP do INS endfor
                Linea(writer, nivel,
                    $"for {Lexema(nodo, 1)} from {Expresion(nodo, 3)} to {Expresion(nodo, 5)} do");
                Sentencia(writer, nodo.Children[7], nivel + 1);
                Linea(writer, nivel, "endfor");
                break;

            case "DO": // do INS end while EXP
                Linea(writer, nivel, "do");
                Sentencia(writer, nodo.Children[1], nivel + 1);
                Linea(writer, nivel, $"end while {Expresion(nodo, 4)}");
                break;

            case "RAND": // PR14 ID CE7 EXP
                Linea(writer, nivel, $"rand {Lexema(nodo, 1)}, {Expresion(nodo, 3)}");
                break;

            case "MOVE": // PR13 EXP CE7 EXP
                Linea(writer, nivel, $"move {Expresion(nodo, 1)}, {Expresion(nodo, 3)}");
                break;

            case "BREAK":
                Linea(writer, nivel, "break");
                break;

            case "CLEAR":
                Linea(writer, nivel, "clear");
                break;

            case "ERROR": // tokens consumidos por la recuperación de errores
                Linea(writer, nivel, ErrorTexto(nodo));
                break;

            default:
                if (nodo.Token is not null) // hojas sueltas (recuperación)
                    Linea(writer, nivel, nodo.Token.Lexeme);
                break;
        }
    }

    private static string Expresion(SyntaxNode padre, int indice) =>
        indice < padre.Children.Count ? Expresion(padre.Children[indice]) : "?";

    // ---------------------------------------------------------------------------
    // Notación infija (inorden): Izquierda, Raíz, Derecha, con paréntesis
    // inteligentes calculados por precedencia y con variables temporales (t1,
    // t2, …) para las condiciones de los estructuras de control.
    // ---------------------------------------------------------------------------

    /// <summary>Devuelve la notación infija de todo el programa, sentencia por línea.</summary>
    public static string Infija(SyntaxNode? root)
    {
        if (root is null)
            return string.Empty;

        var writer = new StringBuilder();
        SentenciaInfija(writer, new ContadorTemporales(), root, nivel: 0);
        return writer.ToString();
    }

    private sealed class ContadorTemporales
    {
        private int contador;

        public int Siguiente() => ++contador;
    }

    private static void SentenciaInfija(
        StringBuilder writer, ContadorTemporales temporales, SyntaxNode nodo, int nivel)
    {
        switch (nodo.Kind)
        {
            case "S":
            case "INICIO":
            case "INS":
                foreach (SyntaxNode hijo in nodo.Children)
                    SentenciaInfija(writer, temporales, hijo, nivel);
                break;

            case "ε":
                break; // cuerpo vacío: no produce línea

            case "ASSIGN": // ID OPA EXP
                Linea(writer, nivel, $"{Lexema(nodo, 0)} = {TextoInfijo(nodo, 2)}");
                break;

            case "WRITE": // PR21 EXP
                Linea(writer, nivel, $"write {TextoInfijo(nodo, 1)}");
                break;

            case "READ": // PR15 ID
                Linea(writer, nivel, $"read {Lexema(nodo, 1)}");
                break;

            case "IF": // if EXP then INS IF_T
                {
                    int t = CondicionInfija(writer, temporales, nodo, nivel);
                    Linea(writer, nivel, $"if Condición (t{t}) then");
                    SentenciaInfija(writer, temporales, nodo.Children[3], nivel + 1);
                    SentenciaInfija(writer, temporales, nodo.Children[4], nivel);
                    break;
                }

            case "IF_T": // endif | else INS endif
                if (nodo.Children.Count > 1)
                {
                    Linea(writer, nivel, "else");
                    SentenciaInfija(writer, temporales, nodo.Children[1], nivel + 1);
                }
                Linea(writer, nivel, "endif");
                break;

            case "WHILE": // while EXP do INS endwhile
                {
                    int t = CondicionInfija(writer, temporales, nodo, nivel);
                    Linea(writer, nivel, $"while Condición (t{t}) do");
                    SentenciaInfija(writer, temporales, nodo.Children[3], nivel + 1);
                    Linea(writer, nivel, "endwhile");
                    break;
                }

            case "FOR": // for ID from EXP to EXP do INS endfor
                {
                    int limiteInicial = temporales.Siguiente();
                    Linea(writer, nivel, $"Límite inicial (t{limiteInicial}) = {TextoInfijo(nodo, 3)}");
                    int limiteFinal = temporales.Siguiente();
                    Linea(writer, nivel, $"Límite final (t{limiteFinal}) = {TextoInfijo(nodo, 5)}");
                    Linea(writer, nivel,
                        $"for {Lexema(nodo, 1)} from t{limiteInicial} to t{limiteFinal} do");
                    SentenciaInfija(writer, temporales, nodo.Children[7], nivel + 1);
                    Linea(writer, nivel, "endfor");
                    break;
                }

            case "DO": // do INS end while EXP
                Linea(writer, nivel, "do");
                SentenciaInfija(writer, temporales, nodo.Children[1], nivel + 1);
                {
                    int t = temporales.Siguiente();
                    Linea(writer, nivel, $"Condición (t{t}) = {TextoInfijo(nodo, 4)}");
                    Linea(writer, nivel, $"end while Condición (t{t})");
                }
                break;

            case "RAND": // PR14 ID CE7 EXP
                Linea(writer, nivel, $"rand {Lexema(nodo, 1)}, {TextoInfijo(nodo, 3)}");
                break;

            case "MOVE": // PR13 EXP CE7 EXP
                Linea(writer, nivel, $"move {TextoInfijo(nodo, 1)}, {TextoInfijo(nodo, 3)}");
                break;

            case "BREAK":
                Linea(writer, nivel, "break");
                break;

            case "CLEAR":
                Linea(writer, nivel, "clear");
                break;

            case "ERROR": // tokens consumidos por la recuperación de errores
                Linea(writer, nivel, ErrorTexto(nodo));
                break;

            default:
                if (nodo.Token is not null) // hojas sueltas (recuperación)
                    Linea(writer, nivel, nodo.Token.Lexeme);
                break;
        }
    }

    // Emite la línea "Condición (tN) = <expresión infija>" y devuelve N.
    private static int CondicionInfija(
        StringBuilder writer, ContadorTemporales temporales, SyntaxNode nodo, int nivel)
    {
        int t = temporales.Siguiente();
        Linea(writer, nivel, $"Condición (t{t}) = {TextoInfijo(nodo, 1)}");
        return t;
    }

    private static string TextoInfijo(SyntaxNode padre, int indice) =>
        indice < padre.Children.Count ? ExpresionInfija(padre.Children[indice]).Texto : "?";

    // Niveles de precedencia (Docs/SyntaxTree.md § precedencia):
    // || 0, && 1, relacional 2, ! 3, +,- 4, *,/,% 5, unario 6, ^ 7, átomo 8.
    private const int NivelOr = 0;
    private const int NivelAnd = 1;
    private const int NivelRelacional = 2;
    private const int NivelNot = 3;
    private const int NivelSuma = 4;
    private const int NivelProducto = 5;
    private const int NivelUnario = 6;
    private const int NivelPotencia = 7;
    private const int NivelAtomo = 8;

    // Cada operador se envuelve en paréntesis solo si su nivel de precedencia
    // es insuficiente para el contexto donde aparece (paréntesis "inteligentes").
    private static (string Texto, int Nivel) ExpresionInfija(SyntaxNode nodo)
    {
        switch (nodo.Kind)
        {
            case "ε":
                return (string.Empty, NivelAtomo);

            case "MISSING":
                return ("?", NivelAtomo);

            case "EXP": // envoltorio de una expresión completa
                return nodo.Children.Count == 1
                    ? ExpresionInfija(nodo.Children[0])
                    : ("?", NivelAtomo);

            case "EXP_OR":
                return CadenaInfija(nodo, NivelOr, asociativaDerecha: false);

            case "EXP_AND":
                return CadenaInfija(nodo, NivelAnd, asociativaDerecha: false);

            case "EXP_REL":
                return CadenaInfija(nodo, NivelRelacional, asociativaDerecha: false);

            case "EXP_ARIT":
                return CadenaInfija(nodo, NivelSuma, asociativaDerecha: false);

            case "TERMINO":
                return CadenaInfija(nodo, NivelProducto, asociativaDerecha: false);

            case "POTENCIA":
                return CadenaInfija(nodo, NivelPotencia, asociativaDerecha: true);

            case "EXP_NOT": // prefijo: ! operand
                return PrefijoInfija(nodo, NivelNot);

            case "UNARIO": // prefijo: +/- operand
                return PrefijoInfija(nodo, NivelUnario);

            case "FACTOR":
                // Paréntesis del código fuente: la precedencia recalcula cuándo
                // hacen falta, así que el interior se reutiliza directo.
                if (nodo.Children.Count > 1 && nodo.Children[0].Kind == TokenKinds.LeftParen)
                    return ExpresionInfija(nodo.Children[1]);
                // Literal booleano: FACTOR envuelve un nodo BOOL con el token.
                if (nodo.Children.Count > 0 && nodo.Children[0].Kind == "BOOL")
                    return ExpresionInfija(nodo.Children[0]);
                return nodo.Children.Count > 0
                    ? (Lexema(nodo, 0), NivelAtomo)
                    : ("?", NivelAtomo);

            case "BOOL":
                return nodo.Children.Count > 0 ? (Lexema(nodo, 0), NivelAtomo) : ("?", NivelAtomo);

            default:
                if (nodo.Token is not null)
                    return (nodo.Token.Lexeme, NivelAtomo);
                if (nodo.Children.Count > 0)
                    return ExpresionInfija(nodo.Children[0]);
                return (nodo.Kind, NivelAtomo);
        }
    }

    // Nodos binarios en infijo: [operando izquierdo, (operador, operando…)…].
    // Asociatividad izquierda: el derecho exige nivel estrictamente mayor;
    // derecha (^): el izquierdo exige nivel estrictamente mayor.
    private static (string Texto, int Nivel) CadenaInfija(
        SyntaxNode nodo, int nivel, bool asociativaDerecha)
    {
        if (nodo.Children.Count == 0)
            return ("?", NivelAtomo);

        (string izquierdo, int nivelIzquierdo) = ExpresionInfija(nodo.Children[0]);
        int requeridoIzquierdo = asociativaDerecha ? nivel + 1 : nivel;
        int requeridoDerecho = asociativaDerecha ? nivel : nivel + 1;

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

        var partes = new List<string> { Envolver(izquierdo, nivelIzquierdo, requeridoIzquierdo) };
        for (int i = 0; i + 1 < pares.Count; i += 2)
        {
            (string derecho, int nivelDerecho) = ExpresionInfija(pares[i + 1]);
            partes.Add(Lexema(pares[i]));
            partes.Add(Envolver(derecho, nivelDerecho, requeridoDerecho));
        }

        return (string.Join(" ", partes), nivel);
    }

    // Operador prefijo (unario +/- y !): hijo 0 = operador (opcional), último = operando.
    private static (string Texto, int Nivel) PrefijoInfija(SyntaxNode nodo, int nivel)
    {
        (string operando, int nivelOperando) = ExpresionInfija(nodo.Children[^1]);
        string textoOperando = Envolver(operando, nivelOperando, nivel);

        // Sin operador el nodo es un simple passthrough: su nivel de lectura
        // es el del operando (un átomo no necesita paréntesis en ningún sitio).
        if (nodo.Children.Count <= 1)
            return (textoOperando, nivelOperando);

        return ($"{Lexema(nodo, 0)}{textoOperando}", nivel);
    }

    private static string Envolver(string texto, int nivel, int requerido) =>
        nivel < requerido ? $"({texto})" : texto;

    // ---------------------------------------------------------------------------
    // Notación postfija (postorden): Izquierda, Derecha, Raíz. Los operadores
    // van después de sus operandos (acumulador 0 =) y se reutiliza el mismo
    // sistema de variables temporales (t1, t2, …) de la notación infija.
    // ---------------------------------------------------------------------------

    /// <summary>Devuelve la notación postfija de todo el programa, sentencia por línea.</summary>
    public static string Postfija(SyntaxNode? root)
    {
        if (root is null)
            return string.Empty;

        var writer = new StringBuilder();
        SentenciaPostfija(writer, new ContadorTemporales(), root, nivel: 0);
        return writer.ToString();
    }

    private static void SentenciaPostfija(
        StringBuilder writer, ContadorTemporales temporales, SyntaxNode nodo, int nivel)
    {
        switch (nodo.Kind)
        {
            case "S":
            case "INICIO":
            case "INS":
                foreach (SyntaxNode hijo in nodo.Children)
                    SentenciaPostfija(writer, temporales, hijo, nivel);
                break;

            case "ε":
                break; // cuerpo vacío: no produce línea

            case "ASSIGN": // ID OPA EXP => "id <expresión> ="
                Linea(writer, nivel, $"{Lexema(nodo, 0)} {TextoPostfijo(nodo, 2)} =");
                break;

            case "WRITE": // => "<expresión> write"
                Linea(writer, nivel, $"{TextoPostfijo(nodo, 1)} write");
                break;

            case "READ": // => "id read"
                Linea(writer, nivel, $"{Lexema(nodo, 1)} read");
                break;

            case "IF":
                {
                    int t = CondicionPostfija(writer, temporales, nodo, nivel);
                    Linea(writer, nivel, $"if Condición (t{t}) then");
                    SentenciaPostfija(writer, temporales, nodo.Children[3], nivel + 1);
                    SentenciaPostfija(writer, temporales, nodo.Children[4], nivel);
                    break;
                }

            case "IF_T": // endif | else INS endif
                if (nodo.Children.Count > 1)
                {
                    Linea(writer, nivel, "else");
                    SentenciaPostfija(writer, temporales, nodo.Children[1], nivel + 1);
                }
                Linea(writer, nivel, "endif");
                break;

            case "WHILE":
                {
                    int t = CondicionPostfija(writer, temporales, nodo, nivel);
                    Linea(writer, nivel, $"while Condición (t{t}) do");
                    SentenciaPostfija(writer, temporales, nodo.Children[3], nivel + 1);
                    Linea(writer, nivel, "endwhile");
                    break;
                }

            case "FOR": // => "for i from tN to tM do"
                {
                    int limiteInicial = temporales.Siguiente();
                    Linea(writer, nivel, $"Límite inicial (t{limiteInicial}) {TextoPostfijo(nodo, 3)} =");
                    int limiteFinal = temporales.Siguiente();
                    Linea(writer, nivel, $"Límite final (t{limiteFinal}) {TextoPostfijo(nodo, 5)} =");
                    Linea(writer, nivel,
                        $"for {Lexema(nodo, 1)} from t{limiteInicial} to t{limiteFinal} do");
                    SentenciaPostfija(writer, temporales, nodo.Children[7], nivel + 1);
                    Linea(writer, nivel, "endfor");
                    break;
                }

            case "DO": // do INS end while EXP
                Linea(writer, nivel, "do");
                SentenciaPostfija(writer, temporales, nodo.Children[1], nivel + 1);
                {
                    int t = temporales.Siguiente();
                    Linea(writer, nivel, $"Condición (t{t}) {TextoPostfijo(nodo, 4)} =");
                    Linea(writer, nivel, $"end while Condición (t{t})");
                }
                break;

            case "RAND": // => "<expresión> id rand"
                Linea(writer, nivel, $"{TextoPostfijo(nodo, 3)} {Lexema(nodo, 1)} rand");
                break;

            case "MOVE": // => "<exp1> <exp2> move"
                Linea(writer, nivel, $"{TextoPostfijo(nodo, 1)} {TextoPostfijo(nodo, 3)} move");
                break;

            case "BREAK":
                Linea(writer, nivel, "break");
                break;

            case "CLEAR":
                Linea(writer, nivel, "clear");
                break;

            case "ERROR": // tokens consumidos por la recuperación de errores
                Linea(writer, nivel, ErrorTexto(nodo));
                break;

            default:
                if (nodo.Token is not null) // hojas sueltas (recuperación)
                    Linea(writer, nivel, nodo.Token.Lexeme);
                break;
        }
    }

    // Emite la línea "Condición (tN) <expresión> =" y devuelve N.
    private static int CondicionPostfija(
        StringBuilder writer, ContadorTemporales temporales, SyntaxNode nodo, int nivel)
    {
        int t = temporales.Siguiente();
        Linea(writer, nivel, $"Condición (t{t}) {TextoPostfijo(nodo, 1)} =");
        return t;
    }

    private static string TextoPostfijo(SyntaxNode padre, int indice) =>
        indice < padre.Children.Count ? ExpresionPostfija(padre.Children[indice]) : "?";

    // Postorden de la cadena binaria: primero el operando izquierdo, luego los
    // pares (operando, operador) en orden, de modo que el último operador de
    // la cadena (la raíz real) se emite al final.
    private static string ExpresionPostfija(SyntaxNode nodo)
    {
        switch (nodo.Kind)
        {
            case "ε":
                return string.Empty;

            case "MISSING":
                return "?";

            case "EXP": // envoltorio de una expresión completa
                return nodo.Children.Count == 1 ? ExpresionPostfija(nodo.Children[0]) : "?";

            case "EXP_OR":
            case "EXP_AND":
            case "EXP_ARIT":
            case "TERMINO":
            case "POTENCIA":
            case "EXP_REL":
                return CadenaPostfija(nodo);

            case "EXP_NOT": // postorden: el operador va después del operando
            case "UNARIO":
                {
                    string operando = ExpresionPostfija(nodo.Children[^1]);
                    if (nodo.Children.Count <= 1)
                        return operando; // passthrough sin operador
                    return $"{operando} {Lexema(nodo, 0)}";
                }

            case "FACTOR":
                // Paréntesis del código fuente: en postfijo no hacen falta.
                if (nodo.Children.Count > 1 && nodo.Children[0].Kind == TokenKinds.LeftParen)
                    return ExpresionPostfija(nodo.Children[1]);
                // Literal booleano: FACTOR envuelve un nodo BOOL con el token.
                if (nodo.Children.Count > 0 && nodo.Children[0].Kind == "BOOL")
                    return ExpresionPostfija(nodo.Children[0]);
                return nodo.Children.Count > 0 ? Lexema(nodo, 0) : "?";

            case "BOOL":
                return nodo.Children.Count > 0 ? Lexema(nodo, 0) : "?";

            default:
                return nodo.Token is not null ? nodo.Token.Lexeme
                    : nodo.Children.Count > 0 ? ExpresionPostfija(nodo.Children[0])
                    : nodo.Kind;
        }
    }

    // Nodos binarios: [operando izquierdo, (operador, operando derecho)…].
    // Postorden: izquierdo, y luego cada (operando derecha, operador).
    private static string CadenaPostfija(SyntaxNode nodo)
    {
        if (nodo.Children.Count == 0)
            return "?";

        var partes = new List<string> { ExpresionPostfija(nodo.Children[0]) };

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
            partes.Add(ExpresionPostfija(pares[i + 1]));
            partes.Add(Lexema(pares[i]));
        }

        return string.Join(" ", partes);
    }

    // Preorden de la cadena de expresiones: el operador principal (último de la
    // cadena, asociatividad izquierda) se emite primero, luego su operando
    // izquierdo y después el derecho.
    private static string Expresion(SyntaxNode nodo)
    {
        switch (nodo.Kind)
        {
            case "ε":
                return string.Empty;

            case "MISSING":
                return "?";

            case "EXP": // envoltorio de una expresión completa
                return nodo.Children.Count == 1 ? Expresion(nodo.Children[0]) : "?";

            case "EXP_OR":
            case "EXP_AND":
            case "EXP_ARIT":
            case "TERMINO":
            case "POTENCIA":
            case "EXP_REL":
                return CadenaBinaria(nodo);

            case "EXP_NOT":
            case "UNARIO":
                {
                    string operador = nodo.Children.Count > 1 ? Lexema(nodo, 0) : string.Empty;
                    string operando = Expresion(nodo.Children[^1]);
                    return operador.Length == 0 ? operando : $"{operador} {operando}";
                }

            case "FACTOR":
                // Paréntesis: la notación prefija no los necesita.
                if (nodo.Children.Count > 1 && nodo.Children[0].Kind == TokenKinds.LeftParen)
                    return Expresion(nodo.Children[1]);
                // Literal booleano: FACTOR envuelve un nodo BOOL con el token.
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
    // EXP_OR/EXP_AND guardan los pares directamente; EXP_ARIT/TERMINO/
    // POTENCIA/EXP_REL los delegan a su nodo cola *_T (ε si no hay operador).
    private static string CadenaBinaria(SyntaxNode nodo)
    {
        if (nodo.Children.Count == 0)
            return "?";

        SyntaxNode izquierdo = nodo.Children[0];

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
        return PrefijaCadena(izquierdo, pares);
    }

    // (...((izq op1 der1) op2 der2) … opN derN) en preorden:
    // opN, luego todo lo anterior, y al final derN.
    private static string PrefijaCadena(SyntaxNode izquierdo, IReadOnlyList<SyntaxNode> pares)
    {
        int paresValidos = (pares.Count / 2) * 2; // ignora ε o restos impares
        if (paresValidos == 0)
            return Expresion(izquierdo);

        SyntaxNode operador = pares[paresValidos - 2];
        SyntaxNode operando = pares[paresValidos - 1];

        var subcadena = new List<SyntaxNode>(paresValidos - 2);
        for (int i = 0; i < paresValidos - 2; i++)
            subcadena.Add(pares[i]);

        return $"{Lexema(operador)} {PrefijaCadena(izquierdo, subcadena)} {Expresion(operando)}";
    }

    private static void Linea(StringBuilder writer, int nivel, string texto)
    {
        if (writer.Length > 0)
            writer.AppendLine();
        writer.Append(new string(' ', nivel * 2));
        writer.Append(texto);
    }

    private static string Lexema(SyntaxNode padre, int indice) =>
        indice < padre.Children.Count ? Lexema(padre.Children[indice]) : "?";

    private static string Lexema(SyntaxNode nodo) =>
        nodo.Token?.Lexeme ?? (nodo.Kind == "MISSING" ? "?" : nodo.Kind);

    private static string ErrorTexto(SyntaxNode nodo) =>
        nodo.Children.Count == 0
            ? "<error>"
            : $"<error {string.Join(" ", nodo.Children.Select(child => $"'{Lexema(child)}'"))}>";
}
