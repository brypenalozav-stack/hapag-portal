import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ShipmentIssuance } from '../../../core/models/shipment.model';
import { BL_ISSUANCE_STATUS_CLASS, BL_ISSUANCE_STATUS_KEYS, TRANSPORT_DOCUMENT_TYPE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Estado de emisión del documento de transporte (M2-02, CL-IMP-14, BO-IMP-14): tipo (BL, SWB o EBL, con la
 * plataforma del EBL, p. ej. Wave BL), estado tal como lo registra el origen y su fecha. Si el origen no
 * responde, lo dice y muestra el último estado conocido como tal, no como vigente (NF-11), con la opción de
 * volver a consultar.
 */
@Component({
  selector: 'app-shipment-issuance',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe],
  template: `
    @if (current(); as i) {
      <section class="hl-card p-4 mb-4" aria-labelledby="shipment-issuance-title" data-testid="shipment-issuance">
        <h2 [attr.aria-level]="headingLevel() === 3 ? 3 : null" id="shipment-issuance-title" class="section-title mb-3">{{ 'shipments.detail.issuance.title' | transloco }}</h2>
        @if (i.available) {
          <dl class="row g-3 mb-0">
            <div class="col-sm-6 col-lg-3">
              <dt class="detail-label">{{ 'shipments.detail.issuance.documentType' | transloco }}</dt>
              <dd class="detail-value">
                {{ i.documentType ? (i.documentType | codeLabel: documentTypeKeys) : ('shipments.detail.issuance.notInformed' | transloco) }}
                @if (i.eblPlatform) {
                  <span class="d-block small fw-normal">{{ 'shipments.detail.issuance.platform' | transloco: { platform: i.eblPlatform } }}</span>
                }
              </dd>
            </div>
            <div class="col-sm-6 col-lg-3">
              <dt class="detail-label">{{ 'shipments.detail.issuance.status' | transloco }}</dt>
              <dd class="mb-0">
                @if (i.status) {
                  <span class="hl-badge" [class]="statusClass[i.status] || 'hl-badge--processing'" data-testid="issuance-status">{{ i.status | codeLabel: statusKeys }}</span>
                } @else {
                  {{ 'shipments.detail.issuance.notInformed' | transloco }}
                }
              </dd>
            </div>
            <div class="col-sm-6 col-lg-3">
              <dt class="detail-label">{{ 'shipments.detail.issuance.statusAt' | transloco }}</dt>
              <dd class="detail-value">{{ i.statusAt ? (i.statusAt | hlDate: 'datetime') : ('shipments.detail.issuance.notInformed' | transloco) }}</dd>
            </div>
            <div class="col-sm-6 col-lg-3">
              <dt class="detail-label">{{ 'shipments.detail.issuance.place' | transloco }}</dt>
              <dd class="detail-value">{{ i.issuancePlace ?? ('shipments.detail.issuance.notInformed' | transloco) }}</dd>
            </div>
          </dl>
          <p class="small text-muted mt-3 mb-0">{{ 'shipments.detail.issuance.source' | transloco: { source: i.source, date: (i.retrievedAt | hlDate: 'datetime') } }}</p>
        } @else {
          <!-- NF-11: el origen no respondió; el último estado conocido no se presenta como vigente -->
          <div class="alert alert-warning mb-3" role="alert" data-testid="issuance-unavailable">
            {{ 'shipments.detail.issuance.unavailable' | transloco: { source: i.source } }}
          </div>
          @if (i.lastKnown?.status; as lastStatus) {
            <p class="mb-3" data-testid="issuance-last-known">
              {{ 'shipments.detail.issuance.lastKnown' | transloco: {
                type: (i.lastKnown?.documentType ? (i.lastKnown?.documentType | codeLabel: documentTypeKeys) : ('shipments.detail.issuance.notInformed' | transloco)),
                status: (lastStatus | codeLabel: statusKeys),
                date: (i.lastKnown?.statusAt ? (i.lastKnown?.statusAt | hlDate: 'datetime') : ('shipments.detail.issuance.notInformed' | transloco))
              } }}
            </p>
          }
          <button type="button" class="btn btn-sm btn-outline-secondary" [disabled]="refreshing()" (click)="refresh()">
            {{ 'shipments.detail.issuance.retry' | transloco }}
          </button>
        }
      </section>
    }
  `,
  styleUrl: '../shipment-detail/shipment-detail.scss',
})
export class ShipmentIssuanceComponent {
  private readonly service = inject(ShipmentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  issuance = input.required<ShipmentIssuance>();
  /** Nivel del título: 3 dentro de un grupo del detalle del BL, cuyo h2 es el título del grupo. */
  readonly headingLevel = input<2 | 3>(2);

  readonly documentTypeKeys = TRANSPORT_DOCUMENT_TYPE_KEYS;
  readonly statusKeys = BL_ISSUANCE_STATUS_KEYS;
  readonly statusClass = BL_ISSUANCE_STATUS_CLASS;

  current = signal<ShipmentIssuance | null>(null);
  refreshing = signal(false);

  constructor() {
    effect(() => {
      const issuance = this.issuance();
      untracked(() => this.current.set(issuance));
    });
  }

  /** Vuelve a consultar el origen (GET /shipments/{bl}/issuance). */
  refresh(): void {
    const bl = this.issuance().blNumber;
    this.refreshing.set(true);
    this.service.getIssuance(bl).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (issuance) => {
        this.refreshing.set(false);
        this.current.set(issuance);
        this.announcer.announce(translate(issuance.available ? 'shipments.detail.issuance.refreshed' : 'shipments.detail.issuance.stillUnavailable'));
      },
      error: () => {
        this.refreshing.set(false);
        this.announcer.announce(translate('shipments.detail.issuance.stillUnavailable'), 'assertive');
      },
    });
  }
}
