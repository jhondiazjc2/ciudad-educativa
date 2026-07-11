using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CiudadEducativa.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Email normalizado: login case-insensitive; los emails en BD deben guardarse en minusculas.
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios
            .Include(u => u.Colegio)
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            return null; // Mismo resultado que usuario inexistente: evita enumeracion de cuentas.

        // Rol Colegio exige ColegioId; sin el, el login falla aunque la contraseña sea correcta.
        if (usuario.Rol == "Colegio" && usuario.ColegioId is null)
            return null;

        var expiraEn = DateTime.UtcNow.AddHours(1);
        var token = GenerarToken(usuario, expiraEn);

        return new LoginResponse(
            token,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.ColegioId,
            usuario.Colegio?.Nombre,
            expiraEn
        );
    }

    private string GenerarToken(Models.Usuario usuario, DateTime expiraEn)
    {
        var jwt = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol)
        };

        if (usuario.ColegioId.HasValue)
        {
            // Claims custom consumidos por UserContext; los nombres deben coincidir con colegio_id / colegio_nombre.
            claims.Add(new Claim("colegio_id", usuario.ColegioId.Value.ToString()));
            if (!string.IsNullOrEmpty(usuario.Colegio?.Nombre))
                claims.Add(new Claim("colegio_nombre", usuario.Colegio.Nombre));
        }

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiraEn,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
