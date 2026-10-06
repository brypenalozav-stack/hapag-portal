import { BLContainer, DemurrageCharge, LocalCharge } from './bl.model';

/** Operación del embarque (M2-07). */
export type ShipmentOperation = 'IMPORT' | 'EXPORT';

/** Selección importación/exportación del listado; '' muestra ambas. */
export type ShipmentOperationFilter = ShipmentOperation | '';

/** Rol de la organización en el embarque y columnas de la matriz de M1-11. */
export type ShipmentRole = 'Customer' | 'Shipper' | 'Consignee' | 'ThirdParty' | 'CustomsAgency' | 'Carrier';

export const SHIPMENT_ROLES: readonly ShipmentRole[] = [
  'Customer',
  'Shipper',
  'Consignee',
  'ThirdParty',
  'CustomsAgency',
  'Carrier',
];

/**
 * Origen del acceso del usuario al embarque: propio, acceso otorgado (M1-12), autoasociado (M1-18),
 * acceso abierto por número de BL (M1-17) o administrador interno.
 */
export type ShipmentAccessSource = 'Own' | 'Grant' | 'SelfAssociated' | 'OpenAccess' | 'Admin';

/** Fila del listado único de embarques (M2-06, M2-07). */
export interface ShipmentListItem {
  id: string;
  blNumber: string;
  bookingNumber: string | null;
  vessel: string | null;
  voyage: string | null;
  status: string;
  operation: ShipmentOperation;
  country: 'CL' | 'BO';
  portOfLoading: string | null;
  portOfDischarge: string | null;
  etd: string | null;
  eta: string | null;
  roles: ShipmentRole[];
  accessSource: ShipmentAccessSource;
  hasPendingCharges: boolean;
}

export interface ShipmentFreight {
  amount: number;
  currency: string;
  status: string;
}

/** Orden de servicio (ODS) del embarque de exportación (CL-EXP-13, BO-EXP-09). */
export interface ShipmentServiceOrder {
  id: string;
  orderNumber: string;
  orderType: string;
  status: string;
  requestedAt: string;
  completedAt: string | null;
}

/**
 * Detalle del embarque según los permisos del usuario (M2-06, M1-11). Un bloque en `null`
 * es información que la matriz no habilita; `allowedActions` y `canOperate` deciden los botones.
 */
export interface ShipmentDetail {
  id: string;
  blNumber: string;
  bookingNumber: string | null;
  operation: ShipmentOperation;
  status: string;
  country: 'CL' | 'BO';
  vessel: string | null;
  voyage: string | null;
  portOfLoading: string | null;
  portOfDischarge: string | null;
  placeOfDelivery: string | null;
  etd: string | null;
  eta: string | null;
  shipper: string | null;
  consignee: string | null;
  roles: ShipmentRole[];
  accessSource: ShipmentAccessSource;
  allowedActions: string[];
  canOperate: boolean;
  /** El BL se ve por acceso abierto y la organización puede asociarse a él (M1-18). */
  canSelfAssociate: boolean;
  /** Agencia o transportista que ve el BL solo por acceso abierto: debe asociarse antes de pagar (M1-18). */
  requiresAssociationForPayment: boolean;
  freight: ShipmentFreight | null;
  containers: BLContainer[];
  localCharges: LocalCharge[] | null;
  demurrageCharges: DemurrageCharge[] | null;
  serviceOrders: ShipmentServiceOrder[];
}

/** Filtros del listado (M2-06): BL, booking, nave, viaje, estado, operación y país. */
export interface ShipmentSearch {
  blNumber?: string;
  bookingNumber?: string;
  vessel?: string;
  voyage?: string;
  status?: string;
  operation?: ShipmentOperationFilter;
  country?: 'CL' | 'BO' | '';
  page?: number;
  pageSize?: number;
}

/** Códigos de acción de la matriz de M1-11 que habilitan botones en el detalle. */
export const SHIPMENT_ACTIONS = {
  PAY_FREIGHT: 'freight.pay',
  PAY_MANDATORY_LOCAL_CHARGES: 'local-charges-mandatory.pay',
  PAY_ON_DEMAND_LOCAL_CHARGES: 'local-charges-on-demand.pay',
  PAY_IMPORT_DEMURRAGE: 'import-demurrage.pay',
  REQUEST_WAREHOUSE_CHANGE: 'warehouse-change.request',
} as const;
