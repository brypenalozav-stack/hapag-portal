import { Component, input, computed } from '@angular/core';

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

const STATUS_LABEL_MAP: Record<string, string> = {
  PENDING: 'Pendiente',
  CONFIRMED: 'Confirmado',
  PAID: 'Pagado',
  FAILED: 'Fallido',
  REJECTED: 'Rechazado',
  PROCESSING: 'Procesando',
  ACTIVE: 'Activo',
  RELEASED: 'Liberado',
  HOLD: 'Retenido',
  CLOSED: 'Cerrado',
  EXEMPT: 'Exento',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `
    <span class="hl-badge" [class]="badgeClass()">
      <svg xmlns="http://www.w3.org/2000/svg" width="8" height="8" viewBox="0 0 8 8">
        <circle cx="4" cy="4" r="4" fill="currentColor"/>
      </svg>
      {{ label() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  badgeClass = computed((): string => {
    return STATUS_CLASS_MAP[this.status().toLowerCase()] ?? 'hl-badge--pending';
  });

  label = computed((): string => {
    return STATUS_LABEL_MAP[this.status().toUpperCase()] ?? this.status();
  });
}
