namespace CiudadEducativa.Api.Models;

public class Grupo
{
    public int Id { get; set; }
    public int GradoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? DocenteDirectorId { get; set; }

    public Grado Grado { get; set; } = null!;
    public Docente? DocenteDirector { get; set; }
    public ICollection<Matricula> Matriculas { get; set; } = [];
}
