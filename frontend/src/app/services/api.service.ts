import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  ActualizarColegioRequest,
  AsignarDocenteRequest,
  CatalogoColegioItem,
  CatalogoItem,
  CatalogoDocumentoItem,
  ColegioItem,
  ColegioMayorMatricula,
  ContratoPorVencer,
  CrearMatriculaRequest,
  DocenteColegio,
  DocentesPorSector,
  EstudiantesPorEdad,
  ActualizarMatriculaRequest,
  AnioAcademicoConfig,
  GrupoItem,
  GuardarColegioRequest,
  HistoricoEstudiante,
  MatriculaResponse
} from '../models';

const API = 'http://localhost:5000/api';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  getColegios() {
    return this.http.get<CatalogoColegioItem[]>(`${API}/catalogos/colegios`);
  }

  getGrados() {
    return this.http.get<CatalogoItem[]>(`${API}/catalogos/grados`);
  }

  getGrupos(codigoDane: string, gradoId?: number) {
    let params = new HttpParams().set('codigoDane', codigoDane);
    if (gradoId) params = params.set('gradoId', gradoId);
    return this.http.get<GrupoItem[]>(`${API}/catalogos/grupos`, { params });
  }

  getAnioAcademicoConfig() {
    return this.http.get<AnioAcademicoConfig>(`${API}/catalogos/anios`);
  }

  getTiposDocumento() {
    return this.http.get<CatalogoDocumentoItem[]>(`${API}/catalogos/tipos-documento`);
  }

  getDocentes() {
    return this.http.get<CatalogoItem[]>(`${API}/catalogos/docentes`);
  }

  crearMatricula(data: CrearMatriculaRequest) {
    return this.http.post<MatriculaResponse>(`${API}/matriculas`, data);
  }

  listarMatriculas(codigoDane?: string, anio?: number, gradoId?: number, busqueda?: string) {
    let params = new HttpParams();
    if (codigoDane) params = params.set('codigoDane', codigoDane);
    if (anio) params = params.set('anio', anio);
    if (gradoId) params = params.set('gradoId', gradoId);
    if (busqueda?.trim()) params = params.set('busqueda', busqueda.trim());
    return this.http.get<MatriculaResponse[]>(`${API}/matriculas/listado`, { params });
  }

  actualizarMatricula(id: number, data: ActualizarMatriculaRequest) {
    return this.http.put<MatriculaResponse>(`${API}/matriculas/${id}`, data);
  }

  eliminarMatricula(id: number) {
    return this.http.delete<void>(`${API}/matriculas/${id}`);
  }

  consultarMatriculas(codigoDane: string, gradoId: number, anio: number) {
    const params = new HttpParams()
      .set('codigoDane', codigoDane)
      .set('gradoId', gradoId)
      .set('anio', anio);
    return this.http.get<MatriculaResponse[]>(`${API}/matriculas`, { params });
  }

  getHistorico(estudianteId: number) {
    return this.http.get<HistoricoEstudiante[]>(`${API}/estudiantes/${estudianteId}/historico`);
  }

  getEstudiantesPorEdad() {
    return this.http.get<EstudiantesPorEdad>(`${API}/consultas/estudiantes-por-edad`);
  }

  getDocentesPorSector() {
    return this.http.get<DocentesPorSector>(`${API}/consultas/docentes-por-sector`);
  }

  getColegioMayorMatricula() {
    return this.http.get<ColegioMayorMatricula>(`${API}/consultas/colegio-mayor-matricula`);
  }

  getContratosPorVencer(dias = 30) {
    return this.http.get<ContratoPorVencer[]>(`${API}/consultas/contratos-por-vencer`, {
      params: new HttpParams().set('dias', dias)
    });
  }

  getAsignaciones(docenteId?: number) {
    let params = new HttpParams();
    if (docenteId) params = params.set('docenteId', docenteId);
    return this.http.get<DocenteColegio[]>(`${API}/docente-colegios`, { params });
  }

  asignarDocente(data: AsignarDocenteRequest) {
    return this.http.post<DocenteColegio>(`${API}/docente-colegios`, data);
  }

  desactivarAsignacion(id: number) {
    return this.http.delete<void>(`${API}/docente-colegios/${id}`);
  }

  getColegiosAdmin() {
    return this.http.get<ColegioItem[]>(`${API}/colegios`);
  }

  crearColegio(data: GuardarColegioRequest) {
    return this.http.post<ColegioItem>(`${API}/colegios`, data);
  }

  actualizarColegio(codigoDane: string, data: ActualizarColegioRequest) {
    return this.http.put<ColegioItem>(`${API}/colegios/${codigoDane}`, data);
  }

  eliminarColegio(codigoDane: string) {
    return this.http.delete<void>(`${API}/colegios/${codigoDane}`);
  }
}
