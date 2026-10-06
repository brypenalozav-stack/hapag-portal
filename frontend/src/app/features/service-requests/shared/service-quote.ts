import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ServiceQuote } from '../../../core/models/service-request.model';
import {
  CHARGE_CONCEPT_KEYS,
  CHARGE_OUTCOME_KEYS,
  DATA_SOURCE_KEYS,
  SERVICE_MILESTONE_SOURCE_KEYS,
  SERVICE_PRICING_MODE_KEYS,
  SERVICE_TIMING_CLASS,
  SERVICE_TIMING_KEYS,
  TARIFF_TIER_UNIT_KEYS,
} from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';

/**
 * Cobro de un servicio on demand: tarifa y su origen, tramo aplicado, dentro o fuera de plazo con el hito y el tiempo
 * transcurrido (M3-13, M3-14, NF-22), cantidad, neto, impuesto y total; exención o unidades del embarcador no
 * cobradas (M3-10) y, en el modo de cargos del sistema de origen, cada cargo con el resultado de las reglas de Nexus
 * (M3-15, M4-01, M4-02). Las fechas se muestran en el huso del país de la operación, con la zona indicada.
 */
@Component({
  selector: 'app-service-quote',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe],
  templateUrl: './service-quote.html',
  styles: [':host { display: block; } .quote-label { font-size: 0.75rem; font-weight: 600; color: var(--hl-text-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.25rem; } .quote-value { font-weight: 600; margin-bottom: 0; }'],
})
export class ServiceQuoteComponent {
  quote = input.required<ServiceQuote>();

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly outcomeKeys = CHARGE_OUTCOME_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;
  readonly milestoneKeys = SERVICE_MILESTONE_SOURCE_KEYS;
  readonly pricingKeys = SERVICE_PRICING_MODE_KEYS;
  readonly timingKeys = SERVICE_TIMING_KEYS;
  readonly timingClass = SERVICE_TIMING_CLASS;
  readonly unitKeys = TARIFF_TIER_UNIT_KEYS;

  /** Tramo aplicado (el de la primera línea; con tarifa plana no hay tramos). */
  tier = computed(() => this.quote().lines.find((l) => l.breakdown.length > 0)?.breakdown[0] ?? null);

  /** Líneas por contenedor: se muestran si hay más de una o si identifican contenedores. */
  showLines = computed(() => {
    const lines = this.quote().lines;
    return lines.length > 1 || lines.some((l) => !!l.containerNumber);
  });
}
