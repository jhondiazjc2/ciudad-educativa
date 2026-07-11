using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Services;

public class MatriculaService(AppDbContext db)
{
    private const int AnioMinimo = 2000;
    private const int AniosProyeccionFutura = 20;

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

        if (request.Anio <= 0)
            throw new InvalidOperationException("El año académico es obligatorio.");

        var anioAcademicoId = await ObtenerOCrearAnioAcademicoIdAsync(request.Anio);

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
            await db.SaveChangesAsync();
        }

        var matriculaActivaOtroColegio = await db.Matriculas
            .AnyAsync(m => m.EstudianteId == estudiante.Id && m.Activa && m.CodigoDane != request.CodigoDane);

        if (matriculaActivaOtroColegio)
            throw new InvalidOperationException("El estudiante ya tiene una matrícula activa en otro colegio.");

        var grupoValido = await db.Grupos.AnyAsync(g =>
            g.Id == request.GrupoId &&
            g.CodigoDane == request.CodigoDane &&
            g.GradoId == request.GradoId);

        if (!grupoValido)
            throw new InvalidOperationException("El grupo seleccionado no pertenece al colegio y grado indicados.");

        var matriculaMismoPeriodo = await db.Matriculas
            .FirstOrDefaultAsync(m =>
                m.EstudianteId == estudiante.Id &&
                m.CodigoDane == request.CodigoDane &&
                m.GradoId == request.GradoId &&
                m.AnioAcademicoId == anioAcademicoId);

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
            AnioAcademicoId = anioAcademicoId,
            Activa = true,
            FechaMatricula = DateTime.Today
        };

        db.Matriculas.Add(matricula);
        await db.SaveChangesAsync();

        return (await ObtenerMatriculaPorIdAsync(matricula.Id))!;
    }

    public async Task<List<MatriculaResponse>> ConsultarMatriculasAsync(string codigoDane, int gradoId, int anio)
    {
        var anioAcademicoId = await ObtenerAnioAcademicoIdAsync(anio);
        if (anioAcademicoId is null)
            return [];

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

    public async Task<List<MatriculaResponse>> ListarMatriculasAsync(
        string? codigoDane, int? anio, int? gradoId, string? busqueda = null)
    {
        var query = db.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .Where(m => m.Activa);

        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(m => m.CodigoDane == codigoDane);

        if (gradoId is > 0)
            query = query.Where(m => m.GradoId == gradoId);

        if (anio is > 0)
        {
            var anioAcademicoId = await ObtenerAnioAcademicoIdAsync(anio.Value);
            if (anioAcademicoId is null)
                return [];
            query = query.Where(m => m.AnioAcademicoId == anioAcademicoId);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            var terminoDocumento = TiposDocumento.NormalizarNumeroDocumento(termino);
            var soloDigitos = terminoDocumento.All(char.IsDigit);

            query = query.Where(m =>
                EF.Functions.Like(m.Estudiante.Nombre, $"%{termino}%") ||
                m.Estudiante.NumeroDocumento.Contains(terminoDocumento) ||
                (!soloDigitos && EF.Functions.Like(
                    m.Estudiante.TipoDocumento + " " + m.Estudiante.NumeroDocumento,
                    $"%{termino}%")));
        }

        return await query
            .OrderBy(m => m.Colegio.Nombre)
            .ThenBy(m => m.Estudiante.Nombre)
            .Select(m => MapToResponse(m))
            .ToListAsync();
    }

    public async Task<MatriculaResponse?> ObtenerMatriculaAsync(int id)
        => await ObtenerMatriculaPorIdAsync(id);

    public async Task<MatriculaResponse> ActualizarMatriculaAsync(int id, ActualizarMatriculaRequest request)
    {
        if (request.Anio <= 0)
            throw new InvalidOperationException("El año académico es obligatorio.");

        var matricula = await db.Matriculas
            .Include(m => m.Estudiante)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Matrícula no encontrada.");

        if (!matricula.Activa)
            throw new InvalidOperationException("Solo se pueden editar matrículas activas.");

        var nombre = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre del estudiante es obligatorio.");

        var grupoValido = await db.Grupos.AnyAsync(g =>
            g.Id == request.GrupoId &&
            g.CodigoDane == matricula.CodigoDane &&
            g.GradoId == request.GradoId);

        if (!grupoValido)
            throw new InvalidOperationException("El grupo seleccionado no pertenece al colegio y grado indicados.");

        var anioAcademicoId = await ObtenerOCrearAnioAcademicoIdAsync(request.Anio);

        var duplicada = await db.Matriculas.AnyAsync(m =>
            m.Id != id &&
            m.EstudianteId == matricula.EstudianteId &&
            m.CodigoDane == matricula.CodigoDane &&
            m.GradoId == request.GradoId &&
            m.AnioAcademicoId == anioAcademicoId &&
            m.Activa);

        if (duplicada)
            throw new InvalidOperationException(
                "El estudiante ya tiene otra matrícula activa en este colegio, grado y año académico.");

        matricula.Estudiante.Nombre = nombre;
        matricula.Estudiante.FechaNacimiento = request.FechaNacimiento.Date;
        matricula.GradoId = request.GradoId;
        matricula.GrupoId = request.GrupoId;
        matricula.AnioAcademicoId = anioAcademicoId;

        await db.SaveChangesAsync();
        return (await ObtenerMatriculaPorIdAsync(id))!;
    }

    public async Task<bool> EliminarMatriculaAsync(int id)
    {
        var matricula = await db.Matriculas.FindAsync(id);
        if (matricula is null || !matricula.Activa)
            return false;

        matricula.Activa = false;
        await db.SaveChangesAsync();
        return true;
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

    private static void ValidarAnio(int anio)
    {
        var maximo = DateTime.Today.Year + AniosProyeccionFutura;
        if (anio < AnioMinimo || anio > maximo)
            throw new InvalidOperationException($"El año académico debe estar entre {AnioMinimo} y {maximo}.");
    }

    private async Task<int?> ObtenerAnioAcademicoIdAsync(int anio)
    {
        ValidarAnio(anio);
        var registro = await db.AniosAcademicos
            .Where(a => a.Anio == anio)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        return registro;
    }

    private async Task<int> ObtenerOCrearAnioAcademicoIdAsync(int anio)
    {
        ValidarAnio(anio);

        var existenteId = await db.AniosAcademicos
            .Where(a => a.Anio == anio)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

        if (existenteId.HasValue)
            return existenteId.Value;

        try
        {
            db.AniosAcademicos.Add(new AnioAcademico { Anio = anio });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otro request pudo crear el mismo año en paralelo.
        }

        var id = await db.AniosAcademicos
            .Where(a => a.Anio == anio)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

        if (id is null or <= 0)
            throw new InvalidOperationException($"No se pudo registrar el año académico {anio}.");

        return id.Value;
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

public static class GrupoCatalogoService
{
    public static string NombreGrupo(int ordenGrado, int subnivel)
    {
        if (subnivel is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(subnivel));

        return ordenGrado == 0
            ? (ordenGrado * 1000 + subnivel).ToString("000")
            : $"{ordenGrado}{subnivel:D2}";
    }

    public static async Task CrearGruposParaColegioAsync(AppDbContext db, string codigoDane)
    {
        if (await db.Grupos.AnyAsync(g => g.CodigoDane == codigoDane))
            return;

        var grados = await db.Grados.OrderBy(g => g.Orden).ToListAsync();
        var docentes = await db.DocenteColegios
            .Where(dc => dc.CodigoDane == codigoDane && dc.Activo)
            .OrderBy(dc => dc.DocenteId)
            .Select(dc => dc.DocenteId)
            .ToListAsync();

        var grupos = new List<Grupo>();
        var indice = 0;

        foreach (var grado in grados)
        {
            for (var subnivel = 1; subnivel <= 5; subnivel++)
            {
                int? directorId = null;
                if (docentes.Count > 0)
                    directorId = docentes[indice % docentes.Count];

                grupos.Add(new Grupo
                {
                    CodigoDane = codigoDane,
                    GradoId = grado.Id,
                    Nombre = NombreGrupo(grado.Orden, subnivel),
                    DocenteDirectorId = directorId
                });
                indice++;
            }
        }

        db.Grupos.AddRange(grupos);
        await db.SaveChangesAsync();
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
        await GrupoCatalogoService.CrearGruposParaColegioAsync(db, codigoDane);
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

        var grupos = await db.Grupos.Where(g => g.CodigoDane == codigoDane).ToListAsync();
        db.Grupos.RemoveRange(grupos);

        db.Colegios.Remove(colegio);
        await db.SaveChangesAsync();
    }
}
