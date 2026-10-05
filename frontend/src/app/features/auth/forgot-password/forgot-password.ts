import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
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
            <h1 class="login-title">{{ 'auth.forgotPassword.title' | transloco }}</h1>
            <p class="login-subtitle">{{ 'auth.forgotPassword.subtitle' | transloco }}</p>
          </div>

          @if (success()) {
            <div class="alert alert-success">
              {{ 'auth.forgotPassword.success' | transloco }}
            </div>
            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link"><strong>{{ 'auth.forgotPassword.backToLogin' | transloco }}</strong></a>
            </div>
          } @else {
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <div class="hl-form-group">
                <label for="email">{{ 'auth.forgotPassword.email' | transloco }}</label>
                <input type="email" id="email" class="form-control" formControlName="email"
                       [placeholder]="'auth.forgotPassword.emailPlaceholder' | transloco"
                       [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid" />
                @if (form.controls.email.touched && form.controls.email.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'auth.forgotPassword.emailRequired' | transloco }}</div>
                }
                @if (form.controls.email.touched && form.controls.email.errors?.['email']) {
                  <div class="invalid-feedback">{{ 'auth.forgotPassword.emailInvalid' | transloco }}</div>
                }
              </div>

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                {{ 'auth.forgotPassword.submit' | transloco }}
              </button>
            </form>

            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link">
                <strong>{{ 'auth.forgotPassword.backToLogin' | transloco }}</strong>
              </a>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styleUrl: '../login/login.scss',
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  loading = signal(false);
  error = signal('');
  success = signal(false);

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.auth.forgotPassword(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: () => {
        this.loading.set(false);
        // Always show success to prevent email enumeration
        this.success.set(true);
      },
    });
  }
}
