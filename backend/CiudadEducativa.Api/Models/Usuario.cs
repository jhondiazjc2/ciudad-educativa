namespace CiudadEducativa.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? CodigoDane { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Colegio? Colegio { get; set; }
}
