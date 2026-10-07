import { Component, DestroyRef, ElementRef, Injector, afterNextRender, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentService } from '../../../core/services/payment.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PaymentSimulatorOutcome } from '../../../core/models/cart.model';
import { PaymentLogoComponent } from '../../../shared/components/payment-logo/payment-logo';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Pasarelas que se simulan (clave del proveedor de pago): clase del encabezado y nombre que se muestra. */
export const SIMULATED_PROVIDERS: Record<string, { css: string; nameKey: string }> = {
  Khipu: { css: 'khipu', nameKey: 'paymentSimulator.providers.Khipu' },
  Santander: { css: 'santander', nameKey: 'paymentSimulator.providers.Santander' },
  Bci: { css: 'bci', nameKey: 'paymentSimulator.providers.Bci' },
  BancoChile: { css: 'bancochile', nameKey: 'paymentSimulator.providers.BancoChile' },
};

/**
 * Ruta del portal a la que se puede volver desde el simulador: solo del mismo origen y dentro de `/payments/`, para que
 * un enlace manipulado no saque al usuario del portal (redirección abierta). Devuelve la ruta relativa o nulo.
 */
export function safeReturnPath(url: string | null | undefined, origin: string): string | null {
  if (!url) return null;
  try {
    const parsed = new URL(url, origin);
    if (parsed.origin !== origin || !parsed.pathname.startsWith('/payments/')) return null;
    return parsed.pathname + parsed.search + parsed.hash;
  } catch {
    return null;
  }
}

/**
 * Simulador de pago (modo de prueba, `Integrations:<Proveedor>:Mode=Dummy`): reemplaza la página de la pasarela o del
 * banco (Khipu, Getnet/Santander, Bci Pagos, Banco de Chile). El usuario elige el resultado —pagar, rechazar, dejar
 * pendiente o cancelar—, el portal lo aplica (`POST /payments/simulator/{referencia}`) y el usuario vuelve a la página
 * de resultado, que verifica el pago como con la pasarela real. Nunca se cobra nada. Ver
 * docs/integraciones/pasarelas-pago.md, «Simulador en modo de prueba».
 */
@Component({
  selector: 'app-payment-simulator',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, PaymentLogoComponent, HlCurrencyPipe],
  templateUrl: './payment-simulator.html',
  styleUrl: './payment-simulator.scss',
})
export class PaymentSimulatorComponent {
  private readonly service = inject(PaymentService);
  private readonly router = inject(Router);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** Clave del proveedor de pago (Khipu, Santander, Bci, BancoChile). */
  provider = input<string>();
  /** Referencia del pago en el portal. */
  ref = input<string>();
  amount = input<string>();
  currency = input<string>();
  /** Página de resultado del portal a la que se vuelve (mismo origen). */
  returnUrl = input<string>();

  readonly busy = signal<PaymentSimulatorOutcome | null>(null);
  readonly error = signal('');
  /** El pago ya tiene resultado final (409): no se ofrecen los botones, sí ver el estado del pago. */
  readonly finished = signal(false);

  readonly simulated = computed(() => SIMULATED_PROVIDERS[this.provider() ?? ''] ?? null);
  readonly headerClass = computed(() => `hl-sim-header--${this.simulated()?.css ?? ''}`);
  readonly providerNameKey = computed(() => this.simulated()?.nameKey ?? 'paymentSimulator.invalidTitle');
  readonly providerKey = computed(() => (this.simulated() ? (this.provider() as string) : ''));
  readonly amountValue = computed(() => {
    const value = Number(this.amount());
    return Number.isFinite(value) && value > 0 ? value : null;
  });
  readonly returnPath = computed(() => safeReturnPath(this.returnUrl(), window.location.origin));
  /** El enlace trae todo lo necesario y vuelve al propio portal. */
  readonly valid = computed(() => !!this.simulated() && !!this.ref() && this.amountValue() !== null && !!this.currency() && !!this.returnPath());

  private readonly heading = viewChild<ElementRef<HTMLElement>>('heading');
  private readonly statusButton = viewChild<ElementRef<HTMLElement>>('statusButton');
  private readonly injector = inject(Injector);

  /** Página de resultado del portal (ruta ya validada). */
  viewResult(): void {
    const path = this.returnPath();
    if (path) this.router.navigateByUrl(path);
  }

  constructor() {
    afterNextRender(() => this.heading()?.nativeElement.focus());
  }

  /** Envía el resultado elegido y vuelve a la página de resultado del portal, que verifica el pago. */
  choose(outcome: PaymentSimulatorOutcome): void {
    const reference = this.ref();
    const path = this.returnPath();
    if (!this.valid() || !reference || !path || this.busy()) return;
    this.busy.set(outcome);
    this.error.set('');
    this.service.simulate(reference, outcome).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(null);
        this.router.navigateByUrl(path);
      },
      error: (err) => {
        this.busy.set(null);
        const status = err instanceof HttpErrorResponse ? err.status : 0;
        const message = status === 404
          ? translate('paymentSimulator.notAvailable')
          : status === 409
            ? translate('paymentSimulator.alreadyFinal')
            : paymentErrorMessage(err, 'paymentSimulator.error');
        this.error.set(message);
        this.finished.set(status === 409);
        // Los botones desaparecen: el foco pasa al botón que lleva al estado del pago.
        if (status === 409) focusAfterRender(this.injector, () => this.statusButton()?.nativeElement);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
