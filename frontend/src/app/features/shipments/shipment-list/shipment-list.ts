import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { ShipmentOperationService } from '../../../core/services/shipment-operation.service';
import { ShipmentListItem, ShipmentOperationFilter } from '../../../core/models/shipment.model';
import { SHIPMENT_OPERATION_KEYS } from '../../../core/i18n/labels';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';

/** Filtros de texto del listado (M2-06), reflejados en los query params. */
const TEXT_FILTERS = ['blNumber', 'bookingNumber', 'vessel', 'voyage', 'status'] as const;

/** Valor del query param `operation` cuando el usuario elige ver ambas operaciones. */
const ALL_OPERATIONS = 'ALL';

/**
 * Listado único de embarques (M2-06): todos los BL y bookings accesibles por el usuario, con
 * filtros por BL, booking, nave, viaje, estado y país, y separación importación/exportación
 * (M2-07) que se mantiene durante la navegación (ShipmentOperationService y query params).
 */
@Component({
  selector: 'app-shipment-list',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, TranslocoPipe, CodeLabelPipe,
    StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './shipment-list.html',
  styles: [':host { display: block; }'],
})
export class ShipmentListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ShipmentService);
  private readonly operationService = inject(ShipmentOperationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = 20;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly operations: { value: ShipmentOperationFilter; key: string }[] = [
    { value: '', key: 'shipments.list.operation.all' },
    { value: 'IMPORT', key: 'shipments.list.operation.import' },
    { value: 'EXPORT', key: 'shipments.list.operation.export' },
  ];

  readonly operation = this.operationService.operation;

  filters = this.fb.nonNullable.group({
    blNumber: [''],
    bookingNumber: [''],
    vessel: [''],
    voyage: [''],
    status: [''],
    country: ['' as 'CL' | 'BO' | ''],
  });

  shipments = signal<ShipmentListItem[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(true);
  error = signal('');
  /** NF-11: la última consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.applyParams(params);
      this.load();
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  load(): void {
    const raw = this.filters.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.search({
      ...raw,
      operation: this.operation(),
      page: this.page(),
      pageSize: this.pageSize,
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => {
        this.shipments.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: (err) => {
        this.shipments.set([]);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('shipments.list.loadError'));
        }
        this.loading.set(false);
      },
    });
  }

  search(): void {
    this.navigate(1);
  }

  clearFilters(): void {
    this.filters.reset();
    this.navigate(1);
  }

  /** Importación / exportación (M2-07): la selección se guarda para el resto de la navegación. */
  setOperation(operation: ShipmentOperationFilter): void {
    if (operation === this.operation()) return;
    this.operationService.set(operation);
    this.navigate(1);
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.navigate(next);
  }

  private navigate(page: number): void {
    const raw = this.filters.getRawValue();
    const queryParams: Record<string, string | number | null> = {
      operation: this.operation() || ALL_OPERATIONS,
      page: page > 1 ? page : null,
      country: raw.country || null,
    };
    for (const key of TEXT_FILTERS) {
      queryParams[key] = raw[key].trim() || null;
    }
    this.router.navigate([], { relativeTo: this.route, queryParams });
  }

  /** Lee filtros, página y operación de la URL; sin `operation`, aplica la selección guardada. */
  private applyParams(params: ParamMap): void {
    const operation = params.get('operation');
    if (operation === 'IMPORT' || operation === 'EXPORT') {
      this.operationService.set(operation);
    } else if (operation === ALL_OPERATIONS) {
      this.operationService.set('');
    }

    const country = params.get('country');
    this.filters.setValue({
      blNumber: params.get('blNumber') ?? '',
      bookingNumber: params.get('bookingNumber') ?? '',
      vessel: params.get('vessel') ?? '',
      voyage: params.get('voyage') ?? '',
      status: params.get('status') ?? '',
      country: country === 'CL' || country === 'BO' ? country : '',
    });

    const page = Number(params.get('page'));
    this.page.set(Number.isInteger(page) && page > 0 ? page : 1);
  }
}
