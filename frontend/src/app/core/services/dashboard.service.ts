import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { Dashboard, DashboardFilters } from '../models/dashboard.model';

/**
 * Dashboard consolidado del cliente (M1-05): una sola consulta con gestiones, pendientes de pago, documentos
 * e indicadores, filtrada por operación (M2-07) y país (M1-04). El servidor aplica los accesos (M1-11, NF-05).
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiService);

  get(filters: DashboardFilters = {}): Observable<Dashboard> {
    return this.api.get<Dashboard>(API_ENDPOINTS.DASHBOARD, {
      ...(filters.operation ? { operation: filters.operation } : {}),
      ...(filters.country ? { country: filters.country } : {}),
    });
  }
}
