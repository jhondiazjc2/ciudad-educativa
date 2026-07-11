namespace CiudadEducativa.Api.Models;

public class Matricula
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public string CodigoDane { get; set; } = string.Empty;
    public int GradoId { get; set; }
    public int GrupoId { get; set; }
    public int AnioAcademicoId { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaMatricula { get; set; }

    public Estudiante Estudiante { get; set; } = null!;
    public Colegio Colegio { get; set; } = null!;
    public Grado Grado { get; set; } = null!;
    public Grupo Grupo { get; set; } = null!;
    public AnioAcademico AnioAcademico { get; set; } = null!;
}
