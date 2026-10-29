using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

/// <summary>
/// Valida que la notación prefija (NotacionesGenerator) respeta la sintaxis y
/// la semántica del proyecto base: se genera únicamente desde árboles ya
/// validados por el parser y jamás altera sus diagnósticos.
/// </summary>
public sealed class NotacionesGeneratorTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    private static string Notacion(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa base debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return NotacionesGenerator.Prefija(result.SyntaxTree);
    }

    private static string NotacionInfija(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa base debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return NotacionesGenerator.Infija(result.SyntaxTree);
    }

    private static string NotacionPostfija(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa base debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return NotacionesGenerator.Postfija(result.SyntaxTree);
    }

    private static string Lineas(params string[] lineas) =>
        string.Join(Environment.NewLine, lineas);

    [Fact]
    public void TodosLosProgramasValidosGeneranNotacionLimpia()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            string nombre = Path.GetFileName(sample);
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló {nombre} en el parser base.");

            string notacion = NotacionesGenerator.Prefija(result.SyntaxTree);
            Assert.False(string.IsNullOrWhiteSpace(notacion), $"Notación vacía en {nombre}.");
            Assert.Equal("start", notacion.Split(Environment.NewLine)[0]);
            Assert.Equal("end", notacion.Split(Environment.NewLine)[^1]);
            Assert.DoesNotContain("<error", notacion);
            Assert.DoesNotContain("ε", notacion);
            Assert.DoesNotContain("?", notacion);
        }
    }

    [Fact]
    public void CadaSentenciaOcupaUnaLinea()
    {
        Assert.Equal(Lineas(
            "start",
            "acumulador = 0",
            "i = 1",
            "end"),
            Notacion("start\nacumulador = 0\ni = 1\nend"));
    }

    [Fact]
    public void PrecedenciaYAsociatividadDelOperadorAritmetico()
    {
        // 15 + 15 * 2 - 10 => ((15 + (15 * 2)) - 10) en preorden.
        Assert.Equal(Lineas(
            "start",
            "total = - + 15 * 15 2 10",
            "end"),
            Notacion("start\ntotal = 15 + 15 * 2 - 10\nend"));
    }

    [Fact]
    public void SumaEncadenadaAsociativaALaIzquierda()
    {
        // 15 + 15 + 15 - 20 => (((15 + 15) + 15) - 20) en preorden: la raíz
        // es el último operador '-'.
        string[] lineas = Notacion("start\nsum = 15 + 15 + 15 - 20\nwrite sum\nend")
            .Split(Environment.NewLine);
        Assert.Equal("sum = - + + 15 15 15 20", lineas[1]);
        Assert.Equal("write sum", lineas[2]);
    }

    [Fact]
    public void NegacionYParentesisSeEliminanEnPrefija()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 0",
            "if ! < x 10 then",
            "  write x",
            "endif",
            "end"),
            Notacion("start\nx = 0\nif !(x < 10) then\n  write x\nendif\nend"));
    }

    [Fact]
    public void ComparacionLogicaConIdentificadores()
    {
        Assert.Equal(Lineas(
            "start",
            "asd = true",
            "x = 0",
            "if && asd < x 10 then",
            "  write x",
            "endif",
            "end"),
            Notacion("start\nasd = true\nx = 0\nif asd && (x < 10) then\n  write x\nendif\nend"));
    }

    [Fact]
    public void CicloForConCuerpoIndentado()
    {
        Assert.Equal(Lineas(
            "start",
            "total = 0",
            "for i from 1 to 5 do",
            "  total = + total i",
            "endfor",
            "write total",
            "end"),
            Notacion("start\ntotal = 0\nfor i from 1 to 5 do\n  total = total + i\nendfor\nwrite total\nend"));
    }

    [Fact]
    public void DoWhileColocaLaCondicionAlFinal()
    {
        Assert.Equal(Lineas(
            "start",
            "sum = 0",
            "count = 1",
            "value = 0",
            "do",
            "  read value",
            "  sum = + sum value",
            "  count = + count 1",
            "end while < count 10",
            "write sum",
            "end"),
            Notacion("start\nsum = 0\ncount = 1\nvalue = 0\ndo\n  read value\n" +
                "  sum = sum + value\n  count = count + 1\nend while count < 10\nwrite sum\nend"));
    }

    [Fact]
    public void LiteralesBooleanosSeEscribenComoLexema()
    {
        Assert.Equal(Lineas(
            "start",
            "if == true false then",
            "endif",
            "end"),
            Notacion("start\nif true == false then\nendif\nend"));
    }

    [Fact]
    public void WhileVacioNoGeneraLineasDeCuerpo()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 0",
            "while < x 10 do",
            "endwhile",
            "end"),
            Notacion("start\nx = 0\nwhile x < 10 do\nendwhile\nend"));
    }

    [Fact]
    public void ProgramaVacioGeneraSoloStartYEnd()
    {
        Assert.Equal(Lineas("start", "end"), Notacion("start\nend"));
    }

    [Fact]
    public void UnarioYPotenciaSeEmitenEnPreorden()
    {
        // -x ^ 2 => -(x ^ 2) en preorden.
        Assert.Equal(Lineas(
            "start",
            "x = 5",
            "z = - ^ x 2",
            "end"),
            Notacion("start\nx = 5\nz = -x ^ 2\nend"));
    }

    [Fact]
    public void TodasLasInstruccionesSeRepresentanSinErrores()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 0",
            "rand x, 5",
            "move x, 1",
            "do",
            "  read x",
            "  clear",
            "  if > x 0 then",
            "    write x",
            "    break",
            "  endif",
            "end while < x 10",
            "end"),
            Notacion("start\nx = 0\nrand x, 5\nmove x, 1\ndo\n  read x\n  clear\n" +
                "  if x > 0 then\n    write x\n    break\n  endif\nend while x < 10\nend"));
    }

    [Fact]
    public void AnidarBloquesRespetaLaIndentacionPorNivel()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 0",
            "y = 0",
            "do",
            "  while < x 10 do",
            "    clear",
            "  endwhile",
            "end while < y 10",
            "end"),
            Notacion("start\nx = 0\ny = 0\ndo\n  while x < 10 do\n    clear\n  endwhile\nend while y < 10\nend"));
    }

    [Fact]
    public void PegarNotacionComoFuenteProduceLosErroresDelParserBase()
    {
        // Escenario real: el usuario pegó la salida prefija como si fuera
        // código fuente. El parser base debe seguir detectando los errores y
        // la notación los muestra como bloques <error> sin lanzar excepciones.
        ParseResult result = Parse("start\ntotal = - + 15 * 15 2 10\nend");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);

        string notacion = NotacionesGenerator.Prefija(result.SyntaxTree);
        Assert.Contains("<error", notacion);
    }

    [Fact]
    public void Infija_TodosLosProgramasValidosGeneranNotacionLimpia()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            string nombre = Path.GetFileName(sample);
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló {nombre} en el parser base.");

            string notacion = NotacionesGenerator.Infija(result.SyntaxTree);
            Assert.False(string.IsNullOrWhiteSpace(notacion), $"Notación vacía en {nombre}.");
            Assert.Equal("start", notacion.Split(Environment.NewLine)[0]);
            Assert.Equal("end", notacion.Split(Environment.NewLine)[^1]);
            Assert.DoesNotContain("<error", notacion);
            Assert.DoesNotContain("ε", notacion);
            Assert.DoesNotContain("?", notacion);
        }
    }

    [Fact]
    public void Infija_ParéntesisInteligentesSegúnPrecedencia()
    {
        Assert.Equal(Lineas(
            "start",
            "a = 1",
            "b = 2",
            "c = 3",
            "d = (a + b) * c",
            "e = a + b * c",
            "ok = true",
            "no = false",
            "Condición (t1) = (ok || no) && a < b",
            "if Condición (t1) then",
            "  write d",
            "endif",
            "end"),
            NotacionInfija("start\na = 1\nb = 2\nc = 3\nd = (a + b) * c\ne = a + b * c\n" +
                "ok = true\nno = false\nif (ok || no) && a < b then\n  write d\nendif\nend"));
    }

    [Fact]
    public void Infija_NegacionYUnarioRespetanLaAgrupación()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 5",
            "z = -x ^ 2",
            "w = -(x + 1)",
            "Condición (t1) = !(x < 10)",
            "if Condición (t1) then",
            "  write z",
            "endif",
            "end"),
            NotacionInfija("start\nx = 5\nz = -x ^ 2\nw = -(x + 1)\n" +
                "if !(x < 10) then\n  write z\nendif\nend"));
    }

    [Fact]
    public void Infija_CondicionesUsanVariablesTemporalesEnOrden()
    {
        Assert.Equal(Lineas(
            "start",
            "x = 0",
            "Condición (t1) = x < 10",
            "if Condición (t1) then",
            "  write x",
            "endif",
            "Condición (t2) = x > 0",
            "while Condición (t2) do",
            "  x = x - 1",
            "endwhile",
            "do",
            "  x = x + 1",
            "Condición (t3) = x < 5",
            "end while Condición (t3)",
            "Límite inicial (t4) = 1",
            "Límite final (t5) = 3",
            "for i from t4 to t5 do",
            "  write i",
            "endfor",
            "end"),
            NotacionInfija("start\nx = 0\nif x < 10 then\n  write x\nendif\n" +
                "while x > 0 do\n  x = x - 1\nendwhile\n" +
                "do\n  x = x + 1\nend while x < 5\n" +
                "for i from 1 to 3 do\n  write i\nendfor\nend"));
    }

    [Fact]
    public void Infija_ExpresionesAritmeticasSeLeenComoElCodigoFuente()
    {
        string[] lineas = NotacionInfija("start\nx = 5\nsum = 15 + 15 * 2 - 10\nz = -x ^ 2\nend")
            .Split(Environment.NewLine);
        Assert.Equal("sum = 15 + 15 * 2 - 10", lineas[2]);
        Assert.Equal("z = -x ^ 2", lineas[3]);
    }

    [Fact]
    public void Postfija_TodosLosProgramasValidosGeneranNotacionLimpia()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            string nombre = Path.GetFileName(sample);
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló {nombre} en el parser base.");

            string notacion = NotacionesGenerator.Postfija(result.SyntaxTree);
            Assert.False(string.IsNullOrWhiteSpace(notacion), $"Notación vacía en {nombre}.");
            Assert.Equal("start", notacion.Split(Environment.NewLine)[0]);
            Assert.Equal("end", notacion.Split(Environment.NewLine)[^1]);
            Assert.DoesNotContain("<error", notacion);
            Assert.DoesNotContain("ε", notacion);
            Assert.DoesNotContain("?", notacion);
        }
    }

    [Fact]
    public void Postfija_LasAsignacionesPonenElIgualAlFinal()
    {
        Assert.Equal(Lineas(
            "start",
            "x 5 =",
            "sum 15 15 2 * + 10 - =",
            "end"),
            NotacionPostfija("start\nx = 5\nsum = 15 + 15 * 2 - 10\nend"));
    }

    [Fact]
    public void Postfija_NegacionYUnarioVanDespuesDelOperando()
    {
        Assert.Equal(Lineas(
            "start",
            "x 5 =",
            "z x 2 ^ - =",
            "Condición (t1) x 10 < ! =",
            "if Condición (t1) then",
            "  z write",
            "endif",
            "end"),
            NotacionPostfija("start\nx = 5\nz = -x ^ 2\nif !(x < 10) then\n  write z\nendif\nend"));
    }

    [Fact]
    public void Postfija_CondicionesUsanVariablesTemporalesEnOrden()
    {
        Assert.Equal(Lineas(
            "start",
            "x 0 =",
            "Condición (t1) x 10 < =",
            "if Condición (t1) then",
            "  x write",
            "endif",
            "Condición (t2) x 0 > =",
            "while Condición (t2) do",
            "  x x 1 - =",
            "endwhile",
            "do",
            "  x x 1 + =",
            "Condición (t3) x 5 < =",
            "end while Condición (t3)",
            "Límite inicial (t4) 1 =",
            "Límite final (t5) 3 =",
            "for i from t4 to t5 do",
            "  i write",
            "endfor",
            "end"),
            NotacionPostfija("start\nx = 0\nif x < 10 then\n  write x\nendif\n" +
                "while x > 0 do\n  x = x - 1\nendwhile\n" +
                "do\n  x = x + 1\nend while x < 5\n" +
                "for i from 1 to 3 do\n  write i\nendfor\nend"));
    }

    [Fact]
    public void Postfija_LasInstruccionesMuevenElOperadorAlFinal()
    {
        Assert.Equal(Lineas(
            "start",
            "x 0 =",
            "6 x rand",
            "x 1 move",
            "x read",
            "x write",
            "end"),
            NotacionPostfija("start\nx = 0\nrand x, 6\nmove x, 1\nread x\nwrite x\nend"));
    }
}
