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
            user.EnsureColegioAccess(request.CodigoDane);
            var result = await service.CrearMatriculaAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Matriculas_Anios") == true)
        {
            return BadRequest(new { message = "El año académico no es válido. Reinicie el backend e intente de nuevo." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<MatriculaResponse>>> Consultar(
        [FromQuery] string codigoDane,
        [FromQuery] int gradoId,
        [FromQuery] int anio)
    {
        try
        {
            user.EnsureColegioAccess(codigoDane);
            var result = await service.ConsultarMatriculasAsync(codigoDane, gradoId, anio);
            return Ok(result);
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

    [HttpGet("listado")]
    public async Task<ActionResult<List<MatriculaResponse>>> Listado(
        [FromQuery] string? codigoDane,
        [FromQuery] int? anio,
        [FromQuery] int? gradoId,
        [FromQuery] string? busqueda)
    {
        try
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MatriculaResponse>> Actualizar(int id, [FromBody] ActualizarMatriculaRequest request)
    {
        try
        {
            var matricula = await service.ObtenerMatriculaAsync(id);
            if (matricula is null) return NotFound();
            user.EnsureColegioAccess(matricula.CodigoDane);

            var result = await service.ActualizarMatriculaAsync(id, request);
            return Ok(result);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var matricula = await service.ObtenerMatriculaAsync(id);
            if (matricula is null) return NotFound();
            user.EnsureColegioAccess(matricula.CodigoDane);

            var ok = await service.EliminarMatriculaAsync(id);
            return ok ? NoContent() : NotFound();
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
            var codigoDane = user.RequireCodigoDane();
            if (!await service.EstudiantePerteneceAColegioAsync(id, codigoDane))
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
            user.EnsureColegioAccess(request.CodigoDane);
            var result = await service.AsignarDocenteAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
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
    public async Task<ActionResult<List<CatalogoColegioItem>>> Colegios()
    {
        var query = db.Colegios.AsQueryable();
        if (!user.IsAdmin)
            query = query.Where(c => c.CodigoDane == user.RequireCodigoDane());

        return Ok(await query.OrderBy(c => c.Nombre)
            .Select(c => new CatalogoColegioItem(c.CodigoDane, c.Nombre)).ToListAsync());
    }

    [HttpGet("grados")]
    public async Task<ActionResult<List<CatalogoItem>>> Grados()
        => Ok(await db.Grados.OrderBy(g => g.Orden)
            .Select(g => new CatalogoItem(g.Id, g.Nombre)).ToListAsync());

    [HttpGet("grupos")]
    public async Task<ActionResult<List<GrupoItem>>> Grupos(
        [FromQuery] string codigoDane,
        [FromQuery] int? gradoId)
    {
        if (string.IsNullOrWhiteSpace(codigoDane))
            return BadRequest(new { message = "El codigo DANE del colegio es obligatorio." });

        try
        {
            user.EnsureColegioAccess(codigoDane);

            var query = db.Grupos
                .Include(g => g.DocenteDirector)
                .Where(g => g.CodigoDane == codigoDane);

            if (gradoId.HasValue) query = query.Where(g => g.GradoId == gradoId.Value);

            return Ok(await query.OrderBy(g => g.Nombre)
                .Select(g => new GrupoItem(g.Id, g.Nombre, g.GradoId, g.DocenteDirector != null ? g.DocenteDirector.Nombre : null))
                .ToListAsync());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("anios")]
    public ActionResult<object> Anios()
    {
        var vigente = DateTime.Today.Year;
        return Ok(new
        {
            vigente,
            minimo = 2000,
            maximo = vigente + 20
        });
    }

    [HttpGet("tipos-documento")]
    public ActionResult<List<CatalogoDocumentoItem>> TiposDocumento()
        => Ok(new List<CatalogoDocumentoItem>
        {
            new("RC", "Registro civil"),
            new("TI", "Tarjeta de identidad"),
            new("CC", "Cedula de ciudadania"),
            new("CE", "Cedula de extranjeria"),
            new("PA", "Pasaporte")
        });

    [HttpGet("docentes")]
    public async Task<ActionResult<List<CatalogoItem>>> Docentes()
    {
        var query = db.Docentes.Where(d => d.Activo);

        if (!user.IsAdmin)
        {
            var codigoDane = user.RequireCodigoDane();
            query = query.Where(d => d.DocenteColegios.Any(dc => dc.CodigoDane == codigoDane && dc.Activo));
        }

        return Ok(await query.OrderBy(d => d.Nombre)
            .Select(d => new CatalogoItem(d.Id, d.Nombre)).ToListAsync());
    }
}

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
        try
        {
            var result = await service.CrearAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{codigoDane}")]
    public async Task<ActionResult<ColegioResponse>> Actualizar(string codigoDane, [FromBody] ActualizarColegioRequest request)
    {
        try
        {
            var result = await service.ActualizarAsync(codigoDane, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{codigoDane}")]
    public async Task<IActionResult> Eliminar(string codigoDane)
    {
        try
        {
            await service.EliminarAsync(codigoDane);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
