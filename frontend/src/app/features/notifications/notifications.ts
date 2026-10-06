import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { NotificationService } from '../../core/services/notification.service';
import { OrganizationService } from '../../core/services/organization.service';
import { OrganizationNetworkService } from '../../core/services/organization-network.service';
import { AuthService } from '../../core/services/auth.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { NOTIFICATION_MODULES, NotificationItem } from '../../core/models/notification.model';
import { ORGANIZATION_PROFILES, OrganizationProfile } from '../../core/models/organization.model';
import {
  NOTIFICATION_ACTION_KEYS,
  NOTIFICATION_ENTITY_KEYS,
  NOTIFICATION_MODULE_KEYS,
  NOTIFICATION_TYPE_KEYS,
  ORGANIZATION_PROFILE_KEYS,
} from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { adminErrorMessage } from '../../shared/administration-errors';

/** Acciones que resuelven una gestión pendiente: dejan de estar disponibles al resolverse (M1-25). */
const RESOLVABLE_ACTIONS = ['ApproveJoinRequest', 'ReviewOrganization', 'ReviewParentLink', 'VerifyDepositProof', 'UploadDepositProof'];

interface InboxFilters {
  module: string;
  type: string;
  blNumber: string;
  onlyUnread: boolean;
  onlyActionable: boolean;
}

function emptyFilters(): InboxFilters {
  return { module: '', type: '', blNumber: '', onlyUnread: false, onlyActionable: false };
}

