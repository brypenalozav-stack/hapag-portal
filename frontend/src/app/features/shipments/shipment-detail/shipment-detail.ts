import { Component, DestroyRef, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { AuthService } from '../../../core/services/auth.service';
import { FeatureService } from '../../../core/services/feature.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { CartService } from '../../../core/services/cart.service';
import { PERMISSIONS } from '../../../core/constants/app.constants';
import { apiErrorKey } from '../../../core/http/api-error';
import { SHIPMENT_ACTIONS, ShipmentDetail } from '../../../core/models/shipment.model';
import {
  COUNTER_SYNC_STATUS_CLASS,
  COUNTER_SYNC_STATUS_KEYS,
  PUBLICATION_REASON_KEYS,
  SHIPMENT_OPERATION_KEYS,
  SHIPMENT_ROLE_KEYS,
} from '../../../core/i18n/labels';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { AccessSourceBadgeComponent } from '../../../shared/components/access-source-badge/access-source-badge';
import { ShipmentAccessComponent } from '../../access/shipment-access/shipment-access';
import { GRANT_ERRORS } from '../../access/shared/access-errors';
import { ChargesPanelComponent } from '../../charges/charges-panel/charges-panel';
import { AddToCartDialogComponent, AddToCartTarget } from '../../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { ShipmentDocumentsComponent } from '../../documents/shipment-documents/shipment-documents';
import { ShipmentIssuanceComponent } from '../shipment-issuance/shipment-issuance';
import { ShipmentTatcComponent } from '../shipment-tatc/shipment-tatc';
import { AvailableServicesComponent } from '../../service-requests/available-services/available-services';
import { SectionNavComponent } from '../../../shared/components/section-nav/section-nav';

/** Orígenes del acceso con los que se muestra la sección "Accesos" del BL. */
const ACCESS_SECTION_SOURCES = ['Own', 'Grant', 'SelfAssociated'];

/**
 * Detalle del embarque (M2-06) según los permisos del usuario (M1-11). Los bloques que la matriz
 * no habilita llegan en null y no se muestran; los botones de pagar y solicitar aparecen solo si
 * la acción está en `allowedActions` y el perfil puede operar (`canOperate`). En exportación
 * muestra las órdenes de servicio generadas (CL-EXP-13, BO-EXP-09). Ola B: origen del acceso,
 * autoasociación a un BL visto por acceso abierto (M1-18) con el aviso previo al pago, y la
 * sección "Accesos" del BL (M1-12, M1-16). Ola C: los cargos locales se muestran con las reglas de
 * Nexus aplicadas (exenciones, IPO por crédito, carta FFWW) y la acción que corresponde a cada uno.
 * Ola D: el flete pendiente se paga desde el carro (M5-01), con el RUT de facturación elegido al agregarlo.
 * Ola E: sección "Documentos" con el repositorio del embarque y las solicitudes de documentos (M6-01 a
 * M6-09); la carta de responsabilidad emitida desde los cargos o desde los documentos actualiza ambas.
 * Ola F: estado de emisión del documento de transporte (M2-02), TATC del BL de importación por contenedor
 * (M2-09) y, para el administrador interno, la publicación del BL por DIFU de destino final (M2-01).
 * Fase 2, Ola G: sección "Servicios disponibles" con los servicios on demand del BL o booking que el usuario puede ver
 * (M2-03, M2-04), la estimación del cobro y el acceso a la solicitud; a pedido, los no disponibles con el motivo.
 * Fase 2, Ola I: bloque Counter (canje, HBL y desconsolidado, M8-09) para perfiles internos y, para la empresa matriz, la
 * organización de origen del BL de una filial, en solo consulta (M1-21).
 */
@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [
    SectionNavComponent, RouterLink, TranslocoPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe, CodeLabelPipe,
    StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent,
    AccessSourceBadgeComponent, ShipmentAccessComponent, ChargesPanelComponent, AddToCartDialogComponent,
    ShipmentDocumentsComponent, ShipmentIssuanceComponent, ShipmentTatcComponent, AvailableServicesComponent,
  ],
  templateUrl: './shipment-detail.html',
  styleUrl: './shipment-detail.scss',
})
export class ShipmentDetailComponent implements OnInit {
  private readonly service = inject(ShipmentService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  readonly cart = inject(CartService);
  /** Flags de funcionalidades: secciones de Fase 2 del detalle (cierre de Fase 1). */
  readonly features = inject(FeatureService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();

  readonly roleKeys = SHIPMENT_ROLE_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly publicationReasonKeys = PUBLICATION_REASON_KEYS;
  readonly counterSyncKeys = COUNTER_SYNC_STATUS_KEYS;
  readonly counterSyncClass = COUNTER_SYNC_STATUS_CLASS;

  shipment = signal<ShipmentDetail | null>(null);
  loading = signal(true);
  error = signal('');
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  addTargets = signal<AddToCartTarget[] | null>(null);

  private readonly chargesPanel = viewChild(ChargesPanelComponent);
  private readonly documentsSection = viewChild(ShipmentDocumentsComponent);
  associating = signal(false);
  associateError = signal('');

  /** Gestionar accesos del BL: permiso org.access.manage del perfil (el servidor valida). */
  canManageAccess = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_THIRD_PARTY_ACCESS));

