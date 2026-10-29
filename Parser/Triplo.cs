namespace BananaCompiler.Parser;

/// <summary>Triplo clásico identificado por su posición en la secuencia.</summary>
public sealed class Triplo
{
    public int Indice { get; set; }
    public string Operador { get; set; }
    public string Arg1 { get; set; }
    public string Arg2 { get; set; }

    public Triplo(int indice, string operador, string arg1, string arg2)
    {
        Indice = indice;
        Operador = operador;
        Arg1 = arg1;
        Arg2 = arg2;
    }
}
