import { Component, DestroyRef, Injector, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { SERVICE_FORM_LIMITS, ServiceRequestDetail } from '../../../core/models/service-request.model';
import { REINVOICING_DEFINITION_CODE } from '../../../core/models/reinvoicing.model';
import { SERVICE_TEAM_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ServiceRequestOverviewComponent } from '../shared/service-request-overview';
import { ServiceRequestPaymentComponent } from '../shared/service-request-payment';
import { fieldLabel, localized, serviceErrorMessage } from '../shared/service-text';

/**
 * Detalle de una solicitud de servicio del cliente: estado, datos, cobro, adjuntos y línea de tiempo (M3-12, M3-13);
 * según el estado, continuar el borrador, pagar el cargo (carro o cuenta, M5-01, M5-07), descargar el documento de
 * salida o anular antes del pago (el cargo generado se retira del carro). Otra organización recibe 404 (NF-05).
 */
@Component({
  selector: 'app-service-request-detail',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent,
    ServiceRequestOverviewComponent, ServiceRequestPaymentComponent,
  ],
  templateUrl: './service-request-detail.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ServiceRequestDetailComponent implements OnInit {
  private readonly service = inject(ServiceRequestService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  id = input.required<string>();

  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly fileAccept = SERVICE_FORM_LIMITS.FILE_ACCEPT;
  /** La refacturación IAO tiene su propia página, con la aceptación de la nueva razón social (M3-11). */
  readonly reinvoicingCode = REINVOICING_DEFINITION_CODE;

  request = signal<ServiceRequestDetail | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  // Anulación
  confirmingCancel = signal(false);
  cancelReason = '';
  cancelling = signal(false);
  cancelError = signal('');

  // Adjuntos del borrador
  uploadField = '';
  uploadFile: File | null = null;
  uploading = signal(false);
  uploadError = signal('');

  /** Campos de archivo del formulario (para adjuntar al borrador). */
  fileFields = computed(() => (this.request()?.inputSchema ?? []).filter((f) => f.type === 'file'));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.get(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (request) => {
        this.request.set(request);
        if (!this.uploadField) this.uploadField = this.fileFields()[0]?.key ?? '';
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('serviceRequests.detail.notFound'));
        else this.error.set(serviceErrorMessage(err, 'serviceRequests.detail.loadError'));
        this.loading.set(false);
      },
    });
  }

  name(r: ServiceRequestDetail): string {
    return localized(this.locale.lang(), r.nameEs, r.nameEn);
  }

  fileFieldLabel(key: string): string {
    const field = this.fileFields().find((f) => f.key === key);
    return field ? fieldLabel(field, this.locale.lang()) : key;
  }

  // Anulación (antes del pago)
  startCancel(): void {
    this.cancelReason = '';
    this.cancelError.set('');
    this.confirmingCancel.set(true);
    focusAfterRender(this.injector, () => document.getElementById('srv-cancel-title'));
  }

  stopCancel(): void {
    this.confirmingCancel.set(false);
    focusAfterRender(this.injector, () => document.getElementById('srv-cancel-start'));
  }

  confirmCancel(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r || this.cancelling()) return;
    this.cancelling.set(true);
    this.cancelError.set('');
    this.service.cancel(r.id, this.cancelReason.trim() || null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.cancelling.set(false);
        this.confirmingCancel.set(false);
        this.request.set(updated);
        this.announcer.announce(translate('serviceRequests.detail.cancel.done', { number: updated.requestNumber }));
        focusAfterRender(this.injector, () => document.getElementById('srv-detail-title'));
      },
      error: (err) => {
        this.cancelling.set(false);
        const message = serviceErrorMessage(err, 'serviceRequests.detail.cancel.error');
        this.cancelError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  // Adjuntos del borrador
  onUploadFile(event: Event): void {
    this.uploadFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  upload(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r || this.uploading()) return;
    this.uploadError.set('');
    const file = this.uploadFile;
    let errorKey = '';
    if (!this.uploadField) errorKey = 'serviceRequests.detail.upload.fieldRequired';
    else if (!file) errorKey = 'serviceRequests.detail.upload.fileRequired';
    else if (!SERVICE_FORM_LIMITS.FILE_TYPES.includes(file.type)) errorKey = 'serviceRequests.detail.upload.fileType';
    else if (file.size > SERVICE_FORM_LIMITS.MAX_FILE_BYTES) errorKey = 'serviceRequests.detail.upload.fileSize';
    if (errorKey || !file) {
      const message = translate(errorKey || 'serviceRequests.detail.upload.fileRequired');
      this.uploadError.set(message);
      this.announcer.announce(message, 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('srv-upload-file'));
      return;
    }
    this.uploading.set(true);
    this.service.uploadAttachment(r.id, this.uploadField, file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (attachment) => {
        this.uploading.set(false);
        this.uploadFile = null;
        const fileInput = document.getElementById('srv-upload-file') as HTMLInputElement | null;
        if (fileInput) fileInput.value = '';
        this.announcer.announce(translate('serviceRequests.detail.upload.done', { name: attachment.fileName }));
        this.load();
      },
      error: (err) => {
        this.uploading.set(false);
        const message = serviceErrorMessage(err, 'serviceRequests.detail.upload.error');
        this.uploadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  onPaymentAdded(): void {
    const r = this.request();
    if (r) this.announcer.announce(translate('serviceRequests.detail.payment.added', { number: r.requestNumber }));
  }
}
