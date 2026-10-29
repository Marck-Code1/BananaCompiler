using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

public sealed class TriplosGeneratorTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(
            Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    [Fact]
    public void TodosLosProgramasValidosGeneranTriplosConIndicesContinuos()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        foreach (string sample in samples)
        {
            ParseResult result = Parse(File.ReadAllText(sample));
            Assert.True(result.Success, $"Falló el parser base en {Path.GetFileName(sample)}.");

            List<Triplo> triplos = TriplosGenerator.Generar(result.SyntaxTree);
            Assert.Equal(Enumerable.Range(0, triplos.Count), triplos.Select(triplo => triplo.Indice));
        }
    }

    [Fact]
    public void ExpresionesRespetanPrecedenciaYReferencianElTriploQueLasCalcula()
    {
        ParseResult result = Parse("start\nx = 2 + 3 * 4\nend");
        Assert.True(result.Success);

        List<Triplo> triplos = TriplosGenerator.Generar(result.SyntaxTree);

        Assert.Collection(triplos,
            triplo => Assert.Equal((0, "*", "3", "4"),
                (triplo.Indice, triplo.Operador, triplo.Arg1, triplo.Arg2)),
            triplo => Assert.Equal((1, "+", "2", "(0)"),
                (triplo.Indice, triplo.Operador, triplo.Arg1, triplo.Arg2)),
            triplo => Assert.Equal((2, "=", "(1)", "x"),
                (triplo.Indice, triplo.Operador, triplo.Arg1, triplo.Arg2)));
    }

    [Fact]
    public void IfYWhileUsanIndicesDeTriplosComoDestinos()
    {
        ParseResult result = Parse("start\nx = 0\nif x < 1 then\nwrite x\nendif\n" +
            "while x < 2 do\nx = x + 1\nendwhile\nend");
        Assert.True(result.Success);

        List<Triplo> triplos = TriplosGenerator.Generar(result.SyntaxTree);
        foreach (Triplo salto in triplos.Where(triplo => triplo.Operador is "ifFalso" or "ifTrue" or "goto"))
        {
            string destino = salto.Operador == "goto" ? salto.Arg1 : salto.Arg2;
            Assert.True(int.TryParse(destino, out int indice), $"Destino no numérico en triplo {salto.Indice}.");
            Assert.InRange(indice, 0, triplos.Count - 1);
            Assert.Equal("Label", triplos[indice].Operador);
        }
    }

    [Fact]
    public void GenerarNoModificaElArbol()
    {
        ParseResult result = Parse("start\nx = -(2 + 3)\nend");
        Assert.True(result.Success);
        string antes = result.SyntaxTree!.ToCompact();

        _ = TriplosGenerator.Generar(result.SyntaxTree);

        Assert.Equal(antes, result.SyntaxTree.ToCompact());
    }
}
