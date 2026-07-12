import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectField, SelectOption } from '../select-field/select-field';
import { CatalogoDocumentoItem } from '../../models';

@Component({
  selector: 'app-matricula-form',
  imports: [FormsModule, SelectField],
  templateUrl: './matricula-form.html'
})
export class MatriculaForm {
  @Input({ required: true }) nombre!: string;
  @Output() nombreChange = new EventEmitter<string>();

  @Input({ required: true }) tipoDocumento!: string;
  @Output() tipoDocumentoChange = new EventEmitter<string>();

  @Input({ required: true }) numeroDocumento!: string;
  @Output() numeroDocumentoChange = new EventEmitter<string>();

  @Input({ required: true }) fechaNacimiento!: string;
  @Output() fechaNacimientoChange = new EventEmitter<string>();

  @Input({ required: true }) codigoDane!: string;
  @Output() codigoDaneChange = new EventEmitter<string>();

  @Input({ required: true }) gradoId!: number;
  @Output() gradoIdChange = new EventEmitter<number>();

  @Input({ required: true }) grupoId!: number;
  @Output() grupoIdChange = new EventEmitter<number>();

  @Input({ required: true }) anio!: number;
  @Output() anioChange = new EventEmitter<number>();

  @Input({ required: true }) anioMin!: number;
  @Input({ required: true }) anioMax!: number;
  @Input({ required: true }) tiposDocumento!: CatalogoDocumentoItem[];
  @Input({ required: true }) colegioOptions!: SelectOption[];
  @Input({ required: true }) gradoOptions!: SelectOption[];
  @Input({ required: true }) grupoOptions!: SelectOption[];

  @Input() enEdicion = false;
  @Input() editandoMatriculaId: number | null = null;
  @Input() colegioBloqueado = false;
  @Input() enviando = false;
  @Input() buscandoEstudiante = false;

  @Output() submitForm = new EventEmitter<void>();
  @Output() cancelar = new EventEmitter<void>();
  @Output() buscarEstudiante = new EventEmitter<void>();
  @Output() colegioChange = new EventEmitter<void>();
  @Output() gradoChange = new EventEmitter<void>();
}
