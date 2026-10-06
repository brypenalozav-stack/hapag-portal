import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccountPaymentService } from '../../core/services/account-payment.service';
import { PaymentService } from '../../core/services/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { newIdempotencyKey } from '../../core/services/cart.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { AccountPayables, CheckoutResult, PayableItem, PaymentBlockStatus, PaymentMethod } from '../../core/models/cart.model';
import { apiErrorCode } from '../../core/http/api-error';
import { CHARGE_CONCEPT_KEYS, PAYABLE_ITEM_TYPE_KEYS, PAYMENT_METHOD_KIND_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { PaymentBlockBannerComponent } from '../../shared/components/payment-block-banner/payment-block-banner';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { focusAfterRender } from '../../shared/focus-after-render';
import { ToastService } from '../../core/services/toast.service';

/** Errores tras los cuales el intento terminó con certeza: el próximo usa una clave nueva. */
const NEW_KEY_AFTER = new Set(['Payment.ProviderUnavailable', 'PaymentIdempotency.AlreadyExists']);

/**
 * "Pagar desde mi cuenta" (M5-07): los clientes con condición de crédito en Nexus (M8-02) no usan el
 * carro; aquí ven sus cargos pendientes (sin IPO, M4-03), flete, demurrage, cambios de almacén y facturas,
 * eligen varios y los pagan en una sola transacción. La liberación es la misma que para cualquier cliente.
 * Comparte con el carro la confirmación previa, la clave de idempotencia (NF-01) y el bloqueo (M8-07).
 */
@Component({
  selector: 'app-account-payments',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    PaymentBlockBannerComponent,
  ],
  templateUrl: './account-payments.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class AccountPaymentsComponent implements OnInit {
  private readonly service = inject(AccountPaymentService);
  private readonly payments = inject(PaymentService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly typeKeys = PAYABLE_ITEM_TYPE_KEYS;
  readonly kindKeys = PAYMENT_METHOD_KIND_KEYS;

  data = signal<AccountPayables | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  /** La organización no tiene crédito: usa el carro. */
  notCredit = signal(false);
  block = signal<PaymentBlockStatus | null>(null);

  selected = signal<Set<string>>(new Set());
  currency = signal('');
  methods = signal<PaymentMethod[]>([]);
  methodsLoading = signal(false);
  methodCode = signal('');
  confirming = signal(false);
  submitting = signal(false);
  submitError = signal('');
  private key: string | null = null;

  private readonly confirmHeading = viewChild<ElementRef<HTMLElement>>('confirmHeading');

  canOperate = computed(() => this.auth.canOperate());

  selectedItems = computed<PayableItem[]>(() => (this.data()?.items ?? []).filter((i) => this.selected().has(this.itemKey(i))));

  /** País de los ítems elegidos; null si mezclan países (un pago es de un solo país). */
  country = computed<string | null>(() => {
    const countries = new Set(this.selectedItems().map((i) => i.country));
    return countries.size === 1 ? [...countries][0] : null;
  });

  mixedCountries = computed(() => new Set(this.selectedItems().map((i) => i.country)).size > 1);

  /** Monedas en que se pueden pagar todos los ítems elegidos (M5-04). */
  currencies = computed<string[]>(() => {
    const [first, ...rest] = this.selectedItems();
    if (!first) return [];
    return first.allowedCurrencies.filter((c) => rest.every((i) => i.allowedCurrencies.includes(c)));
  });

  /** Totales de lo elegido en la moneda de cada cargo. */
  selectedTotals = computed(() => {
    const map = new Map<string, number>();
    for (const item of this.selectedItems()) map.set(item.currency, (map.get(item.currency) ?? 0) + item.totalAmount);
    return [...map.entries()].map(([currency, total]) => ({ currency, total }));
  });

  needsConversion = computed(() => this.selectedItems().some((i) => i.currency !== this.currency()));

  payDisabled = computed(() =>
    !!this.block()?.blocked || !this.canOperate() || this.selectedItems().length === 0 || this.mixedCountries()
    || !this.currency() || !this.methodCode());

  ngOnInit(): void {
    this.load();
    this.payments.blockStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => this.block.set(status),
      error: () => this.block.set(null),
    });
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.notCredit.set(false);
    this.service.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.selected.set(new Set());
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (apiErrorCode(err) === 'AccountPayment.NotCreditCustomer') this.notCredit.set(true);
        else this.error.set(paymentErrorMessage(err, 'accountPayments.errors.load'));
      },
    });
  }

  itemKey(item: PayableItem): string {
    return `${item.itemType}:${item.sourceId}`;
  }

  conceptLabel(item: PayableItem): string {
    const key = CHARGE_CONCEPT_KEYS[item.conceptCode];
    return key ? translate(key) : (item.conceptName ?? item.conceptCode);
  }

  /** RUT al que se factura: el propio o, en facturas, el facturado. */
  billingOf(item: PayableItem): string {
    const own = item.billingOptions.find((o) => o.source === 'Own') ?? item.billingOptions[0];
    return own ? translate('accountPayments.items.billingEntry', { name: own.name, taxId: own.taxId }) : '—';
  }

  isSelected(item: PayableItem): boolean {
    return this.selected().has(this.itemKey(item));
  }

  allSelected = computed(() => {
    const items = this.data()?.items ?? [];
    return items.length > 0 && items.every((i) => this.selected().has(this.itemKey(i)));
  });

  toggle(item: PayableItem, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) next.add(this.itemKey(item));
      else next.delete(this.itemKey(item));
      return next;
    });
    this.afterSelection();
  }

  toggleAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.set(new Set(checked ? (this.data()?.items ?? []).map((i) => this.itemKey(i)) : []));
    this.afterSelection();
  }

  /** La moneda y los medios dependen de lo elegido. */
  private afterSelection(): void {
    this.confirming.set(false);
    this.submitError.set('');
    const currencies = this.currencies();
    if (!currencies.includes(this.currency())) this.setCurrency(currencies[0] ?? '');
    else if (this.methods().length === 0) this.loadMethods();
  }

  onCurrency(event: Event): void {
    this.setCurrency((event.target as HTMLSelectElement).value);
  }

  private setCurrency(currency: string): void {
    this.currency.set(currency);
    this.methodCode.set('');
    this.loadMethods();
  }

  private loadMethods(): void {
    const country = this.country();
    const currency = this.currency();
    this.methods.set([]);
    if (!country || !currency) return;
    this.methodsLoading.set(true);
    this.payments.availableMethods(country, currency).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  selectedMethodName = computed(() => this.methods().find((m) => m.code === this.methodCode())?.name ?? '');

  review(): void {
    if (this.payDisabled()) return;
    this.key = newIdempotencyKey();
    this.confirming.set(true);
    this.submitError.set('');
    focusAfterRender(this.injector, () => this.confirmHeading()?.nativeElement);
  }

  cancelReview(): void {
    this.confirming.set(false);
    this.key = null;
  }

  pay(): void {
    if (this.submitting() || this.payDisabled()) return;
    const key = this.key ?? newIdempotencyKey();
    this.key = key;
    this.submitting.set(true);
    this.submitError.set('');
    this.announcer.announce(translate('accountPayments.checkout.sending'));
    this.service.checkout({
      items: this.selectedItems().map((i) => ({ itemType: i.itemType, sourceId: i.sourceId, billingTaxId: null })),
      paymentCurrency: this.currency(),
      paymentMethodCode: this.methodCode(),
    }, key).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.confirming.set(false);
        this.key = null;
        this.afterCheckout(result);
      },
      error: (err) => {
        this.submitting.set(false);
        const uncertain = isServiceUnavailable(err);
        const message = uncertain
          ? translate('accountPayments.checkout.uncertain')
          : paymentErrorMessage(err, 'accountPayments.checkout.error', { itemPrefix: true });
        if (!uncertain && NEW_KEY_AFTER.has(apiErrorCode(err) ?? '')) this.key = null;
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  private afterCheckout(result: CheckoutResult): void {
    this.toast.success(translate('accountPayments.checkout.created', { number: result.payment.paymentNumber }));
    const url = result.nextAction === 'Redirect' ? result.redirectUrl : null;
    if (url && /^https?:\/\//i.test(url)) {
      window.location.assign(url);
    } else if (url) {
      this.router.navigateByUrl(url);
    } else {
      this.router.navigate(['/payments', result.payment.id, 'result']);
    }
  }
}
