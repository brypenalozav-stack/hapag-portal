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
import { AuthService } from '../../../core/services/auth.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ResponsibilityLetterTerms, ShipmentDocument } from '../../../core/models/document.model';
import { apiErrorCode } from '../../../core/http/api-error';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

/** Formato mínimo de un correo (el servidor lo vuelve a validar). */
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Campos del formulario de la carta (M6-06). */
type LetterField =
  | 'signatoryName'
  | 'signatoryTaxId'
  | 'signatoryPosition'
  | 'contactEmail'
  | 'contactPhone'
  | 'cargoDescription'
  | 'observations';

interface FormError {
  fieldId: string;
  key: string;
}

/**
 * Carta de responsabilidad (M6-06): ingreso de los datos requeridos, lectura y aceptación de los términos
 * vigentes y emisión del PDF, sin cobro. Una carta vigente levanta el bloqueo de M4-04 sobre el BL; una
 * nueva carta de la misma organización reemplaza a la anterior. El correo de contacto se propone con el
 * del usuario (WCAG 3.3.7). Diálogo modal nativo (`<dialog>`): atrapa el foco y se cierra con Escape.
 */
@Component({
  selector: 'app-responsibility-letter-dialog',
  standalone: true,
  imports: [TranslocoPipe, LoadingSpinnerComponent],
  templateUrl: './responsibility-letter-dialog.html',
  styleUrl: '../document-dialog.scss',
})
export class ResponsibilityLetterDialogComponent implements AfterViewInit {
  private readonly service = inject(DocumentService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();
  /** Se emite al cerrar con la carta emitida, o null si no se emitió. */
  closed = output<ShipmentDocument | null>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  terms = signal<ResponsibilityLetterTerms | null>(null);
  termsLoading = signal(true);
  termsError = signal('');

  readonly values = signal<Record<LetterField, string>>({
    signatoryName: '',
    signatoryTaxId: '',
    signatoryPosition: '',
    contactEmail: this.auth.currentUser()?.email ?? '',
    contactPhone: '',
    cargoDescription: '',
    observations: '',
  });
  acceptTerms = signal(false);
  submitted = signal(false);
  submitting = signal(false);
  submitError = signal('');
  private issued: ShipmentDocument | null = null;

  errors = computed<FormError[]>(() => {
    if (!this.submitted()) return [];
    const v = this.values();
    const list: FormError[] = [];
    if (!v.signatoryName.trim()) list.push({ fieldId: 'letter-signatory-name', key: 'documents.letter.errors.nameRequired' });
    if (!v.signatoryTaxId.trim()) list.push({ fieldId: 'letter-signatory-tax-id', key: 'documents.letter.errors.taxIdRequired' });
    if (!v.signatoryPosition.trim()) list.push({ fieldId: 'letter-signatory-position', key: 'documents.letter.errors.positionRequired' });
    if (!v.contactEmail.trim()) {
      list.push({ fieldId: 'letter-contact-email', key: 'documents.letter.errors.emailRequired' });
    } else if (!EMAIL.test(v.contactEmail.trim())) {
      list.push({ fieldId: 'letter-contact-email', key: 'documents.letter.errors.emailInvalid' });
    }
    if (!this.acceptTerms()) list.push({ fieldId: 'letter-accept-terms', key: 'documents.letter.errors.termsRequired' });
    return list;
  });

  ngAfterViewInit(): void {
    this.dialog().nativeElement.showModal();
    this.loadTerms();
  }

  /** Términos vigentes de la carta: la aceptación se envía con su versión. */
  loadTerms(): void {
    this.termsLoading.set(true);
    this.termsError.set('');
    this.service.getLetterTerms().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (terms) => {
        this.terms.set(terms);
        this.termsLoading.set(false);
      },
      error: (err) => {
        this.termsLoading.set(false);
        const message = documentErrorMessage(err, 'documents.letter.termsError');
        this.termsError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  fieldError(fieldId: string): string | null {
    return this.errors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  onInput(field: LetterField, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement).value;
    this.values.update((v) => ({ ...v, [field]: value }));
  }

  onAcceptTerms(event: Event): void {
    this.acceptTerms.set((event.target as HTMLInputElement).checked);
  }

  submit(event: Event): void {
    event.preventDefault();
    const terms = this.terms();
    if (this.submitting() || !terms) return;
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const v = this.values();
    const optional = (value: string) => value.trim() || null;
    this.submitting.set(true);
    this.service.issueResponsibilityLetter(this.blNumber(), {
      signatoryName: v.signatoryName.trim(),
      signatoryTaxId: v.signatoryTaxId.trim(),
      signatoryPosition: v.signatoryPosition.trim(),
      contactEmail: v.contactEmail.trim(),
      contactPhone: optional(v.contactPhone),
      cargoDescription: optional(v.cargoDescription),
      observations: optional(v.observations),
      acceptTerms: true,
      termsVersion: terms.version,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (letter) => {
        this.submitting.set(false);
        this.issued = letter;
        this.close();
      },
      error: (err) => {
        this.submitting.set(false);
        // Los términos cambiaron mientras el formulario estaba abierto: se leen de nuevo y se piden aceptar.
        if (apiErrorCode(err) === 'ResponsibilityLetter.TermsVersionMismatch') {
          this.acceptTerms.set(false);
          this.loadTerms();
        }
        const message = documentErrorMessage(err, 'documents.letter.errors.submit');
        this.submitError.set(message);
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

  /** Cierre nativo (botón, Escape o tras emitir): avisa al contenedor si se emitió la carta. */
  onClosed(): void {
    this.closed.emit(this.issued);
  }
}
