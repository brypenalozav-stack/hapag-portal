export interface ImportBillRow {
  clientId: string;
  blNumber: string;
  shipmentType: string;
  country: string;
  portOfLoading?: string;
  portOfDischarge?: string;
  freightCurrency: string;
  consigneeName?: string;
  consigneeTaxId?: string;
  hsCode?: string;
  grossWeight?: number;
  containerNumber?: string;
  containerIsoType?: string;
}

export interface ImportRowError {
  index: number;
  blNumber?: string;
  messages: string[];
}

export interface ImportResult {
  created: number;
  failed: number;
  errors: ImportRowError[];
}

export interface ClientOption {
  id: string;
  name: string;
  taxId: string;
  country: string;
}
