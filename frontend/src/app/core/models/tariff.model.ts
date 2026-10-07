/**
 * Mantenedor de tarifas de cargos locales (M8-01) y reglas internas de cobro (M3-04, M3-16), con su
 * registro de cambios: usuario, fecha, valor anterior y nuevo (NF-15). Fechas de vigencia en
 * formato yyyy-MM-dd (fecha local del país, NF-22).
 */

/** Unidad de medida de los tramos; `None` = tarifa plana sin tramos. */
export type TariffTierUnit = 'None' | 'Hours' | 'CalendarDays' | 'BusinessDays' | 'Units';

export const TARIFF_TIER_UNITS: readonly TariffTierUnit[] = ['None', 'Hours', 'CalendarDays', 'BusinessDays', 'Units'];

/** `Flat`: valor del tramo que contiene la medida; `PerUnit`: cada unidad al valor de su tramo. */
export type TariffTierMode = 'Flat' | 'PerUnit';

export const TARIFF_TIER_MODES: readonly TariffTierMode[] = ['Flat', 'PerUnit'];

/** Monedas en que se definen las tarifas. */
export const TARIFF_CURRENCIES = ['CLP', 'USD', 'BOB', 'EUR'] as const;

export interface TariffTier {
  fromUnit: number;
  /** null = tramo abierto (solo el último). */
  toUnit: number | null;
  amount: number;
}

export interface TariffBreakdownLine {
  fromUnit: number;
  toUnit?: number | null;
  units: number;
  unitAmount: number;
  amount: number;
}

export interface Tariff {
  id: string;
  conceptCode: string;
  conceptName?: string | null;
  code?: string | null;
  country: 'CL' | 'BO';
  currency: string;
  containerType?: string | null;
  description?: string | null;
  amount: number;
  tierUnit: TariffTierUnit;
  tierMode: TariffTierMode;
  tiers: TariffTier[];
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

/** Cuerpo de POST y PUT /tariffs. */
export interface TariffRequest {
  conceptCode: string;
  code: string | null;
  country: 'CL' | 'BO';
  currency: string;
  containerType: string | null;
  description: string | null;
  amount: number;
  tierUnit: TariffTierUnit;
  tierMode: TariffTierMode;
  tiers: TariffTier[];
  validFrom: string;
  validTo: string | null;
}

export interface TariffSnapshot {
  conceptCode: string;
  code?: string | null;
  country: string;
  currency: string;
  containerType?: string | null;
  description?: string | null;
  amount: number;
  tierUnit: TariffTierUnit;
  tierMode: TariffTierMode;
  tiers: TariffTier[];
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
}

export type MaintainerChangeAction = 'Created' | 'Updated' | 'Deactivated';

/** Un cambio del mantenedor de tarifas (NF-15); la lista llega de la más reciente a la más antigua. */
export interface TariffChange {
  id: string;
  tariffId: string;
  action: MaintainerChangeAction;
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: TariffSnapshot | null;
  current?: TariffSnapshot | null;
}

export interface TariffSearch {
  country?: string;
  concept?: string;
  inForceOn?: string;
  includeInactive?: boolean;
}

export interface ChargeConcept {
  code: string;
  name: string;
  category: 'LocalCharge' | 'Demurrage' | 'Service';
  countries: string[];
  nexusTariff: boolean;
  nexusExemptible: boolean;
  displayOrder: number;
}

/** Tipo de regla interna: cambio de almacén gratuito (M3-04) o demoras anticipadas exigidas (M3-16). */
export type InternalChargeRuleType = 'FreeWarehouseChange' | 'AdvanceDemurrageRequired';

export const INTERNAL_CHARGE_RULE_TYPES: readonly InternalChargeRuleType[] = ['FreeWarehouseChange', 'AdvanceDemurrageRequired'];

export interface InternalChargeRule {
  id: string;
  ruleType: InternalChargeRuleType;
  country: 'CL' | 'BO';
  taxId?: string | null;
  matchCode?: string | null;
  accountName?: string | null;
  reason?: string | null;
  maxUsesPerBl?: number | null;
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
  createdAt: string;
  modifiedAt?: string | null;
}

/** Cuerpo de POST y PUT /internal-charge-rules (RUT/NIT o Match Code obligatorio). */
export interface InternalChargeRuleRequest {
  ruleType: InternalChargeRuleType;
  country: 'CL' | 'BO';
  taxId: string | null;
  matchCode: string | null;
  accountName: string | null;
  reason: string | null;
  maxUsesPerBl: number | null;
  validFrom: string;
  validTo: string | null;
}

export interface InternalChargeRuleSnapshot {
  ruleType: InternalChargeRuleType;
  country: string;
  taxId?: string | null;
  matchCode?: string | null;
  accountName?: string | null;
  reason?: string | null;
  maxUsesPerBl?: number | null;
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
}

export interface InternalChargeRuleChange {
  id: string;
  ruleId: string;
  action: MaintainerChangeAction;
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: InternalChargeRuleSnapshot | null;
  current?: InternalChargeRuleSnapshot | null;
}

export interface InternalChargeRuleSearch {
  ruleType?: string;
  country?: string;
  includeInactive?: boolean;
}
