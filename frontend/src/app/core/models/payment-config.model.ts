/**
 * Configuración interna de pagos de la Ola D: monedas por concepto y país (M5-04), medios de pago (M5-03),
 * ventanas de bloqueo por horario (M8-07) y herramientas de Finanzas (M5-02, NF-03, NF-04).
 */

import { PaymentMethodKind } from './cart.model';

export const PAYMENT_CURRENCIES = ['CLP', 'USD', 'EUR', 'BOB'] as const;

/** Moneda habilitada de un concepto en un país (regla del mantenedor). */
export interface PaymentCurrencyRule {
  id: string;
  currency: string;
  isEnabled: boolean;
  createdAt: string;
  modifiedAt?: string | null;
}

/** Fila del mantenedor de monedas: un concepto de cobro en un país. */
export interface PaymentCurrencyConfig {
  country: string;
  conceptCode: string;
  conceptName: string;
  /** false = sin configuración: se paga en la moneda del cargo y en la local. */
  configured: boolean;
  enabledCurrencies: string[];
  rules: PaymentCurrencyRule[];
  /** Reglas de Finanzas para Chile (CLP siempre; EUR solo en CLP; BOB no se paga en BOB). */
  chileFinanceRules: boolean;
}

/** Registro de cambios de un mantenedor (NF-15): valores anterior y nuevo. */
export interface MaintainerChange<T> {
  id: string;
  entityId: string;
  action: string;
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: T | null;
  current?: T | null;
}

export interface PaymentCurrencySnapshot {
  country: string;
  conceptCode: string;
  currency: string;
  isEnabled: boolean;
}

export interface PaymentMethodRequest {
  code: string;
  name: string;
  description?: string | null;
  country: string;
  kind: PaymentMethodKind;
  providerKey?: string | null;
  currencies: string[];
  isEnabled: boolean;
  displayOrder: number;
}

export type PaymentMethodSnapshot = PaymentMethodRequest;

/** Plataformas de pago registradas para los medios en línea. */
export const PAYMENT_PROVIDER_KEYS = ['Khipu', 'BancoChile', 'Santander', 'Bci'] as const;

export type PaymentBlockWindowStatus = 'Scheduled' | 'Active' | 'Ended' | 'Cancelled';

/** Ventana de bloqueo de pagos (M8-07); fechas y horas locales del país. */
export interface PaymentBlockWindow {
  id: string;
  /** null = todos los países, cada uno en su hora local. */
  country?: string | null;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  reason: string;
  clientMessage: string;
  status: PaymentBlockWindowStatus;
  timeZone?: string | null;
  createdAt: string;
  createdBy?: string | null;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface PaymentBlockWindowRequest {
  country: string | null;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  reason: string;
  clientMessage: string;
}

export type PaymentBlockWindowSnapshot = Partial<PaymentBlockWindowRequest> & { isActive?: boolean };

export type PaymentOperationStatus = 'Pending' | 'Succeeded' | 'Stuck';

/** Paso posterior a la confirmación (liberación o aviso) en la cola recuperable (NF-03). */
export interface PaymentOperation {
  id: string;
  paymentId: string;
  paymentNumber: string;
  jobType: 'Release' | 'Notify';
  status: PaymentOperationStatus;
  attempts: number;
  maxAttempts: number;
  nextAttemptAt?: string | null;
  lastAttemptAt?: string | null;
  lastError?: string | null;
  createdAt: string;
  completedAt?: string | null;
}

export type ReconciliationStatus = 'Matched' | 'MissingReceipt' | 'MissingTransactionReference' | 'NotSettled';

/** Pago con su estado de conciliación (NF-04). */
export interface PaymentReconciliation {
  paymentId: string;
  paymentNumber: string;
  country: string;
  status: string;
  method?: string | null;
  currency: string;
  totalAmount: number;
  createdAt: string;
  confirmedAt?: string | null;
  externalReference?: string | null;
  providerReference?: string | null;
  providerTransactionId?: string | null;
  receiptNumber?: string | null;
  slipNumber?: string | null;
  payerTaxId?: string | null;
  reconciliationStatus: ReconciliationStatus;
}

export interface ReconciliationSearch {
  from?: string;
  to?: string;
  country?: string;
  status?: string;
}
