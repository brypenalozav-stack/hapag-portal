import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { DashboardIndicators, DashboardShipment } from '../../../core/models/dashboard.model';
import { SHIPMENT_STATUS_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';

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

/**
 * Indicadores operativos de los embarques (M1-05): por estado y próximos arribos y zarpes en 14 días. El dashboard
 * los muestra a pedido y los carga con `@defer`: no compiten con la bandeja "Requiere su acción". El demurrage en
 * riesgo no se repite aquí: está en la bandeja.
 */
@Component({
  selector: 'app-dashboard-indicators',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, HlNumberPipe],
  templateUrl: './dashboard-indicators.html',
  styleUrl: './dashboard-indicators.scss',
})
export class DashboardIndicatorsComponent {
  indicators = input.required<DashboardIndicators>();

  readonly statusKeys = SHIPMENT_STATUS_KEYS;

  /** Embarques por estado: la barra es proporcional al estado con más embarques. */
  statusBars = computed<StatusBar[]>(() => {
    const byStatus = this.indicators().byStatus;
    const max = Math.max(1, ...byStatus.map((s) => s.count));
    return [...byStatus]
      .sort((a, b) => b.count - a.count)
      .map((s) => ({ key: s.key, count: s.count, percent: Math.round((s.count * 100) / max) }));
  });

  /** Próximos arribos (ETA) y zarpes (ETD) en los próximos 14 días. */
  shipmentBlocks = computed<ShipmentBlock[]>(() => {
    const indicators = this.indicators();
    return [
      {
        id: 'arrivals',
        titleKey: 'dashboard.indicators.arrivals.title',
        captionKey: 'dashboard.indicators.arrivals.caption',
        dateKey: 'dashboard.indicators.arrivals.col.date',
        emptyKey: 'dashboard.indicators.arrivals.empty',
        items: indicators.upcomingArrivals,
      },
      {
        id: 'departures',
        titleKey: 'dashboard.indicators.departures.title',
        captionKey: 'dashboard.indicators.departures.caption',
        dateKey: 'dashboard.indicators.departures.col.date',
        emptyKey: 'dashboard.indicators.departures.empty',
        items: indicators.upcomingDepartures,
      },
    ];
  });
}
