import {
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  signal
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
  @Input() disabled = false;

  readonly open = signal(false);
  value: SelectValue = '';

  private onChange: (value: SelectValue) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private host: ElementRef<HTMLElement>) {}

  get selectedLabel(): string {
    if (this.isEmpty(this.value)) return this.placeholder;
    return this.options.find((o) => o.id === this.value)?.label ?? this.placeholder;
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
    this.open.update((v) => !v);
    this.onTouched();
  }

  choose(id: SelectValue): void {
    this.value = id;
    this.onChange(id);
    this.onTouched();
    this.open.set(false);
  }

  private isEmpty(value: SelectValue): boolean {
    return value === '' || value === 0 || value === null || value === undefined;
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: Event): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.open.set(false);
  }
}
