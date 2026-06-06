import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-page">
      <div class="login-container">
        <div class="login-card">
          <div class="text-center mb-4">
            <svg viewBox="0 0 280 80" xmlns="http://www.w3.org/2000/svg" style="max-width: 280px;">
              <rect x="0" y="10" width="60" height="60" rx="6" fill="#FF6600"/>
              <text x="30" y="52" text-anchor="middle" font-family="Montserrat, sans-serif" font-weight="800" font-size="28" fill="#FFFFFF">HL</text>
              <text x="75" y="38" font-family="Montserrat, sans-serif" font-weight="700" font-size="22" fill="#33424F">Hapag-Lloyd</text>
              <text x="75" y="58" font-family="Inter, sans-serif" font-weight="300" font-size="11" fill="rgba(51,66,79,0.5)" letter-spacing="3">SHIPPING &amp; LOGISTICS</text>
            </svg>
            <h1 class="login-title">Recuperar Contraseña</h1>
            <p class="login-subtitle">Ingrese su correo para recibir instrucciones</p>
          </div>

          @if (success()) {
            <div class="alert alert-success">
              Se han enviado las instrucciones a su correo electrónico. Revise su bandeja de entrada.
            </div>
            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link"><strong>Volver al inicio de sesión</strong></a>
            </div>
          } @else {
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <div class="hl-form-group">
                <label for="email">Correo Electrónico</label>
                <input type="email" id="email" class="form-control" formControlName="email"
                       placeholder="usuario@empresa.com"
                       [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid" />
                @if (form.controls.email.touched && form.controls.email.errors?.['required']) {
                  <div class="invalid-feedback">El correo es obligatorio.</div>
                }
                @if (form.controls.email.touched && form.controls.email.errors?.['email']) {
                  <div class="invalid-feedback">Ingrese un correo válido.</div>
                }
              </div>

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                Enviar Instrucciones
              </button>
            </form>

            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link">
                <strong>Volver al inicio de sesión</strong>
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
