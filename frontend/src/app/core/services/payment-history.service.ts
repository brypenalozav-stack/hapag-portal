import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import { PaymentHistoryItem, PaymentHistorySearch } from '../models/payment.model';

const BASE = API_ENDPOINTS.PAYMENT_HISTORY;

/** Historial de pagos del portal (M7-02) con su comprobante o boleta. */
@Injectable({ providedIn: 'root' })
export class PaymentHistoryService {
  private readonly api = inject(ApiService);

  search(filters: PaymentHistorySearch): Observable<PagedResult<PaymentHistoryItem>> {
    const params: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') params[key] = value as string | number;
    }
    return this.api.get<PagedResult<PaymentHistoryItem>>(BASE, params);
  }

  getById(id: string): Observable<PaymentHistoryItem> {
    return this.api.get<PaymentHistoryItem>(`${BASE}/${id}`);
  }

  downloadReceipt(id: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/${id}/receipt`);
  }
}
