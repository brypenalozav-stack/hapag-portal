import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { PagedResult } from '../../../core/models/admin-user.model';
import { SERVICE_REQUEST_STATUSES, ServiceRequestSummary } from '../../../core/models/service-request.model';
import {
  SERVICE_DEFINITION_KEYS,
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { localized, serviceErrorMessage } from '../shared/service-text';

const PAGE_SIZE = 20;

interface RequestFilters {
  status: string;
  definitionCode: string;
  blNumber: string;
}

function emptyFilters(): RequestFilters {
  return { status: '', definitionCode: '', blNumber: '' };
}

/**
 * Mis solicitudes de servicios on demand (M3-07 a M3-15): las de la organización como solicitante o mandante, con su
 * estado, embarque, servicio y monto; filtros por estado, servicio y BL o booking. Cada fila lleva al detalle con la
 * línea de tiempo (M3-12, M3-13: la solicitud queda registrada y es consultable).
 */
@Component({
  selector: 'app-service-request-list',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './service-request-list.html',
  styles: [':host { display: block; }'],
})
export class ServiceRequestListComponent implements OnInit {
  private readonly service = inject(ServiceRequestService);
  private readonly locale = inject(LocaleService);
  readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** BL o booking precargado (?bl=). */
  bl = input<string>();

  readonly statuses = SERVICE_REQUEST_STATUSES;
  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly definitionKeys = SERVICE_DEFINITION_KEYS;
  readonly definitionCodes = Object.keys(SERVICE_DEFINITION_KEYS);

  filters: RequestFilters = emptyFilters();
  page = signal(1);
  result = signal<PagedResult<ServiceRequestSummary> | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.filters.blNumber = this.bl() ?? '';
    this.search();
  }

  search(page = 1): void {
    this.page.set(page);
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.list({ ...this.filters, page, pageSize: PAGE_SIZE }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(serviceErrorMessage(err, 'serviceRequests.list.loadError'));
      },
    });
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  name(item: ServiceRequestSummary): string {
    return localized(this.locale.lang(), item.nameEs, item.nameEn);
  }

  statusClass(item: ServiceRequestSummary): string {
    return SERVICE_REQUEST_STATUS_CLASS[item.status] ?? 'hl-badge--processing';
  }
}
