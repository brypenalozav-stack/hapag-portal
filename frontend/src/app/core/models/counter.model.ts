/** Fase 2, Ola I: Counter Bolivia/Ultramar (M8-09): canje del BL, recepción del HBL y desconsolidado. */

export type CounterSyncStatus = 'Pending' | 'Synced' | 'Failed';

export const COUNTER_SYNC_STATUSES: readonly CounterSyncStatus[] = ['Pending', 'Synced', 'Failed'];

export interface CounterRecord {
  id: string;
  billOfLadingId: string;
  blNumber: string;
  country: 'CL' | 'BO';
  exchangeDate?: string | null;
  hblReceived: boolean;
  hblReceivedAt?: string | null;
  deconsolidated: boolean;
  deconsolidatedAt?: string | null;
  notes?: string | null;
  syncStatus: CounterSyncStatus;
  syncedAt?: string | null;
  syncError?: string | null;
  sourceReference?: string | null;
  recordedBy?: string | null;
  recordedAt?: string | null;
  createdAt: string;
  createdBy?: string | null;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface CounterShipment {
  id: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  operation?: string | null;
  vessel?: string | null;
  voyage?: string | null;
  portOfDischarge?: string | null;
  placeOfDelivery?: string | null;
}

/** Lo que registra el sistema de origen (Nexus) para el BL. */
export interface CounterSource {
  blNumber: string;
  country?: string | null;
  exchangeDate?: string | null;
  hblReceived: boolean;
  hblReceivedAt?: string | null;
  deconsolidated: boolean;
  deconsolidatedAt?: string | null;
  sourceReference?: string | null;
  recordedAt?: string | null;
  recordedBy?: string | null;
}

export interface CounterDetail {
  shipment: CounterShipment;
  record?: CounterRecord | null;
  sourceAvailable: boolean;
  source?: CounterSource | null;
  sourceErrorCode?: string | null;
}

export interface CounterRecordRequest {
  country: 'CL' | 'BO';
  exchangeDate?: string | null;
  hblReceived: boolean;
  hblReceivedAt?: string | null;
  deconsolidated: boolean;
  deconsolidatedAt?: string | null;
  notes?: string | null;
}

export interface CounterSearch {
  blNumber?: string;
  country?: string;
  syncStatus?: string;
  deconsolidated?: string;
  page?: number;
  pageSize?: number;
}

export interface CounterSnapshot {
  country?: string;
  exchangeDate?: string | null;
  hblReceived?: boolean;
  hblReceivedAt?: string | null;
  deconsolidated?: boolean;
  deconsolidatedAt?: string | null;
  notes?: string | null;
  syncStatus?: string;
}
