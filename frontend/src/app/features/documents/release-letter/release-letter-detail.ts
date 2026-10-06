import { Component, DestroyRef, Injector, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { ServiceRequestService } from '../../../core/services/service-request.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ReleaseLetterRequest } from '../../../core/models/document.model';
import { LEGAL_ENTITY_TYPE_KEYS, SERVICE_TEAM_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';
import { ServiceRequestOverviewComponent } from '../../service-requests/shared/service-request-overview';
import { ReleaseLetterTatcComponent } from './release-letter-tatc';

/**
 * Seguimiento de una carta de liberación y desconsolidado (M6-08): estado (pendiente de aprobación de Customer Service,
 * emitida, rechazada o anulada), tipo de sociedad y transportista, el TATC de las unidades al enviar y al aprobar
 * (M2-09), la carta emitida para descargar (también en el repositorio, M6-09), la anulación antes de la aprobación y el
 * detalle común de la solicitud con su línea de tiempo. Otra organización recibe 404 (NF-05).
 */
@Component({
  selector: 'app-release-letter-detail',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    ServiceRequestOverviewComponent, ReleaseLetterTatcComponent,
  ],
  templateUrl: './release-letter-detail.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ReleaseLetterDetailComponent implements OnInit {
  private readonly service = inject(DocumentService);
  private readonly requests = inject(ServiceRequestService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  id = input.required<string>();

  readonly entityKeys = LEGAL_ENTITY_TYPE_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;

  letter = signal<ReleaseLetterRequest | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  downloading = signal(false);
  downloadError = signal('');

  confirmingCancel = signal(false);
  cancelReason = '';
  cancelling = signal(false);
  cancelError = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(this.letter() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getReleaseLetterRequest(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (letter) => {
        this.letter.set(letter);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('documents.releaseLetter.detail.notFound'));
        else this.error.set(documentErrorMessage(err, 'documents.releaseLetter.detail.loadError'));
      },
    });
  }

  /** Descarga la carta emitida con la sesión del usuario; el servidor registra la descarga (NF-14). */
  download(): void {
    const l = this.letter();
    const doc = l?.document;
    if (!l || !doc || this.downloading()) return;
    this.downloading.set(true);
    this.downloadError.set('');
    this.service.download(l.request.blNumber, doc.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(false);
        saveBlob(blob, doc.fileName);
        this.announcer.announce(translate('documents.section.downloaded', { number: doc.documentNumber }));
      },
      error: (err) => {
        this.downloading.set(false);
        const message = documentErrorMessage(err, 'documents.section.downloadError');
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  // Anulación antes de la aprobación (POST /service-requests/{id}/cancel)
  startCancel(): void {
    this.cancelReason = '';
    this.cancelError.set('');
    this.confirmingCancel.set(true);
    focusAfterRender(this.injector, () => document.getElementById('release-cancel-title'));
  }

  stopCancel(): void {
    this.confirmingCancel.set(false);
    focusAfterRender(this.injector, () => document.getElementById('release-cancel-start'));
  }

  confirmCancel(event: Event): void {
    event.preventDefault();
    const l = this.letter();
    if (!l || this.cancelling()) return;
    this.cancelling.set(true);
    this.cancelError.set('');
    this.requests.cancel(l.request.id, this.cancelReason.trim() || null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.cancelling.set(false);
        this.confirmingCancel.set(false);
        this.letter.set({ ...l, request: updated });
        this.announcer.announce(translate('documents.releaseLetter.detail.cancelled', { number: updated.requestNumber }));
        focusAfterRender(this.injector, () => document.getElementById('release-detail-title'));
      },
      error: (err) => {
        this.cancelling.set(false);
        const message = documentErrorMessage(err, 'documents.releaseLetter.detail.cancelError');
        this.cancelError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
