import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { AccountCheckoutRequest, AccountPayables, CheckoutResult } from '../models/cart.model';

/** Pago desde la cuenta para clientes con condición de crédito (M5-07), separado del carro. */
@Injectable({ providedIn: 'root' })
export class AccountPaymentService {
  private readonly api = inject(ApiService);

  get(): Observable<AccountPayables> {
    return this.api.get<AccountPayables>(API_ENDPOINTS.ACCOUNT_PAYMENTS);
  }

  /** Un solo pago con los ítems elegidos; la clave se reutiliza en los reintentos (NF-01). */
  checkout(request: AccountCheckoutRequest, idempotencyKey: string): Observable<CheckoutResult> {
    return this.api.post<CheckoutResult>(`${API_ENDPOINTS.ACCOUNT_PAYMENTS}/checkout`, request, { 'Idempotency-Key': idempotencyKey });
  }
}
