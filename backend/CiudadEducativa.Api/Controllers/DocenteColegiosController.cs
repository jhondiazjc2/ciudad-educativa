using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CiudadEducativa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/docente-colegios")]
public class DocenteColegiosController(DocenteService service, UserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DocenteColegioResponse>>> Listar([FromQuery] int? docenteId)
        => Ok(await service.ListarAsignacionesAsync(docenteId, user.GetColegioFilter()));

    [HttpPost]
    public async Task<ActionResult<DocenteColegioResponse>> Asignar([FromBody] AsignarDocenteRequest request)
    {
        user.EnsureColegioAccess(request.CodigoDane);
        var result = await service.AsignarDocenteAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var ok = await service.DesactivarAsignacionAsync(id, user.GetColegioFilter());
        return ok ? NoContent() : Forbid();
    }
}
