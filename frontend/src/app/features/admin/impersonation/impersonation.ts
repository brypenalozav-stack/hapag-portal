import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AdministrationService } from '../../../core/services/administration.service';
import { AdminOrganizationService } from '../../../core/services/admin-organization.service';
import { ImpersonationService } from '../../../core/services/impersonation.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { AdminOrganizationItem } from '../../../core/models/organization.model';
import {
  ImpersonationRequestEntry,
  ImpersonationSession,
  ImpersonationTarget,
} from '../../../core/models/administration.model';
import {
  IMPERSONATION_END_REASON_KEYS,
  IMPERSONATION_REQUEST_ACTION_KEYS,
  IMPERSONATION_STATUS_CLASS,
  IMPERSONATION_STATUS_KEYS,
  ORGANIZATION_PROFILE_KEYS,
  ORGANIZATION_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { ToastService } from '../../../core/services/toast.service';

interface FormError {
  fieldId: string;
  key: string;
}

const SESSIONS_PAGE_SIZE = 20;
const REQUESTS_PAGE_SIZE = 50;

/**
 * Vista como cliente (Fase 2, Ola I, M8-08; permiso `impersonation.use`, solo el Administrador interno): elegir la
 * organización y el usuario cliente, indicar el motivo e iniciar una sesión de solo lectura con la misma visibilidad y
 * permisos del cliente; las sesiones quedan registradas con administrador, cliente, organización, inicio, término,
 * duración y cada solicitud (permitida o bloqueada).
 */
@Component({
  selector: 'app-impersonation',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './impersonation.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ImpersonationComponent implements OnInit {
  private readonly service = inject(AdministrationService);
  private readonly organizations = inject(AdminOrganizationService);
  private readonly impersonation = inject(ImpersonationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly statusKeys = IMPERSONATION_STATUS_KEYS;
  readonly statusClass = IMPERSONATION_STATUS_CLASS;
  readonly endReasonKeys = IMPERSONATION_END_REASON_KEYS;
  readonly requestActionKeys = IMPERSONATION_REQUEST_ACTION_KEYS;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;
  readonly requestsPageSize = REQUESTS_PAGE_SIZE;

  // Inicio de sesión
  orgSearch = '';
  orgResults = signal<AdminOrganizationItem[] | null>(null);
  orgSearching = signal(false);
  selectedOrg = signal<AdminOrganizationItem | null>(null);
  targets = signal<ImpersonationTarget[] | null>(null);
  targetsLoading = signal(false);
  selectedUserId = '';
  reason = '';
  errors = signal<FormError[]>([]);
  starting = signal(false);
  startError = signal('');

  // Sesiones
  statusFilter = '';
  from = '';
  to = '';
  sessions = signal<ImpersonationSession[]>([]);
  total = signal(0);
  page = signal(1);
  sessionsLoading = signal(true);
  sessionsFailed = signal(false);
  actionError = signal('');
  endingId = signal<string | null>(null);

  // Registro de solicitudes de una sesión
  auditSession = signal<ImpersonationSession | null>(null);
  requests = signal<ImpersonationRequestEntry[]>([]);
  requestsTotal = signal(0);
  requestsPage = signal(1);
  requestsLoading = signal(false);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly auditHeading = viewChild<ElementRef<HTMLElement>>('auditHeading');

  ngOnInit(): void {
    this.loadSessions();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / SESSIONS_PAGE_SIZE));
  }

  get requestPages(): number {
    return Math.max(1, Math.ceil(this.requestsTotal() / REQUESTS_PAGE_SIZE));
  }

  searchOrganizations(): void {
    const search = this.orgSearch.trim();
    this.orgSearching.set(true);
    this.organizations.search({ search, status: 'Approved', pageSize: 10 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        const list = r.items.filter((o) => o.organizationType !== 'Internal');
        this.orgResults.set(list);
        this.orgSearching.set(false);
        this.announcer.announce(translate('admin.impersonation.start.orgResults', { count: list.length }));
      },
      error: (err) => {
        this.orgResults.set([]);
        this.orgSearching.set(false);
        this.startError.set(adminErrorMessage(err, 'admin.impersonation.start.errors.search'));
      },
    });
  }

  selectOrganization(org: AdminOrganizationItem): void {
    this.selectedOrg.set(org);
    this.selectedUserId = '';
    this.targets.set(null);
    this.targetsLoading.set(true);
    this.service.getTargets(org.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (targets) => {
        this.targets.set(targets);
        this.targetsLoading.set(false);
      },
      error: (err) => {
        this.targets.set([]);
        this.targetsLoading.set(false);
        this.startError.set(adminErrorMessage(err, 'admin.impersonation.start.errors.targets'));
      },
    });
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  start(event: Event): void {
    event.preventDefault();
    this.startError.set('');
    const errors: FormError[] = [];
    const org = this.selectedOrg();
    if (!org) errors.push({ fieldId: 'impersonation-org-search', key: 'admin.impersonation.start.errors.organization' });
    else if (!this.selectedUserId) errors.push({ fieldId: 'impersonation-users', key: 'admin.impersonation.start.errors.user' });
    if (this.reason.trim().length < 5) errors.push({ fieldId: 'impersonation-reason', key: 'admin.impersonation.start.errors.reason' });
    this.errors.set(errors);
    if (errors.length > 0 || !org) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.starting.set(true);
    this.service.startSession({ organizationId: org.id, userId: this.selectedUserId, reason: this.reason.trim() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (started) => {
          this.starting.set(false);
          this.impersonation.begin(started);
        },
        error: (err) => {
          this.starting.set(false);
          const message = adminErrorMessage(err, 'admin.impersonation.start.errors.submit');
          this.startError.set(message);
          this.announcer.announce(message, 'assertive');
        },
      });
  }

  loadSessions(): void {
    this.sessionsLoading.set(true);
    this.sessionsFailed.set(false);
    this.service.getSessions({
      status: this.statusFilter, from: this.from, to: this.to, page: this.page(), pageSize: SESSIONS_PAGE_SIZE,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        this.sessions.set(r.items);
        this.total.set(r.total);
        this.sessionsLoading.set(false);
      },
      error: (err) => {
        this.sessions.set([]);
        this.sessionsLoading.set(false);
        if (isServiceUnavailable(err)) this.sessionsFailed.set(true);
        else this.actionError.set(adminErrorMessage(err, 'admin.impersonation.sessions.errors.load'));
      },
    });
  }

  searchSessions(): void {
    this.page.set(1);
    this.loadSessions();
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.page.set(next);
    this.loadSessions();
  }

  /** Termina una sesión activa desde el área de administración (otro administrador o la propia en otra pestaña). */
  endSession(s: ImpersonationSession): void {
    this.endingId.set(s.id);
    this.actionError.set('');
    this.service.endSession(s.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.endingId.set(null);
        this.toast.success(translate('admin.impersonation.sessions.ended', { name: s.subject.fullName || s.subject.email }));
        this.loadSessions();
      },
      error: (err) => {
        this.endingId.set(null);
        const message = adminErrorMessage(err, 'admin.impersonation.sessions.errors.end');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openAudit(s: ImpersonationSession): void {
    this.auditSession.set(s);
    this.requestsPage.set(1);
    this.loadRequests(true);
  }

  closeAudit(): void {
    this.auditSession.set(null);
  }

  changeRequestsPage(delta: number): void {
    const next = this.requestsPage() + delta;
    if (next < 1 || next > this.requestPages) return;
    this.requestsPage.set(next);
    this.loadRequests(false);
  }

  private loadRequests(focus: boolean): void {
    const s = this.auditSession();
    if (!s) return;
    this.requestsLoading.set(true);
    this.service.getSessionRequests(s.id, this.requestsPage(), REQUESTS_PAGE_SIZE).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        this.requests.set(r.items);
        this.requestsTotal.set(r.total);
        this.requestsLoading.set(false);
        if (focus) focusAfterRender(this.injector, () => this.auditHeading()?.nativeElement);
      },
      error: () => {
        this.requests.set([]);
        this.requestsLoading.set(false);
      },
    });
  }

  duration(s: ImpersonationSession): number {
    return Math.max(1, Math.round((s.durationSeconds ?? 0) / 60));
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    const el = document.getElementById(fieldId);
    (el?.querySelector<HTMLElement>('input') ?? el)?.focus();
  }
}
