import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PaymentMethod, PaymentStatusDetail } from '../models/cart.model';
import {
  MaintainerChange,
  PaymentBlockWindow,
  PaymentBlockWindowRequest,
  PaymentBlockWindowSnapshot,
  PaymentCurrencyConfig,
  PaymentCurrencySnapshot,
  PaymentMethodRequest,
  PaymentMethodSnapshot,
  PaymentOperation,
  PaymentReconciliation,
  ReconciliationSearch,
} from '../models/payment-config.model';

const CONFIG = API_ENDPOINTS.PAYMENT_CONFIG;
const BLOCKS = API_ENDPOINTS.PAYMENT_BLOCKS;
const FINANCE = API_ENDPOINTS.ADMIN_PAYMENTS;

/**
 * Configuración interna de pagos: monedas por concepto y país (M5-04) y medios de pago (M5-03) con el
 * permiso maintainers.manage; ventanas de bloqueo (M8-07) con payment-blocks.manage; herramientas de
 * Finanzas (M5-02, NF-03, NF-04) con payments.finance. Cada cambio queda en su registro (NF-15).
 */
@Injectable({ providedIn: 'root' })
export class PaymentConfigService {
  private readonly api = inject(ApiService);

  // Monedas por concepto y país (M5-04)
  getCurrencies(country: string): Observable<PaymentCurrencyConfig[]> {
    return this.api.get<PaymentCurrencyConfig[]>(`${CONFIG}/currencies`, { country });
  }

  setCurrencies(country: string, conceptCode: string, currencies: string[]): Observable<PaymentCurrencyConfig> {
    return this.api.put<PaymentCurrencyConfig>(`${CONFIG}/currencies/${country}/${encodeURIComponent(conceptCode)}`, { currencies });
  }

  /** Vuelve a la regla por defecto (moneda del cargo y moneda local). */
  resetCurrencies(country: string, conceptCode: string): Observable<void> {
    return this.api.delete<void>(`${CONFIG}/currencies/${country}/${encodeURIComponent(conceptCode)}`);
  }

  getCurrencyHistory(country: string, conceptCode: string): Observable<MaintainerChange<PaymentCurrencySnapshot>[]> {
    return this.api.get<MaintainerChange<PaymentCurrencySnapshot>[]>(
      `${CONFIG}/currencies/${country}/${encodeURIComponent(conceptCode)}/history`,
    );
  }

  // Medios de pago (M5-03)
  getMethods(country: string, includeDisabled: boolean): Observable<PaymentMethod[]> {
    return this.api.get<PaymentMethod[]>(`${CONFIG}/methods`, { country, includeDisabled });
  }

  createMethod(body: PaymentMethodRequest): Observable<PaymentMethod> {
    return this.api.post<PaymentMethod>(`${CONFIG}/methods`, body);
  }

  updateMethod(id: string, body: PaymentMethodRequest): Observable<PaymentMethod> {
    return this.api.put<PaymentMethod>(`${CONFIG}/methods/${id}`, body);
  }

  disableMethod(id: string): Observable<void> {
    return this.api.delete<void>(`${CONFIG}/methods/${id}`);
  }

  getMethodHistory(id: string): Observable<MaintainerChange<PaymentMethodSnapshot>[]> {
    return this.api.get<MaintainerChange<PaymentMethodSnapshot>[]>(`${CONFIG}/methods/${id}/history`);
  }

  // Ventanas de bloqueo (M8-07)
  getBlockWindows(country: string, includeCancelled: boolean): Observable<PaymentBlockWindow[]> {
    return this.api.get<PaymentBlockWindow[]>(BLOCKS, { country, includeCancelled });
  }

  createBlockWindow(body: PaymentBlockWindowRequest): Observable<PaymentBlockWindow> {
    return this.api.post<PaymentBlockWindow>(BLOCKS, body);
  }

  updateBlockWindow(id: string, body: PaymentBlockWindowRequest): Observable<PaymentBlockWindow> {
    return this.api.put<PaymentBlockWindow>(`${BLOCKS}/${id}`, body);
  }

  /** Cancela una ventana programada o termina antes una activa. */
  cancelBlockWindow(id: string): Observable<void> {
    return this.api.delete<void>(`${BLOCKS}/${id}`);
  }

  getBlockWindowHistory(id: string): Observable<MaintainerChange<PaymentBlockWindowSnapshot>[]> {
    return this.api.get<MaintainerChange<PaymentBlockWindowSnapshot>[]>(`${BLOCKS}/${id}/history`);
  }

  // Finanzas (NF-03, M5-02, NF-04)
  getOperations(status?: string): Observable<PaymentOperation[]> {
    return this.api.get<PaymentOperation[]>(`${FINANCE}/operations`, status ? { status } : undefined);
  }

  retryOperation(id: string): Observable<PaymentOperation> {
    return this.api.post<PaymentOperation>(`${FINANCE}/operations/${id}/retry`, {});
  }

  /** Anulación por Finanzas (boleta emitida o pago en curso) con motivo obligatorio. */
  cancelPayment(paymentId: string, reason: string): Observable<PaymentStatusDetail> {
    return this.api.post<PaymentStatusDetail>(`${FINANCE}/${paymentId}/cancel`, { reason });
  }

  getReconciliation(filters: ReconciliationSearch): Observable<PaymentReconciliation[]> {
    const params: Record<string, string> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value) params[key] = value as string;
    }
    return this.api.get<PaymentReconciliation[]>(`${FINANCE}/reconciliation`, params);
  }
}
