/**
 * Facturas del cliente (M7-01): segregadas por organización, con número SII y del sistema de origen
 * diferenciados, descarga en PDF y múltiple (zip) y pago desde el carro.
 */

import { InvoiceCoverage } from './account-statement.model';

/** `Superseded`: reemplazada por la factura de una refacturación IAO (Fase 2, Ola H, M3-11). */
export type InvoiceStatus = 'Pending' | 'Overdue' | 'Paid' | 'Cancelled' | 'Superseded';
export type InvoiceDocumentType = 'Invoice' | 'ExemptInvoice' | 'CreditNote' | 'DebitNote';

export const INVOICE_STATUSES: readonly InvoiceStatus[] = ['Pending', 'Overdue', 'Paid', 'Cancelled', 'Superseded'];
export const INVOICE_DOCUMENT_TYPES: readonly InvoiceDocumentType[] = ['Invoice', 'ExemptInvoice', 'CreditNote', 'DebitNote'];

/** Máximo de facturas por descarga múltiple (contrato del backend). */
export const INVOICE_DOWNLOAD_MAX = 50;

/** Organización cuyas facturas puede consultar el usuario (la propia primero). */
export interface InvoiceOrganization {
  id: string;
  name: string;
  taxId: string;
  isOwn: boolean;
}

export interface Invoice {
  id: string;
  /** Folio SII; null = sin folio emitido (no se puede descargar). */
  siiNumber?: string | null;
  sourceNumber: string;
  documentType: InvoiceDocumentType;
  issueDate: string;
  dueDate?: string | null;
  blId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  legalName: string;
  taxId: string;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: InvoiceStatus;
  siiStatus?: string | null;
  canDownload: boolean;
  isPayable: boolean;
  inCart: boolean;
  syncedAt?: string | null;
  /** Fase 2, Ola H: factura que la reemplaza (refacturación IAO, M3-11). */
  supersededByInvoiceId?: string | null;
  /** Fase 2, Ola H: factura a la que reemplaza (refacturación IAO, M3-11). */
  supersedesInvoiceId?: string | null;
  /** Fase 2, Ola H: pago anticipado que la cubre (M3-19, M7-03); la factura queda pagada. */
  coveredBy?: InvoiceCoverage | null;
}

export interface InvoiceSearch {
  organizationId?: string;
  blNumber?: string;
  bookingNumber?: string;
  from?: string;
  to?: string;
  status?: string;
  currency?: string;
  documentType?: string;
  page: number;
  pageSize: number;
}

/** GET /invoices. */
export interface InvoiceList {
  organization: InvoiceOrganization;
  items: Invoice[];
  total: number;
  page: number;
  pageSize: number;
  lastUpdatedAt?: string | null;
  timeZone: string;
}

/** POST /invoices/refresh. */
export interface InvoiceRefreshResult {
  organizationId: string;
  checked: number;
  updated: number;
  lastUpdatedAt: string;
}
