import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccountStatementService } from '../../core/services/account-statement.service';
import { InvoiceService } from '../../core/services/invoice.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { PaymentService } from '../../core/services/payment.service';
import { LocaleService } from '../../core/services/locale.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import {
  AccountCheckoutResult,
  AccountStatement,
  CartBatchResult,
  STATEMENT_DOCUMENT_TYPES,
  STATEMENT_SORTS,
  STATEMENT_STATUSES,
  StatementAgingRow,
  StatementCredit,
  StatementExportFormat,
  StatementLine,
  StatementLineKind,
} from '../../core/models/account-statement.model';
import { InvoiceOrganization } from '../../core/models/invoice.model';
import { PaymentBlockStatus } from '../../core/models/cart.model';
import { PAYMENT_CURRENCIES } from '../../core/models/payment-config.model';
import {
  CHARGE_CONCEPT_KEYS,
  CREDIT_UNAVAILABLE_REASON_KEYS,
  NEXUS_CREDIT_CONCEPT_KEYS,
  PAYMENT_ERRORS,
  SETTLEMENT_STATUS_CLASS,
  SETTLEMENT_STATUS_KEYS,
  STATEMENT_DOCUMENT_TYPE_KEYS,
  STATEMENT_LINE_KIND_KEYS,
  STATEMENT_SORT_KEYS,
  STATEMENT_STATUS_CLASS,
  STATEMENT_STATUS_KEYS,
} from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { PaymentBlockBannerComponent } from '../../shared/components/payment-block-banner/payment-block-banner';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { paymentErrorMessage } from '../../shared/payment-errors';
import { focusAfterRender } from '../../shared/focus-after-render';
import { saveBlob } from '../../shared/save-blob';
import { StatementCheckoutComponent } from './statement-checkout/statement-checkout';
import { agingBucketLabel, lineConcept, totalsByCurrency } from './statement-text';

interface StatementFilterForm {
  blNumber: string;
  bookingNumber: string;
  from: string;
  to: string;
  status: string;
  currency: string;
  documentType: string;
  sort: string;
  direction: string;
}

function emptyFilters(): StatementFilterForm {
  return { blNumber: '', bookingNumber: '', from: '', to: '', status: '', currency: '', documentType: '', sort: 'dueDate', direction: 'asc' };
}

/** Bloque de líneas de un mismo tipo: facturado, calculado no facturado o imputado a crédito (M7-03). */
interface LineGroup {
  kind: StatementLineKind;
  titleKey: string;
  helpKey: string;
  emptyKey: string;
  lines: StatementLine[];
}

const GROUPS: { kind: StatementLineKind; titleKey: string; helpKey: string; emptyKey: string }[] = [
  { kind: 'Invoiced', titleKey: 'accountStatement.lines.invoiced.title', helpKey: 'accountStatement.lines.invoiced.help', emptyKey: 'accountStatement.lines.invoiced.empty' },
  { kind: 'Uninvoiced', titleKey: 'accountStatement.lines.uninvoiced.title', helpKey: 'accountStatement.lines.uninvoiced.help', emptyKey: 'accountStatement.lines.uninvoiced.empty' },
  { kind: 'CreditImputed', titleKey: 'accountStatement.lines.creditImputed.title', helpKey: 'accountStatement.lines.creditImputed.help', emptyKey: 'accountStatement.lines.creditImputed.empty' },
];

/** Columna de la tabla por criterio de orden (para `aria-sort`). */
const SORT_COLUMN: Record<string, string> = { dueDate: 'due', issueDate: 'issue', amount: 'balance', blNumber: 'reference' };

/**
 * Estado de cuenta en línea (Fase 2, Ola H, M7-03): la posición financiera de la organización elegida, sin mezclar
 * organizaciones (selector como en M7-01). Muestra el saldo total, lo vencido, lo por vencer y el crédito disponible
 * (clientes con crédito, M8-02), la antigüedad por tramos (tabla con barras decorativas), y las líneas separadas en
 * facturadas, calculadas no facturadas e imputadas a crédito, con los anticipos aplicados y el cruce con su factura
 * (M3-19). Filtra por BL, booking, fecha, estado, moneda y tipo de documento, ordena, destaca lo próximo a vencer, indica
 * la última actualización y exporta a planilla (xlsx o csv). Lo elegido se paga en una sola transacción: en el carro
 * para clientes sin crédito (M5-01) o con forma de pago por ítem para clientes con crédito (M5-10, M5-07).
 */
