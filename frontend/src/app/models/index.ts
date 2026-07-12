export interface CatalogoItem {
  id: number;
  nombre: string;
}

export interface CatalogoDocenteItem {
  id: number;
  tipoDocumento: string;
  numeroDocumento: string;
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
  anio: number;
}

export interface AnioAcademicoConfig {
  vigente: number;
  minimo: number;
  maximo: number;
}

export interface ActualizarMatriculaRequest {
  nombre: string;
  fechaNacimiento: string;
  gradoId: number;
  grupoId: number;
  anio: number;
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
  matriculaId: number;
  anio: number;
  codigoDane: string;
  gradoId: number;
  grupoId: number;
  grado: string;
  grupo: string;
  colegio: string;
  docenteDirector: string | null;
  activa: boolean;
  fechaMatricula: string;
  fechaAnulacion: string | null;
  motivoInactivacion: string | null;
  motivoEtiqueta: string;
  estado: string;
}

export interface HistorialEstudianteCompleto {
  estudianteId: number;
  nombre: string;
  tipoDocumento: string;
  numeroDocumento: string;
  fechaNacimiento: string;
  tieneMatriculaActiva: boolean;
  colegioActivo: string | null;
  anioActivo: number | null;
  registros: HistoricoEstudiante[];
}

export interface InactivarMatriculaRequest {
  motivo: 'Traslado' | 'FinPeriodo' | 'Retiro';
}

export const MOTIVOS_INACTIVACION: { id: InactivarMatriculaRequest['motivo']; label: string; descripcion: string }[] = [
  {
    id: 'Traslado',
    label: 'Traslado',
    descripcion: 'Libera al estudiante para matricularlo en otro colegio.'
  },
  {
    id: 'FinPeriodo',
    label: 'Fin de periodo',
    descripcion: 'Cierra el año académico; podrá matricularse en el siguiente año.'
  },
  {
    id: 'Retiro',
    label: 'Retiro',
    descripcion: 'El estudiante deja el colegio sin continuidad inmediata.'
  }
];

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
  tipoDocumento: string;
  numeroDocumento: string;
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

export interface ColegioMatriculaRanking {
  posicion: number;
  colegio: string;
  sector: string;
  totalEstudiantes: number;
}

export interface DocenteGrupoAsignado {
  id: number;
  nombre: string;
  gradoNombre: string;
}

export interface DocenteColegio {
  id: number;
  docenteId: number;
  tipoDocumento: string;
  numeroDocumento: string;
  docenteNombre: string;
  codigoDane: string;
  colegioNombre: string;
  sector: string;
  activo: boolean;
  grupos: DocenteGrupoAsignado[];
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
