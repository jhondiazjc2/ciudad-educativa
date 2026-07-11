namespace CiudadEducativa.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Nombre,
    string Email,
    string Rol,
    int? ColegioId,
    string? ColegioNombre,
    DateTime ExpiraEn
);
