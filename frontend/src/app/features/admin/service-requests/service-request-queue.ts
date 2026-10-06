import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AdminServiceRequestService, ServiceRequestService } from '../../../core/services/service-request.service';
import { LocaleService } from '../../../core/services/locale.service';
import { PagedResult } from '../../../core/models/admin-user.model';
import { SERVICE_REQUEST_STATUSES, ServiceDefinitionOption, ServiceRequestSummary } from '../../../core/models/service-request.model';
import {
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
  SERVICE_TEAM_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { localized, serviceErrorMessage } from '../../service-requests/shared/service-text';

const PAGE_SIZE = 20;

interface QueueFilters {
  status: string;
  team: string;
  definitionCode: string;
  country: string;
  blNumber: string;
}

function emptyFilters(): QueueFilters {
  return { status: '', team: '', definitionCode: '', country: '', blNumber: '' };
}

/**
 * Bandeja interna de solicitudes de servicios on demand (permiso `service-requests.process`): por defecto, lo que hay
 * que atender (pendiente de aprobación del equipo ED y en curso con Customer Service), del cambio de estado más
 * antiguo al más reciente; filtros por estado, equipo, servicio, país y BL o booking. Los borradores no se listan.
 * Fase 2, Ola H: los servicios del filtro son las definiciones activas que informa el servidor.
 */
@Component({
  selector: 'app-service-request-queue',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './service-request-queue.html',
  styles: [':host { display: block; }'],
})
export class ServiceRequestQueueComponent implements OnInit {
  private readonly service = inject(AdminServiceRequestService);
  private readonly clientService = inject(ServiceRequestService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statuses = SERVICE_REQUEST_STATUSES.filter((s) => s !== 'Draft');
  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly teams = ['ED', 'CustomerService'];
  /** Servicios del filtro (GET /service-requests/definitions); si no responde, el filtro queda sin opciones. */
  definitions = signal<ServiceDefinitionOption[]>([]);

  filters: QueueFilters = emptyFilters();
  page = signal(1);
  result = signal<PagedResult<ServiceRequestSummary> | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  /** Sin estado, BL ni organización, la bandeja muestra solo lo que hay que atender. */
  actionableOnly = signal(true);

  totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.clientService.definitions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (definitions) => this.definitions.set(definitions),
      error: () => this.definitions.set([]),
    });
    this.search();
  }

  search(page = 1): void {
    this.page.set(page);
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.actionableOnly.set(!this.filters.status && !this.filters.blNumber.trim());
    this.service.list({ ...this.filters, page, pageSize: PAGE_SIZE }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(serviceErrorMessage(err, 'admin.serviceRequests.queue.loadError'));
      },
    });
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  definitionName(definition: ServiceDefinitionOption): string {
    return localized(this.locale.lang(), definition.nameEs, definition.nameEn);
  }

  name(item: ServiceRequestSummary): string {
    return localized(this.locale.lang(), item.nameEs, item.nameEn);
  }

  statusClass(item: ServiceRequestSummary): string {
    return SERVICE_REQUEST_STATUS_CLASS[item.status] ?? 'hl-badge--processing';
  }
}
