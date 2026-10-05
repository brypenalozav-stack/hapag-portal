import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AdminOrganizationService } from '../../../core/services/admin-organization.service';
import {
  AdminOrganizationItem,
  ORGANIZATION_STATUSES,
  OrganizationStatus,
  OrganizationType,
  REGISTRABLE_ORGANIZATION_TYPES,
} from '../../../core/models/organization.model';
import { ORGANIZATION_STATUS_KEYS, ORGANIZATION_TYPE_KEYS } from '../../../core/i18n/labels';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Bandeja interna de organizaciones (M8-04, M8-06): registros por estado para validar al
 * cliente, hacer el control con AR y asignar el Match Code.
 */
@Component({
  selector: 'app-admin-organizations',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe,
    CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './organizations.html',
  styles: [':host { display: block; }'],
})
export class AdminOrganizationsComponent implements OnInit {
  private readonly service = inject(AdminOrganizationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statuses = ORGANIZATION_STATUSES;
  readonly types = REGISTRABLE_ORGANIZATION_TYPES;
  readonly statusKeys = ORGANIZATION_STATUS_KEYS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;
  readonly pageSize = 20;

  statusFilter: OrganizationStatus | '' = '';
  typeFilter: OrganizationType | '' = '';
  searchTerm = '';

  organizations = signal<AdminOrganizationItem[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(true);
  error = signal('');
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  ngOnInit(): void {
    this.load();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.search({
      status: this.statusFilter,
      organizationType: this.typeFilter,
      search: this.searchTerm.trim(),
      page: this.page(),
      pageSize: this.pageSize,
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => {
        this.organizations.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: (err) => {
        this.organizations.set([]);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('admin.organizations.errors.load'));
        }
        this.loading.set(false);
      },
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.page.set(next);
    this.load();
  }
}
