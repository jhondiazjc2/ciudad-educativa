using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CiudadEducativa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/estudiantes")]
public class EstudiantesController(MatriculaService service, UserContext user) : ControllerBase
{
    [HttpGet("historico")]
    public async Task<ActionResult<HistorialEstudianteCompletoResponse>> HistoricoPorDocumento(
        [FromQuery] string? tipoDocumento,
        [FromQuery] string? numeroDocumento,
        [FromQuery] string? busqueda)
    {
        HistorialEstudianteCompletoResponse? historial;

        if (!string.IsNullOrWhiteSpace(numeroDocumento))
        {
            historial = await service.ObtenerHistorialPorDocumentoAsync(
                tipoDocumento ?? "RC", numeroDocumento);
        }
        else if (!string.IsNullOrWhiteSpace(busqueda))
        {
            historial = await service.ObtenerHistorialPorBusquedaAsync(busqueda);
        }
        else
        {
            return BadRequest(new { message = "Ingrese nombre o número de documento para buscar." });
        }

        if (historial is null)
            return NotFound(new { message = "No se encontró un estudiante con esos datos." });

        if (!user.IsAdmin)
        {
            var codigoDane = user.RequireCodigoDane();
            if (!await service.EstudiantePerteneceAColegioAsync(historial.EstudianteId, codigoDane))
                return Forbid();
        }

        return Ok(historial);
    }

    [HttpGet("{id}/historico")]
    public async Task<ActionResult<List<HistoricoEstudianteResponse>>> Historico(int id)
    {
        if (!user.IsAdmin)
        {
            var codigoDane = user.RequireCodigoDane();
            if (!await service.EstudiantePerteneceAColegioAsync(id, codigoDane))
                return Forbid();
        }

        return Ok(await service.ObtenerHistoricoAsync(id));
    }
}
