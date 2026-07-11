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
        // Reglas de matricula:
        // 1. El estudiante se identifica por TipoDocumento + NumeroDocumento (upsert: actualiza nombre/fecha si ya existe).
        // 2. No puede tener matricula activa en otro colegio.
        // 3. Al matricular, se desactivan todas las matriculas activas previas del estudiante.
        // 4. FechaMatricula = fecha del servidor, no la envia el cliente.
        // 5. No se permite matricula duplicada en el mismo colegio, grado y ano academico.
        var tipoDocumento = TiposDocumento.Normalizar(request.TipoDocumento);
        var numeroDocumento = TiposDocumento.NormalizarNumeroDocumento(request.NumeroDocumento);

        if (!TiposDocumento.EsValido(tipoDocumento))
            throw new InvalidOperationException("Tipo de documento invalido. Use RC, TI, CC, CE o PA.");

        if (string.IsNullOrWhiteSpace(numeroDocumento))
            throw new InvalidOperationException("El numero de documento es obligatorio.");

        var estudiante = await db.Estudiantes
            .FirstOrDefaultAsync(e => e.TipoDocumento == tipoDocumento && e.NumeroDocumento == numeroDocumento);

        if (estudiante is null)
        {
            estudiante = new Estudiante
            {
                Nombre = request.Nombre.Trim(),
                TipoDocumento = tipoDocumento,
                NumeroDocumento = numeroDocumento,
                FechaNacimiento = request.FechaNacimiento.Date
            };
            db.Estudiantes.Add(estudiante);
            await db.SaveChangesAsync();
        }
        else
        {
            estudiante.Nombre = request.Nombre.Trim();
            estudiante.FechaNacimiento = request.FechaNacimiento.Date;
        }

        var matriculaActivaOtroColegio = await db.Matriculas
            .AnyAsync(m => m.EstudianteId == estudiante.Id && m.Activa && m.CodigoDane != request.CodigoDane);

        if (matriculaActivaOtroColegio)
            throw new InvalidOperationException("El estudiante ya tiene una matrícula activa en otro colegio.");

        var matriculaMismoPeriodo = await db.Matriculas
            .FirstOrDefaultAsync(m =>
                m.EstudianteId == estudiante.Id &&
                m.CodigoDane == request.CodigoDane &&
                m.GradoId == request.GradoId &&
                m.AnioAcademicoId == request.AnioAcademicoId);

        if (matriculaMismoPeriodo is not null)
        {
            if (matriculaMismoPeriodo.Activa)
                throw new InvalidOperationException(
                    "El estudiante ya está matriculado en este colegio, grado y año académico.");

            var otrasActivas = await db.Matriculas
                .Where(m => m.EstudianteId == estudiante.Id && m.Activa)
                .ToListAsync();

            foreach (var m in otrasActivas)
                m.Activa = false;

            matriculaMismoPeriodo.Activa = true;
            matriculaMismoPeriodo.GrupoId = request.GrupoId;
            matriculaMismoPeriodo.FechaMatricula = DateTime.Today;
            await db.SaveChangesAsync();
            return (await ObtenerMatriculaPorIdAsync(matriculaMismoPeriodo.Id))!;
        }

        var matriculasActivas = await db.Matriculas
            .Where(m => m.EstudianteId == estudiante.Id && m.Activa)
            .ToListAsync();

        foreach (var m in matriculasActivas)
            m.Activa = false;

        var matricula = new Matricula
        {
            EstudianteId = estudiante.Id,
            CodigoDane = request.CodigoDane,
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

    public async Task<List<MatriculaResponse>> ConsultarMatriculasAsync(string codigoDane, int gradoId, int anioAcademicoId)
    {
        return await db.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .Where(m => m.CodigoDane == codigoDane && m.GradoId == gradoId && m.AnioAcademicoId == anioAcademicoId && m.Activa)
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

    public async Task<EstudiantesPorEdadResponse> ObtenerEstudiantesPorEdadAsync(string? codigoDane = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(m => m.CodigoDane == codigoDane);

        // Solo matriculas activas. Distinct por fecha de nacimiento: cada fecha unica cuenta una vez en los rangos.
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

    public async Task<bool> EstudiantePerteneceAColegioAsync(int estudianteId, string codigoDane)
        => await db.Matriculas.AnyAsync(m => m.EstudianteId == estudianteId && m.CodigoDane == codigoDane);

    public async Task<ColegioMayorMatriculaResponse?> ObtenerColegioMayorMatriculaAsync(string? codigoDane = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(m => m.CodigoDane == codigoDane);

        var resultado = await query
            .GroupBy(m => new { m.CodigoDane, m.Colegio.Nombre, m.Colegio.Sector })
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
        m.Estudiante.TipoDocumento,
        m.Estudiante.NumeroDocumento,
        m.Estudiante.FechaNacimiento,
        CalcularEdad(m.Estudiante.FechaNacimiento),
        m.CodigoDane,
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
            d.Nombre,
            d.FechaContratacion,
            d.VigenciaContrato!.Value,
            (d.VigenciaContrato.Value - hoy).Days,
            d.PeriodoContrato
        )).ToList();
    }

    public async Task<DocenteColegioResponse> AsignarDocenteAsync(AsignarDocenteRequest request)
    {
        // Indice unico (DocenteId, CodigoDane): si ya existe activa, se rechaza; si existe inactiva, se reactiva.
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

        return await query
            .OrderBy(dc => dc.Docente.Nombre)
            .ThenBy(dc => dc.Colegio.Nombre)
            .Select(dc => new DocenteColegioResponse(
                dc.Id,
                dc.DocenteId,
                dc.Docente.Nombre,
                dc.CodigoDane,
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
                dc.CodigoDane,
                dc.Colegio.Nombre,
                dc.Colegio.Sector,
                dc.Activo
            ))
            .FirstOrDefaultAsync();
    }
}

