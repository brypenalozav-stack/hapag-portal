import { Component, DestroyRef, ElementRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ACCESS_DURATION_MAX_DAYS,
  ACCESS_DURATION_MIN_DAYS,
  DefaultGrantee,
  GrantableAction,
  GranteeOrganization,
  UpdateDefaultGranteeRequest,
} from '../../../core/models/access.model';
import { ACCESS_ACTION_KEYS, ORGANIZATION_TYPE_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { GranteePickerComponent } from '../shared/grantee-picker';
import { PermissionChecklistComponent } from '../shared/permission-checklist';
import { PermissionsFormValue, basePermissions, explicitCodes, permissionsOf } from '../shared/access-form';
import { GRANT_ERRORS } from '../shared/access-errors';

/** Formulario abierto: alta de un tercero por defecto o edición de uno existente. */
type DefaultForm = { kind: 'new' } | { kind: 'edit'; item: DefaultGrantee };

/**
 * Terceros por defecto (M1-13): reciben acceso automático sobre cada BL nuevo de la organización,
 * con permisos opcionales (M1-15) y duración opcional (M1-14). Cambiar la configuración no altera
 * los accesos ya otorgados; el ajuste puntual se hace en la tabla de accesos.
 */
@Component({
  selector: 'app-access-defaults',
  standalone: true,
  imports: [
    TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    GranteePickerComponent, PermissionChecklistComponent,
  ],
  templateUrl: './access-defaults.html',
})
export class AccessDefaultsComponent implements OnInit {
  private readonly service = inject(AccessService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject(ElementRef<HTMLElement>);

  canManage = input(false);

  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;
  readonly minDays = ACCESS_DURATION_MIN_DAYS;
  readonly maxDays = ACCESS_DURATION_MAX_DAYS;

  items = signal<DefaultGrantee[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');

  form = signal<DefaultForm | null>(null);
  grantee = signal<GranteeOrganization | null>(null);
  permissions = signal<PermissionsFormValue>(basePermissions());
  durationDays = signal<number | null>(null);
  actions = signal<GrantableAction[]>([]);
  actionsLoading = signal(false);
  submitted = signal(false);
  formError = signal('');
  saving = signal(false);

  deleting = signal<DefaultGrantee | null>(null);
  deleteBusy = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getDefaults().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.items.set([]);
        this.loadFailed.set(isServiceUnavailable(err));
        if (!isServiceUnavailable(err)) this.actionError.set(translate('thirdPartyAccess.defaults.errors.load'));
        this.loading.set(false);
      },
    });
  }

  openNew(): void {
    this.deleting.set(null);
    this.grantee.set(null);
    this.permissions.set(basePermissions());
    this.durationDays.set(null);
    this.actions.set([]);
    this.submitted.set(false);
    this.formError.set('');
    this.form.set({ kind: 'new' });
    this.focus('access-default-form-title');
  }

  openEdit(item: DefaultGrantee): void {
    this.deleting.set(null);
    this.grantee.set(null);
    this.permissions.set(permissionsOf(item.actionCodes));
    this.durationDays.set(item.durationDays);
    this.submitted.set(false);
    this.formError.set('');
    this.form.set({ kind: 'edit', item });
    this.loadActions(item.grantee.id);
    this.focus('access-default-form-title');
  }

  cancelForm(): void {
    this.form.set(null);
  }

  onGranteeChange(grantee: GranteeOrganization | null): void {
    this.grantee.set(grantee);
    if (grantee) this.loadActions(grantee.id);
    else this.actions.set([]);
  }

  onDurationInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const value = raw === '' ? null : Number(raw);
    this.durationDays.set(value === null || Number.isNaN(value) ? null : value);
  }

  durationInvalid(): boolean {
    const days = this.durationDays();
    return days !== null && (!Number.isInteger(days) || days < this.minDays || days > this.maxDays);
  }

  /** Acciones del nivel de cliente de la organización (sin BL), para el selector de permisos. */
  private loadActions(granteeId: string): void {
    this.actionsLoading.set(true);
    this.service.getGrantableActions({ granteeOrganizationId: granteeId }).pipe(
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
    const form = this.form();
    if (!form) return;
    this.submitted.set(true);
    this.formError.set('');
    if ((form.kind === 'new' && !this.grantee()) || this.durationInvalid()) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    const permissions = this.permissions();
    const codes = permissions.mode === 'custom' ? explicitCodes(permissions.codes) : undefined;
    const duration = this.durationDays();

    let request: Observable<DefaultGrantee>;
    if (form.kind === 'new') {
      request = this.service.createDefault({
        granteeOrganizationId: (this.grantee() as GranteeOrganization).id,
        ...(codes ? { actionCodes: codes } : {}),
        ...(duration !== null ? { durationDays: duration } : {}),
      });
    } else {
      const update: UpdateDefaultGranteeRequest = { durationDays: duration };
      if (codes) update.actionCodes = codes;
      else update.resetToBaseLevel = true;
      request = this.service.updateDefault(form.item.id, update);
    }

    this.saving.set(true);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.form.set(null);
        this.announcer.announce(
          translate(form.kind === 'new' ? 'thirdPartyAccess.defaults.added' : 'thirdPartyAccess.defaults.updated', { name: saved.grantee.name }),
        );
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.defaults.errors.save')));
      },
    });
  }

  startDelete(item: DefaultGrantee): void {
    this.form.set(null);
    this.deleting.set(item);
    this.focus('access-default-delete-title');
  }

  cancelDelete(): void {
    this.deleting.set(null);
  }

  confirmDelete(event: Event): void {
    event.preventDefault();
    const item = this.deleting();
    if (!item) return;
    this.deleteBusy.set(true);
    this.actionError.set('');
    this.service.deleteDefault(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.deleteBusy.set(false);
        this.deleting.set(null);
        this.items.update((list) => list.filter((d) => d.id !== item.id));
        this.announcer.announce(translate('thirdPartyAccess.defaults.removed', { name: item.grantee.name }));
      },
      error: (err) => {
        this.deleteBusy.set(false);
        this.actionError.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.defaults.errors.delete')));
      },
    });
  }

  private focus(id: string): void {
    setTimeout(() => (this.host.nativeElement as HTMLElement).querySelector<HTMLElement>(`#${id}`)?.focus());
  }
}
