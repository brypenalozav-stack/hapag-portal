import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReinvoicingService } from '../../../core/services/reinvoicing.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ReinvoicingAcceptanceView } from '../../../core/models/reinvoicing.model';
import { REINVOICING_ACCEPTANCE_STATUS_CLASS, REINVOICING_ACCEPTANCE_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { reinvoicingErrorMessage } from '../reinvoicing-text';

type Answer = '' | 'accept' | 'decline';

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Aceptación del cobro de una refacturación IAO por la nueva razón social (Fase 2, Ola H, M3-11), sin sesión: el enlace
 * de un solo uso llega al correo de la nueva razón social. Muestra la factura original, la nueva razón social y el cobro
 * (refacturación y pérdida de IVA); quien responde acepta o rechaza declarando su nombre y RUT (quedan registrados con
 * fecha, hora y dirección de origen). La factura nueva se emite solo con la aceptación y el pago.
 */
@Component({
  selector: 'app-reinvoicing-acceptance',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './reinvoicing-acceptance.html',
  styles: [':host { display: block; max-width: 60rem; margin: 0 auto; padding: calc(var(--navbar-height) + 1.5rem) 1rem 1.5rem; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ReinvoicingAcceptanceComponent implements OnInit {
  private readonly service = inject(ReinvoicingService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  token = input.required<string>();

  readonly statusKeys = REINVOICING_ACCEPTANCE_STATUS_KEYS;
  readonly statusClass = REINVOICING_ACCEPTANCE_STATUS_CLASS;

  view = signal<ReinvoicingAcceptanceView | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  answer: Answer = '';
  name = '';
  taxId = '';
  reason = '';
  errors = signal<FormError[]>([]);
  sending = signal(false);
  sendError = signal('');

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getAcceptance(this.token()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (view) => {
        this.view.set(view);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('common.reinvoicingErrors.acceptanceNotFound'));
        else this.error.set(reinvoicingErrorMessage(err, 'reinvoicing.acceptance.loadError'));
      },
    });
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  errorKey(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  private validate(): FormError[] {
    const list: FormError[] = [];
    if (!this.answer) list.push({ fieldId: 'acceptance-accept', key: 'reinvoicing.acceptance.errors.answer' });
    if (!this.name.trim()) list.push({ fieldId: 'acceptance-name', key: 'reinvoicing.acceptance.errors.name' });
    if (!this.taxId.trim()) list.push({ fieldId: 'acceptance-tax-id', key: 'reinvoicing.acceptance.errors.taxId' });
    return list;
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.sending()) return;
    this.sendError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.sending.set(true);
    this.service.respondAcceptance(this.token(), {
      accept: this.answer === 'accept',
      name: this.name.trim(),
      taxId: this.taxId.trim(),
      reason: this.reason.trim() || null,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (view) => {
        this.sending.set(false);
        this.view.set(view);
        this.announcer.announce(translate(view.acceptanceStatus === 'Accepted' ? 'reinvoicing.acceptance.result.accepted' : 'reinvoicing.acceptance.result.declined',
          { number: view.requestNumber }));
        focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
      },
      error: (err) => {
        this.sending.set(false);
        const message = reinvoicingErrorMessage(err, 'reinvoicing.acceptance.errors.submit');
        this.sendError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
