import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DepositProofService } from '../../../core/services/deposit-proof.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { DEPOSIT_PROOF_LIMITS, DepositProof, PaymentDepositProofs } from '../../../core/models/deposit-proof.model';
import { DEPOSIT_PROOF_STATUS_CLASS, DEPOSIT_PROOF_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../../shared/payment-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { saveBlob } from '../../../shared/save-blob';
import { fileSize } from '../../service-requests/shared/service-text';

interface ProofForm {
  bankName: string;
  bankReference: string;
  depositDate: string;
  depositAmount: string;
  notes: string;
}

interface FormError {
  fieldId: string;
  key: string;
}

function emptyForm(): ProofForm {
  return { bankName: '', bankReference: '', depositDate: '', depositAmount: '', notes: '' };
}

/**
 * Comprobante de depósito de un pago con boleta (Fase 2, Ola H, M5-06): el cliente adjunta el comprobante del abono (PDF,
 * PNG o JPEG de hasta 10 MB) con los datos del depósito; si la boleta aún no se emitió, adjuntarlo la emite (M5-02).
 * Muestra cada comprobante con su revisión: en revisión, verificado o rechazado con el motivo, y tras un rechazo permite
 * subir uno nuevo. Finanzas verifica o rechaza desde su bandeja. Se usa en el resultado del pago y en el historial.
 */
@Component({
  selector: 'app-deposit-proofs',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './deposit-proofs.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .proof-pre-line { white-space: pre-line; }'],
})
export class DepositProofsComponent implements OnInit {
  private readonly service = inject(DepositProofService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  paymentId = input.required<string>();
  /** Se emite tras adjuntar un comprobante (el estado del pago puede haber cambiado). */
  changed = output<PaymentDepositProofs>();

  readonly statusKeys = DEPOSIT_PROOF_STATUS_KEYS;
  readonly statusClass = DEPOSIT_PROOF_STATUS_CLASS;
  readonly fileAccept = DEPOSIT_PROOF_LIMITS.FILE_ACCEPT;
  readonly maxText = DEPOSIT_PROOF_LIMITS.MAX_TEXT;
  readonly maxNotes = DEPOSIT_PROOF_LIMITS.MAX_NOTES;

  data = signal<PaymentDepositProofs | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  form: ProofForm = emptyForm();
  file: File | null = null;
  errors = signal<FormError[]>([]);
  uploading = signal(false);
  uploadError = signal('');
  downloading = signal<string | null>(null);
  actionError = signal('');

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  /** Último comprobante enviado (el que define qué se muestra arriba). */
  latest = computed<DepositProof | null>(() => {
    const proofs = this.data()?.proofs ?? [];
    return proofs.length === 0 ? null : [...proofs].sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt))[0];
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(this.data() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.get(this.paymentId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'depositProofs.errors.load'));
      },
    });
  }

  size(proof: DepositProof): string {
    return fileSize(proof.sizeBytes);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  errorKey(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  onFile(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  private validate(): FormError[] {
    const list: FormError[] = [];
    const file = this.file;
    if (!file) list.push({ fieldId: 'proof-file', key: 'depositProofs.form.errors.fileRequired' });
    else if (!DEPOSIT_PROOF_LIMITS.FILE_TYPES.includes(file.type)) list.push({ fieldId: 'proof-file', key: 'depositProofs.form.errors.fileType' });
    else if (file.size > DEPOSIT_PROOF_LIMITS.MAX_FILE_BYTES) list.push({ fieldId: 'proof-file', key: 'depositProofs.form.errors.fileSize' });
    const amount = this.form.depositAmount.trim().replace(',', '.');
    if (amount && !(/^\d+(\.\d{1,2})?$/.test(amount) && Number(amount) > 0)) {
      list.push({ fieldId: 'proof-amount', key: 'depositProofs.form.errors.amount' });
    }
    return list;
  }

  upload(event: Event): void {
    event.preventDefault();
    if (this.uploading()) return;
    this.uploadError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0 || !this.file) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.uploading.set(true);
    const name = this.file.name;
    this.service.upload(this.paymentId(), {
      file: this.file,
      bankName: this.form.bankName,
      bankReference: this.form.bankReference,
      depositDate: this.form.depositDate,
      depositAmount: this.form.depositAmount.trim().replace(',', '.'),
      notes: this.form.notes,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.uploading.set(false);
        this.data.set(data);
        this.form = emptyForm();
        this.file = null;
        const fileInput = document.getElementById('proof-file') as HTMLInputElement | null;
        if (fileInput) fileInput.value = '';
        this.announcer.announce(translate('depositProofs.form.done', { name }));
        this.changed.emit(data);
      },
      error: (err) => {
        this.uploading.set(false);
        const message = paymentErrorMessage(err, 'depositProofs.form.errors.submit');
        this.uploadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  download(proof: DepositProof): void {
    if (this.downloading()) return;
    this.downloading.set(proof.id);
    this.actionError.set('');
    this.service.download(proof.paymentId, proof.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, proof.fileName);
        this.announcer.announce(translate('depositProofs.downloaded', { name: proof.fileName }));
      },
      error: (err) => {
        this.downloading.set(null);
        const message = paymentErrorMessage(err, 'depositProofs.downloadError');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
