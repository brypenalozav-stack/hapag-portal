import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { ShipmentOperationService } from '../../../core/services/shipment-operation.service';
import { ShipmentListItem, ShipmentOperationFilter } from '../../../core/models/shipment.model';
import {
  BL_ISSUANCE_STATUS_CLASS,
  BL_ISSUANCE_STATUS_KEYS,
  PUBLICATION_REASON_KEYS,
  SHIPMENT_OPERATION_KEYS,
  TRANSPORT_DOCUMENT_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { AuthService } from '../../../core/services/auth.service';
import { filter, switchMap } from 'rxjs';
import { FeatureService } from '../../../core/services/feature.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { AccessSourceBadgeComponent } from '../../../shared/components/access-source-badge/access-source-badge';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { ParentLinkOrganization } from '../../../core/models/organization-network.model';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';
import { SortHeaderComponent, SortState, sortParams } from '../../../shared/components/sort-header/sort-header';
import { TableSkeletonComponent } from '../../../shared/components/table-skeleton/table-skeleton';

/** Filtros de texto del listado (M2-06), reflejados en los query params. */
const TEXT_FILTERS = ['blNumber', 'bookingNumber', 'vessel', 'voyage', 'status'] as const;

/** Columnas ordenables del listado (lista blanca del servidor); el orden viaja en los query params `sort` y `dir`. */
const SORTABLE_COLUMNS = ['blNumber', 'bookingNumber', 'vessel', 'voyage', 'status', 'operation', 'country'];

/** Valor del query param `operation` cuando el usuario elige ver ambas operaciones. */
const ALL_OPERATIONS = 'ALL';

/** Filas por página por defecto; otro tamaño viaja en el query param `pageSize`. */
const DEFAULT_PAGE_SIZE = 20;

/**
 * Listado único de embarques (M2-06): todos los BL y bookings accesibles por el usuario, con
 * filtros por BL, booking, nave, viaje, estado y país, y separación importación/exportación
 * (M2-07) que se mantiene durante la navegación (ShipmentOperationService y query params).
 * Incluye los BL recibidos por acceso y los autoasociados, con el origen del acceso, y la búsqueda
 * por número exacto, que abre el detalle y permite ver un BL con acceso abierto (M1-17).
 * Ola F: estado de emisión del documento de transporte (M2-02) y, solo para el administrador interno, la
 * publicación por DIFU con su motivo y el filtro de publicados / no publicados (M2-01).
 * Fase 2, Ola I: la empresa matriz ve también los BL de sus filiales con la columna de organización de origen y un
 * filtro por organización, para revisar una filial a la vez sin mezclar la información (M1-21).
 */
@Component({
  selector: 'app-shipment-list',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, TranslocoPipe, CodeLabelPipe,
    StatusBadgeComponent, CountryBadgeComponent, StateMessageComponent,
    AccessSourceBadgeComponent, PaginatorComponent, SortHeaderComponent, TableSkeletonComponent,
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
  private readonly network = inject(OrganizationNetworkService);
  private readonly features = inject(FeatureService);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);

  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly documentTypeKeys = TRANSPORT_DOCUMENT_TYPE_KEYS;
  readonly issuanceKeys = BL_ISSUANCE_STATUS_KEYS;
  readonly issuanceClass = BL_ISSUANCE_STATUS_CLASS;
  readonly publicationReasonKeys = PUBLICATION_REASON_KEYS;
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
    /** Solo el administrador interno (M2-01): '' todos, 'true' publicados, 'false' no publicados. */
    published: ['' as '' | 'true' | 'false'],
    /** Empresa matriz (M1-21): la propia organización o una filial visible. */
    organizationId: [''],
  });

  /** Filiales que dan visibilidad a esta organización (M1-21). */
  subsidiaries = signal<ParentLinkOrganization[]>([]);
  /** Columna de organización de origen: la matriz ve BL de sus filiales. */
  showOrigin = computed(() => this.subsidiaries().length > 0 || this.shipments().some((s) => !!s.originOrganization));

  /** Número exacto de un BL para abrir su detalle, también por acceso abierto (M1-17). */
  openBl = this.fb.nonNullable.control('');
  openBlSubmitted = signal(false);

  shipments = signal<ShipmentListItem[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = signal(DEFAULT_PAGE_SIZE);
  /** Orden por columna; `null` es el orden por defecto del servidor. */
  sort = signal<SortState | null>(null);
  loading = signal(true);
  error = signal('');
  /** NF-11: la última consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  constructor() {
    this.loadSubsidiaries();
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.applyParams(params);
      this.load();
    });
  }

  /** Filiales visibles para el filtro por organización (solo organizaciones de clientes aprobadas). */
  private loadSubsidiaries(): void {
    const org = this.auth.organization();
    if (!org || org.status !== 'Approved' || (org.organizationType !== 'Customer' && org.organizationType !== 'FreightForwarder')) return;
    // Empresa matriz (M1-21, Fase 2): solo con su flag encendido.
    this.features.ready().pipe(
      filter(() => this.features.enabled('ParentCompany')),
      switchMap(() => this.network.getParentCompany()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (view) => this.subsidiaries.set((view.subsidiaries ?? []).filter((s) => s.visibilityEnabled).map((s) => s.organization)),
      error: () => this.subsidiaries.set([]),
    });
  }

  load(): void {
    const raw = this.filters.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.search({
      ...raw,
      published: this.auth.isInternal() && raw.published ? raw.published === 'true' : '',
      organizationId: raw.organizationId,
      operation: this.operation(),
      page: this.page(),
      pageSize: this.pageSize(),
      ...sortParams(this.sort()),
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

  /** Abre el detalle del BL ingresado; el servidor valida el acceso (propio, otorgado o abierto). */
  openByNumber(event: Event): void {
    event.preventDefault();
    this.openBlSubmitted.set(true);
    const blNumber = this.openBl.value.trim().toUpperCase();
    if (!blNumber) return;
    this.router.navigate(['/shipments', blNumber]);
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

  changePage(page: number): void {
    this.navigate(page);
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.navigate(1, size);
  }

  /** Otro orden vuelve a la primera página. */
  onSort(sort: SortState | null): void {
    this.navigate(1, this.pageSize(), sort);
  }

  private navigate(page: number, pageSize = this.pageSize(), sort = this.sort()): void {
    const raw = this.filters.getRawValue();
    const queryParams: Record<string, string | number | null> = {
      operation: this.operation() || ALL_OPERATIONS,
      page: page > 1 ? page : null,
      pageSize: pageSize !== DEFAULT_PAGE_SIZE ? pageSize : null,
      sort: sort?.column ?? null,
      dir: sort?.direction ?? null,
      country: raw.country || null,
      published: this.auth.isInternal() && raw.published ? raw.published : null,
      organizationId: raw.organizationId || null,
    };
    for (const key of TEXT_FILTERS) {
      queryParams[key] = raw[key].trim() || null;
    }
    this.router.navigate([], { relativeTo: this.route, queryParams });
  }

  /** Lee filtros, página, orden y operación de la URL; sin `operation`, aplica la selección guardada. */
  private applyParams(params: ParamMap): void {
    const operation = params.get('operation');
    if (operation === 'IMPORT' || operation === 'EXPORT') {
      this.operationService.set(operation);
    } else if (operation === ALL_OPERATIONS) {
      this.operationService.set('');
    }

    const country = params.get('country');
    const published = params.get('published');
    this.filters.setValue({
      blNumber: params.get('blNumber') ?? '',
      bookingNumber: params.get('bookingNumber') ?? '',
      vessel: params.get('vessel') ?? '',
      voyage: params.get('voyage') ?? '',
      status: params.get('status') ?? '',
      country: country === 'CL' || country === 'BO' ? country : '',
      published: published === 'true' || published === 'false' ? published : '',
      organizationId: params.get('organizationId') ?? '',
    });

    const page = Number(params.get('page'));
    this.page.set(Number.isInteger(page) && page > 0 ? page : 1);
    const pageSize = Number(params.get('pageSize'));
    this.pageSize.set(Number.isInteger(pageSize) && pageSize > 0 && pageSize <= 100 ? pageSize : DEFAULT_PAGE_SIZE);
    const sort = params.get('sort');
    const dir = params.get('dir');
    this.sort.set(sort && SORTABLE_COLUMNS.includes(sort) ? { column: sort, direction: dir === 'desc' ? 'desc' : 'asc' } : null);
  }
}
