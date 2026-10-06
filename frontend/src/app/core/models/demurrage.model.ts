import {
  ChargeAction,
  ChargeActionBlockedReason,
  CurrencyTotal,
  ProcessRequirement,
} from './charges.model';
import { TariffBreakdownLine } from './tariff.model';

/**
 * Demurrage por estado del BL (Fase 1, Ola C): cada estado muestra solo su información y su acción
 * (M3-18), MHD y otros conceptos de la pestaña (M3-02) y demoras anticipadas de Bolivia (M3-16).
 */

/** Estado del demurrage del BL (M3-18). */
export type DemurrageState = 'InvoicedWithDebt' | 'CalculatedUnpaid' | 'NotCalculated' | 'NoDemurrage';

export const DEMURRAGE_STATES: readonly DemurrageState[] = [
  'InvoicedWithDebt',
  'CalculatedUnpaid',
  'NotCalculated',
  'NoDemurrage',
];

/** Estado de las demoras anticipadas de Bolivia (M3-16). */
export type AdvanceDemurrageStatus = 'NotRequired' | 'NotRequested' | 'Pending' | 'Paid';

export interface DemurrageInvoice {
  invoiceNumber: string;
  amount: number;
  currency: string;
  dueDate?: string | null;
  invoicedAt?: string | null;
  lineIds: string[];
}

export interface DemurrageLine {
  id: string;
  containerNumber: string;
  freeDays: number;
  demurrageDays: number;
  dailyRate: number;
  totalAmount: number;
  currency: string;
  startDate: string;
  endDate: string;
  status: 'Pending' | 'Invoiced' | 'Paid';
  isExempt: boolean;
  exemptReason?: string | null;
  invoiceNumber?: string | null;
  invoicedAt?: string | null;
  invoiceDueDate?: string | null;
}

export interface CalculationContainer {
  containerNumber: string;
  containerType: string;
  status: string;
}

/** Datos necesarios para calcular (M3-18, demurrage aún no calculado). */
export interface CalculationInputs {
  dischargeDate?: string | null;
  freeDays?: number | null;
  freeDaysSource: string;
  containers: CalculationContainer[];
  tariffAvailable: boolean;
  /** Fecha local del país (yyyy-MM-dd). */
  today: string;
}

/** MHD y demoras anticipadas: conceptos que se agregan al carro desde la pestaña de demurrage. */
export interface DemurrageConceptCharge {
  chargeId: string;
  conceptCode: string;
  conceptName: string;
  description?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: 'Pending' | 'Paid' | 'Exempt';
  /** Demoras anticipadas ya pagadas que se descuentan del MHD (M3-16). */
  deduction: number;
  payableTotal: number;
  action: ChargeAction;
  actionBlockedReason?: ChargeActionBlockedReason | null;
}

export interface MhdSummary {
  currency: string;
  total: number;
  advanceDeducted: number;
  netPayable: number;
}

export interface AdvanceDemurrage {
  required: boolean;
  status: AdvanceDemurrageStatus;
  ruleId?: string | null;
  ruleReason?: string | null;
  amount?: number | null;
  currency?: string | null;
  chargeId?: string | null;
  /** El CLD queda bloqueado hasta que el pago esté confirmado (la liberación del CLD es la Ola E). */
  cldBlocked: boolean;
  cldBlockReason?: string | null;
  action: ChargeAction;
  actionBlockedReason?: ChargeActionBlockedReason | null;
}

/** GET /demurrage/{blNumber}/status. */
export interface DemurrageStatus {
  blId: string;
  blNumber: string;
  country: 'CL' | 'BO';
  timeZone: string;
  state: DemurrageState;
  /** La única acción que corresponde al estado. */
  action: ChargeAction;
  actionAllowed: boolean;
  actionBlockedReason?: ChargeActionBlockedReason | null;
  /** false siempre que exista una factura. */
  calculatorEnabled: boolean;
  messageCode?: 'NO_DEBT' | null;
  invoices: DemurrageInvoice[];
  lines: DemurrageLine[];
  calculationInputs?: CalculationInputs | null;
  otherConcepts: DemurrageConceptCharge[];
  mhd?: MhdSummary | null;
  advance: AdvanceDemurrage;
  requirements: ProcessRequirement[];
  evaluatedAt: string;
}

/** Cuerpo de POST /demurrage/{blNumber}/calculate; `save: false` es una vista previa. */
export interface CalculateDemurrageRequest {
  untilDate?: string;
  containerNumbers?: string[];
  save: boolean;
}

export interface DemurrageCalculationLine {
  containerNumber: string;
  containerType: string;
  startDate: string;
  untilDate: string;
  elapsedDays: number;
  freeDays: number;
  demurrageDays: number;
  dailyRate: number;
  totalAmount: number;
  currency: string;
  tariffSource: string;
  tariffId?: string | null;
  breakdown: TariffBreakdownLine[];
}

export interface DemurrageCalculation {
  blNumber: string;
  saved: boolean;
  lines: DemurrageCalculationLine[];
  totals: CurrencyTotal[];
  status?: DemurrageStatus | null;
}
