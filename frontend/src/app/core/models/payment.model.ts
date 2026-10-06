import { PaymentItem, PaymentOrigin, PaymentStatus } from './cart.model';

/**
 * Pago en el formato anterior (GET /payments/my), que aún usa el resumen del dashboard. El historial de
 * la Ola D (M7-02) usa `PaymentHistoryItem`.
 */
export interface Payment {
  id: string;
  paymentNumber: string;
  type: string;
  method: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: string;
  blNumber: string;
  /** Null en pagos de carro con varios BL. */
  blId: string | null;
  clientId: string;
  clientName: string;
  country: 'CL' | 'BO';
  receiptUrl?: string;
  createdAt: string;
  confirmedAt?: string;
  details: PaymentDetail[];
}

export interface PaymentDetail {
  id: string;
  chargeCode: string;
  description: string;
  amount: number;
  currency: string;
}

/** Organización mandante de un pago hecho bajo mandato (NF-14). */
export interface PaymentOnBehalfOf {
  id: string;
  name: string;
  taxId: string;
}

/**
 * Pago del portal en el historial (M7-02): solo pagos de la organización como pagadora o como mandante,
 * con el RUT del pagador diferenciado de los RUT de facturación.
 */
export interface PaymentHistoryItem {
  id: string;
  paymentNumber: string;
  /** Comprobante o, si aún no existe, boleta de depósito. */
  documentNumber?: string | null;
  receiptNumber?: string | null;
  slipNumber?: string | null;
  paymentDate: string;
  status: PaymentStatus;
  origin: PaymentOrigin;
  /** Código del medio de pago. */
  method?: string | null;
  methodName?: string | null;
  country: string;
  currency: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  payerTaxId: string;
  payerName: string;
  billingTaxIds: string[];
  payerDiffersFromBilling: boolean;
  blNumbers: string[];
  bookingNumbers: string[];
  onBehalfOf?: PaymentOnBehalfOf | null;
  /** Correo del usuario que ejecutó el pago. */
  executedBy?: string | null;
  receiptAvailable: boolean;
  items: PaymentItem[];
}

export interface PaymentHistorySearch {
  from?: string;
  to?: string;
  status?: string;
  blNumber?: string;
  page: number;
  pageSize: number;
}
