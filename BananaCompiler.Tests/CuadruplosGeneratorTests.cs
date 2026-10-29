using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

/// <summary>
/// Valida que la secuencia de cuádruplos y el código 3D (CuadruplosGenerator)
/// se derivan del AST validados sin modificarlo: temporales T1… en orden,
/// etiquetas L1… consistentes (toda etiqueta referenciada está definida y no
/// quedan etiquetas muertas) y formato académico clásico (Op, Arg1, Arg2,
/// Resultado) con nombres de variables, nunca direcciones de memoria.
/// </summary>
public sealed class CuadruplosGeneratorTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    private static IReadOnlyList<Cuadruplo> Cuadruplos(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa base debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return CuadruplosGenerator.Generar(result.SyntaxTree);
    }

    private static string Codigo3D(string code) =>
        CuadruplosGenerator.Codigo3D(Cuadruplos(code));

    private static string Lineas(params string[] lineas) =>
        string.Join(Environment.NewLine, lineas);

    [Fact]
    public void Cuadruplos_TodosLosProgramasValidosGeneranSalidaConsistente()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            string nombre = Path.GetFileName(sample);
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló {nombre} en el parser base.");

            IReadOnlyList<Cuadruplo> cuadruplos = CuadruplosGenerator.Generar(result.SyntaxTree);
            string codigo = CuadruplosGenerator.Codigo3D(cuadruplos);

            // Toda etiqueta referenciada por un salto está definida y, a la
            // inversa, no se emiten etiquetas muertas (p. ej. la salida de un
            // do…while sin break se omite por completo).
            HashSet<string> referenciadas = cuadruplos
                .Where(quad => quad.Op is "goto" or "ifFalse" or "ifTrue")
                .Select(quad => quad.Resultado)
                .ToHashSet();
            HashSet<string> definidas = cuadruplos
                .Where(quad => quad.Op == "label")
                .Select(quad => quad.Resultado)
                .ToHashSet();
            Assert.Equal(referenciadas, definidas);

            // Sin marcadores de error ni operandos desconocidos: el árbol es válido.
            Assert.DoesNotContain(cuadruplos,
                quad => quad.Arg1 == "?" || quad.Arg2 == "?" || quad.Resultado == "?");
            Assert.DoesNotContain("<error", codigo);
            Assert.DoesNotContain("ε", codigo);

            // El código 3D es una instrucción por línea, sin líneas en blanco.
            string[] lineas = codigo.Length == 0 ? [] : codigo.Split(Environment.NewLine);
            Assert.Equal(cuadruplos.Count, lineas.Length);
            Assert.All(lineas, linea => Assert.False(string.IsNullOrWhiteSpace(linea)));

            // Solo el programa vacío (start…end sin sentencias) no genera código.
            if (nombre != "EmptyProgram.banana")
                Assert.NotEmpty(cuadruplos);
        }
    }

    [Fact]
    public void Cuadruplos_AsignacionAritmeticaUsaTemporalesEnOrden()
    {
        // 15 + 15 * 2 - 10 => T1 = 15 * 2, T2 = 15 + T1, T3 = T2 - 10.
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "5", "", "x"),
            new Cuadruplo("*", "15", "2", "T1"),
            new Cuadruplo("+", "15", "T1", "T2"),
            new Cuadruplo("-", "T2", "10", "T3"),
            new Cuadruplo("=", "T3", "", "sum"),
        }, Cuadruplos("start\nx = 5\nsum = 15 + 15 * 2 - 10\nend"));

        Assert.Equal(Lineas(
            "x = 5",
            "T1 = 15 * 2",
            "T2 = 15 + T1",
            "T3 = T2 - 10",
            "sum = T3"),
            Codigo3D("start\nx = 5\nsum = 15 + 15 * 2 - 10\nend"));
    }

    [Fact]
    public void Codigo3D_UnariosYNegacionVanAntesDelOperando()
    {
        // -x ^ 2 => T1 = x ^ 2, T2 = -T1 (nunca "T2 = T1 - _").
        Assert.Equal(Lineas(
            "x = 5",
            "T1 = x ^ 2",
            "T2 = -T1",
            "z = T2",
            "ok = true"),
            Codigo3D("start\nx = 5\nz = -x ^ 2\nok = true\nend"));

        string negado = Codigo3D("start\nx = 5\nif !(x < 10) then\n  write x\nendif\nend");
        Assert.Contains(Environment.NewLine + "T2 = !T1" + Environment.NewLine, negado);
        Assert.DoesNotContain("0x", negado);
    }

    [Fact]
    public void Cuadruplos_IfConElseGeneraSaltoYEtiquetas()
    {
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "1", "", "x"),
            new Cuadruplo("<", "x", "2", "T1"),
            new Cuadruplo("ifFalse", "T1", "_", "L1"),
            new Cuadruplo("write", "x", "_", "_"),
            new Cuadruplo("goto", "_", "_", "L2"),
            new Cuadruplo("label", "_", "_", "L1"),
            new Cuadruplo("write", "0", "_", "_"),
            new Cuadruplo("label", "_", "_", "L2"),
        }, Cuadruplos("start\nx = 1\nif x < 2 then\n  write x\nelse\n  write 0\nendif\nend"));

        Assert.Equal(Lineas(
            "x = 1",
            "T1 = x < 2",
            "ifFalse T1 goto L1",
            "write x",
            "goto L2",
            "L1:",
            "write 0",
            "L2:"),
            Codigo3D("start\nx = 1\nif x < 2 then\n  write x\nelse\n  write 0\nendif\nend"));
    }

    [Fact]
    public void Cuadruplos_WhileVuelveALaCondicionYSaltaAlFin()
    {
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "3", "", "x"),
            new Cuadruplo("label", "_", "_", "L1"),
            new Cuadruplo(">", "x", "0", "T1"),
            new Cuadruplo("ifFalse", "T1", "_", "L2"),
            new Cuadruplo("-", "x", "1", "T2"),
            new Cuadruplo("=", "T2", "", "x"),
            new Cuadruplo("goto", "_", "_", "L1"),
            new Cuadruplo("label", "_", "_", "L2"),
        }, Cuadruplos("start\nx = 3\nwhile x > 0 do\n  x = x - 1\nendwhile\nend"));

        string codigo = Codigo3D("start\nx = 3\nwhile x > 0 do\n  x = x - 1\nendwhile\nend");
        Assert.Contains("L1:" + Environment.NewLine, codigo);
        Assert.Contains("ifFalse T1 goto L2", codigo);
        Assert.EndsWith("L2:", codigo);
    }

    [Fact]
    public void Cuadruplos_ForSeExpandeComoCicloConIncremento()
    {
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "1", "", "i"),
            new Cuadruplo("label", "_", "_", "L1"),
            new Cuadruplo(">", "i", "3", "T1"),
            new Cuadruplo("ifTrue", "T1", "_", "L2"),
            new Cuadruplo("write", "i", "_", "_"),
            new Cuadruplo("+", "i", "1", "T2"),
            new Cuadruplo("=", "T2", "", "i"),
            new Cuadruplo("goto", "_", "_", "L1"),
            new Cuadruplo("label", "_", "_", "L2"),
        }, Cuadruplos("start\nfor i from 1 to 3 do\n  write i\nendfor\nend"));

        string codigo = Codigo3D("start\nfor i from 1 to 3 do\n  write i\nendfor\nend");
        Assert.Contains("T1 = i > 3", codigo);
        Assert.Contains("ifTrue T1 goto L2", codigo);
        Assert.Contains("T2 = i + 1", codigo);
    }

    [Fact]
    public void Cuadruplos_DoWhileRepiteYOmiteLaEtiquetaSinUso()
    {
        // El ciclo salta al inicio con ifTrue; la etiqueta de salida no se
        // referencia (no hay break), así que no se emite: ni referenciada ni
        // definida. Ambos conjuntos de etiquetas coinciden.
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "0", "", "x"),
            new Cuadruplo("label", "_", "_", "L1"),
            new Cuadruplo("+", "x", "1", "T1"),
            new Cuadruplo("=", "T1", "", "x"),
            new Cuadruplo("<", "x", "3", "T2"),
            new Cuadruplo("ifTrue", "T2", "_", "L1"),
        }, Cuadruplos("start\nx = 0\ndo\n  x = x + 1\nend while x < 3\nend"));

        string codigo = Codigo3D("start\nx = 0\ndo\n  x = x + 1\nend while x < 3\nend");
        Assert.Contains("ifTrue T2 goto L1", codigo);
        Assert.DoesNotContain("L2", codigo);
    }

    [Fact]
    public void Cuadruplos_BreakSaltaAlFinDelCicloMasExterno()
    {
        Assert.Equal(new[]
        {
            new Cuadruplo("=", "0", "", "x"),
            new Cuadruplo("label", "_", "_", "L1"),
            new Cuadruplo("<", "x", "10", "T1"),
            new Cuadruplo("ifFalse", "T1", "_", "L2"),
            new Cuadruplo("+", "x", "1", "T2"),
            new Cuadruplo("=", "T2", "", "x"),
            new Cuadruplo(">", "x", "5", "T3"),
            new Cuadruplo("ifFalse", "T3", "_", "L3"),
            new Cuadruplo("goto", "_", "_", "L2"),
            new Cuadruplo("label", "_", "_", "L3"),
            new Cuadruplo("goto", "_", "_", "L1"),
            new Cuadruplo("label", "_", "_", "L2"),
        }, Cuadruplos(
            "start\nx = 0\nwhile x < 10 do\n  x = x + 1\n  if x > 5 then\n    break\n" +
            "  endif\nendwhile\nend"));
    }

    [Fact]
    public void Cuadruplos_InstruccionesDeESGeneranCuadruplosDirectos()
    {
        // Todas las sentencias del lenguaje en un solo programa.
        string programa = "start\nx = 0\nrand x, 6\nmove x, 1\nread x\nwrite x\nclear\nend";

        Assert.Equal(new[]
        {
            new Cuadruplo("=", "0", "", "x"),
            new Cuadruplo("rand", "6", "_", "x"),
            new Cuadruplo("move", "x", "1", "_"),
            new Cuadruplo("read", "_", "_", "x"),
            new Cuadruplo("write", "x", "_", "_"),
            new Cuadruplo("clear", "_", "_", "_"),
        }, Cuadruplos(programa));

        Assert.Equal(Lineas(
            "x = 0",
            "x = rand(6)",
            "move x, 1",
            "read x",
            "write x",
            "clear"),
            Codigo3D(programa));
    }
}
