import { Component, computed, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ColegioMayorMatricula, ContratoPorVencer, DocentesPorSector, EstudiantesPorEdad } from '../../models';
import { formatearSector } from '../../utils/texto';
import { ReportBarChart, ReportBarItem } from '../../components/report-bar-chart/report-bar-chart';
import { ReportDonutChart, ReportDonutItem } from '../../components/report-donut-chart/report-donut-chart';

type FiltroContrato = 'todos' | 'urgente' | 'proximo';

@Component({
  selector: 'app-reportes',
  imports: [DatePipe, ReportBarChart, ReportDonutChart],
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
  readonly ultimaActualizacion = signal<Date | null>(null);
  readonly edadSeleccionada = signal<string | null>(null);
  readonly sectorSeleccionado = signal<string | null>(null);
  readonly contratoExpandido = signal<number | null>(null);
  readonly filtroContrato = signal<FiltroContrato>('todos');

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

  readonly participacionColegio = computed(() => {
    const top = this.colegioTop();
    const total = this.edades()?.total ?? 0;
    if (!top || total <= 0) return 0;
    return Math.round((top.totalEstudiantes / total) * 100);
  });

  readonly contratosFiltrados = computed(() => {
    const filtro = this.filtroContrato();
    const lista = this.contratosPorVencer();

    if (filtro === 'urgente') return lista.filter((c) => c.diasRestantes <= 7);
    if (filtro === 'proximo') return lista.filter((c) => c.diasRestantes <= 14);
    return lista;
  });

  readonly resumenContratos = computed(() => ({
    total: this.contratosPorVencer().length,
    urgentes: this.contratosPorVencer().filter((c) => c.diasRestantes <= 7).length
  }));

  constructor(private api: ApiService, protected auth: AuthService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.error.set('');
    this.cargando.set(true);
    this.edades.set(null);
    this.docentes.set(null);
    this.colegioTop.set(null);
    this.edadSeleccionada.set(null);
    this.sectorSeleccionado.set(null);
    this.contratoExpandido.set(null);

    let pendientes = this.auth.isAdmin() ? 4 : 3;

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

    this.api.getColegioMayorMatricula().subscribe({
      next: (data) => this.colegioTop.set(data),
      error: () => this.colegioTop.set(null),
      complete: finalizar
    });

    this.api.getContratosPorVencer().subscribe({
      next: (data) => this.contratosPorVencer.set(data),
      error: () => this.contratosPorVencer.set([]),
      complete: finalizar
    });
  }

  alternarEdad(id: string): void {
    this.edadSeleccionada.update((actual) => (actual === id ? null : id));
  }

  alternarSector(id: string): void {
    this.sectorSeleccionado.update((actual) => (actual === id ? null : id));
  }

  alternarContrato(docenteId: number): void {
    this.contratoExpandido.update((actual) => (actual === docenteId ? null : docenteId));
  }

  filtrarContratos(filtro: FiltroContrato): void {
    this.filtroContrato.set(filtro);
    this.contratoExpandido.set(null);
  }

  urgenciaContrato(dias: number): 'alta' | 'media' | 'baja' {
    if (dias <= 7) return 'alta';
    if (dias <= 14) return 'media';
    return 'baja';
  }
}
