namespace CiudadEducativa.Api.Models;

public static class TiposDocumento
{
    public const string RC = "RC";
    public const string TI = "TI";
    public const string CC = "CC";
    public const string CE = "CE";
    public const string PA = "PA";

    public static readonly string[] Validos = [RC, TI, CC, CE, PA];

    public static bool EsValido(string tipo)
        => Validos.Contains(tipo, StringComparer.OrdinalIgnoreCase);

    public static string Normalizar(string tipo) => tipo.Trim().ToUpperInvariant();

    public static string NormalizarNumeroDocumento(string numero)
    {
        var limpio = new string(numero.Trim().Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(limpio) ? numero.Trim() : limpio;
    }
}
