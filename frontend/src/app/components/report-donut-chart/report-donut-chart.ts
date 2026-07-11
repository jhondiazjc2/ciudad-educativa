import { Component, computed, input, output, signal } from '@angular/core';

export interface ReportDonutItem {
  id: string;
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-report-donut-chart',
  templateUrl: './report-donut-chart.html'
})
export class ReportDonutChart {
  readonly items = input.required<ReportDonutItem[]>();
  readonly selectedId = input<string | null>(null);

  readonly selectItem = output<string>();

  readonly hoveredId = signal<string | null>(null);

  readonly total = computed(() =>
    this.items().reduce((sum, item) => sum + item.value, 0)
  );

  readonly foco = computed(() => {
    const id = this.hoveredId() ?? this.selectedId();
    if (!id) return null;
    return this.items().find((item) => item.id === id) ?? null;
  });

  readonly gradiente = computed(() => {
    const total = this.total();
    if (total <= 0) return 'conic-gradient(#E2EEF7 0deg 360deg)';

    let acumulado = 0;
    const partes = this.items().map((item) => {
      const inicio = (acumulado / total) * 100;
      acumulado += item.value;
      const fin = (acumulado / total) * 100;
      const opaco = this.resaltar(item.id) ? 1 : 0.35;
      return `${this.colorConOpacidad(item.color, opaco)} ${inicio}% ${fin}%`;
    });

    return `conic-gradient(${partes.join(', ')})`;
  });

  porcentaje(value: number): number {
    const total = this.total();
    return total > 0 ? Math.round((value / total) * 100) : 0;
  }

  resaltar(id: string): boolean {
    const hovered = this.hoveredId();
    const selected = this.selectedId();
    if (!hovered && !selected) return true;
    return id === hovered || id === selected;
  }

  private colorConOpacidad(hex: string, alpha: number): string {
    const limpio = hex.replace('#', '');
    const r = parseInt(limpio.slice(0, 2), 16);
    const g = parseInt(limpio.slice(2, 4), 16);
    const b = parseInt(limpio.slice(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }
}
