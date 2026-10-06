import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import {
  AdminOverview,
  ImpersonationRequestEntry,
  ImpersonationSession,
  ImpersonationSessionSearch,
  ImpersonationStarted,
  ImpersonationTarget,
} from '../models/administration.model';

const IMP = API_ENDPOINTS.ADMIN_IMPERSONATION;

/**
 * Área de administración (Fase 2, Ola I, M8-05): resumen con los contadores de cada sección, y la vista como cliente
 * (M8-08): usuarios elegibles, inicio y término de sesiones y su registro de solicitudes.
 */
@Injectable({ providedIn: 'root' })
export class AdministrationService {
  private readonly api = inject(ApiService);

  getOverview(): Observable<AdminOverview> {
    return this.api.get<AdminOverview>(API_ENDPOINTS.ADMIN_OVERVIEW);
  }

  getTargets(organizationId: string): Observable<ImpersonationTarget[]> {
    return this.api.get<ImpersonationTarget[]>(`${IMP}/targets`, { organizationId });
  }

  startSession(body: { organizationId: string; userId: string; reason: string }): Observable<ImpersonationStarted> {
    return this.api.post<ImpersonationStarted>(`${IMP}/sessions`, body);
  }

  getSessions(filters: ImpersonationSessionSearch): Observable<PagedResult<ImpersonationSession>> {
    const query: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = value as string | number;
    }
    return this.api.get<PagedResult<ImpersonationSession>>(`${IMP}/sessions`, query);
  }

  getSessionRequests(id: string, page = 1, pageSize = 50): Observable<PagedResult<ImpersonationRequestEntry>> {
    return this.api.get<PagedResult<ImpersonationRequestEntry>>(`${IMP}/sessions/${id}/requests`, { page, pageSize });
  }

  endSession(id: string): Observable<ImpersonationSession> {
    return this.api.post<ImpersonationSession>(`${IMP}/sessions/${id}/end`, {});
  }

  /** Con el token de la vista como cliente: la sesión en curso (banner). */
  getCurrentImpersonation(): Observable<ImpersonationSession> {
    return this.api.get<ImpersonationSession>(`${API_ENDPOINTS.IMPERSONATION}/current`);
  }

  /** Con el token de la vista como cliente: termina la sesión. */
  endCurrentImpersonation(): Observable<ImpersonationSession> {
    return this.api.post<ImpersonationSession>(`${API_ENDPOINTS.IMPERSONATION}/end`, {});
  }
}
