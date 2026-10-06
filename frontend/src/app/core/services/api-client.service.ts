import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import {
  API_CLIENT_LIMITS,
  ApiClient,
  ApiClientFilters,
  ApiClientRequestLog,
  ApiClientSecret,
  CreateApiClientRequest,
} from '../models/api-client.model';

const BASE = API_ENDPOINTS.ADMIN_API_CLIENTS;

/**
 * Administración del canal Web Service (Fase 2, Ola J, M3-17; permiso `api-clients.manage`): clientes por organización,
 * alta con su primera clave, rotación con período de gracia, revocación de una clave o del cliente y bitácora de
 * solicitudes. Cada cambio queda en la auditoría (sin la clave).
 */
@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly api = inject(ApiService);

  list(filters: ApiClientFilters = {}): Observable<ApiClient[]> {
    return this.api.get<ApiClient[]>(BASE, {
      ...(filters.organizationId ? { organizationId: filters.organizationId } : {}),
      ...(filters.status ? { status: filters.status } : {}),
    });
  }

  get(id: string): Observable<ApiClient> {
    return this.api.get<ApiClient>(`${BASE}/${id}`);
  }

  /** Crea el cliente, su usuario técnico y su primera clave (se muestra una sola vez). */
  create(body: CreateApiClientRequest): Observable<ApiClientSecret> {
    return this.api.post<ApiClientSecret>(BASE, body);
  }

  /** Emite una clave nueva; las vigentes siguen sirviendo `graceMinutes` (0 = se revocan ahora). */
  rotate(id: string, graceMinutes: number): Observable<ApiClientSecret> {
    return this.api.post<ApiClientSecret>(`${BASE}/${id}/keys/rotate`, { graceMinutes });
  }

  revokeKey(id: string, keyId: string): Observable<ApiClient> {
    return this.api.post<ApiClient>(`${BASE}/${id}/keys/${keyId}/revoke`, {});
  }

  /** Revoca el cliente: todas sus claves dejan de servir y su usuario técnico se desactiva. */
  revoke(id: string, reason: string): Observable<ApiClient> {
    return this.api.post<ApiClient>(`${BASE}/${id}/revoke`, { reason });
  }

  requests(id: string, page: number, pageSize: number = API_CLIENT_LIMITS.LOG_PAGE_SIZE): Observable<PagedResult<ApiClientRequestLog>> {
    return this.api.get<PagedResult<ApiClientRequestLog>>(`${BASE}/${id}/requests`, { page, pageSize });
  }
}
