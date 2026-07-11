import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoItem, DocenteColegio } from '../../models';

@Component({
  selector: 'app-docentes',
  imports: [FormsModule],
  templateUrl: './docentes.html',
  styleUrl: './docentes.css'
})
export class Docentes implements OnInit {
  docentes: CatalogoItem[] = [];
  colegios: CatalogoItem[] = [];
  asignaciones: DocenteColegio[] = [];

  docenteId = 0;
  colegioId = 0;
  mensaje = '';
  error = '';
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  ngOnInit(): void {
    this.api.getDocentes().subscribe((d) => (this.docentes = d));
    this.api.getColegios().subscribe((d) => {
      this.colegios = d;
      if (this.auth.isColegio() && this.auth.getColegioId()) {
        this.colegioId = this.auth.getColegioId()!;
        this.colegioBloqueado = true;
      }
    });
    this.cargarAsignaciones();
  }

  cargarAsignaciones(): void {
    this.api.getAsignaciones().subscribe((d) => (this.asignaciones = d));
  }

  asignar(): void {
    this.mensaje = '';
    this.error = '';

    if (!this.docenteId || !this.colegioId) {
      this.error = 'Selecciona docente y colegio.';
      return;
    }

    this.api.asignarDocente({ docenteId: this.docenteId, colegioId: this.colegioId }).subscribe({
      next: () => {
        this.mensaje = 'Docente asignado correctamente.';
        this.docenteId = 0;
        if (!this.colegioBloqueado) this.colegioId = 0;
        this.cargarAsignaciones();
      },
      error: (err) => {
        this.error = err.status === 403
          ? 'No tiene permiso para asignar docentes a este colegio.'
          : 'Error al asignar docente.';
      }
    });
  }

  quitar(id: number): void {
    this.api.desactivarAsignacion(id).subscribe({
      next: () => this.cargarAsignaciones(),
      error: () => (this.error = 'No tiene permiso para quitar esta asignacion.')
    });
  }
}
