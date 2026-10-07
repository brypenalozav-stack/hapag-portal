import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { FeatureService } from '../../core/services/feature.service';
import { DocumentService } from '../../core/services/document.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { PortalLinkService } from '../../core/services/portal-link.service';
import { ShipmentOperationService } from '../../core/services/shipment-operation.service';
import { Dashboard, DashboardDocument, DashboardPayable, DashboardTarget } from '../../core/models/dashboard.model';
import { ShipmentOperationFilter } from '../../core/models/shipment.model';
import {
  CHARGE_CONCEPT_KEYS,
  DASHBOARD_REQUEST_KIND_KEYS,
  DASHBOARD_REQUEST_STATUS_CLASS,
  DASHBOARD_REQUEST_STATUS_KEYS,
  DEMURRAGE_STATE_KEYS,
  DOCUMENT_ERRORS,
  SHIPMENT_DOCUMENT_TYPE_KEYS,
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
import { ToastService } from '../../core/services/toast.service';
import { DashboardIndicatorsComponent } from './dashboard-indicators/dashboard-indicators';
import {
  ACTION_URGENCY_CLASS,
  ACTION_URGENCY_KEYS,
  ActionGroup,
  PaymentMode,
  actionableRequests,
  buildActionGroups,
  targetQueryParams,
  targetRoute,
} from './action-items';

/** Acceso rápido a una tarea frecuente (M1-01): nombrado por lo que el cliente quiere hacer. */
interface QuickTask {
  /** `dispute`: tarjeta del enlace externo de Dispute (M2-05). */
  kind: 'route' | 'dispute';
  route: string;
  titleKey: string;
  descriptionKey: string;
  /** Trazado del ícono (Bootstrap Icons, 16×16). */
  icon: string;
}

/** Como máximo 4 accesos rápidos: son atajos a tareas, no una copia del menú. */
export const MAX_QUICK_TASKS = 4;
/** Grupos de la bandeja visibles antes de "Ver todo". */
export const ACTION_PREVIEW = 5;
/** Filas de las listas de gestiones y documentos recientes. */
const RECENT_LIMIT = 5;

const ICON = {
  search: 'M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001q.044.06.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1 1 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0',
  pay: 'M0 3a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v1h14V3a1 1 0 0 0-1-1zm13 4H1v7a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1z',
  letter: 'M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z',
  check: 'M10.854 7.146a.5.5 0 0 1 0 .708l-3 3a.5.5 0 0 1-.708 0l-1.5-1.5a.5.5 0 1 1 .708-.708L7.5 9.793l2.646-2.647a.5.5 0 0 1 .708 0M14 14V4.5L9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2',
  request: 'M5 10.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5M3 0h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2m0 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z',
} as const;

/**
 * Dashboard del cliente (M1-05), orientado a la acción (cierre de Fase 1, UX):
 * 1. "Requiere su acción": los pendientes agrupados por BL y ordenados por urgencia (vencido, urgente, pendiente), con
 *    una acción principal por grupo; los primeros {@link ACTION_PREVIEW} y "Ver todo". Vacía, dice "Todo al día".
 * 2. Tres indicadores: embarques, pendientes de pago y gestiones en curso. El demurrage solo aparece en la bandeja.
 * 3. Hasta {@link MAX_QUICK_TASKS} atajos a tareas (M1-01, con el enlace de Dispute M2-05 si cabe).
 * 4. Gestiones y documentos recientes; los indicadores operativos, a pedido y con `@defer`.
 * Todo sale de GET /dashboard, que respeta los accesos del usuario (M1-11, NF-05); se filtra por operación (M2-07,
 * misma selección que el listado de embarques) y país (M1-04). Las piezas de Fase 2 siguen detrás de sus flags.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent, AddToCartDialogComponent, DisputeLinkComponent,
    AnnouncementsBannerComponent, DashboardIndicatorsComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  readonly features = inject(FeatureService);
  private readonly links = inject(PortalLinkService);
  private readonly service = inject(DashboardService);
  private readonly documents = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly operationService = inject(ShipmentOperationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly requestKindKeys = DASHBOARD_REQUEST_KIND_KEYS;
  readonly requestStatusKeys = DASHBOARD_REQUEST_STATUS_KEYS;
  readonly requestStatusClass = DASHBOARD_REQUEST_STATUS_CLASS;
  readonly documentTypeKeys = SHIPMENT_DOCUMENT_TYPE_KEYS;
  readonly demurrageStateKeys = DEMURRAGE_STATE_KEYS;
  readonly urgencyKeys = ACTION_URGENCY_KEYS;
  readonly urgencyClass = ACTION_URGENCY_CLASS;
  readonly actionPreview = ACTION_PREVIEW;

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
  /** La bandeja muestra todos los grupos ("Ver todo"). */
  showAllActions = signal(false);
  /** Indicadores operativos desplegados (a pedido). */
  indicatorsOpen = signal(false);

  /** La organización opera en más de un país: se ofrece el filtro por país (M1-04). */
  showCountryFilter = computed(() => this.auth.isInternal() || this.auth.operatingCountries().length > 1);

  private readonly paymentMode = computed<PaymentMode>(() =>
    this.cart.accountPaymentsEnabled() ? 'account' : this.cart.cartEnabled() ? 'cart' : 'none');

  /** Bandeja "Requiere su acción": pendientes agrupados por BL y ordenados por urgencia. */
  actionGroups = computed<ActionGroup[]>(() => {
    const d = this.dashboard();
    return d ? buildActionGroups(d, this.paymentMode(), (item) => this.inCart(item)) : [];
  });

  visibleActionGroups = computed(() =>
    this.showAllActions() ? this.actionGroups() : this.actionGroups().slice(0, ACTION_PREVIEW));

  /** Gestiones recientes que no están ya en la bandeja (sin repetir la misma información). */
  recentRequests = computed(() => {
    const items = this.dashboard()?.requests.items ?? [];
    const inTray = new Set(actionableRequests(items));
    return items.filter((r) => !inTray.has(r)).slice(0, RECENT_LIMIT);
  });

  recentDocuments = computed(() => (this.dashboard()?.documents ?? []).slice(0, RECENT_LIMIT));

  /** Atajos a tareas frecuentes, como máximo {@link MAX_QUICK_TASKS} (M1-01). */
  quickTasks = computed<QuickTask[]>(() => {
    const internal = this.auth.isInternal();
    const list: QuickTask[] = [];
    list.push(internal
      ? { kind: 'route', route: '/shipments', titleKey: 'dashboard.services.findShipment.title', descriptionKey: 'dashboard.services.findShipment.description', icon: ICON.search }
      : { kind: 'route', route: '/bl-status', titleKey: 'dashboard.services.checkBl.title', descriptionKey: 'dashboard.services.checkBl.description', icon: ICON.search });
    if (this.cart.accountPaymentsEnabled()) {
      list.push({ kind: 'route', route: '/account-payments', titleKey: 'dashboard.services.accountPayments.title', descriptionKey: 'dashboard.services.accountPayments.description', icon: ICON.pay });
    } else if (this.cart.cartEnabled()) {
      list.push({ kind: 'route', route: '/cart', titleKey: 'dashboard.services.pay.title', descriptionKey: 'dashboard.services.pay.description', icon: ICON.pay });
    }
    // Fase 2, Ola G: servicios on demand (M2-03, M2-04); los pide la organización cliente, no el administrador interno.
    if (!internal && this.features.enabled('OnDemandServices')) {
      list.push({ kind: 'route', route: '/service-requests/new', titleKey: 'dashboard.services.requestService.title', descriptionKey: 'dashboard.services.requestService.description', icon: ICON.request });
    }
    // Fase 2, Ola H: estado de cuenta de la organización (M7-03).
    if (!internal && this.features.enabled('AccountStatement')) {
      list.push({ kind: 'route', route: '/account-statement', titleKey: 'dashboard.services.accountStatement.title', descriptionKey: 'dashboard.services.accountStatement.description', icon: ICON.pay });
    }
    // Carta de liberación (M6-08): importaciones de Bolivia, como en el menú.
    const bolivia = this.auth.getCountry() === 'BO' || this.auth.operatingCountries().includes('BO');
    if (!internal && bolivia && this.features.enabled('ReleaseLetter')) {
      list.push({ kind: 'route', route: '/release-letter', titleKey: 'dashboard.services.releaseLetter.title', descriptionKey: 'dashboard.services.releaseLetter.description', icon: ICON.letter });
    }
    if (this.links.disputeUrl()) {
      list.push({ kind: 'dispute', route: '', titleKey: '', descriptionKey: '', icon: '' });
    }
    if (!internal) {
      list.push({ kind: 'route', route: '/tatc', titleKey: 'dashboard.services.tatc.title', descriptionKey: 'dashboard.services.tatc.description', icon: ICON.check });
    }
    return list.slice(0, MAX_QUICK_TASKS);
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

  route(target: DashboardTarget): string[] {
    return targetRoute(target);
  }

  queryParams(target: DashboardTarget): Record<string, string> | null {
    return targetQueryParams(target);
  }

  private conceptLabel(item: DashboardPayable): string {
    const key = CHARGE_CONCEPT_KEYS[item.conceptCode];
    return key ? translate(key) : (item.description ?? item.conceptCode);
  }

  private inCart(item: DashboardPayable): boolean {
    return item.inCart || this.cart.contains(item.itemType, item.sourceId);
  }

  /** Agrega al carro los pendientes del grupo (M5-01): el diálogo valida RUT de facturación y moneda en el servidor. */
  addToCart(payables: DashboardPayable[]): void {
    this.addTargets.set(payables.map((item) => {
      const concept = this.conceptLabel(item);
      const label = item.blNumber ? translate('dashboard.action.cartLabel', { concept, bl: item.blNumber }) : concept;
      return { itemType: item.itemType, sourceId: item.sourceId, label };
    }));
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
        this.toast.success(translate('dashboard.documents.downloaded', { number: doc.documentNumber }));
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
