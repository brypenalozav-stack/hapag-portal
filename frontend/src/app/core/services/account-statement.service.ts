import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  AccountCheckoutResult,
  AccountStatement,
  AccountStatementCheckoutRequest,
  StatementExportFormat,
  StatementFilters,
} from '../models/account-statement.model';

const BASE = API_ENDPOINTS.ACCOUNT_STATEMENT;

function params(filters: StatementFilters): Record<string, string> {
  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(filters)) {
    if (typeof value === 'string' && value.trim() !== '') result[key] = value.trim();
  }
  return result;
}

/**
 * Estado de cuenta en línea (Fase 2, Ola H, M7-03), segregado por organización como las facturas (M7-01): saldos,
 * antigüedad, crédito disponible, líneas y anticipos; exportación a planilla y cierre de los clientes con crédito con
 * forma de pago por ítem (M5-10). Los clientes sin crédito pagan lo elegido en el carro (CartService.addItems).
 */
@Injectable({ providedIn: 'root' })
export class AccountStatementService {
  private readonly api = inject(ApiService);

  get(filters: StatementFilters): Observable<AccountStatement> {
    return this.api.get<AccountStatement>(BASE, params(filters));
  }

  /** Planilla con los mismos filtros: xlsx (líneas, resumen y anticipos) o csv (solo las líneas). */
  export(filters: StatementFilters, format: StatementExportFormat, language: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/export`, { ...params(filters), format, language });
  }

  /** Cada ítem se paga ahora o se imputa a crédito; la clave se reutiliza en los reintentos (NF-01). */
  checkout(request: AccountStatementCheckoutRequest, idempotencyKey: string): Observable<AccountCheckoutResult> {
    return this.api.post<AccountCheckoutResult>(`${BASE}/checkout`, request, { 'Idempotency-Key': idempotencyKey });
  }
}
