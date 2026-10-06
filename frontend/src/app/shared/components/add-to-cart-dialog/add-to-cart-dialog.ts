import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { concatMap, forkJoin, from, last, tap } from 'rxjs';
import { CartService } from '../../../core/services/cart.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { BillingOption, PayableItem, PayableItemRef } from '../../../core/models/cart.model';
import {
  BILLING_OPTION_SOURCE_KEYS,
  CHARGE_CONCEPT_KEYS,
  PAYABLE_ITEM_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { paymentErrorMessage } from '../../payment-errors';
import { focusAfterRender } from '../../focus-after-render';
import { ExchangeRateNoteComponent } from '../exchange-rate-note/exchange-rate-note';
import { CodeLabelPipe } from '../../pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../pipes/hl-currency.pipe';

/** Ítem que se quiere agregar al carro, con el texto con que se nombra en la pantalla de origen. */
export interface AddToCartTarget extends PayableItemRef {
  label: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Agregar al carro (M5-01) con validación previa en el servidor (GET /cart/item-options):
 * - el RUT de facturación se elige aquí, solo entre los habilitados para el usuario (M5-09);
 * - la moneda de pago, solo entre las habilitadas para el concepto y el país (M5-04, M5-08), con el
 *   tipo de cambio de Nexus si difiere de la del cargo (M5-05);
 * - los errores de validación (asociación previa M1-18, carta FFWW, valor cero, moneda no habilitada,
 *   crédito, Nexus caído) se muestran en el diálogo sin agregar nada.
 * Con varios ítems (p. ej. líneas de demurrage) se ofrecen los RUT y monedas comunes a todos.
 * Diálogo modal nativo (`<dialog>`): atrapa el foco y se cierra con Escape.
 */
@Component({
  selector: 'app-add-to-cart-dialog',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, ExchangeRateNoteComponent],
  templateUrl: './add-to-cart-dialog.html',
  styleUrl: './add-to-cart-dialog.scss',
})
export class AddToCartDialogComponent implements AfterViewInit {
  private readonly cart = inject(CartService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Uno o más ítems que se agregan con el mismo RUT de facturación y la misma moneda de pago. */
  targets = input.required<AddToCartTarget[]>();
  /** Se emite al cerrar: true si se agregó algo al carro. */
  closed = output<boolean>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  readonly typeKeys = PAYABLE_ITEM_TYPE_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly sourceKeys = BILLING_OPTION_SOURCE_KEYS;

  loading = signal(true);
  loadError = signal('');
  options = signal<PayableItem[]>([]);
  billingTaxId = signal('');
  paymentCurrency = signal('');
  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');
  private added = false;

  /** RUT habilitados para todos los ítems (M5-09). */
  billingOptions = computed<BillingOption[]>(() => {
    const [first, ...rest] = this.options();
    if (!first) return [];
    return first.billingOptions.filter((o) => rest.every((i) => i.billingOptions.some((b) => b.taxId === o.taxId)));
  });

  /** Monedas habilitadas para todos los ítems (M5-04, M5-08). */
  currencies = computed<string[]>(() => {
    const [first, ...rest] = this.options();
    if (!first) return [];
    return first.allowedCurrencies.filter((c) => rest.every((i) => i.allowedCurrencies.includes(c)));
  });

  /** Totales por moneda del cargo de lo que se agrega. */
  totals = computed(() => {
    const map = new Map<string, number>();
    for (const item of this.options()) map.set(item.currency, (map.get(item.currency) ?? 0) + item.totalAmount);
    return [...map.entries()].map(([currency, total]) => ({ currency, total }));
  });

  errors = computed<FormError[]>(() => {
    if (!this.submitted()) return [];
    const list: FormError[] = [];
    if (!this.billingTaxId()) list.push({ fieldId: 'add-to-cart-billing', key: 'shared.addToCart.errors.billingRequired' });
    if (!this.paymentCurrency()) list.push({ fieldId: 'add-to-cart-currency', key: 'shared.addToCart.errors.currencyRequired' });
    return list;
  });

  ngAfterViewInit(): void {
    this.dialog().nativeElement.showModal();
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set('');
    forkJoin(this.targets().map((t) => this.cart.getItemOptions(t))).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (options) => {
        this.options.set(options);
        const billing = this.billingOptions();
        const currencies = this.currencies();
        if (billing.length === 0) {
          this.loadError.set(translate('shared.addToCart.errors.noCommonBilling'));
        } else if (currencies.length === 0) {
          this.loadError.set(translate('shared.addToCart.errors.noCommonCurrency'));
        }
        // Con un solo RUT habilitado queda elegido; si hay varios, el usuario elige (M5-09).
        this.billingTaxId.set(billing.length === 1 ? billing[0].taxId : '');
        const preferred = options[0]?.defaultPaymentCurrency;
        this.paymentCurrency.set(preferred && currencies.includes(preferred) ? preferred : (currencies[0] ?? ''));
        this.loading.set(false);
        if (this.loadError()) this.announcer.announce(this.loadError(), 'assertive');
      },
      error: (err) => {
        this.loading.set(false);
        const message = paymentErrorMessage(err, 'shared.addToCart.errors.load');
        this.loadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  onBilling(event: Event): void {
    this.billingTaxId.set((event.target as HTMLSelectElement).value);
  }

  onCurrency(event: Event): void {
    this.paymentCurrency.set((event.target as HTMLSelectElement).value);
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) return;
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const billingTaxId = this.billingTaxId();
    const paymentCurrency = this.paymentCurrency();
    this.submitting.set(true);
    // Uno a uno: si un ítem falla, los anteriores quedan en el carro y se informa cuál falló.
    from(this.options()).pipe(
      concatMap((item) => this.cart.addItem({
        itemType: item.itemType,
        sourceId: item.sourceId,
        reference: null,
        billingTaxId,
        paymentCurrency,
      }).pipe(tap(() => (this.added = true)))),
      last(),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.submitting.set(false);
        this.announcer.announce(translate('shared.addToCart.added', {
          count: this.options().length,
          label: this.targets()[0]?.label ?? '',
          currency: paymentCurrency,
        }));
        this.close();
      },
      error: (err) => {
        this.submitting.set(false);
        const message = paymentErrorMessage(err, 'shared.addToCart.errors.submit');
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  conceptLabel(item: PayableItem): string {
    const key = CHARGE_CONCEPT_KEYS[item.conceptCode];
    return key ? translate(key) : (item.conceptName ?? item.conceptCode);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  /** Cierre nativo (botón, Escape o tras agregar): avisa al contenedor si se agregó algo. */
  onClosed(): void {
    this.closed.emit(this.added);
  }
}
