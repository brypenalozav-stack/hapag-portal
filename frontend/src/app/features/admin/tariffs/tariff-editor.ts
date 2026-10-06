import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TariffService } from '../../../core/services/tariff.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ChargeConcept,
  TARIFF_CURRENCIES,
  TARIFF_TIER_MODES,
  TARIFF_TIER_UNITS,
  Tariff,
  TariffChange,
  TariffRequest,
  TariffTierMode,
  TariffTierUnit,
} from '../../../core/models/tariff.model';
import { apiErrorKey } from '../../../core/http/api-error';
import {
  CHARGE_CONCEPT_KEYS,
  CHARGE_ERRORS,
  MAINTAINER_ACTION_KEYS,
  TARIFF_TIER_MODE_KEYS,
  TARIFF_TIER_UNIT_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { TariffSnapshotComponent } from './tariff-snapshot';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Tramo en edición: los números llegan como texto desde los campos. */
interface TierRow {
  key: number;
  fromUnit: string;
  toUnit: string;
  amount: string;
}

interface TariffForm {
  conceptCode: string;
  code: string;
  country: 'CL' | 'BO';
  currency: string;
  containerType: string;
  description: string;
  amount: string;
  tierUnit: TariffTierUnit;
  tierMode: TariffTierMode;
  validFrom: string;
  validTo: string;
}

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

function emptyForm(): TariffForm {
  return {
    conceptCode: '',
    code: '',
    country: 'CL',
    currency: 'CLP',
    containerType: '',
    description: '',
    amount: '0',
    tierUnit: 'None',
    tierMode: 'Flat',
    validFrom: '',
    validTo: '',
  };
}

const INTEGER = /^\d+$/;
const DECIMAL = /^\d+([.,]\d+)?$/;

function toNumber(text: string): number {
  return Number(text.replace(',', '.'));
}

/**
 * Crear o editar una tarifa del mantenedor (M8-01): concepto, código (por ejemplo KTE o KTF), país,
 * moneda, tipo de contenedor, monto o tramos (incluidos tramos de tiempo, en horas o días) y
 * vigencia. Las reglas de los tramos se validan antes de enviar (ascendentes, sin traslapes, solo el
 * último abierto) y el servidor las vuelve a exigir. Debajo, el registro de cambios con valor
 * anterior y nuevo, usuario y fecha (NF-15).
 */
@Component({
  selector: 'app-tariff-editor',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    TariffSnapshotComponent,
  ],
  templateUrl: './tariff-editor.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .tier-input { min-width: 7rem; }'],
})
export class TariffEditorComponent implements OnInit {
  private readonly service = inject(TariffService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** ID de la tarifa (ruta /admin/tariffs/:id); sin él se crea una nueva. */
  id = input<string>();

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly unitKeys = TARIFF_TIER_UNIT_KEYS;
  readonly modeKeys = TARIFF_TIER_MODE_KEYS;
  readonly actionKeys = MAINTAINER_ACTION_KEYS;
  readonly units = TARIFF_TIER_UNITS;
  readonly modes = TARIFF_TIER_MODES;
  readonly currencies = TARIFF_CURRENCIES;

  form: TariffForm = emptyForm();
  tiers: TierRow[] = [];
  private nextKey = 1;

  concepts = signal<ChargeConcept[]>([]);
  tariff = signal<Tariff | null>(null);
  history = signal<TariffChange[]>([]);
  historyLoading = signal(false);
  loading = signal(false);
  loadFailed = signal(false);
  loadError = signal('');

  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');
  saved = signal('');
  deactivating = signal(false);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  ngOnInit(): void {
    this.service.getConcepts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (concepts) => this.concepts.set([...concepts].sort((a, b) => a.displayOrder - b.displayOrder)),
      error: () => this.concepts.set([]),
    });
    if (this.id()) this.load();
  }

  isNew(): boolean {
    return !this.id();
  }

  load(): void {
    const id = this.id();
    if (!id) return;
    this.loading.set(true);
    this.loadFailed.set(false);
    this.loadError.set('');
    this.service.getTariff(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tariff) => {
        this.fill(tariff);
        this.loading.set(false);
        this.loadHistory();
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.loadError.set(translate('admin.tariffs.editor.notFound'));
        else this.loadError.set(translate('admin.tariffs.editor.loadError'));
        this.loading.set(false);
      },
    });
  }

  private fill(tariff: Tariff): void {
    this.tariff.set(tariff);
    this.form = {
      conceptCode: tariff.conceptCode,
      code: tariff.code ?? '',
      country: tariff.country,
      currency: tariff.currency,
      containerType: tariff.containerType ?? '',
      description: tariff.description ?? '',
      amount: String(tariff.amount),
      tierUnit: tariff.tierUnit,
      tierMode: tariff.tierMode,
      validFrom: tariff.validFrom,
      validTo: tariff.validTo ?? '',
    };
    this.tiers = tariff.tiers.map((t) => ({
      key: this.nextKey++,
      fromUnit: String(t.fromUnit),
      toUnit: t.toUnit === null || t.toUnit === undefined ? '' : String(t.toUnit),
      amount: String(t.amount),
    }));
  }

  loadHistory(): void {
    const id = this.id();
    if (!id) return;
    this.historyLoading.set(true);
    this.service.getTariffHistory(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }

  // Tramos
  onTierUnitChange(unit: TariffTierUnit): void {
    this.form.tierUnit = unit;
    if (unit === 'None') {
      this.tiers = [];
    } else if (this.tiers.length === 0) {
      this.addTier();
    }
  }

  addTier(): void {
    const last = this.tiers.at(-1);
    const nextFrom = last && INTEGER.test(last.toUnit) ? String(Number(last.toUnit) + 1) : last ? '' : '0';
    this.tiers = [...this.tiers, { key: this.nextKey++, fromUnit: nextFrom, toUnit: '', amount: '' }];
    const index = this.tiers.length - 1;
    focusAfterRender(this.injector, () => document.getElementById(`tariff-tier-from-${index}`));
    this.announcer.announce(translate('admin.tariffs.editor.tiers.added', { number: this.tiers.length }));
  }

  removeTier(index: number): void {
    this.tiers = this.tiers.filter((_, i) => i !== index);
    this.announcer.announce(translate('admin.tariffs.editor.tiers.removed', { number: index + 1 }));
    focusAfterRender(this.injector, () => document.getElementById('tariff-add-tier'));
  }

  /** Valida el formulario con las mismas reglas del servidor (M8-01). */
  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.conceptCode) list.push({ fieldId: 'tariff-concept', key: 'admin.tariffs.editor.errors.conceptRequired' });
    if (!DECIMAL.test(f.amount.trim())) list.push({ fieldId: 'tariff-amount', key: 'admin.tariffs.editor.errors.amountInvalid' });
    if (!f.validFrom) list.push({ fieldId: 'tariff-valid-from', key: 'admin.tariffs.editor.errors.validFromRequired' });
    if (f.validFrom && f.validTo && f.validTo < f.validFrom) {
      list.push({ fieldId: 'tariff-valid-to', key: 'admin.tariffs.editor.errors.validToBeforeFrom' });
    }
    if (f.tierUnit !== 'None' && this.tiers.length === 0) {
      list.push({ fieldId: 'tariff-add-tier', key: 'admin.tariffs.editor.errors.tiersRequired' });
    }
    let previousTo: number | null = null;
    this.tiers.forEach((tier, i) => {
      const number = i + 1;
      const isLast = i === this.tiers.length - 1;
      if (!INTEGER.test(tier.fromUnit.trim())) {
        list.push({ fieldId: `tariff-tier-from-${i}`, key: 'admin.tariffs.editor.errors.tierFrom', params: { number } });
        return;
      }
      const from = Number(tier.fromUnit);
      if (tier.toUnit.trim() === '') {
        if (!isLast) list.push({ fieldId: `tariff-tier-to-${i}`, key: 'admin.tariffs.editor.errors.tierOpenNotLast', params: { number } });
      } else if (!INTEGER.test(tier.toUnit.trim()) || Number(tier.toUnit) < from) {
        list.push({ fieldId: `tariff-tier-to-${i}`, key: 'admin.tariffs.editor.errors.tierTo', params: { number } });
      }
      if (previousTo !== null && from <= previousTo) {
        list.push({ fieldId: `tariff-tier-from-${i}`, key: 'admin.tariffs.editor.errors.tierOverlap', params: { number } });
      }
      if (!DECIMAL.test(tier.amount.trim())) {
        list.push({ fieldId: `tariff-tier-amount-${i}`, key: 'admin.tariffs.editor.errors.tierAmount', params: { number } });
      }
      previousTo = tier.toUnit.trim() === '' ? null : Number(tier.toUnit);
    });
    return list;
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    this.saved.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const body: TariffRequest = {
      conceptCode: f.conceptCode,
      code: f.code.trim() || null,
      country: f.country,
      currency: f.currency,
      containerType: f.containerType.trim() || null,
      description: f.description.trim() || null,
      amount: toNumber(f.amount.trim()),
      tierUnit: f.tierUnit,
      tierMode: f.tierMode,
      tiers: f.tierUnit === 'None' ? [] : this.tiers.map((t) => ({
        fromUnit: Number(t.fromUnit),
        toUnit: t.toUnit.trim() === '' ? null : Number(t.toUnit),
        amount: toNumber(t.amount.trim()),
      })),
      validFrom: f.validFrom,
      validTo: f.validTo || null,
    };
    const id = this.id();
    this.saving.set(true);
    const request$ = id ? this.service.updateTariff(id, body) : this.service.createTariff(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tariff) => {
        this.saving.set(false);
        const message = translate(id ? 'admin.tariffs.editor.updated' : 'admin.tariffs.editor.created');
        this.announcer.announce(message);
        if (id) {
          this.fill(tariff);
          this.saved.set(message);
          this.loadHistory();
        } else {
          this.router.navigate(['/admin/tariffs', tariff.id]);
        }
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'admin.tariffs.editor.errors.submit'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Desactiva la tarifa; queda en el registro de cambios (NF-15). */
  deactivate(): void {
    const id = this.id();
    if (!id) return;
    this.deactivating.set(true);
    this.saveError.set('');
    this.service.deactivateTariff(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.deactivating.set(false);
        this.announcer.announce(translate('admin.tariffs.editor.deactivated'));
        this.load();
      },
      error: (err) => {
        this.deactivating.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'admin.tariffs.editor.errors.deactivate'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
