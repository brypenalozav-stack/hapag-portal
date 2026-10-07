import { Component, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { FreightCertificateContext, FreightCertificateResult } from '../../../core/models/document.model';
import { SERVICE_REQUEST_STATUS_CLASS, SERVICE_REQUEST_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { FreightCertificateDialogComponent } from './freight-certificate-dialog';

/**
 * Certificado de flete de importación de Bolivia (M6-02) en los documentos del embarque: el flete que se certifica, las
 * solicitudes de la organización con su estado y la solicitud con el diálogo. Primera entrega de Fase 2, sin pago ni
 * carro (decisión Q1 de la v4): la solicitud registra los datos y el certificado firmado se emite en el momento.
 */
@Component({
  selector: 'app-freight-certificate-panel',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    FreightCertificateDialogComponent,
  ],
  templateUrl: './freight-certificate-panel.html',
})
export class FreightCertificatePanelComponent {
  private readonly service = inject(DocumentService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();
  /** Huso del país de la operación (NF-22). */
  timeZone = input<string | null>(null);
  /** Perfil que opera de una organización cliente (la sección ya lo calcula). */
  canOperate = input(false);
  /** Se emite con el certificado emitido (la sección actualiza el repositorio). */
  issued = output<FreightCertificateResult>();

  readonly statusKeys = SERVICE_REQUEST_STATUS_KEYS;
  readonly statusClass = SERVICE_REQUEST_STATUS_CLASS;

  data = signal<FreightCertificateContext | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  dialogOpen = signal(false);
  private opener: HTMLElement | null = null;

  constructor() {
    effect(() => {
      this.blNumber();
      untracked(() => this.load());
    });
  }

  load(): void {
    this.loading.set(this.data() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getFreightCertificate(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.data.set(null);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(documentErrorMessage(err, 'documents.freight.loadError'));
      },
    });
  }

  open(event: Event): void {
    this.opener = event.currentTarget as HTMLElement;
    this.dialogOpen.set(true);
  }

  /** Al cerrar el diálogo: si se emitió, se actualizan las solicitudes y se avisa a la sección. */
  onClosed(result: FreightCertificateResult | null): void {
    this.dialogOpen.set(false);
    setTimeout(() => this.opener?.focus());
    if (!result) return;
    this.load();
    this.issued.emit(result);
  }
}
