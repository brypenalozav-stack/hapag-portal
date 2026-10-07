import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TransactionReportService } from '../../../core/services/transaction-report.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { EXCEPTION_TYPES, ExceptionReport } from '../../../core/models/transaction-report.model';
import { CHARGE_CONCEPT_KEYS, EXCEPTION_TYPE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { saveBlob } from '../../../shared/save-blob';
import { ToastService } from '../../../core/services/toast.service';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

const PAGE_SIZE = 50;

/**
 * Excepciones aplicadas a los cargos (Fase 2, Ola I, M9-01; permiso `transactions-report.view`): exenciones de Gate In,
 * EDS y Gate Out aplicadas desde Nexus, cambios de almacén gratuitos, imputaciones a crédito y cargos IPO excluidos por
 * crédito en Nexus, cada una con el embarque y el cliente sobre el que se aplicó. Si Nexus no respondió, las exclusiones
 * de IPO no se calculan y la pantalla lo advierte.
 */
@Component({
  selector: 'app-exceptions-report',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent, PaginatorComponent,
  ],
  templateUrl: './exceptions-report.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ExceptionsReportComponent implements OnInit {
  private readonly service = inject(TransactionReportService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly types = EXCEPTION_TYPES;
  readonly typeKeys = EXCEPTION_TYPE_KEYS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;

  from = '';
  to = '';
  country = '';
  type = '';
  blNumber = '';

  report = signal<ExceptionReport | null>(null);
  page = signal(1);
  pageSize = signal(PAGE_SIZE);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  exporting = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private filters() {
    return { from: this.from, to: this.to, country: this.country, type: this.type, blNumber: this.blNumber.trim().toUpperCase() };
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getExceptions({ ...this.filters(), page: this.page(), pageSize: this.pageSize() }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (report) => {
        this.report.set(report);
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

  changePage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  export(format: 'xlsx' | 'csv'): void {
    const report = this.report();
    if (!report || this.exporting()) return;
    this.exporting.set(format);
    this.service.export('exceptions', this.filters(), format, this.locale.lang()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.exporting.set(null);
        const name = `excepciones-${report.from.replace(/-/g, '')}-${report.to.replace(/-/g, '')}.${format}`;
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
