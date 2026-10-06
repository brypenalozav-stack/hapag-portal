import { Component, DestroyRef, ElementRef, Injector, computed, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable, concatMap, from, last, map, of, switchMap, toArray } from 'rxjs';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { apiErrorCode } from '../../../core/http/api-error';
import {
  AvailableService,
  AvailableServices,
  SERVICE_FORM_LIMITS,
  ServiceBillingData,
  ServiceInputField,
  ServiceInputValues,
  ServiceQuote,
  ServiceReference,
  ServiceRequestDetail,
  ShipmentContainerOption,
} from '../../../core/models/service-request.model';
import {
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
  SERVICE_TEAM_KEYS,
  SERVICE_UNAVAILABLE_REASON_KEYS,
  SHIPMENT_OPERATION_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { AvailableServicesComponent } from '../available-services/available-services';
import { ServiceQuoteComponent } from '../shared/service-quote';
import { ServiceInputValuesComponent } from '../shared/service-input-values';
import { ServiceRequestPaymentComponent } from '../shared/service-request-payment';
import {
  billingErrorKeys,
  fieldHelp,
  fieldLabel,
  inputErrorKeys,
  localized,
  maxLengthOf,
  serviceErrorMessage,
} from '../shared/service-text';

type StepId = 'details' | 'billing' | 'confirm';

/** Pasos de la solicitud (identificadores, no textos). */
const STEP: Record<StepId, StepId> = { details: 'details', billing: 'billing', confirm: 'confirm' };

const STEP_KEYS: Record<StepId, string> = {
  details: 'serviceRequests.form.steps.details',
  billing: 'serviceRequests.form.steps.billing',
  confirm: 'serviceRequests.form.steps.confirm',
};

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

interface BillingForm {
  taxId: string;
  name: string;
  address: string;
  email: string;
  activity: string;
}

type BillingField = keyof BillingForm;

/** Validación de envío (todo) o de borrador (solo formatos, como el servidor al crear el borrador). */
type ValidationMode = 'submit' | 'draft';

const NUMBER = /^-?\d+([.,]\d+)?$/;
const DATE = /^\d{4}-\d{2}-\d{2}$/;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
/** RUT chileno (con o sin puntos, con guion y dígito verificador) y NIT boliviano (solo dígitos). */
const TAX_ID: Record<string, RegExp> = {
  CL: /^\d{1,2}\.?\d{3}\.?\d{3}-[\dkK]$/,
  BO: /^\d{5,15}$/,
};

/**
 * Solicitud de un servicio on demand (M2-03, M2-04, M3-07 a M3-15). Sin embarque elegido, busca el BL o booking y
 * ofrece sus servicios disponibles; con el servicio elegido, guía en pasos:
 * 1. Datos del servicio: formulario armado desde la definición (texto, área de texto, fecha, número, selección,
 *    archivo y contenedores del BL) con las mismas reglas del servidor.
 * 2. Datos de facturación, cuando la definición los exige (RUT o NIT, razón social, dirección y correo).
 * 3. Revisión y envío: cotización con el tramo vigente ahora (dentro o fuera de plazo, horas o días desde el hito) y la
 *    aceptación de la tarifa cuando corresponde (M3-09). Si la tarifa cambió al enviar, se muestra el total nuevo y se
 *    vuelve a pedir la aceptación.
 * Se puede guardar como borrador en cualquier paso y continuarlo después (?draft=). Los archivos se suben al borrador y
 * luego se envía. Tras el envío: pendiente de aprobación (ED), pendiente de pago con el cargo listo para agregar al
 * carro (o pagar desde la cuenta) o en curso sin cobro.
 */
@Component({
  selector: 'app-service-request-form',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, LoadingSpinnerComponent, StateMessageComponent,
    AvailableServicesComponent, ServiceQuoteComponent, ServiceInputValuesComponent, ServiceRequestPaymentComponent,
  ],
  templateUrl: './service-request-form.html',
  styleUrl: './service-request-form.scss',
})
export class ServiceRequestFormComponent {
  private readonly service = inject(ServiceRequestService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Parámetros de la ruta /service-requests/new: BL o booking, código del servicio y borrador a continuar. */
  bl = input<string>();
  booking = input<string>();
  code = input<string>();
  draft = input<string>();

  readonly stepKeys = STEP_KEYS;
  readonly reasonKeys = SERVICE_UNAVAILABLE_REASON_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly fileAccept = SERVICE_FORM_LIMITS.FILE_ACCEPT;

  // Búsqueda del embarque
  searchType: 'bl' | 'booking' = 'bl';
  searchValue = '';
  searchError = signal('');

  // Servicio elegido
  shipment = signal<AvailableServices | null>(null);
  setup = signal<AvailableService | null>(null);
  draftDetail = signal<ServiceRequestDetail | null>(null);
  loading = signal(false);
  loadFailed = signal(false);
  loadError = signal('');

  // Formulario
  text: Record<string, string> = {};
  selected: Record<string, string[]> = {};
  files: Record<string, File | null> = {};
  billing: BillingForm = { taxId: '', name: '', address: '', email: '', activity: '' };

  step = signal<StepId>('details');
  errors = signal<FormError[]>([]);

  // Cotización y envío
  quote = signal<ServiceQuote | null>(null);
  quoting = signal(false);
  quoteError = signal('');
  accept = signal(false);
  tariffChanged = signal(false);
  submitting = signal(false);
  savingDraft = signal(false);
  submitError = signal('');
  draftSaved = signal('');
  draftId = signal<string | null>(null);
  result = signal<ServiceRequestDetail | null>(null);
  /** Datos que se revisan antes de enviar (se fijan al entrar al último paso). */
  preview = signal<ServiceInputValues>({});

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  /** Referencia elegida (BL o booking) desde la ruta. */
  reference = computed<ServiceReference | null>(() => {
    const bl = this.bl()?.trim();
    const booking = this.booking()?.trim();
    if (bl) return { blNumber: bl };
    if (booking) return { bookingNumber: booking };
    return null;
  });

  schema = computed<ServiceInputField[]>(() => this.setup()?.inputSchema ?? []);
  steps = computed<StepId[]>(() => (this.setup()?.billingDataRequired ? ['details', 'billing', 'confirm'] : ['details', 'confirm']));
  stepIndex = computed(() => this.steps().indexOf(this.step()));
  containers = computed<ShipmentContainerOption[]>(() => this.shipment()?.containers ?? []);
  canRequest = computed(() => {
    const s = this.setup();
    return !!s && s.available && s.canRequest;
  });

  /** Se pide aceptar la tarifa cuando la definición lo exige y hay algo que pagar (M3-09). */
  acceptanceRequired = computed(() => !!this.setup()?.tariffAcceptanceRequired && !!this.quote()?.requiresPayment);

  constructor() {
    effect(() => {
      const draft = this.draft();
      const ref = this.reference();
      const code = this.code();
      untracked(() => this.start(draft ?? null, ref, code ?? null));
    });
  }

  // -------------------------------------------------------------------------
  // Carga
  // -------------------------------------------------------------------------

  private reset(): void {
    this.shipment.set(null);
    this.setup.set(null);
    this.draftDetail.set(null);
    this.loadFailed.set(false);
    this.loadError.set('');
    this.errors.set([]);
    this.quote.set(null);
    this.quoteError.set('');
    this.accept.set(false);
    this.tariffChanged.set(false);
    this.submitError.set('');
    this.draftSaved.set('');
    this.draftId.set(null);
    this.result.set(null);
    this.step.set(STEP.details);
    this.text = {};
    this.selected = {};
    this.files = {};
  }

  private start(draft: string | null, ref: ServiceReference | null, code: string | null): void {
    this.reset();
    if (ref) {
      this.searchType = ref.bookingNumber ? 'booking' : 'bl';
      this.searchValue = ref.bookingNumber ?? ref.blNumber ?? '';
    }
    if (draft) {
      this.loadDraft(draft);
    } else if (ref && code) {
      this.loadService(ref, code);
    }
  }

  retry(): void {
    this.start(this.draft() ?? null, this.reference(), this.code() ?? null);
  }

  private loadService(ref: ServiceReference, code: string): void {
    this.loading.set(true);
    this.service.getAvailable(ref, true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (shipment) => {
        this.shipment.set(shipment);
        const found = shipment.services.find((s) => s.code === code.toUpperCase()) ?? null;
        if (!found) this.loadError.set(translate('serviceRequests.form.notOffered'));
        this.setup.set(found);
        if (found) this.initForm(found, null);
        this.loading.set(false);
      },
      error: (err) => this.onLoadError(err),
    });
  }

