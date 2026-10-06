import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { OrganizationService } from '../../../core/services/organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ORGANIZATION_PROFILES,
  OrganizationProfile,
  OrganizationUser,
} from '../../../core/models/organization.model';
import { MEMBERSHIP_STATUS_KEYS, ORGANIZATION_PROFILE_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

const USER_ERRORS: Record<string, string> = {
  'User.EmailExists': 'organization.users.errors.emailExists',
  'Organization.CannotChangeOwnAccount': 'organization.users.errors.ownAccount',
};

/** Modo del formulario para invitar a un usuario nuevo. */
const NEW_USER = 'new';

/**
 * Usuarios de la propia organización (M1-02): invitar, modificar el perfil y activar o
 * desactivar. Requiere el permiso org.users.manage, que el servidor vuelve a exigir.
 */
@Component({
  selector: 'app-organization-users',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './organization-users.html',
})
export class OrganizationUsersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly service = inject(OrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly profiles = ORGANIZATION_PROFILES;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;
  readonly membershipKeys = MEMBERSHIP_STATUS_KEYS;

  users = signal<OrganizationUser[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');

  /** Formulario abierto: 'new' para invitar o el usuario que se edita. */
  formMode = signal<typeof NEW_USER | OrganizationUser | null>(null);
  editing = computed(() => {
    const mode = this.formMode();
    return mode && mode !== NEW_USER ? mode : null;
  });
  submitting = signal(false);
  formError = signal('');

  form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    profile: ['OrgViewer' as OrganizationProfile, Validators.required],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getUsers().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: (err) => {
        this.users.set([]);
        this.loadFailed.set(isServiceUnavailable(err));
        if (!isServiceUnavailable(err)) this.actionError.set(translate('organization.users.errors.load'));
        this.loading.set(false);
      },
    });
  }

  isSelf(user: OrganizationUser): boolean {
    return user.email.toLowerCase() === this.auth.currentUser()?.email?.toLowerCase();
  }

  openInvite(): void {
    this.form.reset({ firstName: '', lastName: '', email: '', phone: '', profile: 'OrgViewer' });
    this.form.controls.email.enable();
    this.formError.set('');
    this.formMode.set(NEW_USER);
  }

  openEdit(user: OrganizationUser): void {
    this.form.reset({
      firstName: user.firstName ?? '',
      lastName: user.lastName ?? '',
      email: user.email,
      phone: user.phone ?? '',
      profile: user.profile ?? 'OrgViewer',
    });
    // El correo identifica la cuenta y no se modifica.
    this.form.controls.email.disable();
    this.formError.set('');
    this.formMode.set(user);
  }

  cancelForm(): void {
    this.formMode.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    const raw = this.form.getRawValue();
    const editing = this.editing();
    this.submitting.set(true);
    this.formError.set('');

    const request = editing
      ? this.service.updateUser(editing.id, {
          firstName: raw.firstName,
          lastName: raw.lastName,
          phone: raw.phone || undefined,
          profile: raw.profile,
        })
      : this.service.createUser({
          firstName: raw.firstName,
          lastName: raw.lastName,
          email: raw.email,
          phone: raw.phone || undefined,
          profile: raw.profile,
        });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.formMode.set(null);
        this.announcer.announce(
          translate(editing ? 'organization.users.updated' : 'organization.users.invited', { email: raw.email }),
        );
        this.load();
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(
          translate(apiErrorKey(err, USER_ERRORS, editing ? 'organization.users.errors.update' : 'organization.users.errors.create')),
        );
      },
    });
  }

  /** Activa o desactiva el acceso del usuario (M1-02). */
  toggleActive(user: OrganizationUser): void {
    const isActive = !user.isActive;
    this.actionError.set('');
    this.service.setUserActive(user.id, isActive).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.users.update((list) => list.map((u) => (u.id === user.id ? { ...u, isActive } : u)));
        this.announcer.announce(
          translate(isActive ? 'organization.users.activated' : 'organization.users.deactivated', { name: user.fullName }),
        );
      },
      error: (err) => this.actionError.set(translate(apiErrorKey(err, USER_ERRORS, 'organization.users.errors.toggle'))),
    });
  }
}
