import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AdministrationService } from '../../../core/services/administration.service';
import { AuthService } from '../../../core/services/auth.service';
import { PERMISSIONS } from '../../../core/constants/app.constants';
import { AdminOverview, AdminOverviewSection } from '../../../core/models/administration.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { adminErrorMessage } from '../../../shared/administration-errors';

interface AdminLink {
  route: string;
  key: string;
}

interface AdminGroup {
  code: string;
  titleKey: string;
  descriptionKey: string;
  links: AdminLink[];
  /** Contador → clave de su texto (con `{count}`). */
  counters: Record<string, string>;
  /**
   * Permiso con que la sección se muestra aunque el servidor todavía no la informe en `GET /admin/overview` (sin
   * contadores); la pantalla vuelve a exigirlo.
   */
  permission?: string;
}

/** Secciones del área y sus pantallas; el servidor informa cuáles puede usar el usuario (`GET /admin/overview`). */
const GROUPS: AdminGroup[] = [
  {
    code: 'organizations',
    titleKey: 'admin.home.section.organizations.title',
    descriptionKey: 'admin.home.section.organizations.description',
    links: [{ route: '/admin/organizations', key: 'admin.home.link.organizations' }],
    counters: {
      pendingValidation: 'admin.home.counter.pendingValidation',
      pendingArCheck: 'admin.home.counter.pendingArCheck',
      preCreatedCarriers: 'admin.home.counter.preCreatedCarriers',
    },
  },
  {
    code: 'parent-links',
    titleKey: 'admin.home.section.parentLinks.title',
    descriptionKey: 'admin.home.section.parentLinks.description',
    links: [{ route: '/admin/organization-links', key: 'admin.home.link.parentLinks' }],
    counters: { pending: 'admin.home.counter.pendingLinks', active: 'admin.home.counter.activeLinks' },
  },
  {
    code: 'users',
    titleKey: 'admin.home.section.users.title',
    descriptionKey: 'admin.home.section.users.description',
    links: [{ route: '/admin/users', key: 'admin.home.link.users' }],
    counters: {},
  },
  {
    code: 'access-matrix',
    titleKey: 'admin.home.section.accessMatrix.title',
    descriptionKey: 'admin.home.section.accessMatrix.description',
    links: [{ route: '/admin/access-matrix', key: 'admin.home.link.accessMatrix' }],
    counters: {},
  },
  {
    code: 'impersonation',
    titleKey: 'admin.home.section.impersonation.title',
    descriptionKey: 'admin.home.section.impersonation.description',
    links: [{ route: '/admin/impersonation', key: 'admin.home.link.impersonation' }],
    counters: { activeSessions: 'admin.home.counter.activeSessions' },
  },
  {
    code: 'announcements',
    titleKey: 'admin.home.section.announcements.title',
    descriptionKey: 'admin.home.section.announcements.description',
    links: [{ route: '/admin/announcements', key: 'admin.home.link.announcements' }],
    counters: { current: 'admin.home.counter.currentAnnouncements', drafts: 'admin.home.counter.drafts' },
  },
  {
    code: 'guides',
    titleKey: 'admin.home.section.guides.title',
    descriptionKey: 'admin.home.section.guides.description',
    links: [{ route: '/admin/guides', key: 'admin.home.link.guides' }],
    counters: { active: 'admin.home.counter.activeGuides' },
  },
  {
    code: 'maintainers',
    titleKey: 'admin.home.section.maintainers.title',
    descriptionKey: 'admin.home.section.maintainers.description',
    links: [
      { route: '/admin/tariffs', key: 'admin.home.link.tariffs' },
      { route: '/admin/internal-charge-rules', key: 'admin.home.link.internalRules' },
      { route: '/admin/service-definitions', key: 'admin.home.link.serviceDefinitions' },
      { route: '/admin/payment-currencies', key: 'admin.home.link.paymentCurrencies' },
      { route: '/admin/payment-methods', key: 'admin.home.link.paymentMethods' },
      { route: '/admin/credit-imputation-rules', key: 'admin.home.link.creditImputationRules' },
      { route: '/admin/publication-rules', key: 'admin.home.link.publicationRules' },
      { route: '/admin/assistant-knowledge', key: 'admin.home.link.assistantKnowledge' },
      { route: '/admin/assistant-mailboxes', key: 'admin.home.link.assistantMailboxes' },
      { route: '/admin/dangerous-goods', key: 'admin.home.link.dangerousGoods' },
    ],
    counters: { tariffs: 'admin.home.counter.tariffs', serviceDefinitions: 'admin.home.counter.serviceDefinitions' },
  },
  {
    code: 'payment-blocks',
    titleKey: 'admin.home.section.paymentBlocks.title',
    descriptionKey: 'admin.home.section.paymentBlocks.description',
    links: [{ route: '/admin/payment-blocks', key: 'admin.home.link.paymentBlocks' }],
    counters: { scheduled: 'admin.home.counter.scheduledBlocks' },
  },
  {
    code: 'finance',
    titleKey: 'admin.home.section.finance.title',
    descriptionKey: 'admin.home.section.finance.description',
    links: [
      { route: '/admin/payments-finance', key: 'admin.home.link.paymentsFinance' },
      { route: '/admin/payments/deposit-proofs', key: 'admin.home.link.depositProofs' },
      { route: '/admin/payments/settlements', key: 'admin.home.link.settlements' },
    ],
    counters: {
      depositProofsToVerify: 'admin.home.counter.depositProofsToVerify',
      paymentsPendingVerification: 'admin.home.counter.paymentsPendingVerification',
      stuckPostPaymentJobs: 'admin.home.counter.stuckPostPaymentJobs',
    },
  },
  {
    code: 'service-requests',
    titleKey: 'admin.home.section.serviceRequests.title',
    descriptionKey: 'admin.home.section.serviceRequests.description',
    links: [{ route: '/admin/service-requests', key: 'admin.home.link.serviceRequests' }],
    counters: { pendingApproval: 'admin.home.counter.pendingApproval', inProgress: 'admin.home.counter.inProgress' },
  },
  {
    code: 'counter',
    titleKey: 'admin.home.section.counter.title',
    descriptionKey: 'admin.home.section.counter.description',
    links: [{ route: '/admin/counter', key: 'admin.home.link.counter' }],
    counters: { records: 'admin.home.counter.counterRecords', syncFailed: 'admin.home.counter.syncFailed' },
  },
  {
    code: 'api-clients',
    titleKey: 'admin.home.section.apiClients.title',
    descriptionKey: 'admin.home.section.apiClients.description',
    links: [{ route: '/admin/api-clients', key: 'admin.home.link.apiClients' }],
    counters: {},
    permission: PERMISSIONS.MANAGE_API_CLIENTS,
  },
  {
    code: 'reports',
    titleKey: 'admin.home.section.reports.title',
    descriptionKey: 'admin.home.section.reports.description',
    links: [
      { route: '/admin/reports/transactions', key: 'admin.home.link.transactionsReport' },
      { route: '/admin/reports/exceptions', key: 'admin.home.link.exceptionsReport' },
    ],
    counters: {},
  },
  {
    code: 'audit',
    titleKey: 'admin.home.section.audit.title',
    descriptionKey: 'admin.home.section.audit.description',
    links: [{ route: '/admin/audit', key: 'admin.home.link.audit' }],
    counters: {},
  },
];