public class ColegioService(AppDbContext db)
{
    private static readonly HashSet<string> SectoresValidos = ["Publico", "Privado"];

    private static string NormalizarSector(string sector)
    {
        var s = sector.Trim();
        if (s.Equals("Publico", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("Público", StringComparison.OrdinalIgnoreCase))
            return "Publico";
        if (s.Equals("Privado", StringComparison.OrdinalIgnoreCase))
            return "Privado";
        return s;
    }

    private static string NormalizarCodigoDane(string codigoDane)
        => new string(codigoDane.Where(char.IsDigit).ToArray());

    private static void ValidarSector(string sector)
    {
        if (!SectoresValidos.Contains(sector))
            throw new InvalidOperationException("Sector invalido. Use Publico o Privado.");
    }

    private static void ValidarCodigoDane(string codigoDane)
    {
        if (string.IsNullOrWhiteSpace(codigoDane))
            throw new InvalidOperationException("El codigo DANE es obligatorio.");
        if (codigoDane.Length > 12)
            throw new InvalidOperationException("El codigo DANE no puede superar 12 digitos.");
    }

    public async Task<List<ColegioResponse>> ListarAsync()
        => await db.Colegios
            .OrderBy(c => c.Nombre)
            .Select(c => new ColegioResponse(c.CodigoDane, c.Nombre, c.Sector))
            .ToListAsync();

    public async Task<ColegioResponse?> ObtenerPorCodigoAsync(string codigoDane)
        => await db.Colegios
            .Where(c => c.CodigoDane == codigoDane)
            .Select(c => new ColegioResponse(c.CodigoDane, c.Nombre, c.Sector))
            .FirstOrDefaultAsync();

    public async Task<ColegioResponse> CrearAsync(CrearColegioRequest request)
    {
        var codigoDane = NormalizarCodigoDane(request.CodigoDane);
        ValidarCodigoDane(codigoDane);

        var nombre = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre del colegio es obligatorio.");

        var sector = NormalizarSector(request.Sector);
        ValidarSector(sector);

        if (await db.Colegios.AnyAsync(c => c.CodigoDane == codigoDane))
            throw new InvalidOperationException("Ya existe un colegio con ese codigo DANE.");

        if (await db.Colegios.AnyAsync(c => c.Nombre == nombre))
            throw new InvalidOperationException("Ya existe un colegio con ese nombre.");

        var colegio = new Colegio { CodigoDane = codigoDane, Nombre = nombre, Sector = sector };
        db.Colegios.Add(colegio);
        await db.SaveChangesAsync();
        return new ColegioResponse(colegio.CodigoDane, colegio.Nombre, colegio.Sector);
    }

    public async Task<ColegioResponse> ActualizarAsync(string codigoDane, ActualizarColegioRequest request)
    {
        var colegio = await db.Colegios.FindAsync(codigoDane)
            ?? throw new InvalidOperationException("Colegio no encontrado.");

        var nombre = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre del colegio es obligatorio.");

        var sector = NormalizarSector(request.Sector);
        ValidarSector(sector);

        if (await db.Colegios.AnyAsync(c => c.Nombre == nombre && c.CodigoDane != codigoDane))
            throw new InvalidOperationException("Ya existe otro colegio con ese nombre.");

        colegio.Nombre = nombre;
        colegio.Sector = sector;
        await db.SaveChangesAsync();
        return new ColegioResponse(colegio.CodigoDane, colegio.Nombre, colegio.Sector);
    }

    public async Task EliminarAsync(string codigoDane)
    {
        var colegio = await db.Colegios.FindAsync(codigoDane)
            ?? throw new InvalidOperationException("Colegio no encontrado.");

        if (await db.Matriculas.AnyAsync(m => m.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene matriculas asociadas.");

        if (await db.Usuarios.AnyAsync(u => u.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene usuarios asociados.");

        if (await db.DocenteColegios.AnyAsync(dc => dc.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene docentes asignados.");

        db.Colegios.Remove(colegio);
        await db.SaveChangesAsync();
    }
}