  private loadDraft(id: string): void {
    this.loading.set(true);
    this.service.get(id).pipe(
      switchMap((detail) => this.service.getAvailable({ blNumber: detail.blNumber }, true).pipe(map((shipment) => ({ detail, shipment })))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: ({ detail, shipment }) => {
        if (!detail.canEdit) {
          this.loadError.set(translate('serviceRequests.form.notEditable', { number: detail.requestNumber }));
          this.loading.set(false);
          return;
        }
        this.shipment.set(shipment);
        this.draftDetail.set(detail);
        this.draftId.set(detail.id);
        const offered = shipment.services.find((s) => s.code === detail.definitionCode);
        // El formulario del borrador es el de su definición; la disponibilidad, la de hoy.
        const setup: AvailableService = {
          definitionId: detail.definitionId,
          code: detail.definitionCode,
          nameEs: detail.nameEs,
          nameEn: detail.nameEn,
          descriptionEs: offered?.descriptionEs,
          descriptionEn: offered?.descriptionEn,
          referenceType: offered?.referenceType ?? 'BL',
          available: offered?.available ?? false,
          canRequest: (offered?.canRequest ?? false) && detail.canSubmit,
          unavailableReasons: offered?.unavailableReasons ?? [],
          inputSchema: detail.inputSchema,
          billingDataRequired: detail.billingDataRequired,
          tariffAcceptanceRequired: detail.tariffAcceptanceRequired,
          approvalTeam: detail.approvalTeam,
          fulfillmentTeam: detail.fulfillmentTeam,
        };
        this.setup.set(setup);
        this.initForm(setup, detail);
        this.loading.set(false);
      },
      error: (err) => this.onLoadError(err),
    });
  }

