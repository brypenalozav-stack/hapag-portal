import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PARENT_LINK_STATUSES, ParentLink } from '../../../core/models/organization-network.model';
import { ORGANIZATION_TYPE_KEYS, PARENT_LINK_STATUS_CLASS, PARENT_LINK_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { ToastService } from '../../../core/services/toast.service';
import { ClientTable, codeText } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/**
 * Revisión interna de las vinculaciones con la empresa matriz (Fase 2, Ola I, M1-21; permiso `organizations.review`):
 * Hapag-Lloyd aprueba o rechaza (con motivo) cada solicitud; con la vinculación activa y la visibilidad encendida, la
 * matriz ve los BL de la filial. Cada paso queda en la auditoría de accesos.
 */
@Component({
  selector: 'app-organization-links',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './organization-links.html',
  styles: [':host { display: block; }'],
})
export class OrganizationLinksComponent implements OnInit {
  private readonly service = inject(OrganizationNetworkService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly statuses = PARENT_LINK_STATUSES;
  readonly statusKeys = PARENT_LINK_STATUS_KEYS;
  readonly statusClass = PARENT_LINK_STATUS_CLASS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  status = 'Pending';
  links = signal<ParentLink[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.links, {
    searchText: (l) =>
      [
        l.organization.name,
        l.organization.country,
        codeText(l.organization.organizationType, this.typeKeys),
        l.parent.name,
        l.parent.country,
        codeText(l.parent.organizationType, this.typeKeys),
        l.requestedBy,
        l.notes,
        codeText(l.status, this.statusKeys),
        l.decisionNotes,
      ].join(' '),
    sortValues: {
      organization: (l) => l.organization.name,
      parent: (l) => l.parent.name,
      requested: (l) => l.requestedAt,
      status: (l) => codeText(l.status, this.statusKeys),
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  busyId = signal<string | null>(null);
  notesFor: Record<string, string> = {};

  rejecting = signal<ParentLink | null>(null);
  rejectReason = '';
  rejectError = signal(false);

  private readonly rejectField = viewChild<ElementRef<HTMLTextAreaElement>>('rejectField');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getParentLinks(this.status).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (links) => {
        this.links.set(links);
        this.loading.set(false);
      },
      error: (err) => {
        this.links.set([]);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.actionError.set(adminErrorMessage(err, 'admin.organizationLinks.errors.load'));
      },
    });
  }

  approve(link: ParentLink): void {
    this.busyId.set(link.id);
    this.actionError.set('');
    this.service.approveParentLink(link.id, this.notesFor[link.id]?.trim() || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyId.set(null);
        this.toast.success(translate('admin.organizationLinks.approved', { organization: link.organization.name, parent: link.parent.name }));
        this.load();
      },
      error: (err) => this.fail(err),
    });
  }

  startReject(link: ParentLink): void {
    this.rejecting.set(link);
    this.rejectReason = '';
    this.rejectError.set(false);
    focusAfterRender(this.injector, () => this.rejectField()?.nativeElement);
  }

  cancelReject(): void {
    this.rejecting.set(null);
  }

  confirmReject(event: Event): void {
    event.preventDefault();
    const link = this.rejecting();
    if (!link) return;
    if (!this.rejectReason.trim()) {
      this.rejectError.set(true);
      this.announcer.announce(translate('admin.organizationLinks.reject.required'), 'assertive');
      focusAfterRender(this.injector, () => this.rejectField()?.nativeElement);
      return;
    }
    this.busyId.set(link.id);
    this.service.rejectParentLink(link.id, this.rejectReason.trim()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyId.set(null);
        this.rejecting.set(null);
        this.toast.success(translate('admin.organizationLinks.rejected', { organization: link.organization.name }));
        this.load();
      },
      error: (err) => this.fail(err),
    });
  }

  private fail(err: unknown): void {
    this.busyId.set(null);
    const message = adminErrorMessage(err, 'admin.organizationLinks.errors.action');
    this.actionError.set(message);
    this.announcer.announce(message, 'assertive');
    this.load();
  }
}
