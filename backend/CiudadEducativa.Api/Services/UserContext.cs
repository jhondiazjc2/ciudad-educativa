using System.Security.Claims;

namespace CiudadEducativa.Api.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAdmin => User?.IsInRole("Admin") ?? false;

    public int? ColegioId =>
        int.TryParse(User?.FindFirstValue("colegio_id"), out var id) ? id : null;

    public string? ColegioNombre => User?.FindFirstValue("colegio_nombre");

    public int? GetColegioFilter() => IsAdmin ? null : ColegioId;

    public void EnsureColegioAccess(int colegioId)
    {
        if (IsAdmin) return;
        if (ColegioId != colegioId)
            throw new UnauthorizedAccessException("No tiene permiso para operar sobre este colegio.");
    }

    public int RequireColegioId()
    {
        if (ColegioId is null)
            throw new UnauthorizedAccessException("Usuario de colegio sin colegio asignado.");
        return ColegioId.Value;
    }
}
