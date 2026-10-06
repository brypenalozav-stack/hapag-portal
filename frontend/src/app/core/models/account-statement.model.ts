/**
 * Estado de cuenta en línea (Fase 2, Ola H, M7-03): posición financiera de una organización (resumen por moneda,
 * antigüedad por tramos, crédito disponible), líneas facturadas, calculadas no facturadas e imputadas a crédito, y
 * anticipos aplicados. Pago desde el estado: carro para clientes sin crédito (M5-01) y forma de pago por ítem para
 * clientes con crédito (M5-10). Anticipos y cruce con las facturas (M3-19, NF-04). El resumen y la antigüedad cubren
 * toda la cuenta; los filtros se aplican a las líneas y a los anticipos. Las propiedades en null no llegan en el JSON.
 */

import { Cart, CheckoutResult, PayableItemType, PaymentItem } from './cart.model';
import { CurrencyTotal } from './charges.model';
import { InvoiceOrganization } from './invoice.model';

/** Facturado (abierto o cubierto), calculado no facturado o imputado a crédito sin facturar. */
export type StatementLineKind = 'Invoiced' | 'Uninvoiced' | 'CreditImputed';

export type StatementLineStatus = 'Pending' | 'Overdue' | 'Covered' | 'Uninvoiced' | 'CreditImputed';

/** Estados del filtro: los de las líneas y "por vencer" (pendientes que vencen dentro de `dueSoonDays`). */
export const STATEMENT_STATUSES = ['Pending', 'Overdue', 'DueSoon', 'Covered', 'Uninvoiced', 'CreditImputed'] as const;

export type StatementDocumentType =
  | 'Invoice'
  | 'ExemptInvoice'
  | 'DebitNote'
  | 'LocalCharge'
  | 'ServiceCharge'
  | 'Demurrage'
  | 'Freight';

export const STATEMENT_DOCUMENT_TYPES: readonly StatementDocumentType[] = [
  'Invoice',
  'ExemptInvoice',
  'DebitNote',
  'LocalCharge',
  'ServiceCharge',
  'Demurrage',
  'Freight',
];

export type StatementSort = 'dueDate' | 'issueDate' | 'amount' | 'blNumber';
export const STATEMENT_SORTS: readonly StatementSort[] = ['dueDate', 'issueDate', 'amount', 'blNumber'];

/** Cómo paga el usuario lo elegido: carro (sin crédito) o forma de pago por ítem (con crédito). */
export type StatementPaymentChannel = 'Cart' | 'Account';

/** Formato de la planilla exportada. */
export type StatementExportFormat = 'xlsx' | 'csv';

/** Factura cubierta por un pago anterior a su emisión (anticipo, M3-19, M7-03). */
export interface InvoiceCoverage {
  kind: 'Advance';
  paymentId: string;
  paymentNumber: string;
  receiptNumber?: string | null;
  /** Recibo del pago anticipado en el repositorio (`RGO-`). */
  receiptDocumentId?: string | null;
  receiptDocumentNumber?: string | null;
  amount: number;
  currency: string;
  settledAt: string;
  matchedAt?: string | null;
}

export type SettlementKind = 'Advance' | 'CreditImputation';
export type SettlementStatus = 'Open' | 'Matched';

/** Anticipo o imputación a crédito de un cargo y su cruce con la factura (NF-04). */
export interface ChargeSettlement {
  id: string;
  kind: SettlementKind;
  status: SettlementStatus;
  paymentId: string;
  paymentNumber: string;
  receiptNumber?: string | null;
  payerOrganizationId: string;
  payerTaxId?: string | null;
  payerName?: string | null;
  onBehalfOfOrganizationId?: string | null;
  billingTaxId?: string | null;
  billingName?: string | null;
  blId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  country: string;
  itemType: PayableItemType;
  sourceId: string;
  conceptCode: string;
  description?: string | null;
  /** Monto en la moneda del cargo (la de la factura). */
  amount: number;
  currency: string;
  paidAmount: number;
  paidCurrency: string;
  settledAt: string;
  receiptDocumentId?: string | null;
  receiptDocumentNumber?: string | null;
  matchedInvoiceId?: string | null;
  matchedInvoiceNumber?: string | null;
  matchedAt?: string | null;
  matchedBy?: string | null;
  matchNote?: string | null;
}

export type CreditUnavailableReason = 'LIMIT_NOT_INFORMED' | 'EXCHANGE_RATE_UNAVAILABLE';

/** Crédito según Nexus (M8-02): sin cupo informado o sin tipo de cambio, `available` no llega. */
export interface StatementCredit {
  creditDays?: number | null;
  concepts: string[];
  validFrom?: string | null;
  validTo?: string | null;
  limit?: number | null;
  limitCurrency?: string | null;
  used?: number | null;
  available?: number | null;
  unavailableReason?: CreditUnavailableReason | null;
}

/** Totales de una moneda: saldo total = facturado abierto + no facturado + imputado a crédito. */
export interface StatementSummary {
  currency: string;
  totalBalance: number;
  invoicedBalance: number;
  overdue: number;
  dueSoon: number;
  notYetDue: number;
  uninvoiced: number;
  creditImputed: number;
  advancesUnapplied: number;
}

/** Tramo de antigüedad: `CURRENT` (no vencido) y luego por días vencidos; sin `toDays` = sin tope. */
export interface StatementAgingBucket {
  code: string;
  fromDays?: number | null;
  toDays?: number | null;
}

/** Saldo facturado abierto de una moneda por tramo (mismo orden que los tramos). */
export interface StatementAgingRow {
  currency: string;
  amounts: number[];
  total: number;
}

