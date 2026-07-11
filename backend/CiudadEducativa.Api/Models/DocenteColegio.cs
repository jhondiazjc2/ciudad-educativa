namespace CiudadEducativa.Api.Models;

public class DocenteColegio
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public int ColegioId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public bool Activo { get; set; }

    public Docente Docente { get; set; } = null!;
    public Colegio Colegio { get; set; } = null!;
}
