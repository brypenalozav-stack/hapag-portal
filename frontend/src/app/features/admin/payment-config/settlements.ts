import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ChargeSettlement, SettlementSearch } from '../../../core/models/account-statement.model';
import {
  CHARGE_CONCEPT_KEYS,
  SETTLEMENT_KIND_KEYS,
  SETTLEMENT_STATUS_CLASS,
  SETTLEMENT_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ClientTable, codeText } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/** Identificador de la factura (GUID) para el cruce manual. */
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface MatchError {
  fieldId: string;
  key: string;
}

/**
 * Anticipos e imputaciones a crédito de Finanzas (Fase 2, Ola H, M7-03, M3-19, M5-10, NF-04; permiso `payments.finance`):
 * los pagos liberados antes de su factura (y las imputaciones a crédito), abiertos o cruzados con la factura, con filtros
 * por estado, tipo, país, BL y fecha de pago. El cruce automático vincula los anticipos abiertos con las facturas que los
 * suman; el manual permite cruzar con una factura indicada (las diferencias de monto se aceptan con una nota).
 */
@Component({
  selector: 'app-settlements',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './settlements.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class SettlementsComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly kindKeys = SETTLEMENT_KIND_KEYS;
  readonly statusKeys = SETTLEMENT_STATUS_KEYS;
  readonly statusClass = SETTLEMENT_STATUS_CLASS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly kinds = Object.keys(SETTLEMENT_KIND_KEYS);
  readonly statuses = Object.keys(SETTLEMENT_STATUS_KEYS);

  filters: SettlementSearch = { status: '', kind: '', country: '', blNumber: '', from: '', to: '' };
  rows = signal<ChargeSettlement[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.rows, {
    searchText: (r) =>
      [
        r.paymentNumber,
        r.receiptNumber,
        r.receiptDocumentNumber,
        codeText(r.kind, this.kindKeys),
        codeText(r.conceptCode, this.conceptKeys),
        r.blNumber,
        r.bookingNumber,
        r.country,
        r.payerName,
        r.payerTaxId,
        r.billingTaxId,
        codeText(r.status, this.statusKeys),
      ].join(' '),
    sortValues: {
      payment: (r) => r.paymentNumber,
      kind: (r) => codeText(r.kind, this.kindKeys),
      charge: (r) => codeText(r.conceptCode, this.conceptKeys),
      parties: (r) => r.payerName,
      amount: (r) => r.amount,
      date: (r) => r.settledAt,
      status: (r) => codeText(r.status, this.statusKeys),
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionMessage = signal('');
  actionError = signal('');
  matching = signal(false);

  manual = signal<ChargeSettlement | null>(null);
  invoiceId = '';
  note = '';
  errors = signal<MatchError[]>([]);
  manualBusy = signal(false);
  manualError = signal('');

  private readonly manualHeading = viewChild<ElementRef<HTMLElement>>('manualHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getSettlements(this.filters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.settlements.errors.load'));
      },
    });
  }

  /** Cruce automático: mismo BL, concepto, moneda y RUT facturado, con montos que suman el total de la factura. */
  matchAll(): void {
    if (this.matching()) return;
    this.matching.set(true);
    this.actionMessage.set('');
    this.actionError.set('');
    this.service.matchSettlements().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.matching.set(false);
        const message = translate('admin.settlements.auto.done', { matched: result.matched, open: result.openBefore });
        this.actionMessage.set(message);
        this.announcer.announce(message);
        this.load();
      },
      error: (err) => {
        this.matching.set(false);
        const message = paymentErrorMessage(err, 'admin.settlements.auto.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openManual(row: ChargeSettlement): void {
    this.manual.set(row);
    this.invoiceId = '';
    this.note = '';
    this.errors.set([]);
    this.manualError.set('');
    focusAfterRender(this.injector, () => this.manualHeading()?.nativeElement);
  }

  closeManual(): void {
    this.manual.set(null);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  errorKey(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  confirmManual(event: Event): void {
    event.preventDefault();
    const row = this.manual();
    if (!row || this.manualBusy()) return;
    const errors: MatchError[] = [];
    if (!this.invoiceId.trim()) errors.push({ fieldId: 'settlement-invoice', key: 'admin.settlements.manual.errors.invoiceRequired' });
    else if (!GUID.test(this.invoiceId.trim())) errors.push({ fieldId: 'settlement-invoice', key: 'admin.settlements.manual.errors.invoiceFormat' });
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.manualBusy.set(true);
    this.manualError.set('');
    this.service.matchSettlement(row.id, this.invoiceId.trim(), this.note.trim() || null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.manualBusy.set(false);
        this.manual.set(null);
        const message = translate('admin.settlements.manual.done', { payment: updated.paymentNumber, invoice: updated.matchedInvoiceNumber ?? '—' });
        this.actionMessage.set(message);
        this.announcer.announce(message);
        this.load();
      },
      error: (err) => {
        this.manualBusy.set(false);
        const message = paymentErrorMessage(err, 'admin.settlements.manual.errors.submit');
        this.manualError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
