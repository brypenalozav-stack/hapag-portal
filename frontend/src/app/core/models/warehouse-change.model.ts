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
