import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { ChargesService } from '../../../core/services/charges.service';
import { ExchangeRate } from '../../../core/models/charges.model';
import { DATA_SOURCE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../pipes/code-label.pipe';
import { HlDatePipe } from '../../pipes/hl-date.pipe';
import { HlNumberPipe } from '../../pipes/hl-number.pipe';

/**
 * Tipo de cambio vigente y su vigencia, leídos de Nexus (M5-05), para un monto que se convierte de
 * moneda (por ejemplo, demoras anticipadas en USD pagadas en BOB). No se muestra si las monedas son
 * iguales; si Nexus no informa el tipo de cambio, lo dice sin inventar un valor (NF-11).
 */
@Component({
  selector: 'app-exchange-rate-note',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe, HlNumberPipe],
  template: `
    @if (from() !== to()) {
      @if (rate(); as r) {
        <p class="small text-muted mb-0" data-testid="exchange-rate-note">
          {{ 'shared.exchangeRate.note' | transloco: {
            from: r.fromCurrency,
            to: r.toCurrency,
            rate: (r.rate | hlNumber: '1.2-6'),
            date: (r.effectiveDate | hlDate: 'localDate'),
            source: (r.source | codeLabel: sourceKeys)
          } }}
        </p>
      } @else if (failed()) {
        <p class="small text-muted mb-0">{{ 'shared.exchangeRate.unavailable' | transloco: { from: from(), to: to() } }}</p>
      }
    }
  `,
})
export class ExchangeRateNoteComponent {
  private readonly service = inject(ChargesService);
  private readonly destroyRef = inject(DestroyRef);

  from = input.required<string>();
  to = input.required<string>();

  readonly sourceKeys = DATA_SOURCE_KEYS;

  rate = signal<ExchangeRate | null>(null);
  failed = signal(false);

  constructor() {
    effect(() => {
      const from = this.from();
      const to = this.to();
      this.rate.set(null);
      this.failed.set(false);
      if (!from || !to || from === to) return;
      this.service.getExchangeRate(from, to).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (rate) => this.rate.set(rate),
        error: () => this.failed.set(true),
      });
    });
  }
}
