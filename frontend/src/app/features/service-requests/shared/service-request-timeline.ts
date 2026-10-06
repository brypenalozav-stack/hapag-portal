import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ServiceRequestEvent } from '../../../core/models/service-request.model';
import { SERVICE_ACTOR_KIND_KEYS, SERVICE_REQUEST_STATUS_CLASS, SERVICE_REQUEST_STATUS_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Línea de tiempo de una solicitud de servicio: cada cambio de estado o nota con fecha y hora en el huso del país de
 * la operación (con la zona, NF-22), quién lo hizo (cliente, equipo interno o sistema, incluido el pago que liberó la
 * solicitud) y las notas. Lista ordenada del evento más antiguo al más reciente.
 */
@Component({
  selector: 'app-service-request-timeline',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe],
  template: `
    @if (sorted().length > 0) {
      <ol class="hl-timeline" [attr.aria-label]="'serviceRequests.timeline.label' | transloco" data-testid="service-timeline">
        @for (event of sorted(); track event.id) {
          <li class="hl-timeline__item">
            <p class="mb-1">
              <span class="hl-badge" [class]="statusClass[event.toStatus] || 'hl-badge--processing'">{{ event.toStatus | codeLabel: statusKeys }}</span>
              @if (event.fromStatus && event.fromStatus !== event.toStatus) {
                <span class="small text-muted ms-1">{{ 'serviceRequests.timeline.from' | transloco: { status: (event.fromStatus | codeLabel: statusKeys) } }}</span>
              } @else if (event.fromStatus === event.toStatus) {
                <span class="small text-muted ms-1">{{ 'serviceRequests.timeline.note' | transloco }}</span>
              }
            </p>
            <p class="small mb-1">
              <time [attr.datetime]="event.occurredAt">{{ event.occurredAt | hlDate: 'datetime' : timeZone() }}</time>
              <span class="ms-1">{{ 'serviceRequests.timeline.actor' | transloco: { kind: (event.actorKind | codeLabel: actorKeys), name: event.actorName } }}</span>
            </p>
            @if (event.notes) {
              <p class="small mb-0 hl-timeline__notes">{{ event.notes }}</p>
            }
          </li>
        }
      </ol>
    } @else {
      <p class="text-muted mb-0" role="status">{{ 'serviceRequests.timeline.empty' | transloco }}</p>
    }
  `,
  styles: [`
    :host { display: block; }
    .hl-timeline { list-style: none; margin: 0; padding: 0 0 0 1rem; border-left: 2px solid var(--hl-border); }
    .hl-timeline__item { position: relative; padding: 0 0 1rem 0.75rem; }
    .hl-timeline__item::before { content: ''; position: absolute; left: -1.4rem; top: 0.35rem; width: 0.75rem; height: 0.75rem; border-radius: 50%; background-color: var(--hl-border); }
    .hl-timeline__item:last-child { padding-bottom: 0; }
    .hl-timeline__notes { white-space: pre-line; }
  `],
})
export class ServiceRequestTimelineComponent {
  events = input.required<ServiceRequestEvent[]>();
  /** Huso del país de la operación (America/Santiago o America/La_Paz). */
  timeZone = input<string | null>(null);

  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly statusClass = SERVICE_REQUEST_STATUS_CLASS;
  readonly actorKeys = SERVICE_ACTOR_KIND_KEYS;

  sorted = computed(() => [...this.events()].sort((a, b) => a.occurredAt.localeCompare(b.occurredAt)));
}
