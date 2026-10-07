import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { ShipmentService } from '../../../core/services/shipment.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ACCESS_DURATION_MAX_DAYS,
  ACCESS_DURATION_MIN_DAYS,
  GrantAccessRequest,
  GrantAccessResult,
  GrantableAction,
  GranteeOrganization,
  MandateTerms,
} from '../../../core/models/access.model';
import { ShipmentListItem } from '../../../core/models/shipment.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { GranteePickerComponent } from '../shared/grantee-picker';
import { ValidityFieldsComponent } from '../shared/validity-fields';
import { PermissionChecklistComponent } from '../shared/permission-checklist';
import {
  FormErrorItem,
  PermissionsFormValue,
  ValidityFormValue,
  basePermissions,
  emptyValidity,
  explicitCodes,
  hasErrors,
  toValidityRequest,
  validateValidity,
} from '../shared/access-form';
import { GRANT_ERRORS, SKIPPED_REASON_KEYS } from '../shared/access-errors';

/** Máximo de referencias por otorgamiento (contrato del backend). */
const MAX_REFERENCES = 500;
/** Embarques que se ofrecen en cada búsqueda del selector. */
const SHIPMENT_PAGE_SIZE = 50;

/**
 * Flujo único de otorgamiento (M1-24): destinatario, embarques (uno o varios: M1-12), vigencia
 * (M1-14), permisos acotados a lo que posee el otorgante (M1-15) y, opcionalmente, mandato con
 * aceptación de términos (M1-03). Diálogo modal nativo (`<dialog>`), que atrapa el foco y se
 * cierra con Escape. El resultado (registros actualizados, creados, modificados y omitidos) se
 * muestra y se anuncia en la región polite.
 */
