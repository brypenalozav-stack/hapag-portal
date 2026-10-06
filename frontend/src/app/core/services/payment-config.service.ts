import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PaymentMethod, PaymentStatusDetail } from '../models/cart.model';
import {
  ChargeSettlement,
  CreditImputationRule,
  CreditImputationRuleRequest,
  CreditImputationRuleSnapshot,
  SettlementMatchResult,
  SettlementSearch,
} from '../models/account-statement.model';
import { DepositProofQueueItem, DepositProofReviewResult } from '../models/deposit-proof.model';
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

  // Conceptos imputables a la línea de crédito (Fase 2, Ola H, M5-10)
  getCreditImputationRules(country: string, includeDisabled: boolean): Observable<CreditImputationRule[]> {
    return this.api.get<CreditImputationRule[]>(`${CONFIG}/credit-imputation`, { ...(country ? { country } : {}), includeDisabled });
  }

  createCreditImputationRule(body: CreditImputationRuleRequest): Observable<CreditImputationRule> {
    return this.api.post<CreditImputationRule>(`${CONFIG}/credit-imputation`, body);
  }

  updateCreditImputationRule(id: string, body: CreditImputationRuleRequest): Observable<CreditImputationRule> {
    return this.api.put<CreditImputationRule>(`${CONFIG}/credit-imputation/${id}`, {
      nexusCreditConcept: body.nexusCreditConcept,
      isEnabled: body.isEnabled,
      notes: body.notes,
    });
  }

  /** Baja lógica: el registro de cambios se conserva. */
  deleteCreditImputationRule(id: string): Observable<void> {
    return this.api.delete<void>(`${CONFIG}/credit-imputation/${id}`);
  }

  getCreditImputationRuleHistory(id: string): Observable<MaintainerChange<CreditImputationRuleSnapshot>[]> {
    return this.api.get<MaintainerChange<CreditImputationRuleSnapshot>[]>(`${CONFIG}/credit-imputation/${id}/history`);
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

  // Comprobantes de depósito por verificar (Fase 2, Ola H, M5-06)
  getDepositProofQueue(status: string, country: string): Observable<DepositProofQueueItem[]> {
    return this.api.get<DepositProofQueueItem[]>(`${FINANCE}/deposit-proofs`, {
      ...(status ? { status } : {}),
      ...(country ? { country } : {}),
    });
  }

  /** Verifica el abono: el pago se confirma por la vía de siempre (comprobante, liberación y aviso). */
  verifyDepositProof(proofId: string, notes: string | null): Observable<DepositProofReviewResult> {
    return this.api.post<DepositProofReviewResult>(`${FINANCE}/deposit-proofs/${proofId}/verify`, notes ? { notes } : {});
  }

  /** Rechaza con motivo: el pago sigue a la espera de un comprobante nuevo. */
  rejectDepositProof(proofId: string, reason: string): Observable<DepositProofReviewResult> {
    return this.api.post<DepositProofReviewResult>(`${FINANCE}/deposit-proofs/${proofId}/reject`, { reason });
  }

  // Anticipos e imputaciones a crédito con su cruce con las facturas (Fase 2, Ola H, M7-03, M3-19, NF-04)
  getSettlements(filters: SettlementSearch): Observable<ChargeSettlement[]> {
    const params: Record<string, string> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (typeof value === 'string' && value.trim()) params[key] = value.trim();
    }
    return this.api.get<ChargeSettlement[]>(`${FINANCE}/settlements`, params);
  }

  /** Cruce automático de los anticipos abiertos con las facturas que los incluyen. */
  matchSettlements(): Observable<SettlementMatchResult> {
    return this.api.post<SettlementMatchResult>(`${FINANCE}/settlements/match`, {});
  }

  /** Cruce manual con una factura (las diferencias de monto se aceptan con una nota). */
  matchSettlement(id: string, invoiceId: string, note: string | null): Observable<ChargeSettlement> {
    return this.api.post<ChargeSettlement>(`${FINANCE}/settlements/${id}/match`, { invoiceId, note });
  }

  getReconciliation(filters: ReconciliationSearch): Observable<PaymentReconciliation[]> {
    const params: Record<string, string> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value) params[key] = value as string;
    }
    return this.api.get<PaymentReconciliation[]>(`${FINANCE}/reconciliation`, params);
  }
}
