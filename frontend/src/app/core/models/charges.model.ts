/**
 * Cargos de un BL con las reglas de Nexus aplicadas (Fase 1, Ola C): exenciones de Gate In, EDS y
 * Gate Out para el consignatario del BL Master y el cliente final (M4-01, M4-02, M3-01), exclusión
 * del IPO por crédito (M4-03, M8-02), carta de responsabilidad FFWW (M4-04, M8-03) y tipo de cambio
 * (M5-05). Las propiedades en null no llegan en el JSON (el backend las omite).
 */

/** Resultado de las reglas sobre un cargo. */
export type ChargeOutcome = 'Payable' | 'PartiallyExempt' | 'Exempt' | 'Paid';

/** Única acción disponible para un cargo o un estado de demurrage. */
export type ChargeAction = 'Pay' | 'AddToCart' | 'Calculate' | 'None';

/** Motivo por el que la acción no está disponible. */
export type ChargeActionBlockedReason = 'NO_PERMISSION' | 'ASSOCIATION_REQUIRED' | 'RULES_UNAVAILABLE';

/** Figura consultada en Nexus para las exenciones (M4-01). */
export type ExemptionParty = 'MasterConsignee' | 'FinalClient';

/** Requisito que bloquea el avance: carta FFWW (M4-04) o demoras anticipadas (M3-16). */
export type ProcessRequirementCode = 'RESPONSIBILITY_LETTER' | 'ADVANCE_DEMURRAGE';
export type ProcessRequirementStatus = 'Missing' | 'Pending' | 'Fulfilled';

/** Conceptos del catálogo de cobros. */
export const CHARGE_CONCEPT_CODES = [
  'GATE_IN',
  'EDS',
  'GATE_OUT',
  'IPO',
  'THC',
  'THC_RF',
  'BL_FEE',
  'ISPS',
  'TRANSIT_FEE',
  'MHD',
  'DEMURRAGE',
  'ADVANCE_DEMURRAGE_BO',
  'WAREHOUSE_CHANGE',
  'LATE_ARRIVAL',
] as const;

export interface ChargeParty {
  organizationId?: string | null;
  name: string;
  taxId: string;
  matchCode?: string | null;
}

/**
 * Condiciones comerciales leídas de Nexus (M8-02 crédito, M8-03 FFWW). `available` es false si
 * Nexus no respondió: el portal no supone condiciones (NF-11).
 */
export interface CommercialConditions {
  available: boolean;
  source: string;
  taxId: string;
  matchCode?: string | null;
  hasCredit: boolean;
  creditDays?: number | null;
  creditConcepts: string[];
  creditValidFrom?: string | null;
  creditValidTo?: string | null;
  isFreightForwarder: boolean;
  responsibilityLetterRequired: boolean;
  ipoExcluded: boolean;
  errorCode?: string | null;
}

export interface ExemptionCondition {
  concept: string;
  amount?: number | null;
  currency?: string | null;
  validFrom: string;
  validTo?: string | null;
}

export interface ExemptionFigure {
  party: ExemptionParty;
  name: string;
  taxId: string;
  matchCode?: string | null;
  available: boolean;
  exemptions: ExemptionCondition[];
}

/** Trazabilidad de la exención aplicada a la condición informada por Nexus (M4-02). */
export interface ExemptionTrace {
  party: ExemptionParty;
  taxId: string;
  matchCode?: string | null;
  concept: string;
  conditionAmount?: number | null;
  conditionCurrency?: string | null;
  validFrom: string;
  validTo?: string | null;
  source: string;
  exemptAmount: number;
  /** Solo después de aplicar las reglas (POST apply-rules). */
  appliedAt?: string | null;
}

/** Monto en la moneda del país con el tipo de cambio de Nexus usado (M5-05). */
export interface LocalCurrencyAmount {
  currency: string;
  amount: number;
  rate: number;
  effectiveDate: string;
  source: string;
}

export interface RuledCharge {
  chargeId: string;
  conceptCode: string;
  conceptName: string;
  description?: string | null;
  category: 'LocalCharge' | 'Demurrage' | 'Service';
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: 'Pending' | 'Paid' | 'Exempt';
  outcome: ChargeOutcome;
  payableAmount: number;
  payableTaxAmount: number;
  payableTotal: number;
  action: ChargeAction;
  actionBlockedReason?: ChargeActionBlockedReason | null;
  exemption?: ExemptionTrace | null;
  localCurrency?: LocalCurrencyAmount | null;
}

export interface CurrencyTotal {
  currency: string;
  amount: number;
  taxAmount: number;
  total: number;
}

export interface ProcessRequirement {
  code: ProcessRequirementCode;
  status: ProcessRequirementStatus;
  blocksProcess: boolean;
  source: string;
}

/** GET /charges/{blNumber}. */
export interface ShipmentCharges {
  blId: string;
  blNumber: string;
  country: 'CL' | 'BO';
  shipmentType: string;
  /** Huso del país de la operación (NF-22). */
  timeZone: string;
  payer: ChargeParty;
  conditions: CommercialConditions;
  exemptionFigures: ExemptionFigure[];
  charges: RuledCharge[];
  payableTotals: CurrencyTotal[];
  /** Todos los cargos no pagados están exentos: el proceso no pasa por el carro (M4-02). */
  allApplicableExempt: boolean;
  requiresPayment: boolean;
  /** false = Nexus no respondió; todas las acciones quedan deshabilitadas. */
  rulesAvailable: boolean;
  requirements: ProcessRequirement[];
  /** false mientras un requisito bloquee el proceso (carta FFWW; su generación es la Ola E). */
  canProceed: boolean;
  evaluatedAt: string;
}

/** POST /charges/{blNumber}/apply-rules. */
export interface ApplyChargeRulesResult {
  completed: boolean;
  requiresPayment: boolean;
  exemptedCharges: RuledCharge[];
  payableChargeIds: string[];
  charges: ShipmentCharges;
}

/** GET /exchange-rates (M5-05). */
export interface ExchangeRate {
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  effectiveDate: string;
  source: string;
  approved: boolean;
}