@Component({
  selector: 'app-account-statement',
  standalone: true,
  imports: [
    FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent,
    PaymentBlockBannerComponent, StatementCheckoutComponent,
  ],
  templateUrl: './account-statement.html',
  styleUrl: './account-statement.scss',
})
export class AccountStatementComponent implements OnInit {
  private readonly service = inject(AccountStatementService);
  private readonly invoices = inject(InvoiceService);
  private readonly cart = inject(CartService);
  private readonly auth = inject(AuthService);
  private readonly payments = inject(PaymentService);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly statuses = STATEMENT_STATUSES;
  readonly statusKeys = STATEMENT_STATUS_KEYS;
  readonly documentTypes = STATEMENT_DOCUMENT_TYPES;
  readonly documentTypeKeys = STATEMENT_DOCUMENT_TYPE_KEYS;
  readonly kindKeys = STATEMENT_LINE_KIND_KEYS;
  readonly sorts = STATEMENT_SORTS;
  readonly sortKeys = STATEMENT_SORT_KEYS;
  readonly currencies = PAYMENT_CURRENCIES;
  readonly nexusConceptKeys = NEXUS_CREDIT_CONCEPT_KEYS;
  readonly settlementStatusKeys = SETTLEMENT_STATUS_KEYS;
  readonly settlementStatusClass = SETTLEMENT_STATUS_CLASS;
  readonly conceptKeys = CHARGE_CONCEPT_KEYS;

  organizations = signal<InvoiceOrganization[]>([]);
  organizationId = '';
  filters: StatementFilterForm = emptyFilters();
  /** Filtros con que se consultó (los de la exportación y del orden anunciado). */
  private applied: StatementFilterForm = emptyFilters();

  statement = signal<AccountStatement | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');
  exporting = signal<StatementExportFormat | null>(null);
  block = signal<PaymentBlockStatus | null>(null);

  selected = signal<Set<string>>(new Set());
  adding = signal(false);
  batchResult = signal<CartBatchResult | null>(null);
  checkoutResult = signal<AccountCheckoutResult | null>(null);

  private readonly batchHeading = viewChild<ElementRef<HTMLElement>>('batchHeading');
  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');

  canOperate = computed(() => this.auth.canOperate());

  /** Las líneas de cada tipo, en el orden que devuelve el servidor. */
  groups = computed<LineGroup[]>(() => {
    const lines = this.statement()?.lines ?? [];
    return GROUPS.map((g) => ({ ...g, lines: lines.filter((l) => l.kind === g.kind) }));
  });

  channel = computed(() => this.statement()?.actions.paymentChannel ?? 'Cart');

  /** La organización elegida es la propia y el usuario opera: puede pagar desde aquí. */
  canPay = computed(() => {
    const s = this.statement();
    return !!s && s.actions.canPay && s.organization.isOwn && this.canOperate();
  });

  selectedLines = computed(() => (this.statement()?.lines ?? []).filter((l) => this.selected().has(l.key)));
  selectedTotals = computed(() => totalsByCurrency(this.selectedLines()));

  /** Saldo mayor de cada fila de antigüedad, para la barra decorativa. */
  private agingMax = computed(() => new Map((this.statement()?.aging.rows ?? []).map((r) => [r.currency, Math.max(1, ...r.amounts)])));

