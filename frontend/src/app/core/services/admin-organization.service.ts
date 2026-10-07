import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/admin-user.model';
import {
  AdminOrganizationDetail,
  AdminOrganizationItem,
  AdminOrganizationSearch,
  ArCheckRequest,
} from '../models/organization.model';

const BASE = API_ENDPOINTS.ADMIN_ORGANIZATIONS;

/** Flujo interno de creación de clientes y asignación de Match Code (M8-04). */
@Injectable({ providedIn: 'root' })
export class AdminOrganizationService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);

  search(filters: AdminOrganizationSearch): Observable<PagedResult<AdminOrganizationItem>> {
    const query: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = value as string | number;
    }
    return this.api.get<PagedResult<AdminOrganizationItem>>(BASE, query);
  }

  getById(id: string): Observable<AdminOrganizationDetail> {
    return this.api.get<AdminOrganizationDetail>(`${BASE}/${id}`);
  }

  downloadDocument(id: string, documentId: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/${BASE}/${id}/documents/${documentId}`, { responseType: 'blob' });
  }

  validate(id: string, notes?: string): Observable<void> {
    return this.api.post<void>(`${BASE}/${id}/validate`, notes ? { notes } : {});
  }

  completeArCheck(id: string, data: ArCheckRequest): Observable<void> {
    return this.api.post<void>(`${BASE}/${id}/ar-check`, data);
  }

  reject(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`${BASE}/${id}/reject`, { reason });
  }
}
