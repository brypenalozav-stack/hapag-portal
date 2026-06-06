import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { FILTER_ALL, API_ENDPOINTS } from '../../../core/constants/app.constants';

interface DemurrageExemption {
  id: string;
  clientName: string;
  taxId: string;
  country: 'CL' | 'BO';
  reason: string | null;
  isActive: boolean;
}

interface CreateExemptionRequest {
  clientName: string;
  taxId: string;
  country: string;
  reason: string | null;
}

@Component({
  selector: 'app-demurrage-exemptions',
  standalone: true,
  imports: [FormsModule, CountryBadgeComponent, LoadingSpinnerComponent],
  template: `
    <div class="hl-page-header">
      <h1>Excepciones de Demurrage</h1>
      <p class="text-muted mb-0">Gestión de exenciones de demurrage para clientes</p>
    </div>

    <div class="hl-card p-3 mb-4">
      <div class="row g-2 align-items-end">
        <div class="col-md-6">
          <label class="form-label small fw-semibold">Buscar</label>
          <input type="text" class="form-control" placeholder="Cliente, RUT/NIT..." [(ngModel)]="searchTerm" />
        </div>
        <div class="col-md-3">
          <label class="form-label small fw-semibold">País</label>
          <select class="form-select" [(ngModel)]="countryFilter">
            <option value="ALL">Todos</option>
            <option value="CL">Chile</option>
            <option value="BO">Bolivia</option>
          </select>
        </div>
        <div class="col-md-3 text-md-end">
          <button class="btn btn-hl-orange" (click)="openCreateForm()">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="me-1" viewBox="0 0 16 16">
              <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4"/>
            </svg>
            Nueva Excepción
          </button>
        </div>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger py-2 mb-3">{{ error() }}</div>
    }

    <!-- Create Form -->
    @if (showCreateForm()) {
      <div class="hl-card p-4 mb-4 border-start border-4 border-warning">
        <h5 class="mb-3">Nueva Excepción de Demurrage</h5>
        @if (createError()) {
          <div class="alert alert-danger py-2 mb-3">{{ createError() }}</div>
        }
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label small fw-semibold">Nombre del Cliente *</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.clientName"
                   placeholder="Ej: Importadora Demo SpA" />
          </div>
          <div class="col-md-3">
            <label class="form-label small fw-semibold">RUT / NIT *</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.taxId"
                   placeholder="Ej: 76.123.456-7" />
          </div>
          <div class="col-md-2">
            <label class="form-label small fw-semibold">País *</label>
            <select class="form-select" [(ngModel)]="newExemption.country">
              <option value="">Seleccionar</option>
              <option value="CL">Chile</option>
              <option value="BO">Bolivia</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label small fw-semibold">Motivo</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.reason"
                   placeholder="Ej: Acuerdo comercial" />
          </div>
        </div>
        <div class="d-flex gap-2 mt-3">
          <button class="btn btn-hl-orange" (click)="createExemption()" [disabled]="creating()">
            @if (creating()) {
              <span class="spinner-border spinner-border-sm me-1"></span>
            }
            Crear Excepción
          </button>
          <button class="btn btn-outline-secondary" (click)="cancelCreate()" [disabled]="creating()">
            Cancelar
          </button>
        </div>
      </div>
    }

    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <div class="hl-card">
        <div class="table-responsive">
          <table class="hl-table">
            <thead>
              <tr>
                <th>Cliente</th>
                <th>RUT / NIT</th>
                <th>País</th>
                <th>Motivo</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (exemption of filteredExemptions(); track exemption.id) {
                <tr>
                  <td class="fw-semibold">{{ exemption.clientName }}</td>
                  <td>{{ exemption.taxId }}</td>
                  <td><app-country-badge [country]="exemption.country" /></td>
                  <td>{{ exemption.reason ?? '-' }}</td>
                  <td>
                    @if (exemption.isActive) {
                      <span class="badge bg-success">Activa</span>
                    } @else {
                      <span class="badge bg-secondary">Inactiva</span>
                    }
                  </td>
                  <td>
                    @if (exemption.isActive) {
                      <button class="btn btn-sm btn-outline-danger" (click)="deactivateExemption(exemption.id)"
                              [disabled]="deactivating()">
                        Desactivar
                      </button>
                    }
                  </td>
                </tr>
              }
              @if (filteredExemptions().length === 0) {
                <tr>
                  <td colspan="6" class="text-center text-muted py-4">No se encontraron excepciones.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `,
  styles: [':host { display: block; }'],
})
export class DemurrageExemptionsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  exemptions = signal<DemurrageExemption[]>([]);
  loading = signal(false);
  error = signal('');
  searchTerm = '';
  countryFilter = FILTER_ALL;

  showCreateForm = signal(false);
  creating = signal(false);
  createError = signal('');
  deactivating = signal(false);
  newExemption: CreateExemptionRequest = { clientName: '', taxId: '', country: '', reason: null };

  filteredExemptions = computed(() => {
    let result = this.exemptions();
    if (this.countryFilter !== FILTER_ALL) {
      result = result.filter((e) => e.country === this.countryFilter);
    }
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase().trim();
      result = result.filter(
        (e) =>
          e.clientName.toLowerCase().includes(term) ||
          e.taxId.toLowerCase().includes(term) ||
          (e.reason && e.reason.toLowerCase().includes(term)),
      );
    }
    return result;
  });

  ngOnInit(): void {
    this.loadExemptions();
  }

  openCreateForm(): void {
    this.showCreateForm.set(true);
    this.createError.set('');
    this.newExemption = { clientName: '', taxId: '', country: '', reason: null };
  }

  cancelCreate(): void {
    this.showCreateForm.set(false);
    this.createError.set('');
  }

  createExemption(): void {
    if (!this.newExemption.clientName.trim() || !this.newExemption.taxId.trim() || !this.newExemption.country) {
      this.createError.set('Nombre del cliente, RUT/NIT y país son obligatorios.');
      return;
    }

    this.creating.set(true);
    this.createError.set('');

    const payload: CreateExemptionRequest = {
      ...this.newExemption,
      reason: this.newExemption.reason?.trim() || null,
    };

    this.api.post<DemurrageExemption>(API_ENDPOINTS.ADMIN_DEMURRAGE_EXEMPTIONS, payload).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (created) => {
        this.exemptions.update((list) => [created, ...list]);
        this.showCreateForm.set(false);
        this.creating.set(false);
      },
      error: () => {
        this.createError.set('Error al crear la excepción. Verifique los datos e intente nuevamente.');
        this.creating.set(false);
      },
    });
  }

  deactivateExemption(id: string): void {
    this.deactivating.set(true);
    this.api.delete(`${API_ENDPOINTS.ADMIN_DEMURRAGE_EXEMPTIONS}/${id}`).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.exemptions.update((list) =>
          list.map((e) => e.id === id ? { ...e, isActive: false } : e),
        );
        this.deactivating.set(false);
      },
      error: () => {
        this.error.set('Error al desactivar la excepción.');
        this.deactivating.set(false);
      },
    });
  }

  private loadExemptions(): void {
    this.loading.set(true);
    this.api.get<DemurrageExemption[]>(API_ENDPOINTS.ADMIN_DEMURRAGE_EXEMPTIONS).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.exemptions.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar las excepciones de demurrage.');
        this.loading.set(false);
      },
    });
  }
}
