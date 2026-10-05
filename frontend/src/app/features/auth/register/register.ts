import { Component, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { VALIDATION } from '../../../core/constants/app.constants';
import { OrganizationType, REGISTRABLE_ORGANIZATION_TYPES } from '../../../core/models/organization.model';
import { ORGANIZATION_TYPE_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';

/** Errores del registro con texto propio; el resto muestra el mensaje genérico. */
const REGISTER_ERRORS: Record<string, string> = {
  'Client.AlreadyExists': 'auth.register.errors.alreadyExists',
  'User.EmailExists': 'auth.register.errors.emailExists',
};

/**
 * Registro autónomo de una organización nueva (M1-07): tipo de organización, identificación
 * tributaria, razón social, país y contacto. La organización queda pendiente de la validación
 * interna de M8-04 y no opera hasta que se apruebe.
 */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe, CodeLabelPipe],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly organizationTypes = REGISTRABLE_ORGANIZATION_TYPES;
  readonly organizationTypeKeys = ORGANIZATION_TYPE_KEYS;

  form = this.fb.nonNullable.group(
    {
      organizationType: ['Customer' as OrganizationType, Validators.required],
      country: ['CL' as 'CL' | 'BO', Validators.required],
      name: ['', [Validators.required, Validators.minLength(VALIDATION.NAME_MIN_LENGTH)]],
      taxId: ['', Validators.required],
      contactFirstName: ['', Validators.required],
      contactLastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(VALIDATION.PASSWORD_MIN_LENGTH)]],
      confirmPassword: ['', Validators.required],
      agentCode: [''],
    },
    { validators: [this.passwordMatchValidator] },
  );

  readonly minPasswordLength = VALIDATION.PASSWORD_MIN_LENGTH;

  loading = signal(false);
  error = signal('');
  /** Razón social de la organización registrada: muestra el resultado pendiente de aprobación. */
  registeredName = signal<string | null>(null);

  selectedCountry = computed(() => this.form.controls.country.value);

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

  /** La agencia de aduanas informa su código de agente. */
  onTypeChange(): void {
    const agentCode = this.form.controls.agentCode;
    if (this.form.controls.organizationType.value === 'CustomsAgency') {
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
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const raw = this.form.getRawValue();
    const payload = {
      name: raw.name,
      email: raw.email,
      password: raw.password,
      taxId: raw.taxId,
      phone: raw.phone,
      country: raw.country,
      organizationType: raw.organizationType,
      contactFirstName: raw.contactFirstName,
      contactLastName: raw.contactLastName,
      // Contrato heredado: el backend sigue aceptando clientType (BUG-1); organizationType prevalece.
      clientType: raw.organizationType === 'CustomsAgency' ? ('CustomsAgent' as const) : ('Client' as const),
      agentCode: raw.agentCode || undefined,
    };
    this.auth.register(payload).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.loading.set(false);
        this.registeredName.set(raw.name);
        this.announcer.announce(translate('auth.register.pending.title'));
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(translate(apiErrorKey(err, REGISTER_ERRORS, 'auth.register.error')));
      },
    });
  }
}
