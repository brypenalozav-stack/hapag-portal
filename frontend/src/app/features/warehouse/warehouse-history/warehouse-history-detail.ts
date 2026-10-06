import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { WarehouseChangeService } from '../../../core/services/warehouse-change.service';
import { WarehouseChangeEvent, WarehouseChangeTrace } from '../../../core/models/warehouse-change.model';
import {
  DATA_SOURCE_KEYS,
  PAYMENT_STATUS_KEYS,
  WAREHOUSE_CHANGE_STATUS_KEYS,
  WAREHOUSE_HISTORY_EVENT_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { serviceErrorMessage } from '../../service-requests/shared/service-text';

/**
 * Trazabilidad de un cambio de almacén (M3-06): solicitud (individual o línea de una masiva), derecho de cambio
 * gratuito aplicado, cada estado del pago con su número, finalización con el comprobante o anulación; quién lo pidió,
 * quién pagó (razón social y RUT o NIT) y el RUT de facturación. Fechas en el huso del país de la operación.
 */
@Component({
  selector: 'app-warehouse-history-detail',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './warehouse-history-detail.html',
  styles: [`
    :host { display: block; }
    .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }
    .detail-label { font-size: 0.75rem; font-weight: 600; color: var(--hl-text-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.25rem; }
    .detail-value { font-size: 0.95rem; font-weight: 600; color: var(--hl-emphasis); margin-bottom: 0; }
  `],
})
export class WarehouseHistoryDetailComponent implements OnInit {
  private readonly service = inject(WarehouseChangeService);
  private readonly destroyRef = inject(DestroyRef);

  id = input.required<string>();

  readonly statusKeys = WAREHOUSE_CHANGE_STATUS_KEYS;
  readonly eventKeys = WAREHOUSE_HISTORY_EVENT_KEYS;
  readonly paymentStatusKeys = PAYMENT_STATUS_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;

  trace = signal<WarehouseChangeTrace | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  events = computed(() => [...(this.trace()?.timeline ?? [])].sort((a, b) => a.occurredAt.localeCompare(b.occurredAt)));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getHistoryItem(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (trace) => {
        this.trace.set(trace);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('warehouse.history.detail.notFound'));
        else this.error.set(serviceErrorMessage(err, 'warehouse.history.detail.loadError'));
        this.loading.set(false);
      },
    });
  }

  /** Estado del evento: el del pago en `PaymentStatusChanged`, el del cambio en los demás. */
  eventStatus(event: WarehouseChangeEvent): string {
    if (!event.status) return '';
    const keys = event.event === 'PaymentStatusChanged' ? PAYMENT_STATUS_KEYS : WAREHOUSE_CHANGE_STATUS_KEYS;
    const key = keys[event.status];
    return key ? translate(key) : event.status;
  }
}
