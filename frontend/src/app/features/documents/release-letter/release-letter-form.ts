import { Component, DestroyRef, ElementRef, Injector, computed, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  LegalEntityType,
  ReleaseLetterContainerOption,
  ReleaseLetterContext,
  ReleaseLetterTatc,
} from '../../../core/models/document.model';
import { apiErrorCode } from '../../../core/http/api-error';
import {
  LEGAL_ENTITY_TYPE_KEYS,
  RELEASE_LETTER_CARRIER_SOURCE_KEYS,
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
  TATC_PENDING_REASON_KEYS,
  TATC_STATUS_CLASS,
  TATC_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Huso de la operación de Bolivia (NF-22). */
const BOLIVIA_TIME_ZONE = 'America/La_Paz';

/** Largos máximos del contrato (el servidor los vuelve a validar). */
const LIMITS = { NAME: 200, TAX_ID: 30, ADDRESS: 300, PLATE: 20, OBSERVATIONS: 1000 } as const;

type CarrierMode = 'registered' | 'free';

type TextField =
  | 'consigneeName'
  | 'consigneeTaxId'
  | 'consigneeAddress'
  | 'legalRepresentativeName'
  | 'legalRepresentativeId'
  | 'carrierOrganizationId'
  | 'carrierName'
  | 'carrierTaxId'
  | 'driverName'
  | 'driverId'
  | 'truckPlate'
  | 'observations';

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Solicitud de la carta de liberación y desconsolidado (M6-08, Bolivia importación): el cliente elige las unidades
 * (con el TATC de cada una, M2-09), el tipo de sociedad del consignatario (empresa: razón social, NIT, domicilio y
 * representante legal con su documento; persona natural: nombre y documento de identidad) y el transportista (uno
 * registrado vinculado al BL o pre-creado, M1-09, o sus datos). La solicitud queda pendiente de aprobación de Customer
 * Service; la carta se emite al aprobarse y queda en el repositorio (M6-09). Estas definiciones son provisorias hasta
 * su validación con el área legal y la pantalla lo dice.
 */
@Component({
  selector: 'app-release-letter-form',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './release-letter-form.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ReleaseLetterFormComponent {
  private readonly service = inject(DocumentService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();

  readonly timeZone = BOLIVIA_TIME_ZONE;
  readonly limits = LIMITS;
  readonly entityKeys = LEGAL_ENTITY_TYPE_KEYS;
  readonly sourceKeys = RELEASE_LETTER_CARRIER_SOURCE_KEYS;
  readonly tatcKeys = TATC_STATUS_KEYS;
  readonly tatcClass = TATC_STATUS_CLASS;
  readonly reasonKeys = TATC_PENDING_REASON_KEYS;
  readonly requestStatusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly requestStatusClass = SERVICE_REQUEST_STATUS_CLASS;

  data = signal<ReleaseLetterContext | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  selected = signal<string[]>([]);
  entityType = signal<LegalEntityType>('COMPANY');
  carrierMode = signal<CarrierMode>('free');
  values = signal<Record<TextField, string>>({
    consigneeName: '',
    consigneeTaxId: '',
    consigneeAddress: '',
    legalRepresentativeName: '',
    legalRepresentativeId: '',
    carrierOrganizationId: '',
    carrierName: '',
    carrierTaxId: '',
    driverName: '',
    driverId: '',
    truckPlate: '',
    observations: '',
  });
  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  /** Solicitar: la carta aplica, la matriz de M1-11 lo permite y el perfil opera en una organización cliente. */
  canSubmit = computed(() => {
    const d = this.data();
    const org = this.auth.organization();
    return !!d && d.applicable && d.canRequest && this.auth.canOperate() && !!org && org.organizationType !== 'Internal';
  });

  /** TATC de las unidades elegidas, tal como se registrará al enviar. */
  selectedTatc = computed<ReleaseLetterContainerOption[]>(() => {
    const chosen = new Set(this.selected());
    return (this.data()?.containers ?? []).filter((c) => chosen.has(c.containerNumber));
  });

  isCompany = computed(() => this.entityType() === 'COMPANY');

  errors = computed<FormError[]>(() => {
    if (!this.submitted()) return [];
    const v = this.values();
    const list: FormError[] = [];
    const required = (value: string, fieldId: string, key: string) => {
      if (!value.trim()) list.push({ fieldId, key });
    };
    if (this.selected().length === 0) list.push({ fieldId: 'release-containers', key: 'documents.releaseLetter.errors.containersRequired' });
    required(v.consigneeName, 'release-consignee-name', this.isCompany() ? 'documents.releaseLetter.errors.legalNameRequired' : 'documents.releaseLetter.errors.personNameRequired');
    required(v.consigneeTaxId, 'release-consignee-tax-id', this.isCompany() ? 'documents.releaseLetter.errors.nitRequired' : 'documents.releaseLetter.errors.idDocumentRequired');
    if (this.isCompany()) {
      required(v.consigneeAddress, 'release-consignee-address', 'documents.releaseLetter.errors.addressRequired');
      required(v.legalRepresentativeName, 'release-rep-name', 'documents.releaseLetter.errors.representativeRequired');
      required(v.legalRepresentativeId, 'release-rep-id', 'documents.releaseLetter.errors.representativeIdRequired');
    }
    if (this.carrierMode() === 'registered') {
      required(v.carrierOrganizationId, 'release-carrier-select', 'documents.releaseLetter.errors.carrierRequired');
    } else {
      required(v.carrierName, 'release-carrier-name', 'documents.releaseLetter.errors.carrierNameRequired');
      required(v.carrierTaxId, 'release-carrier-tax-id', 'documents.releaseLetter.errors.carrierTaxIdRequired');
    }
    return list;
  });

  constructor() {
    effect(() => {
      this.blNumber();
      untracked(() => this.load(true));
    });
  }

  load(reset: boolean): void {
    this.loading.set(this.data() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getReleaseLetter(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
        if (reset) this.prefill(data);
        // Una unidad que quedó en otra carta pendiente sale de la selección.
        const free = new Set(data.containers.filter((c) => !c.pendingRequestNumber).map((c) => c.containerNumber));
        this.selected.update((list) => list.filter((n) => free.has(n)));
      },
      error: (err) => {
        this.data.set(null);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('documents.section.notFound', { bl: this.blNumber() }));
        else this.error.set(documentErrorMessage(err, 'documents.releaseLetter.form.loadError'));
      },
    });
  }

  /** Consignatario del BL (WCAG 3.3.7) y, si hay transportistas registrados, se propone elegir uno. */
  private prefill(data: ReleaseLetterContext): void {
    this.values.update((v) => ({ ...v, consigneeName: data.consignee.name ?? '', consigneeTaxId: data.consignee.taxId ?? '' }));
    this.carrierMode.set(data.carriers.length > 0 ? 'registered' : 'free');
    // Con una sola unidad disponible queda elegida.
    const free = data.containers.filter((c) => !c.pendingRequestNumber);
    if (free.length === 1) this.selected.set([free[0].containerNumber]);
  }

  fieldError(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  isSelected(containerNumber: string): boolean {
    return this.selected().includes(containerNumber);
  }

  onContainer(containerNumber: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((list) => (checked ? [...list, containerNumber] : list.filter((n) => n !== containerNumber)));
  }

  onEntityType(type: LegalEntityType): void {
    this.entityType.set(type);
  }

  onCarrierMode(mode: CarrierMode): void {
    this.carrierMode.set(mode);
  }

  onInput(field: TextField, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement).value;
    this.values.update((v) => ({ ...v, [field]: value }));
  }

  /** Estado del TATC de una unidad para leerlo junto a su casilla. */
  tatcText(c: ReleaseLetterContainerOption, tatc: ReleaseLetterTatc): string {
    if (!tatc.available) return translate('documents.releaseLetter.form.tatcUnknown');
    if (!c.tatc) return translate(TATC_STATUS_KEYS['NotRegistered']);
    const status = translate(TATC_STATUS_KEYS[c.tatc.status] ?? TATC_STATUS_KEYS['Unknown']);
    return c.tatc.tatcNumber ? translate('documents.releaseLetter.form.tatcWithNumber', { status, number: c.tatc.tatcNumber }) : status;
  }

  submit(event: Event): void {
    event.preventDefault();
    const d = this.data();
    if (!d || this.submitting() || !this.canSubmit()) return;
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const v = this.values();
    const optional = (value: string) => value.trim() || null;
    const company = this.isCompany();
    const registered = this.carrierMode() === 'registered';
    this.submitting.set(true);
    this.service.requestReleaseLetter(d.blNumber, {
      containers: this.selected(),
      legalEntityType: this.entityType(),
      consigneeName: v.consigneeName.trim(),
      consigneeTaxId: v.consigneeTaxId.trim(),
      consigneeAddress: optional(v.consigneeAddress),
      legalRepresentativeName: company ? optional(v.legalRepresentativeName) : null,
      legalRepresentativeId: company ? optional(v.legalRepresentativeId) : null,
      carrierOrganizationId: registered ? v.carrierOrganizationId : null,
      carrierName: registered ? null : optional(v.carrierName),
      carrierTaxId: registered ? null : optional(v.carrierTaxId),
      driverName: optional(v.driverName),
      driverId: optional(v.driverId),
      truckPlate: optional(v.truckPlate),
      observations: optional(v.observations),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.announcer.announce(translate('documents.releaseLetter.form.submitted', { number: result.request.requestNumber }));
        this.router.navigate(['/release-letters', result.request.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        const message = documentErrorMessage(err, 'documents.releaseLetter.errors.submit');
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
        // Otra carta pendiente ya incluye alguna unidad o el transportista dejó de estar vinculado: se relee el BL.
        const code = apiErrorCode(err);
        if (code === 'ReleaseLetter.AlreadyRequested' || code === 'ReleaseLetter.CarrierNotFound') this.load(false);
        focusAfterRender(this.injector, () => document.getElementById('release-submit-error'));
      },
    });
  }

  /** Lleva el foco al campo del error; en un grupo, a su primera opción. */
  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    const el = document.getElementById(fieldId);
    (el?.tagName === 'FIELDSET' ? el.querySelector<HTMLElement>('input:not([disabled])') : el)?.focus();
  }
}
