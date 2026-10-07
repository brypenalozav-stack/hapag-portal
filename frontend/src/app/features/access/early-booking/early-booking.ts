import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  EARLY_BOOKING_ROLES,
  EarlyBookingAccessRequest,
  EarlyBookingRole,
  GrantableAction,
  GranteeOrganization,
} from '../../../core/models/access.model';
import { SHIPMENT_ROLE_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { GranteePickerComponent } from '../shared/grantee-picker';
import { ValidityFieldsComponent } from '../shared/validity-fields';
import { PermissionChecklistComponent } from '../shared/permission-checklist';
import {
  PermissionsFormValue,
  ValidityFormValue,
  basePermissions,
  emptyValidity,
  explicitCodes,
  hasErrors,
  toValidityRequest,
  validateValidity,
} from '../shared/access-form';
import { GRANT_ERRORS } from '../shared/access-errors';

/** Rol por defecto del receptor del booking: el futuro shipper (contrato del backend). */
const DEFAULT_ROLE: EarlyBookingRole = 'Shipper';

/**
 * Acceso anticipado por booking (M1-20): el customer da visibilidad del booking al futuro shipper
 * (o consignee; un FFWW puede quedar como tercero) antes de que exista el BL. Al llegar el BL el
 * acceso se vincula y, si el rol oficial coincide, se reconcilia sin pérdida de visibilidad.
 */
@Component({
  selector: 'app-early-booking',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, GranteePickerComponent, ValidityFieldsComponent, PermissionChecklistComponent],
  templateUrl: './early-booking.html',
})
export class EarlyBookingComponent {
  private readonly service = inject(AccessService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly roleKeys = SHIPMENT_ROLE_KEYS;

  bookingNumber = signal('');
  grantee = signal<GranteeOrganization | null>(null);
  intendedRole = signal<EarlyBookingRole>(DEFAULT_ROLE);
  validity = signal<ValidityFormValue>(emptyValidity());
  permissions = signal<PermissionsFormValue>(basePermissions());
  actions = signal<GrantableAction[]>([]);
  actionsLoading = signal(false);

  submitted = signal(false);
  saving = signal(false);
  error = signal('');
  success = signal('');

  /** Tercero solo si el receptor es Freight Forwarder (contrato del backend). */
  roles = computed(() =>
    EARLY_BOOKING_ROLES.filter((r) => r !== 'ThirdParty' || this.grantee()?.organizationType === 'FreightForwarder'),
  );

  bookingInvalid = computed(() => this.submitted() && this.bookingNumber().trim() === '');

  onBookingInput(event: Event): void {
    this.bookingNumber.set((event.target as HTMLInputElement).value);
  }

  onRoleChange(event: Event): void {
    this.intendedRole.set((event.target as HTMLSelectElement).value as EarlyBookingRole);
    this.loadActions();
  }

  onGranteeChange(grantee: GranteeOrganization | null): void {
    this.grantee.set(grantee);
    if (!this.roles().includes(this.intendedRole())) this.intendedRole.set(DEFAULT_ROLE);
    this.loadActions();
  }

  private loadActions(): void {
    const grantee = this.grantee();
    if (!grantee) {
      this.actions.set([]);
      return;
    }
    this.actionsLoading.set(true);
    this.service.getGrantableActions({ granteeOrganizationId: grantee.id, receiverRole: this.intendedRole() }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (actions) => {
        this.actions.set(actions);
        this.actionsLoading.set(false);
      },
      error: () => {
        this.actions.set([]);
        this.actionsLoading.set(false);
      },
    });
  }

  submit(event: Event): void {
    event.preventDefault();
    this.submitted.set(true);
    this.error.set('');
    this.success.set('');
    const grantee = this.grantee();
    const booking = this.bookingNumber().trim();
    if (!grantee || !booking || hasErrors(validateValidity(this.validity()))) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    const permissions = this.permissions();
    const request: EarlyBookingAccessRequest = {
      bookingNumber: booking,
      granteeOrganizationId: grantee.id,
      intendedRole: this.intendedRole(),
      ...toValidityRequest(this.validity()),
      ...(permissions.mode === 'custom' ? { actionCodes: explicitCodes(permissions.codes) } : {}),
    };
    this.saving.set(true);
    this.service.grantEarlyBooking(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        const message = translate('thirdPartyAccess.earlyBooking.done', { booking, name: grantee.name });
        this.success.set(message);
        this.announcer.announce(message);
        this.submitted.set(false);
        this.bookingNumber.set('');
        this.grantee.set(null);
        this.validity.set(emptyValidity());
        this.permissions.set(basePermissions());
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.earlyBooking.error'));
        this.error.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
