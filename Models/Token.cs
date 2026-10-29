namespace BananaCompiler.Models;

public class Token
{
    public string Kind { get; set; }
    public string Lexeme { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public bool IsSynthetic { get; init; }


    public Token(string tipo, string valor, int linea = 0, int columna = 0)
    {
        Kind = NormalizeTipo(tipo);
        Lexeme = valor;
        Line = linea;
        Column = columna;
    }

    private static string NormalizeTipo(string tipo)
    {
        return tipo.Trim().ToUpperInvariant();
    }
}
