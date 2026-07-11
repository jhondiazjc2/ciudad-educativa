import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectField, toSelectOptions } from '../../components/select-field/select-field';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoItem, HistoricoEstudiante, MatriculaResponse } from '../../models';

@Component({
  selector: 'app-consultar',
  imports: [FormsModule, SelectField],
  templateUrl: './consultar.html'
})
export class Consultar implements OnInit {
  colegios: CatalogoItem[] = [];
  grados: CatalogoItem[] = [];
  anios: CatalogoItem[] = [];

  colegioId = 0;
  gradoId = 0;
  anioAcademicoId = 0;

  resultados: MatriculaResponse[] = [];
  historico: HistoricoEstudiante[] = [];
  estudianteSeleccionado = '';
  error = '';
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  get colegioOptions() {
    return toSelectOptions(this.colegios);
  }

  get gradoOptions() {
    return toSelectOptions(this.grados);
  }

  get anioOptions() {
    return toSelectOptions(this.anios);
  }

  ngOnInit(): void {
    this.api.getColegios().subscribe({
      next: (d) => {
        this.colegios = d;
        if (this.auth.isColegio() && this.auth.getColegioId()) {
          this.colegioId = this.auth.getColegioId()!;
          this.colegioBloqueado = true;
        }
      },
      error: () => (this.error = 'No se pudieron cargar los colegios.')
    });
    this.api.getGrados().subscribe({
      next: (d) => (this.grados = d),
      error: () => (this.error = 'No se pudieron cargar los grados.')
    });
    this.api.getAnios().subscribe({
      next: (d) => (this.anios = d),
      error: () => (this.error = 'No se pudieron cargar los años académicos.')
    });
  }

  buscar(): void {
    this.error = '';
    this.historico = [];
    this.estudianteSeleccionado = '';

    if (!this.colegioId || !this.gradoId || !this.anioAcademicoId) {
      this.error = 'Selecciona colegio, grado y año.';
      return;
    }

    this.api.consultarMatriculas(this.colegioId, this.gradoId, this.anioAcademicoId).subscribe({
      next: (data) => (this.resultados = data),
      error: (err) => {
        this.error = err.status === 403
          ? 'No tiene permiso para consultar este colegio.'
          : 'Error al consultar estudiantes.';
      }
    });
  }

  verHistorico(estudianteId: number, nombre: string): void {
    this.estudianteSeleccionado = nombre;
    this.api.getHistorico(estudianteId).subscribe({
      next: (data) => (this.historico = data),
      error: (err) => {
        if (err.status === 403) this.error = 'No tiene permiso para ver el histórico de este estudiante.';
      }
    });
  }
}
