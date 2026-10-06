import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ApiClientService } from '../../../core/services/api-client.service';
import { AdminOrganizationService } from '../../../core/services/admin-organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { AdminOrganizationItem } from '../../../core/models/organization.model';
import {
  API_CLIENT_LIMITS,
  API_CLIENT_SCOPES,
  API_CLIENT_STATUSES,
  ApiClient,
  ApiClientKey,
  ApiClientRequestLog,
  ApiClientScope,
  ApiClientSecret,
  WS_BASE_PATH,
  WS_OPENAPI_DOC,
  WS_OPERATIONS,
  WS_OUTCOMES,
} from '../../../core/models/api-client.model';
import {
  API_CLIENT_SCOPE_KEYS,
  API_CLIENT_STATUS_CLASS,
  API_CLIENT_STATUS_KEYS,
  ORGANIZATION_TYPE_KEYS,
  WS_OPERATION_KEYS,
  WS_OUTCOME_CLASS,
  WS_OUTCOME_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { apiClientErrorMessage } from '../../../shared/api-client-errors';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';

/** Formato mínimo de un correo (el servidor lo vuelve a validar). */
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormError {
  fieldId: string;
  key: string;
}

/** Datos del alta de un cliente del canal. */
interface ClientForm {
  name: string;
  scopes: ApiClientScope[];
  rateLimit: number | null;
  signatoryName: string;
  signatoryTaxId: string;
  signatoryPosition: string;
  signatoryEmail: string;
  technicalContactEmail: string;
  notes: string;
}

function emptyForm(): ClientForm {
  return {
    name: '',
    scopes: [],
    rateLimit: API_CLIENT_LIMITS.RATE_LIMIT_DEFAULT,
    signatoryName: '',
    signatoryTaxId: '',
    signatoryPosition: '',
    signatoryEmail: '',
    technicalContactEmail: '',
    notes: '',
  };
}

/** Confirmación pendiente sobre el cliente abierto: revocar una clave o el cliente completo. */
type PendingConfirmation = { kind: 'client' } | null;

/**
 * Canal de requerimientos vía Web Service (Fase 2, Ola J, M3-17; permiso `api-clients.manage`): clientes del canal por
 * organización, alta con su usuario técnico y su primera clave, rotación con período de gracia, revocación de una clave o
 * del cliente (con confirmación) y bitácora de solicitudes con su resultado. La clave se muestra una sola vez, al crearla o
 * rotarla, con un botón para copiarla: el servidor guarda solo su hash y al salir de la pantalla ya no se puede ver. El
 * canal (`/api/ws/v1`, cabecera `X-Api-Key`) aplica los mismos permisos, estados y trazabilidad que el portal.
 */
@Component({
  selector: 'app-api-clients',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './api-clients.html',
  styles: [`
    :host { display: block; }
    .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }
    .hl-api-key { font-family: var(--bs-font-monospace); }
  `],
})
export class ApiClientsComponent implements OnInit {
  private readonly service = inject(ApiClientService);
  private readonly modal = inject(ModalService);
  private readonly organizations = inject(AdminOrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly limits = API_CLIENT_LIMITS;
  readonly scopes = API_CLIENT_SCOPES;
  readonly statuses = API_CLIENT_STATUSES;
  readonly operations = WS_OPERATIONS;
  readonly outcomes = WS_OUTCOMES;
  readonly wsBasePath = WS_BASE_PATH;
  readonly openApiDoc = WS_OPENAPI_DOC;
  readonly scopeKeys = API_CLIENT_SCOPE_KEYS;
  readonly statusKeys = API_CLIENT_STATUS_KEYS;
  readonly statusClass = API_CLIENT_STATUS_CLASS;
  readonly operationKeys = WS_OPERATION_KEYS;
  readonly outcomeKeys = WS_OUTCOME_KEYS;
  readonly outcomeClass = WS_OUTCOME_CLASS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  // Listado y filtros
  organizationFilter = '';
  statusFilter = '';
  clients = signal<ApiClient[]>([]);
  /** Organizaciones con clientes, para el filtro (se completan con cada consulta sin filtro de organización). */
  organizationOptions = signal<{ id: string; name: string }[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  listError = signal('');

  // Clave recién emitida: solo en memoria y solo hasta que se confirma que se guardó o se sale de la pantalla.
  secret = signal<ApiClientSecret | null>(null);
  copied = signal(false);
  private readonly secretHeading = viewChild<ElementRef<HTMLElement>>('secretHeading');

  // Alta
  creating = signal(false);
  form: ClientForm = emptyForm();
  orgSearch = '';
  orgResults = signal<AdminOrganizationItem[] | null>(null);
  orgSearching = signal(false);
  selectedOrg = signal<AdminOrganizationItem | null>(null);
  formErrors = signal<FormError[]>([]);
  saving = signal(false);
  createError = signal('');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  // Cliente abierto
  selected = signal<ApiClient | null>(null);
  graceMinutes: number | null = 0;
  rotating = signal(false);
  confirmation = signal<PendingConfirmation>(null);
  revokeReason = '';
  revokeReasonError = signal('');
  acting = signal(false);
  detailError = signal('');
  private readonly detailHeading = viewChild<ElementRef<HTMLElement>>('detailHeading');

  // Bitácora del cliente abierto
  log = signal<ApiClientRequestLog[]>([]);
  logTotal = signal(0);
  logPage = signal(1);
  logLoading = signal(false);
  logError = signal('');
  operationFilter = signal('');
  outcomeFilter = signal('');

  /** Filas de la página de la bitácora que cumplen los filtros de operación y resultado. */
  filteredLog = computed(() => this.log().filter((r) =>
    (!this.operationFilter() || r.operation === this.operationFilter()) && (!this.outcomeFilter() || r.outcome === this.outcomeFilter())));

  ngOnInit(): void {
    this.load();
  }

  get logPages(): number {
    return Math.max(1, Math.ceil(this.logTotal() / API_CLIENT_LIMITS.LOG_PAGE_SIZE));
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.listError.set('');
    const organizationId = this.organizationFilter;
    this.service.list({ organizationId, status: this.statusFilter }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (clients) => {
        const sorted = [...clients].sort((a, b) => a.organizationName.localeCompare(b.organizationName) || a.name.localeCompare(b.name));
        this.clients.set(sorted);
        if (!organizationId) this.rememberOrganizations(sorted);
        this.loading.set(false);
      },
      error: (err) => {
        this.clients.set([]);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.listError.set(apiClientErrorMessage(err, 'admin.apiClients.list.loadError'));
      },
    });
  }

  private rememberOrganizations(clients: ApiClient[]): void {
    const known = new Map(this.organizationOptions().map((o) => [o.id, o.name]));
    for (const c of clients) known.set(c.organizationId, c.organizationName);
    this.organizationOptions.set([...known].map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name)));
  }

  search(event: Event): void {
    event.preventDefault();
    this.load();
  }

  clearFilters(): void {
    this.organizationFilter = '';
    this.statusFilter = '';
    this.load();
  }

  activeKeys(client: ApiClient): number {
    return client.keys.filter((k) => k.active).length;
  }

  // Clave mostrada una sola vez
  private showSecret(secret: ApiClientSecret, messageKey: string): void {
    this.secret.set(secret);
    this.copied.set(false);
    this.announcer.announce(translate(messageKey, { name: secret.client.name }));
    focusAfterRender(this.injector, () => this.secretHeading()?.nativeElement);
  }

  copySecret(): void {
    const s = this.secret();
    if (!s) return;
    const done = () => {
      this.copied.set(true);
      this.toast.success(translate('admin.apiClients.secret.copied'));
    };
    const fail = () => this.announcer.announce(translate('admin.apiClients.secret.copyFailed'), 'assertive');
    try {
      navigator.clipboard.writeText(s.apiKey).then(done, fail);
    } catch {
      fail();
    }
  }

  /** Selecciona el texto de la clave para copiarla a mano si el portapapeles no está disponible. */
  selectKey(event: Event): void {
    (event.target as HTMLInputElement).select();
  }

  dismissSecret(): void {
    this.secret.set(null);
    this.copied.set(false);
    this.announcer.announce(translate('admin.apiClients.secret.dismissed'));
    focusAfterRender(this.injector, () => (this.selected() ? this.detailHeading()?.nativeElement : document.getElementById('api-clients-title')));
  }

  // Alta
  startCreate(): void {
    this.form = emptyForm();
    this.orgSearch = '';
    this.orgResults.set(null);
    this.selectedOrg.set(null);
    this.formErrors.set([]);
    this.createError.set('');
    this.creating.set(true);
    focusAfterRender(this.injector, () => document.getElementById('api-client-form-title'));
  }

  cancelCreate(): void {
    this.creating.set(false);
    focusAfterRender(this.injector, () => document.getElementById('api-client-new'));
  }

  searchOrganizations(): void {
    this.orgSearching.set(true);
    this.organizations.search({ search: this.orgSearch.trim(), status: 'Approved', pageSize: 10 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        // El canal es para organizaciones cliente aprobadas: nunca la interna (ApiClient.OrganizationNotAllowed).
        const list = r.items.filter((o) => o.organizationType !== 'Internal');
        this.orgResults.set(list);
        this.orgSearching.set(false);
        this.announcer.announce(translate('admin.apiClients.form.orgResults', { count: list.length }));
      },
      error: (err) => {
        this.orgResults.set([]);
        this.orgSearching.set(false);
        this.createError.set(apiClientErrorMessage(err, 'admin.apiClients.form.errors.search'));
      },
    });
  }

  selectOrganization(org: AdminOrganizationItem): void {
    this.selectedOrg.set(org);
  }

  hasScope(scope: ApiClientScope): boolean {
    return this.form.scopes.includes(scope);
  }

  toggleScope(scope: ApiClientScope, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.form.scopes = checked ? [...this.form.scopes, scope] : this.form.scopes.filter((s) => s !== scope);
  }

  hasError(fieldId: string): boolean {
    return this.formErrors().some((e) => e.fieldId === fieldId);
  }

  errorFor(fieldId: string): string | null {
    return this.formErrors().find((e) => e.fieldId === fieldId)?.key ?? null;
  }

  private validate(): FormError[] {
    const f = this.form;
    const errors: FormError[] = [];
    if (!this.selectedOrg()) errors.push({ fieldId: 'api-client-org-search', key: 'admin.apiClients.form.errors.organization' });
    if (!f.name.trim()) errors.push({ fieldId: 'api-client-name', key: 'admin.apiClients.form.errors.name' });
    if (f.scopes.length === 0) errors.push({ fieldId: 'api-client-scopes', key: 'admin.apiClients.form.errors.scopes' });
    const rate = f.rateLimit;
    if (rate === null || !Number.isInteger(rate) || rate < API_CLIENT_LIMITS.RATE_LIMIT_MIN || rate > API_CLIENT_LIMITS.RATE_LIMIT_MAX) {
      errors.push({ fieldId: 'api-client-rate', key: 'admin.apiClients.form.errors.rate' });
    }
    if (f.signatoryEmail.trim() && !EMAIL.test(f.signatoryEmail.trim())) {
      errors.push({ fieldId: 'api-client-signatory-email', key: 'admin.apiClients.form.errors.email' });
    }
    if (f.technicalContactEmail.trim() && !EMAIL.test(f.technicalContactEmail.trim())) {
      errors.push({ fieldId: 'api-client-contact-email', key: 'admin.apiClients.form.errors.email' });
    }
    return errors;
  }

  create(event: Event): void {
    event.preventDefault();
    if (this.saving()) return;
    this.createError.set('');
    const errors = this.validate();
    this.formErrors.set(errors);
    const org = this.selectedOrg();
    if (errors.length > 0 || !org) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const optional = (value: string) => value.trim() || null;
    const signatory = { name: optional(f.signatoryName), taxId: optional(f.signatoryTaxId), position: optional(f.signatoryPosition), email: optional(f.signatoryEmail) };
    this.saving.set(true);
    this.service.create({
      organizationId: org.id,
      name: f.name.trim(),
      scopes: f.scopes,
      rateLimitPerMinute: f.rateLimit ?? API_CLIENT_LIMITS.RATE_LIMIT_DEFAULT,
      signatory: Object.values(signatory).some((v) => v !== null) ? signatory : null,
      technicalContactEmail: optional(f.technicalContactEmail),
      notes: optional(f.notes),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (secret) => {
        this.saving.set(false);
        this.creating.set(false);
        this.load();
        this.showSecret(secret, 'admin.apiClients.secret.created');
      },
      error: (err) => {
        this.saving.set(false);
        const message = apiClientErrorMessage(err, 'admin.apiClients.form.errors.submit');
        this.createError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    const el = document.getElementById(fieldId);
    (el?.tagName === 'FIELDSET' ? el.querySelector<HTMLElement>('input') : el)?.focus();
  }

  // Cliente abierto: datos, claves, rotación, revocación y bitácora
  open(client: ApiClient): void {
    this.selected.set(client);
    this.graceMinutes = 0;
    this.confirmation.set(null);
    this.detailError.set('');
    this.operationFilter.set('');
    this.outcomeFilter.set('');
    this.logPage.set(1);
    this.loadLog();
    focusAfterRender(this.injector, () => this.detailHeading()?.nativeElement);
  }

  close(): void {
    const id = this.selected()?.id;
    this.selected.set(null);
    this.confirmation.set(null);
    focusAfterRender(this.injector, () => (id ? document.getElementById(`api-client-open-${id}`) : null));
  }

  private replaceClient(client: ApiClient): void {
    this.selected.set(client);
    this.clients.update((list) => list.map((c) => (c.id === client.id ? client : c)));
  }

  rotate(event: Event): void {
    event.preventDefault();
    const client = this.selected();
    if (!client || this.rotating()) return;
    const grace = this.graceMinutes ?? 0;
    if (!Number.isInteger(grace) || grace < 0 || grace > API_CLIENT_LIMITS.GRACE_MINUTES_MAX) {
      this.detailError.set(translate('admin.apiClients.detail.rotate.graceInvalid'));
      this.announcer.announce(this.detailError(), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('api-client-grace'));
      return;
    }
    this.rotating.set(true);
    this.detailError.set('');
    this.service.rotate(client.id, grace).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (secret) => {
        this.rotating.set(false);
        this.replaceClient(secret.client);
        this.showSecret(secret, 'admin.apiClients.secret.rotated');
      },
      error: (err) => this.failDetail(err, 'admin.apiClients.detail.rotate.error', () => this.rotating.set(false)),
    });
  }

  async askRevokeKey(key: ApiClientKey): Promise<void> {
    this.confirmation.set(null);
    const confirmed = await this.modal.confirm({
      title: 'admin.apiClients.detail.keys.confirmTitle',
      message: 'admin.apiClients.detail.keys.confirmText',
      params: { prefix: key.prefix },
      confirmLabel: 'admin.apiClients.detail.keys.confirm',
      cancelLabel: 'admin.apiClients.detail.keys.keep',
      tone: 'danger',
    });
    if (confirmed) this.confirmRevokeKey(key);
  }

  askRevokeClient(): void {
    this.revokeReason = '';
    this.revokeReasonError.set('');
    this.confirmation.set({ kind: 'client' });
    focusAfterRender(this.injector, () => document.getElementById('api-client-confirm-title'));
  }

  cancelConfirmation(): void {
    this.confirmation.set(null);
    focusAfterRender(this.injector, () => document.getElementById('api-client-revoke'));
  }

  confirmRevokeKey(key: ApiClientKey): void {
    const client = this.selected();
    if (!client || this.acting()) return;
    this.acting.set(true);
    this.detailError.set('');
    this.service.revokeKey(client.id, key.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.acting.set(false);
        this.replaceClient(updated);
        this.toast.success(translate('admin.apiClients.detail.keys.revoked', { prefix: key.prefix }));
        focusAfterRender(this.injector, () => document.getElementById('api-client-keys-title'));
      },
      error: (err) => this.failDetail(err, 'admin.apiClients.detail.keys.revokeError', () => this.acting.set(false)),
    });
  }

  confirmRevokeClient(event: Event): void {
    event.preventDefault();
    const client = this.selected();
    if (!client || this.acting()) return;
    const reason = this.revokeReason.trim();
    if (!reason) {
      this.revokeReasonError.set('admin.apiClients.detail.revoke.reasonRequired');
      this.announcer.announce(translate('admin.apiClients.detail.revoke.reasonRequired'), 'assertive');
      focusAfterRender(this.injector, () => document.getElementById('api-client-revoke-reason'));
      return;
    }
    this.revokeReasonError.set('');
    this.acting.set(true);
    this.detailError.set('');
    this.service.revoke(client.id, reason).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.acting.set(false);
        this.confirmation.set(null);
        this.replaceClient(updated);
        this.announcer.announce(translate('admin.apiClients.detail.revoke.done', { name: updated.name }));
        focusAfterRender(this.injector, () => this.detailHeading()?.nativeElement);
      },
      error: (err) => this.failDetail(err, 'admin.apiClients.detail.revoke.error', () => this.acting.set(false)),
    });
  }

  private failDetail(err: unknown, fallbackKey: string, reset: () => void): void {
    reset();
    const message = apiClientErrorMessage(err, fallbackKey);
    this.detailError.set(message);
    this.announcer.announce(message, 'assertive');
  }

  loadLog(): void {
    const client = this.selected();
    if (!client) return;
    this.logLoading.set(true);
    this.logError.set('');
    this.service.requests(client.id, this.logPage()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => {
        this.log.set(r.items);
        this.logTotal.set(r.total);
        this.logLoading.set(false);
      },
      error: (err) => {
        this.log.set([]);
        this.logTotal.set(0);
        this.logLoading.set(false);
        this.logError.set(apiClientErrorMessage(err, 'admin.apiClients.log.loadError'));
      },
    });
  }

  changeLogPage(delta: number): void {
    const next = this.logPage() + delta;
    if (next < 1 || next > this.logPages) return;
    this.logPage.set(next);
    this.loadLog();
  }

  onOperationFilter(event: Event): void {
    this.operationFilter.set((event.target as HTMLSelectElement).value);
  }

  onOutcomeFilter(event: Event): void {
    this.outcomeFilter.set((event.target as HTMLSelectElement).value);
  }
}
