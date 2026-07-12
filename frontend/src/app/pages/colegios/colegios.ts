import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { extraerMensajeApi } from '../../utils/api-error';
import { finalize } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { ColegioItem } from '../../models';
import { formatearSector } from '../../utils/texto';

@Component({
  selector: 'app-colegios',
  imports: [FormsModule],
  templateUrl: './colegios.html'
})
export class Colegios implements OnInit {
  protected readonly formatearSector = formatearSector;
  readonly colegios = signal<ColegioItem[]>([]);
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  readonly cargando = signal(true);

  codigoDane = '';
  nombre = '';
  sector = 'Publico';
  editandoCodigoDane: string | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.api.getColegiosAdmin().pipe(
      finalize(() => this.cargando.set(false))
    ).subscribe({
      next: (data) => this.colegios.set(data),
      error: () => this.error.set('No se pudo cargar la lista de colegios.')
    });
  }

  guardar(): void {
    this.mensaje.set('');
    this.error.set('');

    if (!this.editandoCodigoDane && !this.codigoDane.trim()) {
      this.error.set('Ingresa el código DANE.');
      return;
    }
    if (!this.nombre.trim()) {
      this.error.set('Ingresa el nombre del colegio.');
      return;
    }

    this.enviando.set(true);
    const request = this.editandoCodigoDane
      ? this.api.actualizarColegio(this.editandoCodigoDane, {
          nombre: this.nombre.trim(),
          sector: this.sector
        })
      : this.api.crearColegio({
          codigoDane: this.codigoDane.replace(/\D/g, ''),
          nombre: this.nombre.trim(),
          sector: this.sector
        });

    request.pipe(finalize(() => this.enviando.set(false))).subscribe({
      next: () => {
        this.mensaje.set(
          this.editandoCodigoDane ? 'Colegio actualizado correctamente.' : 'Colegio creado correctamente.'
        );
        this.limpiarFormulario();
        this.cargar();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(extraerMensajeApi(err) ?? 'Error al guardar el colegio.');
      }
    });
  }

  editar(colegio: ColegioItem): void {
    this.mensaje.set('');
    this.error.set('');
    this.editandoCodigoDane = colegio.codigoDane;
    this.codigoDane = colegio.codigoDane;
    this.nombre = colegio.nombre;
    this.sector = colegio.sector;
  }

  cancelarEdicion(): void {
    this.limpiarFormulario();
    this.mensaje.set('');
    this.error.set('');
  }

  eliminar(colegio: ColegioItem): void {
    if (!confirm(`¿Eliminar el colegio "${colegio.nombre}"?`)) return;

    this.mensaje.set('');
    this.error.set('');
    this.api.eliminarColegio(colegio.codigoDane).subscribe({
      next: () => {
        this.mensaje.set('Colegio eliminado correctamente.');
        if (this.editandoCodigoDane === colegio.codigoDane) this.limpiarFormulario();
        this.cargar();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(extraerMensajeApi(err) ?? 'No se pudo eliminar el colegio.');
      }
    });
  }

  private limpiarFormulario(): void {
    this.editandoCodigoDane = null;
    this.codigoDane = '';
    this.nombre = '';
    this.sector = 'Publico';
  }
}
