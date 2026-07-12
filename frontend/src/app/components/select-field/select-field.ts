import {
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  signal,
  ViewChild
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type SelectValue = string | number;

export interface SelectOption {
  id: SelectValue;
  label: string;
}

export function toSelectOptions(items: { id: number; nombre: string }[]): SelectOption[] {
  return items.map((item) => ({ id: item.id, label: item.nombre }));
}

export function toDocenteSelectOptions(items: { id: number; tipoDocumento: string; numeroDocumento: string; nombre: string }[]): SelectOption[] {
  return items.map((item) => ({
    id: item.id,
    label: `${item.nombre} (${item.tipoDocumento} ${item.numeroDocumento})`
  }));
}

export function toColegioSelectOptions(items: { codigoDane: string; nombre: string }[]): SelectOption[] {
  return items.map((item) => ({ id: item.codigoDane, label: `${item.codigoDane} — ${item.nombre}` }));
}

@Component({
  selector: 'app-select-field',
  templateUrl: './select-field.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectField),
      multi: true
    }
  ]
})
export class SelectField implements ControlValueAccessor {
  @Input() options: SelectOption[] = [];
  @Input() placeholder = 'Seleccionar...';
  @Input() searchPlaceholder = 'Buscar...';
  @Input() searchable = false;
  @Input() disabled = false;

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  readonly open = signal(false);
  readonly filterQuery = signal('');
  value: SelectValue = '';

  private onChange: (value: SelectValue) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private host: ElementRef<HTMLElement>) {}

  get selectedLabel(): string {
    if (this.isEmpty(this.value)) return this.placeholder;
    return this.options.find((o) => o.id === this.value)?.label ?? this.placeholder;
  }

  get filteredOptions(): SelectOption[] {
    const query = this.normalize(this.filterQuery());
    if (!query) return this.options;
    return this.options.filter((option) => this.normalize(option.label).includes(query));
  }

  onFilterInput(event: Event): void {
    this.filterQuery.set((event.target as HTMLInputElement).value);
  }

  private normalize(text: string): string {
    return text
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '');
  }

  private resetFilter(): void {
    this.filterQuery.set('');
  }

  private focusSearchInput(): void {
    setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
  }

  writeValue(value: SelectValue | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: SelectValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (isDisabled) this.open.set(false);
  }

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    if (this.disabled) return;
    const willOpen = !this.open();
    this.open.set(willOpen);
    if (!willOpen) {
      this.resetFilter();
    } else if (this.searchable) {
      this.resetFilter();
      this.focusSearchInput();
    }
    this.onTouched();
  }

  choose(id: SelectValue): void {
    this.value = id;
    this.onChange(id);
    this.onTouched();
    this.open.set(false);
    this.resetFilter();
  }

  private isEmpty(value: SelectValue): boolean {
    return value === '' || value === 0 || value === null || value === undefined;
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: Event): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
      this.resetFilter();
    }
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.open.set(false);
    this.resetFilter();
  }
}
