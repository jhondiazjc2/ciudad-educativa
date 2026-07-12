import { Component, computed, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ColegioMatriculaRanking, DocentesPorSector, EstudiantesPorEdad } from '../../models';
import { formatearSector } from '../../utils/texto';
import { ReportBarChart, ReportBarItem } from '../../components/report-bar-chart/report-bar-chart';
import { ReportDonutChart, ReportDonutItem } from '../../components/report-donut-chart/report-donut-chart';

type LimiteRanking = 3 | 5;

@Component({
  selector: 'app-reportes',
  imports: [DatePipe, ReportBarChart, ReportDonutChart],
  templateUrl: './reportes.html'
})
export class Reportes implements OnInit {
  protected readonly formatearSector = formatearSector;

  readonly edades = signal<EstudiantesPorEdad | null>(null);
  readonly docentes = signal<DocentesPorSector | null>(null);
  readonly colegiosRanking = signal<ColegioMatriculaRanking[]>([]);
  readonly limiteRanking = signal<LimiteRanking>(5);
  readonly error = signal('');
  readonly cargando = signal(true);
  readonly ultimaActualizacion = signal<Date | null>(null);
  readonly edadSeleccionada = signal<string | null>(null);
  readonly sectorSeleccionado = signal<string | null>(null);

  readonly edadesChart = computed<ReportBarItem[]>(() => {
    const data = this.edades();
    if (!data) return [];

    return [
      { id: '3-7', label: '3 - 7 años', value: data.entre3y7, colorClass: 'bg-accent' },
      { id: '8-12', label: '8 - 12 años', value: data.entre8y12, colorClass: 'bg-primary' },
      { id: '12+', label: 'Mayores de 12', value: data.mayoresDe12, colorClass: 'bg-[#4DA3D9]' }
    ];
  });

  readonly docentesChart = computed<ReportDonutItem[]>(() => {
    const data = this.docentes();
    if (!data) return [];

    return [
      { id: 'publico', label: 'Público', value: data.publico, color: '#002D56' },
      { id: 'privado', label: 'Privado', value: data.privado, color: '#0077B6' }
    ];
  });

  readonly detalleEdad = computed(() => {
    const id = this.edadSeleccionada();
    const data = this.edades();
    if (!id || !data) return null;

    const item = this.edadesChart().find((entry) => entry.id === id);
    if (!item) return null;

    return {
      ...item,
      porcentaje: data.total > 0 ? Math.round((item.value / data.total) * 100) : 0
    };
  });

  readonly detalleSector = computed(() => {
    const id = this.sectorSeleccionado();
    const data = this.docentes();
    if (!id || !data) return null;

    const item = this.docentesChart().find((entry) => entry.id === id);
    if (!item) return null;

    return {
      ...item,
      porcentaje: data.total > 0 ? Math.round((item.value / data.total) * 100) : 0
    };
  });

  readonly colegioTop = computed(() => this.colegiosRanking()[0] ?? null);

  readonly maxEstudiantesRanking = computed(() =>
    Math.max(...this.colegiosRanking().map((c) => c.totalEstudiantes), 0)
  );

  constructor(private api: ApiService, protected auth: AuthService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.error.set('');
    this.cargando.set(true);
    this.edades.set(null);
    this.docentes.set(null);
    this.colegiosRanking.set([]);
    this.edadSeleccionada.set(null);
    this.sectorSeleccionado.set(null);

    let pendientes = this.auth.isAdmin() ? 3 : 2;

    const finalizar = () => {
      pendientes -= 1;
      if (pendientes <= 0) {
        this.cargando.set(false);
        this.ultimaActualizacion.set(new Date());
      }
    };

    this.api.getEstudiantesPorEdad().subscribe({
      next: (data) => this.edades.set(data),
      error: () => this.error.set('No se pudo cargar estudiantes por edad.'),
      complete: finalizar
    });

    if (this.auth.isAdmin()) {
      this.api.getDocentesPorSector().subscribe({
        next: (data) => this.docentes.set(data),
        error: () => this.error.set('No se pudo cargar docentes por sector.'),
        complete: finalizar
      });
    }

    this.cargarRanking(finalizar);
  }

  cambiarLimiteRanking(limite: LimiteRanking): void {
    if (this.limiteRanking() === limite) return;
    this.limiteRanking.set(limite);
    this.cargarRanking();
  }

  participacionRanking(totalEstudiantes: number): number {
    const total = this.edades()?.total ?? 0;
    if (total <= 0) return 0;
    return Math.round((totalEstudiantes / total) * 100);
  }

  barraRanking(totalEstudiantes: number): number {
    const max = this.maxEstudiantesRanking();
    if (max <= 0) return 0;
    return Math.round((totalEstudiantes / max) * 100);
  }

  alternarEdad(id: string): void {
    this.edadSeleccionada.update((actual) => (actual === id ? null : id));
  }

  alternarSector(id: string): void {
    this.sectorSeleccionado.update((actual) => (actual === id ? null : id));
  }

  private cargarRanking(onComplete?: () => void): void {
    this.api.getColegiosRankingMatricula(this.limiteRanking()).subscribe({
      next: (data) => this.colegiosRanking.set(data),
      error: () => this.colegiosRanking.set([]),
      complete: () => onComplete?.()
    });
  }
}
