/**
 * Servicios on demand configurables (Fase 2, Ola G): definiciones administrables con su formulario tipado,
 * condiciones, cobro y equipos (M2-03, M2-04) y la solicitud genérica que las usa (sellos M3-07, Late Arrival y
 * Early M3-08, Drop Off M3-09, XOM M3-10, correcciones de BL M3-12, BL hijo M3-13, matriz fuera de plazo M3-14 y
 * Gate In por devolución M3-15). El servidor decide la disponibilidad, el cobro y los permisos (M1-11, NF-05); las
 * propiedades en null no llegan en el JSON. Fechas `yyyy-MM-dd` (DateOnly) o ISO-8601 UTC.
 */

import { TariffBreakdownLine } from './tariff.model';

export type ServiceRequestStatus =
  | 'Draft'
  | 'Submitted'
  | 'PendingApproval'
  | 'Approved'
  | 'Rejected'
  | 'PendingPayment'
  | 'Paid'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';

/** Estados que se ofrecen en los filtros (el borrador solo lo ve el cliente). */
export const SERVICE_REQUEST_STATUSES: readonly ServiceRequestStatus[] = [
  'Draft',
  'Submitted',
  'PendingApproval',
  'Approved',
  'Rejected',
  'PendingPayment',
  'Paid',
  'InProgress',
  'Completed',
  'Cancelled',
];

/** Equipo interno que aprueba o atiende la solicitud. */
export type ServiceTeam = 'None' | 'ED' | 'CustomerService';
export const SERVICE_TEAMS: readonly ServiceTeam[] = ['None', 'ED', 'CustomerService'];

export type ServicePricingMode = 'None' | 'Tariff' | 'SourceCharge';
export const SERVICE_PRICING_MODES: readonly ServicePricingMode[] = ['None', 'Tariff', 'SourceCharge'];

export type ServiceQuantityMode = 'PerRequest' | 'PerContainer';
export const SERVICE_QUANTITY_MODES: readonly ServiceQuantityMode[] = ['PerRequest', 'PerContainer'];

export type ServiceMilestone = 'None' | 'VesselDeparture' | 'VesselArrival' | 'CustomsDeadline';
export const SERVICE_MILESTONES: readonly ServiceMilestone[] = ['None', 'VesselDeparture', 'VesselArrival', 'CustomsDeadline'];

export type ServiceTimingRule = 'None' | 'InTimeAndLate' | 'LateOnly';
export const SERVICE_TIMING_RULES: readonly ServiceTimingRule[] = ['None', 'InTimeAndLate', 'LateOnly'];

export type ServiceAvailabilityWindow = 'Always' | 'BeforeDeparture' | 'AfterDeparture' | 'AfterArrival';
export const SERVICE_AVAILABILITY_WINDOWS: readonly ServiceAvailabilityWindow[] = ['Always', 'BeforeDeparture', 'AfterDeparture', 'AfterArrival'];

/** Referencia con que se pide el servicio (la API acepta BL o booking; es una pista para la pantalla). */
export type ServiceReferenceType = 'BL' | 'Booking';
export const SERVICE_REFERENCE_TYPES: readonly ServiceReferenceType[] = ['BL', 'Booking'];

export type ServiceOperation = 'IMPORT' | 'EXPORT';
export const SERVICE_OPERATIONS: readonly ServiceOperation[] = ['IMPORT', 'EXPORT'];
export const SERVICE_COUNTRIES = ['CL', 'BO'] as const;

/** Tipo de un campo del formulario del servicio. */
export type ServiceFieldType = 'text' | 'textarea' | 'date' | 'number' | 'select' | 'file' | 'containers';
export const SERVICE_FIELD_TYPES: readonly ServiceFieldType[] = ['text', 'textarea', 'date', 'number', 'select', 'file', 'containers'];

/** Dentro de plazo, fuera de plazo o sin plazo (M3-13, M3-14). */
export type ServiceQuoteTiming = 'NotApplicable' | 'InTime' | 'Late';

export type ServiceActorKind = 'Client' | 'Internal' | 'System';

/** Clave reservada del documento de salida que sube el equipo interno. */
export const OUTPUT_FIELD_KEY = '_output';

/** Límites del formulario y de los adjuntos (contrato del backend). */
export const SERVICE_FORM_LIMITS = {
  MAX_FIELDS: 30,
  DEFAULT_TEXT_LENGTH: 500,
  DEFAULT_TEXTAREA_LENGTH: 4000,
  MAX_FILE_BYTES: 10 * 1024 * 1024,
  FILE_TYPES: ['application/pdf', 'image/png', 'image/jpeg'] as readonly string[],
  FILE_ACCEPT: '.pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg',
} as const;

export interface ServiceInputOption {
  value: string;
  labelEs: string;
  labelEn: string;
}

/** Campo tipado del formulario: `min`/`max`/`integer` para números, `maxLength` para textos, `options` para selecciones. */
export interface ServiceInputField {
  key: string;
  labelEs: string;
  labelEn: string;
  type: ServiceFieldType;
  required?: boolean;
  options?: ServiceInputOption[] | null;
  min?: number | null;
  max?: number | null;
  integer?: boolean;
  maxLength?: number | null;
  helpEs?: string | null;
  helpEn?: string | null;
}

