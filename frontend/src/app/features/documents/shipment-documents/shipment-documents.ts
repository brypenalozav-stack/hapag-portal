import { Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { AuthService } from '../../../core/services/auth.service';
import { FeatureService } from '../../../core/services/feature.service';
import { CartService } from '../../../core/services/cart.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  DocumentDelivery,
  FreightCertificateResult,
  RelatedDocument,
  ShipmentDocument,
  ShipmentDocuments,
  TransshipmentRequest,
} from '../../../core/models/document.model';
import {
  RELATED_DOCUMENT_KIND_KEYS,
  REQUIREMENT_STATUS_KEYS,
  SHIPMENT_DOCUMENT_ORIGIN_KEYS,
  SHIPMENT_DOCUMENT_STATUS_CLASS,
  SHIPMENT_DOCUMENT_STATUS_KEYS,
  SHIPMENT_DOCUMENT_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { AddToCartDialogComponent, AddToCartTarget } from '../../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { saveBlob } from '../../../shared/save-blob';
import { BlCopyDialogComponent } from '../bl-copy-dialog/bl-copy-dialog';
import { ResponsibilityLetterDialogComponent } from '../responsibility-letter-dialog/responsibility-letter-dialog';
import { NoDebtCertificateComponent } from '../no-debt-certificate/no-debt-certificate';
import { FreightCertificatePanelComponent } from '../freight-certificate/freight-certificate-panel';
import { ToastService } from '../../../core/services/toast.service';

/** Diálogo abierto desde la sección. */
type OpenDialog = 'blCopy' | 'letter' | null;

/**
 * Sección "Documentos" del embarque (M6-09): reúne los documentos emitidos que el usuario puede ver según
 * M1-11 (certificado de transbordo, cupón de retiro, comprobante Collect solo para la AGA, copias del BL,
 * carta de responsabilidad y CLD) con su tipo, número, fecha de emisión, BL, estado y firma, más los
 * comprobantes de pago y las facturas del BL. Cada descarga y reenvío queda registrado (NF-14). Desde aquí
 * se solicitan la copia del BL (M6-05), la carta de responsabilidad (M6-06), el CLD de Bolivia (M6-07) y el
 * certificado de transbordo, que genera un cargo para pagar con el carro (M6-01). Fase 2, Ola J: en importaciones de
 * Bolivia, el certificado de flete (M6-02, sin pago ni carro en esta entrega) y la carta de liberación y desconsolidado
 * (M6-08, con su propia página de solicitud y seguimiento). Las acciones las informa el servidor (`actions`); las
 * solicitudes y reenvíos requieren un perfil que opera.
 */
@Component({
  selector: 'app-shipment-documents',
  standalone: true,
  imports: [
    RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    AddToCartDialogComponent, BlCopyDialogComponent, ResponsibilityLetterDialogComponent, NoDebtCertificateComponent,
    FreightCertificatePanelComponent,
  ],
  templateUrl: './shipment-documents.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ShipmentDocumentsComponent {
  private readonly service = inject(DocumentService);
  private readonly auth = inject(AuthService);
  /** Certificado de flete (M6-02) y carta de liberación (M6-08) según sus flags. */
  readonly features = inject(FeatureService);
  readonly cart = inject(CartService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();
  /** El usuario opera sobre este embarque (detalle del BL); sin valor, el perfil de la sesión. */
  canOperate = input<boolean | null>(null);
  /** Se emite cuando se emite un documento que puede cambiar los cargos del BL (carta FFWW, M4-04). */
  changed = output<ShipmentDocument>();

  readonly typeKeys = SHIPMENT_DOCUMENT_TYPE_KEYS;
  readonly statusKeys = SHIPMENT_DOCUMENT_STATUS_KEYS;
  readonly originKeys = SHIPMENT_DOCUMENT_ORIGIN_KEYS;
  readonly relatedKeys = RELATED_DOCUMENT_KIND_KEYS;
  readonly requirementStatusKeys = REQUIREMENT_STATUS_KEYS;

  data = signal<ShipmentDocuments | null>(null);
  loading = signal(true);
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);
  error = signal('');

  /** Documento o comprobante que se está descargando o reenviando. */
  busyId = signal<string | null>(null);
  actionError = signal('');
  /** Resultado visible de la última solicitud (además del anuncio). */
  actionResult = signal('');

  dialog = signal<OpenDialog>(null);
  private opener: HTMLElement | null = null;

  transshipment = signal<TransshipmentRequest | null>(null);
  requestingTransshipment = signal(false);
  addTargets = signal<AddToCartTarget[] | null>(null);

  /** Reenviar y solicitar: perfil que opera de una organización cliente (no la visibilidad interna). */
  canResend = computed(() => {
    const org = this.auth.organization();
    return (this.canOperate() ?? this.auth.canOperate()) && !!org && org.organizationType !== 'Internal';
  });

  canRequestCopy = computed(() => {
    const a = this.data()?.actions;
    return !!a && (a.canRequestValuedCopy || a.canRequestNonValuedCopy);
  });

  hasRequests = computed(() => {
    const a = this.data()?.actions;
    return this.canRequestCopy() || !!a?.canIssueResponsibilityLetter;
  });

  statusClass(status: string): string {
    return SHIPMENT_DOCUMENT_STATUS_CLASS[status] ?? 'hl-badge--pending';
  }

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
    this.service.getDocuments(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.data.set(null);
        this.loading.set(false);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          // Un BL ajeno responde 404, igual que uno inexistente (NF-05).
          this.error.set(translate('documents.section.notFound', { bl: this.blNumber() }));
        } else {
          this.error.set(documentErrorMessage(err, 'documents.section.loadError'));
        }
      },
    });
  }

  /**
   * Descarga el PDF con la sesión del usuario; el servidor registra la descarga (NF-14). `announcement`
   * reemplaza al anuncio de descarga cuando el documento se acaba de emitir.
   */
  download(doc: ShipmentDocument, announcement?: string): void {
    this.busyId.set(doc.id);
    this.actionError.set('');
    this.service.download(this.blNumber(), doc.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.busyId.set(null);
        saveBlob(blob, doc.fileName);
        this.announcer.announce(announcement ?? translate('documents.section.downloaded', { number: doc.documentNumber }));
        // Los documentos de demostración se generan y firman en la primera descarga: se actualiza la firma.
        if (!doc.contentHash) this.load();
      },
      error: (err) => this.fail(err, 'documents.section.downloadError'),
    });
  }

  downloadRelated(item: RelatedDocument): void {
    this.busyId.set(item.id);
    this.actionError.set('');
    this.service.downloadRelated(item.downloadPath).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.busyId.set(null);
        saveBlob(blob, `${item.number}.pdf`);
        this.toast.success(translate('documents.section.downloaded', { number: item.number }));
      },
      error: (err) => this.fail(err, 'documents.section.downloadError'),
    });
  }

  /** Reenvía el PDF al correo registrado de la organización (sin destinatarios libres, M6-05). */
  resend(doc: ShipmentDocument): void {
    this.busyId.set(doc.id);
    this.actionError.set('');
    this.actionResult.set('');
    this.service.resend(this.blNumber(), doc.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (delivery) => {
        this.busyId.set(null);
        this.showResult(translate('documents.section.resent', { number: doc.documentNumber, emails: delivery.sentTo.join(', ') }));
        this.load();
      },
      error: (err) => this.fail(err, 'documents.section.resendError'),
    });
  }

  openDialog(dialog: Exclude<OpenDialog, null>, event: Event): void {
    this.opener = event.currentTarget as HTMLElement;
    this.actionResult.set('');
    this.dialog.set(dialog);
  }

  onBlCopyClosed(delivery: DocumentDelivery | null): void {
    this.closeDialog();
    if (delivery) {
      this.actionResult.set(translate('documents.section.copyIssued', { number: delivery.document.documentNumber }));
      this.load();
    }
  }

  /** Carta emitida (M6-06): se descarga, se actualiza el repositorio y se avisa para recargar los cargos (M4-04). */
  onLetterClosed(letter: ShipmentDocument | null): void {
    this.closeDialog();
    if (!letter) return;
    const message = translate('documents.section.letterIssued', { number: letter.documentNumber });
    this.actionResult.set(message);
    this.download(letter, message);
    this.load();
    this.changed.emit(letter);
  }

  /** CLD emitido (M6-07): se descarga y queda en el repositorio. */
  onNoDebtIssued(doc: ShipmentDocument): void {
    const message = translate('documents.section.noDebtIssued', { number: doc.documentNumber });
    this.actionResult.set(message);
    this.download(doc, message);
    this.load();
  }

  /** Certificado de flete emitido (M6-02): queda en el repositorio, que se actualiza. */
  onFreightIssued(result: FreightCertificateResult): void {
    this.actionResult.set(translate('documents.section.freightIssued', { number: result.document.documentNumber }));
    this.load();
  }

  /**
   * Certificado de transbordo (M6-01): el servidor crea (o devuelve) el cargo del servicio y se agrega al
   * carro con el diálogo de siempre; al confirmarse el pago, el certificado se emite y se envía solo.
   */
  requestTransshipment(): void {
    if (this.requestingTransshipment()) return;
    this.requestingTransshipment.set(true);
    this.actionError.set('');
    this.actionResult.set('');
    this.service.requestTransshipmentCertificate(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (request) => {
        this.requestingTransshipment.set(false);
        this.transshipment.set(request);
        if (request.status === 'Paid') {
          this.showResult(translate('documents.transshipment.paid'));
          this.load();
          return;
        }
        this.toast.success(translate('documents.transshipment.created'));
        if (!this.cart.contains('LocalCharge', request.chargeId) && this.cart.cartEnabled()) this.addTransshipmentToCart();
      },
      error: (err) => {
        this.requestingTransshipment.set(false);
        this.fail(err, 'documents.transshipment.error');
      },
    });
  }

  addTransshipmentToCart(): void {
    const request = this.transshipment();
    if (!request) return;
    this.addTargets.set([{ itemType: 'LocalCharge', sourceId: request.chargeId, label: translate('common.chargeConcept.transshipmentCert') }]);
  }

  onAddClosed(): void {
    this.addTargets.set(null);
  }

  private closeDialog(): void {
    this.dialog.set(null);
    setTimeout(() => this.opener?.focus());
  }

  private showResult(message: string): void {
    this.actionResult.set(message);
    this.announcer.announce(message);
  }

  private fail(err: unknown, fallbackKey: string): void {
    this.busyId.set(null);
    const message = documentErrorMessage(err, fallbackKey);
    this.actionError.set(message);
    this.announcer.announce(message, 'assertive');
  }
}