  /** Sección "Accesos": BL propio, otorgado o autoasociado de una organización aprobada (no el interno). */
  showAccessSection = computed(() => {
    const s = this.shipment();
    const org = this.auth.organization();
    return !!s && !!org && org.status === 'Approved' && org.organizationType !== 'Internal'
      && ACCESS_SECTION_SOURCES.includes(s.accessSource);
  });

  private readonly allowed = computed(() => {
    const s = this.shipment();
    return new Set(s?.canOperate ? s.allowedActions : []);
  });

  /** Pagar flete: la matriz lo permite para el rol y el perfil del usuario opera. */
  canPayFreight = computed(() => this.allowed().has(SHIPMENT_ACTIONS.PAY_FREIGHT));
  canPayDemurrage = computed(() => this.allowed().has(SHIPMENT_ACTIONS.PAY_IMPORT_DEMURRAGE));
  canRequestWarehouseChange = computed(() => this.allowed().has(SHIPMENT_ACTIONS.REQUEST_WAREHOUSE_CHANGE));

  /** TATC del BL (M2-09): solo importación y con la acción `tatc.download` de la matriz (M1-11). */
  showTatc = computed(() => {
    const s = this.shipment();
    return !!s && s.operation === 'IMPORT' && s.allowedActions.includes(SHIPMENT_ACTIONS.DOWNLOAD_TATC);
  });

  /** Publicación por DIFU (M2-01): solo la ve el administrador interno. */
  showPublication = computed(() => this.auth.isInternal() && !!this.shipment()?.publication);

  /** Counter Bolivia/Ultramar (Fase 2, Ola I, M8-09): el servidor lo envía solo a perfiles internos. */
  showCounter = computed(() => this.auth.isInternal() && !!this.shipment());
  canManageCounter = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_COUNTER));

  /** Las ODS se consultan en exportación (CL-EXP-13, BO-EXP-09) o si el embarque ya tiene alguna. */
  showServiceOrders = computed(() => {
    const s = this.shipment();
    return !!s && (s.operation === 'EXPORT' || s.serviceOrders.length > 0);
  });

  ngOnInit(): void {
    this.load();
  }

  /** Agrega el flete al carro (M5-01): el servidor valida el permiso, la asociación (M1-18) y la moneda. */
  addFreightToCart(): void {
    const s = this.shipment();
    if (!s) return;
    this.addTargets.set([{ itemType: 'Freight', sourceId: s.id, label: translate('shipments.detail.freight.cartLabel', { bl: s.blNumber }) }]);
  }

  onAddClosed(): void {
    this.addTargets.set(null);
  }

  /** Carta emitida desde los cargos (M4-04): el repositorio la muestra. */
  onLetterIssued(): void {
    this.documentsSection()?.load();
  }

  /** Carta emitida desde los documentos (M6-06): los cargos se vuelven a leer sin el bloqueo. */
  onDocumentsChanged(): void {
    this.chargesPanel()?.load();
  }

  /** Autoasociación (M1-18): el BL queda guardado en el listado de la organización. */
  associate(): void {
    const s = this.shipment();
    if (!s) return;
    this.associating.set(true);
    this.associateError.set('');
    this.service.associate(s.blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.associating.set(false);
        this.announcer.announce(translate('shipments.detail.associate.done', { bl: s.blNumber }));
        this.load();
      },
      error: (err) => {
        this.associating.set(false);
        const message = translate(apiErrorKey(err, GRANT_ERRORS, 'shipments.detail.associate.error'));
        this.associateError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.getByBl(this.blNumber()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (shipment) => {
        this.shipment.set(shipment);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          // Un BL ajeno responde 404, igual que uno inexistente (NF-05).
          this.error.set(translate('shipments.detail.notFound'));
        } else {
          this.error.set(translate('shipments.detail.loadError'));
        }
        this.loading.set(false);
      },
    });
  }
}