  private onLoadError(err: unknown): void {
    if (isServiceUnavailable(err)) this.loadFailed.set(true);
    else if (err instanceof HttpErrorResponse && err.status === 404 && this.draft()) this.loadError.set(translate('serviceRequests.form.draftNotFound'));
    else this.loadError.set(serviceErrorMessage(err, 'serviceRequests.form.loadError'));
    this.loading.set(false);
  }

  /** Valores iniciales: los del borrador o vacíos; la facturación, con los datos de la organización (3.3.7). */
  private initForm(setup: AvailableService, detail: ServiceRequestDetail | null): void {
    const values = detail?.inputValues ?? {};
    for (const field of setup.inputSchema) {
      const v = values[field.key];
      if (field.type === 'containers') this.selected[field.key] = Array.isArray(v) ? [...v] : [];
      else if (field.type === 'file') this.files[field.key] = null;
      else this.text[field.key] = v === undefined || v === null || Array.isArray(v) ? '' : String(v);
    }
    const org = this.auth.organization();
    const b = detail?.billing;
    this.billing = {
      taxId: b?.taxId ?? org?.taxId ?? '',
      name: b?.name ?? org?.name ?? '',
      address: b?.address ?? '',
      email: b?.email ?? this.auth.currentUser()?.email ?? '',
      activity: b?.activity ?? '',
    };
  }

