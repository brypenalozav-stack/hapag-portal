import { Component, ElementRef, OnInit, afterNextRender, computed, inject, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { PaymentRedirectForm } from '../../../core/models/cart.model';
import { PaymentRedirectService } from '../../../core/services/payment-redirect.service';

/**
 * Paso al sitio del banco con un formulario firmado (botón bancario por POST, M5-03). El formulario se envía solo al
 * mostrarse; si el navegador no lo envía (bloqueo de scripts, lector de pantalla que detiene la carga), el botón
 * "Continuar al banco" lo envía. Al recargar la página el formulario ya no está: se ofrece ver el estado del pago.
 */
@Component({
  selector: 'app-payment-redirect',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  templateUrl: './payment-redirect.html',
  styles: [':host { display: block; }'],
})
export class PaymentRedirectComponent implements OnInit {
  private readonly redirects = inject(PaymentRedirectService);

  id = input.required<string>();

  readonly form = signal<PaymentRedirectForm | null>(null);
  readonly fields = computed(() => Object.entries(this.form()?.fields ?? {}).map(([name, value]) => ({ name, value })));
  readonly method = computed(() => (this.form()?.method ?? 'POST').toLowerCase() === 'get' ? 'get' : 'post');

  private readonly bankForm = viewChild<ElementRef<HTMLFormElement>>('bankForm');

  constructor() {
    afterNextRender(() => this.bankForm()?.nativeElement.submit());
  }

  ngOnInit(): void {
    this.form.set(this.redirects.take(this.id()));
  }
}
