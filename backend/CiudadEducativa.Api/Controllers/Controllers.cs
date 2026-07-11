using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatriculasController(MatriculaService service, UserContext user) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MatriculaResponse>> Crear([FromBody] CrearMatriculaRequest request)
    {
        try
        {
            user.EnsureColegioAccess(request.ColegioId);
            var result = await service.CrearMatriculaAsync(request);
            return CreatedAtAction(nameof(Crear), result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<MatriculaResponse>>> Consultar(
        [FromQuery] int colegioId,
        [FromQuery] int gradoId,
        [FromQuery] int anioAcademicoId)
    {
        try
        {
            user.EnsureColegioAccess(colegioId);
            var result = await service.ConsultarMatriculasAsync(colegioId, gradoId, anioAcademicoId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

[Authorize]
[ApiController]
[Route("api/estudiantes")]
public class EstudiantesController(MatriculaService service, UserContext user) : ControllerBase
{
    [HttpGet("{id}/historico")]
    public async Task<ActionResult<List<HistoricoEstudianteResponse>>> Historico(int id)
    {
        if (!user.IsAdmin)
        {
            var colegioId = user.RequireColegioId();
            if (!await service.EstudiantePerteneceAColegioAsync(id, colegioId))
                return Forbid();
        }

        var result = await service.ObtenerHistoricoAsync(id);
        return Ok(result);
    }
}

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

    [HttpGet("contratos-por-vencer")]
    public async Task<ActionResult<List<ContratoPorVencerResponse>>> ContratosPorVencer([FromQuery] int dias = 30)
        => Ok(await docenteService.ObtenerContratosPorVencerAsync(dias, user.GetColegioFilter()));
}

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
        try
        {
            user.EnsureColegioAccess(request.ColegioId);
            var result = await service.AsignarDocenteAsync(request);
            return CreatedAtAction(nameof(Listar), result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var ok = await service.DesactivarAsignacionAsync(id, user.GetColegioFilter());
        return ok ? NoContent() : Forbid();
    }
}

[Authorize]
[ApiController]
[Route("api/catalogos")]
public class CatalogosController(AppDbContext db, UserContext user) : ControllerBase
{
    [HttpGet("colegios")]
    public async Task<ActionResult<List<CatalogoItem>>> Colegios()
    {
        var query = db.Colegios.AsQueryable();
        if (!user.IsAdmin)
            query = query.Where(c => c.Id == user.RequireColegioId());

        return Ok(await query.OrderBy(c => c.Nombre)
            .Select(c => new CatalogoItem(c.Id, c.Nombre)).ToListAsync());
    }

    [HttpGet("grados")]
    public async Task<ActionResult<List<CatalogoItem>>> Grados()
        => Ok(await db.Grados.OrderBy(g => g.Orden)
            .Select(g => new CatalogoItem(g.Id, g.Nombre)).ToListAsync());

    [HttpGet("grupos")]
    public async Task<ActionResult<List<GrupoItem>>> Grupos([FromQuery] int? gradoId)
    {
        var query = db.Grupos.Include(g => g.DocenteDirector).AsQueryable();
        if (gradoId.HasValue) query = query.Where(g => g.GradoId == gradoId.Value);

        return Ok(await query.OrderBy(g => g.Nombre)
            .Select(g => new GrupoItem(g.Id, g.Nombre, g.GradoId, g.DocenteDirector != null ? g.DocenteDirector.Nombre : null))
            .ToListAsync());
    }

    [HttpGet("anios")]
    public async Task<ActionResult<List<CatalogoItem>>> Anios()
        => Ok(await db.AniosAcademicos.OrderByDescending(a => a.Anio)
            .Select(a => new CatalogoItem(a.Id, a.Anio.ToString())).ToListAsync());

    [HttpGet("docentes")]
    public async Task<ActionResult<List<CatalogoItem>>> Docentes()
    {
        var query = db.Docentes.Where(d => d.Activo);

        if (!user.IsAdmin)
        {
            var colegioId = user.RequireColegioId();
            query = query.Where(d => d.DocenteColegios.Any(dc => dc.ColegioId == colegioId && dc.Activo));
        }

        return Ok(await query.OrderBy(d => d.Nombre)
            .Select(d => new CatalogoItem(d.Id, d.Nombre)).ToListAsync());
    }
}
