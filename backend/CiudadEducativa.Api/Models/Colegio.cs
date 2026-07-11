namespace CiudadEducativa.Api.Models;

public class Colegio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;

    public ICollection<Matricula> Matriculas { get; set; } = [];
    public ICollection<DocenteColegio> DocenteColegios { get; set; } = [];
}
