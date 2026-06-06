import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ClientService } from '../../core/services/client.service';
import { Client } from '../../core/models/client.model';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, CountryBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly clientService = inject(ClientService);
  private readonly destroyRef = inject(DestroyRef);

  profile = signal<Client | null>(null);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  success = signal('');
  editing = signal(false);

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    phone: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.clientService.getProfile().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.profile.set(data);
        this.form.patchValue({ name: data.name, phone: data.phone });
        this.loading.set(false);
      },
      error: () => {
        // Fallback to local user data
        const user = this.auth.getCurrentUser();
        if (user) {
          this.profile.set(user);
          this.form.patchValue({ name: user.name, phone: user.phone });
        }
        this.loading.set(false);
      },
    });
  }

  toggleEdit(): void {
    this.editing.update((v) => !v);
    this.success.set('');
    this.error.set('');
    if (this.editing()) {
      const p = this.profile();
      if (p) this.form.patchValue({ name: p.name, phone: p.phone });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.success.set('');

    this.clientService.updateProfile(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.saving.set(false);
        this.editing.set(false);
        this.success.set('Perfil actualizado exitosamente.');
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'Error al actualizar el perfil.');
      },
    });
  }
}
