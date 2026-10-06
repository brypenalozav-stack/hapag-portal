/**
 * Canal de requerimientos vía Web Service (Fase 2, Ola J, M3-17): clientes del canal que administra el área interna
 * (permiso `api-clients.manage`). El canal (`/api/ws/v1`, cabecera `X-Api-Key`) es de máquina a máquina y no lo usa la
 * aplicación; aquí solo se administran sus clientes, claves y bitácora. La clave se muestra una sola vez al crearla o
 * rotarla: el servidor guarda solo su hash. Las propiedades en null no llegan en el JSON (el backend las omite).
 */

/** Procesos que el cliente puede usar por el canal. */
export type ApiClientScope = 'responsibility-letter' | 'warehouse-change';
export const API_CLIENT_SCOPES: readonly ApiClientScope[] = ['responsibility-letter', 'warehouse-change'];

export type ApiClientStatus = 'Active' | 'Revoked';
export const API_CLIENT_STATUSES: readonly ApiClientStatus[] = ['Active', 'Revoked'];

/** Operación registrada en la bitácora: las tres primeras crean algo; el resto son consultas. */
export type WsOperation =
  | 'ResponsibilityLetter'
  | 'WarehouseChange'
  | 'WarehouseChangeBatch'
  | 'ResponsibilityLetterTerms'
  | 'ListRequests'
  | 'GetRequest'
  | 'ClientInfo';
export const WS_OPERATIONS: readonly WsOperation[] = [
  'ResponsibilityLetter',
  'WarehouseChange',
  'WarehouseChangeBatch',
  'ResponsibilityLetterTerms',
  'ListRequests',
  'GetRequest',
  'ClientInfo',
];

/** Resultado: en proceso, aceptada (2xx), rechazada (4xx) o fallida (5xx, se reintenta con la misma Idempotency-Key). */
export type WsOutcome = 'Processing' | 'Accepted' | 'Rejected' | 'Failed';
export const WS_OUTCOMES: readonly WsOutcome[] = ['Processing', 'Accepted', 'Rejected', 'Failed'];

/** Límites del alta (contrato del backend). */
export const API_CLIENT_LIMITS = {
  NAME_MAX: 150,
  RATE_LIMIT_MIN: 1,
  RATE_LIMIT_MAX: 600,
  RATE_LIMIT_DEFAULT: 60,
  /** Período de gracia de la rotación: hasta 7 días. */
  GRACE_MINUTES_MAX: 10080,
  NOTES_MAX: 1000,
  REVOKE_REASON_MAX: 500,
  LOG_PAGE_SIZE: 50,
} as const;

/** Contrato OpenAPI del canal, publicado en el repositorio (CT-WS, propuesta). */
export const WS_OPENAPI_DOC = 'docs/integraciones/ws-clientes.openapi.yaml';
/** Base del canal para los sistemas del cliente. */
export const WS_BASE_PATH = '/api/ws/v1';

/** Firmante de la carta de responsabilidad que el cliente emite por el canal (M6-06). */
export interface ApiClientSignatory {
  name?: string | null;
  taxId?: string | null;
  position?: string | null;
  email?: string | null;
}

/** Clave del cliente: solo su prefijo y fechas (nunca el valor). */
export interface ApiClientKey {
  id: string;
  prefix: string;
  createdAt: string;
  createdBy: string;
  /** Fin del período de gracia tras una rotación. */
  expiresAt?: string | null;
  revokedAt?: string | null;
  revokedBy?: string | null;
  lastUsedAt?: string | null;
  active: boolean;
}

export interface ApiClient {
  id: string;
  name: string;
  organizationId: string;
  organizationName: string;
  organizationTaxId: string;
  technicalUserId: string;
  technicalUserEmail: string;
  status: ApiClientStatus;
  scopes: ApiClientScope[];
  rateLimitPerMinute: number;
  signatory: ApiClientSignatory;
  technicalContactEmail?: string | null;
  notes?: string | null;
  lastUsedAt?: string | null;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  revokedAt?: string | null;
  revokedBy?: string | null;
  revocationReason?: string | null;
  keys: ApiClientKey[];
}

/** Cliente con la clave recién emitida: `apiKey` se muestra una sola vez y no se puede recuperar. */
export interface ApiClientSecret {
  client: ApiClient;
  apiKey: string;
  keyPrefix: string;
}

/** POST /admin/api-clients. */
export interface CreateApiClientRequest {
  organizationId: string;
  name: string;
  scopes: ApiClientScope[];
  rateLimitPerMinute: number;
  signatory: ApiClientSignatory | null;
  technicalContactEmail: string | null;
  notes: string | null;
}

/** Fila de la bitácora del canal. */
export interface ApiClientRequestLog {
  id: string;
  keyId?: string | null;
  operation: WsOperation;
  method: string;
  path: string;
  idempotencyKey?: string | null;
  outcome: WsOutcome;
  statusCode?: number | null;
  errorCode?: string | null;
  targetType?: string | null;
  targetId?: string | null;
  targetReference?: string | null;
  blNumber?: string | null;
  sourceAddress?: string | null;
  receivedAt: string;
  completedAt?: string | null;
  durationMs?: number | null;
}

export interface ApiClientFilters {
  organizationId?: string;
  status?: string;
}
