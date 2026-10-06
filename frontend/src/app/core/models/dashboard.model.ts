/**
 * Dashboard consolidado del cliente (Fase 1, Ola F, M1-05): gestiones en curso, servicios pendientes de pago,
 * documentos recientes e indicadores operativos de los embarques, en una sola consulta (GET /dashboard). El
 * servidor filtra cada bloque por los accesos del usuario (M1-11, NF-05). Las propiedades en null no llegan en
 * el JSON (el backend las omite).
 */

import { PayableItemType } from './cart.model';

/** Destino de navegación de un elemento del dashboard y tipo de gestión. */
export type DashboardTargetKind =
  | 'Shipment'
  | 'Charges'
  | 'Demurrage'
  | 'Invoice'
  | 'Documents'
  | 'WarehouseChange'
  | 'WarehouseChangeBatch'
  | 'ServiceOrder'
  | 'TatcBatch'
  | 'BlCopy'
  | 'ResponsibilityLetter'
  // Fase 2, Ola G: solicitud de servicio on demand.
  | 'ServiceRequest';

export interface DashboardTarget {
  kind: DashboardTargetKind;
  blNumber?: string | null;
  id?: string | null;
}

export interface DashboardAmount {
  currency: string;
  total: number;
}

export interface DashboardCount {
  key: string;
  count: number;
}

/** Gestión de la organización (solicitud del portal) con su estado. */
export interface DashboardRequest {
  kind: DashboardTargetKind;
  id: string;
  reference: string;
  blNumber?: string | null;
  status: string;
  inProgress: boolean;
  createdAt: string;
  completedAt?: string | null;
  target: DashboardTarget;
}

export interface DashboardRequests {
  inProgress: number;
  items: DashboardRequest[];
}

/** Servicio pendiente de pago: `itemType` y `sourceId` son los que acepta el carro (POST /cart/items). */
export interface DashboardPayable {
  itemType: PayableItemType;
  sourceId: string;
  blNumber?: string | null;
  bookingNumber?: string | null;
  country: string;
  conceptCode: string;
  description?: string | null;
  totalAmount: number;
  currency: string;
  /** `Pending` u `Overdue` (facturas). */
  status: string;
  /** Vencimiento de la factura (DateOnly). */
  dueDate?: string | null;
  inCart: boolean;
  target: DashboardTarget;
}

export interface DashboardPayments {
  count: number;
  totals: DashboardAmount[];
  items: DashboardPayable[];
  /** Hay más pendientes que los listados (máximo 50). */
  truncated: boolean;
}

export interface DashboardDocument {
  id: string;
  documentType: string;
  documentNumber: string;
  blNumber: string;
  issuedAt: string;
  /** Ruta absoluta de la descarga (`/api/v1/documents/{bl}/{id}/download`). */
  downloadPath: string;
}

/** Próximo arribo (ETA) o zarpe (ETD) en los próximos 14 días. */
export interface DashboardShipment {
  blNumber: string;
  bookingNumber?: string | null;
  operation: string;
  vessel?: string | null;
  voyage?: string | null;
  port?: string | null;
  date?: string | null;
}

/** Demurrage en riesgo: estado de M3-18, líneas pendientes y montos por moneda. */
export interface DashboardDemurrageItem {
  blNumber: string;
  state: string;
  pendingLines: number;
  amounts: DashboardAmount[];
}

export interface DashboardDemurrage {
  count: number;
  items: DashboardDemurrageItem[];
}

export interface DashboardIndicators {
  totalShipments: number;
  byStatus: DashboardCount[];
  byOperation: DashboardCount[];
  withPendingCharges: number;
  upcomingArrivals: DashboardShipment[];
  upcomingDepartures: DashboardShipment[];
  demurrageAtRisk: DashboardDemurrage;
  /** Embarques sobre los que se calcularon los bloques que dependen de los permisos por BL. */
  evaluatedShipments: number;
  shipmentsTruncated: boolean;
}

/** GET /dashboard. El administrador interno no recibe gestiones ni facturas. */
export interface Dashboard {
  organizationId?: string | null;
  country?: string | null;
  operation?: string | null;
  generatedAt: string;
  requests: DashboardRequests;
  pendingPayments: DashboardPayments;
  documents: DashboardDocument[];
  indicators: DashboardIndicators;
}

/** Filtros del dashboard: operación (M2-07) y país (M1-04). */
export interface DashboardFilters {
  operation?: 'IMPORT' | 'EXPORT' | '';
  country?: 'CL' | 'BO' | '';
}
