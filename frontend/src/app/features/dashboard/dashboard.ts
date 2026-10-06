import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { DocumentService } from '../../core/services/document.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { ShipmentOperationService } from '../../core/services/shipment-operation.service';
import { Dashboard, DashboardDocument, DashboardPayable, DashboardShipment, DashboardTarget } from '../../core/models/dashboard.model';
import { ShipmentOperationFilter } from '../../core/models/shipment.model';
import {
  CHARGE_CONCEPT_KEYS,
  DASHBOARD_PAYABLE_STATUS_KEYS,
  DASHBOARD_REQUEST_KIND_KEYS,
  DASHBOARD_REQUEST_STATUS_CLASS,
  DASHBOARD_REQUEST_STATUS_KEYS,
  DEMURRAGE_STATE_KEYS,
  DOCUMENT_ERRORS,
  PAYABLE_ITEM_TYPE_KEYS,
  SHIPMENT_DOCUMENT_TYPE_KEYS,
  SHIPMENT_OPERATION_KEYS,
  SHIPMENT_STATUS_KEYS,
} from '../../core/i18n/labels';
import { apiErrorKey } from '../../core/http/api-error';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { AnnouncementsBannerComponent } from '../../shared/components/announcements-banner/announcements-banner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { AddToCartDialogComponent, AddToCartTarget } from '../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { DisputeLinkComponent } from '../../shared/components/dispute-link/dispute-link';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../shared/pipes/hl-number.pipe';
import { saveBlob } from '../../shared/save-blob';

/** Acceso rápido a un servicio, nombrado por lo que el cliente quiere hacer (M1-01). */
interface QuickService {
  route: string;
  titleKey: string;
  descriptionKey: string;
  /** Trazado del ícono (Bootstrap Icons, 16×16). */
  icon: string;
}

/** Bloque de próximos arribos o zarpes. */
interface ShipmentBlock {
  id: string;
  titleKey: string;
  captionKey: string;
  dateKey: string;
  emptyKey: string;
  items: DashboardShipment[];
}

/** Fila del gráfico de embarques por estado: la barra es decorativa, el número está en la tabla. */
interface StatusBar {
  key: string;
  count: number;
  percent: number;
}

const ICON = {
  pay: 'M0 3a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v1h14V3a1 1 0 0 0-1-1zm13 4H1v7a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1z',
  ship: 'M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z',
  invoice: 'M4 6.5a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5m0 2a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z',
  warehouse: 'M8.186 1.113a.5.5 0 0 0-.372 0L1.846 3.5l2.404.961L10.404 2zm3.564 1.426L5.596 5 8 5.961 14.154 3.5zm3.25 1.7-6.5 2.6v7.922l6.5-2.6V4.24zM7.5 14.762V6.838L1 4.239v7.923z',
  clock: 'M8 3.5a.5.5 0 0 0-1 0V9a.5.5 0 0 0 .252.434l3.5 2a.5.5 0 0 0 .496-.868L8 8.71zM8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16m7-8A7 7 0 1 1 1 8a7 7 0 0 1 14 0',
  check: 'M10.854 7.146a.5.5 0 0 1 0 .708l-3 3a.5.5 0 0 1-.708 0l-1.5-1.5a.5.5 0 1 1 .708-.708L7.5 9.793l2.646-2.647a.5.5 0 0 1 .708 0M14 14V4.5L9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2',
  hazard: 'M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5m.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2',
  request: 'M5 10.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5M3 0h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2m0 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z',
  help: 'M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14m0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16M5.255 5.786a.237.237 0 0 0 .241.247h.825c.138 0 .248-.113.266-.25.09-.656.54-1.134 1.342-1.134.686 0 1.314.343 1.314 1.168 0 .635-.374.927-.965 1.371-.673.489-1.206 1.06-1.168 1.987l.003.217a.25.25 0 0 0 .25.246h.811a.25.25 0 0 0 .25-.25v-.105c0-.718.273-.927 1.01-1.486.609-.463 1.244-.977 1.244-2.056 0-1.511-1.276-2.241-2.673-2.241-1.267 0-2.655.59-2.75 2.286m1.557 5.763c0 .533.425.927 1.01.927.609 0 1.028-.394 1.028-.927 0-.552-.42-.94-1.029-.94-.584 0-1.009.388-1.009.94',
} as const;

