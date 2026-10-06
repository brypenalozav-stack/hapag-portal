/**
 * Documentos del embarque (Fase 1, Ola E): repositorio documental por BL (M6-09) con el certificado de
 * transbordo (M6-01), el cupón de retiro de Gate Out (M6-03), el comprobante Collect (M6-04), la copia del
 * BL valorada y no valorada (M6-05), la carta de responsabilidad (M6-06) y el certificado de libre deuda
 * de Bolivia (M6-07). Fase 2, Ola J: certificado de flete (M6-02) y carta de liberación y desconsolidado
 * (M6-08) de Bolivia. El servidor filtra cada tipo por la matriz de M1-11 (NF-05). Las propiedades en null
 * no llegan en el JSON (el backend las omite).
 */

import { ServiceRequestDetail, ServiceRequestSummary } from './service-request.model';
import { TatcPendingReason, TatcStatus } from './shipment.model';

/** Tipo de documento del repositorio. */
export type ShipmentDocumentType =
  | 'TransshipmentCertificate'
  | 'GateOutCoupon'
  | 'CollectReceipt'
  | 'BlCopyValued'
  | 'BlCopyNonValued'
  | 'ResponsibilityLetter'
  | 'NoDebtCertificate'
  // Fase 2, Ola H: recibo del pago anticipado de Gate Out de exportación (M3-19).
  | 'GateOutAdvanceReceipt'
  // Fase 2, Ola J: certificado de flete (M6-02, firmado) y carta de liberación y desconsolidado (M6-08) de Bolivia.
  | 'FreightCertificate'
  | 'ReleaseLetter';

/** Estado del documento: una carta más nueva deja la anterior reemplazada. */
export type ShipmentDocumentStatus = 'Issued' | 'Superseded' | 'Revoked';

/** Cómo se emitió: por la cola posterior al pago, a solicitud del usuario o como dato de demostración. */
export type ShipmentDocumentOrigin = 'Payment' | 'Request' | 'Seed';

/** Documento de otro módulo publicado en el repositorio: comprobante de pago (M7-02) o factura (M7-01). */
export type RelatedDocumentKind = 'Receipt' | 'Invoice';

/** Motivo que impide emitir el certificado de libre deuda (M6-07, M3-16). */
export type NoDebtBlockerCode =
  | 'PENDING_CHARGES'
  | 'PENDING_DEMURRAGE'
  | 'PENDING_INVOICES'
  | 'PENDING_FREIGHT'
  | 'ADVANCE_DEMURRAGE';

/** Documento del repositorio del embarque (M6-09). */
export interface ShipmentDocument {
  id: string;
  documentType: ShipmentDocumentType;
  documentNumber: string;
  status: ShipmentDocumentStatus;
  billOfLadingId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  /** Unidades a las que corresponde (cupón de retiro, M6-03). */
  containerNumbers: string[];
  issuedAt: string;
  origin: ShipmentDocumentOrigin;
  issuedForOrganizationId?: string | null;
  issuedForOrganizationName?: string | null;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  /** SHA-256 del PDF; null hasta que el archivo existe (los de demostración se generan en la primera descarga). */
  contentHash?: string | null;
  /** Código impreso en el PDF para verificar el documento. */
  verificationCode: string;
  /** El tipo se emite con firma electrónica (M6-01, M6-07). */
  signed: boolean;
  signatureId?: string | null;
  signatureProvider?: string | null;
  signatureLevel?: string | null;
  signedAt?: string | null;
  paymentId?: string | null;
  /** Correos a los que se envió automáticamente. */
  recipientEmails: string[];
  deliveredAt?: string | null;
  /** Carta de responsabilidad: versión y aceptación de los términos (M6-06). */
  termsVersion?: string | null;
  termsAcceptedAt?: string | null;
  /** Vigencia de la carta; null = sin vencimiento. */
  validUntil?: string | null;
  /** Conservación del documento (NF-16). */
  retainUntil: string;
}

/** Comprobante o factura del BL que se descarga por su propia ruta. */
export interface RelatedDocument {
  kind: RelatedDocumentKind;
  id: string;
  /** Número del comprobante o folio SII de la factura. */
  number: string;
  issuedAt: string;
  description?: string | null;
  /** Ruta absoluta desde el host (`/api/v1/...`). */
  downloadPath: string;
}

/** Qué documentos puede solicitar el usuario sobre el BL (M1-11 y perfil que opera). */
export interface DocumentActions {
  canRequestValuedCopy: boolean;
  canRequestNonValuedCopy: boolean;
  canIssueResponsibilityLetter: boolean;
  canRequestNoDebtCertificate: boolean;
  canRequestTransshipmentCertificate: boolean;
  /** Fase 2, Ola J: importación de Bolivia, acción de M1-11 y perfil que opera (M6-02, M6-08). */
  canRequestFreightCertificate?: boolean;
  canRequestReleaseLetter?: boolean;
}

