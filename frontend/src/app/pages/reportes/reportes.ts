import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ColegioMayorMatricula, ContratoPorVencer, DocentesPorSector, EstudiantesPorEdad } from '../../models';

import { formatearSector } from '../../utils/texto';

@Component({
  selector: 'app-reportes',
  imports: [DatePipe],
  templateUrl: './reportes.html'
})
export class Reportes implements OnInit {
  protected readonly formatearSector = formatearSector;
  readonly edades = signal<EstudiantesPorEdad | null>(null);
  readonly docentes = signal<DocentesPorSector | null>(null);
  readonly colegioTop = signal<ColegioMayorMatricula | null>(null);
  readonly contratosPorVencer = signal<ContratoPorVencer[]>([]);
  readonly error = signal('');
  readonly cargando = signal(true);

  constructor(private api: ApiService, protected auth: AuthService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    // Peticiones en paralelo. El backend filtra por JWT (Admin = ciudad, Colegio = su colegio).
    // cargando se apaga cuando termina contratos-por-vencer, no cuando terminan todas las tarjetas.
    // docentes-por-sector solo se pide si isAdmin(); el endpoint tambien exige rol Admin.
    this.error.set('');
    this.cargando.set(true);
    this.edades.set(null);
    this.docentes.set(null);
    this.colegioTop.set(null);

    this.api.getEstudiantesPorEdad().subscribe({
      next: (data) => this.edades.set(data),
      error: () => this.error.set('No se pudo cargar estudiantes por edad.')
    });

    if (this.auth.isAdmin()) {
      this.api.getDocentesPorSector().subscribe({
        next: (data) => this.docentes.set(data),
        error: () => this.error.set('No se pudo cargar docentes por sector.')
      });
    }

    this.api.getColegioMayorMatricula().subscribe({
      next: (data) => this.colegioTop.set(data),
      error: () => this.colegioTop.set(null)
    });

    this.api.getContratosPorVencer().subscribe({
      next: (data) => {
        this.contratosPorVencer.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }
}