/**
 * Dashboard consolidado del cliente (M1-05): en una sola vista muestra las gestiones en curso y su estado, los
 * servicios pendientes de pago con acceso directo (agregar al carro o, para clientes con crédito, pagar desde la
 * cuenta, M5-07), los documentos recientes para descargar y los indicadores operativos de sus embarques (por
 * estado y operación, próximos arribos y zarpes, demurrage en riesgo). Todo sale de GET /dashboard, que respeta
 * los accesos del usuario (M1-11, NF-05); se filtra por operación (M2-07, misma selección que el listado de
 * embarques) y país (M1-04). Los accesos rápidos nombran los servicios por lo que el cliente quiere hacer, sin
 * nombres técnicos (M1-01), e incluyen el enlace al módulo de Dispute (M2-05). Fase 2, Ola G: acceso rápido para
 * solicitar un servicio on demand y, si el servidor las informa, las solicitudes de servicio entre las gestiones.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent, AddToCartDialogComponent, DisputeLinkComponent,
    AnnouncementsBannerComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  private readonly service = inject(DashboardService);
  private readonly documents = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly operationService = inject(ShipmentOperationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly itemTypeKeys = PAYABLE_ITEM_TYPE_KEYS;
  readonly payableStatusKeys = DASHBOARD_PAYABLE_STATUS_KEYS;
  readonly requestKindKeys = DASHBOARD_REQUEST_KIND_KEYS;
  readonly requestStatusKeys = DASHBOARD_REQUEST_STATUS_KEYS;
  readonly requestStatusClass = DASHBOARD_REQUEST_STATUS_CLASS;
  readonly documentTypeKeys = SHIPMENT_DOCUMENT_TYPE_KEYS;
  readonly demurrageStateKeys = DEMURRAGE_STATE_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly statusKeys = SHIPMENT_STATUS_KEYS;

  readonly operations: { value: ShipmentOperationFilter; key: string }[] = [
    { value: '', key: 'dashboard.filters.operation.all' },
    { value: 'IMPORT', key: 'dashboard.filters.operation.import' },
    { value: 'EXPORT', key: 'dashboard.filters.operation.export' },
  ];

  readonly operation = this.operationService.operation;
  country: 'CL' | 'BO' | '' = '';

  dashboard = signal<Dashboard | null>(null);
  loading = signal(true);
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);
  error = signal('');
  downloadError = signal('');
  downloading = signal<string | null>(null);
  addTargets = signal<AddToCartTarget[] | null>(null);

  /** La organización opera en más de un país: se ofrece el filtro por país (M1-04). */
  showCountryFilter = computed(() => this.auth.isInternal() || this.auth.operatingCountries().length > 1);

  /** Servicios del portal nombrados por lo que el cliente quiere hacer (M1-01). */
  quickServices = computed<QuickService[]>(() => {
    const list: QuickService[] = [];
    if (this.cart.accountPaymentsEnabled()) {
      list.push({ route: '/account-payments', titleKey: 'dashboard.services.accountPayments.title', descriptionKey: 'dashboard.services.accountPayments.description', icon: ICON.pay });
    } else if (this.cart.cartEnabled()) {
      list.push({ route: '/cart', titleKey: 'dashboard.services.pay.title', descriptionKey: 'dashboard.services.pay.description', icon: ICON.pay });
    }
    list.push(
      { route: '/shipments', titleKey: 'dashboard.services.shipments.title', descriptionKey: 'dashboard.services.shipments.description', icon: ICON.ship },
    );
    // Fase 2, Ola G: servicios on demand (M2-03, M2-04); los pide la organización cliente, no el administrador interno.
    if (!this.auth.isInternal()) {
      list.push({ route: '/service-requests/new', titleKey: 'dashboard.services.requestService.title', descriptionKey: 'dashboard.services.requestService.description', icon: ICON.request });
    }
    list.push(
      { route: '/invoices', titleKey: 'dashboard.services.invoices.title', descriptionKey: 'dashboard.services.invoices.description', icon: ICON.invoice },
    );
    // Fase 2, Ola H: estado de cuenta de la organización (M7-03).
    if (!this.auth.isInternal()) {
      list.push({ route: '/account-statement', titleKey: 'dashboard.services.accountStatement.title', descriptionKey: 'dashboard.services.accountStatement.description', icon: ICON.pay });
    }
    list.push(
      { route: '/warehouse', titleKey: 'dashboard.services.warehouse.title', descriptionKey: 'dashboard.services.warehouse.description', icon: ICON.warehouse },
      { route: '/demurrage', titleKey: 'dashboard.services.demurrage.title', descriptionKey: 'dashboard.services.demurrage.description', icon: ICON.clock },
      { route: '/tatc', titleKey: 'dashboard.services.tatc.title', descriptionKey: 'dashboard.services.tatc.description', icon: ICON.check },
      { route: '/dangerous-goods', titleKey: 'dashboard.services.dangerousGoods.title', descriptionKey: 'dashboard.services.dangerousGoods.description', icon: ICON.hazard },
      { route: '/faq', titleKey: 'dashboard.services.faq.title', descriptionKey: 'dashboard.services.faq.description', icon: ICON.help },
    );
    return list;
  });

  /** Embarques por estado: la barra es proporcional al estado con más embarques. */
  statusBars = computed<StatusBar[]>(() => {
    const byStatus = this.dashboard()?.indicators.byStatus ?? [];
    const max = Math.max(1, ...byStatus.map((s) => s.count));
    return [...byStatus]
      .sort((a, b) => b.count - a.count)
      .map((s) => ({ key: s.key, count: s.count, percent: Math.round((s.count * 100) / max) }));
  });

  /** Próximos arribos (ETA) y zarpes (ETD) en los próximos 14 días. */
  shipmentBlocks = computed<ShipmentBlock[]>(() => {
    const indicators = this.dashboard()?.indicators;
    return [
      {
        id: 'arrivals',
        titleKey: 'dashboard.indicators.arrivals.title',
        captionKey: 'dashboard.indicators.arrivals.caption',
        dateKey: 'dashboard.indicators.arrivals.col.date',
        emptyKey: 'dashboard.indicators.arrivals.empty',
        items: indicators?.upcomingArrivals ?? [],
      },
      {
        id: 'departures',
        titleKey: 'dashboard.indicators.departures.title',
        captionKey: 'dashboard.indicators.departures.caption',
        dateKey: 'dashboard.indicators.departures.col.date',
        emptyKey: 'dashboard.indicators.departures.empty',
        items: indicators?.upcomingDepartures ?? [],
      },
    ];
  });

  /** Embarques de importación y de exportación (M2-07). */
  importCount = computed(() => this.operationCount('IMPORT'));
  exportCount = computed(() => this.operationCount('EXPORT'));

  private operationCount(key: 'IMPORT' | 'EXPORT'): number {
    return this.dashboard()?.indicators.byOperation.find((o) => o.key === key)?.count ?? 0;
  }

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.get({ operation: this.operation(), country: this.country }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: (err) => {
        this.dashboard.set(null);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('dashboard.loadError'));
        }
        this.loading.set(false);
      },
    });
  }

  /** Importación / exportación (M2-07): la selección se comparte con el listado de embarques. */
  setOperation(operation: ShipmentOperationFilter): void {
    if (operation === this.operation()) return;
    this.operationService.set(operation);
    this.load();
  }

  onCountry(): void {
    this.load();
  }

  /** Ruta de la pantalla donde se gestiona el elemento. */
  route(target: DashboardTarget): string[] {
    const bl = target.blNumber ?? '';
    switch (target.kind) {
      case 'Shipment':
        return bl ? ['/shipments', bl] : ['/shipments'];
      case 'Charges':
        return bl ? ['/charges', bl] : ['/charges'];
      case 'Demurrage':
        return bl ? ['/demurrage', bl] : ['/demurrage'];
      case 'Invoice':
        return ['/invoices'];
      case 'Documents':
      case 'BlCopy':
      case 'ResponsibilityLetter':
        return bl ? ['/shipments', bl, 'documents'] : ['/shipments'];
      case 'WarehouseChangeBatch':
        return target.id ? ['/warehouse/bulk', target.id] : ['/warehouse'];
      case 'WarehouseChange':
        return ['/warehouse'];
      case 'ServiceOrder':
        return ['/service-orders'];
      case 'TatcBatch':
        return ['/tatc'];
      case 'ServiceRequest':
        return target.id ? ['/service-requests', target.id] : ['/service-requests'];
      default:
        return ['/dashboard'];
    }
  }

  /** La solicitud masiva de TATC se abre con su resultado (M2-09). */
  queryParams(target: DashboardTarget): Record<string, string> | null {
    return target.kind === 'TatcBatch' && target.id ? { batch: target.id } : null;
  }

  conceptLabel(item: DashboardPayable): string {
    const key = CHARGE_CONCEPT_KEYS[item.conceptCode];
    return key ? translate(key) : (item.description ?? item.conceptCode);
  }

  inCart(item: DashboardPayable): boolean {
    return item.inCart || this.cart.contains(item.itemType, item.sourceId);
  }

  /** Agrega el pendiente al carro (M5-01): el diálogo valida RUT de facturación y moneda en el servidor. */
  addToCart(item: DashboardPayable): void {
    const concept = this.conceptLabel(item);
    const label = item.blNumber
      ? translate('dashboard.pending.cartLabel', { concept, bl: item.blNumber })
      : concept;
    this.addTargets.set([{ itemType: item.itemType, sourceId: item.sourceId, label }]);
  }

  onAddClosed(added: boolean): void {
    this.addTargets.set(null);
    if (added) this.cart.refresh();
  }

  /** Descarga el documento con la sesión del usuario (M6-09); el servidor registra la descarga (NF-14). */
  download(doc: DashboardDocument): void {
    if (this.downloading()) return;
    this.downloading.set(doc.id);
    this.downloadError.set('');
    this.documents.downloadRelated(doc.downloadPath).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, `${doc.documentNumber}.pdf`);
        this.announcer.announce(translate('dashboard.documents.downloaded', { number: doc.documentNumber }));
      },
      error: (err) => {
        this.downloading.set(null);
        const message = translate(apiErrorKey(err, DOCUMENT_ERRORS, 'dashboard.documents.downloadError'));
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
