import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { SHIPMENT_ACTIONS, ShipmentDetail } from '../../../core/models/shipment.model';
import {
  SHIPMENT_ACCESS_SOURCE_KEYS,
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

/**
 * Detalle del embarque (M2-06) según los permisos del usuario (M1-11). Los bloques que la matriz
 * no habilita llegan en null y no se muestran; los botones de pagar y solicitar aparecen solo si
 * la acción está en `allowedActions` y el perfil puede operar (`canOperate`). En exportación
 * muestra las órdenes de servicio generadas (CL-EXP-13, BO-EXP-09).
 */
@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe, CodeLabelPipe,
    StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './shipment-detail.html',
  styleUrl: './shipment-detail.scss',
})
export class ShipmentDetailComponent implements OnInit {
  private readonly service = inject(ShipmentService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();

  readonly roleKeys = SHIPMENT_ROLE_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly accessSourceKeys = SHIPMENT_ACCESS_SOURCE_KEYS;

  shipment = signal<ShipmentDetail | null>(null);
  loading = signal(true);
  error = signal('');
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  private readonly allowed = computed(() => {
    const s = this.shipment();
    return new Set(s?.canOperate ? s.allowedActions : []);
  });

  /** Pagar flete: la matriz lo permite para el rol y el perfil del usuario opera. */
  canPayFreight = computed(() => this.allowed().has(SHIPMENT_ACTIONS.PAY_FREIGHT));
  canPayLocalCharges = computed(
    () =>
      this.allowed().has(SHIPMENT_ACTIONS.PAY_MANDATORY_LOCAL_CHARGES) ||
      this.allowed().has(SHIPMENT_ACTIONS.PAY_ON_DEMAND_LOCAL_CHARGES),
  );
  canPayDemurrage = computed(() => this.allowed().has(SHIPMENT_ACTIONS.PAY_IMPORT_DEMURRAGE));
  canRequestWarehouseChange = computed(() => this.allowed().has(SHIPMENT_ACTIONS.REQUEST_WAREHOUSE_CHANGE));

  /** Las ODS se consultan en exportación (CL-EXP-13, BO-EXP-09) o si el embarque ya tiene alguna. */
  showServiceOrders = computed(() => {
    const s = this.shipment();
    return !!s && (s.operation === 'EXPORT' || s.serviceOrders.length > 0);
  });

  ngOnInit(): void {
    this.load();
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
