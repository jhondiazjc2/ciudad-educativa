namespace CiudadEducativa.Api.Models;

public static class MotivosInactivacion
{
    public const string Traslado = "Traslado";
    public const string FinPeriodo = "FinPeriodo";
    public const string Retiro = "Retiro";
    public const string Renovacion = "Renovacion";

    private static readonly HashSet<string> MotivosUsuario = [Traslado, FinPeriodo, Retiro];

    public static bool EsValidoUsuario(string? motivo)
        => !string.IsNullOrWhiteSpace(motivo) && MotivosUsuario.Contains(motivo);

    public static string Etiqueta(string? motivo) => motivo switch
    {
        Traslado => "Traslado a otro colegio",
        FinPeriodo => "Fin de periodo académico",
        Retiro => "Retiro del colegio",
        Renovacion => "Renovación de matrícula",
        _ => "Inactiva"
    };
}
