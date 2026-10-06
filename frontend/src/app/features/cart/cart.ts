import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, signal, viewChildren } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { CartService, newIdempotencyKey } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { CartGroup, CartItem, CheckoutResult, PaymentMethod } from '../../core/models/cart.model';
import { apiErrorCode } from '../../core/http/api-error';
import {
  CHARGE_CONCEPT_KEYS,
  DATA_SOURCE_KEYS,
  PAYABLE_ITEM_TYPE_KEYS,
  PAYMENT_METHOD_KIND_KEYS,
} from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { PaymentBlockBannerComponent } from '../../shared/components/payment-block-banner/payment-block-banner';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../shared/pipes/hl-number.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { focusAfterRender } from '../../shared/focus-after-render';
import { ModalService } from '../../core/services/modal.service';
import { ToastService } from '../../core/services/toast.service';

/** Estado del cierre de un sub-carro. */
interface CheckoutState {
  methodCode: string;
  /** Paso de confirmación con el resumen antes de pagar (WCAG 3.3.4). */
  confirming: boolean;
  submitting: boolean;
  error: string;
  /** Clave de idempotencia del intento en curso: se reutiliza en reintentos y dobles clics (NF-01). */
  key: string | null;
}

const NEW_STATE: CheckoutState = { methodCode: '', confirming: false, submitting: false, error: '', key: null };

/** Errores tras los cuales el intento terminó con certeza: el próximo usa una clave nueva. */
const NEW_KEY_AFTER = new Set(['Payment.ProviderUnavailable', 'Cart.Conflict', 'PaymentIdempotency.AlreadyExists']);

/**
 * Carro de compra unificado (M5-01) separado por país y moneda de pago (M5-08): cada sub-carro tiene sus
 * ítems con el detalle por tipo de servicio y el RUT de facturación elegido al agregarlos (M5-09), su
 * subtotal, la conversión de moneda con el tipo de cambio de Nexus (M5-04, M5-05), los medios de pago
 * habilitados (M5-03) y su propio cierre. El cierre muestra un resumen para confirmar (WCAG 3.3.4), usa
 * una clave de idempotencia por intento (NF-01) y respeta las ventanas de bloqueo (M8-07). Los clientes
 * con crédito pagan desde su cuenta (M5-07).
 */
