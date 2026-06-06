export interface BillOfLading {
  id: string;
  blNumber: string;
  type: string;
  vessel: string;
  voyage: string;
  portOfLoading: string;
  portOfDischarge: string;
  placeOfDelivery: string;
  etd: string;
  eta: string;
  status: string;
  freightAmount: number;
  freightCurrency: string;
  freightStatus: string;
  country: 'CL' | 'BO';
  clientId: string;
  clientName: string;
  containers: BLContainer[];
  createdAt: string;
}

export interface BLContainer {
  id: string;
  containerNumber: string;
  type: string;
  size: string;
  sealNumber: string;
  weight: number;
  packages: number;
  description: string;
}

export interface LocalCharge {
  id: string;
  blId: string;
  blNumber: string;
  chargeCode: string;
  description: string;
  amount: number;
  currency: string;
  taxAmount: number;
  totalAmount: number;
  status: string;
  country: 'CL' | 'BO';
}

export interface BLChargesResponse {
  blNumber: string;
  localCharges: LocalCharge[];
  demurrageCharges: DemurrageCharge[];
}

export interface DemurrageCharge {
  id: string;
  blId: string;
  blNumber: string;
  containerNumber: string;
  freeDays: number;
  demurrageDays: number;
  dailyRate: number;
  currency: string;
  totalAmount: number;
  status: string;
  isExempt: boolean;
  exemptionReason?: string;
  country: 'CL' | 'BO';
}
