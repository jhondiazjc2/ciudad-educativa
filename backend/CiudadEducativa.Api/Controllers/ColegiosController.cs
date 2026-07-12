using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CiudadEducativa.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/colegios")]
public class ColegiosController(ColegioService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ColegioResponse>>> Listar()
        => Ok(await service.ListarAsync());

    [HttpGet("{codigoDane}")]
    public async Task<ActionResult<ColegioResponse>> Obtener(string codigoDane)
    {
        var result = await service.ObtenerPorCodigoAsync(codigoDane);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ColegioResponse>> Crear([FromBody] CrearColegioRequest request)
    {
        var result = await service.CrearAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{codigoDane}")]
    public async Task<ActionResult<ColegioResponse>> Actualizar(string codigoDane, [FromBody] ActualizarColegioRequest request)
        => Ok(await service.ActualizarAsync(codigoDane, request));

    [HttpDelete("{codigoDane}")]
    public async Task<IActionResult> Eliminar(string codigoDane)
    {
        await service.EliminarAsync(codigoDane);
        return NoContent();
    }
}
