import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ParentCandidate, ParentCompanyView } from '../../../core/models/organization-network.model';
import { ORGANIZATION_TYPE_KEYS, PARENT_LINK_STATUS_CLASS, PARENT_LINK_STATUS_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { adminErrorMessage } from '../../../shared/administration-errors';

/**
 * Empresa matriz (Fase 2, Ola I, M1-21): la organización solicita vincularse con su matriz (buscándola por RUT o razón
 * social), ve el estado de la solicitud y activa o desactiva la visibilidad de todos sus BL hacia la matriz, sin
 * asignación BL por BL; Hapag-Lloyd revisa la vinculación. Si la organización es matriz, lista sus filiales visibles con
 * el acceso a sus embarques, uno por filial para no mezclar la información. Gestionar exige `org.access.manage`.
 */
@Component({
  selector: 'app-parent-company',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './parent-company.html',
})
export class ParentCompanyComponent implements OnInit {
  private readonly service = inject(OrganizationNetworkService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statusKeys = PARENT_LINK_STATUS_KEYS;
  readonly statusClass = PARENT_LINK_STATUS_CLASS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  view = signal<ParentCompanyView | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  busy = signal(false);

  search = '';
  candidates = signal<ParentCandidate[] | null>(null);
  searching = signal(false);
  selectedParentId = '';
  enableVisibility = true;
  notes = '';
  requestError = signal('');
  confirmingRemove = signal(false);

  /** Se puede pedir una vinculación nueva: sin vínculo o con el anterior rechazado o quitado. */
  canRequest = computed(() => {
    const v = this.view();
    const status = v?.link?.status;
    return !!v?.canManage && (!status || status === 'Rejected' || status === 'Removed');
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getParentCompany().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (view) => {
        this.view.set(view);
        this.loading.set(false);
      },
      error: () => {
        this.view.set(null);
        this.loading.set(false);
        this.loadFailed.set(true);
      },
    });
  }

  searchCandidates(): void {
    const text = this.search.trim();
    if (!text) return;
    this.searching.set(true);
    this.requestError.set('');
    this.service.searchParentCandidates(text).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        this.candidates.set(list);
        this.searching.set(false);
        this.announcer.announce(translate('organization.parentCompany.request.results', { count: list.length }));
      },
      error: (err) => {
        this.candidates.set([]);
        this.searching.set(false);
        this.requestError.set(adminErrorMessage(err, 'organization.parentCompany.errors.search'));
      },
    });
  }

  request(event: Event): void {
    event.preventDefault();
    this.requestError.set('');
    if (!this.selectedParentId) {
      const message = translate('organization.parentCompany.request.errors.parent');
      this.requestError.set(message);
      this.announcer.announce(message, 'assertive');
      return;
    }
    this.busy.set(true);
    this.service.requestParentLink({
      parentOrganizationId: this.selectedParentId,
      enableVisibility: this.enableVisibility,
      notes: this.notes.trim() || undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (link) => {
        this.busy.set(false);
        this.candidates.set(null);
        this.search = '';
        this.selectedParentId = '';
        this.notes = '';
        this.announcer.announce(translate('organization.parentCompany.request.sent', { name: link.parent.name }));
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        const message = adminErrorMessage(err, 'organization.parentCompany.request.errors.submit');
        this.requestError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  toggleVisibility(event: Event): void {
    const enabled = (event.target as HTMLInputElement).checked;
    this.busy.set(true);
    this.actionError.set('');
    this.service.setParentVisibility(enabled).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (link) => {
        this.busy.set(false);
        this.view.update((v) => (v ? { ...v, link } : v));
        this.announcer.announce(translate(enabled ? 'organization.parentCompany.visibilityOn' : 'organization.parentCompany.visibilityOff', { name: link.parent.name }));
      },
      error: (err) => {
        this.busy.set(false);
        (event.target as HTMLInputElement).checked = !enabled;
        const message = adminErrorMessage(err, 'organization.parentCompany.errors.visibility');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  askRemove(): void {
    this.confirmingRemove.set(true);
  }

  cancelRemove(): void {
    this.confirmingRemove.set(false);
  }

  remove(): void {
    this.busy.set(true);
    this.actionError.set('');
    this.service.removeParentLink().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirmingRemove.set(false);
        this.announcer.announce(translate('organization.parentCompany.removed'));
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        const message = adminErrorMessage(err, 'organization.parentCompany.errors.remove');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
