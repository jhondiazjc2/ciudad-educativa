namespace CiudadEducativa.Api.Models;

public class Docente
{
    public int Id { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaContratacion { get; set; }
    public string PeriodoContrato { get; set; } = string.Empty;
    public DateTime? VigenciaContrato { get; set; }
    public bool Activo { get; set; }

    public ICollection<Grupo> GruposDirigidos { get; set; } = [];
    public ICollection<DocenteColegio> DocenteColegios { get; set; } = [];
}
