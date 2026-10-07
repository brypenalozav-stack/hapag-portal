import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { TariffService } from '../../../core/services/tariff.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  CreditImputationRule,
  CreditImputationRuleRequest,
  CreditImputationRuleSnapshot,
  NEXUS_CREDIT_CONCEPTS,
} from '../../../core/models/account-statement.model';
import { MaintainerChange } from '../../../core/models/payment-config.model';
import { ChargeConcept } from '../../../core/models/tariff.model';
import { CHARGE_CONCEPT_KEYS, NEXUS_CREDIT_CONCEPT_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ChangeLogComponent } from './change-log';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';
import { ClientTable, codeText } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

interface RuleForm {
  country: string;
  conceptCode: string;
  nexusCreditConcept: string;
  isEnabled: boolean;
  notes: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

const SNAPSHOT_KEYS: Record<string, string> = {
  country: 'admin.creditRules.form.country',
  conceptCode: 'admin.creditRules.form.concept',
  nexusCreditConcept: 'admin.creditRules.form.nexusConcept',
  isEnabled: 'admin.creditRules.form.enabled',
  notes: 'admin.creditRules.form.notes',
};

function emptyForm(country: string): RuleForm {
  return { country: country || 'CL', conceptCode: '', nexusCreditConcept: 'LOCAL_CHARGES', isEnabled: true, notes: '' };
}

/**
 * Conceptos imputables a la línea de crédito (Fase 2, Ola H, M5-10; permiso `maintainers.manage`, NF-15): por país y
 * concepto de cobro, a qué concepto de crédito de Nexus se imputa y si está habilitado. Un ítem solo se puede imputar si
 * su concepto tiene una regla habilitada y el concepto de Nexus está en la condición de crédito del cliente. Finanzas
 * confirma los conceptos elegibles; cada alta, cambio y baja queda en el registro de cambios.
 */
@Component({
  selector: 'app-credit-imputation-rules',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './credit-imputation-rules.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class CreditImputationRulesComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly modal = inject(ModalService);
  private readonly tariffs = inject(TariffService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly nexusKeys = NEXUS_CREDIT_CONCEPT_KEYS;
  readonly nexusConcepts = NEXUS_CREDIT_CONCEPTS;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  country = '';
  includeDisabled = true;
  rules = signal<CreditImputationRule[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.rules, {
    searchText: (r) =>
      [this.conceptName(r), r.conceptCode, r.country, codeText(r.nexusCreditConcept, this.nexusKeys), r.notes, r.modifiedBy ?? r.createdBy].join(' '),
    sortValues: {
      concept: (r) => this.conceptName(r),
      country: (r) => r.country,
      nexusConcept: (r) => codeText(r.nexusCreditConcept, this.nexusKeys),
      status: (r) => r.isEnabled,
      modified: (r) => r.modifiedAt ?? r.createdAt,
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  formOpen = signal(false);
  editing = signal<CreditImputationRule | null>(null);
  form: RuleForm = emptyForm('CL');
  concepts = signal<ChargeConcept[]>([]);
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyRule = signal<CreditImputationRule | null>(null);
  history = signal<MaintainerChange<CreditImputationRuleSnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getCreditImputationRules(this.country, this.includeDisabled).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.creditRules.errors.load'));
      },
    });
  }

  conceptName(rule: CreditImputationRule): string {
    const key = CHARGE_CONCEPT_KEYS[rule.conceptCode];
    return key ? translate(key) : rule.conceptName;
  }

  /** Conceptos de cobro del país (catálogo de tarifas) para el alta. */
  loadConcepts(): void {
    this.tariffs.getConcepts(this.form.country).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (concepts) => this.concepts.set(concepts),
      error: () => this.concepts.set([]),
    });
  }

  onFormCountry(): void {
    this.form.conceptCode = '';
    this.loadConcepts();
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm(this.country);
    this.loadConcepts();
    this.openForm();
  }

  openEdit(rule: CreditImputationRule): void {
    this.editing.set(rule);
    this.form = {
      country: rule.country,
      conceptCode: rule.conceptCode,
      nexusCreditConcept: rule.nexusCreditConcept,
      isEnabled: rule.isEnabled,
      notes: rule.notes ?? '',
    };
    this.openForm();
  }

  private openForm(): void {
    this.errors.set([]);
    this.saveError.set('');
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const list: FormError[] = [];
    if (!this.editing() && !this.form.conceptCode) list.push({ fieldId: 'credit-rule-concept', key: 'admin.creditRules.form.errors.concept' });
    if (!this.form.nexusCreditConcept) list.push({ fieldId: 'credit-rule-nexus', key: 'admin.creditRules.form.errors.nexusConcept' });
    return list;
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const body: CreditImputationRuleRequest = {
      country: f.country,
      conceptCode: f.conceptCode,
      nexusCreditConcept: f.nexusCreditConcept,
      isEnabled: f.isEnabled,
      notes: f.notes.trim() || null,
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateCreditImputationRule(editing.id, body) : this.service.createCreditImputationRule(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.creditRules.form.updated' : 'admin.creditRules.form.created', { concept: this.conceptName(saved) }));
        this.closeForm();
        this.load();
        if (editing && this.historyRule()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = paymentErrorMessage(err, 'admin.creditRules.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async remove(rule: CreditImputationRule): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.remove.title',
      message: 'shared.modal.remove.message',
      params: { name: this.conceptName(rule) },
      confirmLabel: 'shared.modal.remove.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.service.deleteCreditImputationRule(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(translate('admin.creditRules.removed', { concept: this.conceptName(rule) }));
        this.load();
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'admin.creditRules.errors.remove');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(rule: CreditImputationRule): void {
    this.historyRule.set(rule);
    this.historyLoading.set(true);
    this.service.getCreditImputationRuleHistory(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
        focusAfterRender(this.injector, () => this.historyHeading()?.nativeElement);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }

  closeHistory(): void {
    this.historyRule.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
