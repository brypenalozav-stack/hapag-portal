import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TransactionReportService } from '../../../core/services/transaction-report.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { TRANSACTION_CATEGORIES, TransactionReport, TransactionRow } from '../../../core/models/transaction-report.model';
import { CHARGE_CONCEPT_KEYS, TRANSACTION_CATEGORY_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { saveBlob } from '../../../shared/save-blob';
import { ToastService } from '../../../core/services/toast.service';

const PAGE_SIZE = 50;

/** Nombre del archivo exportado, como lo arma el servidor (`transacciones-yyyyMMdd-yyyyMMdd.xlsx`). */
function exportName(prefix: string, from: string, to: string, format: string): string {
  return `${prefix}-${from.replace(/-/g, '')}-${to.replace(/-/g, '')}.${format}`;
}

/**
 * Reportería general de transacciones por servicio (Fase 2, Ola I, M9-01; permiso `transactions-report.view`): pagos
 * confirmados del portal por rango de fechas (por defecto el mes en curso, máximo 366 días), país, categoría, servicio
 * o concepto y moneda, con el resumen por servicio y los totales de todo el filtro, el detalle paginado y la exportación
 * a Excel o CSV. Las imputaciones a crédito se informan como excepciones.
 */
@Component({
  selector: 'app-transactions-report',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './transactions-report.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class TransactionsReportComponent implements OnInit {
  private readonly reports = inject(TransactionReportService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly categories = TRANSACTION_CATEGORIES;
  readonly categoryKeys = TRANSACTION_CATEGORY_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;

  from = '';
  to = '';
  country = '';
  category = '';
  service = '';
  currency = '';
  blNumber = '';

  report = signal<TransactionReport | null>(null);
  page = signal(1);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  exporting = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  get totalPages(): number {
    const items = this.report()?.items;
    return items ? Math.max(1, Math.ceil(items.total / PAGE_SIZE)) : 1;
  }

  private filters() {
    return {
      from: this.from, to: this.to, country: this.country, category: this.category,
      service: this.service.trim().toUpperCase(), currency: this.currency, blNumber: this.blNumber.trim().toUpperCase(),
    };
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.reports.getTransactions({ ...this.filters(), page: this.page(), pageSize: PAGE_SIZE }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (report) => {
        this.report.set(report);
        // El servidor aplica el rango por defecto: el formulario lo muestra.
        this.from = report.from;
        this.to = report.to;
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(adminErrorMessage(err, 'admin.transactionReports.errors.load'));
      },
    });
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.page.set(next);
    this.load();
  }

  serviceName(row: { service: string; serviceName?: string | null }): string {
    const key = CHARGE_CONCEPT_KEYS[row.service];
    return key ? translate(key) : row.serviceName || row.service;
  }

  rowReference(row: TransactionRow): string {
    return row.blNumber ?? row.bookingNumber ?? row.serviceRequestNumber ?? '—';
  }

  export(format: 'xlsx' | 'csv'): void {
    const report = this.report();
    if (!report || this.exporting()) return;
    this.exporting.set(format);
    this.reports.export('transactions', this.filters(), format, this.locale.lang()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.exporting.set(null);
        const name = exportName('transacciones', report.from, report.to, format);
        saveBlob(blob, name);
        this.toast.success(translate('admin.transactionReports.exported', { name }));
      },
      error: (err) => {
        this.exporting.set(null);
        const message = adminErrorMessage(err, 'admin.transactionReports.errors.export');
        this.error.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
