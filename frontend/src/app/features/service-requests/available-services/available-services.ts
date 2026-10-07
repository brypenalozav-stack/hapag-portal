import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { LocaleService } from '../../../core/services/locale.service';
import { AvailableService, AvailableServices } from '../../../core/models/service-request.model';
import {
  SERVICE_REFERENCE_TYPE_KEYS,
  SERVICE_TEAM_KEYS,
  SERVICE_TIMING_CLASS,
  SERVICE_TIMING_KEYS,
  SERVICE_UNAVAILABLE_REASON_KEYS,
  SHIPMENT_OPERATION_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { localized, serviceErrorMessage } from '../shared/service-text';

/**
 * Servicios on demand de un BL o booking (M2-03, M2-04): los que el usuario puede ver según su rol en el embarque
 * (M1-11), con la estimación del cobro (tramo vigente, dentro o fuera de plazo) y el acceso a la solicitud. A pedido
 * muestra también los no disponibles con el motivo (fuera de la ventana del embarque, sin contenedores, ya
 * solicitado, sin permiso para solicitar…). El servidor decide todo; la pantalla solo lo presenta.
 */
@Component({
  selector: 'app-available-services',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, LoadingSpinnerComponent],
  templateUrl: './available-services.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .service-name { font-size: 1rem; font-weight: 700; }'],
})
export class AvailableServicesComponent {
  private readonly service = inject(ServiceRequestService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input<string | null>(null);
  /** Nivel del título: 3 dentro de un grupo del detalle del BL, cuyo h2 es el título del grupo. */
  readonly headingLevel = input<2 | 3>(2);
  bookingNumber = input<string | null>(null);

  readonly reasonKeys = SERVICE_UNAVAILABLE_REASON_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly timingKeys = SERVICE_TIMING_KEYS;
  readonly timingClass = SERVICE_TIMING_CLASS;
  readonly referenceKeys = SERVICE_REFERENCE_TYPE_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;

  includeUnavailable = signal(false);
  result = signal<AvailableServices | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  services = computed(() => this.result()?.services ?? []);

  constructor() {
    effect(() => {
      const bl = this.blNumber();
      const booking = this.bookingNumber();
      const include = this.includeUnavailable();
      untracked(() => this.load(bl, booking, include));
    });
  }

  reload(): void {
    this.load(this.blNumber(), this.bookingNumber(), this.includeUnavailable());
  }

  private load(bl: string | null, booking: string | null, include: boolean): void {
    if (!bl && !booking) return;
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getAvailable({ blNumber: bl, bookingNumber: booking }, include).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.result.set(null);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(serviceErrorMessage(err, 'serviceRequests.available.loadError'));
        this.loading.set(false);
      },
    });
  }

  onIncludeUnavailable(event: Event): void {
    this.includeUnavailable.set((event.target as HTMLInputElement).checked);
  }

  name(s: AvailableService): string {
    return localized(this.locale.lang(), s.nameEs, s.nameEn);
  }

  description(s: AvailableService): string {
    return localized(this.locale.lang(), s.descriptionEs, s.descriptionEn);
  }

  /** Parámetros de la solicitud: el servicio se pide con el booking si la definición lo indica (M3-07, M3-15). */
  requestParams(s: AvailableService): Record<string, string> {
    const r = this.result();
    const booking = r?.bookingNumber;
    return s.referenceType === 'Booking' && booking
      ? { booking, code: s.code }
      : { bl: r?.blNumber ?? this.blNumber() ?? '', code: s.code };
  }
}
