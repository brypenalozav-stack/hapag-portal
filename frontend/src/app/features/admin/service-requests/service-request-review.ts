import { Component, DestroyRef, Injector, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { AdminServiceRequestService } from '../../../core/services/service-request.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { SERVICE_FORM_LIMITS, ServiceRequestDetail } from '../../../core/models/service-request.model';
import { RELEASE_LETTER_DEFINITION_CODE } from '../../../core/models/document.model';
import { SERVICE_TEAM_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ServiceRequestOverviewComponent } from '../../service-requests/shared/service-request-overview';
import { isOutput, localized, serviceErrorMessage } from '../../service-requests/shared/service-text';
import { ReleaseLetterReviewPanelComponent } from './release-letter-review-panel';

/** Acción del equipo interno sobre la solicitud. */
type ReviewAction = 'approve' | 'reject' | 'complete' | 'note' | 'output';

/** Error de un campo de las acciones (motivo, nota o archivo). */
interface ActionError {
  fieldId: string;
  key: string;
}

/**
 * Revisión de una solicitud en la bandeja interna (ED, Customer Service): el mismo detalle que ve el cliente más las
 * acciones según el estado: aprobar (genera el cargo con la tarifa aceptada al enviar) o rechazar con motivo visible
 * para el cliente (M3-09), subir el documento de salida y completar la atención (exigido si la definición lo pide), y
 * agregar notas a la línea de tiempo. Cada cambio de estado se notifica al cliente. Fase 2, Ola J: la carta de liberación y
 * desconsolidado (M6-08) agrega su panel con el consignatario, el transportista, el TATC y el Counter; aprobarla la emite.
 */
@Component({
  selector: 'app-service-request-review',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent, ServiceRequestOverviewComponent,
    ReleaseLetterReviewPanelComponent,
  ],
  templateUrl: './service-request-review.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ServiceRequestReviewComponent implements OnInit {
  private readonly service = inject(AdminServiceRequestService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  id = input.required<string>();

  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly fileAccept = SERVICE_FORM_LIMITS.FILE_ACCEPT;
  readonly releaseLetterCode = RELEASE_LETTER_DEFINITION_CODE;

  request = signal<ServiceRequestDetail | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  approveNotes = '';
  rejectReason = '';
  completeNotes = '';
  note = '';
  outputFile: File | null = null;

  busy = signal<ReviewAction | null>(null);
  actionError = signal('');
  fieldError = signal<ActionError | null>(null);
  done = signal('');

  hasOutput = computed(() => (this.request()?.attachments ?? []).some((a) => isOutput(a.fieldKey)));
  /** Se puede subir el documento de salida mientras la solicitud no termina. */
  canUploadOutput = computed(() => {
    const s = this.request()?.status;
    return !!s && !['Rejected', 'Completed', 'Cancelled', 'Draft'].includes(s);
  });

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
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 404) this.error.set(translate('admin.serviceRequests.review.notFound'));
        else this.error.set(serviceErrorMessage(err, 'admin.serviceRequests.review.loadError'));
        this.loading.set(false);
      },
    });
  }

  name(r: ServiceRequestDetail): string {
    return localized(this.locale.lang(), r.nameEs, r.nameEn);
  }

  isInvalid(fieldId: string): boolean {
    return this.fieldError()?.fieldId === fieldId;
  }

  private invalid(fieldId: string, key: string): void {
    this.fieldError.set({ fieldId, key });
    this.announcer.announce(translate(key), 'assertive');
    focusAfterRender(this.injector, () => document.getElementById(fieldId));
  }

  onOutputFile(event: Event): void {
    this.outputFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  private run(action: ReviewAction, request$: Observable<unknown>, doneKey: string, onDone: () => void): void {
    const r = this.request();
    if (!r || this.busy()) return;
    this.busy.set(action);
    this.actionError.set('');
    this.fieldError.set(null);
    this.done.set('');
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.busy.set(null);
        onDone();
        const message = translate(doneKey, { number: r.requestNumber });
        this.done.set(message);
        this.announcer.announce(message);
        if (result && typeof result === 'object' && 'timeline' in result) {
          this.request.set(result as ServiceRequestDetail);
        } else {
          this.load();
        }
        focusAfterRender(this.injector, () => document.getElementById('srv-review-done'));
      },
      error: (err) => {
        this.busy.set(null);
        const message = serviceErrorMessage(err, 'admin.serviceRequests.review.actionError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  approve(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r) return;
    this.run('approve', this.service.approve(r.id, this.approveNotes.trim() || null), 'admin.serviceRequests.review.approved', () => (this.approveNotes = ''));
  }

  reject(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r) return;
    const reason = this.rejectReason.trim();
    if (!reason) {
      this.invalid('srv-reject-reason', 'admin.serviceRequests.review.reject.required');
      return;
    }
    this.run('reject', this.service.reject(r.id, reason), 'admin.serviceRequests.review.rejected', () => (this.rejectReason = ''));
  }

  uploadOutput(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r) return;
    const file = this.outputFile;
    if (!file) {
      this.invalid('srv-output-file', 'admin.serviceRequests.review.output.required');
      return;
    }
    if (!SERVICE_FORM_LIMITS.FILE_TYPES.includes(file.type)) {
      this.invalid('srv-output-file', 'admin.serviceRequests.review.output.fileType');
      return;
    }
    if (file.size > SERVICE_FORM_LIMITS.MAX_FILE_BYTES) {
      this.invalid('srv-output-file', 'admin.serviceRequests.review.output.fileSize');
      return;
    }
    this.run('output', this.service.uploadOutput(r.id, file), 'admin.serviceRequests.review.output.done', () => {
      this.outputFile = null;
      const fileInput = document.getElementById('srv-output-file') as HTMLInputElement | null;
      if (fileInput) fileInput.value = '';
    });
  }

  complete(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r) return;
    if (r.requiresOutputDocument && !this.hasOutput()) {
      this.invalid('srv-output-file', 'admin.serviceRequests.review.complete.outputRequired');
      return;
    }
    this.run('complete', this.service.complete(r.id, this.completeNotes.trim() || null), 'admin.serviceRequests.review.completed', () => (this.completeNotes = ''));
  }

  addNote(event: Event): void {
    event.preventDefault();
    const r = this.request();
    if (!r) return;
    const note = this.note.trim();
    if (!note) {
      this.invalid('srv-note', 'admin.serviceRequests.review.note.required');
      return;
    }
    this.run('note', this.service.addNote(r.id, note), 'admin.serviceRequests.review.note.done', () => (this.note = ''));
  }
}
