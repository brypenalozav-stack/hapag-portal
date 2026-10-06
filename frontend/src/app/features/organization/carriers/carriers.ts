import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  CarrierPreRegistration,
  CarrierPreRegistrationRequest,
  CarrierPreRegistrationResult,
} from '../../../core/models/organization-network.model';
import { ACCESS_GRANT_STATUS_KEYS, CARRIER_PRE_REGISTRATION_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { GRANT_ERRORS } from '../../access/shared/access-errors';
import { ToastService } from '../../../core/services/toast.service';

interface CarrierForm {
  legalName: string;
  taxId: string;
  email: string;
  country: 'CL' | 'BO';
  contactFirstName: string;
  contactLastName: string;
  blNumbers: string;
  bookingNumbers: string;
  durationDays: number | null;
}

interface FormError {
  fieldId: string;
  key: string;
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Números de BL o booking separados por coma, espacio o salto de línea; en mayúsculas y sin repetir. */
function parseReferences(text: string): string[] {
  return [...new Set(text.split(/[\s,;]+/).map((r) => r.trim().toUpperCase()).filter(Boolean))];
}

function emptyForm(country: 'CL' | 'BO'): CarrierForm {
  return { legalName: '', taxId: '', email: '', country, contactFirstName: '', contactLastName: '', blNumbers: '', bookingNumbers: '', durationDays: null };
}

/**
 * Transportistas pre-creados (Fase 2, Ola I, M1-09): el cliente crea el perfil de un transportista sin cuenta, con sus
 * datos básicos y los BL y bookings que le asigna; el transportista recibe una invitación y, al ingresar por primera vez,
 * queda vinculado a esos embarques (los accesos esperan su activación). Si ya existe un perfil pre-creado con el mismo
 * RUT o correo, se reutiliza. Crear y asignar exige `org.access.manage`; el servidor valida cada BL como un otorgamiento.
 */
@Component({
  selector: 'app-carriers',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './carriers.html',
})
export class CarriersComponent implements OnInit {
  private readonly service = inject(OrganizationNetworkService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Crear, asignar y reenviar la invitación (`org.access.manage`). */
  canManage = input(false);
  country = input<'CL' | 'BO'>('CL');

  readonly statusKeys = CARRIER_PRE_REGISTRATION_STATUS_KEYS;
  readonly grantStatusKeys = ACCESS_GRANT_STATUS_KEYS;

  carriers = signal<CarrierPreRegistration[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');

  formOpen = signal(false);
  form: CarrierForm = emptyForm('CL');
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');
  result = signal<CarrierPreRegistrationResult | null>(null);

  assigning = signal<CarrierPreRegistration | null>(null);
  assignBls = '';
  assignBookings = '';
  assignError = signal('');
  resending = signal<string | null>(null);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');
  private readonly assignField = viewChild<ElementRef<HTMLTextAreaElement>>('assignField');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getCarriers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (carriers) => {
        this.carriers.set(carriers);
        this.loading.set(false);
      },
      error: () => {
        this.carriers.set([]);
        this.loading.set(false);
        this.loadFailed.set(true);
      },
    });
  }

  openForm(): void {
    this.form = emptyForm(this.country());
    this.errors.set([]);
    this.saveError.set('');
    this.result.set(null);
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.legalName.trim()) list.push({ fieldId: 'carrier-legal-name', key: 'organization.carriers.form.errors.legalName' });
    if (!f.taxId.trim()) list.push({ fieldId: 'carrier-tax-id', key: 'organization.carriers.form.errors.taxId' });
    if (!EMAIL.test(f.email.trim())) list.push({ fieldId: 'carrier-email', key: 'organization.carriers.form.errors.email' });
    if (f.durationDays !== null && (!Number.isInteger(f.durationDays) || f.durationDays < 1 || f.durationDays > 365)) {
      list.push({ fieldId: 'carrier-duration', key: 'organization.carriers.form.errors.duration' });
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
    const body: CarrierPreRegistrationRequest = {
      legalName: f.legalName.trim(),
      taxId: f.taxId.trim(),
      email: f.email.trim().toLowerCase(),
      country: f.country,
      contactFirstName: f.contactFirstName.trim() || undefined,
      contactLastName: f.contactLastName.trim() || undefined,
      blNumbers: parseReferences(f.blNumbers),
      bookingNumbers: parseReferences(f.bookingNumbers),
      durationDays: f.durationDays ?? undefined,
    };
    this.saving.set(true);
    this.service.preCreateCarrier(body).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.showResult(result);
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'organization.carriers.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  startAssign(c: CarrierPreRegistration): void {
    this.assigning.set(c);
    this.assignBls = '';
    this.assignBookings = '';
    this.assignError.set('');
    focusAfterRender(this.injector, () => this.assignField()?.nativeElement);
  }

  cancelAssign(): void {
    this.assigning.set(null);
  }

  assign(event: Event): void {
    event.preventDefault();
    const c = this.assigning();
    if (!c) return;
    const blNumbers = parseReferences(this.assignBls);
    const bookingNumbers = parseReferences(this.assignBookings);
    if (blNumbers.length === 0 && bookingNumbers.length === 0) {
      const message = translate('organization.carriers.assign.errors.required');
      this.assignError.set(message);
      this.announcer.announce(message, 'assertive');
      return;
    }
    this.saving.set(true);
    this.service.assignToCarrier(c.id, { blNumbers, bookingNumbers }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.assigning.set(null);
        this.showResult(result);
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'organization.carriers.assign.errors.submit');
        this.assignError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  resend(c: CarrierPreRegistration): void {
    this.resending.set(c.id);
    this.actionError.set('');
    this.service.resendPreCreatedInvitation(c.email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.resending.set(null);
        this.toast.success(translate('organization.carriers.resent', { email: c.email }));
      },
      error: (err) => {
        this.resending.set(null);
        this.actionError.set(adminErrorMessage(err, 'organization.carriers.errors.resend'));
      },
    });
  }

  /** Motivo traducido de un BL o booking que no se asignó (mismos códigos que un otorgamiento). */
  skippedReason(code: string): string {
    const key = GRANT_ERRORS[code];
    return key ? translate(key) : code;
  }

  closeResult(): void {
    this.result.set(null);
  }

  private showResult(result: CarrierPreRegistrationResult): void {
    this.result.set(result);
    this.load();
    const key = result.created ? 'organization.carriers.result.created' : 'organization.carriers.result.updated';
    this.announcer.announce(translate(key, { name: result.preRegistration.legalName, assigned: result.assigned }));
    focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
