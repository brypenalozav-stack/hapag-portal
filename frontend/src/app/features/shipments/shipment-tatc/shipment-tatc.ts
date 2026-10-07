import { Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../../core/services/shipment.service';
import { ShipmentTatc } from '../../../core/models/shipment.model';
import { PORTAL_ERRORS, TATC_PENDING_REASON_KEYS, TATC_STATUS_CLASS, TATC_STATUS_KEYS } from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Estado del TATC del BL de importación y de cada contenedor (M2-09, CL-IMP-13, BO-IMP-13), leído del sistema de
 * TATC en el momento: número, estado, fecha de emisión, almacén y motivos pendientes. Si el sistema no responde lo
 * dice (NF-11) y no presenta datos anteriores como vigentes. La organización con perfil operativo puede ir a la
 * solicitud masiva de TATC con el BL ya cargado.
 */
@Component({
  selector: 'app-shipment-tatc',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent],
  templateUrl: './shipment-tatc.html',
  styleUrl: '../shipment-detail/shipment-detail.scss',
})
export class ShipmentTatcComponent implements OnInit {
  private readonly service = inject(ShipmentService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();
  /** Nivel del título: 3 dentro de un grupo del detalle del BL, cuyo h2 es el título del grupo. */
  readonly headingLevel = input<2 | 3>(2);

  readonly statusKeys = TATC_STATUS_KEYS;
  readonly statusClass = TATC_STATUS_CLASS;
  readonly reasonKeys = TATC_PENDING_REASON_KEYS;

  tatc = signal<ShipmentTatc | null>(null);
  loading = signal(true);
  /** La consulta al portal falló (5xx o sin conexión), distinto del sistema de TATC caído (`available: false`). */
  loadFailed = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getTatc(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tatc) => {
        this.tatc.set(tatc);
        this.loading.set(false);
      },
      error: (err) => {
        this.tatc.set(null);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate(apiErrorKey(err, PORTAL_ERRORS, 'shipments.detail.tatc.loadError')));
        this.loading.set(false);
      },
    });
  }
}
