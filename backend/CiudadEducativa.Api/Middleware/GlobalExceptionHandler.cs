using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case UnauthorizedAccessException:
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return true;

            case InvalidOperationException ex:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new { message = ex.Message }, cancellationToken);
                return true;

            case DbUpdateException ex when ex.InnerException?.Message.Contains("UQ_DocenteColegio") == true:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new { message = "El docente ya está asignado a este colegio." },
                    cancellationToken);
                return true;

            case Microsoft.Data.SqlClient.SqlException { Number: -2 }:
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsJsonAsync(
                    new { message = "La base de datos no respondió a tiempo. Intente de nuevo en unos segundos." },
                    cancellationToken);
                return true;

            case DbUpdateException ex when ex.InnerException?.Message.Contains("FK_Matriculas_Anios") == true:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new { message = "El año académico no es válido. Reinicie el backend e intente de nuevo." },
                    cancellationToken);
                return true;

            case DbUpdateException ex when ex.InnerException?.Message.Contains("UX_Matriculas_EstudianteActiva") == true:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new { message = "El estudiante ya tiene una matrícula activa. Inactívela antes de crear otra." },
                    cancellationToken);
                return true;
        }

        _logger.LogError(exception, "Error no controlado en {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        return false;
    }
}
