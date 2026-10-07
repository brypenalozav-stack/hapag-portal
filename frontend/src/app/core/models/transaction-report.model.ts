import { PagedResult } from './admin-user.model';

/** Fase 2, Ola I: reportería de transacciones por servicio y de excepciones aplicadas (M9-01). */

export type TransactionCategory = 'LocalCharge' | 'OnDemandService' | 'Demurrage' | 'Freight' | 'WarehouseChange' | 'Invoice' | 'Other';
export type ExceptionType =
  | 'GateInExemption'
  | 'EdsExemption'
  | 'GateOutExemption'
  | 'OtherExemption'
  | 'IpoExclusion'
  | 'FreeWarehouseChange'
  | 'CreditImputation';

export const TRANSACTION_CATEGORIES: readonly TransactionCategory[] = [
  'LocalCharge', 'OnDemandService', 'Demurrage', 'Freight', 'WarehouseChange', 'Invoice', 'Other',
];
export const EXCEPTION_TYPES: readonly ExceptionType[] = [
  'GateInExemption', 'EdsExemption', 'GateOutExemption', 'OtherExemption', 'IpoExclusion', 'FreeWarehouseChange', 'CreditImputation',
];

export interface ReportOrganization {
  id: string;
  name: string;
  taxId?: string | null;
}

export interface TransactionSummaryRow {
  category: string;
  service: string;
  serviceName?: string | null;
  currency: string;
  transactions: number;
  items: number;
  amount: number;
  taxAmount: number;
  total: number;
}

export interface TransactionTotal {
  currency: string;
  transactions: number;
  items: number;
  total: number;
}

export interface TransactionRow {
  paymentId: string;
  paymentNumber: string;
  receiptNumber?: string | null;
  confirmedAt: string;
  country: string;
  origin?: string | null;
  paymentMethodCode?: string | null;
  organization?: ReportOrganization | null;
  onBehalfOf?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  itemType?: string | null;
  category: string;
  service: string;
  serviceName?: string | null;
  conceptCode?: string | null;
  description?: string | null;
  billingTaxId?: string | null;
  serviceRequestNumber?: string | null;
  currency: string;
  amount: number;
  taxAmount: number;
  total: number;
  originalAmount?: number | null;
  originalCurrency?: string | null;
}

export interface TransactionReport {
  from: string;
  to: string;
  country?: string | null;
  timeZone: string;
  generatedAt: string;
  summary: TransactionSummaryRow[];
  totals: TransactionTotal[];
  items: PagedResult<TransactionRow>;
}

export interface ExceptionSummaryRow {
  type: string;
  currency?: string | null;
  count: number;
  amount: number;
}

export interface ExceptionRow {
  type: string;
  occurredAt: string;
  country: string;
  billOfLadingId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  organization?: ReportOrganization | null;
  partyTaxId?: string | null;
  exemptParty?: string | null;
  conceptCode?: string | null;
  amount?: number | null;
  currency?: string | null;
  source: 'NEXUS' | 'PORTAL' | string;
  reference?: string | null;
  detail?: string | null;
}

export interface ExceptionReport {
  from: string;
  to: string;
  country?: string | null;
  timeZone: string;
  generatedAt: string;
  /** false: Nexus no respondió y las exclusiones de IPO no se pudieron calcular. */
  ipoExclusionsAvailable: boolean;
  summary: ExceptionSummaryRow[];
  items: PagedResult<ExceptionRow>;
}

/** Filtros comunes; `category`/`service`/`currency` solo en transacciones, `type` solo en excepciones. */
export interface ReportFilters {
  from?: string;
  to?: string;
  country?: string;
  category?: string;
  service?: string;
  currency?: string;
  type?: string;
  organizationId?: string;
  blNumber?: string;
  page?: number;
  pageSize?: number;
}
