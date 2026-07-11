import { Component, computed, input, output, signal } from '@angular/core';

export interface ReportBarItem {
  id: string;
  label: string;
  value: number;
  colorClass: string;
}

@Component({
  selector: 'app-report-bar-chart',
  templateUrl: './report-bar-chart.html'
})
export class ReportBarChart {
  readonly items = input.required<ReportBarItem[]>();
  readonly total = input(0);
  readonly selectedId = input<string | null>(null);
  readonly animated = input(true);

  readonly selectItem = output<string>();

  readonly hoveredId = signal<string | null>(null);

  readonly maxValue = computed(() =>
    Math.max(1, ...this.items().map((item) => item.value))
  );

  porcentaje(value: number): number {
    const base = this.total() || this.items().reduce((sum, item) => sum + item.value, 0);
    return base > 0 ? Math.round((value / base) * 100) : 0;
  }

  anchoBarra(value: number): number {
    return (value / this.maxValue()) * 100;
  }

  resaltar(id: string): boolean {
    const hovered = this.hoveredId();
    const selected = this.selectedId();
    if (!hovered && !selected) return true;
    return id === hovered || id === selected;
  }
}
