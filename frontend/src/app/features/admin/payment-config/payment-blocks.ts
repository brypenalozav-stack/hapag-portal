import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  MaintainerChange,
  PaymentBlockWindow,
  PaymentBlockWindowRequest,
  PaymentBlockWindowSnapshot,
} from '../../../core/models/payment-config.model';
import { BLOCK_WINDOW_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ChangeLogComponent } from './change-log';
import { ModalService } from '../../../core/services/modal.service';
import { ClientTable, codeText } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/** Valor del selector de país para "todos los países" (country: null en la API). */
const ALL_COUNTRIES = 'ALL';

interface WindowForm {
  country: string;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  reason: string;
  clientMessage: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

const SNAPSHOT_KEYS: Record<string, string> = {
  country: 'admin.paymentBlocks.form.country',
  startDate: 'admin.paymentBlocks.form.startDate',
  startTime: 'admin.paymentBlocks.form.startTime',
  endDate: 'admin.paymentBlocks.form.endDate',
  endTime: 'admin.paymentBlocks.form.endTime',
  reason: 'admin.paymentBlocks.form.reason',
  clientMessage: 'admin.paymentBlocks.form.clientMessage',
  isActive: 'admin.paymentBlocks.snapshot.active',
};

function emptyForm(): WindowForm {
  return { country: ALL_COUNTRIES, startDate: '', startTime: '', endDate: '', endTime: '', reason: '', clientMessage: '' };
}

/**
 * Bloqueo programado de pagos por horario (M8-07): fecha y hora de inicio y término en la hora local del
 * país, país (o todos), motivo y mensaje al cliente. Se activa y desactiva solo; una ventana se edita o
 * cancela antes de su inicio y una activa puede terminarse antes. Toda creación, modificación o
 * cancelación queda en el registro con usuario, fecha y hora.
 */
@Component({
  selector: 'app-payment-blocks',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './payment-blocks.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentBlocksComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly statusKeys = BLOCK_WINDOW_STATUS_KEYS;
  readonly allCountries = ALL_COUNTRIES;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  country = '';
  includeCancelled = false;
  windows = signal<PaymentBlockWindow[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.windows, {
    searchText: (w) =>
      [this.countryLabel(w.country), w.reason, w.clientMessage, codeText(w.status, this.statusKeys), w.startDate, w.endDate, w.modifiedBy ?? w.createdBy].join(' '),
    sortValues: {
      country: (w) => this.countryLabel(w.country),
      window: (w) => `${w.startDate} ${w.startTime}`,
      reason: (w) => w.reason,
      status: (w) => codeText(w.status, this.statusKeys),
      changed: (w) => w.modifiedAt ?? w.createdAt,
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  formOpen = signal(false);
  editing = signal<PaymentBlockWindow | null>(null);
  form: WindowForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyWindow = signal<PaymentBlockWindow | null>(null);
  history = signal<MaintainerChange<PaymentBlockWindowSnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getBlockWindows(this.country, this.includeCancelled).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (windows) => {
        this.windows.set(windows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.paymentBlocks.errors.load'));
      },
    });
  }

  countryLabel(country: string | null | undefined): string {
    if (!country) return translate('admin.paymentBlocks.allCountries');
    return translate(country === 'BO' ? 'common.country.bo' : 'common.country.cl');
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(window: PaymentBlockWindow): void {
    this.editing.set(window);
    this.form = {
      country: window.country ?? ALL_COUNTRIES,
      startDate: window.startDate,
      startTime: window.startTime.slice(0, 5),
      endDate: window.endDate,
      endTime: window.endTime.slice(0, 5),
      reason: window.reason,
      clientMessage: window.clientMessage,
    };
    this.openForm();
  }

  private openForm(): void {
    this.errors.set([]);
    this.saveError.set('');
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.startDate) list.push({ fieldId: 'block-start-date', key: 'admin.paymentBlocks.form.errors.startDate' });
    if (!f.startTime) list.push({ fieldId: 'block-start-time', key: 'admin.paymentBlocks.form.errors.startTime' });
    if (!f.endDate) list.push({ fieldId: 'block-end-date', key: 'admin.paymentBlocks.form.errors.endDate' });
    if (!f.endTime) list.push({ fieldId: 'block-end-time', key: 'admin.paymentBlocks.form.errors.endTime' });
    if (f.startDate && f.startTime && f.endDate && f.endTime && `${f.endDate}T${f.endTime}` <= `${f.startDate}T${f.startTime}`) {
      list.push({ fieldId: 'block-end-date', key: 'admin.paymentBlocks.form.errors.endBeforeStart' });
    }
    if (!f.reason.trim()) list.push({ fieldId: 'block-reason', key: 'admin.paymentBlocks.form.errors.reason' });
    if (!f.clientMessage.trim()) list.push({ fieldId: 'block-message', key: 'admin.paymentBlocks.form.errors.message' });
    return list;
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const body: PaymentBlockWindowRequest = {
      country: f.country === ALL_COUNTRIES ? null : f.country,
      startDate: f.startDate,
      startTime: `${f.startTime}:00`,
      endDate: f.endDate,
      endTime: `${f.endTime}:00`,
      reason: f.reason.trim(),
      clientMessage: f.clientMessage.trim(),
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateBlockWindow(editing.id, body) : this.service.createBlockWindow(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.paymentBlocks.form.updated' : 'admin.paymentBlocks.form.created'));
        this.closeForm();
        this.load();
        if (editing && this.historyWindow()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = paymentErrorMessage(err, 'admin.paymentBlocks.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Cancela una ventana programada o termina antes una activa. */
  async cancelWindow(window: PaymentBlockWindow): Promise<void> {
    const ending = window.status === 'Active';
    const confirmed = await this.modal.confirm({
      title: ending ? 'shared.modal.endBlock.title' : 'shared.modal.cancelBlock.title',
      message: ending ? 'shared.modal.endBlock.message' : 'shared.modal.cancelBlock.message',
      confirmLabel: ending ? 'shared.modal.endBlock.action' : 'shared.modal.cancelBlock.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.service.cancelBlockWindow(window.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.announcer.announce(translate(window.status === 'Active' ? 'admin.paymentBlocks.ended' : 'admin.paymentBlocks.cancelled'));
        this.load();
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'admin.paymentBlocks.errors.cancel');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(window: PaymentBlockWindow): void {
    this.historyWindow.set(window);
    this.historyLoading.set(true);
    this.service.getBlockWindowHistory(window.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.historyWindow.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
