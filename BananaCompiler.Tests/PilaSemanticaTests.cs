using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using Xunit;

namespace BananaCompiler.Tests;

/// <summary>
/// Valida el contrato de la traza de la pila semántica (SemanticPile ->
/// ParseResult.SemanticPileLog) que consume la pestaña "Pila Semántica":
/// cada registro es "L{linea}: {reducción}  [op '{op}' nivel={n} assoc={a}]"
/// con la precedencia y asociabilidad que SemanticPile registra.
/// </summary>
public sealed class PilaSemanticaTests
{
    private static ParseResult Parse(string code)
    {
        var lexer = new LexicalAnalyzer(SqliteTransitionTableSource.Load(Path.Combine(AppContext.BaseDirectory, "data.db")));
        IReadOnlyList<Token> tokens = lexer.Tokenize(code);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKinds.Error);
        return new TopDownParser(tokens).Parse();
    }

    private static ParseResult ParseValido(string code)
    {
        ParseResult result = Parse(code);
        Assert.True(result.Success, "El programa base debe compilar sin errores: " +
            string.Join("; ", result.Errors.Select(error => error.Format())
                .Concat(result.SemanticErrors.Select(error => error.Format()))));
        return result;
    }

    [Fact]
    public void PilaSemantica_TodosLosRegistrosTienenLineaReduccionYPrecedencia()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Samples", "Valid");
        string[] samples = Directory.GetFiles(directory, "*.banana");
        Assert.NotEmpty(samples);

        var registros = new List<string>();
        foreach (string sample in samples)
        {
            ParseResult result = ParseValido(File.ReadAllText(sample));
            registros.AddRange(result.SemanticPileLog);
        }

        Assert.NotEmpty(registros);
        foreach (string registro in registros)
        {
            // "L6: x + 2 => Entero  [op '+' nivel=4 assoc=izquierda]"
            Assert.Matches(
                @"^L\d+: .+ => .+  \[op '.+' nivel=-?\d+ assoc=(izquierda|derecha)\]$",
                registro);
        }
    }

    [Fact]
    public void PilaSemantica_LosRegistrosIndicanLaLineaYLaPrecedenciaDeCadaReduccion()
    {
        ParseResult result = ParseValido("start\nx = 1 + 2\ny = x * 2\nend");

        Assert.Equal(2, result.SemanticPileLog.Count);
        Assert.Contains(result.SemanticPileLog, registro =>
            registro.StartsWith("L2: ") &&
            registro.Contains("1 + 2 =>") &&
            registro.Contains("nivel=4 assoc=izquierda"));
        Assert.Contains(result.SemanticPileLog, registro =>
            registro.StartsWith("L3: ") &&
            registro.Contains("x * 2 =>") &&
            registro.Contains("nivel=5 assoc=izquierda"));
    }

    [Fact]
    public void PilaSemantica_LosProgramasSinExpresionesNoRegistranReducciones()
    {
        ParseResult result = ParseValido("start\nend");
        Assert.Empty(result.SemanticPileLog);
    }
}
