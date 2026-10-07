import { Component, DestroyRef, ElementRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ACCESS_GRANT_STATUSES,
  AccessGrant,
  AccessGrantDirection,
  AccessGrantStatus,
  GrantAccessResult,
  GrantableAction,
  MandateTerms,
  RevokeAccessResult,
  UpdateAccessGrantRequest,
} from '../../../core/models/access.model';
import {
  ACCESS_ACTION_KEYS,
  ACCESS_DIRECTION_KEYS,
  ACCESS_END_REASON_KEYS,
  ACCESS_GRANT_STATUS_KEYS,
  ACCESS_ORIGIN_KEYS,
  ORGANIZATION_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { GrantDialogComponent } from '../grant-dialog/grant-dialog';
import { ValidityFieldsComponent } from '../shared/validity-fields';
import { PermissionChecklistComponent } from '../shared/permission-checklist';
import {
  PermissionsFormValue,
  ValidityFormValue,
  basePermissions,
  emptyValidity,
  explicitCodes,
  hasErrors,
  permissionsOf,
  samePermissions,
  sameValidity,
  toValidityRequest,
  validateValidity,
  validityOf,
} from '../shared/access-form';
import { GRANT_ERRORS } from '../shared/access-errors';
import { ToastService } from '../../../core/services/toast.service';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/** Clase de la insignia por estado; el texto del estado siempre acompaña al color (1.4.1). */
const STATUS_BADGE: Record<AccessGrantStatus, string> = {
  PendingAcceptance: 'hl-badge--pending',
  Active: 'hl-badge--active',
  Expired: 'hl-badge--failed',
  Revoked: 'hl-badge--failed',
  Reconciled: 'hl-badge--processing',
};

/** Revocación en curso: un acceso o la selección (masiva). */
type RevokeTarget = { kind: 'single'; grant: AccessGrant } | { kind: 'bulk'; ids: string[] };

/**
 * Tabla de accesos otorgados y recibidos (M1-24): destinatario, BL o booking, inicio, término,
 * permisos, estado y origen, con filtros y paginación. Con `org.access.manage`: otorgar (flujo
 * único), editar vigencia y permisos en la misma fila (M1-14, M1-15), revocar uno o varios con
 * aviso de la revocación en cadena (M1-22) y aceptar los términos de un mandato pendiente (M1-03).
 */
@Component({
  selector: 'app-access-grants',
  standalone: true,
  imports: [
    FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    GrantDialogComponent, ValidityFieldsComponent, PermissionChecklistComponent, PaginatorComponent,
  ],
  templateUrl: './access-grants.html',
})
export class AccessGrantsComponent implements OnInit {
  private readonly service = inject(AccessService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject(ElementRef<HTMLElement>);

  /** Permiso org.access.manage: otorgar, editar y revocar. */
  canManage = input(false);

  pageSize = signal(20);
  readonly statuses = ACCESS_GRANT_STATUSES;
  readonly directions: readonly AccessGrantDirection[] = ['Given', 'Received'];
  readonly statusKeys = ACCESS_GRANT_STATUS_KEYS;
  readonly originKeys = ACCESS_ORIGIN_KEYS;
  readonly directionKeys = ACCESS_DIRECTION_KEYS;
  readonly endReasonKeys = ACCESS_END_REASON_KEYS;
  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  // Filtros
  direction = signal<AccessGrantDirection | ''>('');
  status = signal<AccessGrantStatus | ''>('');
  reference = signal('');

  grants = signal<AccessGrant[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');


  // Selección para revocar varios a la vez
  selection = signal<Set<string>>(new Set());
  revocable = computed(() => this.grants().filter((g) => g.canEdit));
  allSelected = computed(() => this.revocable().length > 0 && this.revocable().every((g) => this.selection().has(g.id)));

  // Otorgar
  dialogOpen = signal(false);
  private opener: HTMLElement | null = null;

  // Edición en el lugar
  editing = signal<AccessGrant | null>(null);
  editValidity = signal<ValidityFormValue>(emptyValidity());
  editPermissions = signal<PermissionsFormValue>(basePermissions());
  editActions = signal<GrantableAction[]>([]);
  editActionsLoading = signal(false);
  editSubmitted = signal(false);
  editError = signal('');
  saving = signal(false);

  // Revocar
  revoking = signal<RevokeTarget | null>(null);
  revokeReason = signal('');
  revokeBusy = signal(false);

  // Aceptar términos del mandato
  accepting = signal<AccessGrant | null>(null);
  terms = signal<MandateTerms | null>(null);
  acceptChecked = signal(false);
  acceptBusy = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getGrants({
      direction: this.direction(),
      status: this.status(),
      reference: this.reference().trim(),
      page: this.page(),
      pageSize: this.pageSize(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.grants.set(result.items);
        this.total.set(result.total);
        this.selection.set(new Set());
        this.loading.set(false);
      },
      error: (err) => {
        this.grants.set([]);
        this.total.set(0);
        this.loadFailed.set(isServiceUnavailable(err));
        if (!isServiceUnavailable(err)) this.actionError.set(translate('thirdPartyAccess.grants.errors.load'));
        this.loading.set(false);
      },
    });
  }

  search(): void {
    this.page.set(1);
    this.actionError.set('');
    this.load();
  }

  clearFilters(): void {
    this.direction.set('');
    this.status.set('');
    this.reference.set('');
    this.search();
  }

  changePage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  // Presentación
  referenceOf(grant: AccessGrant): string {
    return grant.blNumber ?? grant.bookingNumber ?? '—';
  }

  originCode(grant: AccessGrant): string {
    return grant.isMandate ? 'Mandate' : grant.grantType;
  }

  badgeClass(grant: AccessGrant): string {
    return STATUS_BADGE[grant.status] ?? 'hl-badge--pending';
  }

  // Selección
  isSelected(grant: AccessGrant): boolean {
    return this.selection().has(grant.id);
  }

  toggleSelection(grant: AccessGrant, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selection.update((current) => {
      const next = new Set(current);
      if (checked) next.add(grant.id);
      else next.delete(grant.id);
      return next;
    });
  }

  toggleAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selection.set(checked ? new Set(this.revocable().map((g) => g.id)) : new Set());
  }

  // Otorgar (flujo único)
  openGrant(event: Event): void {
    this.opener = event.currentTarget as HTMLElement;
    this.dialogOpen.set(true);
  }

  onDialogClosed(result: GrantAccessResult | null): void {
    this.dialogOpen.set(false);
    setTimeout(() => this.opener?.focus());
    if (result && result.updated > 0) {
      this.page.set(1);
      this.load();
    }
  }

  // Edición en el lugar (M1-24)
  startEdit(grant: AccessGrant): void {
    this.revoking.set(null);
    this.accepting.set(null);
    this.editing.set(grant);
    this.editValidity.set(validityOf(grant));
    this.editPermissions.set(permissionsOf(grant.actionCodes));
    this.editSubmitted.set(false);
    this.editError.set('');
    this.editActions.set([]);
    this.editActionsLoading.set(true);
    this.service.getGrantableActions({
      blNumber: grant.blNumber ?? undefined,
      granteeOrganizationId: grant.grantee.id,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (actions) => {
        this.editActions.set(actions);
        this.editActionsLoading.set(false);
      },
      error: () => this.editActionsLoading.set(false),
    });
  }

  cancelEdit(): void {
    this.editing.set(null);
  }

  saveEdit(grant: AccessGrant): void {
    this.editSubmitted.set(true);
    this.editError.set('');
    const validity = this.editValidity();
    if (hasErrors(validateValidity(validity, grant.isMandate))) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    const request: UpdateAccessGrantRequest = {};
    if (!sameValidity(validity, validityOf(grant))) Object.assign(request, toValidityRequest(validity));
    const permissions = this.editPermissions();
    if (!samePermissions(permissions, permissionsOf(grant.actionCodes))) {
      if (permissions.mode === 'base' && !grant.isMandate) request.resetToBaseLevel = true;
      else request.actionCodes = explicitCodes(permissions.codes);
    }
    if (Object.keys(request).length === 0) {
      this.editError.set(translate('thirdPartyAccess.grants.edit.noChanges'));
      return;
    }
    this.saving.set(true);
    this.service.updateGrant(grant.id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.editing.set(null);
        this.grants.update((list) => list.map((g) => (g.id === updated.id ? updated : g)));
        this.toast.success(translate('thirdPartyAccess.grants.edit.saved', { name: grant.grantee.name, reference: this.referenceOf(grant) }));
      },
      error: (err) => {
        this.saving.set(false);
        this.editError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.grants.edit.error')));
      },
    });
  }

  // Revocación (M1-22)
  startRevoke(grant: AccessGrant): void {
    this.editing.set(null);
    this.accepting.set(null);
    this.revokeReason.set('');
    this.revoking.set({ kind: 'single', grant });
    this.focusPanel('access-revoke-title');
  }

  startBulkRevoke(): void {
    const ids = [...this.selection()];
    if (ids.length === 0) return;
    this.editing.set(null);
    this.accepting.set(null);
    this.revokeReason.set('');
    this.revoking.set({ kind: 'bulk', ids });
    this.focusPanel('access-revoke-title');
  }

  cancelRevoke(): void {
    this.revoking.set(null);
  }

  confirmRevoke(): void {
    const target = this.revoking();
    if (!target) return;
    const reason = this.revokeReason().trim() || undefined;
    const request = target.kind === 'single'
      ? this.service.revokeGrant(target.grant.id, reason)
      : this.service.revokeGrants(target.ids, reason);
    this.revokeBusy.set(true);
    this.actionError.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: RevokeAccessResult) => {
        this.revokeBusy.set(false);
        this.revoking.set(null);
        this.announcer.announce(translate('thirdPartyAccess.grants.revoke.done', {
          revoked: result.revoked,
          cascade: result.cascadeRevoked,
          widenings: result.wideningsRevoked,
        }));
        this.load();
      },
      error: (err) => {
        this.revokeBusy.set(false);
        this.actionError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.grants.revoke.error')));
      },
    });
  }

  // Aceptación de términos del mandato (M1-03)
  startAccept(grant: AccessGrant): void {
    this.editing.set(null);
    this.revoking.set(null);
    this.acceptChecked.set(false);
    this.accepting.set(grant);
    this.focusPanel('access-accept-title');
    if (!this.terms()) {
      this.service.getMandateTerms().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (terms) => this.terms.set(terms),
        error: () => this.actionError.set(translate('thirdPartyAccess.grants.accept.termsError')),
      });
    }
  }

  cancelAccept(): void {
    this.accepting.set(null);
  }

  onAcceptChange(event: Event): void {
    this.acceptChecked.set((event.target as HTMLInputElement).checked);
  }

  confirmAccept(): void {
    const grant = this.accepting();
    const terms = this.terms();
    if (!grant || !terms || !this.acceptChecked()) return;
    this.acceptBusy.set(true);
    this.actionError.set('');
    this.service.acceptTerms(grant.id, terms.version).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.acceptBusy.set(false);
        this.accepting.set(null);
        this.grants.update((list) => list.map((g) => (g.id === updated.id ? updated : g)));
        this.announcer.announce(translate('thirdPartyAccess.grants.accept.done', { name: grant.grantee.name }));
      },
      error: (err) => {
        this.acceptBusy.set(false);
        this.actionError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.grants.accept.error')));
      },
    });
  }

  /** Lleva el foco al título del panel de confirmación que se acaba de abrir. */
  private focusPanel(id: string): void {
    setTimeout(() => (this.host.nativeElement as HTMLElement).querySelector<HTMLElement>(`#${id}`)?.focus());
  }
}
