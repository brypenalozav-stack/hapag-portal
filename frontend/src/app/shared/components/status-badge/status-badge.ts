import { Component, input, computed } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SHIPMENT_STATUS_KEYS } from '../../../core/i18n/labels';

const STATUS_CLASS_MAP: Record<string, string> = {
  pending: 'hl-badge--pending',
  pendiente: 'hl-badge--pending',
  confirmed: 'hl-badge--confirmed',
  paid: 'hl-badge--confirmed',
  released: 'hl-badge--confirmed',
  confirmado: 'hl-badge--confirmed',
  pagado: 'hl-badge--confirmed',
  failed: 'hl-badge--failed',
  rejected: 'hl-badge--failed',
  fallido: 'hl-badge--failed',
  rechazado: 'hl-badge--failed',
  processing: 'hl-badge--processing',
  procesando: 'hl-badge--processing',
  active: 'hl-badge--active',
  activo: 'hl-badge--active',
};

/** Estados con texto traducido en `status.<code>` (es.json / en.json). Los estados de embarque usan SHIPMENT_STATUS_KEYS; el resto se muestra tal cual. */
const STATUS_LABEL_KEYS: Record<string, string> = {
  PENDING: 'status.pending',
  CONFIRMED: 'status.confirmed',
  PAID: 'status.paid',
  FAILED: 'status.failed',
  REJECTED: 'status.rejected',
  PROCESSING: 'status.processing',
  ACTIVE: 'status.active',
  RELEASED: 'status.released',
  HOLD: 'status.hold',
  CLOSED: 'status.closed',
  EXEMPT: 'status.exempt',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    <span class="hl-badge" [class]="badgeClass()">
      <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="8" height="8" viewBox="0 0 8 8">
        <circle cx="4" cy="4" r="4" fill="currentColor"/>
      </svg>
      @if (labelKey(); as key) {
        {{ key | transloco }}
      } @else {
        {{ status() }}
      }
    </span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  badgeClass = computed((): string => {
    return STATUS_CLASS_MAP[this.status().toLowerCase()] ?? 'hl-badge--pending';
  });

  labelKey = computed((): string | null => {
    return STATUS_LABEL_KEYS[this.status().toUpperCase()] ?? SHIPMENT_STATUS_KEYS[this.status()] ?? null;
  });
}
