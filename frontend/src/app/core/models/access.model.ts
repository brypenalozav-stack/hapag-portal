import { OrganizationType } from './organization.model';

/**
 * Accesos a terceros (Fase 1, Ola B): otorgamiento individual, masivo y por defecto, vigencia,
 * permisos granulares, mandatos, acceso abierto, acceso por booking, ampliaciones y auditoría
 * (M1-03, M1-12 a M1-24). Los códigos de acción son los de la matriz de M1-11.
 */

/** Tipo de vigencia (M1-14): sin término, por cantidad de días o hasta una fecha. */
export type AccessValidityType = 'Indefinite' | 'Duration' | 'UntilDate';

export const ACCESS_VALIDITY_TYPES: readonly AccessValidityType[] = ['Indefinite', 'Duration', 'UntilDate'];

/** Límites de la vigencia por cantidad de días (contrato del backend). */
export const ACCESS_DURATION_MIN_DAYS = 1;
export const ACCESS_DURATION_MAX_DAYS = 3650;

/** Estado de un acceso; `Active` con la vigencia cumplida llega como `Expired`. */
export type AccessGrantStatus = 'PendingAcceptance' | 'Active' | 'Expired' | 'Revoked' | 'Reconciled';

export const ACCESS_GRANT_STATUSES: readonly AccessGrantStatus[] = [
  'PendingAcceptance',
  'Active',
  'Expired',
  'Revoked',
  'Reconciled',
];

/** Origen del acceso: individual, masivo, por defecto (M1-13) o anticipado por booking (M1-20). */
export type AccessGrantType = 'Individual' | 'Bulk' | 'Default' | 'EarlyBooking';

/** Motivo de término: manual, vencimiento, en cadena (M1-22) o reconciliado con el rol oficial (M1-20). */
export type AccessEndReason = 'Manual' | 'Expired' | 'Cascade' | 'Reconciled';

/** Otorgados por mi organización o recibidos de otra. */
export type AccessGrantDirection = 'Given' | 'Received';

/** Rol con el que el futuro receptor del booking quedará en el BL (M1-20). */
export type EarlyBookingRole = 'Shipper' | 'Consignee' | 'ThirdParty';

export const EARLY_BOOKING_ROLES: readonly EarlyBookingRole[] = ['Shipper', 'Consignee', 'ThirdParty'];

/** Rol hacia el que se amplía la visibilidad de un dato del BL (M1-16). */
export type WideningTargetRole = 'Customer' | 'Shipper' | 'Consignee';

export const WIDENING_TARGET_ROLES: readonly WideningTargetRole[] = ['Customer', 'Shipper', 'Consignee'];

export interface OrganizationRef {
  id: string;
  name: string;
  organizationType: OrganizationType;
  country: 'CL' | 'BO';
}

/**
 * Fila de la vista única de accesos (M1-24). `actionCodes` en null = sin permisos explícitos
 * (nivel base de M1-11); `effectiveActionCodes` es lo que el acceso habilita hoy. `canEdit` =
 * lo otorgó mi organización y sigue abierto.
 */
export interface AccessGrant {
  id: string;
  direction: AccessGrantDirection;
  grantor: OrganizationRef;
  grantee: OrganizationRef;
  grantorRole: string;
  billOfLadingId: string | null;
  blNumber: string | null;
  bookingNumber: string | null;
  grantType: AccessGrantType;
  intendedRole: string | null;
  hasExplicitPermissions: boolean;
  actionCodes: string[] | null;
  effectiveActionCodes: string[];
  validityType: AccessValidityType;
  validFrom: string;
  validTo: string | null;
  durationDays: number | null;
  status: AccessGrantStatus;
  isEffective: boolean;
  isMandate: boolean;
  termsVersion: string | null;
  termsAcceptedAt: string | null;
  parentGrantId: string | null;
  defaultGranteeId: string | null;
  createdAt: string;
  endedAt: string | null;
  endReason: AccessEndReason | null;
  canEdit: boolean;
}

export interface AccessGrantSearch {
  direction?: AccessGrantDirection | '';
  status?: AccessGrantStatus | '';
  reference?: string;
  counterpartId?: string;
  page?: number;
  pageSize?: number;
}

/** Vigencia común a otorgar, editar y acceso por booking (M1-14). */
export interface AccessValidity {
  validityType: AccessValidityType;
  validFrom?: string;
  validTo?: string;
  durationDays?: number;
}

/** Flujo único de otorgamiento (M1-12, M1-15, M1-24) y mandato (M1-03). */
export interface GrantAccessRequest extends AccessValidity {
  granteeOrganizationId: string;
  blNumbers?: string[];
  bookingNumbers?: string[];
  actionCodes?: string[] | null;
  isMandate?: boolean;
  acceptTerms?: boolean;
  termsVersion?: string;
}

export interface GrantSkipped {
  reference: string;
  code: string;
  message: string;
}

/** Resultado del otorgamiento: `updated` = registros actualizados (creados + modificados), M1-12. */
export interface GrantAccessResult {
  requested: number;
  updated: number;
  created: number;
  modified: number;
  skipped: GrantSkipped[];
  grants: AccessGrant[];
}

