import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    <div class="hl-spinner-overlay">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">{{ 'shared.loadingSpinner.label' | transloco }}</span>
      </div>
    </div>
  `,
})
export class LoadingSpinnerComponent {}
