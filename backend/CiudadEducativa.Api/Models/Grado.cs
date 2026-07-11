namespace CiudadEducativa.Api.Models;

public class Grado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }

    public ICollection<Grupo> Grupos { get; set; } = [];
    public ICollection<Matricula> Matriculas { get; set; } = [];
}
