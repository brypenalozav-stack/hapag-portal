import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import { MaintainerChange } from '../models/payment-config.model';
import {
  CounterDetail,
  CounterRecord,
  CounterRecordRequest,
  CounterSearch,
  CounterSnapshot,
} from '../models/counter.model';

const BASE = API_ENDPOINTS.ADMIN_COUNTER;

/**
 * Counter Bolivia/Ultramar (Fase 2, Ola I, M8-09; permiso `counter.manage`): registro por BL del canje, la recepción
 * del HBL y el desconsolidado, su propagación a Nexus con reintento y el registro de cambios (NF-15).
 */
@Injectable({ providedIn: 'root' })
export class CounterService {
  private readonly api = inject(ApiService);

  search(filters: CounterSearch): Observable<PagedResult<CounterRecord>> {
    const query: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = value as string | number;
    }
    return this.api.get<PagedResult<CounterRecord>>(BASE, query);
  }

  getByBl(blNumber: string): Observable<CounterDetail> {
    return this.api.get<CounterDetail>(`${BASE}/${encodeURIComponent(blNumber)}`);
  }

  save(blNumber: string, body: CounterRecordRequest): Observable<CounterRecord> {
    return this.api.put<CounterRecord>(`${BASE}/${encodeURIComponent(blNumber)}`, body);
  }

  retrySync(blNumber: string): Observable<CounterRecord> {
    return this.api.post<CounterRecord>(`${BASE}/${encodeURIComponent(blNumber)}/sync`, {});
  }

  getHistory(blNumber: string): Observable<MaintainerChange<CounterSnapshot>[]> {
    return this.api.get<MaintainerChange<CounterSnapshot>[]>(`${BASE}/${encodeURIComponent(blNumber)}/history`);
  }
}
