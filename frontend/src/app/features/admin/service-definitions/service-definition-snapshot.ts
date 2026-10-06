import { Component, computed, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LocaleService } from '../../../core/services/locale.service';
import { ServiceDefinitionSnapshot, ServiceInputField } from '../../../core/models/service-request.model';
import {
  CHARGE_CONCEPT_KEYS,
  SERVICE_FIELD_TYPE_KEYS,
  SERVICE_MILESTONE_KEYS,
  SERVICE_PRICING_MODE_KEYS,
  SERVICE_TEAM_KEYS,
  SERVICE_TIMING_RULE_KEYS,
} from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { localized } from '../../service-requests/shared/service-text';

/**
 * Valor de una definición de servicio en el registro de cambios (NF-15): nombre, alcance, campos del formulario,
 * cobro, plazo, equipos y estado.
 */
@Component({
  selector: 'app-service-definition-snapshot',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe],
  template: `
    @if (snapshot(); as s) {
      <span class="d-block fw-semibold">{{ name() }}</span>
      <span class="d-block">{{ 'admin.serviceDefinitions.snapshot.scope' | transloco: { code: s.code, operations: s.operations, countries: s.countries } }}</span>
      <span class="d-block">
        {{ 'admin.serviceDefinitions.snapshot.pricing' | transloco: { mode: (s.pricingMode | codeLabel: pricingKeys), concept: s.chargeConceptCode ? (s.chargeConceptCode | codeLabel: conceptKeys) : '—' } }}
      </span>
      @if (s.tariffCode || s.lateTariffCode) {
        <span class="d-block">{{ 'admin.serviceDefinitions.snapshot.tariffs' | transloco: { inTime: s.tariffCode ?? '—', late: s.lateTariffCode ?? '—' } }}</span>
      }
      @if (s.milestone !== 'None') {
        <span class="d-block">
          {{ 'admin.serviceDefinitions.snapshot.timing' | transloco: { milestone: (s.milestone | codeLabel: milestoneKeys), offset: s.milestoneOffsetHours, rule: (s.timingRule | codeLabel: timingKeys) } }}
        </span>
      }
      <span class="d-block">
        {{ 'admin.serviceDefinitions.snapshot.teams' | transloco: { approval: (s.approvalTeam | codeLabel: teamKeys), fulfillment: (s.fulfillmentTeam | codeLabel: teamKeys) } }}
      </span>
      @if (fields().length > 0) {
        <span class="d-block">{{ 'admin.serviceDefinitions.snapshot.fields' | transloco: { count: fields().length } }}</span>
        <ul class="mb-0 ps-3">
          @for (f of fields(); track f.key) {
            <li>{{ 'admin.serviceDefinitions.snapshot.field' | transloco: { key: f.key, type: (f.type | codeLabel: fieldTypeKeys) } }}</li>
          }
        </ul>
      } @else {
        <span class="d-block">{{ 'admin.serviceDefinitions.snapshot.noFields' | transloco }}</span>
      }
      @if (!s.isActive) {
        <span class="d-block">{{ 'admin.serviceDefinitions.status.inactive' | transloco }}</span>
      }
    } @else {
      <span aria-hidden="true">—</span>
      <span class="visually-hidden">{{ 'admin.serviceDefinitions.snapshot.none' | transloco }}</span>
    }
  `,
})
export class ServiceDefinitionSnapshotComponent {
  private readonly locale = inject(LocaleService);

  snapshot = input<ServiceDefinitionSnapshot | null | undefined>(null);

  readonly pricingKeys = SERVICE_PRICING_MODE_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly milestoneKeys = SERVICE_MILESTONE_KEYS;
  readonly timingKeys = SERVICE_TIMING_RULE_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly fieldTypeKeys = SERVICE_FIELD_TYPE_KEYS;

  /** Campos del formulario guardados como JSON en la instantánea. */
  fields = computed<ServiceInputField[]>(() => {
    const json = this.snapshot()?.inputSchemaJson;
    if (!json) return [];
    try {
      const parsed = JSON.parse(json) as unknown;
      return Array.isArray(parsed) ? (parsed as ServiceInputField[]) : [];
    } catch {
      return [];
    }
  });

  name(): string {
    const s = this.snapshot();
    return s ? localized(this.locale.lang(), s.nameEs, s.nameEn) : '';
  }
}
