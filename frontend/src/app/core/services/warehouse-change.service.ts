import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import {
  WarehouseChangeBatch,
  WarehouseChangeDetail,
  WarehouseChangeHistoryFilters,
  WarehouseChangeHistoryItem,
  WarehouseChangeQuote,
  WarehouseChangeRequest,
  WarehouseChangeTrace,
} from '../models/warehouse-change.model';

const BASE = API_ENDPOINTS.WAREHOUSE_CHANGES;

/**
 * Cambio de almacén gratuito o tarifado (M3-04, M8-01), solicitudes masivas (M3-05, NF-19) y, en la Ola G, el
 * historial con la trazabilidad de cada solicitud: quién la pidió y quién la pagó (M3-06).
 */
@Injectable({ providedIn: 'root' })
export class WarehouseChangeService {
  private readonly api = inject(ApiService);

  getQuote(blNumber: string, containerNumber?: string): Observable<WarehouseChangeQuote> {
    return this.api.get<WarehouseChangeQuote>(
      `${BASE}/quote/${encodeURIComponent(blNumber)}`,
      containerNumber ? { containerNumber } : undefined,
    );
  }

  request(body: WarehouseChangeRequest): Observable<WarehouseChangeDetail> {
    return this.api.post<WarehouseChangeDetail>(`${BASE}/requests`, body);
  }

  submitBulk(items: WarehouseChangeRequest[]): Observable<WarehouseChangeBatch> {
    return this.api.post<WarehouseChangeBatch>(`${BASE}/bulk`, { items });
  }

  getBulk(id: string): Observable<WarehouseChangeBatch> {
    return this.api.get<WarehouseChangeBatch>(`${BASE}/bulk/${encodeURIComponent(id)}`);
  }

  /** Historial de la organización (el administrador interno ve todo); `from`/`to` en el huso del país. */
  getHistory(filters: WarehouseChangeHistoryFilters): Observable<PagedResult<WarehouseChangeHistoryItem>> {
    return this.api.get<PagedResult<WarehouseChangeHistoryItem>>(`${BASE}/history`, {
      ...(filters.blNumber ? { blNumber: filters.blNumber.trim() } : {}),
      ...(filters.status ? { status: filters.status } : {}),
      ...(filters.from ? { from: filters.from } : {}),
      ...(filters.to ? { to: filters.to } : {}),
      ...(filters.organizationId ? { organizationId: filters.organizationId } : {}),
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
    });
  }

  getHistoryItem(id: string): Observable<WarehouseChangeTrace> {
    return this.api.get<WarehouseChangeTrace>(`${BASE}/history/${encodeURIComponent(id)}`);
  }
}
