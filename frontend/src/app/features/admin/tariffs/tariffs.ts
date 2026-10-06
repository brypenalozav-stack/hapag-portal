import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TariffService } from '../../../core/services/tariff.service';
import { ChargeConcept, Tariff } from '../../../core/models/tariff.model';
import { CHARGE_CONCEPT_KEYS, TARIFF_TIER_UNIT_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Mantenedor de tarifas de cargos locales (M8-01) del portal interno: listado con filtros por país,
 * concepto, vigencia en una fecha e inactivas. Crear y editar se hace en /admin/tariffs/new y
 * /admin/tariffs/:id, donde también está el registro de cambios (NF-15).
 */
@Component({
  selector: 'app-tariffs',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './tariffs.html',
  styles: [':host { display: block; }'],
})
export class TariffsComponent implements OnInit {
  private readonly service = inject(TariffService);
  private readonly destroyRef = inject(DestroyRef);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly unitKeys = TARIFF_TIER_UNIT_KEYS;

  country = '';
  concept = '';
  inForceOn = '';
  includeInactive = false;

  concepts = signal<ChargeConcept[]>([]);
  tariffs = signal<Tariff[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.service.getConcepts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (concepts) => this.concepts.set([...concepts].sort((a, b) => a.displayOrder - b.displayOrder)),
      error: () => this.concepts.set([]),
    });
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getTariffs({
      country: this.country,
      concept: this.concept,
      inForceOn: this.inForceOn,
      includeInactive: this.includeInactive || undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tariffs) => {
        this.tariffs.set(tariffs);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate('admin.tariffs.list.loadError'));
        this.loading.set(false);
      },
    });
  }

  clearFilters(): void {
    this.country = '';
    this.concept = '';
    this.inForceOn = '';
    this.includeInactive = false;
    this.search();
  }
}
