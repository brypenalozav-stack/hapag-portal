/** Tipo de organización declarado en el registro (M1-07). `Internal` es Hapag-Lloyd. */
export type OrganizationType = 'Customer' | 'FreightForwarder' | 'CustomsAgency' | 'Carrier' | 'Internal';

/** Tipos que una organización puede declarar al registrarse (M1-07). */
export const REGISTRABLE_ORGANIZATION_TYPES: readonly OrganizationType[] = [
  'Customer',
  'FreightForwarder',
  'CustomsAgency',
  'Carrier',
];

/** Estado del registro (M1-07 / M8-04): solo `Approved` opera. */
/** `PreCreated`: transportista pre-creado por un cliente; pasa a `Approved` en su primer ingreso (Fase 2, Ola I, M1-09). */
export type OrganizationStatus = 'PendingValidation' | 'PendingArCheck' | 'Approved' | 'Rejected' | 'PreCreated';

export const ORGANIZATION_STATUSES: readonly OrganizationStatus[] = [
  'PendingValidation',
  'PendingArCheck',
  'Approved',
  'Rejected',
];

/** Estado de la vinculación del usuario a su organización (M1-08). */
export type MembershipStatus = 'Pending' | 'Active' | 'Rejected';

/** Perfil del usuario dentro de su organización (M1-02). */
export type OrganizationProfile = 'OrgAdmin' | 'OrgOperator' | 'OrgViewer';

export const ORGANIZATION_PROFILES: readonly OrganizationProfile[] = ['OrgAdmin', 'OrgOperator', 'OrgViewer'];

/** Documentación de respaldo del registro (M1-07). */
export type OrganizationDocumentType = 'RegistrationLetter' | 'CreditAuthorization' | 'TaxCertificate' | 'Other';

export const ORGANIZATION_DOCUMENT_TYPES: readonly OrganizationDocumentType[] = [
  'RegistrationLetter',
  'CreditAuthorization',
  'TaxCertificate',
  'Other',
];

/** Formatos y tamaño admitidos para los documentos de respaldo (M1-07). */
export const ORGANIZATION_DOCUMENT_CONTENT_TYPES: readonly string[] = ['application/pdf', 'image/png', 'image/jpeg'];
export const ORGANIZATION_DOCUMENT_MAX_BYTES = 10 * 1024 * 1024;

/** Organización del usuario y su situación (login, refresh-token y GET /organizations/me). */
export interface OrganizationSummary {
  id: string;
  name: string;
  taxId: string;
  taxIdType: string;
  country: 'CL' | 'BO';
  organizationType: OrganizationType;
  status: OrganizationStatus;
  matchCode: string | null;
  operatingCountries: ('CL' | 'BO')[];
  membershipStatus: MembershipStatus;
  profile: OrganizationProfile | null;
  canOperate: boolean;
}

/** Usuario de la propia organización (M1-02). */
export interface OrganizationUser {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  fullName: string;
  phone: string | null;
  profile: OrganizationProfile | null;
  isActive: boolean;
  membershipStatus: MembershipStatus;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface CreateOrganizationUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  profile: OrganizationProfile;
  phone?: string;
}

export interface UpdateOrganizationUserRequest {
  firstName: string;
  lastName: string;
  phone?: string;
  profile?: OrganizationProfile;
}

/** Solicitud pendiente de vinculación a la organización (M1-08). */
export interface JoinRequest {
  userId: string;
  email: string;
  fullName: string;
  phone: string | null;
  requestedAt: string;
  organizationId: string;
  organizationName: string;
}

/** País de operación del usuario (M1-04). */
export interface OperatingCountry {
  country: 'CL' | 'BO';
  availableCountries: ('CL' | 'BO')[];
  canChange: boolean;
}

/** Documento de respaldo adjunto al registro (M1-07). */
export interface OrganizationDocument {
  id: string;
  documentType: OrganizationDocumentType;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

/** Solicitud para unirse a una organización ya registrada (M1-08). */
export interface JoinOrganizationRequest {
  taxId: string;
  country: 'CL' | 'BO';
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
}

export interface JoinOrganizationResponse {
  userId: string;
  organizationName: string;
  status: MembershipStatus;
}

/** Fila de la bandeja interna de organizaciones (M8-04, M8-06). */
export interface AdminOrganizationItem {
  id: string;
  name: string;
  taxId: string;
  taxIdType: string;
  country: 'CL' | 'BO';
  organizationType: OrganizationType;
  status: OrganizationStatus;
  matchCode: string | null;
  email: string;
  phone: string | null;
  userCount: number;
  createdAt: string;
}

/** Detalle para validar el cliente, registrar el control con AR y asignar el Match Code (M8-04). */
export interface AdminOrganizationDetail {
  id: string;
  name: string;
  taxId: string;
  taxIdType: string;
  country: 'CL' | 'BO';
  organizationType: OrganizationType;
  status: OrganizationStatus;
  matchCode: string | null;
  operatingCountries: ('CL' | 'BO')[];
  email: string;
  phone: string | null;
  address: string | null;
  city: string | null;
  createdAt: string;
  validatedAt: string | null;
  validatedBy: string | null;
  arCheckedAt: string | null;
  arCheckedBy: string | null;
  arReference: string | null;
  approvedAt: string | null;
  rejectedAt: string | null;
  reviewNotes: string | null;
  users: OrganizationUser[];
  documents: OrganizationDocument[];
}

export interface AdminOrganizationSearch {
  status?: OrganizationStatus | '';
  organizationType?: OrganizationType | '';
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ArCheckRequest {
  matchCode: string;
  arReference?: string;
  operatingCountries?: ('CL' | 'BO')[];
  notes?: string;
}
