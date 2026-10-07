import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  MaintainerChange,
  PAYMENT_CURRENCIES,
  PaymentCurrencyConfig,
  PaymentCurrencySnapshot,
} from '../../../core/models/payment-config.model';
import { CHARGE_CONCEPT_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ChangeLogComponent } from './change-log';
import { ToastService } from '../../../core/services/toast.service';

/** Campos del registro de cambios de una moneda habilitada. */
const SNAPSHOT_KEYS: Record<string, string> = {
  country: 'admin.paymentCurrencies.snapshot.country',
  conceptCode: 'admin.paymentCurrencies.snapshot.concept',
  currency: 'admin.paymentCurrencies.snapshot.currency',
  isEnabled: 'admin.paymentCurrencies.snapshot.enabled',
};

/**
 * Monedas de pago habilitadas por concepto de cobro y país (M5-04), sin intervención de desarrollo: se
 * habilitan o deshabilitan por concepto, se vuelve a la regla por defecto (moneda del cargo y local) y
 * cada cambio queda registrado (NF-15). En Chile se aplican además las reglas de Finanzas (M5-08).
 */
@Component({
  selector: 'app-payment-currencies',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent],
  templateUrl: './payment-currencies.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentCurrenciesComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly currencies = PAYMENT_CURRENCIES;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  country = 'CL';
  rows = signal<PaymentCurrencyConfig[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  editing = signal<PaymentCurrencyConfig | null>(null);
  selection = signal<Set<string>>(new Set());
  formError = signal('');
  saving = signal(false);

  historyRow = signal<PaymentCurrencyConfig | null>(null);
  history = signal<MaintainerChange<PaymentCurrencySnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getCurrencies(this.country).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.paymentCurrencies.errors.load'));
      },
    });
  }

  conceptLabel(row: PaymentCurrencyConfig): string {
    const key = CHARGE_CONCEPT_KEYS[row.conceptCode];
    return key ? translate(key) : row.conceptName;
  }

  edit(row: PaymentCurrencyConfig): void {
    this.editing.set(row);
    this.selection.set(new Set(row.enabledCurrencies));
    this.formError.set('');
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.editing.set(null);
  }

  isChecked(currency: string): boolean {
    return this.selection().has(currency);
  }

  toggle(currency: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selection.update((current) => {
      const next = new Set(current);
      if (checked) next.add(currency);
      else next.delete(currency);
      return next;
    });
  }

  save(event: Event): void {
    event.preventDefault();
    const row = this.editing();
    if (!row) return;
    const currencies = PAYMENT_CURRENCIES.filter((c) => this.selection().has(c));
    if (currencies.length === 0) {
      this.formError.set(translate('admin.paymentCurrencies.errors.required'));
      this.announcer.announce(this.formError(), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById(`currency-option-${PAYMENT_CURRENCIES[0]}`));
      return;
    }
    this.saving.set(true);
    this.formError.set('');
    this.service.setCurrencies(row.country, row.conceptCode, currencies).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(translate('admin.paymentCurrencies.saved', { concept: this.conceptLabel(row) }));
        this.editing.set(null);
        this.load();
        if (this.historyRow()?.conceptCode === row.conceptCode) this.openHistory(row);
      },
      error: (err) => {
        this.saving.set(false);
        const message = paymentErrorMessage(err, 'admin.paymentCurrencies.errors.save');
        this.formError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  reset(row: PaymentCurrencyConfig): void {
    this.actionError.set('');
    this.service.resetCurrencies(row.country, row.conceptCode).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.announcer.announce(translate('admin.paymentCurrencies.reset', { concept: this.conceptLabel(row) }));
        this.load();
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'admin.paymentCurrencies.errors.save');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(row: PaymentCurrencyConfig): void {
    this.historyRow.set(row);
    this.historyLoading.set(true);
    this.service.getCurrencyHistory(row.country, row.conceptCode).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.historyRow.set(null);
  }
}
