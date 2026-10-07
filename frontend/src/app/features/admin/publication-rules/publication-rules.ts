import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PublicationRuleService } from '../../../core/services/publication-rule.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ShipmentPublicationRule,
  ShipmentPublicationRuleChange,
  ShipmentPublicationRuleRequest,
} from '../../../core/models/shipment.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { MAINTAINER_ACTION_KEYS, PORTAL_ERRORS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';
import { ClientTable } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

interface RuleForm {
  country: 'CL' | 'BO';
  finalDestinationCode: string;
  finalDestinationName: string;
  dischargePortCode: string;
  description: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

/** UN/LOCODE: dos letras de país y tres caracteres de localidad (p. ej. CLANF). */
const LOCODE = /^[A-Z]{2}[A-Z0-9]{3}$/;

function emptyForm(): RuleForm {
  return { country: 'CL', finalDestinationCode: '', finalDestinationName: '', dischargePortCode: '', description: '' };
}

/**
 * Reglas de publicación de BL por DIFU de destino final (M2-01, permiso maintainers.manage): los BL del país con
 * ese destino final (y descargados en el puerto indicado, si lo hay) se publican a los clientes solo si el origen
 * informa un DIFU asociado al destino, por ejemplo Antofagasta o Punta Arenas desde San Antonio. Alta, edición,
 * desactivación y registro de cambios (NF-15). Los BL no publicados se revisan en Embarques con el filtro
 * "No publicados".
 */
@Component({
  selector: 'app-publication-rules',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './publication-rules.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PublicationRulesComponent implements OnInit {
  private readonly service = inject(PublicationRuleService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly actionKeys = MAINTAINER_ACTION_KEYS;

  // Filtros
  country = '';
  includeInactive = false;

  rules = signal<ShipmentPublicationRule[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.rules, {
    searchText: (r) =>
      [r.finalDestinationCode, r.finalDestinationName, r.dischargePortCode, r.country, r.description, r.modifiedBy ?? r.createdBy].join(' '),
    sortValues: {
      destination: (r) => r.finalDestinationCode,
      port: (r) => r.dischargePortCode,
      country: (r) => r.country,
      description: (r) => r.description,
      modified: (r) => r.modifiedAt ?? r.createdAt,
      status: (r) => r.isActive,
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  // Alta y edición
  formOpen = signal(false);
  editing = signal<ShipmentPublicationRule | null>(null);
  form: RuleForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  // Registro de cambios
  historyRule = signal<ShipmentPublicationRule | null>(null);
  history = signal<ShipmentPublicationRuleChange[]>([]);
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
    this.service.getRules(this.country, this.includeInactive).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate('admin.publicationRules.list.loadError'));
        this.loading.set(false);
      },
    });
  }

  clearFilters(): void {
    this.country = '';
    this.includeInactive = false;
    this.search();
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(rule: ShipmentPublicationRule): void {
    this.editing.set(rule);
    this.form = {
      country: rule.country,
      finalDestinationCode: rule.finalDestinationCode,
      finalDestinationName: rule.finalDestinationName ?? '',
      dischargePortCode: rule.dischargePortCode ?? '',
      description: rule.description ?? '',
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
    const destination = this.form.finalDestinationCode.trim().toUpperCase();
    const port = this.form.dischargePortCode.trim().toUpperCase();
    const list: FormError[] = [];
    if (!destination) list.push({ fieldId: 'publication-destination', key: 'admin.publicationRules.form.errors.destinationRequired' });
    else if (!LOCODE.test(destination)) list.push({ fieldId: 'publication-destination', key: 'admin.publicationRules.form.errors.destinationInvalid' });
    if (port && !LOCODE.test(port)) list.push({ fieldId: 'publication-port', key: 'admin.publicationRules.form.errors.portInvalid' });
    else if (port && port === destination) list.push({ fieldId: 'publication-port', key: 'admin.publicationRules.form.errors.portSameAsDestination' });
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
    const body: ShipmentPublicationRuleRequest = {
      country: f.country,
      finalDestinationCode: f.finalDestinationCode.trim().toUpperCase(),
      finalDestinationName: f.finalDestinationName.trim() || null,
      dischargePortCode: f.dischargePortCode.trim().toUpperCase() || null,
      description: f.description.trim() || null,
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateRule(editing.id, body) : this.service.createRule(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.publicationRules.form.updated' : 'admin.publicationRules.form.created', { code: body.finalDestinationCode }));
        this.closeForm();
        this.search();
        if (editing && this.historyRule()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.publicationRules.form.errors.submit'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async deactivate(rule: ShipmentPublicationRule): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.deactivate.title',
      message: 'shared.modal.deactivate.message',
      params: { name: this.ruleName(rule) },
      confirmLabel: 'shared.modal.deactivate.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.service.deactivateRule(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(translate('admin.publicationRules.list.deactivated', { rule: this.ruleName(rule) }));
        this.search();
      },
      error: (err) => {
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.publicationRules.list.deactivateError'));
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(rule: ShipmentPublicationRule): void {
    this.historyRule.set(rule);
    this.historyLoading.set(true);
    this.service.getHistory(rule.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  /** Nombre de la regla en los textos: destino final (y su nombre) desde el puerto. */
  ruleName(rule: ShipmentPublicationRule): string {
    return rule.finalDestinationName ? `${rule.finalDestinationCode} (${rule.finalDestinationName})` : rule.finalDestinationCode;
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
