import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentHistoryService } from '../../core/services/payment-history.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { PagedResult } from '../../core/models/admin-user.model';
import { PaymentHistoryItem } from '../../core/models/payment.model';
import { PAYMENT_STATUS_CLASS, PAYMENT_STATUS_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { saveBlob } from '../../shared/save-blob';

const PAGE_SIZE = 20;

/** Estados que se ofrecen en el filtro del historial (NF-02). */
const STATUSES = ['Confirmed', 'PendingVerification', 'Processing', 'Pending', 'Failed', 'Cancelled'] as const;

interface HistoryFilters {
  from: string;
  to: string;
  status: string;
  blNumber: string;
}

function emptyFilters(): HistoryFilters {
  return { from: '', to: '', status: '', blNumber: '' };
}

/**
 * Historial de pagos y boletas (M7-02): solo pagos hechos en el portal por la organización, como pagadora
 * o como mandante (NF-14). Cada pago muestra su comprobante o boleta, fecha, medio, moneda, total, BL o
 * booking y el RUT del pagador diferenciado de los RUT de facturación; el comprobante se descarga desde
 * aquí o desde el detalle. Reemplaza al listado de pagos y a la vista de comprobantes anteriores.
 */
@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './payment-history.html',
  styles: [':host { display: block; }'],
})
export class PaymentHistoryComponent implements OnInit {
  private readonly service = inject(PaymentHistoryService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statusKeys = PAYMENT_STATUS_KEYS;
  readonly statuses = STATUSES;

  filters: HistoryFilters = emptyFilters();
  page = signal(1);
  result = signal<PagedResult<PaymentHistoryItem> | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.search();
  }

  search(page = 1): void {
    this.page.set(page);
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.search({ ...this.filters, page, pageSize: PAGE_SIZE }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'paymentHistory.errors.load'));
      },
    });
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  statusClass(item: PaymentHistoryItem): string {
    return PAYMENT_STATUS_CLASS[item.status] ?? 'hl-badge--pending';
  }

  document(item: PaymentHistoryItem): string {
    return item.documentNumber ?? item.paymentNumber;
  }

  downloadReceipt(item: PaymentHistoryItem): void {
    this.actionError.set('');
    this.service.downloadReceipt(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, `${this.document(item)}.pdf`);
        this.announcer.announce(translate('paymentHistory.receipt.done', { number: this.document(item) }));
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'paymentHistory.receipt.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
