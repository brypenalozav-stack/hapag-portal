import { Component, DestroyRef, ElementRef, Injector, computed, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChargesService } from '../../../core/services/charges.service';
import { CartService } from '../../../core/services/cart.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ProcessRequirement, RuledCharge, ShipmentCharges } from '../../../core/models/charges.model';
import { apiErrorKey } from '../../../core/http/api-error';
import {
  CHARGE_BLOCKED_REASON_KEYS,
  CHARGE_CONCEPT_KEYS,
  CHARGE_ERRORS,
  CHARGE_OUTCOME_KEYS,
  DATA_SOURCE_KEYS,
  EXEMPTION_PARTY_KEYS,
  REQUIREMENT_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { AddToCartDialogComponent, AddToCartTarget } from '../../../shared/components/add-to-cart-dialog/add-to-cart-dialog';

/** Color del resultado de las reglas con las variantes de .hl-badge. */
const OUTCOME_CLASS: Record<string, string> = {
  Exempt: 'hl-badge--confirmed',
  Paid: 'hl-badge--confirmed',
  PartiallyExempt: 'hl-badge--processing',
  Payable: 'hl-badge--pending',
};

/** Resultado de "Aplicar reglas" que se muestra sobre la tabla. */
interface ApplyOutcome {
  completed: boolean;
  exempted: number;
  payable: number;
}

/**
 * Cargos locales de un BL con las reglas de Nexus aplicadas (GET /charges/{bl}):
 * - exenciones de Gate In, EDS y Gate Out del consignatario del Master o del cliente final, con su
 *   trazabilidad a Nexus (M4-01, M4-02, M3-01); "Aplicar reglas" completa el proceso sin carro
 *   cuando todo queda exento (M4-02);
 * - el IPO de un cliente con crédito no llega del servidor y no se muestra (M4-03, M8-02);
 * - la carta de responsabilidad FFWW bloquea el avance (M4-04, M8-03; su generación es la Ola E);
 * - el monto en moneda local con el tipo de cambio de Nexus (M5-05);
 * - si Nexus no responde, no se presentan los cargos como definitivos (NF-11).
 * "Agregar al carro" (Ola D) valida el cargo en el servidor y pide el RUT de facturación y la moneda de
 * pago (M5-01, M5-09, M5-04); los clientes con crédito pagan desde su cuenta (M5-07).
 */
@Component({
  selector: 'app-charges-panel',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent, AddToCartDialogComponent,
  ],
  templateUrl: './charges-panel.html',
  styleUrl: './charges-panel.scss',
})
export class ChargesPanelComponent {
  private readonly service = inject(ChargesService);
  readonly cart = inject(CartService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly outcomeKeys = CHARGE_OUTCOME_KEYS;
  readonly blockedKeys = CHARGE_BLOCKED_REASON_KEYS;
  readonly partyKeys = EXEMPTION_PARTY_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;
  readonly requirementStatusKeys = REQUIREMENT_STATUS_KEYS;

  data = signal<ShipmentCharges | null>(null);
  loading = signal(true);
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);
  error = signal('');

  applying = signal(false);
  applyError = signal('');
  applyOutcome = signal<ApplyOutcome | null>(null);
  /** Cargo que se está agregando al carro (diálogo con RUT de facturación y moneda). */
  addTargets = signal<AddToCartTarget[] | null>(null);

  private readonly outcomeHeading = viewChild<ElementRef<HTMLElement>>('outcomeHeading');

  /** Requisitos sin cumplir que bloquean el proceso (carta FFWW, M4-04). */
  blockingRequirements = computed<ProcessRequirement[]>(() =>
    (this.data()?.requirements ?? []).filter((r) => r.blocksProcess && r.status !== 'Fulfilled'),
  );

  /** Hay exenciones de Nexus pendientes de registrar en cargos aún no pagados (M4-02). */
  canApplyRules = computed(() => {
    const d = this.data();
    return !!d && d.rulesAvailable && d.canProceed
      && d.charges.some((c) => c.status === 'Pending' && !!c.exemption && !c.exemption.appliedAt);
  });

  /** Cargos a pagar que se pueden agregar al carro (acción Pagar o AddToCart sin bloqueo). */
  cartableCharges = computed(() => {
    const d = this.data();
    if (!d || !d.canProceed) return [];
    return d.charges.filter((c) => (c.action === 'AddToCart' || c.action === 'Pay') && !c.actionBlockedReason);
  });

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
    this.applyOutcome.set(null);
    this.applyError.set('');
    this.service.getCharges(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.data.set(null);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          // Un BL ajeno responde 404, igual que uno inexistente (NF-05).
          this.error.set(translate('charges.panel.notFound', { bl: this.blNumber() }));
        } else if (err instanceof HttpErrorResponse && err.status === 403) {
          this.error.set(translate('charges.panel.forbidden'));
        } else {
          this.error.set(translate(apiErrorKey(err, CHARGE_ERRORS, 'charges.panel.loadError')));
        }
        this.loading.set(false);
      },
    });
  }

  /** Registra las exenciones de Nexus; si todo queda exento, el proceso termina sin carro (M4-02). */
  applyRules(): void {
    this.applying.set(true);
    this.applyError.set('');
    this.applyOutcome.set(null);
    this.service.applyRules(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.applying.set(false);
        this.data.set(result.charges);
        const outcome: ApplyOutcome = {
          completed: result.completed,
          exempted: result.exemptedCharges.length,
          payable: result.payableChargeIds.length,
        };
        this.applyOutcome.set(outcome);
        this.announcer.announce(
          outcome.completed
            ? translate('charges.panel.apply.completed')
            : translate('charges.panel.apply.partial', { exempted: outcome.exempted, payable: outcome.payable }),
        );
        focusAfterRender(this.injector, () => this.outcomeHeading()?.nativeElement);
      },
      error: (err) => {
        this.applying.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'charges.panel.apply.error'));
        this.applyError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Abre el diálogo para agregar el cargo al carro (M5-01, M5-09). */
  addToCart(charge: RuledCharge): void {
    this.addTargets.set([{ itemType: 'LocalCharge', sourceId: charge.chargeId, label: this.conceptLabel(charge) }]);
  }

  onAddClosed(): void {
    this.addTargets.set(null);
  }

  isSaved(charge: RuledCharge): boolean {
    return this.cart.contains('LocalCharge', charge.chargeId);
  }

  outcomeClass(outcome: string): string {
    return OUTCOME_CLASS[outcome] ?? 'hl-badge--pending';
  }

  conceptLabel(charge: RuledCharge): string {
    const key = CHARGE_CONCEPT_KEYS[charge.conceptCode];
    return key ? translate(key) : charge.conceptName;
  }
}
