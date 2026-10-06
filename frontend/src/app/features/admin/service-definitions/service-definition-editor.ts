import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ServiceDefinitionService } from '../../../core/services/service-definition.service';
import { TariffService } from '../../../core/services/tariff.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ChargeConcept } from '../../../core/models/tariff.model';
import {
  SERVICE_AVAILABILITY_WINDOWS,
  SERVICE_FIELD_TYPES,
  SERVICE_FORM_LIMITS,
  SERVICE_MILESTONES,
  SERVICE_PRICING_MODES,
  SERVICE_QUANTITY_MODES,
  SERVICE_REFERENCE_TYPES,
  SERVICE_TEAMS,
  SERVICE_TIMING_RULES,
  ServiceAvailabilityWindow,
  ServiceDefinition,
  ServiceDefinitionChange,
  ServiceDefinitionInput,
  ServiceFieldType,
  ServiceInputField,
  ServiceMilestone,
  ServiceOperation,
  ServicePricingMode,
  ServiceQuantityMode,
  ServiceReferenceType,
  ServiceTeam,
  ServiceTimingRule,
} from '../../../core/models/service-request.model';
import {
  ACCESS_ACTION_KEYS,
  CHARGE_CONCEPT_KEYS,
  MAINTAINER_ACTION_KEYS,
  SERVICE_AVAILABILITY_WINDOW_KEYS,
  SERVICE_FIELD_TYPE_KEYS,
  SERVICE_MILESTONE_KEYS,
  SERVICE_PRICING_MODE_KEYS,
  SERVICE_QUANTITY_MODE_KEYS,
  SERVICE_REFERENCE_TYPE_KEYS,
  SERVICE_TEAM_KEYS,
  SERVICE_TIMING_RULE_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { serviceErrorMessage } from '../../service-requests/shared/service-text';
import { ServiceDefinitionSnapshotComponent } from './service-definition-snapshot';

/** Opción de una selección en edición. */
interface OptionRow {
  uid: number;
  value: string;
  labelEs: string;
  labelEn: string;
}

/** Campo del formulario en edición: los números llegan como texto desde los campos. */
interface FieldRow {
  uid: number;
  key: string;
  labelEs: string;
  labelEn: string;
  type: ServiceFieldType;
  required: boolean;
  maxLength: string;
  min: string;
  max: string;
  integer: boolean;
  helpEs: string;
  helpEn: string;
  options: OptionRow[];
}

interface DefinitionForm {
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs: string;
  descriptionEn: string;
  operations: Record<ServiceOperation, boolean>;
  countries: Record<string, boolean>;
  referenceType: ServiceReferenceType;
  requiredBlStatuses: string;
  availabilityWindow: ServiceAvailabilityWindow;
  requiresContainers: boolean;
  allowMultiplePerBl: boolean;
  billingDataRequired: boolean;
  tariffAcceptanceRequired: boolean;
  pricingMode: ServicePricingMode;
  chargeConceptCode: string;
  tariffCode: string;
  lateTariffCode: string;
  quantityMode: ServiceQuantityMode;
  measureFieldKey: string;
  milestone: ServiceMilestone;
  milestoneOffsetHours: string;
  deadlineRuleCode: string;
  timingRule: ServiceTimingRule;
  taxable: boolean;
  exemptionConcept: string;
  excludeShipperOwnedContainers: boolean;
  approvalTeam: ServiceTeam;
  fulfillmentTeam: ServiceTeam;
  requiresOutputDocument: boolean;
  actionCode: string;
  displayOrder: string;
  isActive: boolean;
}

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

function emptyForm(): DefinitionForm {
  return {
    code: '',
    nameEs: '',
    nameEn: '',
    descriptionEs: '',
    descriptionEn: '',
    operations: { IMPORT: false, EXPORT: false },
    countries: { CL: false, BO: false },
    referenceType: 'BL',
    requiredBlStatuses: '',
    availabilityWindow: 'Always',
    requiresContainers: false,
    allowMultiplePerBl: true,
    billingDataRequired: true,
    tariffAcceptanceRequired: false,
    pricingMode: 'Tariff',
    chargeConceptCode: '',
    tariffCode: '',
    lateTariffCode: '',
    quantityMode: 'PerRequest',
    measureFieldKey: '',
    milestone: 'None',
    milestoneOffsetHours: '0',
    deadlineRuleCode: '',
    timingRule: 'None',
    taxable: true,
    exemptionConcept: '',
    excludeShipperOwnedContainers: false,
    approvalTeam: 'None',
    fulfillmentTeam: 'None',
    requiresOutputDocument: false,
    actionCode: 'local-charges-on-demand.pay',
    displayOrder: '100',
    isActive: true,
  };
}

const CODE = /^[A-Za-z][A-Za-z0-9_]{1,49}$/;
const FIELD_KEY = /^[a-z][a-zA-Z0-9]{0,39}$/;
const INTEGER = /^-?\d+$/;
const POSITIVE_INTEGER = /^\d+$/;
const DECIMAL = /^-?\d+([.,]\d+)?$/;

function toNumber(text: string): number {
  return Number(text.trim().replace(',', '.'));
}

function optional(text: string): string | null {
  const t = text.trim();
  return t === '' ? null : t;
}

/**
 * Crear o editar la definición de un servicio on demand (M2-03, M2-04): datos generales y acción de la matriz de
 * accesos (M1-11); condiciones (operaciones, países, ventana del embarque, estados del BL, contenedores, una solicitud
 * por BL); formulario tipado con un editor estructurado (agregar, quitar y reordenar campos con su tipo, reglas y
 * opciones); facturación y aceptación de tarifa; cobro (concepto del mantenedor de tarifas M8-01, códigos de tarifa
 * en plazo y fuera de plazo, por solicitud o por contenedor, medida para tramos, exención de Nexus y SOC); plazos
 * (hito, desfase, regla de plazo); equipos de aprobación y atención y documento de salida. Las reglas se validan antes
 * de enviar con las mismas del servidor, que las vuelve a exigir. Debajo, el registro de cambios (NF-15).
 */
@Component({
  selector: 'app-service-definition-editor',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ServiceDefinitionSnapshotComponent],
  templateUrl: './service-definition-editor.html',
  styleUrl: './service-definition-editor.scss',
})
export class ServiceDefinitionEditorComponent implements OnInit {
  private readonly service = inject(ServiceDefinitionService);
  private readonly tariffs = inject(TariffService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** ID de la definición (ruta /admin/service-definitions/:id); sin él se crea una nueva. */
  id = input<string>();

  readonly fieldTypes = SERVICE_FIELD_TYPES;
  readonly pricingModes = SERVICE_PRICING_MODES;
  readonly quantityModes = SERVICE_QUANTITY_MODES;
  readonly milestones = SERVICE_MILESTONES;
  readonly timingRules = SERVICE_TIMING_RULES;
  readonly windows = SERVICE_AVAILABILITY_WINDOWS;
  readonly referenceTypes = SERVICE_REFERENCE_TYPES;
  readonly teams = SERVICE_TEAMS;
  readonly actionCodes = Object.keys(ACCESS_ACTION_KEYS);
  readonly maxFields = SERVICE_FORM_LIMITS.MAX_FIELDS;

  readonly fieldTypeKeys = SERVICE_FIELD_TYPE_KEYS;
  readonly pricingKeys = SERVICE_PRICING_MODE_KEYS;
  readonly quantityKeys = SERVICE_QUANTITY_MODE_KEYS;
  readonly milestoneKeys = SERVICE_MILESTONE_KEYS;
  readonly timingRuleKeys = SERVICE_TIMING_RULE_KEYS;
  readonly windowKeys = SERVICE_AVAILABILITY_WINDOW_KEYS;
  readonly referenceKeys = SERVICE_REFERENCE_TYPE_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly maintainerActionKeys = MAINTAINER_ACTION_KEYS;

  form: DefinitionForm = emptyForm();
  fields: FieldRow[] = [];
  private nextUid = 1;

  concepts = signal<ChargeConcept[]>([]);
  definition = signal<ServiceDefinition | null>(null);
  history = signal<ServiceDefinitionChange[]>([]);
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
    this.tariffs.getConcepts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.service.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (definition) => {
        this.fill(definition);
        this.loading.set(false);
        this.loadHistory();
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.loadError.set(translate('admin.serviceDefinitions.editor.notFound'));
        else this.loadError.set(serviceErrorMessage(err, 'admin.serviceDefinitions.editor.loadError'));
        this.loading.set(false);
      },
    });
  }

  loadHistory(): void {
    const id = this.id();
    if (!id) return;
    this.historyLoading.set(true);
    this.service.history(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  private fill(d: ServiceDefinition): void {
    this.definition.set(d);
    this.form = {
      code: d.code,
      nameEs: d.nameEs,
      nameEn: d.nameEn,
      descriptionEs: d.descriptionEs ?? '',
      descriptionEn: d.descriptionEn ?? '',
      operations: { IMPORT: d.operations.includes('IMPORT'), EXPORT: d.operations.includes('EXPORT') },
      countries: { CL: d.countries.includes('CL'), BO: d.countries.includes('BO') },
      referenceType: d.referenceType,
      requiredBlStatuses: (d.requiredBlStatuses ?? []).join(', '),
      availabilityWindow: d.availabilityWindow,
      requiresContainers: d.requiresContainers,
      allowMultiplePerBl: d.allowMultiplePerBl,
      billingDataRequired: d.billingDataRequired,
      tariffAcceptanceRequired: d.tariffAcceptanceRequired,
      pricingMode: d.pricingMode,
      chargeConceptCode: d.chargeConceptCode ?? '',
      tariffCode: d.tariffCode ?? '',
      lateTariffCode: d.lateTariffCode ?? '',
      quantityMode: d.quantityMode,
      measureFieldKey: d.measureFieldKey ?? '',
      milestone: d.milestone,
      milestoneOffsetHours: String(d.milestoneOffsetHours ?? 0),
      deadlineRuleCode: d.deadlineRuleCode ?? '',
      timingRule: d.timingRule,
      taxable: d.taxable,
      exemptionConcept: d.exemptionConcept ?? '',
      excludeShipperOwnedContainers: d.excludeShipperOwnedContainers,
      approvalTeam: d.approvalTeam,
      fulfillmentTeam: d.fulfillmentTeam,
      requiresOutputDocument: d.requiresOutputDocument,
      actionCode: d.actionCode,
      displayOrder: String(d.displayOrder),
      isActive: d.isActive,
    };
    this.fields = d.inputSchema.map((f) => this.toRow(f));
  }

  private toRow(f: ServiceInputField): FieldRow {
    const text = (n: number | null | undefined) => (n === null || n === undefined ? '' : String(n));
    return {
      uid: this.nextUid++,
      key: f.key,
      labelEs: f.labelEs,
      labelEn: f.labelEn,
      type: f.type,
      required: !!f.required,
      maxLength: text(f.maxLength),
      min: text(f.min),
      max: text(f.max),
      integer: !!f.integer,
      helpEs: f.helpEs ?? '',
      helpEn: f.helpEn ?? '',
      options: (f.options ?? []).map((o) => ({ uid: this.nextUid++, value: o.value, labelEs: o.labelEs, labelEn: o.labelEn })),
    };
  }

  // -------------------------------------------------------------------------
  // Editor del formulario
  // -------------------------------------------------------------------------

  /** Campos numéricos: la medida de los tramos se toma de uno de ellos. */
  numberFields(): FieldRow[] {
    return this.fields.filter((f) => f.type === 'number' && f.key.trim() !== '');
  }

  fieldName(field: FieldRow, index: number): string {
    return field.key.trim() || translate('admin.serviceDefinitions.editor.fields.unnamed', { number: index + 1 });
  }

  addField(): void {
    if (this.fields.length >= this.maxFields) {
      this.announcer.announce(translate('admin.serviceDefinitions.editor.fields.maxReached', { max: this.maxFields }), 'assertive');
      return;
    }
    this.fields = [...this.fields, {
      uid: this.nextUid++, key: '', labelEs: '', labelEn: '', type: 'text', required: false, maxLength: '', min: '', max: '',
      integer: false, helpEs: '', helpEn: '', options: [],
    }];
    const index = this.fields.length - 1;
    this.announcer.announce(translate('admin.serviceDefinitions.editor.fields.added', { number: index + 1 }));
    focusAfterRender(this.injector, () => document.getElementById(`def-field-${index}-key`));
  }

  removeField(index: number): void {
    const name = this.fieldName(this.fields[index], index);
    this.fields = this.fields.filter((_, i) => i !== index);
    if (this.form.measureFieldKey && !this.fields.some((f) => f.key === this.form.measureFieldKey)) this.form.measureFieldKey = '';
    this.announcer.announce(translate('admin.serviceDefinitions.editor.fields.removed', { name }));
    focusAfterRender(this.injector, () => document.getElementById(this.fields.length > 0 ? `def-field-${Math.min(index, this.fields.length - 1)}-key` : 'def-add-field'));
  }

  moveField(index: number, delta: -1 | 1): void {
    const target = index + delta;
    if (target < 0 || target >= this.fields.length) return;
    const list = [...this.fields];
    [list[index], list[target]] = [list[target], list[index]];
    this.fields = list;
    this.announcer.announce(translate('admin.serviceDefinitions.editor.fields.moved', {
      name: this.fieldName(list[target], target),
      position: target + 1,
      total: list.length,
    }));
    // El foco sigue al campo movido, en el mismo botón si sigue disponible.
    const button = delta < 0 ? (target > 0 ? 'up' : 'down') : (target < list.length - 1 ? 'down' : 'up');
    focusAfterRender(this.injector, () => document.getElementById(`def-field-${target}-${button}`));
  }

  onTypeChange(field: FieldRow, type: ServiceFieldType, index: number): void {
    field.type = type;
    if (type === 'select' && field.options.length === 0) this.addOption(field, index, false);
    if (type !== 'select') field.options = [];
    if (type !== 'number') {
      field.min = '';
      field.max = '';
      field.integer = false;
      if (this.form.measureFieldKey === field.key) this.form.measureFieldKey = '';
    }
    if (type !== 'text' && type !== 'textarea') field.maxLength = '';
  }

  addOption(field: FieldRow, fieldIndex: number, focus = true): void {
    field.options = [...field.options, { uid: this.nextUid++, value: '', labelEs: '', labelEn: '' }];
    const optionIndex = field.options.length - 1;
    if (focus) {
      this.announcer.announce(translate('admin.serviceDefinitions.editor.options.added', { number: optionIndex + 1 }));
      focusAfterRender(this.injector, () => document.getElementById(`def-field-${fieldIndex}-option-${optionIndex}-value`));
    }
  }

  removeOption(field: FieldRow, fieldIndex: number, optionIndex: number): void {
    field.options = field.options.filter((_, i) => i !== optionIndex);
    this.announcer.announce(translate('admin.serviceDefinitions.editor.options.removed', { number: optionIndex + 1 }));
    focusAfterRender(this.injector, () => document.getElementById(`def-field-${fieldIndex}-add-option`));
  }

  // -------------------------------------------------------------------------
  // Validación (mismas reglas del servidor)
  // -------------------------------------------------------------------------

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!CODE.test(f.code.trim())) list.push({ fieldId: 'def-code', key: 'admin.serviceDefinitions.editor.errors.code' });
    if (!f.nameEs.trim()) list.push({ fieldId: 'def-name-es', key: 'admin.serviceDefinitions.editor.errors.nameEs' });
    if (!f.nameEn.trim()) list.push({ fieldId: 'def-name-en', key: 'admin.serviceDefinitions.editor.errors.nameEn' });
    if (!f.actionCode) list.push({ fieldId: 'def-action', key: 'admin.serviceDefinitions.editor.errors.action' });
    if (!INTEGER.test(f.displayOrder.trim())) list.push({ fieldId: 'def-order', key: 'admin.serviceDefinitions.editor.errors.order' });
    if (!f.operations.IMPORT && !f.operations.EXPORT) list.push({ fieldId: 'def-operation-IMPORT', key: 'admin.serviceDefinitions.editor.errors.operations' });
    if (!f.countries['CL'] && !f.countries['BO']) list.push({ fieldId: 'def-country-CL', key: 'admin.serviceDefinitions.editor.errors.countries' });

    // Formulario
    if (this.fields.length > this.maxFields) list.push({ fieldId: 'def-add-field', key: 'admin.serviceDefinitions.editor.errors.tooManyFields', params: { max: this.maxFields } });
    const keys = new Set<string>();
    this.fields.forEach((field, i) => {
      const number = i + 1;
      const key = field.key.trim();
      if (!FIELD_KEY.test(key)) {
        list.push({ fieldId: `def-field-${i}-key`, key: 'admin.serviceDefinitions.editor.errors.fieldKey', params: { number } });
      } else if (keys.has(key)) {
        list.push({ fieldId: `def-field-${i}-key`, key: 'admin.serviceDefinitions.editor.errors.fieldKeyDuplicated', params: { number } });
      }
      keys.add(key);
      if (!field.labelEs.trim()) list.push({ fieldId: `def-field-${i}-label-es`, key: 'admin.serviceDefinitions.editor.errors.fieldLabelEs', params: { number } });
      if (!field.labelEn.trim()) list.push({ fieldId: `def-field-${i}-label-en`, key: 'admin.serviceDefinitions.editor.errors.fieldLabelEn', params: { number } });
      if (field.maxLength.trim() && (!POSITIVE_INTEGER.test(field.maxLength.trim()) || Number(field.maxLength) <= 0)) {
        list.push({ fieldId: `def-field-${i}-max-length`, key: 'admin.serviceDefinitions.editor.errors.maxLength', params: { number } });
      }
      const minOk = !field.min.trim() || DECIMAL.test(field.min.trim());
      const maxOk = !field.max.trim() || DECIMAL.test(field.max.trim());
      if (!minOk) list.push({ fieldId: `def-field-${i}-min`, key: 'admin.serviceDefinitions.editor.errors.min', params: { number } });
      if (!maxOk) list.push({ fieldId: `def-field-${i}-max`, key: 'admin.serviceDefinitions.editor.errors.max', params: { number } });
      if (minOk && maxOk && field.min.trim() && field.max.trim() && toNumber(field.min) > toNumber(field.max)) {
        list.push({ fieldId: `def-field-${i}-min`, key: 'admin.serviceDefinitions.editor.errors.minMax', params: { number } });
      }
      if (field.type === 'select') {
        if (field.options.length === 0) {
          list.push({ fieldId: `def-field-${i}-add-option`, key: 'admin.serviceDefinitions.editor.errors.options', params: { number } });
        }
        const values = new Set<string>();
        field.options.forEach((o, j) => {
          const option = j + 1;
          const value = o.value.trim();
          if (!value) list.push({ fieldId: `def-field-${i}-option-${j}-value`, key: 'admin.serviceDefinitions.editor.errors.optionValue', params: { number, option } });
          else if (values.has(value)) list.push({ fieldId: `def-field-${i}-option-${j}-value`, key: 'admin.serviceDefinitions.editor.errors.optionDuplicated', params: { number, option } });
          values.add(value);
          if (!o.labelEs.trim() || !o.labelEn.trim()) {
            list.push({ fieldId: `def-field-${i}-option-${j}-label-es`, key: 'admin.serviceDefinitions.editor.errors.optionLabels', params: { number, option } });
          }
        });
      }
    });
    const containerFields = this.fields.filter((x) => x.type === 'containers').length;
    if (containerFields > 1) list.push({ fieldId: 'def-add-field', key: 'admin.serviceDefinitions.editor.errors.containersFields' });

    // Cobro
    if (f.pricingMode !== 'None' && !f.chargeConceptCode) list.push({ fieldId: 'def-concept', key: 'admin.serviceDefinitions.editor.errors.concept' });
    if (f.pricingMode === 'SourceCharge' && f.quantityMode !== 'PerRequest') {
      list.push({ fieldId: 'def-quantity', key: 'admin.serviceDefinitions.editor.errors.sourceChargePerRequest' });
    }
    if (f.quantityMode === 'PerContainer' && containerFields === 0) {
      list.push({ fieldId: 'def-quantity', key: 'admin.serviceDefinitions.editor.errors.perContainer' });
    }
    if (f.measureFieldKey && !this.fields.some((x) => x.type === 'number' && x.key.trim() === f.measureFieldKey)) {
      list.push({ fieldId: 'def-measure', key: 'admin.serviceDefinitions.editor.errors.measure' });
    }

    // Plazos
    if (!INTEGER.test(f.milestoneOffsetHours.trim())) list.push({ fieldId: 'def-offset', key: 'admin.serviceDefinitions.editor.errors.offset' });
    if (f.timingRule !== 'None' && f.milestone === 'None') list.push({ fieldId: 'def-timing-rule', key: 'admin.serviceDefinitions.editor.errors.timingMilestone' });
    if (f.milestone === 'CustomsDeadline' && !f.deadlineRuleCode.trim()) list.push({ fieldId: 'def-deadline-rule', key: 'admin.serviceDefinitions.editor.errors.deadlineRule' });
    return list;
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }

  private body(): ServiceDefinitionInput {
    const f = this.form;
    const number = (text: string) => (text.trim() === '' ? null : toNumber(text));
    const statuses = f.requiredBlStatuses.split(',').map((s) => s.trim()).filter((s) => s !== '');
    return {
      code: f.code.trim(),
      nameEs: f.nameEs.trim(),
      nameEn: f.nameEn.trim(),
      descriptionEs: optional(f.descriptionEs),
      descriptionEn: optional(f.descriptionEn),
      operations: (['IMPORT', 'EXPORT'] as ServiceOperation[]).filter((o) => f.operations[o]),
      countries: ['CL', 'BO'].filter((c) => f.countries[c]),
      referenceType: f.referenceType,
      requiredBlStatuses: statuses.length > 0 ? statuses : null,
      availabilityWindow: f.availabilityWindow,
      requiresContainers: f.requiresContainers,
      allowMultiplePerBl: f.allowMultiplePerBl,
      inputSchema: this.fields.map((field) => ({
        key: field.key.trim(),
        labelEs: field.labelEs.trim(),
        labelEn: field.labelEn.trim(),
        type: field.type,
        required: field.required,
        ...(field.type === 'select'
          ? { options: field.options.map((o) => ({ value: o.value.trim(), labelEs: o.labelEs.trim(), labelEn: o.labelEn.trim() })) }
          : {}),
        ...(field.type === 'number' ? { min: number(field.min), max: number(field.max), integer: field.integer } : {}),
        ...((field.type === 'text' || field.type === 'textarea') && field.maxLength.trim() ? { maxLength: Number(field.maxLength) } : {}),
        helpEs: optional(field.helpEs),
        helpEn: optional(field.helpEn),
      })),
      billingDataRequired: f.billingDataRequired,
      tariffAcceptanceRequired: f.tariffAcceptanceRequired,
      pricingMode: f.pricingMode,
      chargeConceptCode: f.pricingMode === 'None' ? null : optional(f.chargeConceptCode),
      tariffCode: optional(f.tariffCode),
      lateTariffCode: optional(f.lateTariffCode),
      quantityMode: f.quantityMode,
      measureFieldKey: optional(f.measureFieldKey),
      milestone: f.milestone,
      milestoneOffsetHours: Number(f.milestoneOffsetHours.trim()),
      deadlineRuleCode: optional(f.deadlineRuleCode),
      timingRule: f.timingRule,
      taxable: f.taxable,
      exemptionConcept: optional(f.exemptionConcept),
      excludeShipperOwnedContainers: f.excludeShipperOwnedContainers,
      approvalTeam: f.approvalTeam,
      fulfillmentTeam: f.fulfillmentTeam,
      requiresOutputDocument: f.requiresOutputDocument,
      actionCode: f.actionCode,
      displayOrder: Number(f.displayOrder.trim()),
      isActive: f.isActive,
    };
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
    const id = this.id();
    this.saving.set(true);
    const request$ = id ? this.service.update(id, this.body()) : this.service.create(this.body());
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (definition) => {
        this.saving.set(false);
        const message = translate(id ? 'admin.serviceDefinitions.editor.updated' : 'admin.serviceDefinitions.editor.created');
        this.announcer.announce(message);
        if (id) {
          this.fill(definition);
          this.saved.set(message);
          this.loadHistory();
        } else {
          this.router.navigate(['/admin/service-definitions', definition.id]);
        }
      },
      error: (err) => {
        this.saving.set(false);
        const message = serviceErrorMessage(err, 'admin.serviceDefinitions.editor.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Desactiva la definición; queda en el registro de cambios (NF-15). */
  deactivate(): void {
    const id = this.id();
    if (!id) return;
    this.deactivating.set(true);
    this.saveError.set('');
    this.service.deactivate(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.deactivating.set(false);
        this.announcer.announce(translate('admin.serviceDefinitions.editor.deactivated'));
        this.load();
      },
      error: (err) => {
        this.deactivating.set(false);
        const message = serviceErrorMessage(err, 'admin.serviceDefinitions.editor.errors.deactivate');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
