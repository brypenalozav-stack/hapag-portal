import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { AccessAuditEntry } from '../../../core/models/access.model';
import { ACCESS_AUDIT_EVENT_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/**
 * Auditoría de accesos (M1-23): historial de un BL o booking (o, sin filtros, de la propia
 * organización) con el tipo de evento —otorgamiento, cambio de vigencia o permisos, revocación
 * manual, por vencimiento o en cadena, acceso abierto, autoasociación, reconciliación—, el
 * usuario (o el sistema) y la fecha y hora.
 */
@Component({
  selector: 'app-access-audit',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, PaginatorComponent],
  templateUrl: './access-audit.html',
})
export class AccessAuditComponent implements OnInit {
  private readonly service = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  pageSize = signal(50);
  readonly eventKeys = ACCESS_AUDIT_EVENT_KEYS;

  blNumber = signal('');
  bookingNumber = signal('');

  entries = signal<AccessAuditEntry[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');


  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getAudit({
      blNumber: this.blNumber().trim(),
      bookingNumber: this.bookingNumber().trim(),
      page: this.page(),
      pageSize: this.pageSize(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.entries.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: (err) => {
        this.entries.set([]);
        this.total.set(0);
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 403) {
          this.error.set(translate('thirdPartyAccess.audit.forbidden'));
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          this.error.set(translate('thirdPartyAccess.audit.notFound'));
        } else {
          this.error.set(translate('thirdPartyAccess.audit.loadError'));
        }
        this.loading.set(false);
      },
    });
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  clear(): void {
    this.blNumber.set('');
    this.bookingNumber.set('');
    this.search();
  }

  changePage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }
}
