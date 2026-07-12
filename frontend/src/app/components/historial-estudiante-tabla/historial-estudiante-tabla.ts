import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HistoricoEstudiante } from '../../models';

@Component({
  selector: 'app-historial-estudiante-tabla',
  imports: [DatePipe],
  templateUrl: './historial-estudiante-tabla.html'
})
export class HistorialEstudianteTabla {
  @Input({ required: true }) registros!: HistoricoEstudiante[];
  @Input() compact = false;
}
