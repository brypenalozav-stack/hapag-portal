import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TariffService } from '../../../core/services/tariff.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  INTERNAL_CHARGE_RULE_TYPES,
  InternalChargeRule,
  InternalChargeRuleChange,
  InternalChargeRuleRequest,
  InternalChargeRuleType,
} from '../../../core/models/tariff.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { CHARGE_ERRORS, INTERNAL_RULE_TYPE_KEYS, MAINTAINER_ACTION_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { RuleSnapshotComponent } from './rule-snapshot';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';

interface RuleForm {
  ruleType: InternalChargeRuleType;
  country: 'CL' | 'BO';
  taxId: string;
  matchCode: string;
  accountName: string;
  reason: string;
  maxUsesPerBl: string;
  validFrom: string;
  validTo: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

function emptyForm(): RuleForm {
  return {
    ruleType: 'FreeWarehouseChange',
    country: 'CL',
    taxId: '',
    matchCode: '',
    accountName: '',
    reason: '',
    maxUsesPerBl: '1',
    validFrom: '',
    validTo: '',
  };
}

/**
 * Reglas internas de cobro del portal (permiso maintainers.manage): cuentas con cambio de almacén
 * gratuito (M3-04) y cuentas de Bolivia sujetas a demoras anticipadas (M3-16), identificadas por
 * RUT/NIT o Match Code, con vigencia y registro de cambios (NF-15). El crédito, las exenciones y los
 * FFWW no se administran aquí: se leen de Nexus (M8-02, M8-03).
 */
@Component({
  selector: 'app-internal-rules',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, RuleSnapshotComponent],
  templateUrl: './internal-rules.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class InternalRulesComponent implements OnInit {
  private readonly service = inject(TariffService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly typeKeys = INTERNAL_RULE_TYPE_KEYS;
  readonly actionKeys = MAINTAINER_ACTION_KEYS;
  readonly ruleTypes = INTERNAL_CHARGE_RULE_TYPES;

  // Filtros
  ruleType = '';
  country = '';
  includeInactive = false;

  rules = signal<InternalChargeRule[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  // Formulario de alta y edición
  formOpen = signal(false);
  editing = signal<InternalChargeRule | null>(null);
  form: RuleForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  // Registro de cambios de una regla
  historyRule = signal<InternalChargeRule | null>(null);
  history = signal<InternalChargeRuleChange[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getRules({
      ruleType: this.ruleType,
      country: this.country,
      includeInactive: this.includeInactive || undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate('admin.internalRules.list.loadError'));
        this.loading.set(false);
      },
    });
  }

  clearFilters(): void {
    this.ruleType = '';
    this.country = '';
    this.includeInactive = false;
    this.search();
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(rule: InternalChargeRule): void {
    this.editing.set(rule);
    this.form = {
      ruleType: rule.ruleType,
      country: rule.country,
      taxId: rule.taxId ?? '',
      matchCode: rule.matchCode ?? '',
      accountName: rule.accountName ?? '',
      reason: rule.reason ?? '',
      maxUsesPerBl: rule.maxUsesPerBl === null || rule.maxUsesPerBl === undefined ? '' : String(rule.maxUsesPerBl),
      validFrom: rule.validFrom,
      validTo: rule.validTo ?? '',
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
    const f = this.form;
    const list: FormError[] = [];
    if (!f.taxId.trim() && !f.matchCode.trim()) list.push({ fieldId: 'rule-tax-id', key: 'admin.internalRules.form.errors.identifierRequired' });
    if (f.ruleType === 'FreeWarehouseChange' && f.maxUsesPerBl.trim() !== '' && !/^[1-9]\d*$/.test(f.maxUsesPerBl.trim())) {
      list.push({ fieldId: 'rule-max-uses', key: 'admin.internalRules.form.errors.maxUsesInvalid' });
    }
    if (!f.validFrom) list.push({ fieldId: 'rule-valid-from', key: 'admin.internalRules.form.errors.validFromRequired' });
    if (f.validFrom && f.validTo && f.validTo < f.validFrom) {
      list.push({ fieldId: 'rule-valid-to', key: 'admin.internalRules.form.errors.validToBeforeFrom' });
    }
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
    const body: InternalChargeRuleRequest = {
      ruleType: f.ruleType,
      country: f.country,
      taxId: f.taxId.trim() || null,
      matchCode: f.matchCode.trim() || null,
      accountName: f.accountName.trim() || null,
      reason: f.reason.trim() || null,
      maxUsesPerBl: f.ruleType === 'FreeWarehouseChange' && f.maxUsesPerBl.trim() !== '' ? Number(f.maxUsesPerBl) : null,
      validFrom: f.validFrom,
      validTo: f.validTo || null,
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateRule(editing.id, body) : this.service.createRule(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.internalRules.form.updated' : 'admin.internalRules.form.created'));
        this.closeForm();
        this.search();
        if (editing && this.historyRule()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'admin.internalRules.form.errors.submit'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async deactivate(rule: InternalChargeRule): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.deactivate.title',
      message: 'shared.modal.deactivate.message',
      params: { name: this.accountOf(rule) },
      confirmLabel: 'shared.modal.deactivate.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.service.deactivateRule(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(translate('admin.internalRules.list.deactivated', { account: this.accountOf(rule) }));
        this.search();
      },
      error: (err) => {
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'admin.internalRules.list.deactivateError'));
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(rule: InternalChargeRule): void {
    this.historyRule.set(rule);
    this.historyLoading.set(true);
    this.service.getRuleHistory(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.history.set([]);
  }

  /** Nombre de la cuenta para los textos: razón social, RUT/NIT o Match Code. */
  accountOf(rule: InternalChargeRule): string {
    return rule.accountName || rule.taxId || rule.matchCode || '';
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
