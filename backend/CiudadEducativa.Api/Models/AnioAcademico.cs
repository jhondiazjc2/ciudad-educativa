namespace CiudadEducativa.Api.Models;

public class AnioAcademico
{
    public int Id { get; set; }
    public int Anio { get; set; }

    public ICollection<Matricula> Matriculas { get; set; } = [];
}
