namespace CiudadEducativa.Api.Models;

public class Estudiante
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NumeroMatricula { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }

    public ICollection<Matricula> Matriculas { get; set; } = [];
}
