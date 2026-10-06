import { Component, computed, input } from '@angular/core';
import { SHIPMENT_ACCESS_SOURCE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../pipes/code-label.pipe';

/** Clase de la insignia por origen del acceso; el texto siempre acompaña al color (1.4.1). */
const SOURCE_CLASS: Record<string, string> = {
  Own: 'hl-badge--active',
  Grant: 'hl-badge--processing',
  SelfAssociated: 'hl-badge--processing',
  OpenAccess: 'hl-badge--pending',
  Admin: 'hl-badge--processing',
};

/**
 * Origen del acceso al embarque: propio, acceso otorgado (M1-12), autoasociado (M1-18), acceso
 * abierto por número de BL (M1-17) o administrador interno. Se usa en el listado y en el detalle.
 */
@Component({
  selector: 'app-access-source-badge',
  standalone: true,
  imports: [CodeLabelPipe],
  template: `<span class="hl-badge" [class]="badgeClass()">{{ source() | codeLabel: keys }}</span>`,
})
export class AccessSourceBadgeComponent {
  source = input.required<string>();

  readonly keys = SHIPMENT_ACCESS_SOURCE_KEYS;

  badgeClass = computed(() => SOURCE_CLASS[this.source()] ?? 'hl-badge--processing');
}
