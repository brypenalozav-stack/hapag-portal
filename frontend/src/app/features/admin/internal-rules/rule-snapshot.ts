import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { InternalChargeRuleSnapshot } from '../../../core/models/tariff.model';
import { INTERNAL_RULE_TYPE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/** Valor de una regla interna en el registro de cambios (NF-15). */
@Component({
  selector: 'app-rule-snapshot',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe],
  template: `
    @if (snapshot(); as s) {
      <span class="d-block">{{ 'admin.internalRules.snapshot.header' | transloco: { type: (s.ruleType | codeLabel: typeKeys), country: s.country } }}</span>
      <span class="d-block">
        {{ 'admin.internalRules.snapshot.account' | transloco: { name: s.accountName ?? '—', taxId: s.taxId ?? '—', matchCode: s.matchCode ?? '—' } }}
      </span>
      @if (s.maxUsesPerBl) {
        <span class="d-block">{{ 'admin.internalRules.snapshot.maxUses' | transloco: { count: s.maxUsesPerBl } }}</span>
      }
      <span class="d-block">
        @if (s.validTo) {
          {{ 'admin.internalRules.validity.range' | transloco: { from: (s.validFrom | hlDate: 'localDate'), to: (s.validTo | hlDate: 'localDate') } }}
        } @else {
          {{ 'admin.internalRules.validity.open' | transloco: { from: (s.validFrom | hlDate: 'localDate') } }}
        }
      </span>
      @if (!s.isActive) {
        <span class="d-block">{{ 'admin.internalRules.status.inactive' | transloco }}</span>
      }
    } @else {
      <span aria-hidden="true">—</span>
      <span class="visually-hidden">{{ 'admin.internalRules.snapshot.none' | transloco }}</span>
    }
  `,
})
export class RuleSnapshotComponent {
  snapshot = input<InternalChargeRuleSnapshot | null | undefined>(null);

  readonly typeKeys = INTERNAL_RULE_TYPE_KEYS;
}
