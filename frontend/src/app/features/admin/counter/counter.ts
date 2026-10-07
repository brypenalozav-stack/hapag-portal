import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { CounterService } from '../../../core/services/counter.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  COUNTER_SYNC_STATUSES,
  CounterDetail,
  CounterRecord,
  CounterRecordRequest,
  CounterSnapshot,
} from '../../../core/models/counter.model';
import { MaintainerChange } from '../../../core/models/payment-config.model';
import { COUNTER_SYNC_STATUS_CLASS, COUNTER_SYNC_STATUS_KEYS, SHIPMENT_OPERATION_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { todayInput } from '../../../shared/date-input';
import { ChangeLogComponent } from '../payment-config/change-log';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

interface CounterForm {
  country: 'CL' | 'BO';
  exchangeDate: string;
  hblReceived: boolean;
  hblReceivedAt: string;
  deconsolidated: boolean;
  deconsolidatedAt: string;
  notes: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

const PAGE_SIZE = 20;

const SNAPSHOT_KEYS: Record<string, string> = {
  country: 'admin.counter.form.country',
  exchangeDate: 'admin.counter.form.exchangeDate',
  hblReceived: 'admin.counter.form.hblReceived',
  hblReceivedAt: 'admin.counter.form.hblReceivedAt',
  deconsolidated: 'admin.counter.form.deconsolidated',
  deconsolidatedAt: 'admin.counter.form.deconsolidatedAt',
  notes: 'admin.counter.form.notes',
  syncStatus: 'admin.counter.col.sync',
};

/** Fecha de calendario (`yyyy-MM-dd`) de un valor ISO, para el campo de fecha. */
function dateOnly(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : '';
}

/**
 * Counter Bolivia/Ultramar (Fase 2, Ola I, M8-09; permiso `counter.manage`): registro por BL de la fecha de canje, la
 * recepción del HBL y la marca de desconsolidado, con el país de la operación. Cada registro se propaga a Nexus; si
 * Nexus falla queda "Fallido" con el error y se reintenta. Cada registro y cambio queda en el historial (NF-15).
 */
@Component({
  selector: 'app-counter',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent, PaginatorComponent],
  templateUrl: './counter.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class CounterComponent implements OnInit {
  private readonly service = inject(CounterService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** `?bl=` abre directamente el registro de un BL (desde el detalle del embarque). */
  bl = input<string>();

  readonly statuses = COUNTER_SYNC_STATUSES;
  readonly syncKeys = COUNTER_SYNC_STATUS_KEYS;
  readonly syncClass = COUNTER_SYNC_STATUS_CLASS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly snapshotKeys = SNAPSHOT_KEYS;
  readonly today = todayInput();

  blFilter = '';
  country = '';
  syncStatus = '';
  deconsolidated = '';
  records = signal<CounterRecord[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = signal(PAGE_SIZE);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  retrying = signal<string | null>(null);

  openBl = '';
  detail = signal<CounterDetail | null>(null);
  detailLoading = signal(false);
  detailError = signal('');
  form: CounterForm = { country: 'BO', exchangeDate: '', hblReceived: false, hblReceivedAt: '', deconsolidated: false, deconsolidatedAt: '', notes: '' };
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyBl = signal<string | null>(null);
  history = signal<MaintainerChange<CounterSnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly detailHeading = viewChild<ElementRef<HTMLElement>>('detailHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
    const bl = this.bl();
    if (bl) this.openRecord(bl);
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.search({
      blNumber: this.blFilter.trim().toUpperCase(), country: this.country, syncStatus: this.syncStatus,
      deconsolidated: this.deconsolidated, page: this.page(), pageSize: this.pageSize(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        this.records.set(r.items);
        this.total.set(r.total);
        this.loading.set(false);
      },
      error: (err) => {
        this.records.set([]);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.actionError.set(adminErrorMessage(err, 'admin.counter.errors.load'));
      },
    });
  }

  search(): void {
    this.page.set(1);
    this.load();
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

  /** Registrar o editar el Counter de un BL por su número. */
  openByNumber(event: Event): void {
    event.preventDefault();
    const bl = this.openBl.trim().toUpperCase();
    if (bl) this.openRecord(bl);
  }

  openRecord(blNumber: string): void {
    this.detailLoading.set(true);
    this.detailError.set('');
    this.errors.set([]);
    this.saveError.set('');
    this.service.getByBl(blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.detailLoading.set(false);
        const r = detail.record;
        const src = detail.source;
        this.form = {
          country: r?.country ?? (detail.shipment.country === 'CL' ? 'CL' : 'BO'),
          exchangeDate: dateOnly(r?.exchangeDate ?? src?.exchangeDate),
          hblReceived: r?.hblReceived ?? src?.hblReceived ?? false,
          hblReceivedAt: dateOnly(r?.hblReceivedAt ?? src?.hblReceivedAt),
          deconsolidated: r?.deconsolidated ?? src?.deconsolidated ?? false,
          deconsolidatedAt: dateOnly(r?.deconsolidatedAt ?? src?.deconsolidatedAt),
          notes: r?.notes ?? '',
        };
        focusAfterRender(this.injector, () => this.detailHeading()?.nativeElement);
      },
      error: (err) => {
        this.detail.set(null);
        this.detailLoading.set(false);
        const message = adminErrorMessage(err, 'admin.counter.errors.detail');
        this.detailError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  closeDetail(): void {
    this.detail.set(null);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    const future = (d: string) => !!d && d > this.today;
    if (future(f.exchangeDate)) list.push({ fieldId: 'counter-exchange-date', key: 'admin.counter.form.errors.futureExchange' });
    if (f.hblReceived && future(f.hblReceivedAt)) list.push({ fieldId: 'counter-hbl-date', key: 'admin.counter.form.errors.futureHbl' });
    if (f.deconsolidated && future(f.deconsolidatedAt)) list.push({ fieldId: 'counter-deconsolidated-date', key: 'admin.counter.form.errors.futureDeconsolidated' });
    return list;
  }

  save(event: Event): void {
    event.preventDefault();
    const detail = this.detail();
    if (!detail) return;
    this.saveError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const body: CounterRecordRequest = {
      country: f.country,
      exchangeDate: f.exchangeDate || null,
      hblReceived: f.hblReceived,
      hblReceivedAt: f.hblReceived ? f.hblReceivedAt || null : null,
      deconsolidated: f.deconsolidated,
      deconsolidatedAt: f.deconsolidated ? f.deconsolidatedAt || null : null,
      notes: f.notes.trim() || null,
    };
    const bl = detail.shipment.blNumber;
    this.saving.set(true);
    this.service.save(bl, body).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (record) => {
        this.saving.set(false);
        this.detail.update((d) => (d ? { ...d, record } : d));
        const key = record.syncStatus === 'Failed' ? 'admin.counter.form.savedFailed' : 'admin.counter.form.saved';
        this.announcer.announce(translate(key, { bl }), record.syncStatus === 'Failed' ? 'assertive' : 'polite');
        this.load();
        if (this.historyBl() === bl) this.openHistory(bl);
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'admin.counter.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  retry(blNumber: string): void {
    this.retrying.set(blNumber);
    this.actionError.set('');
    this.service.retrySync(blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (record) => {
        this.retrying.set(null);
        this.detail.update((d) => (d && d.shipment.blNumber === blNumber ? { ...d, record } : d));
        const key = record.syncStatus === 'Synced' ? 'admin.counter.retried' : 'admin.counter.retryFailed';
        this.announcer.announce(translate(key, { bl: blNumber }), record.syncStatus === 'Synced' ? 'polite' : 'assertive');
        this.load();
      },
      error: (err) => {
        this.retrying.set(null);
        const message = adminErrorMessage(err, 'admin.counter.errors.retry');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(blNumber: string): void {
    this.historyBl.set(blNumber);
    this.historyLoading.set(true);
    this.service.getHistory(blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
        focusAfterRender(this.injector, () => this.historyHeading()?.nativeElement);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }

  closeHistory(): void {
    this.historyBl.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