  ngOnInit(): void {
    this.invoices.getOrganizations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (orgs) => {
        this.organizations.set(orgs);
        this.organizationId = orgs[0]?.id ?? '';
        this.search();
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(paymentErrorMessage(err, 'accountStatement.errors.load'));
      },
    });
    this.payments.blockStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => this.block.set(status),
      error: () => this.block.set(null),
    });
  }

  search(): void {
    this.loading.set(this.statement() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.applied = { ...this.filters };
    this.service.get({ organizationId: this.organizationId || undefined, ...this.applied }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (statement) => {
        this.statement.set(statement);
        this.selected.set(new Set());
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.statement.set(null);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else if (err instanceof HttpErrorResponse && err.status === 403) this.error.set(translate('accountStatement.errors.forbidden'));
        else this.error.set(paymentErrorMessage(err, 'accountStatement.errors.load'));
      },
    });
  }

  /** La información no se mezcla entre organizaciones: cambiar de organización vuelve a consultar. */
  onOrganization(): void {
    this.batchResult.set(null);
    this.checkoutResult.set(null);
    this.statement.set(null);
    this.search();
  }

  clearFilters(): void {
    this.filters = emptyFilters();
    this.search();
  }

  ariaSort(column: string): 'ascending' | 'descending' | null {
    return SORT_COLUMN[this.applied.sort] === column ? (this.applied.direction === 'desc' ? 'descending' : 'ascending') : null;
  }

  agingLabel = agingBucketLabel;

  agingPercent(row: StatementAgingRow, index: number): number {
    return Math.round(((row.amounts[index] ?? 0) * 100) / (this.agingMax().get(row.currency) ?? 1));
  }

  /** Por qué no se informa el disponible (sin motivo del servidor: cupo no informado). */
  creditReason(credit: StatementCredit): string {
    return translate(CREDIT_UNAVAILABLE_REASON_KEYS[credit.unavailableReason ?? 'LIMIT_NOT_INFORMED']);
  }

  concept(line: StatementLine): string {
    return lineConcept(line);
  }

  statusClass(line: StatementLine): string {
    return STATEMENT_STATUS_CLASS[line.status] ?? 'hl-badge--processing';
  }

  /** Se puede elegir para pagar ahora (en el carro, si aún no está en él). */
  selectable(line: StatementLine): boolean {
    if (!this.canPay() || !line.payable || line.inPayment) return false;
    return this.channel() === 'Account' || !line.inCart;
  }

  isSelected(line: StatementLine): boolean {
    return this.selected().has(line.key);
  }

  toggle(line: StatementLine, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) next.add(line.key);
      else next.delete(line.key);
      return next;
    });
    this.batchResult.set(null);
  }

  groupSelectable(group: LineGroup): StatementLine[] {
    return group.lines.filter((l) => this.selectable(l));
  }

  groupAllSelected(group: LineGroup): boolean {
    const lines = this.groupSelectable(group);
    return lines.length > 0 && lines.every((l) => this.selected().has(l.key));
  }

  toggleGroup(group: LineGroup, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      for (const line of this.groupSelectable(group)) {
        if (checked) next.add(line.key);
        else next.delete(line.key);
      }
      return next;
    });
    this.batchResult.set(null);
  }

  /** Planilla con los filtros aplicados, en el idioma de la interfaz. */
  export(format: StatementExportFormat): void {
    const s = this.statement();
    if (!s || this.exporting()) return;
    this.actionError.set('');
    this.exporting.set(format);
    const lang = this.locale.lang();
    this.service.export({ organizationId: s.organization.id, ...this.applied }, format, lang).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.exporting.set(null);
        const prefix = lang === 'en' ? 'account-statement' : 'estado-de-cuenta';
        const fileName = `${prefix}-${s.organization.taxId}-${s.asOf.replace(/-/g, '')}.${format}`;
        saveBlob(blob, fileName);
        this.announcer.announce(translate('accountStatement.export.done', { name: fileName }));
      },
      error: (err) => {
        this.exporting.set(null);
        const message = paymentErrorMessage(err, 'accountStatement.export.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Clientes sin crédito: lo elegido va al carro de una vez; los ítems que fallan se informan sin bloquear al resto. */
  addToCart(): void {
    const lines = this.selectedLines();
    if (lines.length === 0 || this.adding()) return;
    this.adding.set(true);
    this.actionError.set('');
    this.batchResult.set(null);
    this.cart.addItems(lines.map((l) => ({ itemType: l.itemType, sourceId: l.sourceId }))).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.adding.set(false);
        this.batchResult.set(result);
        this.announcer.announce(translate('accountStatement.cart.added', { count: result.addedCount }));
        focusAfterRender(this.injector, () => this.batchHeading()?.nativeElement);
        this.search();
      },
      error: (err) => {
        this.adding.set(false);
        const message = paymentErrorMessage(err, 'accountStatement.cart.error');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** Línea de un ítem que no se pudo agregar, con el motivo traducido. */
  batchFailure(itemType: string, sourceId: string | null | undefined, errorCode: string | null | undefined): string {
    const line = (this.statement()?.lines ?? []).find((l) => l.itemType === itemType && l.sourceId === sourceId);
    const reason = translate(errorCode && PAYMENT_ERRORS[errorCode] ? PAYMENT_ERRORS[errorCode] : 'accountStatement.cart.itemError');
    return translate('accountStatement.cart.failure', { item: line ? `${line.number} ${line.blNumber ?? ''}`.trim() : (sourceId ?? ''), reason });
  }

  /** Clientes con crédito: resultado del cierre por ítem (M5-10). */
  onCheckoutDone(result: AccountCheckoutResult): void {
    this.checkoutResult.set(result);
    const imputed = result.creditImputations.map((c) => c.number).join(', ');
    this.announcer.announce(translate('accountStatement.result.announce', {
      payment: result.payment?.payment.paymentNumber ?? '—',
      imputations: imputed || '—',
    }));
    focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
    this.search();
  }

  /** Continúa el pago inmediato: la plataforma, la boleta de depósito o el resultado. */
  continuePayment(): void {
    const payment = this.checkoutResult()?.payment;
    if (!payment) return;
    const url = payment.nextAction === 'Redirect' ? payment.redirectUrl : null;
    if (url && /^https?:\/\//i.test(url)) {
      window.location.assign(url);
    } else if (url) {
      this.router.navigateByUrl(url);
    } else {
      this.router.navigate(['/payments', payment.payment.id, 'result']);
    }
  }
}
