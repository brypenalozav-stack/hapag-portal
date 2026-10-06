/**
 * Cambio de almacén (Fase 1, Ola C): cambio gratuito por regla interna o Nexus (M3-04), tarifas
 * KTE/KTF del mantenedor (M8-01) y solicitudes masivas procesadas en segundo plano (M3-05, NF-19).
 * El monto lo determina siempre el servidor.
 */

/** Derecho a cambio gratuito: origen (NEXUS o PORTAL), referencia y usos por BL. */
export interface FreeEntitlement {
  isFree: boolean;
  source?: string | null;
  reference?: string | null;
  reason?: string | null;
  maxUsesPerBl?: number | null;
  usedOnBl: number;
}

export interface WarehouseChangeTariff {
  code?: string | null;
  description?: string | null;
  amount: number;
  currency: string;
  source: string;
  validFrom: string;
  validTo?: string | null;
}

/** GET /warehouse-changes/quote/{blNumber}. */
export interface WarehouseChangeQuote {
  blNumber: string;
  country: 'CL' | 'BO';
  containerNumber?: string | null;
  entitlement: FreeEntitlement;
  tariffs: WarehouseChangeTariff[];
  defaultTariffCode?: string | null;
  canRequest: boolean;
  /** NO_PERMISSION o Tariff.NotInForce. */
  blockedReason?: string | null;
}

/** Cuerpo de POST /warehouse-changes/requests y de cada línea de la solicitud masiva. */
export interface WarehouseChangeRequest {
  blNumber: string;
  containerNumber: string | null;
  fromWarehouse: string | null;
  toWarehouse: string;
  tariffCode: string | null;
}

export type WarehouseChangeStatus = 'Completed' | 'Pending' | 'Cancelled';

export interface WarehouseChangeDetail {
  id: string;
  billOfLadingId: string;
  blNumber: string;
  containerNumber?: string | null;
  fromWarehouse: string;
  toWarehouse: string;
  amount: number;
  currency: string;
  status: WarehouseChangeStatus;
  country: 'CL' | 'BO';
  isFree: boolean;
  requiresPayment: boolean;
  tariffCode?: string | null;
  tariffSource?: string | null;
  entitlementSource?: string | null;
  entitlementReference?: string | null;
  batchId?: string | null;
  createdAt: string;
  completedAt?: string | null;
}

export type WarehouseChangeBatchStatus = 'Queued' | 'Processing' | 'Completed' | 'CompletedWithErrors';
export type WarehouseChangeBatchItemStatus = 'Pending' | 'Succeeded' | 'Failed';

/** Estados en que la solicitud masiva ya terminó (se deja de consultar su avance). */
export const WAREHOUSE_BATCH_FINAL_STATUSES: readonly WarehouseChangeBatchStatus[] = ['Completed', 'CompletedWithErrors'];

/** Máximo de líneas por solicitud masiva (contrato del backend). */
export const WAREHOUSE_BATCH_MAX_ITEMS = 500;

export interface WarehouseChangeBatchItem {
  lineNumber: number;
  blNumber: string;
  containerNumber?: string | null;
  fromWarehouse?: string | null;
  toWarehouse: string;
  tariffCode?: string | null;
  status: WarehouseChangeBatchItemStatus;
  errorCode?: string | null;
  errorMessage?: string | null;
  warehouseChangeId?: string | null;
  processedAt?: string | null;
}

export interface WarehouseChangeBatch {
  id: string;
  status: WarehouseChangeBatchStatus;
  totalItems: number;
  processedItems: number;
  succeededItems: number;
  failedItems: number;
  progressPercent: number;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  /** Solo en GET /warehouse-changes/bulk/{id}. */
  items?: WarehouseChangeBatchItem[] | null;
}

// ---------------------------------------------------------------------------
// Fase 2, Ola G: historial y trazabilidad del cambio de almacén (M3-06).
// ---------------------------------------------------------------------------

/** Organización que solicitó o pagó el cambio: razón social y RUT o NIT. */
export interface WarehouseChangeParty {
  organizationId?: string | null;
  name: string;
  taxId: string;
}

/** Fila del historial: solicitante, pagador (solo con un pago confirmado) y RUT de facturación. */
export interface WarehouseChangeHistoryItem {
  id: string;
  createdAt: string;
  status: WarehouseChangeStatus;
  billOfLadingId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  timeZone: string;
  containerNumber?: string | null;
  fromWarehouse: string;
  toWarehouse: string;
  amount: number;
  currency: string;
  isFree: boolean;
  entitlementSource?: string | null;
  tariffCode?: string | null;
  requestedByEmail?: string | null;
  requestedBy?: WarehouseChangeParty | null;
  /** Línea de una solicitud masiva (M3-05). */
  batchId?: string | null;
  batchLineNumber?: number | null;
  payer?: WarehouseChangeParty | null;
  billingTaxId?: string | null;
  billingName?: string | null;
  paymentId?: string | null;
  paymentNumber?: string | null;
  paymentStatus?: string | null;
  receiptNumber?: string | null;
  paidAt?: string | null;
  completedAt?: string | null;
}

/** Evento de la trazabilidad de un cambio de almacén. */
export type WarehouseChangeEventType = 'Requested' | 'FreeEntitlementApplied' | 'PaymentStatusChanged' | 'Completed' | 'Cancelled';

export interface WarehouseChangeEvent {
  occurredAt: string;
  event: WarehouseChangeEventType;
  /** Estado del cambio o, en `PaymentStatusChanged`, estado del pago. */
  status?: string | null;
  actor?: string | null;
  /** `BATCH:<id>`, derecho de cambio gratuito, número de pago o comprobante. */
  reference?: string | null;
  notes?: string | null;
}

/** GET /warehouse-changes/history/{id}. */
export interface WarehouseChangeTrace {
  change: WarehouseChangeHistoryItem;
  timeline: WarehouseChangeEvent[];
}

export interface WarehouseChangeHistoryFilters {
  blNumber?: string;
  status?: string;
  /** Fecha de la solicitud en el huso del país (yyyy-MM-dd). */
  from?: string;
  to?: string;
  organizationId?: string;
  page?: number;
  pageSize?: number;
}
