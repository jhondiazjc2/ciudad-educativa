using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Services;

public class DocenteService(AppDbContext db)
{
    public async Task<DocentesPorSectorResponse> ObtenerDocentesPorSectorAsync()
    {
        var asignaciones = await db.DocenteColegios
            .Include(dc => dc.Colegio)
            .Include(dc => dc.Docente)
            .Where(dc => dc.Activo && dc.Docente.Activo)
            .ToListAsync();

        var docentesPublico = asignaciones
            .Where(a => a.Colegio.Sector == "Publico")
            .Select(a => a.DocenteId)
            .Distinct()
            .Count();

        var docentesPrivado = asignaciones
            .Where(a => a.Colegio.Sector == "Privado")
            .Select(a => a.DocenteId)
            .Distinct()
            .Count();

        var total = asignaciones.Select(a => a.DocenteId).Distinct().Count();

        return new DocentesPorSectorResponse(docentesPublico, docentesPrivado, total);
    }

    public async Task<List<ContratoPorVencerResponse>> ObtenerContratosPorVencerAsync(int dias = 30, string? codigoDane = null)
    {
        var hoy = DateTime.Today;
        var limite = hoy.AddDays(dias);

        var query = db.Docentes
            .Where(d => d.Activo && d.VigenciaContrato != null && d.VigenciaContrato >= hoy && d.VigenciaContrato <= limite);

        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(d => d.DocenteColegios.Any(dc => dc.CodigoDane == codigoDane && dc.Activo));

        var docentes = await query
            .OrderBy(d => d.VigenciaContrato)
            .ToListAsync();

        return docentes.Select(d => new ContratoPorVencerResponse(
            d.Id,
            d.TipoDocumento,
            d.NumeroDocumento,
            d.Nombre,
            d.FechaContratacion,
            d.VigenciaContrato!.Value,
            (d.VigenciaContrato.Value - hoy).Days,
            d.PeriodoContrato
        )).ToList();
    }

    public async Task<DocenteColegioResponse> AsignarDocenteAsync(AsignarDocenteRequest request)
    {
        var docenteExiste = await db.Docentes.AnyAsync(d => d.Id == request.DocenteId && d.Activo);
        if (!docenteExiste)
            throw new InvalidOperationException("El docente no existe o esta inactivo.");

        var existe = await db.DocenteColegios
            .FirstOrDefaultAsync(dc => dc.DocenteId == request.DocenteId && dc.CodigoDane == request.CodigoDane);

        if (existe is not null)
        {
            if (existe.Activo)
                throw new InvalidOperationException("El docente ya está asignado a este colegio.");

            existe.Activo = true;
            existe.FechaAsignacion = DateTime.Today;
        }
        else
        {
            existe = new DocenteColegio
            {
                DocenteId = request.DocenteId,
                CodigoDane = request.CodigoDane,
                FechaAsignacion = DateTime.Today,
                Activo = true
            };
            db.DocenteColegios.Add(existe);
        }

        await db.SaveChangesAsync();
        return (await ObtenerAsignacionPorIdAsync(existe.Id))!;
    }

    public async Task<bool> DesactivarAsignacionAsync(int id, string? codigoDane = null)
    {
        var asignacion = await db.DocenteColegios.FindAsync(id);
        if (asignacion is null) return false;
        if (!string.IsNullOrWhiteSpace(codigoDane) && asignacion.CodigoDane != codigoDane) return false;

        asignacion.Activo = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DocenteColegioResponse>> ListarAsignacionesAsync(int? docenteId = null, string? codigoDane = null)
    {
        var query = db.DocenteColegios
            .Include(dc => dc.Docente)
            .Include(dc => dc.Colegio)
            .AsQueryable();

        if (docenteId.HasValue)
            query = query.Where(dc => dc.DocenteId == docenteId.Value);

        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(dc => dc.CodigoDane == codigoDane);

        query = query.Where(dc => dc.Activo);

        var asignaciones = await query
            .OrderBy(dc => dc.Docente.Nombre)
            .ThenBy(dc => dc.Colegio.Nombre)
            .Select(dc => new
            {
                dc.Id,
                dc.DocenteId,
                dc.Docente.TipoDocumento,
                dc.Docente.NumeroDocumento,
                DocenteNombre = dc.Docente.Nombre,
                dc.CodigoDane,
                ColegioNombre = dc.Colegio.Nombre,
                Sector = dc.Colegio.Sector,
                dc.Activo
            })
            .ToListAsync();

        if (asignaciones.Count == 0)
            return [];

        var docenteIds = asignaciones.Select(a => a.DocenteId).Distinct().ToList();
        var codigosDane = asignaciones.Select(a => a.CodigoDane).Distinct().ToList();

        var gruposPorClave = await db.Grupos
            .Include(g => g.Grado)
            .Where(g =>
                g.DocenteDirectorId != null &&
                docenteIds.Contains(g.DocenteDirectorId.Value) &&
                codigosDane.Contains(g.CodigoDane))
            .OrderBy(g => g.Grado.Orden)
            .ThenBy(g => g.Nombre)
            .Select(g => new
            {
                DocenteId = g.DocenteDirectorId!.Value,
                g.CodigoDane,
                g.Id,
                g.Nombre,
                GradoNombre = g.Grado.Nombre
            })
            .ToListAsync();

        var gruposLookup = gruposPorClave
            .GroupBy(g => (g.DocenteId, g.CodigoDane))
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DocenteGrupoAsignadoResponse(x.Id, x.Nombre, x.GradoNombre)).ToList());

        return asignaciones.Select(a =>
        {
            gruposLookup.TryGetValue((a.DocenteId, a.CodigoDane), out var grupos);
            return new DocenteColegioResponse(
                a.Id,
                a.DocenteId,
                a.TipoDocumento,
                a.NumeroDocumento,
                a.DocenteNombre,
                a.CodigoDane,
                a.ColegioNombre,
                a.Sector,
                a.Activo,
                grupos ?? []);
        }).ToList();
    }

    private async Task<DocenteColegioResponse?> ObtenerAsignacionPorIdAsync(int id)
    {
        var asignacion = await db.DocenteColegios
            .Include(dc => dc.Docente)
            .Include(dc => dc.Colegio)
            .Where(dc => dc.Id == id)
            .Select(dc => new
            {
                dc.Id,
                dc.DocenteId,
                dc.Docente.TipoDocumento,
                dc.Docente.NumeroDocumento,
                DocenteNombre = dc.Docente.Nombre,
                dc.CodigoDane,
                ColegioNombre = dc.Colegio.Nombre,
                Sector = dc.Colegio.Sector,
                dc.Activo
            })
            .FirstOrDefaultAsync();

        if (asignacion is null)
            return null;

        var grupos = await db.Grupos
            .Include(g => g.Grado)
            .Where(g =>
                g.CodigoDane == asignacion.CodigoDane &&
                g.DocenteDirectorId == asignacion.DocenteId)
            .OrderBy(g => g.Grado.Orden)
            .ThenBy(g => g.Nombre)
            .Select(g => new DocenteGrupoAsignadoResponse(g.Id, g.Nombre, g.Grado.Nombre))
            .ToListAsync();

        return new DocenteColegioResponse(
            asignacion.Id,
            asignacion.DocenteId,
            asignacion.TipoDocumento,
            asignacion.NumeroDocumento,
            asignacion.DocenteNombre,
            asignacion.CodigoDane,
            asignacion.ColegioNombre,
            asignacion.Sector,
            asignacion.Activo,
            grupos);
    }
}
