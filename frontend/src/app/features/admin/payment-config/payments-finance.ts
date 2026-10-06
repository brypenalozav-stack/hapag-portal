import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PaymentOperation, PaymentReconciliation, ReconciliationSearch } from '../../../core/models/payment-config.model';
import {
  PAYMENT_OPERATION_JOB_KEYS,
  PAYMENT_OPERATION_STATUS_KEYS,
  PAYMENT_STATUS_KEYS,
  RECONCILIATION_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Estados de pago que Finanzas puede anular: boleta emitida o pago en curso (M5-02). */
const CANCELLABLE = ['PendingVerification', 'Processing', 'Pending'];

/**
 * Herramientas de Finanzas (permiso payments.finance):
 * - operaciones posteriores a la confirmación que quedaron detenidas o en reintento, con reintento manual
 *   (NF-03: el pago sigue confirmado y la operación no se pierde);
 * - conciliación de los pagos de un período con su comprobante, referencia y abono (NF-04);
 * - anulación de boletas emitidas o pagos en curso con motivo obligatorio y trazabilidad (M5-02).
 */
@Component({
  selector: 'app-payments-finance',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './payments-finance.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentsFinanceComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly operationStatusKeys = PAYMENT_OPERATION_STATUS_KEYS;
  readonly jobKeys = PAYMENT_OPERATION_JOB_KEYS;
  readonly paymentStatusKeys = PAYMENT_STATUS_KEYS;
  readonly reconciliationKeys = RECONCILIATION_STATUS_KEYS;
  readonly operationStatuses = Object.keys(PAYMENT_OPERATION_STATUS_KEYS);
  readonly paymentStatuses = Object.keys(PAYMENT_STATUS_KEYS);

  // Operaciones (NF-03)
  operationStatus = '';
  operations = signal<PaymentOperation[]>([]);
  operationsLoading = signal(true);
  operationsFailed = signal(false);
  operationsError = signal('');
  retrying = signal<string | null>(null);

  // Conciliación (NF-04)
  filters: ReconciliationSearch = { from: '', to: '', country: '', status: '' };
  reconciliation = signal<PaymentReconciliation[]>([]);
  reconciliationLoading = signal(true);
  reconciliationFailed = signal(false);
  reconciliationError = signal('');

  // Anulación por Finanzas (M5-02)
  cancelling = signal<PaymentReconciliation | null>(null);
  cancelReason = '';
  cancelSubmitted = signal(false);
  cancelBusy = signal(false);
  cancelError = signal('');
  actionMessage = signal('');
  actionError = signal('');

  private readonly cancelHeading = viewChild<ElementRef<HTMLElement>>('cancelHeading');

  ngOnInit(): void {
    this.loadOperations();
    this.loadReconciliation();
  }

  loadOperations(): void {
    this.operationsLoading.set(true);
    this.operationsFailed.set(false);
    this.operationsError.set('');
    this.service.getOperations(this.operationStatus || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (operations) => {
        this.operations.set(operations);
        this.operationsLoading.set(false);
      },
      error: (err) => {
        this.operationsLoading.set(false);
        if (isServiceUnavailable(err)) this.operationsFailed.set(true);
        else this.operationsError.set(paymentErrorMessage(err, 'admin.finance.errors.load'));
      },
    });
  }

  retry(operation: PaymentOperation): void {
    this.retrying.set(operation.id);
    this.actionMessage.set('');
    this.actionError.set('');
    this.service.retryOperation(operation.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.retrying.set(null);
        const status = translate(PAYMENT_OPERATION_STATUS_KEYS[updated.status] ?? 'common.paymentOperationStatus.pending');
        this.announcer.announce(translate('admin.finance.operations.retried', { number: updated.paymentNumber, status }));
        this.loadOperations();
      },
      error: (err) => {
        this.retrying.set(null);
        const message = paymentErrorMessage(err, 'admin.finance.operations.retryError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  loadReconciliation(): void {
    this.reconciliationLoading.set(true);
    this.reconciliationFailed.set(false);
    this.reconciliationError.set('');
    this.service.getReconciliation(this.filters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rows) => {
        this.reconciliation.set(rows);
        this.reconciliationLoading.set(false);
      },
      error: (err) => {
        this.reconciliationLoading.set(false);
        if (isServiceUnavailable(err)) this.reconciliationFailed.set(true);
        else this.reconciliationError.set(paymentErrorMessage(err, 'admin.finance.errors.load'));
      },
    });
  }

  canCancel(row: PaymentReconciliation): boolean {
    return CANCELLABLE.includes(row.status);
  }

  askCancel(row: PaymentReconciliation): void {
    this.cancelling.set(row);
    this.cancelReason = '';
    this.cancelSubmitted.set(false);
    this.cancelError.set('');
    focusAfterRender(this.injector, () => this.cancelHeading()?.nativeElement);
  }

  closeCancel(): void {
    this.cancelling.set(null);
  }

  /** Anulación con motivo: queda el usuario, el rol Finanzas, la fecha y la transición (M5-02). */
  confirmCancel(event: Event): void {
    event.preventDefault();
    const row = this.cancelling();
    if (!row || this.cancelBusy()) return;
    this.cancelSubmitted.set(true);
    if (!this.cancelReason.trim()) {
      this.announcer.announce(translate('admin.finance.cancel.reasonRequired'), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('finance-cancel-reason'));
      return;
    }
    this.cancelBusy.set(true);
    this.cancelError.set('');
    this.service.cancelPayment(row.paymentId, this.cancelReason.trim()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cancelBusy.set(false);
        this.cancelling.set(null);
        const message = translate('admin.finance.cancel.done', { number: row.paymentNumber });
        this.actionMessage.set(message);
        this.announcer.announce(message);
        this.loadReconciliation();
      },
      error: (err) => {
        this.cancelBusy.set(false);
        const message = paymentErrorMessage(err, 'admin.finance.cancel.error');
        this.cancelError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
