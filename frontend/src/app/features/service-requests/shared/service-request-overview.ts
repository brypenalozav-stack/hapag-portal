import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AdminServiceRequestService, ServiceRequestService } from '../../../core/services/service-request.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ServiceRequestAttachment, ServiceRequestDetail } from '../../../core/models/service-request.model';
import {
  SERVICE_REQUEST_STATUS_CLASS,
  SERVICE_REQUEST_STATUS_KEYS,
  SERVICE_TEAM_KEYS,
  SHIPMENT_OPERATION_KEYS,
} from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { saveBlob } from '../../../shared/save-blob';
import { ServiceInputValuesComponent } from './service-input-values';
import { ServiceQuoteComponent } from './service-quote';
import { ServiceRequestTimelineComponent } from './service-request-timeline';
import { fieldLabel, fileSize, isOutput, serviceErrorMessage } from './service-text';
import { ToastService } from '../../../core/services/toast.service';

/**
 * Detalle de una solicitud de servicio, común al cliente y a la bandeja interna: estado y equipo, embarque, motivo del
 * rechazo o notas del equipo, datos ingresados, facturación, cobro fijado al enviar (tramo vigente en ese momento,
 * NF-22), adjuntos y documento de salida con descarga, y la línea de tiempo con fecha, hora y zona.
 */
@Component({
  selector: 'app-service-request-overview',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe, ServiceInputValuesComponent, ServiceQuoteComponent, ServiceRequestTimelineComponent],
  templateUrl: './service-request-overview.html',
  styleUrl: './service-request-overview.scss',
})
export class ServiceRequestOverviewComponent {
  private readonly clientService = inject(ServiceRequestService);
  private readonly adminService = inject(AdminServiceRequestService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  request = input.required<ServiceRequestDetail>();
  /** Vista de la bandeja interna: descarga por /admin/service-requests y muestra la organización solicitante. */
  internal = input(false);

  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly statusClass = SERVICE_REQUEST_STATUS_CLASS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;

  downloading = signal<string | null>(null);
  downloadError = signal('');

  /** Adjuntos del cliente y documento de salida del equipo interno, por separado. */
  outputs = computed(() => this.request().attachments.filter((a) => isOutput(a.fieldKey)));
  clientAttachments = computed(() => this.request().attachments.filter((a) => !isOutput(a.fieldKey)));

  attachmentField(a: ServiceRequestAttachment): string {
    const field = this.request().inputSchema.find((f) => f.key === a.fieldKey);
    return field ? fieldLabel(field, this.locale.lang()) : a.fieldKey;
  }

  size(a: ServiceRequestAttachment): string {
    return fileSize(a.sizeBytes);
  }

  download(a: ServiceRequestAttachment): void {
    if (this.downloading()) return;
    const r = this.request();
    this.downloading.set(a.id);
    this.downloadError.set('');
    const download$ = this.internal()
      ? this.adminService.downloadAttachment(r.id, a.id)
      : this.clientService.downloadAttachment(r.id, a.id);
    download$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, a.fileName);
        this.toast.success(translate('serviceRequests.detail.attachments.downloaded', { name: a.fileName }));
      },
      error: (err) => {
        this.downloading.set(null);
        const message = serviceErrorMessage(err, 'serviceRequests.detail.attachments.downloadError');
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
