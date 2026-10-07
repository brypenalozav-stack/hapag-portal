import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ShipGraphicComponent } from '../ship-graphic/ship-graphic';

/** Carga de una sección: la nave del portal con el texto "Cargando..." anunciado a lectores de pantalla. */
@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, ShipGraphicComponent],
  template: `
    <div class="hl-spinner-overlay" role="status">
      <app-ship-graphic width="6rem" />
      <span class="visually-hidden">{{ 'shared.loadingSpinner.label' | transloco }}</span>
    </div>
  `,
})
export class LoadingSpinnerComponent {}
