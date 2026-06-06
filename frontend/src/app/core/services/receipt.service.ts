import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { environment } from '../../../environments/environment';

export interface Receipt {
  id: string;
  receiptNumber: string;
  paymentId: string;
  paymentNumber: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  clientName: string;
  clientTaxId: string;
  country: 'CL' | 'BO';
  issuedAt: string;
}

@Injectable({ providedIn: 'root' })
export class ReceiptService {
  private readonly api = inject(ApiService);

  getMyReceipts(): Observable<Receipt[]> {
    return this.api.get<Receipt[]>(`${API_ENDPOINTS.RECEIPTS}/my`);
  }

  getById(id: string): Observable<Receipt> {
    return this.api.get<Receipt>(`${API_ENDPOINTS.RECEIPTS}/${id}`);
  }

  getPdfUrl(id: string): string {
    return `${environment.apiUrl}/${API_ENDPOINTS.RECEIPTS}/${id}/pdf`;
  }
}
