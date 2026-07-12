using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Controllers;

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

        user.EnsureColegioAccess(codigoDane);

        var query = db.Grupos
            .Include(g => g.DocenteDirector)
            .Where(g => g.CodigoDane == codigoDane);

        if (gradoId.HasValue) query = query.Where(g => g.GradoId == gradoId.Value);

        return Ok(await query.OrderBy(g => g.Nombre)
            .Select(g => new GrupoItem(g.Id, g.Nombre, g.GradoId, g.DocenteDirector != null ? g.DocenteDirector.Nombre : null))
            .ToListAsync());
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
    public async Task<ActionResult<List<CatalogoDocenteItem>>> Docentes()
    {
        var query = db.Docentes.Where(d => d.Activo);

        if (!user.IsAdmin)
        {
            var codigoDane = user.RequireCodigoDane();
            query = query.Where(d => d.DocenteColegios.Any(dc => dc.CodigoDane == codigoDane && dc.Activo));
        }

        return Ok(await query.OrderBy(d => d.Nombre)
            .Select(d => new CatalogoDocenteItem(d.Id, d.TipoDocumento, d.NumeroDocumento, d.Nombre)).ToListAsync());
    }
}