/** Estado de la carta de responsabilidad del usuario sobre el BL (M4-04, M6-06). */
export interface ResponsibilityLetterState {
  required: boolean;
  status: 'Missing' | 'Fulfilled';
  blocksProcess: boolean;
}

/** GET /documents/{blNumber}. */
export interface ShipmentDocuments {
  blId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  /** Huso del país de la operación (NF-22). */
  timeZone: string;
  /** Más nuevos primero; solo los tipos que el usuario puede ver. */
  documents: ShipmentDocument[];
  related: RelatedDocument[];
  actions: DocumentActions;
  /** Se omite cuando el usuario no es FFWW ni puede emitir la carta. */
  responsibilityLetter?: ResponsibilityLetterState | null;
}

/** Documento emitido o reenviado y los correos a los que se envió (M6-05). */
export interface DocumentDelivery {
  document: ShipmentDocument;
  sentTo: string[];
}

/** POST /documents/{blNumber}/bl-copy. */
export interface BlCopyRequest {
  valued: boolean;
  sendEmail: boolean;
}

/** GET /documents/responsibility-letter/terms. */
export interface ResponsibilityLetterTerms {
  version: string;
  title: string;
  text: string;
}

/** POST /documents/{blNumber}/responsibility-letter. */
export interface ResponsibilityLetterRequest {
  signatoryName: string;
  signatoryTaxId: string;
  signatoryPosition: string;
  contactEmail: string;
  contactPhone: string | null;
  cargoDescription: string | null;
  observations: string | null;
  acceptTerms: boolean;
  termsVersion: string;
}

export interface NoDebtAmount {
  currency: string;
  total: number;
}

export interface NoDebtBlocker {
  code: NoDebtBlockerCode;
  /** Conceptos, contenedores o facturas pendientes; en ADVANCE_DEMURRAGE, el estado (NotRequested o Pending). */
  references: string[];
  amounts: NoDebtAmount[];
}

/** GET /documents/{blNumber}/no-debt-certificate (M6-07). */
export interface NoDebtEligibility {
  blId: string;
  blNumber: string;
  country: 'CL' | 'BO';
  /** Bolivia e importación. */
  applicable: boolean;
  /** Sin deuda pendiente: se puede emitir. */
  eligible: boolean;
  canRequest: boolean;
  blockers: NoDebtBlocker[];
  evaluatedAt: string;
}

/** POST /documents/{blNumber}/transshipment-certificate: cargo del servicio listo para el carro (M6-01). */
export interface TransshipmentRequest {
  chargeId: string;
  blId: string;
  blNumber: string;
  conceptCode: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: 'Pending' | 'Paid';
  /** Solo cuando ya se pagó y se emitió el certificado. */
  documentId?: string | null;
}

/** Conceptos cuyo pago emite un documento del embarque (M6-01, M6-03). */
export const DOCUMENT_ISSUING_CONCEPTS: readonly string[] = ['TRANSSHIPMENT_CERT', 'GATE_OUT'];

// ---------------------------------------------------------------------------
// Fase 2, Ola J: certificado de flete (M6-02) y carta de liberación y desconsolidado (M6-08), Bolivia importación.
// ---------------------------------------------------------------------------

/** Finalidad del certificado de flete. */
export type FreightCertificatePurpose = 'CUSTOMS' | 'INSURANCE' | 'BANK' | 'OTHER';

/** Modo de cobro del certificado: en esta entrega solo `Free` (sin pago ni carro, decisión Q1 de la v4). */
export type FreightCertificatePaymentMode = 'Free' | 'Paid';

export interface FreightInfo {
  amount: number;
  currency?: string | null;
  terms?: string | null;
}

/** Consignatario del BL con que se precarga la solicitud. */
export interface DocumentConsignee {
  name?: string | null;
  taxId?: string | null;
}

/** GET /documents/{blNumber}/freight-certificate. */
export interface FreightCertificateContext {
  blId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  /** Bolivia e importación. */
  applicable: boolean;
  /** Aplica, la matriz de M1-11 lo permite y el perfil opera. */
  canRequest: boolean;
  paymentMode: FreightCertificatePaymentMode;
  requiresPayment: boolean;
  /** Flete que certifica el documento. */
  freight: FreightInfo;
  consignee: DocumentConsignee;
  purposes: FreightCertificatePurpose[];
  /** Solicitudes de la organización sobre el BL (más nuevas primero). */
  requests: ServiceRequestSummary[];
  /** Certificados del BL que el usuario puede ver. */
  documents: ShipmentDocument[];
}

