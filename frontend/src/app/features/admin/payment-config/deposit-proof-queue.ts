import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { DepositProofService } from '../../../core/services/deposit-proof.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { DEPOSIT_PROOF_LIMITS, DEPOSIT_PROOF_STATUSES, DepositProofQueueItem } from '../../../core/models/deposit-proof.model';
import { DEPOSIT_PROOF_STATUS_CLASS, DEPOSIT_PROOF_STATUS_KEYS, PAYMENT_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';

type Decision = 'verify' | 'reject';

/**
 * Bandeja de Finanzas de los comprobantes de depósito (Fase 2, Ola H, M5-06; permiso `payments.finance`): por omisión los
 * que esperan revisión, del más antiguo al más reciente. Finanzas ve el archivo y los datos del depósito contra el pago
 * y lo verifica (el pago se confirma por la vía de siempre: comprobante, liberación y aviso; la referencia bancaria pasa
 * a la conciliación, NF-04) o lo rechaza con motivo obligatorio (el pago sigue esperando un comprobante nuevo).
 */
@Component({
  selector: 'app-deposit-proof-queue',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './deposit-proof-queue.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class DepositProofQueueComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly proofs = inject(DepositProofService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly statuses = DEPOSIT_PROOF_STATUSES;
  readonly statusKeys = DEPOSIT_PROOF_STATUS_KEYS;
  readonly statusClass = DEPOSIT_PROOF_STATUS_CLASS;
  readonly paymentStatusKeys = PAYMENT_STATUS_KEYS;
  readonly maxNotes = DEPOSIT_PROOF_LIMITS.MAX_NOTES;

  status = 'Submitted';
  country = '';
  items = signal<DepositProofQueueItem[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionMessage = signal('');
  actionError = signal('');
  downloading = signal<string | null>(null);

  reviewing = signal<DepositProofQueueItem | null>(null);
  decision: Decision = 'verify';
  notes = '';
  reason = '';
  submitted = signal(false);
  busy = signal(false);
  reviewError = signal('');

  private readonly reviewHeading = viewChild<ElementRef<HTMLElement>>('reviewHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getDepositProofQueue(this.status, this.country).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.depositProofs.errors.load'));
      },
    });
  }

  download(item: DepositProofQueueItem): void {
    if (this.downloading()) return;
    this.downloading.set(item.proof.id);
    this.actionError.set('');
    this.proofs.download(item.proof.paymentId, item.proof.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, item.proof.fileName);
        this.announcer.announce(translate('admin.depositProofs.downloaded', { name: item.proof.fileName }));
      },
      error: (err) => {
        this.downloading.set(null);
        const message = paymentErrorMessage(err, 'admin.depositProofs.downloadError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  review(item: DepositProofQueueItem): void {
    this.reviewing.set(item);
    this.decision = 'verify';
    this.notes = '';
    this.reason = '';
    this.submitted.set(false);
    this.reviewError.set('');
    focusAfterRender(this.injector, () => this.reviewHeading()?.nativeElement);
  }

  closeReview(): void {
    this.reviewing.set(null);
  }

  reasonMissing(): boolean {
    return this.submitted() && this.decision === 'reject' && !this.reason.trim();
  }

  /** Verifica o rechaza (con motivo); queda el usuario, la fecha y la transición del pago (M5-06, NF-04). */
  confirmReview(event: Event): void {
    event.preventDefault();
    const item = this.reviewing();
    if (!item || this.busy()) return;
    this.submitted.set(true);
    if (this.decision === 'reject' && !this.reason.trim()) {
      this.announcer.announce(translate('admin.depositProofs.review.reasonRequired'), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('proof-review-reason'));
      return;
    }
    this.busy.set(true);
    this.reviewError.set('');
    const request$ = this.decision === 'verify'
      ? this.service.verifyDepositProof(item.proof.id, this.notes.trim() || null)
      : this.service.rejectDepositProof(item.proof.id, this.reason.trim());
    const decision = this.decision;
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.busy.set(false);
        this.reviewing.set(null);
        const message = decision === 'verify'
          ? translate('admin.depositProofs.review.verified', { number: item.paymentNumber, receipt: result.payment.payment.receiptNumber ?? '—' })
          : translate('admin.depositProofs.review.rejected', { number: item.paymentNumber });
        this.actionMessage.set(message);
        this.announcer.announce(message);
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        const message = paymentErrorMessage(err, 'admin.depositProofs.review.error');
        this.reviewError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
