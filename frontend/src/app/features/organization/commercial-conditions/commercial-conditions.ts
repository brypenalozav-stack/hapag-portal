import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { ChargesService } from '../../../core/services/charges.service';
import { CommercialConditions } from '../../../core/models/charges.model';
import { CHARGE_CONCEPT_KEYS, DATA_SOURCE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Condiciones comerciales de la organización leídas de Nexus por RUT/NIT y Match Code: condición de
 * crédito (M8-02) y FFWW autorizado con carta de responsabilidad obligatoria (M8-03, M4-04). Solo
 * lectura: se administran en Nexus, sin listado paralelo en el portal. Si Nexus no responde se
 * informa como falla temporal, no como ausencia de condiciones (NF-11).
 */
@Component({
  selector: 'app-commercial-conditions',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  template: `
    <section class="hl-card" aria-labelledby="commercial-conditions-title">
      <div class="card-header p-3">
        <h2 id="commercial-conditions-title" class="section-title">{{ 'organization.commercialConditions.title' | transloco }}</h2>
        <p class="small text-muted mb-0 mt-1">{{ 'organization.commercialConditions.help' | transloco }}</p>
      </div>
      @if (loading()) {
        <app-loading-spinner />
      } @else if (failed()) {
        <app-state-message kind="error" [messageKey]="'organization.commercialConditions.unavailable'" (retry)="load()" />
      } @else if (conditions(); as c) {
        <dl class="row g-3 p-3 mb-0">
          <div class="col-sm-6 col-lg-4">
            <dt class="detail-label">{{ 'organization.commercialConditions.credit' | transloco }}</dt>
            <dd class="detail-value" data-testid="commercial-conditions-credit">
              @if (c.hasCredit) {
                {{ 'organization.commercialConditions.creditYes' | transloco: { days: c.creditDays ?? '—' } }}
              } @else {
                {{ 'organization.commercialConditions.creditNo' | transloco }}
              }
            </dd>
          </div>
          @if (c.hasCredit) {
            <div class="col-sm-6 col-lg-4">
              <dt class="detail-label">{{ 'organization.commercialConditions.creditValidity' | transloco }}</dt>
              <dd class="detail-value">
                @if (c.creditValidTo) {
                  {{ 'organization.commercialConditions.validRange' | transloco: { from: (c.creditValidFrom | hlDate: 'localDate'), to: (c.creditValidTo | hlDate: 'localDate') } }}
                } @else {
                  {{ 'organization.commercialConditions.validFrom' | transloco: { from: (c.creditValidFrom | hlDate: 'localDate') } }}
                }
              </dd>
            </div>
            <div class="col-sm-6 col-lg-4">
              <dt class="detail-label">{{ 'organization.commercialConditions.creditConcepts' | transloco }}</dt>
              <dd class="detail-value">
                @for (concept of c.creditConcepts; track concept; let last = $last) {
                  {{ concept | codeLabel: creditConceptKeys }}@if (!last) {<span>, </span>}
                } @empty {
                  <span aria-hidden="true">—</span>
                }
              </dd>
            </div>
          }
          <div class="col-sm-6 col-lg-4">
            <dt class="detail-label">{{ 'organization.commercialConditions.ipo' | transloco }}</dt>
            <dd class="detail-value">{{ (c.ipoExcluded ? 'organization.commercialConditions.ipoExcluded' : 'organization.commercialConditions.ipoApplies') | transloco }}</dd>
          </div>
          <div class="col-sm-6 col-lg-4">
            <dt class="detail-label">{{ 'organization.commercialConditions.ffww' | transloco }}</dt>
            <dd class="detail-value" data-testid="commercial-conditions-ffww">
              {{ (c.isFreightForwarder ? 'organization.commercialConditions.ffwwYes' : 'organization.commercialConditions.ffwwNo') | transloco }}
            </dd>
          </div>
          @if (c.isFreightForwarder) {
            <div class="col-sm-6 col-lg-4">
              <dt class="detail-label">{{ 'organization.commercialConditions.letter' | transloco }}</dt>
              <dd class="detail-value">{{ (c.responsibilityLetterRequired ? 'organization.commercialConditions.letterRequired' : 'organization.commercialConditions.letterNotRequired') | transloco }}</dd>
            </div>
          }
        </dl>
        <p class="small text-muted px-3 pb-3 mb-0">
          {{ 'organization.commercialConditions.source' | transloco: { source: (c.source | codeLabel: sourceKeys), taxId: c.taxId, matchCode: c.matchCode ?? '—' } }}
        </p>
      }
    </section>
  `,
  styles: [`
    :host { display: block; }
    .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }
    .detail-label { font-size: 0.75rem; font-weight: 600; color: var(--hl-text-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.25rem; }
    .detail-value { font-size: 0.95rem; font-weight: 600; color: var(--hl-dark); margin-bottom: 0; }
  `],
})
export class CommercialConditionsComponent implements OnInit {
  private readonly service = inject(ChargesService);
  private readonly destroyRef = inject(DestroyRef);

  readonly sourceKeys = DATA_SOURCE_KEYS;
  /** Conceptos con crédito: los del catálogo y el grupo de cargos locales. */
  readonly creditConceptKeys: Record<string, string> = {
    ...CHARGE_CONCEPT_KEYS,
    LOCAL_CHARGES: 'organization.commercialConditions.localCharges',
  };

  conditions = signal<CommercialConditions | null>(null);
  loading = signal(true);
  /** Nexus no respondió (available=false) o la consulta falló (NF-11). */
  failed = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.service.getCommercialConditions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (conditions) => {
        this.conditions.set(conditions);
        this.failed.set(!conditions.available);
        this.loading.set(false);
      },
      error: () => {
        this.conditions.set(null);
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
