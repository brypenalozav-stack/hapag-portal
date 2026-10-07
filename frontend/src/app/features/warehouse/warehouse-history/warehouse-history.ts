import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { WarehouseChangeService } from '../../../core/services/warehouse-change.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PagedResult } from '../../../core/models/admin-user.model';
import { WarehouseChangeHistoryItem } from '../../../core/models/warehouse-change.model';
import { DATA_SOURCE_KEYS, WAREHOUSE_CHANGE_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { serviceErrorMessage } from '../../service-requests/shared/service-text';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

const PAGE_SIZE = 20;

/** Estados de un cambio de almacén (M3-04). */
const STATUSES = ['Pending', 'Completed', 'Cancelled'] as const;

/** Variante de .hl-badge por estado (el texto acompaña siempre al color). */
const STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Completed: 'hl-badge--confirmed',
  Cancelled: 'hl-badge--failed',
};

interface HistoryFilters {
  blNumber: string;
  status: string;
  from: string;
  to: string;
}

function emptyFilters(): HistoryFilters {
  return { blNumber: '', status: '', from: '', to: '' };
}

/**
 * Historial de cambios de almacén (M3-06; CL-IMP-12, BO-IMP-12): solicitudes de la organización, individuales y de
 * solicitudes masivas, con fecha y hora en el huso del país, estado, embarque y, de quien pagó, la razón social y el
 * RUT o NIT, distinto del RUT de facturación. Filtros por BL o booking, estado y fecha local de la solicitud; cada fila
 * abre la trazabilidad completa.
 */
@Component({
  selector: 'app-warehouse-history',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, PaginatorComponent],
  templateUrl: './warehouse-history.html',
  styles: [':host { display: block; }'],
})
export class WarehouseHistoryComponent implements OnInit {
  private readonly service = inject(WarehouseChangeService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** BL o booking precargado (?bl=). */
  bl = input<string>();

  readonly statuses = STATUSES;
  readonly statusKeys = WAREHOUSE_CHANGE_STATUS_KEYS;
  readonly sourceKeys = DATA_SOURCE_KEYS;

  filters: HistoryFilters = emptyFilters();
  page = signal(1);
  pageSize = signal(PAGE_SIZE);
  /** Con el tamaño por defecto y una sola página se muestra solo el total. */
  readonly defaultPageSize = PAGE_SIZE;
  result = signal<PagedResult<WarehouseChangeHistoryItem> | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  filterError = signal('');

  totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.filters.blNumber = this.bl() ?? '';
    this.search();
  }

  search(page = 1): void {
    const f = this.filters;
    if (f.from && f.to && f.to < f.from) {
      this.filterError.set('warehouse.history.filters.rangeError');
      this.announcer.announce(translate('warehouse.history.filters.rangeError'), 'assertive');
      return;
    }
    this.filterError.set('');
    this.page.set(page);
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getHistory({ ...f, page, pageSize: this.pageSize() }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(serviceErrorMessage(err, 'warehouse.history.loadError'));
      },
    });
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.search(1);
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  statusClass(item: WarehouseChangeHistoryItem): string {
    return STATUS_CLASS[item.status] ?? 'hl-badge--processing';
  }
}