@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent, PaymentBlockBannerComponent,
  ],
  templateUrl: './cart.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class CartComponent implements OnInit {
  readonly cartService = inject(CartService);
  private readonly modal = inject(ModalService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly typeKeys = PAYABLE_ITEM_TYPE_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly kindKeys = PAYMENT_METHOD_KIND_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;

  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  busyItemId = signal<string | null>(null);
  private readonly states = signal<Record<string, CheckoutState>>({});

  private readonly confirmHeadings = viewChildren<ElementRef<HTMLElement>>('confirmHeading');

  groups = computed<CartGroup[]>(() => this.cartService.cart()?.groups ?? []);

  /** El perfil puede pagar (OrgViewer solo consulta su carro). */
  canOperate = computed(() => this.auth.canOperate());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.actionError.set('');
    this.cartService.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.loading.set(false),
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.actionError.set(paymentErrorMessage(err, 'cart.errors.load'));
      },
    });
  }

  groupKey(group: CartGroup): string {
    return `${group.country}-${group.paymentCurrency}`;
  }

  state(group: CartGroup): CheckoutState {
    return this.states()[this.groupKey(group)] ?? NEW_STATE;
  }

  private patch(group: CartGroup, changes: Partial<CheckoutState>): void {
    const key = this.groupKey(group);
    this.states.update((all) => ({ ...all, [key]: { ...(all[key] ?? NEW_STATE), ...changes } }));
  }

  conceptLabel(item: CartItem): string {
    const key = CHARGE_CONCEPT_KEYS[item.conceptCode];
    return key ? translate(key) : (item.conceptName ?? item.conceptCode);
  }

  countryName(country: string): string {
    return translate(country === 'BO' ? 'common.country.bo' : 'common.country.cl');
  }

  /** Ítems que entran en el cierre: los que no están en un pago en curso. */
  payableItems(group: CartGroup): CartItem[] {
    return group.items.filter((i) => !i.lockedByPaymentId);
  }

  /** RUT de facturación distintos del sub-carro, para el resumen de confirmación. */
  billingTaxIds(group: CartGroup): string[] {
    return [...new Set(this.payableItems(group).map((i) => translate('cart.checkout.billingEntry', { name: i.billingName, taxId: i.billingTaxId })))];
  }

  selectedMethod(group: CartGroup): PaymentMethod | undefined {
    return group.paymentMethods.find((m) => m.code === this.state(group).methodCode);
  }

  /** El cierre no está disponible: bloqueo de pagos, perfil de consulta, sin medios o sin ítems. */
  checkoutDisabled(group: CartGroup): boolean {
    return group.block.blocked || !this.canOperate() || group.paymentMethods.length === 0 || this.payableItems(group).length === 0;
  }

  selectMethod(group: CartGroup, code: string): void {
    this.patch(group, { methodCode: code, error: '' });
  }

  /** Paso 1: resumen para confirmar. Comienza un intento nuevo con su propia clave (NF-01). */
  review(group: CartGroup): void {
    if (this.checkoutDisabled(group)) return;
    if (!this.state(group).methodCode) {
      const message = translate('cart.checkout.methodRequired');
      this.patch(group, { error: message });
      this.announcer.announce(message, 'assertive');
      focusAfterRender(this.injector, () => document.getElementById(`cart-methods-${this.groupKey(group)}`)?.querySelector('input'));
      return;
    }
    this.patch(group, { confirming: true, error: '', key: newIdempotencyKey() });
    focusAfterRender(this.injector, () => this.confirmHeadings().find((h) => h.nativeElement.id === `cart-confirm-${this.groupKey(group)}`)?.nativeElement);
  }

  cancelReview(group: CartGroup): void {
    this.patch(group, { confirming: false, error: '', key: null });
  }

  /** Paso 2: paga el sub-carro; un doble clic o un reintento reutilizan la misma clave. */
  pay(group: CartGroup): void {
    const current = this.state(group);
    if (current.submitting || this.checkoutDisabled(group)) return;
    const key = current.key ?? newIdempotencyKey();
    this.patch(group, { submitting: true, error: '', key });
    this.announcer.announce(translate('cart.checkout.sending'));
    this.cartService.checkout(
      { country: group.country, paymentCurrency: group.paymentCurrency, paymentMethodCode: current.methodCode },
      key,
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.patch(group, { ...NEW_STATE });
        this.afterCheckout(result);
      },
      error: (err) => {
        const code = apiErrorCode(err);
        // Sin respuesta clara (sin conexión o 5xx) no se sabe si llegó: se reintenta con la misma clave.
        const uncertain = isServiceUnavailable(err);
        const message = uncertain
          ? translate('cart.checkout.uncertain')
          : paymentErrorMessage(err, 'cart.checkout.error', { itemPrefix: true });
        this.patch(group, { submitting: false, error: message, key: uncertain || !NEW_KEY_AFTER.has(code ?? '') ? key : null });
        this.announcer.announce(message, 'assertive');
        if (!uncertain) this.cartService.refresh();
      },
    });
  }

  /** Lleva a la plataforma de pago o a la página del resultado (boleta de depósito, estado). */
  private afterCheckout(result: CheckoutResult): void {
    const payment = result.payment;
    this.toast.success(translate('cart.checkout.created', { number: payment.paymentNumber }));
    this.cartService.refresh();
    const url = result.nextAction === 'Redirect' ? result.redirectUrl : null;
    if (url && /^https?:\/\//i.test(url)) {
      window.location.assign(url);
    } else if (url) {
      // Plataforma simulada (Dummy): devuelve directamente a la página de resultado del portal.
      this.router.navigateByUrl(url);
    } else {
      this.router.navigate(['/payments', payment.id, 'result']);
    }
  }

  changeCurrency(item: CartItem, event: Event): void {
    const currency = (event.target as HTMLSelectElement).value;
    if (!currency || currency === item.paymentCurrency) return;
    this.busyItemId.set(item.id);
    this.actionError.set('');
    this.cartService.changeCurrency(item.id, currency).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyItemId.set(null);
        this.toast.success(translate('cart.item.converted', { concept: this.conceptLabel(item), currency }));
      },
      error: (err) => {
        this.busyItemId.set(null);
        (event.target as HTMLSelectElement).value = item.paymentCurrency;
        const message = paymentErrorMessage(err, 'cart.errors.convert');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async remove(item: CartItem): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.cartRemove.title',
      message: 'shared.modal.cartRemove.message',
      params: { name: this.conceptLabel(item) },
      confirmLabel: 'shared.modal.cartRemove.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.busyItemId.set(item.id);
    this.actionError.set('');
    this.cartService.removeItem(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyItemId.set(null);
        this.toast.success(translate('cart.item.removed', { concept: this.conceptLabel(item) }));
        focusAfterRender(this.injector, () => document.getElementById('cart-title'));
      },
      error: (err) => {
        this.busyItemId.set(null);
        const message = paymentErrorMessage(err, 'cart.errors.remove');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async askClear(group: CartGroup): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'cart.group.clearQuestion',
      params: { currency: group.paymentCurrency },
      confirmLabel: 'cart.group.clearConfirm',
      cancelLabel: 'cart.group.clearCancel',
      tone: 'danger',
    });
    if (confirmed) this.clear(group);
  }

  clear(group: CartGroup): void {
    this.actionError.set('');
    this.cartService.clear(group.country, group.paymentCurrency).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(translate('cart.group.cleared', { currency: group.paymentCurrency }));
        focusAfterRender(this.injector, () => document.getElementById('cart-title'));
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'cart.errors.remove');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
