import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  AccessGrant,
  GrantAccessResult,
  GrantableAction,
  VisibilityWidening,
  WIDENING_TARGET_ROLES,
  WideningTargetRole,
} from '../../../core/models/access.model';
import { ShipmentDetail } from '../../../core/models/shipment.model';
import {
  ACCESS_ACTION_KEYS,
  ACCESS_GRANT_STATUS_KEYS,
  ACCESS_ORIGIN_KEYS,
  SHIPMENT_ROLE_KEYS,
  WIDENING_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { GrantDialogComponent } from '../grant-dialog/grant-dialog';
import { GRANT_ERRORS } from '../shared/access-errors';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';

/** Accesos del BL que se muestran en el detalle (los demás se ven en Mi organización). */
const GRANTS_PAGE_SIZE = 50;

/**
 * Sección "Accesos" del detalle del embarque: quién tiene acceso al BL (si el servidor lo
 * permite), otorgamiento rápido para este BL con el flujo único (M1-12, M1-24) y ampliaciones de
 * visibilidad entre roles (M1-16), que se pueden crear y revocar con `org.access.manage`.
 */
@Component({
  selector: 'app-shipment-access',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, GrantDialogComponent],
  templateUrl: './shipment-access.html',
})
export class ShipmentAccessComponent implements OnInit {
  private readonly service = inject(AccessService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  shipment = input.required<ShipmentDetail>();
  canManage = input(false);
  /** Nivel del título: 3 dentro de un grupo del detalle del BL, cuyo h2 es el título del grupo. */
  readonly headingLevel = input<2 | 3>(2);

  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly statusKeys = ACCESS_GRANT_STATUS_KEYS;
  readonly originKeys = ACCESS_ORIGIN_KEYS;
  readonly roleKeys = SHIPMENT_ROLE_KEYS;
  readonly wideningStatusKeys = WIDENING_STATUS_KEYS;

  grants = signal<AccessGrant[]>([]);
  grantsLoading = signal(true);
  /** El servidor no muestra los accesos de este BL a la organización (403/404). */
  grantsHidden = signal(false);
  grantsError = signal('');

  widenings = signal<VisibilityWidening[]>([]);
  wideningsLoading = signal(true);
  wideningsHidden = signal(false);
  wideningsError = signal('');

  dialogOpen = signal(false);
  private opener: HTMLElement | null = null;

  // Ampliación de visibilidad (M1-16)
  wideningFormOpen = signal(false);
  targetRole = signal<WideningTargetRole | ''>('');
  wideningActions = signal<GrantableAction[]>([]);
  wideningActionsLoading = signal(false);
  wideningCodes = signal<Set<string>>(new Set());
  wideningSubmitted = signal(false);
  wideningSaving = signal(false);
  wideningFormError = signal('');

  /** Roles destino: los de M1-16 que no son míos en este BL. */
  targetRoles = computed(() => WIDENING_TARGET_ROLES.filter((r) => !this.shipment().roles.includes(r)));

  /** Solo datos de información que mi organización posee en el BL; el servidor valida el resto. */
  widenable = computed(() => this.wideningActions().filter((a) => a.category === 'Information' && a.grantorHas));

  ngOnInit(): void {
    this.loadGrants();
    this.loadWidenings();
  }

  loadGrants(): void {
    this.grantsLoading.set(true);
    this.grantsError.set('');
    this.service.getGrants({ reference: this.shipment().blNumber, page: 1, pageSize: GRANTS_PAGE_SIZE }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => {
        this.grants.set(result.items);
        this.grantsLoading.set(false);
      },
      error: (err) => {
        this.grants.set([]);
        if (err instanceof HttpErrorResponse && (err.status === 403 || err.status === 404)) this.grantsHidden.set(true);
        else this.grantsError.set(translate('shipments.detail.access.grantsError'));
        this.grantsLoading.set(false);
      },
    });
  }

  loadWidenings(): void {
    this.wideningsLoading.set(true);
    this.wideningsError.set('');
    this.service.getWidenings(this.shipment().blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (widenings) => {
        this.widenings.set(widenings);
        this.wideningsLoading.set(false);
      },
      error: (err) => {
        this.widenings.set([]);
        if (err instanceof HttpErrorResponse && (err.status === 403 || err.status === 404)) this.wideningsHidden.set(true);
        else this.wideningsError.set(translate('shipments.detail.access.wideningsError'));
        this.wideningsLoading.set(false);
      },
    });
  }

  // Otorgar acceso a este BL
  openGrant(event: Event): void {
    this.opener = event.currentTarget as HTMLElement;
    this.dialogOpen.set(true);
  }

  onDialogClosed(result: GrantAccessResult | null): void {
    this.dialogOpen.set(false);
    setTimeout(() => this.opener?.focus());
    if (result && result.updated > 0) this.loadGrants();
  }

  // Ampliaciones (M1-16)
  openWideningForm(): void {
    this.targetRole.set('');
    this.wideningCodes.set(new Set());
    this.wideningSubmitted.set(false);
    this.wideningFormError.set('');
    this.wideningFormOpen.set(true);
    if (this.wideningActions().length === 0) {
      this.wideningActionsLoading.set(true);
      this.service.getGrantableActions({ blNumber: this.shipment().blNumber }).pipe(
        takeUntilDestroyed(this.destroyRef),
      ).subscribe({
        next: (actions) => {
          this.wideningActions.set(actions);
          this.wideningActionsLoading.set(false);
        },
        error: () => this.wideningActionsLoading.set(false),
      });
    }
  }

  cancelWidening(): void {
    this.wideningFormOpen.set(false);
  }

  onTargetRoleChange(event: Event): void {
    this.targetRole.set((event.target as HTMLSelectElement).value as WideningTargetRole | '');
  }

  toggleWideningCode(code: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.wideningCodes.update((current) => {
      const next = new Set(current);
      if (checked) next.add(code);
      else next.delete(code);
      return next;
    });
  }

  submitWidening(event: Event): void {
    event.preventDefault();
    this.wideningSubmitted.set(true);
    this.wideningFormError.set('');
    const role = this.targetRole();
    const codes = [...this.wideningCodes()];
    if (!role || codes.length === 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    this.wideningSaving.set(true);
    this.service.createWidenings(this.shipment().blNumber, role, codes).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created) => {
        this.wideningSaving.set(false);
        this.wideningFormOpen.set(false);
        this.toast.success(translate('shipments.detail.access.widenings.created', { count: created.length }));
        this.loadWidenings();
      },
      error: (err) => {
        this.wideningSaving.set(false);
        this.wideningFormError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'shipments.detail.access.widenings.createError')));
      },
    });
  }

  async revokeWidening(widening: VisibilityWidening): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.revokeWidening.title',
      message: 'shared.modal.revokeWidening.message',
      confirmLabel: 'shared.modal.revokeWidening.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.wideningsError.set('');
    this.service.revokeWidening(this.shipment().blNumber, widening.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.widenings.update((list) => list.filter((w) => w.id !== widening.id));
        this.toast.success(translate('shipments.detail.access.widenings.revoked'));
      },
      error: (err) =>
        this.wideningsError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'shipments.detail.access.widenings.revokeError'))),
    });
  }

  originCode(grant: AccessGrant): string {
    return grant.isMandate ? 'Mandate' : grant.grantType;
  }
}
