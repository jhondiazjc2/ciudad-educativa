import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectField, toColegioSelectOptions, toSelectOptions } from '../../components/select-field/select-field';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoColegioItem, CatalogoItem, HistoricoEstudiante, MatriculaResponse } from '../../models';

@Component({
  selector: 'app-consultar',
  imports: [FormsModule, SelectField],
  templateUrl: './consultar.html'
})
export class Consultar implements OnInit {
  colegios: CatalogoColegioItem[] = [];
  grados: CatalogoItem[] = [];

  codigoDane = '';
  gradoId = 0;
  anio = new Date().getFullYear();
  anioMin = 2000;
  anioMax = new Date().getFullYear() + 20;

  resultados: MatriculaResponse[] = [];
  historico: HistoricoEstudiante[] = [];
  estudianteSeleccionado = '';
  error = '';
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  get colegioOptions() {
    return toColegioSelectOptions(this.colegios);
  }

  get gradoOptions() {
    return toSelectOptions(this.grados);
  }

  ngOnInit(): void {
    this.api.getColegios().subscribe({
      next: (d) => {
        this.colegios = d;
        if (this.auth.isColegio() && this.auth.getCodigoDane()) {
          this.codigoDane = this.auth.getCodigoDane()!;
          this.colegioBloqueado = true;
        }
      },
      error: () => (this.error = 'No se pudieron cargar los colegios.')
    });
    this.api.getGrados().subscribe({
      next: (d) => (this.grados = d),
      error: () => (this.error = 'No se pudieron cargar los grados.')
    });
    this.api.getAnioAcademicoConfig().subscribe({
      next: (cfg) => {
        this.anio = cfg.vigente;
        this.anioMin = cfg.minimo;
        this.anioMax = cfg.maximo;
      },
      error: () => (this.error = 'No se pudo cargar la configuración del año académico.')
    });
  }

  buscar(): void {
    this.error = '';
    this.historico = [];
    this.estudianteSeleccionado = '';

    if (!this.codigoDane || !this.gradoId || !this.anio) {
      this.error = 'Selecciona colegio, grado y año.';
      return;
    }

    this.api.consultarMatriculas(this.codigoDane, this.gradoId, this.anio).subscribe({
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