/** Respuesta de un campo: texto, fecha (yyyy-MM-dd), número, opción o lista de contenedores. */
export type ServiceInputValue = string | number | string[];
export type ServiceInputValues = Record<string, ServiceInputValue>;

export interface ServiceQuoteLine {
  containerNumber?: string | null;
  containerType?: string | null;
  amount: number;
  tariffCode?: string | null;
  breakdown: TariffBreakdownLine[];
}

/** Cargo del sistema de origen vinculado (modo SourceCharge) con las reglas de Nexus (M4-01, M4-02). */
export interface ServiceSourceCharge {
  chargeId: string;
  conceptCode: string;
  description?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  outcome: 'Payable' | 'PartiallyExempt' | 'Exempt';
  payableTotal: number;
}

/** Cobro del servicio: tramo vigente (NF-22), dentro o fuera de plazo, exención y monto con impuesto. */
export interface ServiceQuote {
  pricingMode: ServicePricingMode;
  chargeConceptCode?: string | null;
  requiresPayment: boolean;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency?: string | null;
  taxRate: number;
  quantity: number;
  tierUnit?: string | null;
  measuredUnits?: number | null;
  timing: ServiceQuoteTiming;
  milestoneAt?: string | null;
  milestoneSource?: string | null;
  tariffId?: string | null;
  tariffCode?: string | null;
  tariffSource?: string | null;
  isExempt: boolean;
  exemptionReference?: string | null;
  excludedContainers: string[];
  lines: ServiceQuoteLine[];
  sourceCharges: ServiceSourceCharge[];
  timeZone: string;
  quotedAt: string;
}

export interface AvailableService {
  definitionId: string;
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs?: string | null;
  descriptionEn?: string | null;
  referenceType: ServiceReferenceType;
  available: boolean;
  canRequest: boolean;
  unavailableReasons: string[];
  inputSchema: ServiceInputField[];
  billingDataRequired: boolean;
  tariffAcceptanceRequired: boolean;
  approvalTeam: ServiceTeam;
  fulfillmentTeam: ServiceTeam;
  /** Estimación con todos los contenedores y sin datos del formulario (solo si está disponible). */
  quote?: ServiceQuote | null;
  /** Código de error cuando la estimación no se puede calcular (p. ej. falta una medida del formulario). */
  quoteErrorCode?: string | null;
}

export interface ShipmentContainerOption {
  containerNumber: string;
  containerType: string;
  status: string;
  isShipperOwned: boolean;
}

/** GET /service-requests/available. */
export interface AvailableServices {
  blId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  operation: ServiceOperation;
  status: string;
  etd?: string | null;
  eta?: string | null;
  timeZone: string;
  containers: ShipmentContainerOption[];
  services: AvailableService[];
  evaluatedAt: string;
}

/** Datos de facturación de la solicitud (M3-07 a M3-14). */
export interface ServiceBillingData {
  taxId?: string | null;
  name?: string | null;
  address?: string | null;
  email?: string | null;
  activity?: string | null;
}

export interface ServiceRequestEvent {
  id: string;
  fromStatus?: ServiceRequestStatus | null;
  toStatus: ServiceRequestStatus;
  occurredAt: string;
  actorName: string;
  actorKind: ServiceActorKind;
  notes?: string | null;
}

export interface ServiceRequestAttachment {
  id: string;
  fieldKey: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
  uploadedBy: string;
}

/** Cargo local que cobra la solicitud; se agrega al carro con `itemType` y `chargeId` (POST /cart/items). */
export interface ServiceRequestCharge {
  chargeId: string;
  itemType: 'LocalCharge';
  conceptCode: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: 'Pending' | 'Paid' | 'Exempt';
  generated: boolean;
}

export interface ServiceRequestSummary {
  id: string;
  requestNumber: string;
  definitionCode: string;
  nameEs: string;
  nameEn: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  operation: ServiceOperation;
  status: ServiceRequestStatus;
  assignedTeam?: ServiceTeam | null;
  totalAmount: number;
  currency?: string | null;
  isExempt: boolean;
  organizationName?: string | null;
  requestedByEmail?: string | null;
  createdAt: string;
  statusChangedAt: string;
}

