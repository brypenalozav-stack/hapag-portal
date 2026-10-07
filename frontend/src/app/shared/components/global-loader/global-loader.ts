import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LoadingOverlayService } from '../../../core/services/loading-overlay.service';
import { ShipGraphicComponent } from '../ship-graphic/ship-graphic';

/**
 * Capa de carga a pantalla completa (LoadingOverlayService). Bloquea la interacción mientras dura la
 * operación global y la anuncia con role="status"; la nave se dibuja sobre una tarjeta legible en ambos temas.
 */
@Component({
  selector: 'app-global-loader',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, ShipGraphicComponent],
  template: `
    @if (overlay.visible()) {
      <div class="hl-global-loader" data-testid="global-loader">
        <div class="hl-global-loader__card" role="status">
          <app-ship-graphic width="9rem" />
          <p class="hl-global-loader__text">{{ overlay.message() | transloco }}</p>
        </div>
      </div>
    }
  `,
  styleUrl: './global-loader.scss',
})
export class GlobalLoaderComponent {
  readonly overlay = inject(LoadingOverlayService);
}
