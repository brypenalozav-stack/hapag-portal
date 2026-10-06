/**
 * Comprobante de depósito (Fase 2, Ola H, M5-06): el cliente adjunta el comprobante del abono a un pago con boleta;
 * Finanzas lo verifica (el pago se confirma) o lo rechaza con motivo (el pago sigue esperando un comprobante nuevo).
 * Cada carga y revisión queda registrada. Las propiedades en null no llegan en el JSON.
 */

import { PaymentStatusDetail } from './cart.model';

export type DepositProofStatus = 'Submitted' | 'Verified' | 'Rejected';
export const DEPOSIT_PROOF_STATUSES: readonly DepositProofStatus[] = ['Submitted', 'Verified', 'Rejected'];

/** Límites del archivo (contrato del backend). */
export const DEPOSIT_PROOF_LIMITS = {
  MAX_FILE_BYTES: 10 * 1024 * 1024,
  FILE_TYPES: ['application/pdf', 'image/png', 'image/jpeg'] as readonly string[],
  FILE_ACCEPT: '.pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg',
  MAX_TEXT: 200,
  MAX_NOTES: 1000,
} as const;

export interface DepositProof {
  id: string;
  paymentId: string;
  status: DepositProofStatus;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  /** SHA-256 del archivo. */
  contentHash?: string | null;
  bankName?: string | null;
  bankReference?: string | null;
  depositDate?: string | null;
  depositAmount?: number | null;
  notes?: string | null;
  uploadedAt: string;
  uploadedBy: string;
  reviewedAt?: string | null;
  reviewedBy?: string | null;
  reviewNotes?: string | null;
  rejectionReason?: string | null;
  downloadPath: string;
}

/** GET y POST /payments/{id}/deposit-proofs. */
export interface PaymentDepositProofs {
  payment: PaymentStatusDetail;
  proofs: DepositProof[];
  /** La boleta espera un comprobante (ninguno enviado o el último rechazado). */
  awaitingProof: boolean;
  canUpload: boolean;
}

/** Datos opcionales del depósito que acompañan al archivo. */
export interface DepositProofUpload {
  file: File;
  bankName?: string;
  bankReference?: string;
  depositDate?: string;
  depositAmount?: string;
  notes?: string;
}

/** Elemento de la bandeja de Finanzas. */
export interface DepositProofQueueItem {
  proof: DepositProof;
  paymentNumber: string;
  slipNumber?: string | null;
  paymentStatus: string;
  country: string;
  currency: string;
  totalAmount: number;
  payerTaxId?: string | null;
  payerName?: string | null;
  blNumbers: string[];
  paymentCreatedAt: string;
  timeZone: string;
}

/** POST /admin/payments/deposit-proofs/{id}/verify y reject. */
export interface DepositProofReviewResult {
  proof: DepositProof;
  payment: PaymentStatusDetail;
}
