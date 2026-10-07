import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentHistoryService } from '../../core/services/payment-history.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { PaymentHistoryItem } from '../../core/models/payment.model';
import {
  CHARGE_CONCEPT_KEYS,
  PAYABLE_ITEM_TYPE_KEYS,
  PAYMENT_ORIGIN_KEYS,
  PAYMENT_STATUS_CLASS,
  PAYMENT_STATUS_KEYS,
} from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../shared/pipes/hl-number.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { saveBlob } from '../../shared/save-blob';
import { DepositProofsComponent } from '../payments/deposit-proofs/deposit-proofs';
import { FeatureService } from '../../core/services/feature.service';

/**
 * Detalle de un pago del historial (M7-02): comprobante o boleta, pagador (RUT que pagó) separado de cada
 * RUT de facturación, mandante si se pagó bajo mandato (NF-14), quién lo ejecutó, medio, moneda y cada
 * documento o concepto incluido con su monto original y el tipo de cambio aplicado (M5-05). Fase 2, Ola H: en un pago con
 * boleta de depósito, el comprobante del abono y su revisión por Finanzas (M5-06).
 */
@Component({
  selector: 'app-payment-history-detail',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe, LoadingSpinnerComponent, StateMessageComponent,
    DepositProofsComponent,
  ],
  templateUrl: './payment-history-detail.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentHistoryDetailComponent implements OnInit {
  private readonly service = inject(PaymentHistoryService);
  /** Comprobante del depósito (M5-06, Fase 2): apagado por defecto. */
  readonly features = inject(FeatureService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  id = input.required<string>();

  readonly statusKeys = PAYMENT_STATUS_KEYS;
  readonly originKeys = PAYMENT_ORIGIN_KEYS;
  readonly typeKeys = PAYABLE_ITEM_TYPE_KEYS;

  payment = signal<PaymentHistoryItem | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  statusClass = computed(() => PAYMENT_STATUS_CLASS[this.payment()?.status ?? ''] ?? 'hl-badge--pending');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    // Tras adjuntar un comprobante se vuelve a leer sin ocultar la página (el estado del pago puede cambiar).
    this.loading.set(this.payment() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getById(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (payment) => {
        this.payment.set(payment);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('paymentHistory.detailPage.notFound'));
        else this.error.set(paymentErrorMessage(err, 'paymentHistory.errors.load'));
      },
    });
  }

  conceptLabel(code: string, description?: string | null): string {
    const key = CHARGE_CONCEPT_KEYS[code];
    return key ? translate(key) : (description ?? code);
  }

  document(p: PaymentHistoryItem): string {
    return p.documentNumber ?? p.paymentNumber;
  }

  downloadReceipt(): void {
    const p = this.payment();
    if (!p) return;
    this.actionError.set('');
    this.service.downloadReceipt(p.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, `${this.document(p)}.pdf`);
        this.announcer.announce(translate('paymentHistory.receipt.done', { number: this.document(p) }));
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'paymentHistory.receipt.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
