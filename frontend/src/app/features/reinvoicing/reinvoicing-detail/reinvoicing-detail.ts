import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReinvoicingService } from '../../../core/services/reinvoicing.service';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { REINVOICING_APPROVAL_FIELD, ReinvoicingDetail } from '../../../core/models/reinvoicing.model';
import { SERVICE_FORM_LIMITS, ServiceRequestAttachment } from '../../../core/models/service-request.model';
import {
  REINVOICING_ACCEPTANCE_STATUS_CLASS,
  REINVOICING_ACCEPTANCE_STATUS_KEYS,
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';
import { fileSize } from '../../service-requests/shared/service-text';
import { ServiceRequestPaymentComponent } from '../../service-requests/shared/service-request-payment';
import { ServiceRequestTimelineComponent } from '../../service-requests/shared/service-request-timeline';
import { ReinvoicingStep, ReinvoicingStepsComponent } from '../reinvoicing-steps';
import { reinvoicingErrorMessage } from '../reinvoicing-text';

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Refacturación IAO, seguimiento (Fase 2, Ola H, M3-11): la factura original y la nueva razón social con el cobro
 * (refacturación y pérdida de IVA). En borrador se adjunta la aprobación de la nueva razón social y se envía aceptando el
 * cobro; luego se pagan ambos cargos juntos (carro o cuenta, M5-01, M5-07) y se sigue la aceptación de la nueva razón
 * social (pendiente, aceptada o rechazada), con reenvío del enlace. La factura nueva se emite solo pagada y aceptada; la
 * original queda reemplazada en las facturas (M7-01). Línea de tiempo con fecha, hora y zona.
 */
@Component({
  selector: 'app-reinvoicing-detail',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    ReinvoicingStepsComponent, ServiceRequestPaymentComponent, ServiceRequestTimelineComponent,
  ],
  templateUrl: './reinvoicing-detail.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .reinvoicing-pre-line { white-space: pre-line; }'],
})
export class ReinvoicingDetailComponent implements OnInit {
  private readonly service = inject(ReinvoicingService);
  private readonly requests = inject(ServiceRequestService);
  readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  id = input.required<string>();

  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly statusClass = SERVICE_REQUEST_STATUS_CLASS;
  readonly acceptanceKeys = REINVOICING_ACCEPTANCE_STATUS_KEYS;
  readonly acceptanceClass = REINVOICING_ACCEPTANCE_STATUS_CLASS;
  readonly fileAccept = SERVICE_FORM_LIMITS.FILE_ACCEPT;

  detail = signal<ReinvoicingDetail | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionMessage = signal('');
  actionError = signal('');

  approvalFile: File | null = null;
  uploading = signal(false);
  acceptTariff = false;
  errors = signal<FormError[]>([]);
  submitting = signal(false);
  resending = signal(false);
  downloading = signal<string | null>(null);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly statusHeading = viewChild<ElementRef<HTMLElement>>('statusHeading');

  requestNumber = computed(() => this.detail()?.request.requestNumber ?? null);

  /** Paso actual: borrador (aprobación y envío), pago y aceptación, o factura emitida. */
  step = computed<ReinvoicingStep>(() => {
    const status = this.detail()?.request.status;
    if (status === 'Draft') return 'submit';
    if (status === 'Completed') return 'issued';
    return 'payAccept';
  });

  /** Total del cobro: refacturación con IVA y pérdida de IVA (es lo que se acepta al enviar). */
  total = computed(() => {
    const r = this.detail()?.reissue;
    return r ? r.feeAmount + r.feeTaxAmount + r.vatLossAmount : 0;
  });

