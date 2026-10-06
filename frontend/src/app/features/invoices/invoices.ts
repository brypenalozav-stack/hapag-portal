import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { InvoiceService } from '../../core/services/invoice.service';
import { CartService } from '../../core/services/cart.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import {
  INVOICE_DOCUMENT_TYPES,
  INVOICE_DOWNLOAD_MAX,
  INVOICE_STATUSES,
  Invoice,
  InvoiceList,
  InvoiceOrganization,
} from '../../core/models/invoice.model';
import { PAYMENT_CURRENCIES } from '../../core/models/payment-config.model';
import { INVOICE_DOCUMENT_TYPE_KEYS, INVOICE_STATUS_CLASS, INVOICE_STATUS_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { AddToCartDialogComponent, AddToCartTarget } from '../../shared/components/add-to-cart-dialog/add-to-cart-dialog';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { saveBlob } from '../../shared/save-blob';

const PAGE_SIZE = 20;

interface InvoiceFilters {
  blNumber: string;
  bookingNumber: string;
  from: string;
  to: string;
  status: string;
  currency: string;
  documentType: string;
}

function emptyFilters(): InvoiceFilters {
  return { blNumber: '', bookingNumber: '', from: '', to: '', status: '', currency: '', documentType: '' };
}

/**
 * Facturas del cliente (M7-01): las de cada organización a la que el usuario tiene acceso, sin mezclarlas
 * (selector cuando hay más de una). Muestra el número SII y el del sistema de origen por separado, fechas,
 * BL o booking, razón social, RUT, monto, moneda y estado; filtra por BL, booking, fecha de emisión,
 * estado, moneda y tipo de documento; descarga el PDF y varias a la vez (solo con folio); agrega al carro
 * las habilitadas para pago e indica la fecha y hora de la última actualización.
 */
@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    FormsModule, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    AddToCartDialogComponent,
  ],
  templateUrl: './invoices.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class InvoicesComponent implements OnInit {
  private readonly service = inject(InvoiceService);
  readonly cart = inject(CartService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statusKeys = INVOICE_STATUS_KEYS;
  readonly docTypeKeys = INVOICE_DOCUMENT_TYPE_KEYS;
  readonly statuses = INVOICE_STATUSES;
  readonly documentTypes = INVOICE_DOCUMENT_TYPES;
  readonly currencies = PAYMENT_CURRENCIES;
  readonly maxDownload = INVOICE_DOWNLOAD_MAX;

  organizations = signal<InvoiceOrganization[]>([]);
  organizationId = '';
  filters: InvoiceFilters = emptyFilters();
  page = signal(1);

  list = signal<InvoiceList | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');
  refreshing = signal(false);
  downloading = signal(false);
  selected = signal<Set<string>>(new Set());
  addTargets = signal<AddToCartTarget[] | null>(null);

  totalPages = computed(() => {
    const l = this.list();
    return l ? Math.max(1, Math.ceil(l.total / l.pageSize)) : 1;
  });

  selectedCount = computed(() => this.selected().size);

  ngOnInit(): void {
    this.service.getOrganizations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (orgs) => {
        this.organizations.set(orgs);
        this.organizationId = orgs[0]?.id ?? '';
        this.search();
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'invoices.errors.load'));
      },
    });
  }

  search(page = 1): void {
    this.page.set(page);
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.search({
      organizationId: this.organizationId || undefined,
      ...this.filters,
      page,
      pageSize: PAGE_SIZE,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        this.list.set(list);
        this.selected.set(new Set());
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 403) this.error.set(translate('invoices.errors.forbidden'));
        else this.error.set(paymentErrorMessage(err, 'invoices.errors.load'));
      },
    });
  }

  /** La información no se mezcla entre organizaciones: cambiar de organización vuelve a consultar. */
  onOrganization(): void {
    this.search();
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  isSelected(invoice: Invoice): boolean {
    return this.selected().has(invoice.id);
  }

  toggle(invoice: Invoice, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) next.add(invoice.id);
      else next.delete(invoice.id);
      return next;
    });
  }

  statusClass(invoice: Invoice): string {
    return INVOICE_STATUS_CLASS[invoice.status] ?? 'hl-badge--pending';
  }

  label(invoice: Invoice): string {
    return invoice.siiNumber ?? invoice.sourceNumber;
  }

  downloadPdf(invoice: Invoice): void {
    this.actionError.set('');
    this.service.downloadPdf(invoice.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, `factura-${this.label(invoice)}.pdf`);
        this.announcer.announce(translate('invoices.download.pdfDone', { number: this.label(invoice) }));
      },
      error: (err) => this.fail(err, 'invoices.download.error'),
    });
  }

  /** Descarga múltiple: solo facturas con folio emitido (M7-01). */
  downloadSelected(): void {
    const ids = [...this.selected()];
    if (ids.length === 0 || ids.length > INVOICE_DOWNLOAD_MAX) return;
    this.actionError.set('');
    this.downloading.set(true);
    this.service.downloadZip(ids).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(false);
        saveBlob(blob, 'facturas.zip');
        this.announcer.announce(translate('invoices.download.zipDone', { count: ids.length }));
      },
      error: (err) => {
        this.downloading.set(false);
        this.fail(err, 'invoices.download.error');
      },
    });
  }

  /** Vuelve a leer las facturas de la fuente; si no responde, no se guarda nada (NF-11). */
  refresh(): void {
    this.actionError.set('');
    this.refreshing.set(true);
    this.service.refresh(this.organizationId || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.refreshing.set(false);
        this.announcer.announce(translate('invoices.refresh.done', { checked: result.checked, updated: result.updated }));
        this.search(this.page());
      },
      error: (err) => {
        this.refreshing.set(false);
        this.fail(err, 'invoices.refresh.error');
      },
    });
  }

  addToCart(invoice: Invoice): void {
    this.addTargets.set([{ itemType: 'Invoice', sourceId: invoice.id, label: translate('invoices.cartLabel', { number: this.label(invoice) }) }]);
  }

  onAddClosed(added: boolean): void {
    this.addTargets.set(null);
    if (added) this.search(this.page());
  }

  private fail(err: unknown, fallbackKey: string): void {
    const message = paymentErrorMessage(err, fallbackKey);
    this.actionError.set(message);
    this.announcer.announce(message, 'assertive');
  }
}
