import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DemurrageService } from '../../../core/services/demurrage.service';
import { CartIntentService } from '../../../core/services/cart-intent.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { DemurrageConceptCharge, DemurrageLine, DemurrageStatus } from '../../../core/models/demurrage.model';
import { apiErrorKey } from '../../../core/http/api-error';
import {
  ADVANCE_DEMURRAGE_STATUS_KEYS,
  CHARGE_BLOCKED_REASON_KEYS,
  CHARGE_CONCEPT_KEYS,
  CHARGE_ERRORS,
  CHARGE_STATUS_KEYS,
  DATA_SOURCE_KEYS,
  DEMURRAGE_STATE_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { ExchangeRateNoteComponent } from '../../../shared/components/exchange-rate-note/exchange-rate-note';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { DemurrageCalculatorComponent } from '../demurrage-calculator/demurrage-calculator';

/** Moneda local de cada país: la conversión de demoras anticipadas en USD se informa en ella (M5-05). */
const COUNTRY_CURRENCY: Record<string, string> = { CL: 'CLP', BO: 'BOB' };

/**
 * Demurrage de un BL según su estado (M3-18): cada estado muestra solo su información y su única
 * acción (facturado → pagar; calculado → agregar al carro; sin calcular → calculadora; sin
 * demurrage → mensaje de que no registra deuda). La calculadora no se habilita si existe factura.
 * También muestra MHD y otros conceptos de la pestaña (M3-02) y, en Bolivia, las demoras
 * anticipadas que bloquean el CLD hasta su pago (M3-16).
 */
@Component({
  selector: 'app-demurrage-panel',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent,
    StateMessageComponent, ExchangeRateNoteComponent, DemurrageCalculatorComponent,
  ],
  templateUrl: './demurrage-panel.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class DemurragePanelComponent {
  private readonly service = inject(DemurrageService);
  private readonly cart = inject(CartIntentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();

  readonly stateKeys = DEMURRAGE_STATE_KEYS;
  readonly statusKeys = CHARGE_STATUS_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly blockedKeys = CHARGE_BLOCKED_REASON_KEYS;
  readonly advanceKeys = ADVANCE_DEMURRAGE_STATUS_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;

  data = signal<DemurrageStatus | null>(null);
  loading = signal(true);
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);
  error = signal('');

  calculatorOpen = signal(false);
  advanceBusy = signal(false);
  advanceError = signal('');
  /** Conceptos y líneas guardados para el carro en esta vista. */
  private readonly savedIds = signal<Set<string>>(new Set());

  /** Moneda local del país del BL. */
  localCurrency = computed(() => COUNTRY_CURRENCY[this.data()?.country ?? 'CL'] ?? 'CLP');

  /** Líneas calculadas y no pagadas (estado "calculado y no pagado"). */
  pendingLines = computed(() => (this.data()?.lines ?? []).filter((l) => l.status === 'Pending' && !l.isExempt));

  constructor() {
    effect(() => {
      this.blNumber();
      untracked(() => this.load());
    });
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.calculatorOpen.set(false);
    this.advanceError.set('');
    this.service.getStatus(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.data.set(null);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          this.error.set(translate('demurrage.panel.notFound', { bl: this.blNumber() }));
        } else if (err instanceof HttpErrorResponse && err.status === 403) {
          this.error.set(translate('demurrage.panel.forbidden'));
        } else {
          this.error.set(translate(apiErrorKey(err, CHARGE_ERRORS, 'demurrage.panel.loadError')));
        }
        this.loading.set(false);
      },
    });
  }

  /** Una línea exenta se muestra como exenta aunque su estado sea pendiente. */
  lineStatus(line: DemurrageLine): string {
    return line.isExempt ? 'Exempt' : line.status;
  }

  openCalculator(): void {
    this.calculatorOpen.set(true);
  }

  /** El cálculo guardado devuelve el nuevo estado del BL (pasa a "calculado y no pagado"). */
  onCalculated(status: DemurrageStatus): void {
    this.data.set(status);
    this.calculatorOpen.set(false);
  }

  /** Guarda las líneas calculadas para el carro de la Ola D. */
  addLinesToCart(): void {
    const d = this.data();
    if (!d) return;
    const lines = this.pendingLines();
    this.cart.add(lines.map((l) => ({
      chargeId: l.id,
      blNumber: d.blNumber,
      conceptCode: 'DEMURRAGE',
      amount: l.totalAmount,
      currency: l.currency,
    })));
    this.savedIds.update((ids) => {
      const next = new Set(ids);
      lines.forEach((l) => next.add(l.id));
      return next;
    });
    this.announcer.announce(translate('demurrage.panel.cart.linesSaved', { count: lines.length }));
  }

  linesSaved(): boolean {
    const lines = this.pendingLines();
    return lines.length > 0 && lines.every((l) => this.savedIds().has(l.id) || this.cart.has(l.id));
  }

  addConceptToCart(concept: DemurrageConceptCharge): void {
    const d = this.data();
    if (!d) return;
    this.cart.add([{
      chargeId: concept.chargeId,
      blNumber: d.blNumber,
      conceptCode: concept.conceptCode,
      amount: concept.payableTotal,
      currency: concept.currency,
    }]);
    this.savedIds.update((ids) => new Set(ids).add(concept.chargeId));
    const key = CHARGE_CONCEPT_KEYS[concept.conceptCode];
    this.announcer.announce(translate('demurrage.panel.cart.conceptSaved', { concept: key ? translate(key) : concept.conceptName }));
  }

  isSaved(chargeId: string | null | undefined): boolean {
    return !!chargeId && (this.savedIds().has(chargeId) || this.cart.has(chargeId));
  }

  /** Genera el cargo de demoras anticipadas (idempotente) para pagarlo antes del CLD (M3-16). */
  requestAdvance(): void {
    this.advanceBusy.set(true);
    this.advanceError.set('');
    this.service.requestAdvance(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => {
        this.advanceBusy.set(false);
        this.data.set(status);
        this.announcer.announce(translate('demurrage.panel.advance.generated'));
      },
      error: (err) => {
        this.advanceBusy.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'demurrage.panel.advance.error'));
        this.advanceError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  addAdvanceToCart(): void {
    const d = this.data();
    const advance = d?.advance;
    if (!d || !advance?.chargeId) return;
    this.cart.add([{
      chargeId: advance.chargeId,
      blNumber: d.blNumber,
      conceptCode: 'ADVANCE_DEMURRAGE_BO',
      amount: advance.amount ?? 0,
      currency: advance.currency ?? 'USD',
    }]);
    this.savedIds.update((ids) => new Set(ids).add(advance.chargeId as string));
    this.announcer.announce(translate('demurrage.panel.cart.conceptSaved', { concept: translate('common.chargeConcept.advanceDemurrageBo') }));
  }
}
