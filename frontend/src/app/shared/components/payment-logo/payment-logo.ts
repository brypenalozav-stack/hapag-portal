import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Archivo en public/payment-methods/ por clave de proveedor (IPaymentProvider) o código de medio. */
const LOGO_FILES: Record<string, string> = {
  KHIPU: 'khipu',
  BANCOCHILE: 'bank-button-bch',
  BANK_BUTTON_BCH: 'bank-button-bch',
  SANTANDER: 'bank-button-santander',
  BANK_BUTTON_SANTANDER: 'bank-button-santander',
  BCI: 'bank-button-bci',
  BANK_BUTTON_BCI: 'bank-button-bci',
  DEPOSIT: 'deposit',
  DIGITAL_USD: 'digital-usd',
  CREDIT_LINE: 'credit-line',
};

/** Lo mínimo de un medio de pago para elegir su logo. */
export interface PaymentLogoSource {
  code: string;
  providerKey?: string | null;
  kind?: string | null;
}

/**
 * Logo de un medio de pago junto a su nombre. Se elige por la clave del proveedor, luego por el código del medio y,
 * para depósitos sin código conocido, por el tipo. Es decorativo (alt vacío): el nombre ya está en el texto. Va
 * sobre una pastilla blanca en ambos temas para que los logos de marca conserven sus colores y su contraste.
 * Un medio sin logo no muestra nada.
 */
@Component({
  selector: 'app-payment-logo',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (src(); as s) {
      <span class="hl-pay-logo" data-testid="payment-logo"><img [src]="s" alt="" height="22" decoding="async" /></span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      vertical-align: middle;
    }

    .hl-pay-logo {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 2.5rem;
      height: 2rem;
      padding: 0.2rem 0.45rem;
      border: 1px solid var(--hl-border);
      border-radius: 0.375rem;
      background-color: var(--hl-white);
    }

    img {
      width: auto;
      height: 1.35rem;
    }
  `,
})
export class PaymentLogoComponent {
  readonly method = input.required<PaymentLogoSource>();

  readonly src = computed(() => {
    const m = this.method();
    const file =
      LOGO_FILES[(m.providerKey ?? '').toUpperCase()] ??
      LOGO_FILES[(m.code ?? '').toUpperCase()] ??
      (m.kind === 'Deposit' ? LOGO_FILES['DEPOSIT'] : undefined);
    return file ? `/payment-methods/${file}.png` : null;
  });
}
