/**
 * Refacturación IAO con pérdida de IVA (Fase 2, Ola H, M3-11): desde una factura chilena emitida, el cliente registra
 * los datos de la nueva razón social, adjunta su aprobación y paga juntos la refacturación y la pérdida de IVA (carro o
 * vista de crédito). La factura nueva se emite solo con la aceptación de la nueva razón social, que responde por un
 * enlace de un solo uso (sin sesión). Las propiedades en null no llegan en el JSON.
 */

import { ServiceBillingData, ServiceRequestDetail } from './service-request.model';

/** Clave del adjunto con la aprobación de la nueva razón social. */
export const REINVOICING_APPROVAL_FIELD = 'newCompanyApproval';

/** Definición de servicio de la refacturación (se pide solo por su flujo propio). */
export const REINVOICING_DEFINITION_CODE = 'IAO_REINVOICING';

export type ReinvoicingAcceptanceStatus = 'Pending' | 'Accepted' | 'Declined';

/** Cargo de la refacturación (`REINVOICING`, con IVA) o de la pérdida de IVA (`VAT_LOSS`, sin IVA). */
export interface ReinvoicingCharge {
  conceptCode: string;
  conceptName: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  tariffCode?: string | null;
}

/** GET /reinvoicing/quote. */
export interface ReinvoicingQuote {
  invoiceId: string;
  siiNumber?: string | null;
  sourceNumber: string;
  legalName: string;
  taxId: string;
  invoiceTotal: number;
  invoiceTaxAmount: number;
  invoiceCurrency: string;
  eligible: boolean;
  /** Código del error cuando no es elegible. */
  ineligibleReason?: string | null;
  fee?: ReinvoicingCharge | null;
  /** null en una factura exenta. */
  vatLoss?: ReinvoicingCharge | null;
  totalAmount: number;
  currency?: string | null;
  exchangeRate?: number | null;
  quotedAt: string;
  timeZone: string;
}

export interface InvoiceReissue {
  id: string;
  originalInvoiceId: string;
  originalSiiNumber?: string | null;
  originalSourceNumber: string;
  originalLegalName: string;
  originalTaxId: string;
  newLegalName?: string | null;
  newTaxId?: string | null;
  vatLossAmount: number;
  feeAmount: number;
  feeTaxAmount: number;
  currency?: string | null;
  exchangeRate?: number | null;
  acceptorEmail: string;
  /** null mientras es borrador. */
  acceptanceStatus?: ReinvoicingAcceptanceStatus | null;
  acceptanceRequestedAt?: string | null;
  acceptanceExpiresAt?: string | null;
  acceptedAt?: string | null;
  acceptedByName?: string | null;
  acceptedByTaxId?: string | null;
  declinedAt?: string | null;
  declineReason?: string | null;
  newInvoiceId?: string | null;
  newInvoiceNumber?: string | null;
  issuedAt?: string | null;
  approvalAttached: boolean;
  canResendAcceptance: boolean;
}

/** Solicitud genérica (estado, cargos, adjuntos, línea de tiempo) y datos de la refacturación. */
export interface ReinvoicingDetail {
  request: ServiceRequestDetail;
  reissue: InvoiceReissue;
}

export interface CreateReinvoicing {
  invoiceId: string;
  billing: ServiceBillingData;
  acceptorEmail: string | null;
  reason: string | null;
}

/** Lo que ve la nueva razón social en el enlace de aceptación. */
export interface ReinvoicingAcceptanceView {
  requestNumber: string;
  originalInvoiceNumber: string;
  originalLegalName: string;
  originalTaxId: string;
  originalTotal: number;
  originalCurrency: string;
  newLegalName?: string | null;
  newTaxId?: string | null;
  feeTotal: number;
  vatLossAmount: number;
  totalAmount: number;
  currency?: string | null;
  acceptanceStatus: ReinvoicingAcceptanceStatus;
  expiresAt?: string | null;
  acceptedAt?: string | null;
  declinedAt?: string | null;
  timeZone: string;
}

export interface ReinvoicingAcceptanceResponse {
  accept: boolean;
  name: string;
  taxId: string;
  reason: string | null;
}
