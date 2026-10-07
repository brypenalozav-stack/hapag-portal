import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { VALIDATION } from '../../../core/constants/app.constants';
import { apiErrorKey } from '../../../core/http/api-error';

/**
 * Errores de ingreso con texto propio (M1-08): una solicitud de vinculación pendiente o
 * rechazada no da acceso a la organización.
 */
const LOGIN_ERRORS: Record<string, string> = {
  'User.PendingApproval': 'auth.login.errors.pendingApproval',
  'User.MembershipRejected': 'auth.login.errors.membershipRejected',
  'User.InvalidCredentials': 'auth.login.errors.invalidCredentials',
  'User.Inactive': 'auth.login.errors.inactive',
};

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  /** Año del aviso de derechos. */
  readonly year = new Date().getFullYear();
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(VALIDATION.LOGIN_PASSWORD_MIN_LENGTH)]],
  });

  readonly minPasswordLength = VALIDATION.LOGIN_PASSWORD_MIN_LENGTH;

  loading = signal(false);
  error = signal('');

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.auth.login(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(translate(apiErrorKey(err, LOGIN_ERRORS, 'auth.login.error')));
      },
    });
  }
}
