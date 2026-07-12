using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CiudadEducativa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/consultas")]
public class ConsultasController(MatriculaService matriculaService, DocenteService docenteService, UserContext user) : ControllerBase
{
    [HttpGet("estudiantes-por-edad")]
    public async Task<ActionResult<EstudiantesPorEdadResponse>> EstudiantesPorEdad()
        => Ok(await matriculaService.ObtenerEstudiantesPorEdadAsync(user.GetColegioFilter()));

    [Authorize(Roles = "Admin")]
    [HttpGet("docentes-por-sector")]
    public async Task<ActionResult<DocentesPorSectorResponse>> DocentesPorSector()
        => Ok(await docenteService.ObtenerDocentesPorSectorAsync());

    [HttpGet("colegio-mayor-matricula")]
    public async Task<ActionResult<ColegioMayorMatriculaResponse>> ColegioMayorMatricula()
    {
        var result = await matriculaService.ObtenerColegioMayorMatriculaAsync(user.GetColegioFilter());
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("colegios-ranking-matricula")]
    public async Task<ActionResult<List<ColegioMatriculaRankingResponse>>> ColegiosRankingMatricula([FromQuery] int top = 5)
    {
        if (top < 1) top = 1;
        if (top > 10) top = 10;
        return Ok(await matriculaService.ObtenerRankingColegiosMatriculaAsync(top, user.GetColegioFilter()));
    }
}
