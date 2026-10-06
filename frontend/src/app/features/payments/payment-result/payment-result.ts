import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentService } from '../../../core/services/payment.service';
import { CartService } from '../../../core/services/cart.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { FINAL_PAYMENT_STATUSES, PaymentStatusDetail } from '../../../core/models/cart.model';
import {
  CANCEL_DENIED_REASON_KEYS,
  CHARGE_CONCEPT_KEYS,
  PAYABLE_ITEM_TYPE_KEYS,
  PAYMENT_FAILURE_REASON_KEYS,
  PAYMENT_STATUS_CLASS,
  PAYMENT_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Intervalo entre consultas del estado mientras la plataforma no confirma. */
export const PAYMENT_POLL_INTERVAL_MS = 3000;
/** Consultas automáticas como máximo (unos 2 minutos); luego se ofrece consultar de nuevo. */
export const PAYMENT_POLL_MAX = 40;

/**
 * Resultado de un pago (NF-02, NF-12): a esta página vuelve el usuario desde la plataforma de pago
 * (`/payments/:id/result?ref=`) y aquí llega también el depósito con boleta. Consulta el estado único
 * del pago hasta que la plataforma confirme o rechace, y anuncia cada cambio en la región polite sin
 * mover el foco. Cada estado dice con certeza si hubo cobro. El depósito permite emitir la boleta; una
 * vez emitida, el cliente ya no puede anularla (M5-02): solo Finanzas.
 */
@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './payment-result.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentResultComponent implements OnInit {
  private readonly service = inject(PaymentService);
  private readonly cart = inject(CartService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  id = input.required<string>();
  /** Referencia del portal que devuelve la plataforma (solo informativa). */
  ref = input<string>();

  readonly statusKeys = PAYMENT_STATUS_KEYS;
  readonly failureKeys = PAYMENT_FAILURE_REASON_KEYS;
  readonly deniedKeys = CANCEL_DENIED_REASON_KEYS;
  readonly typeKeys = PAYABLE_ITEM_TYPE_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;

  data = signal<PaymentStatusDetail | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  polling = signal(false);
  pollExhausted = signal(false);
  actionBusy = signal(false);
  actionError = signal('');
  confirmingCancel = signal(false);

  private polls = 0;
  private timer: ReturnType<typeof setTimeout> | undefined;
  private lastStatus = '';

  private readonly slipHeading = viewChild<ElementRef<HTMLElement>>('slipHeading');

  statusClass = computed(() => PAYMENT_STATUS_CLASS[this.data()?.payment.status ?? ''] ?? 'hl-badge--pending');

  /** Esperando a la plataforma: se sigue consultando. */
  waiting = computed(() => {
    const d = this.data();
    return !!d && (d.payment.status === 'Processing' || (d.payment.status === 'Pending' && !d.canIssueSlip && !d.payment.slipNumber));
  });

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.timer));
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    clearTimeout(this.timer);
    this.loading.set(this.data() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.pollExhausted.set(false);
    this.polls = 0;
    this.fetch();
  }

  private fetch(): void {
    this.service.getStatus(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.loading.set(false);
        this.apply(detail);
      },
      error: (err) => {
        this.loading.set(false);
        this.polling.set(false);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && (err.status === 404 || err.status === 403)) {
          this.error.set(translate('paymentResult.notFound'));
        } else {
          this.error.set(paymentErrorMessage(err, 'paymentResult.loadError'));
        }
      },
    });
  }

  /** Muestra el estado, lo anuncia si cambió y decide si seguir consultando. */
  private apply(detail: PaymentStatusDetail): void {
    this.data.set(detail);
    const status = detail.payment.status;
    if (status !== this.lastStatus) {
      this.announcer.announce(this.statusAnnouncement(detail));
      this.lastStatus = status;
      // Pago confirmado o fallido: el carro cambió (ítems pagados o devueltos).
      if ((FINAL_PAYMENT_STATUSES as readonly string[]).includes(status)) this.cart.refresh();
    }
    if (this.waiting() && this.polls < PAYMENT_POLL_MAX) {
      this.polls++;
      this.polling.set(true);
      this.timer = setTimeout(() => this.fetch(), PAYMENT_POLL_INTERVAL_MS);
    } else {
      this.polling.set(false);
      this.pollExhausted.set(this.waiting());
    }
  }

  /** Texto del anuncio: el estado y, en lo posible, si hubo cobro (NF-12). */
  statusAnnouncement(detail: PaymentStatusDetail): string {
    const p = detail.payment;
    const status = translate(PAYMENT_STATUS_KEYS[p.status] ?? 'common.paymentStatus.pending');
    switch (p.status) {
      case 'Confirmed':
        return translate('paymentResult.announce.confirmed', { number: p.paymentNumber, receipt: p.receiptNumber ?? '' });
      case 'Failed':
        return translate('paymentResult.announce.failed', { number: p.paymentNumber });
      case 'Cancelled':
        return translate('paymentResult.announce.cancelled', { number: p.paymentNumber });
      case 'PendingVerification':
        return translate('paymentResult.announce.slipIssued', { number: p.paymentNumber, slip: p.slipNumber ?? '' });
      default:
        return translate('paymentResult.announce.status', { number: p.paymentNumber, status });
    }
  }

  conceptLabel(code: string, description?: string | null): string {
    const key = CHARGE_CONCEPT_KEYS[code];
    return key ? translate(key) : (description ?? code);
  }

  /** Emite la boleta de depósito (M5-02): desde aquí el cliente ya no puede anularla. */
  issueSlip(): void {
    const d = this.data();
    if (!d || this.actionBusy()) return;
    this.actionBusy.set(true);
    this.actionError.set('');
    this.service.issueSlip(d.payment.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.actionBusy.set(false);
        this.apply(detail);
        focusAfterRender(this.injector, () => this.slipHeading()?.nativeElement);
      },
      error: (err) => {
        this.actionBusy.set(false);
        const message = paymentErrorMessage(err, 'paymentResult.slip.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  askCancel(): void {
    this.confirmingCancel.set(true);
  }

  keepPayment(): void {
    this.confirmingCancel.set(false);
  }

  /** Anulación por el cliente: solo mientras el pago está pendiente (boleta sin emitir). */
  cancel(): void {
    const d = this.data();
    if (!d || this.actionBusy()) return;
    this.actionBusy.set(true);
    this.actionError.set('');
    this.service.cancel(d.payment.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionBusy.set(false);
        this.confirmingCancel.set(false);
        this.load();
      },
      error: (err) => {
        this.actionBusy.set(false);
        this.confirmingCancel.set(false);
        const message = paymentErrorMessage(err, 'paymentResult.cancel.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
        this.load();
      },
    });
  }
}
