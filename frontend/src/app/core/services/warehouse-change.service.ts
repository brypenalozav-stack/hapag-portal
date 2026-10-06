import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  WarehouseChangeBatch,
  WarehouseChangeDetail,
  WarehouseChangeQuote,
  WarehouseChangeRequest,
} from '../models/warehouse-change.model';

const BASE = API_ENDPOINTS.WAREHOUSE_CHANGES;

/** Cambio de almacén gratuito o tarifado (M3-04, M8-01) y solicitudes masivas (M3-05, NF-19). */
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
}
