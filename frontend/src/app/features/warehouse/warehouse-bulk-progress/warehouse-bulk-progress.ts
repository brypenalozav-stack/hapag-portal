import { Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { WarehouseChangeService } from '../../../core/services/warehouse-change.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  WAREHOUSE_BATCH_FINAL_STATUSES,
  WarehouseChangeBatch,
} from '../../../core/models/warehouse-change.model';
import {
  CHARGE_ERRORS,
  WAREHOUSE_BATCH_ITEM_STATUS_KEYS,
  WAREHOUSE_BATCH_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/** Intervalo de consulta del avance (el servidor procesa por tramos cada pocos segundos, NF-19). */
export const BULK_POLL_INTERVAL_MS = 2000;

/**
 * Avance de una solicitud masiva de cambio de almacén (M3-05, NF-19): consulta el estado hasta que
 * termina, muestra la barra de progreso y el resultado de cada línea, y anuncia los cambios en la
 * región polite (guía UI §3.3: cargas con inicio, avance y resultado).
 */
@Component({
  selector: 'app-warehouse-bulk-progress',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './warehouse-bulk-progress.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class WarehouseBulkProgressComponent implements OnInit {
  private readonly service = inject(WarehouseChangeService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** ID de la solicitud masiva (ruta /warehouse/bulk/:id). */
  id = input.required<string>();

  readonly statusKeys = WAREHOUSE_BATCH_STATUS_KEYS;
  readonly itemStatusKeys = WAREHOUSE_BATCH_ITEM_STATUS_KEYS;
  readonly errorKeys = CHARGE_ERRORS;

  batch = signal<WarehouseChangeBatch | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  private polling?: Subscription;
  private lastAnnounced = -1;

  ngOnInit(): void {
    this.start();
  }

  isFinal(batch: WarehouseChangeBatch): boolean {
    return WAREHOUSE_BATCH_FINAL_STATUSES.includes(batch.status);
  }

  /** Consulta el avance cada BULK_POLL_INTERVAL_MS hasta que la solicitud termina. */
  start(): void {
    this.polling?.unsubscribe();
    this.loadFailed.set(false);
    this.error.set('');
    if (!this.batch()) this.loading.set(true);
    this.polling = timer(0, BULK_POLL_INTERVAL_MS).pipe(
      switchMap(() => this.service.getBulk(this.id())),
      takeWhile((batch) => !this.isFinal(batch), true),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (batch) => {
        this.batch.set(batch);
        this.loading.set(false);
        this.announce(batch);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          this.error.set(translate('warehouse.progress.notFound'));
        } else {
          this.error.set(translate('warehouse.progress.loadError'));
        }
        this.loading.set(false);
      },
    });
  }

  /** Anuncia el avance solo cuando cambia, y el resultado al terminar. */
  private announce(batch: WarehouseChangeBatch): void {
    if (this.isFinal(batch)) {
      this.announcer.announce(translate('warehouse.progress.finished', {
        succeeded: batch.succeededItems,
        failed: batch.failedItems,
        total: batch.totalItems,
      }));
      return;
    }
    if (batch.processedItems === this.lastAnnounced) return;
    this.lastAnnounced = batch.processedItems;
    this.announcer.announce(translate('warehouse.progress.update', {
      processed: batch.processedItems,
      total: batch.totalItems,
      percent: batch.progressPercent,
    }));
  }
}
