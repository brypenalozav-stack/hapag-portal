import { Component, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { CartService } from '../../../core/services/cart.service';
import { LocaleService } from '../../../core/services/locale.service';
import { ServiceRequestDetail } from '../../../core/models/service-request.model';
import { CHARGE_CONCEPT_KEYS, CHARGE_STATUS_KEYS } from '../../../core/i18n/labels';
import { AddToCartDialogComponent, AddToCartTarget } from '../../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { localized } from './service-text';

/**
 * Pago del cargo que generó la solicitud (cargo local del BL, categoría Service): se agrega al carro con el flujo de
 * siempre, que valida el RUT de facturación y la moneda en el servidor (M5-01, M5-09); el cliente con crédito lo paga
 * desde su cuenta (M5-07). Al confirmarse el pago, la solicitud avanza sola.
 */
@Component({
  selector: 'app-service-request-payment',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, AddToCartDialogComponent],
  template: `
    <div data-testid="service-payment">
      <div class="table-responsive mb-3" tabindex="0" role="region" [attr.aria-label]="'serviceRequests.payment.caption' | transloco">
        <table class="hl-table">
          <caption class="visually-hidden">{{ 'serviceRequests.payment.caption' | transloco }}</caption>
          <thead>
            <tr>
              <th scope="col">{{ 'serviceRequests.payment.col.concept' | transloco }}</th>
              <th scope="col">{{ 'serviceRequests.payment.col.status' | transloco }}</th>
              <th scope="col" class="text-end">{{ 'serviceRequests.payment.col.total' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @for (charge of request().charges; track charge.chargeId) {
              <tr>
                <td class="fw-semibold">{{ charge.conceptCode | codeLabel: conceptKeys }}</td>
                <td>{{ charge.status | codeLabel: chargeStatusKeys }}</td>
                <td class="text-end">{{ charge.totalAmount | hlCurrency: charge.currency }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (pending().length > 0) {
        <div class="d-flex flex-wrap align-items-center gap-2">
          @if (cart.accountPaymentsEnabled()) {
            <a routerLink="/account-payments" class="btn btn-hl-orange">{{ 'serviceRequests.payment.accountPayments' | transloco }}</a>
          } @else if (cart.cartEnabled()) {
            @if (notInCart().length === 0) {
              <span class="small fw-semibold" data-testid="service-payment-in-cart">{{ 'serviceRequests.payment.inCart' | transloco }}</span>
              <a routerLink="/cart" class="btn btn-outline-secondary">{{ 'serviceRequests.payment.viewCart' | transloco }}</a>
            } @else {
              <button type="button" class="btn btn-hl-orange" data-testid="service-payment-add" (click)="addToCart()">
                {{ 'serviceRequests.payment.addToCart' | transloco }}
              </button>
            }
          }
        </div>
      }
    </div>
    @if (targets(); as t) {
      <app-add-to-cart-dialog [targets]="t" (closed)="onClosed($event)" />
    }
  `,
  styles: [':host { display: block; }'],
})
export class ServiceRequestPaymentComponent {
  readonly cart = inject(CartService);
  private readonly locale = inject(LocaleService);

  request = input.required<ServiceRequestDetail>();
  /** Se emite al cerrar el diálogo con algo agregado al carro. */
  added = output<void>();

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly chargeStatusKeys = CHARGE_STATUS_KEYS;

  targets = signal<AddToCartTarget[] | null>(null);

  /** Cargos aún por pagar. */
  pending = computed(() => this.request().charges.filter((c) => c.status === 'Pending'));
  notInCart = computed(() => this.pending().filter((c) => !this.cart.contains(c.itemType, c.chargeId)));

  addToCart(): void {
    const r = this.request();
    const service = localized(this.locale.lang(), r.nameEs, r.nameEn);
    const label = translate('serviceRequests.payment.cartLabel', { service, number: r.requestNumber });
    this.targets.set(this.notInCart().map((c) => ({ itemType: c.itemType, sourceId: c.chargeId, label })));
  }

  onClosed(added: boolean): void {
    this.targets.set(null);
    if (added) {
      this.cart.refresh();
      this.added.emit();
    }
  }
}
