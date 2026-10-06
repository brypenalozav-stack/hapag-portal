import { Component, inject, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PortalLinkService } from '../../../core/services/portal-link.service';

/**
 * Acceso al módulo de Dispute de productos digitales de Hapag-Lloyd (M2-05). Abre el sitio configurado para el
 * país en una pestaña nueva (`rel="noopener noreferrer"`, sin datos del usuario ni del BL en la URL) y lo avisa
 * en el nombre accesible. Sin URL configurada no se muestra.
 * - `sidebar`: enlace del menú lateral.
 * - `card`: tarjeta de los accesos rápidos del dashboard (M1-01).
 */
@Component({
  selector: 'app-dispute-link',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    @if (links.disputeUrl(); as url) {
      @if (variant() === 'sidebar') {
        <a class="sidebar-link" [href]="url" target="_blank" rel="noopener noreferrer" data-testid="dispute-link" (click)="activated.emit()">
          <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8 15c4.418 0 8-3.134 8-7s-3.582-7-8-7-8 3.134-8 7c0 1.76.743 3.37 1.97 4.6-.097 1.016-.417 2.13-.771 2.966-.079.186.074.394.273.362 2.256-.37 3.597-.938 4.18-1.234A9 9 0 0 0 8 15"/>
          </svg>
          <span>{{ 'shared.disputeLink.label' | transloco }}<span class="visually-hidden">{{ 'shared.disputeLink.newTab' | transloco }}</span></span>
          <svg aria-hidden="true" focusable="false" class="ms-auto" xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
            <path fill-rule="evenodd" d="M8.636 3.5a.5.5 0 0 0-.5-.5H1.5A1.5 1.5 0 0 0 0 4.5v10A1.5 1.5 0 0 0 1.5 16h10a1.5 1.5 0 0 0 1.5-1.5V7.864a.5.5 0 0 0-1 0V14.5a.5.5 0 0 1-.5.5h-10a.5.5 0 0 1-.5-.5v-10a.5.5 0 0 1 .5-.5h6.636a.5.5 0 0 0 .5-.5"/>
            <path fill-rule="evenodd" d="M16 .5a.5.5 0 0 0-.5-.5h-5a.5.5 0 0 0 0 1h3.793L6.146 9.146a.5.5 0 1 0 .708.708L15 1.707V5.5a.5.5 0 0 0 1 0z"/>
          </svg>
        </a>
      } @else {
        <a class="hl-service" [href]="url" target="_blank" rel="noopener noreferrer" data-testid="dispute-link-card">
          <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8 15c4.418 0 8-3.134 8-7s-3.582-7-8-7-8 3.134-8 7c0 1.76.743 3.37 1.97 4.6-.097 1.016-.417 2.13-.771 2.966-.079.186.074.394.273.362 2.256-.37 3.597-.938 4.18-1.234A9 9 0 0 0 8 15"/>
          </svg>
          <span>
            <span class="hl-service__title">
              {{ 'shared.disputeLink.label' | transloco }}
              <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                <path fill-rule="evenodd" d="M8.636 3.5a.5.5 0 0 0-.5-.5H1.5A1.5 1.5 0 0 0 0 4.5v10A1.5 1.5 0 0 0 1.5 16h10a1.5 1.5 0 0 0 1.5-1.5V7.864a.5.5 0 0 0-1 0V14.5a.5.5 0 0 1-.5.5h-10a.5.5 0 0 1-.5-.5v-10a.5.5 0 0 1 .5-.5h6.636a.5.5 0 0 0 .5-.5"/>
                <path fill-rule="evenodd" d="M16 .5a.5.5 0 0 0-.5-.5h-5a.5.5 0 0 0 0 1h3.793L6.146 9.146a.5.5 0 1 0 .708.708L15 1.707V5.5a.5.5 0 0 0 1 0z"/>
              </svg>
              <span class="visually-hidden">{{ 'shared.disputeLink.newTab' | transloco }}</span>
            </span>
            <span class="hl-service__description">{{ 'shared.disputeLink.description' | transloco }}</span>
          </span>
        </a>
      }
    }
  `,
  styles: [':host { display: contents; }'],
})
export class DisputeLinkComponent {
  readonly links = inject(PortalLinkService);

  variant = input<'sidebar' | 'card'>('sidebar');
  /** Se emite al activar el enlace del menú lateral (cierra el menú móvil). */
  activated = output<void>();
}
