import { Component, Input, output } from '@angular/core';

@Component({
  selector: 'app-accordion-panel',
  templateUrl: './accordion-panel.html'
})
export class AccordionPanel {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() step = '';
  @Input() open = false;
  @Input() panelId = '';

  readonly toggle = output<void>();
}
