import { Component, DestroyRef, ElementRef, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationService } from '../../../core/services/organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ORGANIZATION_DOCUMENT_CONTENT_TYPES,
  ORGANIZATION_DOCUMENT_MAX_BYTES,
  ORGANIZATION_DOCUMENT_TYPES,
  OrganizationDocument,
  OrganizationDocumentType,
} from '../../../core/models/organization.model';
import { ORGANIZATION_DOCUMENT_TYPE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { ToastService } from '../../../core/services/toast.service';

/**
 * Documentación de respaldo del registro (M1-07): la carga es un paso manual y queda registro
 * de lo adjuntado. Formatos PDF, PNG o JPEG de hasta 10 MB.
 */
@Component({
  selector: 'app-organization-documents',
  standalone: true,
  imports: [
    ReactiveFormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './organization-documents.html',
})
export class OrganizationDocumentsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(OrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  readonly documentTypes = ORGANIZATION_DOCUMENT_TYPES;
  readonly documentTypeKeys = ORGANIZATION_DOCUMENT_TYPE_KEYS;
  readonly accept = ORGANIZATION_DOCUMENT_CONTENT_TYPES.join(',');

  documents = signal<OrganizationDocument[]>([]);
  loading = signal(true);
  loadFailed = signal(false);

  form = this.fb.nonNullable.group({
    documentType: ['' as OrganizationDocumentType | '', Validators.required],
  });
  file = signal<File | null>(null);
  /** Error del archivo: falta, formato o tamaño. */
  fileError = signal('');
  fileTouched = signal(false);
  uploading = signal(false);
  uploadError = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getDocuments().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.loading.set(false);
      },
      error: (err) => {
        this.documents.set([]);
        this.loadFailed.set(isServiceUnavailable(err));
        this.loading.set(false);
      },
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
    this.fileTouched.set(true);
    this.validateFile();
  }

  upload(): void {
    this.fileTouched.set(true);
    this.validateFile();
    if (this.form.invalid || this.fileError()) {
      this.form.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }

    const file = this.file();
    const documentType = this.form.controls.documentType.value;
    if (!file || !documentType) return;

    this.uploading.set(true);
    this.uploadError.set('');
    this.service.uploadDocument(documentType, file).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (document) => {
        this.uploading.set(false);
        this.documents.update((list) => [document, ...list]);
        this.form.reset();
        this.file.set(null);
        this.fileTouched.set(false);
        const input = this.fileInput()?.nativeElement;
        if (input) input.value = '';
        this.toast.success(translate('organization.documents.uploaded', { name: document.fileName }));
      },
      error: () => {
        this.uploading.set(false);
        this.uploadError.set(translate('organization.documents.errors.upload'));
      },
    });
  }

  private validateFile(): void {
    const file = this.file();
    if (!file) {
      this.fileError.set(translate('organization.documents.errors.fileRequired'));
    } else if (!ORGANIZATION_DOCUMENT_CONTENT_TYPES.includes(file.type)) {
      this.fileError.set(translate('organization.documents.errors.fileType'));
    } else if (file.size > ORGANIZATION_DOCUMENT_MAX_BYTES) {
      this.fileError.set(translate('organization.documents.errors.fileSize'));
    } else {
      this.fileError.set('');
    }
  }
}