export interface StatementAging {
  buckets: StatementAgingBucket[];
  rows: StatementAgingRow[];
}

/** Línea del estado de cuenta; `itemType` y `sourceId` son los del carro y del cierre con crédito. */
export interface StatementLine {
  key: string;
  kind: StatementLineKind;
  documentType: StatementDocumentType;
  status: StatementLineStatus;
  dueSoon: boolean;
  daysOverdue?: number | null;
  agingBucket?: string | null;
  itemType: PayableItemType;
  sourceId: string;
  /** Folio SII, número de origen, concepto o número de la solicitud (`SRV-`). */
  number: string;
  siiNumber?: string | null;
  sourceNumber?: string | null;
  conceptCode: string;
  conceptName: string;
  description?: string | null;
  blId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  legalName?: string | null;
  taxId?: string | null;
  referenceDate: string;
  issueDate?: string | null;
  dueDate?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  /** Saldo: 0 si la factura está cubierta por un anticipo. */
  balance: number;
  currency: string;
  serviceRequestNumber?: string | null;
  creditImputationNumber?: string | null;
  coveredBy?: InvoiceCoverage | null;
  /** Se puede elegir para pagar ahora. */
  payable: boolean;
  inCart: boolean;
  /** Está en un pago en curso. */
  inPayment: boolean;
  /** Se puede imputar a la línea de crédito (M5-10). */
  canImputeToCredit: boolean;
}

export interface StatementActions {
  paymentChannel: StatementPaymentChannel;
  canPay: boolean;
  canImputeToCredit: boolean;
}

/** GET /account-statement. */
export interface AccountStatement {
  organization: InvoiceOrganization;
  country: 'CL' | 'BO';
  timeZone: string;
  asOf: string;
  evaluatedAt: string;
  /** Última sincronización de las facturas de la organización. */
  lastUpdatedAt?: string | null;
  /** false: Nexus no respondió; lo no facturado se muestra con el monto del cargo (NF-11). */
  conditionsAvailable: boolean;
  isCreditCustomer: boolean;
  credit?: StatementCredit | null;
  dueSoonDays: number;
  summary: StatementSummary[];
  aging: StatementAging;
  lines: StatementLine[];
  advances: ChargeSettlement[];
  actions: StatementActions;
}

export interface StatementFilters {
  organizationId?: string;
  blNumber?: string;
  bookingNumber?: string;
  from?: string;
  to?: string;
  status?: string;
  currency?: string;
  documentType?: string;
  sort?: string;
  direction?: string;
}

// ---------------------------------------------------------------------------
// Forma de pago por ítem para clientes con crédito (M5-10)
// ---------------------------------------------------------------------------

export type AccountCheckoutMode = 'PayNow' | 'Credit';

export interface AccountCheckoutItem {
  itemType: PayableItemType;
  sourceId: string;
  billingTaxId?: string | null;
  mode: AccountCheckoutMode;
}

/** POST /account-statement/checkout; moneda y medio solo si algún ítem se paga ahora. */
export interface AccountStatementCheckoutRequest {
  items: AccountCheckoutItem[];
  paymentCurrency: string | null;
  paymentMethodCode: string | null;
}

/** Imputación a la línea de crédito (número `CRI-`), una por moneda del cargo. */
export interface CreditImputation {
  id: string;
  number: string;
  status: string;
  country: string;
  currency: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  imputedAt: string;
  items: PaymentItem[];
}

export interface AccountCheckoutResult {
  /** Pago de los ítems a pagar ahora (null si todo se imputó a crédito). */
  payment?: CheckoutResult | null;
  creditImputations: CreditImputation[];
  payNowTotals: CurrencyTotal[];
  creditTotals: CurrencyTotal[];
  replayed: boolean;
}

// ---------------------------------------------------------------------------
// Selección múltiple al carro (M7-03) y conceptos imputables (mantenedor, NF-15)
// ---------------------------------------------------------------------------

export interface CartBatchItem {
  itemType: PayableItemType;
  sourceId?: string | null;
  reference?: string | null;
  billingTaxId?: string | null;
  paymentCurrency?: string | null;
}

export interface CartBatchItemResult {
  itemType: PayableItemType;
  sourceId?: string | null;
  reference?: string | null;
  added: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
}

/** POST /cart/items/batch: los ítems que fallan se informan sin bloquear al resto. */
export interface CartBatchResult {
  cart: Cart;
  results: CartBatchItemResult[];
  addedCount: number;
}

/** Concepto de crédito de Nexus al que se imputa. */
export const NEXUS_CREDIT_CONCEPTS = ['LOCAL_CHARGES', 'MHD', 'FREIGHT', 'STORAGE'] as const;

/** Regla del mantenedor de conceptos imputables a crédito (M5-10). */
export interface CreditImputationRule {
  id: string;
  country: string;
  conceptCode: string;
  conceptName: string;
  nexusCreditConcept: string;
  isEnabled: boolean;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface CreditImputationRuleRequest {
  country?: string;
  conceptCode?: string;
  nexusCreditConcept: string;
  isEnabled: boolean;
  notes: string | null;
}

export interface CreditImputationRuleSnapshot {
  country: string;
  conceptCode: string;
  nexusCreditConcept: string;
  isEnabled: boolean;
  notes?: string | null;
}

/** POST /admin/payments/settlements/match. */
export interface SettlementMatchResult {
  openBefore: number;
  matched: number;
  invoiceIds: string[];
  matchedAt: string;
}

export interface SettlementSearch {
  status?: string;
  kind?: string;
  country?: string;
  blNumber?: string;
  from?: string;
  to?: string;
}
