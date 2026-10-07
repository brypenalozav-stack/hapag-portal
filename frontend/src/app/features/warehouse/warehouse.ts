import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { WarehouseChangeService } from '../../core/services/warehouse-change.service';
import { ShipmentService } from '../../core/services/shipment.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { CartService } from '../../core/services/cart.service';
import {
  WAREHOUSE_BATCH_MAX_ITEMS,
  WarehouseChangeDetail,
  WarehouseChangeQuote,
} from '../../core/models/warehouse-change.model';
import { ShipmentListItem } from '../../core/models/shipment.model';
import { apiErrorKey } from '../../core/http/api-error';
import { CHARGE_ERRORS, DATA_SOURCE_KEYS, WAREHOUSE_CHANGE_STATUS_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { ExchangeRateNoteComponent } from '../../shared/components/exchange-rate-note/exchange-rate-note';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { parseBulkLines } from './bulk-lines';
import { focusAfterRender } from '../../shared/focus-after-render';
import { AddToCartDialogComponent, AddToCartTarget } from '../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { ToastService } from '../../core/services/toast.service';
import { FeatureService } from '../../core/services/feature.service';

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

/** Moneda local de cada país, para informar el tipo de cambio de una tarifa en otra moneda (M5-05). */
const COUNTRY_CURRENCY: Record<string, string> = { CL: 'CLP', BO: 'BOB' };
/** Motivos de bloqueo de la cotización → clave Transloco. */
const QUOTE_BLOCKED_KEYS: Record<string, string> = {
  NO_PERMISSION: 'warehouse.single.blocked.noPermission',
  'Tariff.NotInForce': 'warehouse.single.blocked.tariffNotInForce',
};
/** Embarques de importación que se ofrecen para la selección múltiple. */
const SHIPMENT_PAGE_SIZE = 50;
/** Tamaño máximo del archivo de la lista (texto). */
const MAX_FILE_BYTES = 512 * 1024;

/**
 * Cambio de almacén (Fase 1, Ola C; reemplaza al formulario anterior, que aceptaba el monto del
 * cliente):
 * - solicitud individual: el servidor indica si hay derecho a cambio gratuito (regla interna o
 *   Nexus) o cobra la tarifa KTE/KTF vigente; la gratuita se completa sin cobro ni Customer
 *   Service (M3-04, M8-01);
 * - solicitud masiva: lista pegada o cargada desde un archivo, o embarques elegidos del listado;
 *   se procesa en segundo plano y el avance se sigue en /warehouse/bulk/:id (M3-05, NF-19).
 * Ola D: la solicitud pendiente de pago se agrega al carro (M5-01) con su RUT de facturación (M5-09).
 */
@Component({
  selector: 'app-warehouse',
  standalone: true,
  imports: [
    TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    ExchangeRateNoteComponent, RouterLink, AddToCartDialogComponent,
  ],
  templateUrl: './warehouse.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .warehouse-shipments { max-height: 16rem; overflow-y: auto; }'],
})
export class WarehouseComponent implements OnInit {
  private readonly service = inject(WarehouseChangeService);
  readonly features = inject(FeatureService);
  private readonly shipmentService = inject(ShipmentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly cart = inject(CartService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** BL precargado desde el detalle del embarque (?bl=). */
  bl = input<string>();

  readonly sourceKeys = DATA_SOURCE_KEYS;
  readonly statusKeys = WAREHOUSE_CHANGE_STATUS_KEYS;
  readonly maxItems = WAREHOUSE_BATCH_MAX_ITEMS;

  // Solicitud individual (M3-04)
  blNumber = signal('');
  containerNumber = signal('');
  quote = signal<WarehouseChangeQuote | null>(null);
  quoteLoading = signal(false);
  quoteFailed = signal(false);
  quoteError = signal('');
  tariffCode = signal<string | null>(null);
  fromWarehouse = signal('');
  toWarehouse = signal('');
  singleSubmitted = signal(false);
  singleBusy = signal(false);
  singleError = signal('');
  result = signal<WarehouseChangeDetail | null>(null);
  addTargets = signal<AddToCartTarget[] | null>(null);

  private readonly singleErrorSummary = viewChild<ElementRef<HTMLElement>>('singleErrorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  // Solicitud masiva (M3-05)
  bulkText = signal('');
  defaultDestination = signal('');
  fileError = signal('');
  shipments = signal<ShipmentListItem[]>([]);
  shipmentsLoaded = signal(false);
  shipmentsLoading = signal(false);
  selectedShipments = signal<Set<string>>(new Set());
  bulkSubmitted = signal(false);
  bulkBusy = signal(false);
  bulkError = signal('');

  private readonly bulkErrorSummary = viewChild<ElementRef<HTMLElement>>('bulkErrorSummary');

  localCurrency = computed(() => COUNTRY_CURRENCY[this.quote()?.country ?? 'CL'] ?? 'CLP');

  quoteBlockedKey = computed(() => {
    const reason = this.quote()?.blockedReason;
    return reason ? (QUOTE_BLOCKED_KEYS[reason] ?? 'warehouse.single.blocked.generic') : null;
  });

  singleErrors = computed<FormError[]>(() => {
    if (!this.singleSubmitted()) return [];
    const list: FormError[] = [];
    if (!this.toWarehouse().trim()) list.push({ fieldId: 'warehouse-to', key: 'warehouse.single.errors.toRequired' });
    if (this.fromWarehouse().trim() && this.fromWarehouse().trim().toLowerCase() === this.toWarehouse().trim().toLowerCase()) {
      list.push({ fieldId: 'warehouse-to', key: 'warehouse.single.errors.sameWarehouse' });
    }
    return list;
  });

  parsed = computed(() => parseBulkLines(this.bulkText(), this.defaultDestination()));

  bulkErrors = computed<FormError[]>(() => {
    if (!this.bulkSubmitted()) return [];
    const list: FormError[] = [];
    const { items, issues } = this.parsed();
    if (items.length === 0 && issues.length === 0) list.push({ fieldId: 'warehouse-bulk-lines', key: 'warehouse.bulk.errors.linesRequired' });
    if (items.length > WAREHOUSE_BATCH_MAX_ITEMS) {
      list.push({ fieldId: 'warehouse-bulk-lines', key: 'warehouse.bulk.errors.tooMany', params: { max: WAREHOUSE_BATCH_MAX_ITEMS } });
    }
    for (const issue of issues.slice(0, 10)) {
      list.push({
        fieldId: issue.kind === 'missingDestination' ? 'warehouse-bulk-destination' : 'warehouse-bulk-lines',
        key: issue.kind === 'missingDestination' ? 'warehouse.bulk.errors.missingDestination' : 'warehouse.bulk.errors.missingBl',
        params: { line: issue.lineNumber },
      });
    }
    return list;
  });

  ngOnInit(): void {
    const bl = this.bl()?.trim();
    if (bl) {
      this.blNumber.set(bl);
      this.loadQuote();
    }
  }

  // Solicitud individual
  onBlNumber(event: Event): void {
    this.blNumber.set((event.target as HTMLInputElement).value);
  }

  onContainer(event: Event): void {
    this.containerNumber.set((event.target as HTMLInputElement).value);
  }

  onFrom(event: Event): void {
    this.fromWarehouse.set((event.target as HTMLInputElement).value);
  }

  onTo(event: Event): void {
    this.toWarehouse.set((event.target as HTMLInputElement).value);
  }

  onTariff(code: string | null | undefined): void {
    this.tariffCode.set(code ?? null);
  }

  /** Derecho a cambio gratuito o tarifas vigentes para el BL (M3-04, M8-01). */
  loadQuote(event?: Event): void {
    event?.preventDefault();
    const bl = this.blNumber().trim();
    if (!bl) return;
    this.quoteLoading.set(true);
    this.quoteFailed.set(false);
    this.quoteError.set('');
    this.quote.set(null);
    this.result.set(null);
    this.singleSubmitted.set(false);
    this.singleError.set('');
    this.service.getQuote(bl, this.containerNumber().trim() || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (quote) => {
        this.quote.set(quote);
        this.tariffCode.set(quote.defaultTariffCode ?? quote.tariffs[0]?.code ?? null);
        this.quoteLoading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.quoteFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          this.quoteError.set(translate('warehouse.single.notFound', { bl }));
        } else if (err instanceof HttpErrorResponse && err.status === 403) {
          this.quoteError.set(translate('warehouse.single.blocked.noPermission'));
        } else {
          this.quoteError.set(translate(apiErrorKey(err, CHARGE_ERRORS, 'warehouse.single.quoteError')));
        }
        this.quoteLoading.set(false);
      },
    });
  }

  submitSingle(event: Event): void {
    event.preventDefault();
    const quote = this.quote();
    if (!quote) return;
    this.singleSubmitted.set(true);
    this.singleError.set('');
    if (this.singleErrors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.singleErrorSummary()?.nativeElement);
      return;
    }
    this.singleBusy.set(true);
    this.service.request({
      blNumber: quote.blNumber,
      containerNumber: this.containerNumber().trim() || null,
      fromWarehouse: this.fromWarehouse().trim() || null,
      toWarehouse: this.toWarehouse().trim(),
      tariffCode: quote.entitlement.isFree ? null : this.tariffCode(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.singleBusy.set(false);
        this.result.set(detail);
        this.announcer.announce(
          detail.isFree
            ? translate('warehouse.single.result.freeAnnouncement', { bl: detail.blNumber })
            : translate('warehouse.single.result.pendingAnnouncement', { bl: detail.blNumber }),
        );
        focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
      },
      error: (err) => {
        this.singleBusy.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'warehouse.single.errors.submit'));
        this.singleError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Agrega al carro el cambio de almacén pendiente de pago (M5-01). */
  addToCart(detail: WarehouseChangeDetail): void {
    this.addTargets.set([{
      itemType: 'WarehouseChange',
      sourceId: detail.id,
      label: translate('warehouse.single.result.cartLabel', { bl: detail.blNumber }),
    }]);
  }

  onAddClosed(): void {
    this.addTargets.set(null);
  }

  newRequest(): void {
    this.result.set(null);
    this.quote.set(null);
    this.toWarehouse.set('');
    this.fromWarehouse.set('');
    this.singleSubmitted.set(false);
    focusAfterRender(this.injector, () => document.getElementById('warehouse-bl'));
  }

  // Solicitud masiva
  onBulkText(event: Event): void {
    this.bulkText.set((event.target as HTMLTextAreaElement).value);
  }

  onDefaultDestination(event: Event): void {
    this.defaultDestination.set((event.target as HTMLInputElement).value);
  }

  /** Carga la lista desde un archivo de texto o CSV y la agrega al área de texto. */
  onFile(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    this.fileError.set('');
    if (!file) return;
    if (file.size > MAX_FILE_BYTES) {
      this.fileError.set(translate('warehouse.bulk.file.tooLarge'));
      inputEl.value = '';
      return;
    }
    file.text().then(
      (content) => {
        this.appendLines(content.split(/\r?\n/).filter((l) => l.trim() !== ''));
        this.announcer.announce(translate('warehouse.bulk.file.loaded', { name: file.name }));
        inputEl.value = '';
      },
      () => this.fileError.set(translate('warehouse.bulk.file.readError')),
    );
  }

  loadShipments(): void {
    this.shipmentsLoading.set(true);
    this.shipmentService.search({ operation: 'IMPORT', page: 1, pageSize: SHIPMENT_PAGE_SIZE }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (page) => {
        this.shipments.set(page.items);
        this.shipmentsLoaded.set(true);
        this.shipmentsLoading.set(false);
      },
      error: () => {
        this.shipments.set([]);
        this.shipmentsLoaded.set(true);
        this.shipmentsLoading.set(false);
      },
    });
  }

  isShipmentSelected(bl: string): boolean {
    return this.selectedShipments().has(bl);
  }

  toggleShipment(bl: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedShipments.update((current) => {
      const next = new Set(current);
      if (checked) next.add(bl);
      else next.delete(bl);
      return next;
    });
  }

  /** Agrega los embarques elegidos a la lista (con el destino común, si se indicó). */
  addSelectedShipments(): void {
    const selected = [...this.selectedShipments()];
    if (selected.length === 0) return;
    this.appendLines(selected);
    this.selectedShipments.set(new Set());
    this.toast.success(translate('warehouse.bulk.shipments.added', { count: selected.length }));
  }

  private appendLines(lines: string[]): void {
    const current = this.bulkText().trimEnd();
    this.bulkText.set([current, ...lines].filter((l) => l !== '').join('\n'));
  }

  submitBulk(event: Event): void {
    event.preventDefault();
    this.bulkSubmitted.set(true);
    this.bulkError.set('');
    if (this.bulkErrors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.bulkErrorSummary()?.nativeElement);
      return;
    }
    this.bulkBusy.set(true);
    this.service.submitBulk(this.parsed().items).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (batch) => {
        this.bulkBusy.set(false);
        this.toast.success(translate('warehouse.bulk.submitted', { count: batch.totalItems }));
        this.router.navigate(['/warehouse/bulk', batch.id]);
      },
      error: (err) => {
        this.bulkBusy.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'warehouse.bulk.errors.submit'));
        this.bulkError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