@Component({
  selector: 'app-grant-dialog',
  standalone: true,
  imports: [
    TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, GranteePickerComponent, ValidityFieldsComponent, PermissionChecklistComponent,
  ],
  templateUrl: './grant-dialog.html',
  styleUrl: './grant-dialog.scss',
})
export class GrantDialogComponent implements AfterViewInit {
  private readonly accessService = inject(AccessService);
  private readonly shipmentService = inject(ShipmentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** BL fijado desde el detalle del embarque; sin él, se eligen uno o varios de la lista. */
  presetBlNumber = input<string | null>(null);
  /** Se emite al cerrar; con el resultado si se otorgó algo. */
  closed = output<GrantAccessResult | null>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('grantDialog');

  readonly idPrefix = 'grant';
  readonly skippedKeys = SKIPPED_REASON_KEYS;
  readonly minDays = ACCESS_DURATION_MIN_DAYS;
  readonly maxDays = ACCESS_DURATION_MAX_DAYS;
  readonly maxReferences = MAX_REFERENCES;

  grantee = signal<GranteeOrganization | null>(null);
  referenceKind = signal<'bl' | 'booking'>('bl');
  shipmentFilter = signal('');
  shipments = signal<ShipmentListItem[]>([]);
  shipmentsLoading = signal(false);
  shipmentsTotal = signal(0);
  /** Embarques elegidos por número de BL (con su booking), conservados entre búsquedas. */
  selected = signal<Map<string, string | null>>(new Map());

  validity = signal<ValidityFormValue>(emptyValidity());
  permissions = signal<PermissionsFormValue>(basePermissions());
  actions = signal<GrantableAction[]>([]);
  actionsLoading = signal(false);
  private actionsKey = '';

  isMandate = signal(false);
  terms = signal<MandateTerms | null>(null);
  termsLoading = signal(false);
  acceptTerms = signal(false);

  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');
  result = signal<GrantAccessResult | null>(null);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  selectedCount = computed(() => (this.presetBlNumber() ? 1 : this.selected().size));

  /** Errores del formulario con el campo al que lleva cada uno (resumen de errores, guía §3.2). */
  errors = computed<FormErrorItem[]>(() => {
    if (!this.submitted()) return [];
    const list: FormErrorItem[] = [];
    if (!this.grantee()) list.push({ fieldId: `${this.idPrefix}-grantee-search`, key: 'thirdPartyAccess.grantDialog.errors.granteeRequired' });
    const count = this.selectedCount();
    if (count === 0) list.push({ fieldId: `${this.idPrefix}-shipment-filter`, key: 'thirdPartyAccess.grantDialog.errors.referencesRequired' });
    if (count > MAX_REFERENCES) list.push({ fieldId: `${this.idPrefix}-shipment-filter`, key: 'thirdPartyAccess.grantDialog.errors.tooManyReferences' });
    if (this.referenceKind() === 'booking' && this.missingBookings().length > 0) {
      list.push({ fieldId: `${this.idPrefix}-shipment-filter`, key: 'thirdPartyAccess.grantDialog.errors.missingBooking' });
    }
    const validity = validateValidity(this.validity(), this.isMandate());
    if (validity.validityType) list.push({ fieldId: `${this.idPrefix}-validity-Duration`, key: validity.validityType });
    if (validity.durationDays) list.push({ fieldId: `${this.idPrefix}-duration`, key: validity.durationDays });
    if (validity.validTo) list.push({ fieldId: `${this.idPrefix}-valid-to`, key: validity.validTo });
    if (this.isMandate() && explicitCodes(this.permissions().codes).length === 0) {
      list.push({ fieldId: `${this.idPrefix}-mandate`, key: 'thirdPartyAccess.grantDialog.errors.mandatePermissions' });
    }
    return list;
  });

  /** BL elegidos sin booking: no se pueden otorgar por booking. */
  missingBookings = computed(() =>
    [...this.selected().entries()].filter(([, booking]) => !booking).map(([bl]) => bl),
  );

  ngAfterViewInit(): void {
    this.dialog().nativeElement.showModal();
    if (!this.presetBlNumber()) this.loadShipments();
  }

  // Embarques
  onShipmentFilter(event: Event): void {
    this.shipmentFilter.set((event.target as HTMLInputElement).value);
  }

  onShipmentFilterKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.loadShipments();
    }
  }

  loadShipments(): void {
    this.shipmentsLoading.set(true);
    const filter = this.shipmentFilter().trim();
    this.shipmentService.search({ blNumber: filter, page: 1, pageSize: SHIPMENT_PAGE_SIZE }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (page) => {
        this.shipments.set(page.items);
        this.shipmentsTotal.set(page.total);
        this.shipmentsLoading.set(false);
      },
      error: () => {
        this.shipments.set([]);
        this.shipmentsTotal.set(0);
        this.shipmentsLoading.set(false);
      },
    });
  }

  isSelected(shipment: ShipmentListItem): boolean {
    return this.selected().has(shipment.blNumber);
  }

  toggleShipment(shipment: ShipmentListItem, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Map(current);
      if (checked) next.set(shipment.blNumber, shipment.bookingNumber);
      else next.delete(shipment.blNumber);
      return next;
    });
    this.refreshActions();
  }

  selectAllVisible(): void {
    this.selected.update((current) => {
      const next = new Map(current);
      for (const s of this.shipments()) next.set(s.blNumber, s.bookingNumber);
      return next;
    });
    this.refreshActions();
  }

  clearSelection(): void {
    this.selected.set(new Map());
    this.refreshActions();
  }

  setReferenceKind(kind: 'bl' | 'booking'): void {
    this.referenceKind.set(kind);
  }

  // Destinatario y catálogo de permisos
  onGranteeChange(grantee: GranteeOrganization | null): void {
    this.grantee.set(grantee);
    this.refreshActions();
  }

  /**
   * Consulta las acciones que se pueden otorgar: con un solo BL, las que el otorgante posee en
   * ese BL; con varios, las del nivel de cliente de la organización (el servidor recorta por BL).
   */
  refreshActions(): void {
    const grantee = this.grantee();
    if (!grantee) {
      this.actions.set([]);
      this.actionsKey = '';
      return;
    }
    const single = this.presetBlNumber() ?? (this.selected().size === 1 ? [...this.selected().keys()][0] : '');
    const key = `${grantee.id}|${single}`;
    if (key === this.actionsKey) return;
    this.actionsKey = key;
    this.actionsLoading.set(true);
    this.accessService.getGrantableActions({ blNumber: single || undefined, granteeOrganizationId: grantee.id }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (actions) => {
        if (key !== this.actionsKey) return;
        this.actions.set(actions);
        // Quita los permisos marcados que dejaron de poder otorgarse.
        const grantable = new Set(actions.filter((a) => a.grantable).map((a) => a.code));
        this.permissions.update((p) => ({ ...p, codes: p.codes.filter((c) => grantable.has(c)) }));
        this.actionsLoading.set(false);
      },
      error: () => {
        this.actions.set([]);
        this.actionsLoading.set(false);
      },
    });
  }

  // Mandato (M1-03)
  onMandateChange(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.isMandate.set(checked);
    if (checked) {
      this.permissions.update((p) => ({ ...p, mode: 'custom' }));
      if (this.validity().validityType === 'Indefinite') {
        this.validity.update((v) => ({ ...v, validityType: 'UntilDate' }));
      }
      if (!this.terms()) this.loadTerms();
    } else {
      this.acceptTerms.set(false);
    }
  }

  onAcceptTermsChange(event: Event): void {
    this.acceptTerms.set((event.target as HTMLInputElement).checked);
  }

  private loadTerms(): void {
    this.termsLoading.set(true);
    this.accessService.getMandateTerms().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (terms) => {
        this.terms.set(terms);
        this.termsLoading.set(false);
      },
      error: () => this.termsLoading.set(false),
    });
  }

  // Envío
  submit(event: Event): void {
    event.preventDefault();
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0 || hasErrors(validateValidity(this.validity(), this.isMandate()))) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      setTimeout(() => this.errorSummary()?.nativeElement.focus());
      return;
    }
    const grantee = this.grantee();
    if (!grantee) return;

    const references = this.presetBlNumber() ? [this.presetBlNumber() as string] : [...this.selected().keys()];
    const byBooking = this.referenceKind() === 'booking' && !this.presetBlNumber();
    const permissions = this.permissions();
    const custom = this.isMandate() || permissions.mode === 'custom';
    const request: GrantAccessRequest = {
      granteeOrganizationId: grantee.id,
      ...(byBooking
        ? { bookingNumbers: references.map((bl) => this.selected().get(bl) as string) }
        : { blNumbers: references }),
      ...toValidityRequest(this.validity()),
      actionCodes: custom ? explicitCodes(permissions.codes) : null,
    };
    if (this.isMandate()) {
      request.isMandate = true;
      request.acceptTerms = this.acceptTerms();
      if (this.acceptTerms() && this.terms()) request.termsVersion = this.terms()?.version;
    }

    this.submitting.set(true);
    this.accessService.grant(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.result.set(result);
        this.announcer.announce(translate('thirdPartyAccess.grantDialog.result.summary', {
          updated: result.updated,
          requested: result.requested,
          created: result.created,
          modified: result.modified,
          skipped: result.skipped.length,
        }));
        setTimeout(() => this.resultHeading()?.nativeElement.focus());
      },
      error: (err) => {
        this.submitting.set(false);
        const message = translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.grantDialog.errors.submit'));
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  /** Cierre nativo (botón, Escape o resultado): avisa al contenedor con el resultado si lo hubo. */
  onClosed(): void {
    this.closed.emit(this.result());
  }
}
