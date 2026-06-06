import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { VALIDATION, REDIRECT_DELAY_MS } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-reset-password',
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
            <h1 class="login-title">Nueva Contraseña</h1>
            <p class="login-subtitle">Ingrese su nueva contraseña</p>
          </div>

          @if (success()) {
            <div class="alert alert-success">
              Su contraseña ha sido actualizada exitosamente. Redirigiendo al inicio de sesión...
            </div>
          } @else {
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <div class="hl-form-group">
                <label for="newPassword">Nueva Contraseña</label>
                <input type="password" id="newPassword" class="form-control" formControlName="newPassword"
                       placeholder="Mínimo 8 caracteres"
                       [class.is-invalid]="form.controls.newPassword.touched && form.controls.newPassword.invalid" />
                @if (form.controls.newPassword.touched && form.controls.newPassword.errors?.['required']) {
                  <div class="invalid-feedback">La contraseña es obligatoria.</div>
                }
                @if (form.controls.newPassword.touched && form.controls.newPassword.errors?.['minlength']) {
                  <div class="invalid-feedback">Mínimo 8 caracteres.</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="confirmPassword">Confirmar Contraseña</label>
                <input type="password" id="confirmPassword" class="form-control" formControlName="confirmPassword"
                       placeholder="Repita la contraseña"
                       [class.is-invalid]="form.controls.confirmPassword.touched && form.controls.confirmPassword.invalid" />
                @if (form.controls.confirmPassword.touched && form.controls.confirmPassword.errors?.['required']) {
                  <div class="invalid-feedback">Confirme la contraseña.</div>
                }
              </div>

              @if (passwordMismatch()) {
                <div class="alert alert-warning py-2 small">Las contraseñas no coinciden.</div>
              }

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                Cambiar Contraseña
              </button>
            </form>

            <div class="text-center mt-3">
              <a routerLink="/login" class="register-link"><strong>Volver al inicio de sesión</strong></a>
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

  loading = signal(false);
  error = signal('');
  success = signal(false);
  passwordMismatch = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';

    if (!this.token || !this.email) {
      this.error.set('Enlace de recuperación inválido o expirado.');
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
        this.error.set(err.error?.message ?? 'Error al cambiar la contraseña. El enlace puede haber expirado.');
      },
    });
  }
}
