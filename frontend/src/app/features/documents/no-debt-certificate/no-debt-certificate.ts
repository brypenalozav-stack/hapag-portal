import { Component, DestroyRef, ElementRef, Injector, effect, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DocumentService } from '../../../core/services/document.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { NoDebtBlocker, NoDebtBlockerCode, NoDebtEligibility, ShipmentDocument } from '../../../core/models/document.model';
import { apiErrorCode } from '../../../core/http/api-error';
import {
  ADVANCE_DEMURRAGE_STATUS_KEYS,
  CHARGE_CONCEPT_KEYS,
  NO_DEBT_BLOCKER_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { documentErrorMessage } from '../../../shared/document-errors';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Dónde se resuelve cada bloqueo: ruta y texto del enlace. */
interface BlockerLink {
  route: string[];
  key: string;
}

/**
 * Certificado de libre deuda de importación de Bolivia (M6-07). Antes de emitir, el servidor verifica que
 * el embarque no registre cargos locales, demurrage, facturas ni flete Collect pendientes y que las demoras
 * anticipadas obligatorias estén pagadas (M3-16); los bloqueos se muestran con sus referencias, montos y
 * dónde pagarlos. Sin bloqueos, se emite el CLD firmado y queda en el repositorio (M6-09).
 */
@Component({
  selector: 'app-no-debt-certificate',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './no-debt-certificate.html',
})
export class NoDebtCertificateComponent {
  private readonly service = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();
  /** Huso del país de la operación (NF-22). */
  timeZone = input<string | null>(null);
  /** Se emite con el certificado emitido. */
  issued = output<ShipmentDocument>();

  readonly blockerKeys = NO_DEBT_BLOCKER_KEYS;

  data = signal<NoDebtEligibility | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  issuing = signal(false);
  issueError = signal('');

  private readonly blockedHeading = viewChild<ElementRef<HTMLElement>>('blockedHeading');

  constructor() {
    effect(() => {
      this.blNumber();
      untracked(() => this.load());
    });
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getNoDebtEligibility(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.data.set(null);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(documentErrorMessage(err, 'documents.noDebt.loadError'));
      },
    });
  }

  /** Emite el CLD; si el servidor encuentra deuda, se vuelve a consultar el detalle de los bloqueos. */
  issue(): void {
    if (this.issuing()) return;
    this.issuing.set(true);
    this.issueError.set('');
    this.service.issueNoDebtCertificate(this.blNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (doc) => {
        this.issuing.set(false);
        this.issued.emit(doc);
      },
      error: (err) => {
        this.issuing.set(false);
        const message = documentErrorMessage(err, 'documents.noDebt.issueError');
        this.issueError.set(message);
        this.announcer.announce(message, 'assertive');
        if (apiErrorCode(err) === 'NoDebtCertificate.DebtPending') {
          this.load();
          focusAfterRender(this.injector, () => this.blockedHeading()?.nativeElement);
        }
      },
    });
  }

  /** Referencia legible: concepto de cobro, estado de las demoras anticipadas o el número tal cual. */
  referenceLabel(blocker: NoDebtBlocker, reference: string): string {
    const key = blocker.code === 'ADVANCE_DEMURRAGE'
      ? ADVANCE_DEMURRAGE_STATUS_KEYS[reference]
      : blocker.code === 'PENDING_CHARGES' ? CHARGE_CONCEPT_KEYS[reference] : undefined;
    return key ? translate(key) : reference;
  }

  references(blocker: NoDebtBlocker): string {
    return blocker.references.map((r) => this.referenceLabel(blocker, r)).join(', ');
  }

  /** Dónde se paga lo que bloquea el CLD: cargos, demurrage, facturas, flete o demoras anticipadas. */
  link(code: NoDebtBlockerCode): BlockerLink {
    const bl = this.blNumber();
    switch (code) {
      case 'PENDING_CHARGES':
        return { route: ['/charges', bl], key: 'documents.noDebt.link.charges' };
      case 'PENDING_DEMURRAGE':
        return { route: ['/demurrage', bl], key: 'documents.noDebt.link.demurrage' };
      case 'PENDING_INVOICES':
        return { route: ['/invoices'], key: 'documents.noDebt.link.invoices' };
      case 'PENDING_FREIGHT':
        return { route: ['/shipments', bl], key: 'documents.noDebt.link.freight' };
      default:
        return { route: ['/demurrage', bl], key: 'documents.noDebt.link.advanceDemurrage' };
    }
  }
}
