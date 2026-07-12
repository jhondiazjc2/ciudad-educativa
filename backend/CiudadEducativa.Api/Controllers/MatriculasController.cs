using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CiudadEducativa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatriculasController(MatriculaService service, UserContext user) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MatriculaResponse>> Crear([FromBody] CrearMatriculaRequest request)
    {
        user.EnsureColegioAccess(request.CodigoDane);
        var result = await service.CrearMatriculaAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<MatriculaResponse>>> Consultar(
        [FromQuery] string codigoDane,
        [FromQuery] int gradoId,
        [FromQuery] int anio)
    {
        user.EnsureColegioAccess(codigoDane);
        return Ok(await service.ConsultarMatriculasAsync(codigoDane, gradoId, anio));
    }

    [HttpGet("listado")]
    public async Task<ActionResult<List<MatriculaResponse>>> Listado(
        [FromQuery] string? codigoDane,
        [FromQuery] int? anio,
        [FromQuery] int? gradoId,
        [FromQuery] string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(codigoDane))
        {
            if (!user.IsAdmin)
                return BadRequest(new { message = "El codigo DANE del colegio es obligatorio." });
        }
        else
        {
            user.EnsureColegioAccess(codigoDane);
        }

        return Ok(await service.ListarMatriculasAsync(codigoDane, anio, gradoId, busqueda));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MatriculaResponse>> Actualizar(int id, [FromBody] ActualizarMatriculaRequest request)
    {
        var matricula = await service.ObtenerMatriculaAsync(id);
        if (matricula is null) return NotFound();
        user.EnsureColegioAccess(matricula.CodigoDane);

        return Ok(await service.ActualizarMatriculaAsync(id, request));
    }

    [HttpPost("{id}/inactivar")]
    public async Task<IActionResult> Inactivar(int id, [FromBody] InactivarMatriculaRequest request)
    {
        var matricula = await service.ObtenerMatriculaAsync(id);
        if (matricula is null) return NotFound();
        user.EnsureColegioAccess(matricula.CodigoDane);

        var ok = await service.InactivarMatriculaAsync(id, request.Motivo);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var matricula = await service.ObtenerMatriculaAsync(id);
        if (matricula is null) return NotFound();
        user.EnsureColegioAccess(matricula.CodigoDane);

        var ok = await service.EliminarMatriculaAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
