using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Services;

public class MatriculaService(AppDbContext db)
{
    public static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad;
    }

    public async Task<MatriculaResponse> CrearMatriculaAsync(CrearMatriculaRequest request)
    {
        var estudiante = await db.Estudiantes
            .FirstOrDefaultAsync(e => e.NumeroMatricula == request.NumeroMatricula);

        if (estudiante is null)
        {
            estudiante = new Estudiante
            {
                Nombre = request.Nombre,
                NumeroMatricula = request.NumeroMatricula,
                FechaNacimiento = request.FechaNacimiento.Date
            };
            db.Estudiantes.Add(estudiante);
            await db.SaveChangesAsync();
        }
        else
        {
            estudiante.Nombre = request.Nombre;
            estudiante.FechaNacimiento = request.FechaNacimiento.Date;
        }

        var matriculaActivaOtroColegio = await db.Matriculas
            .AnyAsync(m => m.EstudianteId == estudiante.Id && m.Activa && m.ColegioId != request.ColegioId);

        if (matriculaActivaOtroColegio)
            throw new InvalidOperationException("El estudiante ya tiene una matricula activa en otro colegio.");

        var matriculasActivas = await db.Matriculas
            .Where(m => m.EstudianteId == estudiante.Id && m.Activa)
            .ToListAsync();

        foreach (var m in matriculasActivas)
            m.Activa = false;

        var matricula = new Matricula
        {
            EstudianteId = estudiante.Id,
            ColegioId = request.ColegioId,
            GradoId = request.GradoId,
            GrupoId = request.GrupoId,
            AnioAcademicoId = request.AnioAcademicoId,
            Activa = true,
            FechaMatricula = DateTime.Today
        };

        db.Matriculas.Add(matricula);
        await db.SaveChangesAsync();

        return (await ObtenerMatriculaPorIdAsync(matricula.Id))!;
    }

    public async Task<List<MatriculaResponse>> ConsultarMatriculasAsync(int colegioId, int gradoId, int anioAcademicoId)
    {
        return await db.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .Where(m => m.ColegioId == colegioId && m.GradoId == gradoId && m.AnioAcademicoId == anioAcademicoId)
            .OrderBy(m => m.Estudiante.Nombre)
            .Select(m => MapToResponse(m))
            .ToListAsync();
    }

    public async Task<List<HistoricoEstudianteResponse>> ObtenerHistoricoAsync(int estudianteId)
    {
        return await db.Matriculas
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .Where(m => m.EstudianteId == estudianteId)
            .OrderByDescending(m => m.AnioAcademico.Anio)
            .Select(m => new HistoricoEstudianteResponse(
                m.AnioAcademico.Anio,
                m.Grado.Nombre,
                m.Grupo.Nombre,
                m.Colegio.Nombre,
                m.Grupo.DocenteDirector != null ? m.Grupo.DocenteDirector.Nombre : null,
                m.Activa
            ))
            .ToListAsync();
    }

    public async Task<EstudiantesPorEdadResponse> ObtenerEstudiantesPorEdadAsync(int? colegioId = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (colegioId.HasValue)
            query = query.Where(m => m.ColegioId == colegioId.Value);

        var fechasNacimiento = await query
            .Select(m => m.Estudiante.FechaNacimiento)
            .Distinct()
            .ToListAsync();

        var edades = fechasNacimiento.Select(CalcularEdad).ToList();
        return new EstudiantesPorEdadResponse(
            edades.Count(e => e >= 3 && e <= 7),
            edades.Count(e => e >= 8 && e <= 12),
            edades.Count(e => e > 12),
            edades.Count
        );
    }

    public async Task<bool> EstudiantePerteneceAColegioAsync(int estudianteId, int colegioId)
        => await db.Matriculas.AnyAsync(m => m.EstudianteId == estudianteId && m.ColegioId == colegioId);

    public async Task<ColegioMayorMatriculaResponse?> ObtenerColegioMayorMatriculaAsync(int? colegioId = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (colegioId.HasValue)
            query = query.Where(m => m.ColegioId == colegioId.Value);

        var resultado = await query
            .GroupBy(m => new { m.ColegioId, m.Colegio.Nombre, m.Colegio.Sector })
            .Select(g => new { g.Key.Nombre, g.Key.Sector, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .FirstOrDefaultAsync();

        return resultado is null
            ? null
            : new ColegioMayorMatriculaResponse(resultado.Nombre, resultado.Sector, resultado.Total);
    }

    private async Task<MatriculaResponse?> ObtenerMatriculaPorIdAsync(int id)
    {
        var matricula = await db.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .FirstOrDefaultAsync(m => m.Id == id);

        return matricula is null ? null : MapToResponse(matricula);
    }

    private static MatriculaResponse MapToResponse(Matricula m) => new(
        m.Id,
        m.EstudianteId,
        m.Estudiante.Nombre,
        m.Estudiante.NumeroMatricula,
        m.Estudiante.FechaNacimiento,
        CalcularEdad(m.Estudiante.FechaNacimiento),
        m.ColegioId,
        m.Colegio.Nombre,
        m.GradoId,
        m.Grado.Nombre,
        m.GrupoId,
        m.Grupo.Nombre,
        m.AnioAcademicoId,
        m.AnioAcademico.Anio,
        m.Activa,
        m.Grupo.DocenteDirector?.Nombre
    );
}

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

    public async Task<List<ContratoPorVencerResponse>> ObtenerContratosPorVencerAsync(int dias = 30, int? colegioId = null)
    {
        var hoy = DateTime.Today;
        var limite = hoy.AddDays(dias);

        var query = db.Docentes
            .Where(d => d.Activo && d.VigenciaContrato != null && d.VigenciaContrato >= hoy && d.VigenciaContrato <= limite);

        if (colegioId.HasValue)
        {
            query = query.Where(d => d.DocenteColegios.Any(dc => dc.ColegioId == colegioId.Value && dc.Activo));
        }

        var docentes = await query
            .OrderBy(d => d.VigenciaContrato)
            .ToListAsync();

        return docentes.Select(d => new ContratoPorVencerResponse(
            d.Id,
            d.Nombre,
            d.FechaContratacion,
            d.VigenciaContrato!.Value,
            (d.VigenciaContrato.Value - hoy).Days,
            d.PeriodoContrato
        )).ToList();
    }

    public async Task<DocenteColegioResponse> AsignarDocenteAsync(AsignarDocenteRequest request)
    {
        var existe = await db.DocenteColegios
            .FirstOrDefaultAsync(dc => dc.DocenteId == request.DocenteId && dc.ColegioId == request.ColegioId);

        if (existe is not null)
        {
            existe.Activo = true;
            existe.FechaAsignacion = DateTime.Today;
        }
        else
        {
            existe = new DocenteColegio
            {
                DocenteId = request.DocenteId,
                ColegioId = request.ColegioId,
                FechaAsignacion = DateTime.Today,
                Activo = true
            };
            db.DocenteColegios.Add(existe);
        }

        await db.SaveChangesAsync();
        return (await ObtenerAsignacionPorIdAsync(existe.Id))!;
    }

    public async Task<bool> DesactivarAsignacionAsync(int id, int? colegioId = null)
    {
        var asignacion = await db.DocenteColegios.FindAsync(id);
        if (asignacion is null) return false;
        if (colegioId.HasValue && asignacion.ColegioId != colegioId.Value) return false;

        asignacion.Activo = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DocenteColegioResponse>> ListarAsignacionesAsync(int? docenteId = null, int? colegioId = null)
    {
        var query = db.DocenteColegios
            .Include(dc => dc.Docente)
            .Include(dc => dc.Colegio)
            .AsQueryable();

        if (docenteId.HasValue)
            query = query.Where(dc => dc.DocenteId == docenteId.Value);

        if (colegioId.HasValue)
            query = query.Where(dc => dc.ColegioId == colegioId.Value);

        return await query
            .OrderBy(dc => dc.Docente.Nombre)
            .ThenBy(dc => dc.Colegio.Nombre)
            .Select(dc => new DocenteColegioResponse(
                dc.Id,
                dc.DocenteId,
                dc.Docente.Nombre,
                dc.ColegioId,
                dc.Colegio.Nombre,
                dc.Colegio.Sector,
                dc.Activo
            ))
            .ToListAsync();
    }

    private async Task<DocenteColegioResponse?> ObtenerAsignacionPorIdAsync(int id)
    {
        return await db.DocenteColegios
            .Include(dc => dc.Docente)
            .Include(dc => dc.Colegio)
            .Where(dc => dc.Id == id)
            .Select(dc => new DocenteColegioResponse(
                dc.Id,
                dc.DocenteId,
                dc.Docente.Nombre,
                dc.ColegioId,
                dc.Colegio.Nombre,
                dc.Colegio.Sector,
                dc.Activo
            ))
            .FirstOrDefaultAsync();
    }
}
