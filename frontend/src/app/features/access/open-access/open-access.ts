import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { GrantableAction, OpenAccessSetting, UpdateOpenAccessRequest } from '../../../core/models/access.model';
import { ACCESS_ACTION_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { PermissionChecklistComponent } from '../shared/permission-checklist';
import { PermissionsFormValue, basePermissions, explicitCodes, permissionsOf } from '../shared/access-form';
import { GRANT_ERRORS } from '../shared/access-errors';

/**
 * Acceso abierto por número de BL (M1-17): configuración general del customer que, activa,
 * permite a cualquier usuario que ingrese el número de un BL suyo ver lo que habilita un único
 * conjunto de permisos (M1-15). Se puede activar y desactivar en cualquier momento.
 */
@Component({
  selector: 'app-open-access',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, PermissionChecklistComponent],
  templateUrl: './open-access.html',
})
export class OpenAccessComponent implements OnInit {
  private readonly service = inject(AccessService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** Permiso org.access.manage del perfil; el servidor además indica `canManage`. */
  canManage = input(false);

  readonly actionKeys = ACCESS_ACTION_KEYS;

  setting = signal<OpenAccessSetting | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  enabled = signal(false);
  permissions = signal<PermissionsFormValue>(basePermissions());
  actions = signal<GrantableAction[]>([]);
  actionsLoading = signal(false);
  saving = signal(false);

  editable = computed(() => this.canManage() && !!this.setting()?.canManage);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getOpenAccess().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (setting) => {
        this.apply(setting);
        this.loading.set(false);
        if (this.editable()) this.loadActions();
      },
      error: (err) => {
        this.loadFailed.set(isServiceUnavailable(err));
        if (!isServiceUnavailable(err)) this.error.set(translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.openAccess.errors.load')));
        this.loading.set(false);
      },
    });
  }

  private apply(setting: OpenAccessSetting): void {
    this.setting.set(setting);
    this.enabled.set(setting.isEnabled);
    this.permissions.set(permissionsOf(setting.actionCodes));
  }

  private loadActions(): void {
    this.actionsLoading.set(true);
    this.service.getGrantableActions({}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (actions) => {
        this.actions.set(actions);
        this.actionsLoading.set(false);
      },
      error: () => this.actionsLoading.set(false),
    });
  }

  onToggle(event: Event): void {
    this.enabled.set((event.target as HTMLInputElement).checked);
  }

  save(event: Event): void {
    event.preventDefault();
    const permissions = this.permissions();
    const request: UpdateOpenAccessRequest = { isEnabled: this.enabled() };
    if (permissions.mode === 'custom') request.actionCodes = explicitCodes(permissions.codes);
    else request.resetToBaseLevel = true;

    this.saving.set(true);
    this.error.set('');
    this.service.updateOpenAccess(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (setting) => {
        this.saving.set(false);
        this.apply(setting);
        this.announcer.announce(translate(setting.isEnabled ? 'thirdPartyAccess.openAccess.savedEnabled' : 'thirdPartyAccess.openAccess.savedDisabled'));
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, GRANT_ERRORS, 'thirdPartyAccess.openAccess.errors.save'));
        this.error.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
