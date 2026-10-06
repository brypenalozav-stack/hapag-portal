import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentConfigService } from '../../../core/services/payment-config.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PaymentMethod, PaymentMethodKind } from '../../../core/models/cart.model';
import {
  MaintainerChange,
  PAYMENT_CURRENCIES,
  PAYMENT_PROVIDER_KEYS,
  PaymentMethodRequest,
  PaymentMethodSnapshot,
} from '../../../core/models/payment-config.model';
import { PAYMENT_METHOD_KIND_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ChangeLogComponent } from './change-log';

interface MethodForm {
  code: string;
  name: string;
  description: string;
  country: string;
  kind: PaymentMethodKind;
  providerKey: string;
  currencies: Set<string>;
  isEnabled: boolean;
  displayOrder: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

const SNAPSHOT_KEYS: Record<string, string> = {
  code: 'admin.paymentMethods.form.code',
  name: 'admin.paymentMethods.form.name',
  country: 'admin.paymentMethods.form.country',
  kind: 'admin.paymentMethods.form.kind',
  providerKey: 'admin.paymentMethods.form.provider',
  currencies: 'admin.paymentMethods.form.currencies',
  isEnabled: 'admin.paymentMethods.form.enabled',
  displayOrder: 'admin.paymentMethods.form.order',
};

function emptyForm(country: string): MethodForm {
  return {
    code: '', name: '', description: '', country, kind: 'Online', providerKey: '', currencies: new Set(),
    isEnabled: true, displayOrder: '10',
  };
}

/**
 * Medios de pago configurables (M5-03): Khipu, botones de banco y depósito con boleta se mantienen; se
 * puede agregar un medio nuevo, reemplazar la plataforma de uno en línea o dejar preparado uno futuro
 * (dólares digitales, deshabilitado). Los medios en línea exigen una plataforma registrada; el depósito no
 * tiene plataforma. Cada cambio queda en el registro (NF-15).
 */
@Component({
  selector: 'app-payment-methods',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent],
  templateUrl: './payment-methods.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class PaymentMethodsComponent implements OnInit {
  private readonly service = inject(PaymentConfigService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly kindKeys = PAYMENT_METHOD_KIND_KEYS;
  readonly currencies = PAYMENT_CURRENCIES;
  readonly providers = PAYMENT_PROVIDER_KEYS;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  country = 'CL';
  includeDisabled = true;
  methods = signal<PaymentMethod[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  formOpen = signal(false);
  editing = signal<PaymentMethod | null>(null);
  form: MethodForm = emptyForm('CL');
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyMethod = signal<PaymentMethod | null>(null);
  history = signal<MaintainerChange<PaymentMethodSnapshot>[]>([]);
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
    this.service.getMethods(this.country, this.includeDisabled).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (methods) => {
        this.methods.set(methods);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'admin.paymentMethods.errors.load'));
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm(this.country);
    this.openForm();
  }

  openEdit(method: PaymentMethod): void {
    this.editing.set(method);
    this.form = {
      code: method.code,
      name: method.name,
      description: method.description ?? '',
      country: method.country,
      kind: method.kind,
      providerKey: method.providerKey ?? '',
      currencies: new Set(method.currencies),
      isEnabled: method.isEnabled,
      displayOrder: String(method.displayOrder),
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

  isCurrency(currency: string): boolean {
    return this.form.currencies.has(currency);
  }

  toggleCurrency(currency: string, event: Event): void {
    const next = new Set(this.form.currencies);
    if ((event.target as HTMLInputElement).checked) next.add(currency);
    else next.delete(currency);
    this.form.currencies = next;
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!/^[A-Za-z0-9_]{2,40}$/.test(f.code.trim())) list.push({ fieldId: 'method-code', key: 'admin.paymentMethods.form.errors.code' });
    if (!f.name.trim()) list.push({ fieldId: 'method-name', key: 'admin.paymentMethods.form.errors.name' });
    if (f.kind === 'Online' && !f.providerKey) list.push({ fieldId: 'method-provider', key: 'admin.paymentMethods.form.errors.provider' });
    if (f.currencies.size === 0) list.push({ fieldId: `method-currency-${PAYMENT_CURRENCIES[0]}`, key: 'admin.paymentMethods.form.errors.currencies' });
    if (!/^\d{1,4}$/.test(f.displayOrder.trim())) list.push({ fieldId: 'method-order', key: 'admin.paymentMethods.form.errors.order' });
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
    const body: PaymentMethodRequest = {
      code: f.code.trim().toUpperCase(),
      name: f.name.trim(),
      description: f.description.trim() || null,
      country: f.country,
      kind: f.kind,
      // El depósito no tiene plataforma (M5-02, M5-03).
      providerKey: f.kind === 'Online' ? f.providerKey : null,
      currencies: PAYMENT_CURRENCIES.filter((c) => f.currencies.has(c)),
      isEnabled: f.isEnabled,
      displayOrder: Number(f.displayOrder),
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateMethod(editing.id, body) : this.service.createMethod(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.paymentMethods.form.updated' : 'admin.paymentMethods.form.created', { name: saved.name }));
        this.closeForm();
        this.load();
        if (editing && this.historyMethod()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = paymentErrorMessage(err, 'admin.paymentMethods.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  disable(method: PaymentMethod): void {
    this.actionError.set('');
    this.service.disableMethod(method.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.announcer.announce(translate('admin.paymentMethods.disabled', { name: method.name }));
        this.load();
      },
      error: (err) => {
        const message = paymentErrorMessage(err, 'admin.paymentMethods.errors.disable');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(method: PaymentMethod): void {
    this.historyMethod.set(method);
    this.historyLoading.set(true);
    this.service.getMethodHistory(method.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.historyMethod.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
