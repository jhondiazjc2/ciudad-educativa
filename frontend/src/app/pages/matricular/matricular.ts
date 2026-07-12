import { Component, computed, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, EMPTY, finalize, timeout, Observable, tap } from 'rxjs';
import { SelectField, SelectOption, toColegioSelectOptions, toSelectOptions } from '../../components/select-field/select-field';
import { MatriculaForm } from '../../components/matricula-form/matricula-form';
import { HistorialEstudianteTabla } from '../../components/historial-estudiante-tabla/historial-estudiante-tabla';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { extraerMensajeApi } from '../../utils/api-error';
import { CatalogoColegioItem, CatalogoDocumentoItem, CatalogoItem, GrupoItem, HistorialEstudianteCompleto, InactivarMatriculaRequest, MatriculaResponse, MOTIVOS_INACTIVACION } from '../../models';

const TODOS_COLEGIOS = '__all__';
const PAGE_SIZE = 15;

@Component({
  selector: 'app-matricular',
  imports: [FormsModule, SelectField, MatriculaForm, HistorialEstudianteTabla],
  templateUrl: './matricular.html'
})
export class Matricular implements OnInit {
  @ViewChild('errorAlert') errorAlert?: ElementRef<HTMLElement>;

  protected readonly motivosInactivacion = MOTIVOS_INACTIVACION;

  colegios: CatalogoColegioItem[] = [];
  grados: CatalogoItem[] = [];
  grupos: GrupoItem[] = [];
  tiposDocumento: CatalogoDocumentoItem[] = [];

  nombre = '';
  tipoDocumento = 'RC';
  numeroDocumento = '';
  fechaNacimiento = '';
  codigoDane = '';
  gradoId = 0;
  grupoId = 0;
  anio = new Date().getFullYear();
  anioMin = 2000;
  anioMax = new Date().getFullYear() + 20;

  filtroAnio = new Date().getFullYear();
  filtroGradoId = 0;
  filtroBusqueda = '';
  filtroCodigoDane: string = TODOS_COLEGIOS;

