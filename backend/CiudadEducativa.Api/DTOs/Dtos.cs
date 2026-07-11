namespace CiudadEducativa.Api.DTOs;

public record CrearMatriculaRequest(
    string Nombre,
    string TipoDocumento,
    string NumeroDocumento,
    DateTime FechaNacimiento,
    int ColegioId,
    int GradoId,
    int GrupoId,
    int AnioAcademicoId
);

public record MatriculaResponse(
    int Id,
    int EstudianteId,
    string NombreEstudiante,
    string TipoDocumento,
    string NumeroDocumento,
    DateTime FechaNacimiento,
    int Edad,
    int ColegioId,
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

public record HistoricoEstudianteResponse(
    int Anio,
    string Grado,
    string Grupo,
    string Colegio,
    string? DocenteDirector,
    bool Activa
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

public record AsignarDocenteRequest(int DocenteId, int ColegioId);

public record DocenteColegioResponse(
    int Id,
    int DocenteId,
    string DocenteNombre,
    int ColegioId,
    string ColegioNombre,
    string Sector,
    bool Activo
);

public record CatalogoItem(int Id, string Nombre);
public record CatalogoDocumentoItem(string Codigo, string Nombre);
public record GrupoItem(int Id, string Nombre, int GradoId, string? DocenteDirector);
