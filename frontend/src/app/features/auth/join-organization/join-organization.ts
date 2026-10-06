import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { VALIDATION } from '../../../core/constants/app.constants';
import { apiErrorCode, apiErrorKey } from '../../../core/http/api-error';
import { PRE_CREATED_ACCOUNT_EXISTS, PreCreatedNoticeComponent } from '../pre-created-notice/pre-created-notice';

const JOIN_ERRORS: Record<string, string> = {
  'Organization.NotFound': 'auth.join.errors.notFound',
  'User.EmailExists': 'auth.join.errors.emailExists',
};

/**
 * Solicitud de un usuario nuevo para unirse a una organización ya registrada (M1-08). Queda
 * pendiente hasta que el administrador de la organización la apruebe; mientras, no ingresa.
 */
@Component({
  selector: 'app-join-organization',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe, PreCreatedNoticeComponent],
  templateUrl: './join-organization.html',
  styleUrl: '../register/register.scss',
})
export class JoinOrganizationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group(
    {
      country: ['CL' as 'CL' | 'BO', Validators.required],
      taxId: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      password: ['', [Validators.required, Validators.minLength(VALIDATION.PASSWORD_MIN_LENGTH)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: [this.passwordMatchValidator] },
  );

  readonly minPasswordLength = VALIDATION.PASSWORD_MIN_LENGTH;

  loading = signal(false);
  error = signal('');
  /** Ya existe una cuenta pre-creada por un cliente para el correo o el RUT (M1-09): se dirige al ingreso. */
  preCreated = signal(false);
  /** Organización a la que se pidió unirse: muestra el resultado pendiente. */
  requestedOrganization = signal<string | null>(null);

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirm = control.get('confirmPassword');
    if (password && confirm && password.value !== confirm.value) {
      confirm.setErrors({ mismatch: true });
      return { mismatch: true };
    }
    return null;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.preCreated.set(false);

    const raw = this.form.getRawValue();
    this.auth.requestMembership({
      taxId: raw.taxId.trim(),
      country: raw.country,
      email: raw.email,
      password: raw.password,
      firstName: raw.firstName,
      lastName: raw.lastName,
      phone: raw.phone || undefined,
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.requestedOrganization.set(response.organizationName);
        this.announcer.announce(translate('auth.join.result.title'));
      },
      error: (err) => {
        this.loading.set(false);
        if (apiErrorCode(err) === PRE_CREATED_ACCOUNT_EXISTS) {
          this.preCreated.set(true);
          this.announcer.announce(translate('auth.preCreated.title'), 'assertive');
          return;
        }
        this.error.set(translate(apiErrorKey(err, JOIN_ERRORS, 'auth.join.errors.generic')));
      },
    });
  }
}
