import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { ExceptionReport, ReportFilters, TransactionReport } from '../models/transaction-report.model';

const BASE = API_ENDPOINTS.ADMIN_REPORTS;

function query(filters: ReportFilters): Record<string, string | number> {
  const q: Record<string, string | number> = {};
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== null && value !== '') q[key] = value as string | number;
  }
  return q;
}

/**
 * Reportería general (Fase 2, Ola I, M9-01; permiso `transactions-report.view`): transacciones por servicio y
 * excepciones aplicadas (exenciones de Nexus, cambio de almacén gratuito, imputaciones a crédito e IPO), con su
 * exportación a Excel o CSV en el idioma de la interfaz.
 */
@Injectable({ providedIn: 'root' })
export class TransactionReportService {
  private readonly api = inject(ApiService);

  getTransactions(filters: ReportFilters): Observable<TransactionReport> {
    return this.api.get<TransactionReport>(`${BASE}/transactions`, query(filters));
  }

  getExceptions(filters: ReportFilters): Observable<ExceptionReport> {
    return this.api.get<ExceptionReport>(`${BASE}/exceptions`, query(filters));
  }

  export(kind: 'transactions' | 'exceptions', filters: ReportFilters, format: 'xlsx' | 'csv', language: string): Observable<Blob> {
    const rest: ReportFilters = { ...filters };
    delete rest.page;
    delete rest.pageSize;
    return this.api.getBlob(`${BASE}/${kind}/export`, { ...query(rest), format, language });
  }
}
