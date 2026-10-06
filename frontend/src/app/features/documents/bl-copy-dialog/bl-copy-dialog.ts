import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { DocumentDelivery } from '../../../core/models/document.model';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';

type CopyVersion = 'valued' | 'nonValued';

/** Nombre y ayuda de cada versión de la copia. */
const VERSION_KEYS: Record<CopyVersion, { name: string; help: string }> = {
  valued: { name: 'documents.blCopy.version.valued', help: 'documents.blCopy.version.valuedHelp' },
  nonValued: { name: 'documents.blCopy.version.nonValued', help: 'documents.blCopy.version.nonValuedHelp' },
};

/**
 * Solicitud de la copia del BL (M6-05): solo se ofrecen las versiones que la matriz de M1-11 habilita al
 * usuario (el shipper, solo la no valorada); el servidor lo vuelve a validar. La copia se registra en el
 * repositorio del embarque (M6-09) y se envía al correo registrado de la organización; cada solicitud y
 * envío queda registrado (NF-14). Diálogo modal nativo (`<dialog>`): atrapa el foco y se cierra con Escape.
 */
@Component({
  selector: 'app-bl-copy-dialog',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './bl-copy-dialog.html',
  styleUrl: '../document-dialog.scss',
})
export class BlCopyDialogComponent implements AfterViewInit {
  private readonly service = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();
  canRequestValued = input(false);
  canRequestNonValued = input(false);
  /** Se emite al cerrar con la copia emitida, o null si no se emitió. */
  closed = output<DocumentDelivery | null>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  readonly versionKeys = VERSION_KEYS;

  /** Versiones habilitadas para el usuario, en el orden en que se ofrecen. */
  versions = computed<CopyVersion[]>(() => [
    ...(this.canRequestValued() ? ['valued' as const] : []),
    ...(this.canRequestNonValued() ? ['nonValued' as const] : []),
  ]);

  version = signal<CopyVersion | ''>('');
  sendEmail = signal(true);
  submitting = signal(false);
  submitError = signal('');
  result = signal<DocumentDelivery | null>(null);
  downloadError = signal('');

  ngAfterViewInit(): void {
    // Con una sola versión habilitada queda elegida.
    const versions = this.versions();
    if (versions.length === 1) this.version.set(versions[0]);
    this.dialog().nativeElement.showModal();
  }

  onVersion(version: CopyVersion): void {
    this.version.set(version);
    this.submitError.set('');
  }

  onSendEmail(event: Event): void {
    this.sendEmail.set((event.target as HTMLInputElement).checked);
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) return;
    const version = this.version();
    if (!version) {
      const message = translate('documents.blCopy.errors.versionRequired');
      this.submitError.set(message);
      this.announcer.announce(message, 'assertive');
      document.getElementById(`bl-copy-${this.versions()[0]}`)?.focus();
      return;
    }
    this.submitting.set(true);
    this.submitError.set('');
    this.service.requestBlCopy(this.blNumber(), { valued: version === 'valued', sendEmail: this.sendEmail() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (delivery) => {
          this.submitting.set(false);
          this.result.set(delivery);
          this.announcer.announce(this.resultText(delivery));
          focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
        },
        error: (err) => {
          this.submitting.set(false);
          const message = documentErrorMessage(err, 'documents.blCopy.errors.submit');
          this.submitError.set(message);
          this.announcer.announce(message, 'assertive');
        },
      });
  }

  /** Texto del resultado: número, registro en el repositorio y, si se envió, a qué correos. */
  resultText(delivery: DocumentDelivery): string {
    const number = delivery.document.documentNumber;
    return delivery.sentTo.length > 0
      ? translate('documents.blCopy.result.sent', { number, emails: delivery.sentTo.join(', ') })
      : translate('documents.blCopy.result.registered', { number });
  }

  download(): void {
    const delivery = this.result();
    if (!delivery) return;
    const doc = delivery.document;
    this.downloadError.set('');
    this.service.download(this.blNumber(), doc.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, doc.fileName);
        this.announcer.announce(translate('documents.section.downloaded', { number: doc.documentNumber }));
      },
      error: (err) => {
        const message = documentErrorMessage(err, 'documents.section.downloadError');
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  /** Cierre nativo (botón o Escape): avisa al contenedor si se emitió la copia. */
  onClosed(): void {
    this.closed.emit(this.result());
  }
}
