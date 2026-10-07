import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PaymentBlockStatus } from '../../../core/models/cart.model';
import { HlDatePipe } from '../../pipes/hl-date.pipe';

/**
 * Aviso de pagos suspendidos por una ventana de bloqueo programada (M8-07): muestra el mensaje que
 * configuró Hapag-Lloyd y hasta cuándo rige, en la hora local del país. Mientras se muestra, la pantalla
 * deshabilita los botones de pago; la consulta y la edición del carro siguen disponibles.
 */
@Component({
  selector: 'app-payment-block-banner',
  standalone: true,
  imports: [TranslocoPipe, HlDatePipe],
  template: `
    @if (status(); as s) {
      @if (s.blocked) {
        <div class="alert alert-warning d-flex gap-2" role="status" data-testid="payment-block-banner">
          <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="flex-shrink-0 mt-1" viewBox="0 0 16 16">
            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5m.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2"/>
          </svg>
          <div>
            <p class="fw-bold mb-1">{{ 'shared.paymentBlock.title' | transloco }}</p>
            @if (s.message) {
              <p class="mb-1">{{ s.message }}</p>
            }
            @if (s.endDate && s.endTime) {
              <p class="small mb-1">{{ 'shared.paymentBlock.until' | transloco: { date: (s.endDate | hlDate: 'localDate'), time: s.endTime.slice(0, 5), zone: s.timeZone } }}</p>
            }
            <p class="small mb-0">{{ 'shared.paymentBlock.consultAvailable' | transloco }}</p>
          </div>
        </div>
      }
    }
  `,
})
export class PaymentBlockBannerComponent {
  status = input<PaymentBlockStatus | null | undefined>(null);
}
