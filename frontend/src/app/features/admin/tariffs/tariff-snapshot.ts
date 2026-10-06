import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TariffSnapshot } from '../../../core/models/tariff.model';
import { CHARGE_CONCEPT_KEYS, TARIFF_TIER_MODE_KEYS, TARIFF_TIER_UNIT_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/** Valor de una tarifa en el registro de cambios (NF-15): monto o tramos, vigencia y estado. */
@Component({
  selector: 'app-tariff-snapshot',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe],
  template: `
    @if (snapshot(); as s) {
      <span class="d-block">
        {{ 'admin.tariffs.snapshot.header' | transloco: { concept: (s.conceptCode | codeLabel: conceptKeys), code: s.code ?? '—', country: s.country, currency: s.currency } }}
      </span>
      @if (s.tiers.length > 0) {
        <span class="d-block">{{ 'admin.tariffs.snapshot.tiers' | transloco: { unit: (s.tierUnit | codeLabel: unitKeys), mode: (s.tierMode | codeLabel: modeKeys) } }}</span>
        <ul class="mb-0 ps-3">
          @for (tier of s.tiers; track tier.fromUnit) {
            <li>
              @if (tier.toUnit !== null && tier.toUnit !== undefined) {
                {{ 'admin.tariffs.snapshot.tierRange' | transloco: { from: tier.fromUnit, to: tier.toUnit, amount: (tier.amount | hlCurrency: s.currency) } }}
              } @else {
                {{ 'admin.tariffs.snapshot.tierOpen' | transloco: { from: tier.fromUnit, amount: (tier.amount | hlCurrency: s.currency) } }}
              }
            </li>
          }
        </ul>
      } @else {
        <span class="d-block">{{ 'admin.tariffs.snapshot.amount' | transloco: { amount: (s.amount | hlCurrency: s.currency) } }}</span>
      }
      <span class="d-block">
        @if (s.validTo) {
          {{ 'admin.tariffs.validity.range' | transloco: { from: (s.validFrom | hlDate: 'localDate'), to: (s.validTo | hlDate: 'localDate') } }}
        } @else {
          {{ 'admin.tariffs.validity.open' | transloco: { from: (s.validFrom | hlDate: 'localDate') } }}
        }
      </span>
      @if (!s.isActive) {
        <span class="d-block">{{ 'admin.tariffs.status.inactive' | transloco }}</span>
      }
    } @else {
      <span aria-hidden="true">—</span>
      <span class="visually-hidden">{{ 'admin.tariffs.snapshot.none' | transloco }}</span>
    }
  `,
})
export class TariffSnapshotComponent {
  snapshot = input<TariffSnapshot | null | undefined>(null);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly unitKeys = TARIFF_TIER_UNIT_KEYS;
  readonly modeKeys = TARIFF_TIER_MODE_KEYS;
}