/** Herramientas de la consola operativa que no dependen de una sección (cualquier perfil interno). */
const OPERATIONS: AdminLink[] = [
  { route: '/admin/bl-import', key: 'admin.home.link.blImport' },
  { route: '/admin/customs', key: 'admin.home.link.customs' },
  { route: '/admin/deadlines', key: 'admin.home.link.deadlines' },
  { route: '/admin/reports', key: 'admin.home.link.operationalReports' },
];

interface VisibleGroup extends AdminGroup {
  values: { key: string; count: number }[];
}

/**
 * Área de administración unificada (Fase 2, Ola I, M8-05; permiso `admin-area.access`): reúne por sección los
 * mantenedores, bandejas y herramientas internas que el usuario puede usar, con los contadores de lo pendiente
 * (`GET /admin/overview`). El servidor decide qué secciones devuelve; cada pantalla vuelve a exigir su permiso.
 */
@Component({
  selector: 'app-admin-home',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './admin-home.html',
  styles: [':host { display: block; } .section-title { font-size: 1.05rem; font-weight: 700; margin-bottom: 0; }'],
})
export class AdminHomeComponent implements OnInit {
  private readonly service = inject(AdministrationService);
  readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly operations = OPERATIONS;

  overview = signal<AdminOverview | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  groups = computed<VisibleGroup[]>(() => {
    const sections = new Map<string, AdminOverviewSection>((this.overview()?.sections ?? []).map((s) => [s.code, s]));
    return GROUPS.filter((g) => sections.has(g.code) || (!!g.permission && this.auth.hasPermission(g.permission))).map((g) => {
      const counters = sections.get(g.code)?.counters ?? {};
      return {
        ...g,
        values: Object.entries(g.counters)
          .filter(([name]) => counters[name] !== undefined)
          .map(([name, key]) => ({ key, count: counters[name] })),
      };
    });
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (overview) => {
        this.overview.set(overview);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(adminErrorMessage(err, 'admin.home.loadError'));
      },
    });
  }
}
