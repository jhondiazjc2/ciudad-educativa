using System.Security.Claims;

namespace CiudadEducativa.Api.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAdmin => User?.IsInRole("Admin") ?? false;

    public string? CodigoDane => User?.FindFirstValue("codigo_dane");

    public string? ColegioNombre => User?.FindFirstValue("colegio_nombre");

    public string? GetColegioFilter() => IsAdmin ? null : CodigoDane;

    public void EnsureColegioAccess(string codigoDane)
    {
        if (IsAdmin) return;
        if (CodigoDane != codigoDane)
            throw new UnauthorizedAccessException("No tiene permiso para operar sobre este colegio.");
    }

    public string RequireCodigoDane()
    {
        if (string.IsNullOrWhiteSpace(CodigoDane))
            throw new UnauthorizedAccessException("Usuario de colegio sin colegio asignado.");
        return CodigoDane;
    }
}
