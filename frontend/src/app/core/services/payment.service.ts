import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { Payment } from '../models/payment.model';
import { PaymentBlockStatus, PaymentMethod, PaymentStatusDetail } from '../models/cart.model';

/**
 * Ciclo de vida de un pago del portal (Ola D): estado único con su historial (NF-02, NF-12), emisión de
 * la boleta de depósito y anulación con las reglas de M5-02, medios disponibles (M5-03) y estado público
 * del bloqueo de pagos (M8-07). El cobro se inicia desde el carro o desde la cuenta (CartService,
 * AccountPaymentService); el pago por BL anterior (POST /payments) se retiró.
 */
@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly api = inject(ApiService);

  /** Pagos en el formato anterior (resumen del dashboard). */
  getAll(): Observable<Payment[]> {
    return this.api.get<Payment[]>(`${API_ENDPOINTS.PAYMENTS}/my`);
  }

  getStatus(id: string): Observable<PaymentStatusDetail> {
    return this.api.get<PaymentStatusDetail>(`${API_ENDPOINTS.PAYMENTS}/${id}/status`);
  }

  /**
   * Vuelta desde la pasarela: pide al portal que consulte a la pasarela el estado del pago en curso y devuelve el
   * estado. La vuelta no confirma por sí sola: el portal solo confirma lo que informa la pasarela.
   */
  verify(id: string): Observable<PaymentStatusDetail> {
    return this.api.post<PaymentStatusDetail>(`${API_ENDPOINTS.PAYMENTS}/${id}/verify`, {});
  }

  /** Emite la boleta de depósito: desde ese momento el cliente ya no puede anularla (M5-02). */
  issueSlip(id: string): Observable<PaymentStatusDetail> {
    return this.api.post<PaymentStatusDetail>(`${API_ENDPOINTS.PAYMENTS}/${id}/issue-slip`, {});
  }

  /** Anulación por el cliente: solo un pago pendiente (boleta no emitida, pago en línea no enviado). */
  cancel(id: string): Observable<unknown> {
    return this.api.post<unknown>(`${API_ENDPOINTS.PAYMENTS}/${id}/cancel`, {});
  }

  availableMethods(country: string, currency: string): Observable<PaymentMethod[]> {
    return this.api.get<PaymentMethod[]>(`${API_ENDPOINTS.PAYMENT_CONFIG}/methods/available`, { country, currency });
  }

  /** Estado del bloqueo de pagos del país (sin país: el del usuario). */
  blockStatus(country?: string): Observable<PaymentBlockStatus> {
    return this.api.get<PaymentBlockStatus>(`${API_ENDPOINTS.PAYMENT_BLOCKS}/status`, country ? { country } : undefined);
  }
}