  readonly matriculas = signal<MatriculaResponse[]>([]);
  readonly paginaActual = signal(1);
  readonly PAGE_SIZE = PAGE_SIZE;
  readonly totalRegistros = computed(() => this.matriculas().length);
  readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.totalRegistros() / PAGE_SIZE))
  );
  readonly matriculasPagina = computed(() => {
    const pagina = Math.min(this.paginaActual(), this.totalPaginas());
    const inicio = (pagina - 1) * PAGE_SIZE;
    return this.matriculas().slice(inicio, inicio + PAGE_SIZE);
  });
  readonly rangoInicio = computed(() =>
    this.totalRegistros() === 0 ? 0 : (Math.min(this.paginaActual(), this.totalPaginas()) - 1) * PAGE_SIZE + 1
  );
  readonly rangoFin = computed(() =>
    Math.min(Math.min(this.paginaActual(), this.totalPaginas()) * PAGE_SIZE, this.totalRegistros())
  );
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  readonly cargandoListado = signal(false);
  readonly historial = signal<HistorialEstudianteCompleto | null>(null);
  readonly cargandoHistorial = signal(false);
  readonly buscandoEstudiante = signal(false);
  readonly estudiantePrecargado = signal(false);
  colegioBloqueado = false;
  editandoMatriculaId: number | null = null;
  inactivandoMatricula: MatriculaResponse | null = null;
  motivoInactivacion: InactivarMatriculaRequest['motivo'] = 'Traslado';
  historialTipoDocumento = 'RC';
  historialNumeroDocumento = '';
  historialBusqueda = '';

  readonly panelAbierto = signal<{ id: number; tipo: 'historial' | 'editar' | 'inactivar' } | null>(null);
  readonly guardandoEdicion = signal(false);
  edicion = { nombre: '', fechaNacimiento: '', gradoId: 0, grupoId: 0, anio: new Date().getFullYear() };
  gruposEdicion: GrupoItem[] = [];

  constructor(private api: ApiService, private auth: AuthService) {}

  get colegioOptions() {
    return toColegioSelectOptions(this.colegios);
  }

  get filtroColegioOptions(): SelectOption[] {
    if (this.colegioBloqueado) {
      return toColegioSelectOptions(this.colegios);
    }
    return [{ id: TODOS_COLEGIOS, label: 'Todos los colegios' }, ...toColegioSelectOptions(this.colegios)];
  }

  get mostrarColumnaColegio(): boolean {
    return !this.colegioBloqueado && this.filtroCodigoDane === TODOS_COLEGIOS;
  }

  get gradoOptions() {
    return toSelectOptions(this.grados);
  }

  get filtroGradoOptions(): SelectOption[] {
    return [{ id: 0, label: 'Todos los grados' }, ...toSelectOptions(this.grados)];
  }

  get grupoOptions(): SelectOption[] {
    return this.grupos.map((g) => ({
      id: g.id,
      label: g.docenteDirector ? `${g.nombre} (Dir: ${g.docenteDirector})` : g.nombre
    }));
  }

  get enEdicion(): boolean {
    return this.editandoMatriculaId !== null;
  }

  ngOnInit(): void {
    this.api.getColegios().subscribe((d) => {
      this.colegios = d;
      if (this.auth.isColegio() && this.auth.getCodigoDane()) {
        this.codigoDane = this.auth.getCodigoDane()!;
        this.filtroCodigoDane = this.codigoDane;
        this.colegioBloqueado = true;
        this.cargarListado();
      }
    });
    this.api.getGrados().subscribe((d) => (this.grados = d));
    this.api.getAnioAcademicoConfig().subscribe((cfg) => {
      this.anio = cfg.vigente;
      this.filtroAnio = cfg.vigente;
      this.anioMin = cfg.minimo;
      this.anioMax = cfg.maximo;
    });
    this.api.getTiposDocumento().subscribe((d) => {
      this.tiposDocumento = d;
      if (d.length && !d.some((t) => t.codigo === this.tipoDocumento)) {
        this.tipoDocumento = d[0].codigo;
      }
    });
  }

  onColegioFormChange(): void {
    this.grupoId = 0;
    this.cargarGrupos();
  }

  onGradoChange(): void {
    this.grupoId = 0;
    this.cargarGrupos();
  }

  onFiltroChange(): void {
    this.paginaActual.set(1);
    this.cargarListado();
  }

  irPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas()) return;
    this.cerrarPanel();
    this.paginaActual.set(pagina);
  }

  paginaAnterior(): void {
    this.irPagina(this.paginaActual() - 1);
  }

  paginaSiguiente(): void {
    this.irPagina(this.paginaActual() + 1);
  }

  cargarListado(): void {
    if (this.colegioBloqueado && !this.filtroCodigoDane) {
      this.matriculas.set([]);
      return;
    }

    const codigoFiltro = this.filtroCodigoDane === TODOS_COLEGIOS
      ? undefined
      : this.filtroCodigoDane || undefined;

    this.cargandoListado.set(true);
    this.api.listarMatriculas(
      codigoFiltro,
      this.filtroAnio || undefined,
      this.filtroGradoId || undefined,
      this.filtroBusqueda || undefined
    ).pipe(finalize(() => this.cargandoListado.set(false))).subscribe({
      next: (data) => {
        this.matriculas.set(data);
        this.paginaActual.set(1);
        this.cerrarPanel();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.error.set('No tiene permiso para ver matrículas de este colegio.');
        } else {
          this.error.set('No se pudo cargar el listado de matrículas.');
        }
      }
    });
  }

  private cargarGrupos(): void {
    if (this.codigoDane && this.gradoId) {
      this.api.getGrupos(this.codigoDane, this.gradoId).subscribe((d) => (this.grupos = d));
    } else {
      this.grupos = [];
    }
  }

  guardar(): void {
    this.mensaje.set('');
    this.error.set('');

    if (!this.nombre || !this.tipoDocumento || !this.numeroDocumento || !this.fechaNacimiento ||
        !this.codigoDane || !this.gradoId || !this.grupoId || !this.anio) {
      this.error.set('Completa todos los campos.');
      return;
    }

    this.enviando.set(true);

    if (this.enEdicion) {
      this.api.actualizarMatricula(this.editandoMatriculaId!, {
        nombre: this.nombre,
        fechaNacimiento: this.fechaNacimiento,
        gradoId: this.gradoId,
        grupoId: this.grupoId,
        anio: Number(this.anio)
      }).pipe(
        timeout(15000),
        finalize(() => { this.enviando.set(false); })
      ).subscribe({
        next: () => {
          this.mensaje.set('Matrícula actualizada correctamente.');
          this.limpiarFormulario();
          this.cargarListado();
        },
        error: (err) => this.manejarError(err, 'Error al actualizar la matrícula.')
      });
      return;
    }

    this.api.crearMatricula({
      nombre: this.nombre,
      tipoDocumento: this.tipoDocumento,
      numeroDocumento: this.numeroDocumento.replace(/\D/g, ''),
      fechaNacimiento: this.fechaNacimiento,
      codigoDane: this.codigoDane,
      gradoId: this.gradoId,
      grupoId: this.grupoId,
      anio: Number(this.anio)
    }).pipe(
      timeout(15000),
      finalize(() => { this.enviando.set(false); })
    ).subscribe({
      next: () => {
        this.mensaje.set('Estudiante matriculado correctamente.');
        this.limpiarFormulario(false);
        this.cargarListado();
      },
      error: (err) => this.manejarError(err, 'Error al matricular estudiante.')
    });
  }

  get grupoEdicionOptions(): SelectOption[] {
    return this.gruposEdicion.map((g) => ({
      id: g.id,
      label: g.docenteDirector ? `${g.nombre} (Dir: ${g.docenteDirector})` : g.nombre
    }));
  }

  esPanel(id: number, tipo: 'historial' | 'editar' | 'inactivar'): boolean {
    const abierto = this.panelAbierto();
    return abierto?.id === id && abierto.tipo === tipo;
  }

  alternarPanel(m: MatriculaResponse, tipo: 'historial' | 'editar' | 'inactivar'): void {
    if (this.esPanel(m.id, tipo)) {
      this.cerrarPanel();
      return;
    }

    this.mensaje.set('');
    this.error.set('');
    this.panelAbierto.set({ id: m.id, tipo });

    if (tipo === 'historial') {
      this.cargarHistorial(m.numeroDocumento || m.nombreEstudiante, { nombreFallback: m.nombreEstudiante }).subscribe();
    } else if (tipo === 'editar') {
      this.prepararEdicionInline(m);
    } else if (tipo === 'inactivar') {
      this.inactivandoMatricula = m;
      this.motivoInactivacion = 'Traslado';
    }
  }

  cerrarPanel(): void {
    this.panelAbierto.set(null);
    this.inactivandoMatricula = null;
  }

  private prepararEdicionInline(m: MatriculaResponse): void {
    this.edicion = {
      nombre: m.nombreEstudiante,
      fechaNacimiento: m.fechaNacimiento.slice(0, 10),
      gradoId: m.gradoId,
      grupoId: m.grupoId,
      anio: m.anio
    };
    this.gruposEdicion = [];
    this.api.getGrupos(m.codigoDane, m.gradoId).subscribe((d) => (this.gruposEdicion = d));
  }

  onGradoEdicionChange(m: MatriculaResponse): void {
    this.edicion.grupoId = 0;
    this.gruposEdicion = [];
    if (this.edicion.gradoId) {
      this.api.getGrupos(m.codigoDane, this.edicion.gradoId).subscribe((d) => (this.gruposEdicion = d));
    }
  }

  guardarEdicionInline(m: MatriculaResponse): void {
    if (!this.edicion.nombre || !this.edicion.fechaNacimiento || !this.edicion.gradoId ||
        !this.edicion.grupoId || !this.edicion.anio) {
      this.error.set('Completa todos los campos de la matrícula.');
      return;
    }

    this.mensaje.set('');
    this.error.set('');
    this.guardandoEdicion.set(true);
    this.api.actualizarMatricula(m.id, {
      nombre: this.edicion.nombre,
      fechaNacimiento: this.edicion.fechaNacimiento,
      gradoId: this.edicion.gradoId,
      grupoId: this.edicion.grupoId,
      anio: Number(this.edicion.anio)
    }).pipe(
      timeout(15000),
      finalize(() => this.guardandoEdicion.set(false))
    ).subscribe({
      next: () => {
        this.mensaje.set('Matrícula actualizada correctamente.');
        this.cerrarPanel();
        this.cargarListado();
      },
      error: (err) => this.manejarError(err, 'Error al actualizar la matrícula.')
    });
  }

  confirmarInactivacion(): void {
    const m = this.inactivandoMatricula;
    if (!m) return;

    this.mensaje.set('');
    this.error.set('');
    this.api.inactivarMatricula(m.id, { motivo: this.motivoInactivacion }).subscribe({
      next: () => {
        const motivo = this.motivosInactivacion.find((item) => item.id === this.motivoInactivacion)?.label ?? '';
        this.mensaje.set(`Matrícula inactivada (${motivo}). El estudiante queda disponible para matricular en otro colegio o año.`);
        this.cerrarPanel();
        this.cargarListado();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.error.set('No tiene permiso para inactivar esta matrícula.');
        } else {
          this.error.set(extraerMensajeApi(err, 'No se pudo inactivar la matrícula.') ?? 'No se pudo inactivar la matrícula.');
        }
      }
    });
  }

  cancelarEdicion(): void {
    this.limpiarFormulario();
    this.mensaje.set('');
    this.error.set('');
  }

  buscarEstudianteEnFormulario(): void {
    if (this.enEdicion) return;

    const termino = this.numeroDocumento.trim() || this.nombre.trim();
    if (!termino) {
      this.error.set('Ingrese nombre o número de documento para buscar.');
      return;
    }

    this.mensaje.set('');
    this.error.set('');
    this.buscandoEstudiante.set(true);
    this.cargarHistorial(termino, {
      onSuccess: (data) => {
        this.estudiantePrecargado.set(true);
        this.aplicarDatosEstudiante(data, false);
        if (data.tieneMatriculaActiva) {
          this.mensaje.set(`${data.nombre} ya tiene matrícula activa en ${data.colegioActivo} (${data.anioActivo}).`);
        } else {
          this.mensaje.set(`${data.nombre} encontrado. Use "Renovar siguiente año" o complete grado y grupo.`);
        }
      },
      onNotFound: () => {
        this.estudiantePrecargado.set(false);
        this.mensaje.set('Estudiante nuevo. Complete los datos para la primera matrícula.');
      }
    }).pipe(finalize(() => this.buscandoEstudiante.set(false))).subscribe();
  }

  renovarDesdeListado(m: MatriculaResponse): void {
    this.cargarHistorial(m.numeroDocumento || m.nombreEstudiante, {
      onSuccess: (data) => this.renovarSiguienteCiclo()
    }).subscribe();
  }

  buscarHistorial(): void {
    const termino = this.historialBusqueda.trim()
      || this.historialNumeroDocumento.trim()
      || this.filtroBusqueda.trim();
    if (!termino) {
      this.error.set('Ingrese nombre o documento para consultar el historial.');
      return;
    }
    this.cargarHistorial(termino).subscribe();
  }

  renovarSiguienteCiclo(): void {
    const h = this.historial();
    if (!h) return;

    if (h.tieneMatriculaActiva) {
      this.error.set('El estudiante aún tiene matrícula activa. Inactívela con "Fin de periodo" antes de renovar.');
      return;
    }

    const ultimo = h.registros[0];
    if (!ultimo) {
      this.error.set('No hay matrículas previas para renovar.');
      return;
    }

    this.cancelarEdicion();
    this.aplicarDatosEstudiante(h, true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private aplicarDatosEstudiante(h: HistorialEstudianteCompleto, renovar: boolean): void {
    const ultimo = h.registros[0];

    this.nombre = h.nombre;
    this.tipoDocumento = h.tipoDocumento;
    this.numeroDocumento = h.numeroDocumento;
    this.fechaNacimiento = h.fechaNacimiento.slice(0, 10);

    if (!renovar || !ultimo) return;

    this.codigoDane = this.colegioBloqueado ? this.codigoDane : ultimo.codigoDane;
    this.anio = ultimo.anio + 1;
    this.gradoId = this.sugerirSiguienteGrado(ultimo.gradoId);
    this.grupoId = 0;
    this.cargarGrupos();

    const gradoNombre = this.grados.find((g) => g.id === this.gradoId)?.nombre ?? '';
    this.mensaje.set(
      `Renovación preparada para ${h.nombre}: año ${this.anio}, grado ${gradoNombre}. Solo seleccione el grupo y confirme.`
    );
  }

  private sugerirSiguienteGrado(gradoId: number): number {
    const indice = this.grados.findIndex((g) => g.id === gradoId);
    if (indice >= 0 && indice < this.grados.length - 1) {
      return this.grados[indice + 1].id;
    }
    return gradoId;
  }

  abrirHistorialDesdeListado(m: MatriculaResponse): void {
    this.alternarPanel(m, 'historial');
  }

  cerrarHistorial(): void {
    this.historial.set(null);
  }

  private cargarHistorial(
    termino: string,
    opts?: {
      nombreFallback?: string;
      onSuccess?: (data: HistorialEstudianteCompleto) => void;
      onNotFound?: () => void;
    }
  ): Observable<HistorialEstudianteCompleto> {
    this.mensaje.set('');
    this.error.set('');
    this.cargandoHistorial.set(true);

    const numeros = termino.replace(/\D/g, '');
    const request = numeros.length >= 5
      ? this.api.getHistorialPorDocumento(this.tipoDocumento, numeros)
      : this.api.getHistorialPorBusqueda(termino);

    return request.pipe(
      finalize(() => this.cargandoHistorial.set(false)),
      tap((data) => {
        const enriched = opts?.nombreFallback
          ? { ...data, nombre: data.nombre || opts.nombreFallback }
          : data;
        this.historial.set(enriched);
        this.historialTipoDocumento = data.tipoDocumento;
        this.historialNumeroDocumento = data.numeroDocumento;
        this.historialBusqueda = data.nombre;
        opts?.onSuccess?.(enriched);
      }),
      catchError((err: HttpErrorResponse) => {
        this.historial.set(null);
        if (err.status === 404) {
          if (opts?.onNotFound) {
            opts.onNotFound();
          } else {
            this.error.set('No se encontró un estudiante con esos datos.');
          }
        } else if (err.status === 403) {
          this.error.set('No tiene permiso para consultar el historial de este estudiante.');
        } else {
          this.error.set('No se pudo consultar el historial.');
        }
        return EMPTY;
      })
    );
  }

  private limpiarFormulario(resetColegio = true): void {
    this.editandoMatriculaId = null;
    this.nombre = '';
    this.tipoDocumento = this.tiposDocumento[0]?.codigo ?? 'RC';
    this.numeroDocumento = '';
    this.fechaNacimiento = '';
    if (resetColegio && !this.colegioBloqueado) {
      this.codigoDane = '';
    }
    this.gradoId = 0;
    this.grupoId = 0;
    this.grupos = [];
    this.anio = this.filtroAnio;
    this.estudiantePrecargado.set(false);
  }

  private manejarError(err: HttpErrorResponse | { name?: string; status?: number; error?: unknown }, fallback: string): void {
    if (err.name === 'TimeoutError') {
      this.mostrarError('El servidor no respondió a tiempo. Verifica que el backend esté corriendo en localhost:5000.');
      return;
    }
    if (err.status === 0) {
      this.mostrarError('No se pudo conectar con el servidor. Reinicia el backend e intenta de nuevo.');
      return;
    }
    if (err.status === 403) {
      this.mostrarError('No tiene permiso para operar sobre este colegio.');
      return;
    }
    this.mostrarError(extraerMensajeApi(err, fallback) ?? fallback);
  }

  private mostrarError(mensaje: string): void {
    this.error.set(mensaje);
    setTimeout(() => {
      this.errorAlert?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    });
  }
}