/**
 * Bandeja de notificaciones (Fase 2, Ola I, M1-25): centraliza los eventos de los módulos con filtros por módulo, tipo
 * y BL, las no leídas por módulo y solo las que esperan una acción. Cada notificación identifica el embarque o la
 * gestión y, cuando corresponde, permite actuar desde ella (aprobar una solicitud de vinculación o la vinculación con la
 * matriz, abrir el pago, la solicitud o el BL). Una acción resuelta por cualquier vía queda deshabilitada; el servidor
 * vuelve a autorizar cada acción. Las preferencias de correo por tipo están en /notifications/preferences.
 */
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './notifications.html',
  styles: [':host { display: block; } .hl-notification--unread { border-left: 4px solid var(--hl-blue); }'],
})
export class NotificationsComponent implements OnInit {
  private readonly service = inject(NotificationService);
  private readonly organizations = inject(OrganizationService);
  private readonly network = inject(OrganizationNetworkService);
  private readonly auth = inject(AuthService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeKeys = NOTIFICATION_TYPE_KEYS;
  readonly moduleKeys = NOTIFICATION_MODULE_KEYS;
  readonly actionKeys = NOTIFICATION_ACTION_KEYS;
  readonly entityKeys = NOTIFICATION_ENTITY_KEYS;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;
  readonly modules = NOTIFICATION_MODULES;
  readonly profiles = ORGANIZATION_PROFILES;

  filters: InboxFilters = emptyFilters();
  /** Tipos del catálogo (preferencias) para el filtro; si no se pueden leer, los tipos con texto propio. */
  types = signal<string[]>(Object.keys(NOTIFICATION_TYPE_KEYS));

  items = signal<NotificationItem[]>([]);
  loading = signal(false);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');
  busyId = signal<string | null>(null);
  /** Perfil elegido para aprobar cada solicitud de vinculación (por defecto, solo consulta). */
  profileFor: Record<string, OrganizationProfile> = {};

  readonly unreadByModule = this.service.unreadByModule;
  /** Módulos con no leídas, para los accesos rápidos. */
  unreadModules = computed(() =>
    this.modules.filter((m) => (this.unreadByModule()[m] ?? 0) > 0).map((m) => ({ module: m, count: this.unreadByModule()[m] })),
  );
  actionableCount = computed(() => this.items().filter((n) => this.isPending(n)).length);

  ngOnInit(): void {
    this.service.getPreferences().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (prefs) => { if (prefs.length > 0) this.types.set(prefs.map((p) => p.type)); },
      error: () => { /* el filtro usa los tipos conocidos */ },
    });
    this.service.refreshUnreadCount();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    const f = this.filters;
    this.service.search({
      module: f.module,
      type: f.type,
      blNumber: f.blNumber.trim().toUpperCase(),
      onlyUnread: f.onlyUnread,
      onlyActionable: f.onlyActionable,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.items.set(items);
        for (const n of items) {
          if (n.action?.type === 'ApproveJoinRequest') this.profileFor[n.id] ??= 'OrgViewer';
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.items.set([]);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate('notifications.loadError'));
        this.loading.set(false);
      },
    });
  }

  search(): void {
    this.load();
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.load();
  }

  /** Acceso rápido por módulo: filtra la bandeja por ese módulo (o lo quita si ya estaba). */
  toggleModule(module: string): void {
    this.filters.module = this.filters.module === module ? '' : module;
    this.load();
  }

  title(n: NotificationItem): string {
    const key = NOTIFICATION_TYPE_KEYS[n.type];
    return key ? translate(key) : n.title;
  }

  /** La acción resuelve una gestión pendiente (aprobar, revisar, verificar, cargar). */
  isResolvable(n: NotificationItem): boolean {
    return !!n.action && RESOLVABLE_ACTIONS.includes(n.action.type);
  }

  isPending(n: NotificationItem): boolean {
    return this.isResolvable(n) && !!n.action?.available;
  }

  /** Ruta de las acciones que abren una pantalla (o que llevan a la pantalla donde se resuelve la gestión). */
  actionRoute(n: NotificationItem): string[] | null {
    const a = n.action;
    if (!a) return null;
    const target = a.targetId ?? '';
    const bl = n.link?.blNumber ?? target;
    switch (a.type) {
      case 'ReviewOrganization': return ['/admin/organizations', target];
      case 'ReviewParentLink': return ['/admin/organization-links'];
      case 'VerifyDepositProof': return ['/admin/payments/deposit-proofs'];
      case 'UploadDepositProof': return ['/payments', target, 'result'];
      case 'OpenPayment': return ['/payment-history', target];
      case 'OpenServiceRequest': return this.auth.isInternal() ? ['/admin/service-requests', target] : ['/service-requests', target];
      case 'OpenShipment': return ['/shipments', bl];
      case 'OpenDocument': return ['/shipments', bl, 'documents'];
      case 'OpenAccessGrants': return ['/organization'];
      case 'OpenAnnouncement': return ['/announcements'];
      case 'OpenInvoice': return ['/invoices'];
      default: return null;
    }
  }

  /** Acción resoluble que se completa en otra pantalla: se abre con un botón para poder deshabilitarla. */
  openAction(n: NotificationItem): void {
    const route = this.actionRoute(n);
    if (!route || !this.isPending(n)) return;
    this.markReadQuietly(n);
    this.router.navigate(route);
  }

  /** Aprobar la solicitud de vinculación desde la notificación (M1-08), con el perfil elegido (M1-02). */
  approveJoin(n: NotificationItem): void {
    const userId = n.action?.targetId;
    if (!userId || !this.isPending(n)) return;
    const name = n.link?.reference ?? '';
    this.run(n, this.organizations.approveJoinRequest(userId, this.profileFor[n.id] ?? 'OrgViewer'),
      translate('notifications.inline.joinApproved', { name }));
  }

  rejectJoin(n: NotificationItem): void {
    const userId = n.action?.targetId;
    if (!userId || !this.isPending(n)) return;
    const name = n.link?.reference ?? '';
    this.run(n, this.organizations.rejectJoinRequest(userId), translate('notifications.inline.joinRejected', { name }));
  }

  /** Aprobar la vinculación con la empresa matriz (M1-21, `organizations.review`). */
  approveParentLink(n: NotificationItem): void {
    const id = n.action?.targetId;
    if (!id || !this.isPending(n)) return;
    this.run(n, this.network.approveParentLink(id), translate('notifications.inline.parentLinkApproved', { reference: n.link?.reference ?? '' }));
  }

  markRead(n: NotificationItem): void {
    if (n.isRead) return;
    this.service.markRead(n.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.items.update((list) => list.map((i) => (i.id === n.id ? { ...i, isRead: true } : i)));
        this.announcer.announce(translate('notifications.markedRead', { title: this.title(n) }));
      },
      error: (err) => this.actionError.set(adminErrorMessage(err, 'notifications.errors.markRead')),
    });
  }

  /** Marca todas como leídas; con un módulo elegido, solo las de ese módulo. */
  markAll(): void {
    const module = this.filters.module || undefined;
    this.service.markAllRead(module).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        this.announcer.announce(translate('notifications.markedAll', { count: r.marked }));
        this.load();
      },
      error: (err) => this.actionError.set(adminErrorMessage(err, 'notifications.errors.markRead')),
    });
  }

  private run(n: NotificationItem, request$: Observable<unknown>, success: string): void {
    this.busyId.set(n.id);
    this.actionError.set('');
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyId.set(null);
        this.announcer.announce(success);
        this.markReadQuietly(n);
        this.load();
      },
      error: (err) => {
        this.busyId.set(null);
        const message = adminErrorMessage(err, 'notifications.errors.action');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
        this.load();
      },
    });
  }

  private markReadQuietly(n: NotificationItem): void {
    if (n.isRead) return;
    this.service.markRead(n.id).subscribe({ error: () => { /* la bandeja se vuelve a leer igual */ } });
  }
}
