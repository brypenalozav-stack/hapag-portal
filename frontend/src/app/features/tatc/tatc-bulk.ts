import { Component, DestroyRef, ElementRef, Injector, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ShipmentService } from '../../core/services/shipment.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { TATC_BATCH_MAX_ITEMS, TatcBatch } from '../../core/models/shipment.model';
import {
  PORTAL_ERRORS,
  TATC_BATCH_ITEM_STATUS_CLASS,
  TATC_BATCH_ITEM_STATUS_KEYS,
  TATC_BATCH_REASON_KEYS,
  TATC_BATCH_STATUS_CLASS,
  TATC_BATCH_STATUS_KEYS,
} from '../../core/i18n/labels';
import { apiErrorKey } from '../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../shared/focus-after-render';

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

/** UN/LOCODE: dos letras de país y tres caracteres de localidad (p. ej. CLIQQ). */
const LOCODE = /^[A-Z]{2}[A-Z0-9]{3}$/;

/** Separadores admitidos entre BL: salto de línea, coma, punto y coma, tabulación o espacio. */
const SEPARATOR = /[\s,;]+/;

/**
 * Solicitud masiva de TATC (M2-09) para clientes con alto volumen de embarques por una misma localidad (p. ej. la
 * operación por Iquique): la localidad (UN/LOCODE del puerto de descarga o del destino final) y la lista de BL de
 * importación. El portal valida cada BL (acceso, importación, localidad, país) y envía las líneas válidas en una
 * sola solicitud al sistema de TATC; el resultado muestra cada BL aceptado, rechazado por el sistema o no enviado
 * con su motivo. Debajo, las últimas solicitudes de la organización.
 */
@Component({
  selector: 'app-tatc-bulk',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './tatc-bulk.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class TatcBulkComponent {
  private readonly service = inject(ShipmentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly maxItems = TATC_BATCH_MAX_ITEMS;
  readonly statusKeys = TATC_BATCH_STATUS_KEYS;
  readonly statusClass = TATC_BATCH_STATUS_CLASS;
  readonly itemStatusKeys = TATC_BATCH_ITEM_STATUS_KEYS;
  readonly itemStatusClass = TATC_BATCH_ITEM_STATUS_CLASS;
  readonly reasonKeys = TATC_BATCH_REASON_KEYS;

  locationCode = signal('');
  blText = signal('');
  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');

  result = signal<TatcBatch | null>(null);
  resultLoading = signal(false);

  batches = signal<TatcBatch[]>([]);
  batchesLoading = signal(true);
  batchesFailed = signal(false);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  /** BL de la lista, en mayúsculas y sin repetir. */
  blNumbers = computed(() => [...new Set(this.blText().split(SEPARATOR).map((b) => b.trim().toUpperCase()).filter((b) => b !== ''))]);

  errors = computed<FormError[]>(() => {
    if (!this.submitted()) return [];
    const list: FormError[] = [];
    const code = this.locationCode().trim().toUpperCase();
    if (!code) list.push({ fieldId: 'tatc-location', key: 'tatcRequests.form.errors.locationRequired' });
    else if (!LOCODE.test(code)) list.push({ fieldId: 'tatc-location', key: 'tatcRequests.form.errors.locationInvalid' });
    const count = this.blNumbers().length;
    if (count === 0) list.push({ fieldId: 'tatc-bls', key: 'tatcRequests.form.errors.blsRequired' });
    else if (count > TATC_BATCH_MAX_ITEMS) list.push({ fieldId: 'tatc-bls', key: 'tatcRequests.form.errors.tooMany', params: { max: TATC_BATCH_MAX_ITEMS } });
    return list;
  });

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const bl = params.get('bl');
      if (bl && !this.blNumbers().includes(bl.toUpperCase())) {
        this.blText.set([this.blText().trimEnd(), bl.toUpperCase()].filter((l) => l !== '').join('\n'));
      }
      const batch = params.get('batch');
      if (batch) this.openBatch(batch);
    });
    this.loadBatches();
  }

  onLocation(event: Event): void {
    this.locationCode.set((event.target as HTMLInputElement).value);
  }

  onBls(event: Event): void {
    this.blText.set((event.target as HTMLTextAreaElement).value);
  }

  loadBatches(): void {
    this.batchesLoading.set(true);
    this.batchesFailed.set(false);
    this.service.getTatcBatches().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (batches) => {
        this.batches.set(batches);
        this.batchesLoading.set(false);
      },
      error: (err) => {
        this.batches.set([]);
        this.batchesFailed.set(isServiceUnavailable(err));
        this.batchesLoading.set(false);
      },
    });
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) return;
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.submitting.set(true);
    this.announcer.announce(translate('tatcRequests.form.sending', { count: this.blNumbers().length }));
    this.service.requestTatcBatch({
      locationCode: this.locationCode().trim().toUpperCase(),
      blNumbers: this.blNumbers(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (batch) => {
        this.submitting.set(false);
        this.showResult(batch);
        this.loadBatches();
      },
      error: (err) => {
        this.submitting.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'tatcRequests.form.errors.submit'));
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Resultado de una solicitud anterior (GET /shipments/tatc-batches/{id}). */
  openBatch(id: string): void {
    this.resultLoading.set(true);
    this.service.getTatcBatch(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (batch) => {
        this.resultLoading.set(false);
        this.showResult(batch);
      },
      error: (err) => {
        this.resultLoading.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'tatcRequests.result.loadError'));
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  private showResult(batch: TatcBatch): void {
    this.result.set(batch);
    this.announcer.announce(translate('tatcRequests.result.announce', {
      accepted: batch.acceptedItems,
      total: batch.totalItems,
      status: translate(TATC_BATCH_STATUS_KEYS[batch.status] ?? batch.status),
    }));
    focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
  }

  /** Líneas no aceptadas (rechazadas por el sistema o no enviadas). */
  notAccepted(batch: TatcBatch): number {
    return batch.totalItems - batch.acceptedItems;
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