  // -------------------------------------------------------------------------
  // Búsqueda del embarque
  // -------------------------------------------------------------------------

  search(event: Event): void {
    event.preventDefault();
    const value = this.searchValue.trim().toUpperCase();
    if (!value) {
      this.searchError.set(translate('serviceRequests.form.search.required'));
      this.announcer.announce(this.searchError(), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('srv-search-value'));
      return;
    }
    this.searchError.set('');
    this.router.navigate(['/service-requests/new'], {
      queryParams: this.searchType === 'booking' ? { booking: value } : { bl: value },
    });
  }

  // -------------------------------------------------------------------------
  // Textos y campos
  // -------------------------------------------------------------------------

  statusClass(status: string): string {
    return SERVICE_REQUEST_STATUS_CLASS[status] ?? 'hl-badge--processing';
  }

  serviceName(): string {
    const s = this.setup();
    return s ? localized(this.locale.lang(), s.nameEs, s.nameEn) : '';
  }

  serviceDescription(): string {
    const s = this.setup();
    return s ? localized(this.locale.lang(), s.descriptionEs, s.descriptionEn) : '';
  }

  label(field: ServiceInputField): string {
    return fieldLabel(field, this.locale.lang());
  }

  help(field: ServiceInputField): string {
    return fieldHelp(field, this.locale.lang());
  }

  optionLabel(option: { labelEs: string; labelEn: string }): string {
    return localized(this.locale.lang(), option.labelEs, option.labelEn);
  }

  maxLength(field: ServiceInputField): number {
    return maxLengthOf(field);
  }

  fieldId(field: ServiceInputField): string {
    return `srv-field-${field.key}`;
  }

