import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { FILTER_ALL, API_ENDPOINTS } from '../../../core/constants/app.constants';

interface CreditClient {
  id: string;
  name: string;
  taxId: string;
  country: 'CL' | 'BO';
  creditLimit: number;
  creditUsed: number;
  currency: string;
  status: string;
  approvedAt: string;
}

@Component({
  selector: 'app-credit-clients',
  standalone: true,
  imports: [DecimalPipe, FormsModule, ReactiveFormsModule, StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './credit-clients.html',
  styles: [':host { display: block; }'],
})
export class CreditClientsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  private allClients = signal<CreditClient[]>([]);
  filteredClients = signal<CreditClient[]>([]);
  loading = signal(false);
  error = signal('');
  searchTerm = '';
  countryFilter = FILTER_ALL;

  showForm = signal(false);
  editingClient = signal<CreditClient | null>(null);
  submitting = signal(false);
  formError = signal('');
  formSuccess = signal('');

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    taxId: ['', Validators.required],
    country: ['CL' as 'CL' | 'BO', Validators.required],
    creditLimit: [0, [Validators.required, Validators.min(1)]],
    currency: ['USD', Validators.required],
  });

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.loading.set(true);
    this.api.get<CreditClient[]>(API_ENDPOINTS.ADMIN_CREDIT_CLIENTS).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.allClients.set(data);
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los clientes con crédito.');
        this.loading.set(false);
      },
    });
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  private applyFilters(): void {
    let result = this.allClients();

    if (this.countryFilter !== FILTER_ALL) {
      result = result.filter((c) => c.country === this.countryFilter);
    }

    const term = this.searchTerm.trim().toLowerCase();
    if (term) {
      result = result.filter(
        (c) => c.name.toLowerCase().includes(term) || c.taxId.toLowerCase().includes(term),
      );
    }

    this.filteredClients.set(result);
  }

  openCreateForm(): void {
    this.editingClient.set(null);
    this.form.reset({ name: '', taxId: '', country: 'CL', creditLimit: 0, currency: 'USD' });
    this.formError.set('');
    this.formSuccess.set('');
    this.showForm.set(true);
  }

  openEditForm(client: CreditClient): void {
    this.editingClient.set(client);
    this.form.patchValue({
      name: client.name,
      taxId: client.taxId,
      country: client.country,
      creditLimit: client.creditLimit,
      currency: client.currency,
    });
    this.formError.set('');
    this.formSuccess.set('');
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingClient.set(null);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set('');
    this.formSuccess.set('');

    const data = this.form.getRawValue();
    const editing = this.editingClient();

    const request$ = editing
      ? this.api.put(`${API_ENDPOINTS.ADMIN_CREDIT_CLIENTS}/${editing.id}`, data)
      : this.api.post(API_ENDPOINTS.ADMIN_CREDIT_CLIENTS, data);

    request$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.submitting.set(false);
        this.formSuccess.set(editing ? 'Cliente actualizado exitosamente.' : 'Cliente creado exitosamente.');
        this.loadClients();
        setTimeout(() => {
          this.showForm.set(false);
          this.editingClient.set(null);
        }, 1500);
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.message ?? 'Error al guardar el cliente.');
      },
    });
  }
}
