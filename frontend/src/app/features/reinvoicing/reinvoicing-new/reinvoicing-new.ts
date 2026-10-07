import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReinvoicingService } from '../../../core/services/reinvoicing.service';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ReinvoicingQuote } from '../../../core/models/reinvoicing.model';
import { CHARGE_CONCEPT_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { billingErrorKeys } from '../../service-requests/shared/service-text';
import { ReinvoicingStepsComponent } from '../reinvoicing-steps';
import { ineligibleReason, isEmail, normalizeTaxId, reinvoicingErrorMessage } from '../reinvoicing-text';
import { ToastService } from '../../../core/services/toast.service';

interface BillingForm {
  taxId: string;
  name: string;
  address: string;
  email: string;
  activity: string;
  acceptorEmail: string;
  reason: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

/** Campo de facturación del servidor → id del control. */
const BILLING_FIELD_IDS: Record<string, string> = {
  taxId: 'reinvoicing-tax-id',
  name: 'reinvoicing-name',
  address: 'reinvoicing-address',
  email: 'reinvoicing-email',
  activity: 'reinvoicing-activity',
};

/**
 * Refacturación IAO, paso 1 (Fase 2, Ola H, M3-11): desde una factura chilena emitida, muestra lo que costaría refacturar
 * (cargo de refacturación con IVA y pérdida de IVA de la factura original, con el tipo de cambio si corresponde) y pide
 * los datos de facturación de la nueva razón social y, opcionalmente, el correo de quien aceptará el cobro. Crea el
 * borrador y sigue en su página (aprobación, envío, pago y aceptación). Si la factura no es elegible, dice por qué.
 */
@Component({
  selector: 'app-reinvoicing-new',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, HlNumberPipe, LoadingSpinnerComponent,
    StateMessageComponent, ReinvoicingStepsComponent,
  ],
  templateUrl: './reinvoicing-new.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ReinvoicingNewComponent implements OnInit {
  private readonly service = inject(ReinvoicingService);
  readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Factura a refacturar (?invoiceId=). */
  invoiceId = input<string>();

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;

  quote = signal<ReinvoicingQuote | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  form: BillingForm = { taxId: '', name: '', address: '', email: '', activity: '', acceptorEmail: '', reason: '' };
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.invoiceId();
    if (!id) {
      this.loading.set(false);
      this.error.set(translate('reinvoicing.new.noInvoice'));
      return;
    }
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.quote(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (quote) => {
        this.quote.set(quote);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(reinvoicingErrorMessage(err, 'reinvoicing.new.loadError'));
      },
    });
  }

  reason(code: string | null | undefined): string {
    return ineligibleReason(code);
  }

  invoiceNumber(q: ReinvoicingQuote): string {
    return q.siiNumber ?? q.sourceNumber;
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  errorKey(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  private validate(q: ReinvoicingQuote): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.taxId.trim()) list.push({ fieldId: 'reinvoicing-tax-id', key: 'reinvoicing.new.errors.taxId' });
    else if (normalizeTaxId(f.taxId) === normalizeTaxId(q.taxId)) list.push({ fieldId: 'reinvoicing-tax-id', key: 'reinvoicing.new.errors.sameTaxId' });
    if (!f.name.trim()) list.push({ fieldId: 'reinvoicing-name', key: 'reinvoicing.new.errors.name' });
    if (!f.address.trim()) list.push({ fieldId: 'reinvoicing-address', key: 'reinvoicing.new.errors.address' });
    if (!f.email.trim()) list.push({ fieldId: 'reinvoicing-email', key: 'reinvoicing.new.errors.email' });
    else if (!isEmail(f.email)) list.push({ fieldId: 'reinvoicing-email', key: 'reinvoicing.new.errors.emailFormat' });
    if (f.acceptorEmail.trim() && !isEmail(f.acceptorEmail)) list.push({ fieldId: 'reinvoicing-acceptor', key: 'reinvoicing.new.errors.acceptorFormat' });
    return list;
  }

  private showErrors(errors: FormError[]): void {
    this.errors.set(errors);
    this.announcer.announce(translate('common.form.invalid'), 'assertive');
    focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
  }

  submit(event: Event): void {
    event.preventDefault();
    const q = this.quote();
    if (!q || this.saving()) return;
    this.saveError.set('');
    const errors = this.validate(q);
    if (errors.length > 0) {
      this.showErrors(errors);
      return;
    }
    this.errors.set([]);
    const f = this.form;
    this.saving.set(true);
    this.service.create({
      invoiceId: q.invoiceId,
      billing: {
        taxId: f.taxId.trim(),
        name: f.name.trim(),
        address: f.address.trim(),
        email: f.email.trim(),
        activity: f.activity.trim() || null,
      },
      acceptorEmail: f.acceptorEmail.trim() || null,
      reason: f.reason.trim() || null,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.saving.set(false);
        this.toast.success(translate('reinvoicing.new.created', { number: detail.request.requestNumber }));
        this.router.navigate(['/reinvoicing', detail.request.id]);
      },
      error: (err) => {
        this.saving.set(false);
        const fields = billingErrorKeys(err).map((k) => BILLING_FIELD_IDS[k]).filter((id): id is string => !!id);
        if (fields.length > 0) {
          this.showErrors(fields.map((fieldId) => ({ fieldId, key: 'reinvoicing.new.errors.server' })));
          return;
        }
        const message = reinvoicingErrorMessage(err, 'reinvoicing.new.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
