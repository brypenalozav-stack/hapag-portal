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
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  FreightCertificateContext,
  FreightCertificatePurpose,
  FreightCertificateResult,
} from '../../../core/models/document.model';
import { FREIGHT_PURPOSE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';
import { ToastService } from '../../../core/services/toast.service';

/** Largos máximos del contrato (el servidor los vuelve a validar). */
const LIMITS = { NAME: 200, TAX_ID: 30, RECIPIENT: 200, NOTES: 1000 } as const;

type FreightField = 'consigneeName' | 'consigneeTaxId' | 'purpose' | 'recipient' | 'notes';

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Solicitud del certificado de flete (M6-02, Bolivia importación): datos del consignatario (precargados con los del BL,
 * WCAG 3.3.7), finalidad, destinatario y observaciones. Sin pago ni carro en esta entrega: al enviar, el portal registra
 * la solicitud (número `SRV-`, completada), emite el PDF firmado, lo publica en el repositorio (M6-09) y lo envía al
 * correo registrado de la organización. El resultado ofrece la descarga. Diálogo modal nativo (`<dialog>`).
 */
@Component({
  selector: 'app-freight-certificate-dialog',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe],
  templateUrl: './freight-certificate-dialog.html',
  styleUrl: '../document-dialog.scss',
})
export class FreightCertificateDialogComponent implements AfterViewInit {
  private readonly service = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  context = input.required<FreightCertificateContext>();
  timeZone = input<string | null>(null);
  /** Se emite al cerrar con el certificado emitido, o null si no se emitió. */
  closed = output<FreightCertificateResult | null>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  readonly purposeKeys = FREIGHT_PURPOSE_KEYS;
  readonly limits = LIMITS;

  values = signal<Record<FreightField, string>>({ consigneeName: '', consigneeTaxId: '', purpose: '', recipient: '', notes: '' });
  sendEmail = signal(true);
  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');
  result = signal<FreightCertificateResult | null>(null);
  downloadError = signal('');

  errors = computed<FormError[]>(() => {
    if (!this.submitted()) return [];
    const v = this.values();
    const list: FormError[] = [];
    if (!v.consigneeName.trim()) list.push({ fieldId: 'freight-consignee-name', key: 'documents.freight.errors.nameRequired' });
    if (!v.consigneeTaxId.trim()) list.push({ fieldId: 'freight-consignee-tax-id', key: 'documents.freight.errors.taxIdRequired' });
    if (!v.purpose) list.push({ fieldId: 'freight-purpose', key: 'documents.freight.errors.purposeRequired' });
    return list;
  });

  ngAfterViewInit(): void {
    const c = this.context();
    this.values.update((v) => ({ ...v, consigneeName: c.consignee.name ?? '', consigneeTaxId: c.consignee.taxId ?? '' }));
    this.dialog().nativeElement.showModal();
  }

  fieldError(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  onInput(field: FreightField, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement).value;
    this.values.update((v) => ({ ...v, [field]: value }));
  }

  onSendEmail(event: Event): void {
    this.sendEmail.set((event.target as HTMLInputElement).checked);
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) return;
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const v = this.values();
    this.submitting.set(true);
    this.service.requestFreightCertificate(this.context().blNumber, {
      consigneeName: v.consigneeName.trim(),
      consigneeTaxId: v.consigneeTaxId.trim(),
      purpose: v.purpose as FreightCertificatePurpose,
      recipient: v.recipient.trim() || null,
      notes: v.notes.trim() || null,
      sendEmail: this.sendEmail(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.result.set(result);
        this.announcer.announce(this.resultText(result));
        focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
      },
      error: (err) => {
        this.submitting.set(false);
        const message = documentErrorMessage(err, 'documents.freight.errors.submit');
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Número del certificado, solicitud completada y a qué correos se envió. */
  resultText(result: FreightCertificateResult): string {
    const issued = translate('documents.freight.result.issued', {
      number: result.document.documentNumber,
      request: result.request.requestNumber,
    });
    const delivery = result.sentTo.length > 0
      ? translate('documents.freight.result.sent', { emails: result.sentTo.join(', ') })
      : translate('documents.freight.result.notSent');
    return `${issued} ${delivery}`;
  }

  /** Descarga el PDF firmado con la sesión del usuario; el servidor registra la descarga (NF-14). */
  download(): void {
    const result = this.result();
    if (!result) return;
    const doc = result.document;
    this.downloadError.set('');
    this.service.download(this.context().blNumber, doc.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, doc.fileName);
        this.toast.success(translate('documents.section.downloaded', { number: doc.documentNumber }));
      },
      error: (err) => {
        const message = documentErrorMessage(err, 'documents.section.downloadError');
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  /** Cierre nativo (botón o Escape): avisa al contenedor si se emitió el certificado. */
  onClosed(): void {
    this.closed.emit(this.result());
  }
}
