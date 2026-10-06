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

/** Tipo de documento de transporte (M2-02). */
export type TransportDocumentType = 'BL' | 'SWB' | 'EBL';

/** Estado de emisión del documento de transporte según el portal (M2-02); `Unknown` = código no reconocido. */
export type BlIssuanceStatus =
  | 'Pending'
  | 'Issued'
  | 'AuthorizedAtDestination'
  | 'IssuedAtDestination'
  | 'Transferred'
  | 'Surrendered'
  | 'TelexReleased'
  | 'Cancelled'
  | 'Unknown';

/** Último estado de emisión conocido (listado) o guardado en el portal cuando el origen no responde. */
export interface ShipmentIssuanceSummary {
  documentType?: TransportDocumentType | null;
  /** Plataforma del EBL (p. ej. WAVE). */
  eblPlatform?: string | null;
  status?: BlIssuanceStatus | null;
  statusAt?: string | null;
}

/**
 * Estado de emisión leído del origen (FIS) en el momento (M2-02). Si el origen no responde, `available` es
 * false, `errorCode` explica la falla (NF-11) y `lastKnown` trae el último estado registrado, que no se
 * presenta como vigente.
 */
export interface ShipmentIssuance {
  blNumber: string;
  country: 'CL' | 'BO';
  operation: ShipmentOperation;
  available: boolean;
  source: string;
  documentType?: TransportDocumentType | null;
  eblPlatform?: string | null;
  status?: BlIssuanceStatus | null;
  /** Código tal como lo informa el origen. */
  sourceStatus?: string | null;
  statusAt?: string | null;
  issuancePlace?: string | null;
  retrievedAt: string;
  errorCode?: string | null;
  lastKnown?: ShipmentIssuanceSummary | null;
}

/** Motivo de publicación por DIFU de destino final (M2-01): los tres primeros publican el BL. */
export type PublicationReasonCode = 'NO_RULE' | 'SAME_AS_DISCHARGE' | 'DIFU_ASSOCIATED' | 'DIFU_MISSING' | 'DIFU_OTHER_LOCATION';

/** Publicación del BL (M2-01), solo para el administrador interno. */
export interface ShipmentPublication {
  published: boolean;
  reasonCode: PublicationReasonCode;
  ruleId?: string | null;
  finalDestinationCode?: string | null;
  portOfDischargeCode?: string | null;
  difuCode?: string | null;
  difuLocationCode?: string | null;
}

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
  /** Último estado de emisión conocido (M2-02). */
  issuance?: ShipmentIssuanceSummary | null;
  /** Solo para el administrador interno (M2-01). */
  publication?: ShipmentPublication | null;
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
  /** UN/LOCODE del puerto de descarga y del destino final (M2-01, M2-09). */
  portOfDischargeCode?: string | null;
  finalDestinationCode?: string | null;
  /** Estado de emisión leído del origen; llega si la matriz habilita `bl-issuance.view` (M2-02). */
  issuance?: ShipmentIssuance | null;
  /** Solo para el administrador interno (M2-01). */
  publication?: ShipmentPublication | null;
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
  /** Solo el administrador interno: publicados (true) o no publicados (false) por las reglas de M2-01. */
  published?: boolean | '';
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
  VIEW_BL_ISSUANCE: 'bl-issuance.view',
  DOWNLOAD_TATC: 'tatc.download',
} as const;

// ---------------------------------------------------------------------------
// Fase 1, Ola F: TATC del BL (M2-09) y reglas de publicación por DIFU (M2-01).
// ---------------------------------------------------------------------------

/** Estado del TATC de un contenedor; el del BL agrega `PartiallyIssued` y `NotRegistered`. */
export type TatcStatus = 'NotIssued' | 'PreTatc' | 'Issued' | 'Cancelled' | 'Unknown' | 'PartiallyIssued' | 'NotRegistered';

/** Motivo por el que el TATC de un contenedor aún no se emite. */
export type TatcPendingReason = 'PAYMENT_PENDING' | 'MHD_PENDING' | 'DOCUMENT_PENDING' | 'OTHER';

export interface ContainerTatc {
  containerNumber: string;
  tatcNumber?: string | null;
  status: TatcStatus;
  sourceStatus: string;
  issuedAt?: string | null;
  warehouseCode?: string | null;
  pendingReasons: TatcPendingReason[];
}

/**
 * Estado del BL y de su TATC (M2-09), leído del sistema de TATC en el momento. Si el sistema no responde,
 * `available` es false, `errorCode` explica la falla (NF-11) y no hay contenedores ni estado.
 */
export interface ShipmentTatc {
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  blStatus: string;
  eta?: string | null;
  available: boolean;
  status?: TatcStatus | null;
  containers: ContainerTatc[];
  sourceUpdatedAt?: string | null;
  retrievedAt: string;
  errorCode?: string | null;
  /** La organización propia, con perfil operativo, puede pedir la generación (BL no emitido por completo). */
  canRequestGeneration: boolean;
}

export type TatcBatchStatus = 'Completed' | 'CompletedWithErrors' | 'Failed';

/** `Rejected`: lo rechazó el sistema de TATC; `Failed`: validación del portal u origen caído. */
export type TatcBatchItemStatus = 'Accepted' | 'Rejected' | 'Failed';

export interface TatcBatchItem {
  lineNumber: number;
  blNumber: string;
  billOfLadingId?: string | null;
  status: TatcBatchItemStatus;
  reasonCode?: string | null;
}

/** Solicitud masiva de TATC para BL de una misma localidad (M2-09); el listado no trae `items`. */
export interface TatcBatch {
  id: string;
  country: 'CL' | 'BO';
  locationCode: string;
  status: TatcBatchStatus;
  sourceRequestId?: string | null;
  errorCode?: string | null;
  totalItems: number;
  acceptedItems: number;
  rejectedItems: number;
  requestedBy: string;
  createdAt: string;
  completedAt?: string | null;
  items?: TatcBatchItem[] | null;
}

export interface TatcBatchRequest {
  locationCode: string;
  blNumbers: string[];
}

/** Máximo de BL por solicitud masiva de TATC. */
export const TATC_BATCH_MAX_ITEMS = 500;

/**
 * Regla de publicación por DIFU de destino final (M2-01): los BL del país con ese destino final (y descargados
 * en el puerto, si se indica) se publican solo si el origen informa un DIFU asociado al destino.
 */
export interface ShipmentPublicationRule {
  id: string;
  country: 'CL' | 'BO';
  finalDestinationCode: string;
  finalDestinationName?: string | null;
  /** null = cualquier puerto de descarga. */
  dischargePortCode?: string | null;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface ShipmentPublicationRuleRequest {
  country: 'CL' | 'BO';
  finalDestinationCode: string;
  finalDestinationName: string | null;
  dischargePortCode: string | null;
  description: string | null;
}

export interface ShipmentPublicationRuleSnapshot {
  country: 'CL' | 'BO';
  finalDestinationCode: string;
  finalDestinationName?: string | null;
  dischargePortCode?: string | null;
  description?: string | null;
  isActive: boolean;
}

/** Entrada del registro de cambios de una regla (NF-15). */
export interface ShipmentPublicationRuleChange {
  id: string;
  ruleId: string;
  action: string;
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: ShipmentPublicationRuleSnapshot | null;
  current?: ShipmentPublicationRuleSnapshot | null;
}
