import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
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
  imports: [FormsModule, TranslocoPipe, CountryBadgeComponent, LoadingSpinnerComponent],
  template: `
    <div class="hl-page-header">
      <h1>{{ 'admin.demurrageExemptions.title' | transloco }}</h1>
      <p class="text-muted mb-0">{{ 'admin.demurrageExemptions.subtitle' | transloco }}</p>
    </div>

    <div class="hl-card p-3 mb-4">
      <div class="row g-2 align-items-end">
        <div class="col-md-6">
          <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.filters.search' | transloco }}</label>
          <input type="text" class="form-control" [placeholder]="'admin.demurrageExemptions.filters.searchPlaceholder' | transloco" [(ngModel)]="searchTerm" />
        </div>
        <div class="col-md-3">
          <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.filters.country' | transloco }}</label>
          <select class="form-select" [(ngModel)]="countryFilter">
            <option value="ALL">{{ 'admin.demurrageExemptions.filters.all' | transloco }}</option>
            <option value="CL">{{ 'common.country.cl' | transloco }}</option>
            <option value="BO">{{ 'common.country.bo' | transloco }}</option>
          </select>
        </div>
        <div class="col-md-3 text-md-end">
          <button class="btn btn-hl-orange" (click)="openCreateForm()">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="me-1" viewBox="0 0 16 16">
              <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4"/>
            </svg>
            {{ 'admin.demurrageExemptions.new' | transloco }}
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
        <h5 class="mb-3">{{ 'admin.demurrageExemptions.form.title' | transloco }}</h5>
        @if (createError()) {
          <div class="alert alert-danger py-2 mb-3">{{ createError() }}</div>
        }
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.form.clientName' | transloco }}</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.clientName"
                   [placeholder]="'admin.demurrageExemptions.form.clientNamePlaceholder' | transloco" />
          </div>
          <div class="col-md-3">
            <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.form.taxId' | transloco }}</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.taxId"
                   [placeholder]="'admin.demurrageExemptions.form.taxIdPlaceholder' | transloco" />
          </div>
          <div class="col-md-2">
            <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.form.country' | transloco }}</label>
            <select class="form-select" [(ngModel)]="newExemption.country">
              <option value="">{{ 'admin.demurrageExemptions.form.selectCountry' | transloco }}</option>
              <option value="CL">{{ 'common.country.cl' | transloco }}</option>
              <option value="BO">{{ 'common.country.bo' | transloco }}</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label small fw-semibold">{{ 'admin.demurrageExemptions.form.reason' | transloco }}</label>
            <input type="text" class="form-control" [(ngModel)]="newExemption.reason"
                   [placeholder]="'admin.demurrageExemptions.form.reasonPlaceholder' | transloco" />
          </div>
        </div>
        <div class="d-flex gap-2 mt-3">
          <button class="btn btn-hl-orange" (click)="createExemption()" [disabled]="creating()">
            @if (creating()) {
              <span class="spinner-border spinner-border-sm me-1"></span>
            }
            {{ 'admin.demurrageExemptions.form.create' | transloco }}
          </button>
          <button class="btn btn-outline-secondary" (click)="cancelCreate()" [disabled]="creating()">
            {{ 'admin.demurrageExemptions.form.cancel' | transloco }}
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
                <th>{{ 'admin.demurrageExemptions.col.client' | transloco }}</th>
                <th>{{ 'admin.demurrageExemptions.col.taxId' | transloco }}</th>
                <th>{{ 'admin.demurrageExemptions.col.country' | transloco }}</th>
                <th>{{ 'admin.demurrageExemptions.col.reason' | transloco }}</th>
                <th>{{ 'admin.demurrageExemptions.col.status' | transloco }}</th>
                <th>{{ 'admin.demurrageExemptions.col.actions' | transloco }}</th>
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
                      <span class="badge bg-success">{{ 'admin.demurrageExemptions.active' | transloco }}</span>
                    } @else {
                      <span class="badge bg-secondary">{{ 'admin.demurrageExemptions.inactive' | transloco }}</span>
                    }
                  </td>
                  <td>
                    @if (exemption.isActive) {
                      <button class="btn btn-sm btn-outline-danger" (click)="deactivateExemption(exemption.id)"
                              [disabled]="deactivating()">
                        {{ 'admin.demurrageExemptions.deactivate' | transloco }}
                      </button>
                    }
                  </td>
                </tr>
              }
              @if (filteredExemptions().length === 0) {
                <tr>
                  <td colspan="6" class="text-center text-muted py-4">{{ 'admin.demurrageExemptions.empty' | transloco }}</td>
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
      this.createError.set(translate('admin.demurrageExemptions.errors.required'));
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
        this.createError.set(translate('admin.demurrageExemptions.errors.create'));
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
        this.error.set(translate('admin.demurrageExemptions.errors.deactivate'));
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
        this.error.set(translate('admin.demurrageExemptions.errors.load'));
        this.loading.set(false);
      },
    });
  }
}
