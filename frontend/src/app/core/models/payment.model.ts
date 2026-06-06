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
  blId: string;
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

export interface CreatePaymentRequest {
  blId: string;
  type: string;
  method: string;
  chargeIds?: string[];
  country: 'CL' | 'BO';
}
