import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CheckoutResult, PaymentRedirectForm } from '../models/cart.model';

/**
 * Lleva al pagador a la pasarela después del cierre del pago (M5-03):
 * - URL absoluta (Khipu, Getnet, Bci Pagos): navegación del navegador a la pasarela;
 * - formulario firmado (botón bancario por POST): página del portal que lo envía sola (`/payments/:id/redirect`);
 * - URL relativa (adaptador simulado, modo de prueba): simulador de pago del portal (`/payments/simulator`), que imita
 *   a la pasarela y luego vuelve a la página de resultado;
 * - sin redirección (boleta de depósito): página de resultado.
 */
@Injectable({ providedIn: 'root' })
export class PaymentRedirectService {
  private readonly router = inject(Router);
  private readonly forms = new Map<string, PaymentRedirectForm>();

  continue(result: CheckoutResult): void {
    const id = result.payment.id;
    const form = result.nextAction === 'Redirect' ? result.redirectForm : null;
    const url = result.nextAction === 'Redirect' ? result.redirectUrl : null;
    if (form) {
      this.forms.set(id, form);
      this.router.navigate(['/payments', id, 'redirect']);
    } else if (url && /^https?:\/\//i.test(url)) {
      window.location.assign(url);
    } else if (url) {
      this.router.navigateByUrl(url);
    } else {
      this.router.navigate(['/payments', id, 'result']);
    }
  }

  /** Formulario pendiente de envío del pago (solo en esta pestaña; se descarta al leerlo). */
  take(id: string): PaymentRedirectForm | null {
    const form = this.forms.get(id) ?? null;
    this.forms.delete(id);
    return form;
  }
}
