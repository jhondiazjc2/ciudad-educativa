import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoColegioItem, CatalogoItem, DocenteColegio } from '../../models';
import { formatearSector } from '../../utils/texto';
import { SelectField, toColegioSelectOptions, toSelectOptions } from '../../components/select-field/select-field';

@Component({
  selector: 'app-docentes',
  imports: [FormsModule, SelectField],
  templateUrl: './docentes.html'
})
export class Docentes implements OnInit {
  protected readonly formatearSector = formatearSector;
  docentes: CatalogoItem[] = [];
  colegios: CatalogoColegioItem[] = [];
  readonly asignaciones = signal<DocenteColegio[]>([]);

  docenteId = 0;
  codigoDane = '';
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  get docenteOptions() {
    return toSelectOptions(this.docentes);
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
    this.api.getAsignaciones().subscribe((d) => this.asignaciones.set(d));
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
            : (this.extraerMensajeApi(err) ?? 'Error al asignar docente.')
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

  private extraerMensajeApi(err: HttpErrorResponse): string | null {
    const body = err.error;
    if (body && typeof body === 'object' && 'message' in body && body.message) {
      return String(body.message);
    }
    if (typeof body === 'string' && body.trim()) {
      return body;
    }
    return null;
  }
}