export interface ServiceRequestDetail {
  id: string;
  requestNumber: string;
  definitionId: string;
  definitionCode: string;
  nameEs: string;
  nameEn: string;
  organizationId: string;
  organizationName?: string | null;
  requestedByEmail?: string | null;
  onBehalfOfOrganizationId?: string | null;
  billOfLadingId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  operation: ServiceOperation;
  timeZone: string;
  containerNumbers: string[];
  inputSchema: ServiceInputField[];
  inputValues: ServiceInputValues;
  billing: ServiceBillingData;
  billingDataRequired: boolean;
  tariffAcceptanceRequired: boolean;
  /** Fijado al enviar (no viene en los borradores). */
  quote?: ServiceQuote | null;
  tariffAcceptedAt?: string | null;
  status: ServiceRequestStatus;
  statusChangedAt: string;
  assignedTeam?: ServiceTeam | null;
  approvalTeam: ServiceTeam;
  fulfillmentTeam: ServiceTeam;
  requiresOutputDocument: boolean;
  /** Motivo del rechazo o notas del equipo, visibles para el cliente. */
  resolutionNotes?: string | null;
  createdAt: string;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  paidAt?: string | null;
  completedAt?: string | null;
  cancelledAt?: string | null;
  charges: ServiceRequestCharge[];
  paymentId?: string | null;
  paymentNumber?: string | null;
  receiptNumber?: string | null;
  canEdit: boolean;
  canSubmit: boolean;
  canCancel: boolean;
  attachments: ServiceRequestAttachment[];
  timeline: ServiceRequestEvent[];
}

/** Referencia del embarque: número de BL o de booking. */
export interface ServiceReference {
  blNumber?: string | null;
  bookingNumber?: string | null;
}

/** POST /service-requests/quote. */
export interface QuoteServiceRequest extends ServiceReference {
  definitionCode: string;
  inputValues: ServiceInputValues;
}

/** POST /service-requests: crea el borrador y, con `submit`, lo envía. */
export interface CreateServiceRequest extends ServiceReference {
  definitionCode: string;
  inputValues: ServiceInputValues;
  billing: ServiceBillingData | null;
  submit: boolean;
  acceptTariff?: boolean;
  acceptedTotal?: number | null;
}

/** PUT /service-requests/{id} (solo borradores; null = se mantiene). */
export interface UpdateServiceRequest {
  inputValues: ServiceInputValues | null;
  billing: ServiceBillingData | null;
}

export interface SubmitServiceRequest {
  acceptTariff?: boolean;
  acceptedTotal?: number | null;
}

export interface ServiceRequestFilters {
  status?: string;
  definitionCode?: string;
  blNumber?: string;
  page?: number;
  pageSize?: number;
}

/** Filtros de la bandeja interna; sin estado, BL ni organización solo trae lo que hay que atender. */
export interface AdminServiceRequestFilters extends ServiceRequestFilters {
  team?: string;
  country?: string;
  organizationId?: string;
}

// ---------------------------------------------------------------------------
// Mantenedor de definiciones (M2-03, M2-04, NF-15)
// ---------------------------------------------------------------------------

/** Cuerpo de POST y PUT /service-definitions. */
export interface ServiceDefinitionInput {
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs: string | null;
  descriptionEn: string | null;
  operations: ServiceOperation[];
  countries: string[];
  referenceType: ServiceReferenceType;
  requiredBlStatuses: string[] | null;
  availabilityWindow: ServiceAvailabilityWindow;
  requiresContainers: boolean;
  allowMultiplePerBl: boolean;
  inputSchema: ServiceInputField[];
  billingDataRequired: boolean;
  tariffAcceptanceRequired: boolean;
  pricingMode: ServicePricingMode;
  chargeConceptCode: string | null;
  tariffCode: string | null;
  lateTariffCode: string | null;
  quantityMode: ServiceQuantityMode;
  measureFieldKey: string | null;
  milestone: ServiceMilestone;
  milestoneOffsetHours: number;
  deadlineRuleCode: string | null;
  timingRule: ServiceTimingRule;
  taxable: boolean;
  exemptionConcept: string | null;
  excludeShipperOwnedContainers: boolean;
  approvalTeam: ServiceTeam;
  fulfillmentTeam: ServiceTeam;
  requiresOutputDocument: boolean;
  actionCode: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ServiceDefinition extends ServiceDefinitionInput {
  id: string;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

/** Instantánea del registro de cambios: operaciones y países en CSV, formulario como JSON. */
export interface ServiceDefinitionSnapshot {
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs?: string | null;
  descriptionEn?: string | null;
  operations: string;
  countries: string;
  referenceType: string;
  requiredBlStatuses?: string | null;
  availabilityWindow: string;
  requiresContainers: boolean;
  allowMultiplePerBl: boolean;
  inputSchemaJson: string;
  billingDataRequired: boolean;
  tariffAcceptanceRequired: boolean;
  pricingMode: string;
  chargeConceptCode?: string | null;
  tariffCode?: string | null;
  lateTariffCode?: string | null;
  quantityMode: string;
  measureFieldKey?: string | null;
  milestone: string;
  milestoneOffsetHours: number;
  deadlineRuleCode?: string | null;
  timingRule: string;
  taxable: boolean;
  exemptionConcept?: string | null;
  excludeShipperOwnedContainers: boolean;
  approvalTeam: string;
  fulfillmentTeam: string;
  requiresOutputDocument: boolean;
  actionCode: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ServiceDefinitionChange {
  id: string;
  definitionId: string;
  action: 'Created' | 'Updated' | 'Deactivated';
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: ServiceDefinitionSnapshot | null;
  current?: ServiceDefinitionSnapshot | null;
}