/** Edición en el lugar de vigencia y/o permisos (M1-24). */
export interface UpdateAccessGrantRequest extends Partial<AccessValidity> {
  actionCodes?: string[];
  resetToBaseLevel?: boolean;
}

/** Resultado de una revocación con lo revocado en cadena (M1-22). */
export interface RevokeAccessResult {
  revoked: number;
  cascadeRevoked: number;
  wideningsRevoked: number;
}

export interface MandateTerms {
  version: string;
  title: string;
  summary: string;
}

/** Acceso anticipado por booking (M1-20). */
export interface EarlyBookingAccessRequest extends AccessValidity {
  bookingNumber: string;
  granteeOrganizationId: string;
  intendedRole: EarlyBookingRole;
  actionCodes?: string[];
}

/** Organización que puede recibir un acceso (buscador del flujo único). */
export interface GranteeOrganization {
  id: string;
  name: string;
  taxId: string;
  organizationType: OrganizationType;
  country: 'CL' | 'BO';
}

/** Acción del selector de permisos: `grantable` = puede marcarse; `includedInBaseLevel` = nivel base. */
export interface GrantableAction {
  code: string;
  name: string;
  category: 'Information' | 'Administration';
  kind: 'View' | 'Operate';
  grantorHas: boolean;
  grantable: boolean;
  includedInBaseLevel: boolean;
}

export interface GrantableActionsQuery {
  blNumber?: string;
  granteeOrganizationId?: string;
  granteeOrganizationType?: OrganizationType;
  receiverRole?: EarlyBookingRole;
}

/** Tercero configurado por defecto (M1-13). */
export interface DefaultGrantee {
  id: string;
  grantee: OrganizationRef;
  actionCodes: string[] | null;
  durationDays: number | null;
  createdAt: string;
  modifiedAt: string | null;
}

export interface CreateDefaultGranteeRequest {
  granteeOrganizationId: string;
  actionCodes?: string[];
  durationDays?: number;
}

export interface UpdateDefaultGranteeRequest {
  actionCodes?: string[];
  durationDays?: number | null;
  resetToBaseLevel?: boolean;
}

/** Acceso abierto por número de BL (M1-17) con su único conjunto de permisos. */
export interface OpenAccessSetting {
  isEnabled: boolean;
  actionCodes: string[] | null;
  effectiveActionCodes: string[];
  canManage: boolean;
  changedAt: string | null;
}

export interface UpdateOpenAccessRequest {
  isEnabled: boolean;
  actionCodes?: string[];
  resetToBaseLevel?: boolean;
}

/** Autoasociación a un BL consultado con acceso abierto (M1-18). */
export interface ShipmentAssociation {
  billOfLadingId: string;
  blNumber: string;
  associatedAt: string;
}

/** Ampliación de un dato del BL hacia otro rol (M1-16). */
export interface VisibilityWidening {
  id: string;
  billOfLadingId: string;
  blNumber: string;
  grantor: OrganizationRef;
  grantorRole: string;
  targetRole: WideningTargetRole;
  actionCode: string;
  originGrantId: string | null;
  status: 'Active' | 'Revoked';
  createdAt: string;
  endedAt: string | null;
  endReason: AccessEndReason | null;
  canRevoke: boolean;
}

/** Evento de la auditoría de accesos (M1-23). */
export type AccessAuditEventType =
  | 'GrantCreated'
  | 'GrantPermissionsChanged'
  | 'GrantValidityChanged'
  | 'GrantRevoked'
  | 'GrantExpired'
  | 'GrantRevokedByCascade'
  | 'MandateTermsAccepted'
  | 'BookingAccessLinked'
  | 'BookingAccessReconciled'
  | 'OpenAccessEnabled'
  | 'OpenAccessDisabled'
  | 'OpenAccessPermissionsChanged'
  | 'SelfAssociated'
  | 'WideningCreated'
  | 'WideningRevoked'
  | 'WideningRevokedByCascade'
  | 'DefaultGranteeAdded'
  | 'DefaultGranteeUpdated'
  | 'DefaultGranteeRemoved';

/** Entrada de la auditoría; `actorUserId` null = la acción la hizo el sistema. */
export interface AccessAuditEntry {
  id: string;
  occurredAt: string;
  eventType: AccessAuditEventType;
  billOfLadingId: string | null;
  blNumber: string | null;
  bookingNumber: string | null;
  accessGrantId: string | null;
  visibilityWideningId: string | null;
  grantor: OrganizationRef | null;
  grantee: OrganizationRef | null;
  actorUserId: string | null;
  actorEmail: string | null;
  actorOrganization: OrganizationRef | null;
  details: string | null;
}

export interface AccessAuditSearch {
  blNumber?: string;
  bookingNumber?: string;
  page?: number;
  pageSize?: number;
}

/** Código de acción que el servidor agrega siempre a un conjunto explícito de permisos. */
export const SHIPMENT_VIEW_ACTION = 'shipment.view';