/** POST /documents/{blNumber}/freight-certificate. */
export interface FreightCertificateRequest {
  consigneeName: string;
  consigneeTaxId: string;
  purpose: FreightCertificatePurpose;
  recipient: string | null;
  notes: string | null;
  sendEmail: boolean;
}

/** Solicitud completada con el certificado firmado y los correos a los que se envió. */
export interface FreightCertificateResult {
  request: ServiceRequestDetail;
  document: ShipmentDocument;
  sentTo: string[];
}

/** Tipo de sociedad del consignatario de la carta (definición provisoria hasta validarla con el área legal). */
export type LegalEntityType = 'COMPANY' | 'NATURAL_PERSON';

/** Origen del transportista registrado: acceso otorgado sobre el BL o pre-creado por la organización (M1-09). */
export type ReleaseLetterCarrierSource = 'Grant' | 'PreCreated';

/** TATC de una unidad seleccionada (M2-09). */
export interface ReleaseLetterTatcContainer {
  containerNumber: string;
  status: TatcStatus;
  sourceStatus?: string | null;
  tatcNumber?: string | null;
  pendingReasons: TatcPendingReason[];
}

/** Consulta del TATC de las unidades: `available` false = el sistema no respondió (`errorCode`, NF-11). */
export interface ReleaseLetterTatc {
  available: boolean;
  /** Agregado de las unidades seleccionadas. */
  status?: TatcStatus | null;
  errorCode?: string | null;
  checkedAt: string;
  containers: ReleaseLetterTatcContainer[];
}

export interface ReleaseLetterContainerOption {
  containerNumber: string;
  containerType: string;
  status: string;
  tatc?: ReleaseLetterTatcContainer | null;
  /** La unidad ya está en una carta pendiente de aprobación de la organización. */
  pendingRequestNumber?: string | null;
}

export interface ReleaseLetterCarrierOption {
  organizationId: string;
  name: string;
  taxId: string;
  source: ReleaseLetterCarrierSource;
}

/** GET /documents/{blNumber}/release-letter. */
export interface ReleaseLetterContext {
  blId: string;
  blNumber: string;
  bookingNumber?: string | null;
  country: 'CL' | 'BO';
  applicable: boolean;
  canRequest: boolean;
  /** La aprobación exige TATC emitido para cada unidad. */
  requiresIssuedTatc: boolean;
  legalEntityTypes: LegalEntityType[];
  consignee: DocumentConsignee;
  containers: ReleaseLetterContainerOption[];
  tatc: ReleaseLetterTatc;
  carriers: ReleaseLetterCarrierOption[];
  requests: ServiceRequestSummary[];
  documents: ShipmentDocument[];
}

/** POST /documents/{blNumber}/release-letter. Con `carrierOrganizationId`, los datos libres del transportista sobran. */
export interface ReleaseLetterRequestBody {
  containers: string[];
  legalEntityType: LegalEntityType;
  consigneeName: string;
  consigneeTaxId: string;
  consigneeAddress: string | null;
  legalRepresentativeName: string | null;
  legalRepresentativeId: string | null;
  carrierOrganizationId: string | null;
  carrierName: string | null;
  carrierTaxId: string | null;
  driverName: string | null;
  driverId: string | null;
  truckPlate: string | null;
  observations: string | null;
}

/** Registro de Counter del BL (M8-09); solo en la vista interna. */
export interface ReleaseLetterCounter {
  exchangeDate?: string | null;
  hblReceived: boolean;
  hblReceivedAt?: string | null;
  deconsolidated: boolean;
  deconsolidatedAt?: string | null;
}

/** Carta de liberación (M6-08): solicitud, TATC al enviar y al aprobar, carta emitida y, en la vista interna, TATC y Counter actuales. */
export interface ReleaseLetterRequest {
  request: ServiceRequestDetail;
  legalEntityType: LegalEntityType;
  carrierOrganizationId?: string | null;
  tatcAtSubmission: ReleaseLetterTatc;
  tatcAtApproval?: ReleaseLetterTatc | null;
  tatcNow?: ReleaseLetterTatc | null;
  counter?: ReleaseLetterCounter | null;
  requiresIssuedTatc: boolean;
  document?: ShipmentDocument | null;
}

/** Códigos de las definiciones con flujo propio de la Ola J (no se piden por el formulario genérico). */
export const FREIGHT_CERTIFICATE_DEFINITION_CODE = 'FREIGHT_CERTIFICATE';
export const RELEASE_LETTER_DEFINITION_CODE = 'RELEASE_LETTER';
