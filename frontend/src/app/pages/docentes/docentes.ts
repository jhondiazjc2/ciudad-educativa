import { Component, computed, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { extraerMensajeApi } from '../../utils/api-error';
import { finalize } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoColegioItem, CatalogoDocenteItem, DocenteColegio } from '../../models';
import { formatearSector } from '../../utils/texto';
import {
  SelectField,
  SelectOption,
  toColegioSelectOptions,
  toDocenteSelectOptions
} from '../../components/select-field/select-field';

const PAGE_SIZE = 15;

@Component({
  selector: 'app-docentes',
  imports: [FormsModule, SelectField],
  templateUrl: './docentes.html'
})
export class Docentes implements OnInit {
  protected readonly formatearSector = formatearSector;
  docentes: CatalogoDocenteItem[] = [];
  colegios: CatalogoColegioItem[] = [];
  readonly asignaciones = signal<DocenteColegio[]>([]);
  readonly paginaActual = signal(1);
  readonly PAGE_SIZE = PAGE_SIZE;
  readonly totalRegistros = computed(() => this.asignaciones().length);
  readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.totalRegistros() / PAGE_SIZE))
  );
  readonly asignacionesPagina = computed(() => {
    const pagina = Math.min(this.paginaActual(), this.totalPaginas());
    const inicio = (pagina - 1) * PAGE_SIZE;
    return this.asignaciones().slice(inicio, inicio + PAGE_SIZE);
  });
  readonly rangoInicio = computed(() =>
    this.totalRegistros() === 0 ? 0 : (Math.min(this.paginaActual(), this.totalPaginas()) - 1) * PAGE_SIZE + 1
  );
  readonly rangoFin = computed(() =>
    Math.min(Math.min(this.paginaActual(), this.totalPaginas()) * PAGE_SIZE, this.totalRegistros())
  );

  docenteId = 0;
  codigoDane = '';
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  get docenteOptions(): SelectOption[] {
    return toDocenteSelectOptions(this.docentes);
  }

  get colegioOptions() {
    return toColegioSelectOptions(this.colegios);
  }

  ngOnInit(): void {
    this.api.getDocentes().subscribe((d) => (this.docentes = d));
    this.api.getColegios().subscribe((d) => {
      this.colegios = d;
      if (this.auth.isColegio() && this.auth.getCodigoDane()) {
        this.codigoDane = this.auth.getCodigoDane()!;
        this.colegioBloqueado = true;
      }
    });
    this.cargarAsignaciones();
  }

  cargarAsignaciones(): void {
    this.api.getAsignaciones().subscribe((d) => {
      this.asignaciones.set(d);
      this.paginaActual.set(1);
    });
  }

  irPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas()) return;
    this.paginaActual.set(pagina);
  }

  paginaAnterior(): void {
    this.irPagina(this.paginaActual() - 1);
  }

  paginaSiguiente(): void {
    this.irPagina(this.paginaActual() + 1);
  }

  asignar(): void {
    this.mensaje.set('');
    this.error.set('');

    if (!this.docenteId || !this.codigoDane) {
      this.error.set('Selecciona docente y colegio.');
      return;
    }

    this.enviando.set(true);
    this.api.asignarDocente({ docenteId: this.docenteId, codigoDane: this.codigoDane }).pipe(
      finalize(() => this.enviando.set(false))
    ).subscribe({
      next: () => {
        this.mensaje.set('Docente asignado correctamente.');
        this.docenteId = 0;
        if (!this.colegioBloqueado) this.codigoDane = '';
        this.cargarAsignaciones();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(
          err.status === 403
            ? 'No tiene permiso para asignar docentes a este colegio.'
            : (extraerMensajeApi(err) ?? 'Error al asignar docente.')
        );
      }
    });
  }

  quitar(id: number): void {
    this.mensaje.set('');
    this.error.set('');
    this.api.desactivarAsignacion(id).subscribe({
      next: () => {
        this.mensaje.set('Asignación desactivada correctamente.');
        this.cargarAsignaciones();
      },
      error: () => this.error.set('No tiene permiso para quitar esta asignación.')
    });
  }
}
