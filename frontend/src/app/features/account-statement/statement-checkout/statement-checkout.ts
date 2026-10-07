import { Component, DestroyRef, ElementRef, Injector, computed, effect, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccountStatementService } from '../../../core/services/account-statement.service';
import { AccountPaymentService } from '../../../core/services/account-payment.service';
import { PaymentService } from '../../../core/services/payment.service';
import { newIdempotencyKey } from '../../../core/services/cart.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { AccountCheckoutMode, AccountCheckoutResult, StatementLine } from '../../../core/models/account-statement.model';
import { PaymentBlockStatus, PaymentMethod } from '../../../core/models/cart.model';
import { apiErrorCode } from '../../../core/http/api-error';
import { isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { lineConcept, totalsByCurrency } from '../statement-text';
import { PaymentMethodPickerComponent } from '../../../shared/components/payment-method-picker/payment-method-picker';

/** Errores tras los cuales el intento terminó con certeza: el próximo usa una clave nueva. */
const NEW_KEY_AFTER = new Set(['Payment.ProviderUnavailable', 'PaymentIdempotency.AlreadyExists']);

/**
 * Forma de pago por ítem de un cliente con condición de crédito vigente (M5-10), desde el estado de cuenta (M7-03, M5-07):
 * cada ítem elegible se paga ahora o se imputa a la línea de crédito (radio por ítem); los no elegibles se pagan ahora.
 * El total se divide entre el pago inmediato y lo imputado, se confirma antes de enviar (WCAG 3.3.4) y se cierra con una
 * clave de idempotencia (NF-01). La moneda y el medio solo se piden si algún ítem se paga ahora; el bloqueo de pagos
 * (M8-07) solo afecta a lo que se paga ahora. Los clientes sin crédito no ven esta opción (usan el carro, M5-01).
 */
@Component({
  selector: 'app-statement-checkout',
  standalone: true,
  imports: [TranslocoPipe, HlCurrencyPipe, PaymentMethodPickerComponent],
  templateUrl: './statement-checkout.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class StatementCheckoutComponent {
  private readonly service = inject(AccountStatementService);
  private readonly accountPayments = inject(AccountPaymentService);
  private readonly payments = inject(PaymentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Líneas elegidas en el estado de cuenta. */
  lines = input.required<StatementLine[]>();
  country = input.required<string>();
  block = input<PaymentBlockStatus | null>(null);
  /** El servidor permite imputar a crédito (condición vigente y conceptos habilitados). */
  canImpute = input(true);
  completed = output<AccountCheckoutResult>();


  /** Forma de pago elegida por línea (por omisión, pagar ahora). */
  modes = signal<Record<string, AccountCheckoutMode>>({});
  /** Monedas habilitadas por ítem (M5-04), leídas de la vista de pago de la cuenta. */
  private readonly allowed = signal<Record<string, string[]>>({});
  currency = signal('');
  methods = signal<PaymentMethod[]>([]);
  methodsLoading = signal(false);
  methodCode = signal('');
  confirming = signal(false);
  submitting = signal(false);
  submitError = signal('');
  private key: string | null = null;

  private readonly confirmHeading = viewChild<ElementRef<HTMLElement>>('confirmHeading');

  modeOf(line: StatementLine): AccountCheckoutMode {
    return line.canImputeToCredit && this.canImpute() ? (this.modes()[line.key] ?? 'PayNow') : 'PayNow';
  }

  payNowLines = computed(() => this.lines().filter((l) => this.modeOf(l) === 'PayNow'));
  creditLines = computed(() => this.lines().filter((l) => this.modeOf(l) === 'Credit'));
  payNowTotals = computed(() => totalsByCurrency(this.payNowLines()));
  creditTotals = computed(() => totalsByCurrency(this.creditLines()));
  needsPayment = computed(() => this.payNowLines().length > 0);
  blocked = computed(() => this.needsPayment() && !!this.block()?.blocked);

  /** Monedas en que se pueden pagar todos los ítems que se pagan ahora (M5-04). */
  currencies = computed<string[]>(() => {
    const map = this.allowed();
    const [first, ...rest] = this.payNowLines().map((l) => map[`${l.itemType}:${l.sourceId}`] ?? [l.currency]);
    if (!first) return [];
    return first.filter((c) => rest.every((list) => list.includes(c)));
  });

  needsConversion = computed(() => this.payNowLines().some((l) => l.currency !== this.currency()));

  submitDisabled = computed(() =>
    this.lines().length === 0 || this.blocked() || (this.needsPayment() && (!this.currency() || !this.methodCode())));

  selectedMethodName = computed(() => this.methods().find((m) => m.code === this.methodCode())?.name ?? '');

  constructor() {
    this.accountPayments.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        const map: Record<string, string[]> = {};
        for (const item of data.items) map[`${item.itemType}:${item.sourceId}`] = item.allowedCurrencies;
        this.allowed.set(map);
      },
      error: () => this.allowed.set({}),
    });
    // La moneda y los medios dependen de lo que se paga ahora.
    effect(() => {
      const currencies = this.currencies();
      untracked(() => {
        this.confirming.set(false);
        if (!currencies.includes(this.currency())) this.setCurrency(currencies[0] ?? '');
      });
    });
  }

  setMode(line: StatementLine, mode: AccountCheckoutMode): void {
    this.modes.update((current) => ({ ...current, [line.key]: mode }));
    this.confirming.set(false);
    this.submitError.set('');
  }

  concept(line: StatementLine): string {
    return lineConcept(line);
  }

  onCurrency(event: Event): void {
    this.setCurrency((event.target as HTMLSelectElement).value);
  }

  private setCurrency(currency: string): void {
    this.currency.set(currency);
    this.methodCode.set('');
    this.methods.set([]);
    if (!currency) return;
    this.methodsLoading.set(true);
    this.payments.availableMethods(this.country(), currency).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (methods) => {
        this.methods.set(methods);
        this.methodsLoading.set(false);
      },
      error: () => {
        this.methods.set([]);
        this.methodsLoading.set(false);
      },
    });
  }

  selectMethod(code: string): void {
    this.methodCode.set(code);
    this.submitError.set('');
  }

  review(): void {
    if (this.submitDisabled()) return;
    this.key = newIdempotencyKey();
    this.confirming.set(true);
    this.submitError.set('');
    focusAfterRender(this.injector, () => this.confirmHeading()?.nativeElement);
  }

  cancelReview(): void {
    this.confirming.set(false);
    this.key = null;
  }

  confirm(): void {
    if (this.submitting() || this.submitDisabled()) return;
    const key = this.key ?? newIdempotencyKey();
    this.key = key;
    const needsPayment = this.needsPayment();
    this.submitting.set(true);
    this.submitError.set('');
    this.announcer.announce(translate('accountStatement.checkout.sending'));
    this.service.checkout({
      items: this.lines().map((l) => ({ itemType: l.itemType, sourceId: l.sourceId, billingTaxId: null, mode: this.modeOf(l) })),
      paymentCurrency: needsPayment ? this.currency() : null,
      paymentMethodCode: needsPayment ? this.methodCode() : null,
    }, key).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.confirming.set(false);
        this.key = null;
        this.modes.set({});
        this.completed.emit(result);
      },
      error: (err) => {
        this.submitting.set(false);
        const uncertain = isServiceUnavailable(err);
        const message = uncertain
          ? translate('accountStatement.checkout.uncertain')
          : paymentErrorMessage(err, 'accountStatement.checkout.error', { itemPrefix: true });
        if (!uncertain && NEW_KEY_AFTER.has(apiErrorCode(err) ?? '')) this.key = null;
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
