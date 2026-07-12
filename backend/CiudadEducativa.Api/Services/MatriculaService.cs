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
            throw new InvalidOperationException("El a├▒o acad├®mico es obligatorio.");

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
            throw new InvalidOperationException("El estudiante ya tiene una matr├¡cula activa en otro colegio.");

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
                    "El estudiante ya est├í matriculado en este colegio, grado y a├▒o acad├®mico.");

            var otrasActivas = await db.Matriculas
                .Where(m => m.EstudianteId == estudiante.Id && m.Activa)
                .ToListAsync();

            foreach (var m in otrasActivas)
                DesactivarMatricula(m, MotivosInactivacion.Renovacion);

            ReactivarMatricula(matriculaMismoPeriodo);
            matriculaMismoPeriodo.GrupoId = request.GrupoId;
            await db.SaveChangesAsync();
            return (await ObtenerMatriculaPorIdAsync(matriculaMismoPeriodo.Id))!;
        }

        var matriculasActivas = await db.Matriculas
            .Where(m => m.EstudianteId == estudiante.Id && m.Activa)
            .ToListAsync();

        foreach (var m in matriculasActivas)
            DesactivarMatricula(m, MotivosInactivacion.Renovacion);

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
        var buscarTexto = !string.IsNullOrWhiteSpace(busqueda);
        var anioVigente = DateTime.Today.Year;
        var consultaAnioAnterior = anio is > 0 && anio < anioVigente;
        var incluirInactivas = buscarTexto || consultaAnioAnterior;

        var query = db.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .AsQueryable();

        if (!incluirInactivas)
            query = query.Where(m => m.Activa);

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
            .OrderByDescending(m => m.Activa)
            .ThenBy(m => m.Colegio.Nombre)
            .ThenBy(m => m.Estudiante.Nombre)
            .Select(m => MapToResponse(m))
            .ToListAsync();
    }

    public async Task<HistorialEstudianteCompletoResponse?> ObtenerHistorialPorBusquedaAsync(string busqueda)
    {
        var termino = busqueda.Trim();
        if (string.IsNullOrWhiteSpace(termino))
            throw new InvalidOperationException("Ingrese nombre o documento para buscar.");

        var terminoDocumento = TiposDocumento.NormalizarNumeroDocumento(termino);
        var soloDigitos = terminoDocumento.All(char.IsDigit);

        var estudiante = await db.Estudiantes
            .FirstOrDefaultAsync(e =>
                e.NumeroDocumento == terminoDocumento ||
                EF.Functions.Like(e.Nombre, $"%{termino}%") ||
                (!soloDigitos && EF.Functions.Like(
                    e.TipoDocumento + " " + e.NumeroDocumento,
                    $"%{termino}%")));

        return estudiante is null ? null : await ConstruirHistorialCompletoAsync(estudiante);
    }

    public async Task<HistorialEstudianteCompletoResponse?> ObtenerHistorialPorDocumentoAsync(
        string tipoDocumento, string numeroDocumento)
    {
        var tipo = TiposDocumento.Normalizar(tipoDocumento);
        var numero = TiposDocumento.NormalizarNumeroDocumento(numeroDocumento);

        if (!TiposDocumento.EsValido(tipo) || string.IsNullOrWhiteSpace(numero))
            throw new InvalidOperationException("Documento inválido.");

        var estudiante = await db.Estudiantes
            .FirstOrDefaultAsync(e => e.TipoDocumento == tipo && e.NumeroDocumento == numero);

        return estudiante is null ? null : await ConstruirHistorialCompletoAsync(estudiante);
    }

    private async Task<HistorialEstudianteCompletoResponse> ConstruirHistorialCompletoAsync(Estudiante estudiante)
    {
        var registros = await ConsultarHistoricoAsync(estudiante.Id);
        var activa = registros.FirstOrDefault(r => r.Activa);

        return new HistorialEstudianteCompletoResponse(
            estudiante.Id,
            estudiante.Nombre,
            estudiante.TipoDocumento,
            estudiante.NumeroDocumento,
            estudiante.FechaNacimiento,
            activa is not null,
            activa?.Colegio,
            activa?.Anio,
            registros
        );
    }

    public async Task<MatriculaResponse?> ObtenerMatriculaAsync(int id)
        => await ObtenerMatriculaPorIdAsync(id);

    public async Task<MatriculaResponse> ActualizarMatriculaAsync(int id, ActualizarMatriculaRequest request)
    {
        if (request.Anio <= 0)
            throw new InvalidOperationException("El a├▒o acad├®mico es obligatorio.");

        var matricula = await db.Matriculas
            .Include(m => m.Estudiante)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Matr├¡cula no encontrada.");

        if (!matricula.Activa)
            throw new InvalidOperationException("Solo se pueden editar matr├¡culas activas.");

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
                "El estudiante ya tiene otra matr├¡cula activa en este colegio, grado y a├▒o acad├®mico.");

        matricula.Estudiante.Nombre = nombre;
        matricula.Estudiante.FechaNacimiento = request.FechaNacimiento.Date;
        matricula.GradoId = request.GradoId;
        matricula.GrupoId = request.GrupoId;
        matricula.AnioAcademicoId = anioAcademicoId;

        await db.SaveChangesAsync();
        return (await ObtenerMatriculaPorIdAsync(id))!;
    }

    public async Task<bool> InactivarMatriculaAsync(int id, string motivo)
    {
        if (!MotivosInactivacion.EsValidoUsuario(motivo))
            throw new InvalidOperationException(
                "Motivo de inactivación inválido. Use Traslado, FinPeriodo o Retiro.");

        var matricula = await db.Matriculas.FindAsync(id);
        if (matricula is null || !matricula.Activa)
            return false;

        DesactivarMatricula(matricula, motivo);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarMatriculaAsync(int id)
        => await InactivarMatriculaAsync(id, MotivosInactivacion.Retiro);

    public async Task<List<HistoricoEstudianteResponse>> ObtenerHistoricoAsync(int estudianteId)
        => await ConsultarHistoricoAsync(estudianteId);

    private async Task<List<HistoricoEstudianteResponse>> ConsultarHistoricoAsync(int estudianteId)
    {
        var matriculas = await db.Matriculas
            .Include(m => m.Colegio)
            .Include(m => m.Grado)
            .Include(m => m.Grupo).ThenInclude(g => g.DocenteDirector)
            .Include(m => m.AnioAcademico)
            .Where(m => m.EstudianteId == estudianteId)
            .OrderByDescending(m => m.AnioAcademico.Anio)
            .ThenByDescending(m => m.FechaMatricula)
            .ToListAsync();

        return matriculas.Select(MapToHistorico).ToList();
    }

    private static HistoricoEstudianteResponse MapToHistorico(Matricula m)
    {
        var motivo = m.MotivoInactivacion;
        return new HistoricoEstudianteResponse(
            m.Id,
            m.AnioAcademico.Anio,
            m.CodigoDane,
            m.GradoId,
            m.GrupoId,
            m.Grado.Nombre,
            m.Grupo.Nombre,
            m.Colegio.Nombre,
            m.Grupo.DocenteDirector != null ? m.Grupo.DocenteDirector.Nombre : null,
            m.Activa,
            m.FechaMatricula,
            m.FechaAnulacion,
            motivo,
            MotivosInactivacion.Etiqueta(motivo),
            m.Activa ? "Activa" : "Histórica"
        );
    }

    private static void DesactivarMatricula(Matricula matricula, string motivo)
    {
        matricula.Activa = false;
        matricula.FechaAnulacion = DateTime.Today;
        matricula.MotivoInactivacion = motivo;
    }

    private static void ReactivarMatricula(Matricula matricula)
    {
        matricula.Activa = true;
        matricula.FechaAnulacion = null;
        matricula.MotivoInactivacion = null;
        matricula.FechaMatricula = DateTime.Today;
    }

    public async Task<EstudiantesPorEdadResponse> ObtenerEstudiantesPorEdadAsync(string? codigoDane = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(m => m.CodigoDane == codigoDane);

        // Solo matriculas activas. Un estudiante cuenta una vez aunque tenga varias matriculas activas (no deberia ocurrir).
        var fechasNacimiento = await query
            .Select(m => new { m.EstudianteId, m.Estudiante.FechaNacimiento })
            .Distinct()
            .Select(x => x.FechaNacimiento)
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
        var ranking = await ObtenerRankingColegiosMatriculaAsync(1, codigoDane);
        var top = ranking.FirstOrDefault();
        return top is null
            ? null
            : new ColegioMayorMatriculaResponse(top.Colegio, top.Sector, top.TotalEstudiantes);
    }

    public async Task<List<ColegioMatriculaRankingResponse>> ObtenerRankingColegiosMatriculaAsync(
        int top = 5,
        string? codigoDane = null)
    {
        var query = db.Matriculas.Where(m => m.Activa);
        if (!string.IsNullOrWhiteSpace(codigoDane))
            query = query.Where(m => m.CodigoDane == codigoDane);

        var resultados = await query
            .GroupBy(m => new { m.CodigoDane, m.Colegio.Nombre, m.Colegio.Sector })
            .Select(g => new { g.Key.Nombre, g.Key.Sector, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToListAsync();

        return resultados
            .Select((x, i) => new ColegioMatriculaRankingResponse(i + 1, x.Nombre, x.Sector, x.Total))
            .ToList();
    }

    private static void ValidarAnio(int anio)
    {
        var maximo = DateTime.Today.Year + AniosProyeccionFutura;
        if (anio < AnioMinimo || anio > maximo)
            throw new InvalidOperationException($"El a├▒o acad├®mico debe estar entre {AnioMinimo} y {maximo}.");
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
            // Otro request pudo crear el mismo a├▒o en paralelo.
        }

        var id = await db.AniosAcademicos
            .Where(a => a.Anio == anio)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

        if (id is null or <= 0)
            throw new InvalidOperationException($"No se pudo registrar el a├▒o acad├®mico {anio}.");

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

