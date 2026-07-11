import { Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, timeout } from 'rxjs';
import { SelectField, SelectOption, toColegioSelectOptions, toSelectOptions } from '../../components/select-field/select-field';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoColegioItem, CatalogoDocumentoItem, CatalogoItem, GrupoItem, MatriculaResponse } from '../../models';

const TODOS_COLEGIOS = '__all__';

@Component({
  selector: 'app-matricular',
  imports: [FormsModule, SelectField],
  templateUrl: './matricular.html'
})
export class Matricular implements OnInit {
  @ViewChild('errorAlert') errorAlert?: ElementRef<HTMLElement>;

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
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  readonly cargandoListado = signal(false);
  colegioBloqueado = false;
  editandoMatriculaId: number | null = null;

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
    this.cargarListado();
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
      next: (data) => this.matriculas.set(data),
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

  editar(m: MatriculaResponse): void {
    this.mensaje.set('');
    this.error.set('');
    this.editandoMatriculaId = m.id;
    this.nombre = m.nombreEstudiante;
    this.tipoDocumento = m.tipoDocumento;
    this.numeroDocumento = m.numeroDocumento;
    this.fechaNacimiento = m.fechaNacimiento.slice(0, 10);
    this.codigoDane = m.codigoDane;
    this.filtroCodigoDane = m.codigoDane;
    this.gradoId = m.gradoId;
    this.grupoId = m.grupoId;
    this.anio = m.anio;
    this.cargarGrupos();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelarEdicion(): void {
    this.limpiarFormulario();
    this.mensaje.set('');
    this.error.set('');
  }

  eliminar(m: MatriculaResponse): void {
    if (!confirm(`¿Anular la matrícula de "${m.nombreEstudiante}"?`)) return;

    this.mensaje.set('');
    this.error.set('');
    this.api.eliminarMatricula(m.id).subscribe({
      next: () => {
        this.mensaje.set('Matrícula anulada correctamente.');
        if (this.editandoMatriculaId === m.id) this.limpiarFormulario();
        this.cargarListado();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.error.set('No tiene permiso para anular esta matrícula.');
        } else {
          this.error.set('No se pudo anular la matrícula.');
        }
      }
    });
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
    this.mostrarError(this.extraerMensajeApi(err, fallback));
  }

  private extraerMensajeApi(err: { error?: unknown }, fallback: string): string {
    const body = err.error;
    if (body && typeof body === 'object' && 'message' in body && body.message) {
      return String(body.message);
    }
    if (typeof body === 'string' && body.trim()) {
      return body;
    }
    return fallback;
  }

  private mostrarError(mensaje: string): void {
    this.error.set(mensaje);
    setTimeout(() => {
      this.errorAlert?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    });
  }
}