  approvals = computed<ServiceRequestAttachment[]>(() =>
    (this.detail()?.request.attachments ?? []).filter((a) => a.fieldKey === REINVOICING_APPROVAL_FIELD));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(this.detail() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.get(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('reinvoicing.detail.notFound'));
        else this.error.set(reinvoicingErrorMessage(err, 'reinvoicing.detail.loadError'));
      },
    });
  }

  originalNumber(d: ReinvoicingDetail): string {
    return d.reissue.originalSiiNumber ?? d.reissue.originalSourceNumber;
  }

  size(a: ServiceRequestAttachment): string {
    return fileSize(a.sizeBytes);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  errorKey(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  onApprovalFile(event: Event): void {
    this.approvalFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  private fileError(file: File | null): string | null {
    if (!file) return 'reinvoicing.detail.approval.errors.fileRequired';
    if (!SERVICE_FORM_LIMITS.FILE_TYPES.includes(file.type)) return 'reinvoicing.detail.approval.errors.fileType';
    if (file.size > SERVICE_FORM_LIMITS.MAX_FILE_BYTES) return 'reinvoicing.detail.approval.errors.fileSize';
    return null;
  }

  private showErrors(errors: FormError[]): void {
    this.errors.set(errors);
    this.announcer.announce(translate('common.form.invalid'), 'assertive');
    focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
  }

  /** Aprobación de la nueva razón social (obligatoria para enviar). */
  uploadApproval(): void {
    const d = this.detail();
    if (!d || this.uploading()) return;
    const key = this.fileError(this.approvalFile);
    if (key || !this.approvalFile) {
      this.showErrors([{ fieldId: 'reinvoicing-approval-file', key: key ?? 'reinvoicing.detail.approval.errors.fileRequired' }]);
      return;
    }
    this.errors.set([]);
    this.actionError.set('');
    this.uploading.set(true);
    this.service.uploadApproval(d.request.id, this.approvalFile).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (attachment) => {
        this.uploading.set(false);
        this.approvalFile = null;
        const fileInput = document.getElementById('reinvoicing-approval-file') as HTMLInputElement | null;
        if (fileInput) fileInput.value = '';
        this.announcer.announce(translate('reinvoicing.detail.approval.done', { name: attachment.fileName }));
        this.load();
      },
      error: (err) => {
        this.uploading.set(false);
        const message = reinvoicingErrorMessage(err, 'reinvoicing.detail.approval.errors.submit');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Envía con el cobro aceptado: genera los dos cargos y el enlace de aceptación. */
  submit(event: Event): void {
    event.preventDefault();
    const d = this.detail();
    if (!d || this.submitting()) return;
    const errors: FormError[] = [];
    if (!d.reissue.approvalAttached && this.approvals().length === 0) {
      errors.push({ fieldId: 'reinvoicing-approval-file', key: 'reinvoicing.detail.submit.errors.approval' });
    }
    if (!this.acceptTariff) errors.push({ fieldId: 'reinvoicing-accept', key: 'reinvoicing.detail.submit.errors.accept' });
    if (errors.length > 0) {
      this.showErrors(errors);
      return;
    }
    this.errors.set([]);
    this.actionError.set('');
    this.submitting.set(true);
    this.service.submit(d.request.id, this.total()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.submitting.set(false);
        this.detail.set(updated);
        this.acceptTariff = false;
        const message = translate('reinvoicing.detail.submit.done', { number: updated.request.requestNumber, email: updated.reissue.acceptorEmail });
        this.actionMessage.set(message);
        this.announcer.announce(message);
        focusAfterRender(this.injector, () => this.statusHeading()?.nativeElement);
      },
      error: (err) => {
        this.submitting.set(false);
        const message = reinvoicingErrorMessage(err, 'reinvoicing.detail.submit.errors.submit');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Enlace nuevo a la nueva razón social (el anterior deja de funcionar). */
  resend(): void {
    const d = this.detail();
    if (!d || this.resending()) return;
    this.resending.set(true);
    this.actionError.set('');
    this.service.resendAcceptance(d.request.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.resending.set(false);
        this.detail.set(updated);
        const message = translate('reinvoicing.detail.acceptance.resent', { email: updated.reissue.acceptorEmail });
        this.actionMessage.set(message);
        this.announcer.announce(message);
      },
      error: (err) => {
        this.resending.set(false);
        const message = reinvoicingErrorMessage(err, 'reinvoicing.detail.acceptance.resendError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  download(a: ServiceRequestAttachment): void {
    const d = this.detail();
    if (!d || this.downloading()) return;
    this.downloading.set(a.id);
    this.requests.downloadAttachment(d.request.id, a.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, a.fileName);
        this.announcer.announce(translate('reinvoicing.detail.approval.downloaded', { name: a.fileName }));
      },
      error: (err) => {
        this.downloading.set(null);
        const message = reinvoicingErrorMessage(err, 'reinvoicing.detail.approval.downloadError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  onPaymentAdded(): void {
    const d = this.detail();
    if (d) this.announcer.announce(translate('reinvoicing.detail.payment.added', { number: d.request.requestNumber }));
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
