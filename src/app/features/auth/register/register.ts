import { Component, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { VALIDATION, REDIRECT_DELAY_MS } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group(
    {
      type: ['CLIENT' as 'CLIENT' | 'AGENT', Validators.required],
      country: ['CL' as 'CL' | 'BO', Validators.required],
      name: ['', [Validators.required, Validators.minLength(VALIDATION.NAME_MIN_LENGTH)]],
      taxId: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(VALIDATION.PASSWORD_MIN_LENGTH)]],
      confirmPassword: ['', Validators.required],
      agentCode: [''],
    },
    { validators: [this.passwordMatchValidator] },
  );

  loading = signal(false);
  error = signal('');
  success = signal(false);

  selectedCountry = computed(() => this.form.controls.country.value);
  selectedType = computed(() => this.form.controls.type.value);

  taxIdLabel = computed(() => {
    return this.form.controls.country.value === 'CL' ? 'RUT' : 'NIT';
  });

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirm = control.get('confirmPassword');
    if (password && confirm && password.value !== confirm.value) {
      confirm.setErrors({ mismatch: true });
      return { mismatch: true };
    }
    return null;
  }

  onCountryChange(): void {
    this.form.controls.country.updateValueAndValidity();
  }

  onTypeChange(): void {
    const agentCode = this.form.controls.agentCode;
    if (this.form.controls.type.value === 'AGENT') {
      agentCode.setValidators(Validators.required);
    } else {
      agentCode.clearValidators();
      agentCode.setValue('');
    }
    agentCode.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const { confirmPassword: _confirmPassword, ...data } = this.form.getRawValue();
    this.auth.register(data).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/login']), REDIRECT_DELAY_MS);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Error al registrar. Intente nuevamente.');
      },
    });
  }
}
