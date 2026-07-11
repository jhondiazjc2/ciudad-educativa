export interface CatalogoItem {
  id: number;
  nombre: string;
}

export interface CatalogoColegioItem {
  codigoDane: string;
  nombre: string;
}

export interface CatalogoDocumentoItem {
  codigo: string;
  nombre: string;
}

export interface GrupoItem {
  id: number;
  nombre: string;
  gradoId: number;
  docenteDirector: string | null;
}

export interface CrearMatriculaRequest {
  nombre: string;
  tipoDocumento: string;
  numeroDocumento: string;
  fechaNacimiento: string;
  codigoDane: string;
  gradoId: number;
  grupoId: number;
  anioAcademicoId: number;
}

export interface MatriculaResponse {
  id: number;
  estudianteId: number;
  nombreEstudiante: string;
  tipoDocumento: string;
  numeroDocumento: string;
  fechaNacimiento: string;
  edad: number;
  codigoDane: string;
  colegioNombre: string;
  gradoId: number;
  gradoNombre: string;
  grupoId: number;
  grupoNombre: string;
  anioAcademicoId: number;
  anio: number;
  activa: boolean;
  docenteDirector: string | null;
}

export interface HistoricoEstudiante {
  anio: number;
  grado: string;
  grupo: string;
  colegio: string;
  docenteDirector: string | null;
  activa: boolean;
}

export interface EstudiantesPorEdad {
  entre3y7: number;
  entre8y12: number;
  mayoresDe12: number;
  total: number;
}

export interface DocentesPorSector {
  publico: number;
  privado: number;
  total: number;
}

export interface ContratoPorVencer {
  docenteId: number;
  nombre: string;
  fechaContratacion: string;
  vigenciaContrato: string;
  diasRestantes: number;
  periodoContrato: string;
}

export interface ColegioMayorMatricula {
  colegio: string;
  sector: string;
  totalEstudiantes: number;
}

export interface DocenteColegio {
  id: number;
  docenteId: number;
  docenteNombre: string;
  codigoDane: string;
  colegioNombre: string;
  sector: string;
  activo: boolean;
}

export interface AsignarDocenteRequest {
  docenteId: number;
  codigoDane: string;
}

export interface ColegioItem {
  codigoDane: string;
  nombre: string;
  sector: string;
}

export interface GuardarColegioRequest {
  codigoDane: string;
  nombre: string;
  sector: string;
}

export interface ActualizarColegioRequest {
  nombre: string;
  sector: string;
}
