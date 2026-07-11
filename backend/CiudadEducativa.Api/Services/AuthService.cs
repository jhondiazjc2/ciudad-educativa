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
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios
            .Include(u => u.Colegio)
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            return null;

        if (usuario.Rol == "Colegio" && string.IsNullOrWhiteSpace(usuario.CodigoDane))
            return null;

        var expiraEn = DateTime.UtcNow.AddHours(1);
        var token = GenerarToken(usuario, expiraEn);

        return new LoginResponse(
            token,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.CodigoDane,
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

        if (!string.IsNullOrWhiteSpace(usuario.CodigoDane))
        {
            claims.Add(new Claim("codigo_dane", usuario.CodigoDane));
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
