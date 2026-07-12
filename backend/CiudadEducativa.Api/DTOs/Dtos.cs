namespace CiudadEducativa.Api.DTOs;

using System.Text.Json.Serialization;

public class CrearMatriculaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string CodigoDane { get; set; } = string.Empty;
    public int GradoId { get; set; }
    public int GrupoId { get; set; }

    [JsonPropertyName("anio")]
    public int Anio { get; set; }
}

public class ActualizarMatriculaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public int GradoId { get; set; }
    public int GrupoId { get; set; }

    [JsonPropertyName("anio")]
    public int Anio { get; set; }
}

public record MatriculaResponse(
    int Id,
    int EstudianteId,
    string NombreEstudiante,
    string TipoDocumento,
    string NumeroDocumento,
    DateTime FechaNacimiento,
    int Edad,
    string CodigoDane,
    string ColegioNombre,
    int GradoId,
    string GradoNombre,
    int GrupoId,
    string GrupoNombre,
    int AnioAcademicoId,
    int Anio,
    bool Activa,
    string? DocenteDirector
);

public record InactivarMatriculaRequest(string Motivo);

public record HistoricoEstudianteResponse(
    int MatriculaId,
    int Anio,
    string CodigoDane,
    int GradoId,
    int GrupoId,
    string Grado,
    string Grupo,
    string Colegio,
    string? DocenteDirector,
    bool Activa,
    DateTime FechaMatricula,
    DateTime? FechaAnulacion,
    string? MotivoInactivacion,
    string MotivoEtiqueta,
    string Estado
);

public record HistorialEstudianteCompletoResponse(
    int EstudianteId,
    string Nombre,
    string TipoDocumento,
    string NumeroDocumento,
    DateTime FechaNacimiento,
    bool TieneMatriculaActiva,
    string? ColegioActivo,
    int? AnioActivo,
    List<HistoricoEstudianteResponse> Registros
);

public record EstudiantesPorEdadResponse(
    int Entre3y7,
    int Entre8y12,
    int MayoresDe12,
    int Total
);

public record DocentesPorSectorResponse(
    int Publico,
    int Privado,
    int Total
);

public record ContratoPorVencerResponse(
    int DocenteId,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    DateTime FechaContratacion,
    DateTime VigenciaContrato,
    int DiasRestantes,
    string PeriodoContrato
);

public record ColegioMayorMatriculaResponse(
    string Colegio,
    string Sector,
    int TotalEstudiantes
);

public record ColegioMatriculaRankingResponse(
    int Posicion,
    string Colegio,
    string Sector,
    int TotalEstudiantes
);

public record AsignarDocenteRequest(int DocenteId, string CodigoDane);

public record DocenteGrupoAsignadoResponse(
    int Id,
    string Nombre,
    string GradoNombre
);

public record DocenteColegioResponse(
    int Id,
    int DocenteId,
    string TipoDocumento,
    string NumeroDocumento,
    string DocenteNombre,
    string CodigoDane,
    string ColegioNombre,
    string Sector,
    bool Activo,
    List<DocenteGrupoAsignadoResponse> Grupos
);

public record CatalogoItem(int Id, string Nombre);
public record CatalogoDocenteItem(
    int Id,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre
);
public record CatalogoColegioItem(string CodigoDane, string Nombre);
public record CatalogoDocumentoItem(string Codigo, string Nombre);
public record GrupoItem(int Id, string Nombre, int GradoId, string? DocenteDirector);

public record ColegioResponse(string CodigoDane, string Nombre, string Sector);

public record CrearColegioRequest(string CodigoDane, string Nombre, string Sector);

public record ActualizarColegioRequest(string Nombre, string Sector);
