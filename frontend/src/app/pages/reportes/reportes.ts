import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ColegioMayorMatricula, ContratoPorVencer, DocentesPorSector, EstudiantesPorEdad } from '../../models';

@Component({
  selector: 'app-reportes',
  imports: [DatePipe],
  templateUrl: './reportes.html',
  styleUrl: './reportes.css'
})
export class Reportes implements OnInit {
  edades: EstudiantesPorEdad | null = null;
  docentes: DocentesPorSector | null = null;
  colegioTop: ColegioMayorMatricula | null = null;
  contratosPorVencer: ContratoPorVencer[] = [];
  error = '';

  constructor(private api: ApiService, protected auth: AuthService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.error = '';
    this.api.getEstudiantesPorEdad().subscribe({
      next: (data) => (this.edades = data),
      error: () => (this.error = 'No se pudo conectar con el backend. Verifica que la API este corriendo.')
    });

    if (this.auth.isAdmin()) {
      this.api.getDocentesPorSector().subscribe({ next: (data) => (this.docentes = data) });
    }

    this.api.getColegioMayorMatricula().subscribe({ next: (data) => (this.colegioTop = data) });
    this.api.getContratosPorVencer().subscribe({ next: (data) => (this.contratosPorVencer = data) });
  }
}