  /** Destino del enlace del resumen: el campo o, en contenedores, la primera casilla. */
  targetId(field: ServiceInputField): string {
    return field.type === 'containers' ? `${this.fieldId(field)}-0` : this.fieldId(field);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  fieldError(fieldId: string): FormError | undefined {
    return this.errors().find((e) => e.fieldId === fieldId);
  }

  /** Reglas del campo que se explican junto a él (formato, rango, largo, archivos admitidos). */
  hints(field: ServiceInputField): string[] {
    const list: string[] = [];
    const min = field.min ?? null;
    const max = field.max ?? null;
    if (field.type === 'number') {
      if (min !== null && max !== null) list.push(translate('serviceRequests.form.hints.range', { min, max }));
      else if (min !== null) list.push(translate('serviceRequests.form.hints.min', { min }));
      else if (max !== null) list.push(translate('serviceRequests.form.hints.max', { max }));
      if (field.integer) list.push(translate('serviceRequests.form.hints.integer'));
    } else if (field.type === 'text' || field.type === 'textarea') {
      list.push(translate('serviceRequests.form.hints.maxLength', { max: this.maxLength(field) }));
    } else if (field.type === 'file') {
      list.push(translate('serviceRequests.form.hints.file'));
    } else if (field.type === 'date') {
      list.push(translate('serviceRequests.form.hints.date'));
    }
    return list;
  }

  /** Ayuda y error del campo, para aria-describedby. */
  describedBy(field: ServiceInputField): string | null {
    const ids: string[] = [];
    if (this.help(field) || this.hints(field).length > 0) ids.push(`${this.fieldId(field)}-help`);
    if (this.hasError(this.targetId(field))) ids.push(`${this.fieldId(field)}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  }

  billingDescribedBy(field: BillingField, help = false): string | null {
    const ids: string[] = [];
    if (help) ids.push(`srv-billing-${field}-help`);
    if (this.hasError(`srv-billing-${field}`)) ids.push(`srv-billing-${field}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  }

  isSelected(field: ServiceInputField, container: string): boolean {
    return (this.selected[field.key] ?? []).includes(container);
  }

  toggleContainer(field: ServiceInputField, container: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const current = this.selected[field.key] ?? [];
    this.selected[field.key] = checked ? [...current.filter((c) => c !== container), container] : current.filter((c) => c !== container);
  }

  selectAll(field: ServiceInputField, all: boolean): void {
    this.selected[field.key] = all ? this.containers().map((c) => c.containerNumber) : [];
    this.announcer.announce(translate(all ? 'serviceRequests.form.containers.allSelected' : 'serviceRequests.form.containers.noneSelected', {
      count: this.selected[field.key].length,
    }));
  }

  onFile(field: ServiceInputField, event: Event): void {
    const input = event.target as HTMLInputElement;
    this.files[field.key] = input.files?.[0] ?? null;
  }

  existingAttachments(field: ServiceInputField) {
    return (this.draftDetail()?.attachments ?? []).filter((a) => a.fieldKey === field.key);
  }

  // -------------------------------------------------------------------------
  // Validación (mismas reglas del servidor)
  // -------------------------------------------------------------------------

  private validateDetails(mode: ValidationMode): FormError[] {
    const list: FormError[] = [];
    const complete = mode === 'submit';
    for (const field of this.schema()) {
      const id = this.targetId(field);
      const label = this.label(field);
      const push = (key: string, params: Record<string, unknown> = {}) => list.push({ fieldId: id, key, params: { label, ...params } });
      switch (field.type) {
        case 'containers': {
          if (complete && field.required && (this.selected[field.key] ?? []).length === 0) push('serviceRequests.form.errors.containersRequired');
          break;
        }
        case 'file': {
          const file = this.files[field.key];
          if (file) {
            if (!SERVICE_FORM_LIMITS.FILE_TYPES.includes(file.type)) push('serviceRequests.form.errors.fileType');
            else if (file.size > SERVICE_FORM_LIMITS.MAX_FILE_BYTES) push('serviceRequests.form.errors.fileSize');
          } else if (complete && field.required && this.existingAttachments(field).length === 0) {
            push('serviceRequests.form.errors.fileRequired');
          }
          break;
        }
        default: {
          const value = (this.text[field.key] ?? '').trim();
          if (!value) {
            if (complete && field.required) {
              push(field.type === 'select' ? 'serviceRequests.form.errors.selectRequired' : 'serviceRequests.form.errors.required');
            }
            break;
          }
          if (field.type === 'text' || field.type === 'textarea') {
            if (value.length > this.maxLength(field)) push('serviceRequests.form.errors.maxLength', { max: this.maxLength(field) });
          } else if (field.type === 'date') {
            if (!DATE.test(value) || Number.isNaN(Date.parse(value))) push('serviceRequests.form.errors.date');
          } else if (field.type === 'number') {
            if (!NUMBER.test(value)) {
              push('serviceRequests.form.errors.number');
            } else {
              const n = Number(value.replace(',', '.'));
              if (field.integer && !Number.isInteger(n)) push('serviceRequests.form.errors.integer');
              else if (field.min !== null && field.min !== undefined && n < field.min) push('serviceRequests.form.errors.min', { min: field.min });
              else if (field.max !== null && field.max !== undefined && n > field.max) push('serviceRequests.form.errors.max', { max: field.max });
            }
          } else if (field.type === 'select') {
            if (!(field.options ?? []).some((o) => o.value === value)) push('serviceRequests.form.errors.selectInvalid');
          }
        }
      }
    }
    return list;
  }

  private validateBilling(): FormError[] {
    const b = this.billing;
    const list: FormError[] = [];
    const pattern = TAX_ID[this.shipment()?.country ?? 'CL'] ?? TAX_ID['CL'];
    if (!b.taxId.trim()) list.push({ fieldId: 'srv-billing-taxId', key: 'serviceRequests.form.billing.errors.taxIdRequired' });
    else if (!pattern.test(b.taxId.trim())) list.push({ fieldId: 'srv-billing-taxId', key: 'serviceRequests.form.billing.errors.taxIdFormat' });
    if (!b.name.trim()) list.push({ fieldId: 'srv-billing-name', key: 'serviceRequests.form.billing.errors.nameRequired' });
    if (!b.address.trim()) list.push({ fieldId: 'srv-billing-address', key: 'serviceRequests.form.billing.errors.addressRequired' });
    if (!b.email.trim()) list.push({ fieldId: 'srv-billing-email', key: 'serviceRequests.form.billing.errors.emailRequired' });
    else if (!EMAIL.test(b.email.trim())) list.push({ fieldId: 'srv-billing-email', key: 'serviceRequests.form.billing.errors.emailFormat' });
    return list;
  }

  private showErrors(errors: FormError[]): boolean {
    this.errors.set(errors);
    if (errors.length === 0) return false;
    this.announcer.announce(translate('common.form.invalid'), 'assertive');
    focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
    return true;
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }

  // -------------------------------------------------------------------------
  // Pasos
  // -------------------------------------------------------------------------

  private goTo(step: StepId): void {
    this.step.set(step);
    this.announcer.announce(translate('serviceRequests.form.steps.announce', {
      number: this.stepIndex() + 1,
      total: this.steps().length,
      name: translate(STEP_KEYS[step]),
    }));
    focusAfterRender(this.injector, () => document.getElementById('srv-step-title'));
    if (step === 'confirm') {
      this.preview.set(this.inputValues());
      this.tariffChanged.set(false);
      this.requestQuote();
    }
  }

  next(event: Event): void {
    event.preventDefault();
    this.submitError.set('');
    const current = this.step();
    if (current === 'confirm') {
      this.submit();
      return;
    }
    const errors = current === 'details' ? this.validateDetails('submit') : this.validateBilling();
    if (this.showErrors(errors)) return;
    this.goTo(this.steps()[this.stepIndex() + 1]);
  }

  back(): void {
    this.errors.set([]);
    this.submitError.set('');
    const index = this.stepIndex();
    if (index > 0) this.goTo(this.steps()[index - 1]);
  }

  // -------------------------------------------------------------------------
  // Datos para la API
  // -------------------------------------------------------------------------

  private inputValues(): ServiceInputValues {
    const out: ServiceInputValues = {};
    for (const field of this.schema()) {
      if (field.type === 'file') continue;
      if (field.type === 'containers') {
        const list = this.selected[field.key] ?? [];
        if (list.length > 0) out[field.key] = list;
        continue;
      }
      const value = (this.text[field.key] ?? '').trim();
      if (!value) continue;
      out[field.key] = field.type === 'number' ? Number(value.replace(',', '.')) : value;
    }
    return out;
  }

  private billingData(): ServiceBillingData | null {
    const b = this.billing;
    const data: ServiceBillingData = {
      taxId: b.taxId.trim() || null,
      name: b.name.trim() || null,
      address: b.address.trim() || null,
      email: b.email.trim() || null,
      activity: b.activity.trim() || null,
    };
    return Object.values(data).some((v) => !!v) ? data : null;
  }

  private ref(): ServiceReference {
    const detail = this.draftDetail();
    if (detail) return { blNumber: detail.blNumber };
    return this.reference() ?? {};
  }

  // -------------------------------------------------------------------------
  // Cotización
  // -------------------------------------------------------------------------

  requestQuote(): void {
    const setup = this.setup();
    if (!setup) return;
    this.quoting.set(true);
    this.quoteError.set('');
    this.quote.set(null);
    this.service.quote({ ...this.ref(), definitionCode: setup.code, inputValues: this.inputValues() }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (quote) => {
        this.quote.set(quote);
        this.quoting.set(false);
        this.announcer.announce(translate(quote.requiresPayment ? 'serviceRequests.form.quote.ready' : 'serviceRequests.form.quote.readyFree'));
      },
      error: (err) => {
        this.quoting.set(false);
        if (this.fieldErrorsFromServer(err)) return;
        const message = serviceErrorMessage(err, 'serviceRequests.form.quote.error');
        this.quoteError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Errores de campo del servidor: vuelve al paso que corresponde y los muestra en el resumen. */
  private fieldErrorsFromServer(err: unknown): boolean {
    const inputKeys = inputErrorKeys(err);
    const code = apiErrorCode(err);
    const containersField = this.schema().find((f) => f.type === 'containers');
    if (inputKeys.length > 0 || (code === 'ServiceRequest.ContainersRequired' && containersField)) {
      const fields = this.schema().filter((f) => inputKeys.includes(f.key) || (code === 'ServiceRequest.ContainersRequired' && f === containersField));
      const errors = fields.map((f) => ({
        fieldId: this.targetId(f),
        key: f.type === 'containers' && code === 'ServiceRequest.ContainersRequired' ? 'serviceRequests.form.errors.containersRequired' : 'serviceRequests.form.errors.server',
        params: { label: this.label(f) },
      }));
      this.step.set(STEP.details);
      this.showErrors(errors.length > 0 ? errors : [{ fieldId: 'srv-step-title', key: 'serviceRequests.form.errors.serverForm' }]);
      return true;
    }
    const billingKeys = billingErrorKeys(err);
    if (code === 'ServiceRequest.BillingDataRequired' || code === 'ServiceRequest.BillingTaxIdNotAllowed' || billingKeys.length > 0) {
      const errors: FormError[] = code === 'ServiceRequest.BillingTaxIdNotAllowed'
        ? [{ fieldId: 'srv-billing-taxId', key: 'serviceRequests.form.billing.errors.taxIdNotAllowed' }]
        : billingKeys.length > 0
          ? billingKeys.map((k) => ({ fieldId: `srv-billing-${k}`, key: 'serviceRequests.form.billing.errors.server' }))
          : this.validateBilling();
      if (this.steps().includes('billing')) {
        this.step.set(STEP.billing);
        this.showErrors(errors.length > 0 ? errors : [{ fieldId: 'srv-billing-taxId', key: 'serviceRequests.form.billing.errors.server' }]);
        return true;
      }
    }
    return false;
  }

  onAccept(event: Event): void {
    this.accept.set((event.target as HTMLInputElement).checked);
  }

  // -------------------------------------------------------------------------
  // Envío y borrador
  // -------------------------------------------------------------------------

  private pendingFiles(): { field: ServiceInputField; file: File }[] {
    return this.schema()
      .filter((f) => f.type === 'file' && !!this.files[f.key])
      .map((f) => ({ field: f, file: this.files[f.key] as File }));
  }

  /** Sube los archivos elegidos al borrador, uno a uno. */
  private upload(detail: ServiceRequestDetail): Observable<ServiceRequestDetail> {
    const pending = this.pendingFiles();
    if (pending.length === 0) return of(detail);
    return from(pending).pipe(
      concatMap(({ field, file }) => this.service.uploadAttachment(detail.id, field.key, file).pipe(map(() => field.key))),
      toArray(),
      map((keys) => {
        for (const key of keys) this.files[key] = null;
        return detail;
      }),
    );
  }

  /** Crea o actualiza el borrador y sube los archivos. */
  private saveDraft$(): Observable<ServiceRequestDetail> {
    const setup = this.setup() as AvailableService;
    const id = this.draftId();
    const save$ = id
      ? this.service.update(id, { inputValues: this.inputValues(), billing: this.billingData() })
      : this.service.create({ ...this.ref(), definitionCode: setup.code, inputValues: this.inputValues(), billing: this.billingData(), submit: false });
    return save$.pipe(
      map((detail) => {
        this.draftId.set(detail.id);
        this.draftDetail.set(detail);
        return detail;
      }),
      switchMap((detail) => this.upload(detail)),
      switchMap((detail) => this.service.get(detail.id)),
      map((detail) => {
        this.draftDetail.set(detail);
        return detail;
      }),
    );
  }

  saveDraft(): void {
    if (this.savingDraft() || this.submitting()) return;
    this.submitError.set('');
    this.draftSaved.set('');
    if (this.showErrors(this.validateDetails('draft'))) {
      this.step.set(STEP.details);
      return;
    }
    this.savingDraft.set(true);
    this.saveDraft$().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.savingDraft.set(false);
        const message = translate('serviceRequests.form.draft.saved', { number: detail.requestNumber });
        this.draftSaved.set(message);
        this.announcer.announce(message);
      },
      error: (err) => {
        this.savingDraft.set(false);
        if (this.fieldErrorsFromServer(err)) return;
        const message = serviceErrorMessage(err, 'serviceRequests.form.draft.error');
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  private submit(): void {
    if (this.submitting() || this.savingDraft()) return;
    const setup = this.setup();
    const quote = this.quote();
    if (!setup) return;
    this.draftSaved.set('');
    if (this.quoting()) return;
    if (!quote) {
      // Sin cotización vigente no se envía: se vuelve a calcular y el usuario la revisa.
      this.requestQuote();
      return;
    }
    if (this.acceptanceRequired() && !this.accept()) {
      this.showErrors([{ fieldId: 'srv-accept-tariff', key: 'serviceRequests.form.errors.acceptTariff' }]);
      return;
    }
    // Las reglas se vuelven a revisar por si se volvió atrás y se cambió algo.
    const detailErrors = this.validateDetails('submit');
    if (detailErrors.length > 0) {
      this.step.set(STEP.details);
      this.showErrors(detailErrors);
      return;
    }
    this.errors.set([]);
    const acceptTariff = this.acceptanceRequired() ? this.accept() : undefined;
    const acceptedTotal = this.acceptanceRequired() ? quote.totalAmount : undefined;
    this.submitting.set(true);
    const submit$ = this.draftId() || this.pendingFiles().length > 0
      ? this.saveDraft$().pipe(switchMap((draft) => this.service.submit(draft.id, { acceptTariff, acceptedTotal })))
      : this.service.create({
        ...this.ref(),
        definitionCode: setup.code,
        inputValues: this.inputValues(),
        billing: setup.billingDataRequired ? this.billingData() : null,
        submit: true,
        acceptTariff,
        acceptedTotal,
      });
    submit$.pipe(last(), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.submitting.set(false);
        this.result.set(detail);
        const statusKey = SERVICE_REQUEST_STATUS_KEYS[detail.status];
        this.announcer.announce(translate('serviceRequests.form.result.announce', {
          number: detail.requestNumber,
          status: statusKey ? translate(statusKey) : detail.status,
        }));
        focusAfterRender(this.injector, () => document.getElementById('srv-result-title'));
      },
      error: (err) => {
        this.submitting.set(false);
        this.onSubmitError(err);
      },
    });
  }

  private onSubmitError(err: unknown): void {
    const code = apiErrorCode(err);
    if (code === 'ServiceRequest.TariffChanged') {
      // La tarifa vigente cambió (otro tramo o tarifa nueva): se muestra el total nuevo y se pide aceptarlo otra vez.
      this.accept.set(false);
      this.tariffChanged.set(true);
      this.announcer.announce(translate('serviceRequests.form.quote.changed'), 'assertive');
      this.requestQuote();
      return;
    }
    if (code === 'ServiceRequest.TariffNotAccepted') {
      this.accept.set(false);
      this.showErrors([{ fieldId: 'srv-accept-tariff', key: 'serviceRequests.form.errors.acceptTariff' }]);
      return;
    }
    if (this.fieldErrorsFromServer(err)) return;
    const message = serviceErrorMessage(err, 'serviceRequests.form.errors.submit');
    this.submitError.set(message);
    this.announcer.announce(message, 'assertive');
  }

  onPaymentAdded(): void {
    const r = this.result();
    if (r) this.announcer.announce(translate('serviceRequests.form.result.addedToCart', { number: r.requestNumber }));
  }
}
