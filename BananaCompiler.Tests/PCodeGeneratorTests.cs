using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class PCodeGeneratorTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(
            Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    private static List<string> Generar(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return PCodeGenerator.Generar(result.SyntaxTree!);
    }

    [Fact]
    public void TodosLosProgramasValidosGeneranCodigoP()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            List<string> codigo = Generar(File.ReadAllText(sample));
            Assert.NotNull(codigo);
        }
    }

    [Fact]
    public void AritmeticaSeEmiteEnPostordenRespetandoPrecedencia()
    {
        Assert.Equal(
            ["LDC 2", "LDC 3", "LDC 4", "MUL", "ADD", "STO x"],
            Generar("start\nx = 2 + 3 * 4\nend"));
    }

    [Fact]
    public void IfEmiteSaltoCondicionalYEtiquetaDeclarada()
    {
        List<string> codigo = Generar("start\nx = 1\nif x < 2 then\nwrite x\nendif\nend");

        Assert.Contains("LOD x", codigo);
        Assert.Contains("LDC 2", codigo);
        Assert.Contains("LES", codigo);
        Assert.Contains("JPC L1", codigo);
        Assert.Contains("LAB L1:", codigo);
    }

    [Fact]
    public void TodosLosSaltosTienenUnaEtiquetaDeclarada()
    {
        List<string> codigo = Generar("start\nx = 0\nif x < 1 then\nx = x + 1 else\n" +
            "while x < 3 do\nx = x + 1\nendwhile\nendif\n" +
            "for i from 1 to 2 do\nwrite i\nendfor\nend");
        HashSet<string> etiquetas = codigo
            .Where(linea => linea.StartsWith("LAB ", StringComparison.Ordinal))
            .Select(linea => linea[4..^1])
            .ToHashSet(StringComparer.Ordinal);

        foreach (string salto in codigo.Where(linea => linea.StartsWith("JMP ", StringComparison.Ordinal) ||
                     linea.StartsWith("JPC ", StringComparison.Ordinal)))
            Assert.Contains(salto[4..], etiquetas);
    }

    [Fact]
    public void BooleanosYOperadoresLogicosSeTraducenSinOpCodesAdicionales()
    {
        List<string> codigo = Generar("start\na = true\nb = false\nc = a && !b || false\nend");

        Assert.Contains("LDC 1", codigo);
        Assert.Contains("LDC 0", codigo);
        Assert.Contains(codigo, linea => linea.StartsWith("JPC ", StringComparison.Ordinal));
        Assert.DoesNotContain(codigo, linea => linea is "AND" or "OR" or "NOT");
        Assert.Equal("STO c", codigo[^1]);
    }

    [Fact]
    public void GenerarNoModificaElArbol()
    {
        ParseResult result = Parse("start\nx = -(2 + 3)\nend");
        Assert.True(result.Success);
        string antes = result.SyntaxTree!.ToCompact();

        _ = PCodeGenerator.Generar(result.SyntaxTree);

        Assert.Equal(antes, result.SyntaxTree.ToCompact());
    }
}
