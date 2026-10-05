import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { VALIDATION, REDIRECT_DELAY_MS } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe],
  template: `
    <div class="login-page">
      <div class="login-container">
        <div class="login-card">
          <div class="text-center mb-4">
            <svg viewBox="0 0 280 80" xmlns="http://www.w3.org/2000/svg" style="max-width: 280px;">
              <rect x="0" y="10" width="60" height="60" rx="6" class="hl-logo__mark"/>
              <text x="30" y="52" text-anchor="middle" font-family="Montserrat, sans-serif" font-weight="800" font-size="28" class="hl-logo__initials">HL</text>
              <text x="75" y="38" font-family="Montserrat, sans-serif" font-weight="700" font-size="22" class="hl-logo__name">Hapag-Lloyd</text>
              <text x="75" y="58" font-family="Inter, sans-serif" font-weight="300" font-size="11" fill="rgba(51,66,79,0.5)" letter-spacing="3">{{ 'common.brand.tagline' | transloco }}</text>
            </svg>
            <h1 class="login-title">{{ 'auth.resetPassword.title' | transloco }}</h1>
            <p class="login-subtitle">{{ 'auth.resetPassword.subtitle' | transloco }}</p>
          </div>

          @if (success()) {
            <div class="alert alert-success">
              {{ 'auth.resetPassword.success' | transloco }}
            </div>
          } @else {
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <div class="hl-form-group">
                <label for="newPassword">{{ 'auth.resetPassword.newPassword' | transloco }}</label>
                <input type="password" id="newPassword" class="form-control" formControlName="newPassword"
                       [placeholder]="'auth.resetPassword.newPasswordPlaceholder' | transloco: { min: minPasswordLength }"
                       [class.is-invalid]="form.controls.newPassword.touched && form.controls.newPassword.invalid" />
                @if (form.controls.newPassword.touched && form.controls.newPassword.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'auth.resetPassword.passwordRequired' | transloco }}</div>
                }
                @if (form.controls.newPassword.touched && form.controls.newPassword.errors?.['minlength']) {
                  <div class="invalid-feedback">{{ 'auth.resetPassword.passwordMinLength' | transloco: { min: minPasswordLength } }}</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="confirmPassword">{{ 'auth.resetPassword.confirmPassword' | transloco }}</label>
                <input type="password" id="confirmPassword" class="form-control" formControlName="confirmPassword"
                       [placeholder]="'auth.resetPassword.confirmPasswordPlaceholder' | transloco"
                       [class.is-invalid]="form.controls.confirmPassword.touched && form.controls.confirmPassword.invalid" />
                @if (form.controls.confirmPassword.touched && form.controls.confirmPassword.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'auth.resetPassword.confirmPasswordRequired' | transloco }}</div>
                }
              </div>

              @if (passwordMismatch()) {
                <div class="alert alert-warning py-2 small">{{ 'auth.resetPassword.passwordMismatch' | transloco }}</div>
              }

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                {{ 'auth.resetPassword.submit' | transloco }}
              </button>
            </form>

            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link"><strong>{{ 'auth.resetPassword.backToLogin' | transloco }}</strong></a>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styleUrl: '../login/login.scss',
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  private token = '';
  private email = '';

  form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(VALIDATION.PASSWORD_MIN_LENGTH)]],
    confirmPassword: ['', Validators.required],
  });

  readonly minPasswordLength = VALIDATION.PASSWORD_MIN_LENGTH;

  loading = signal(false);
  error = signal('');
  success = signal(false);
  passwordMismatch = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';

    if (!this.token || !this.email) {
      this.error.set(translate('auth.resetPassword.invalidLink'));
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { newPassword, confirmPassword } = this.form.getRawValue();
    if (newPassword !== confirmPassword) {
      this.passwordMismatch.set(true);
      return;
    }
    this.passwordMismatch.set(false);

    this.loading.set(true);
    this.error.set('');

    this.auth.resetPassword({ token: this.token, email: this.email, newPassword }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/login']), REDIRECT_DELAY_MS);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? translate('auth.resetPassword.error'));
      },
    });
  }
}
