import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Payment, CreatePaymentRequest } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly api = inject(ApiService);

  create(data: CreatePaymentRequest): Observable<Payment> {
    return this.api.post<Payment>('payments', data);
  }

  getById(id: string): Observable<Payment> {
    return this.api.get<Payment>(`payments/${id}`);
  }

  getAll(): Observable<Payment[]> {
    return this.api.get<Payment[]>('payments');
  }
}
