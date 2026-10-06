import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import { OrganizationType } from '../models/organization.model';
import {
  AccessAuditEntry,
  AccessAuditSearch,
  AccessGrant,
  AccessGrantSearch,
  CreateDefaultGranteeRequest,
  DefaultGrantee,
  EarlyBookingAccessRequest,
  GrantAccessRequest,
  GrantAccessResult,
  GrantableAction,
  GrantableActionsQuery,
  GranteeOrganization,
  MandateTerms,
  OpenAccessSetting,
  RevokeAccessResult,
  UpdateAccessGrantRequest,
  UpdateDefaultGranteeRequest,
  UpdateOpenAccessRequest,
  VisibilityWidening,
  WideningTargetRole,
} from '../models/access.model';

const ACCESS = API_ENDPOINTS.ACCESS;

/** Quita los filtros vacíos antes de armar la consulta. */
function query(filters: object): Record<string, string | number | boolean> {
  const result: Record<string, string | number | boolean> = {};
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== null && value !== '') result[key] = value as string | number | boolean;
  }
  return result;
}

/**
 * Vista única de accesos y permisos (M1-24) y las fichas que reúne: otorgamiento individual y
 * masivo (M1-12), por defecto (M1-13), vigencia (M1-14), permisos (M1-15), ampliaciones (M1-16),
 * acceso abierto (M1-17), acceso por booking (M1-20), revocación en cadena (M1-22), auditoría
 * (M1-23) y mandatos (M1-03). El servidor aplica las reglas de M1-11 en cada operación.
 */
@Injectable({ providedIn: 'root' })
export class AccessService {
  private readonly api = inject(ApiService);

  // Accesos otorgados y recibidos
  getGrants(filters: AccessGrantSearch): Observable<PagedResult<AccessGrant>> {
    return this.api.get<PagedResult<AccessGrant>>(`${ACCESS}/grants`, query(filters));
  }

  /** Una referencia usa el otorgamiento individual; varias, el masivo (mismo contrato). */
  grant(request: GrantAccessRequest): Observable<GrantAccessResult> {
    const references = (request.blNumbers?.length ?? 0) + (request.bookingNumbers?.length ?? 0);
    return this.api.post<GrantAccessResult>(references > 1 ? `${ACCESS}/grants/bulk` : `${ACCESS}/grants`, request);
  }

  updateGrant(id: string, request: UpdateAccessGrantRequest): Observable<AccessGrant> {
    return this.api.put<AccessGrant>(`${ACCESS}/grants/${id}`, request);
  }

  revokeGrant(id: string, reason?: string): Observable<RevokeAccessResult> {
    return this.api.post<RevokeAccessResult>(`${ACCESS}/grants/${id}/revoke`, reason ? { reason } : {});
  }

  revokeGrants(grantIds: string[], reason?: string): Observable<RevokeAccessResult> {
    return this.api.post<RevokeAccessResult>(`${ACCESS}/grants/revoke`, reason ? { grantIds, reason } : { grantIds });
  }

  // Mandatos (M1-03)
  getMandateTerms(): Observable<MandateTerms> {
    return this.api.get<MandateTerms>(`${ACCESS}/mandate-terms`);
  }

  acceptTerms(id: string, termsVersion: string): Observable<AccessGrant> {
    return this.api.post<AccessGrant>(`${ACCESS}/grants/${id}/accept-terms`, { termsVersion });
  }

  // Acceso anticipado por booking (M1-20)
  grantEarlyBooking(request: EarlyBookingAccessRequest): Observable<AccessGrant> {
    return this.api.post<AccessGrant>(`${ACCESS}/grants/early-booking`, request);
  }

  // Catálogos del flujo único
  searchGrantees(search: string, organizationType?: OrganizationType | ''): Observable<GranteeOrganization[]> {
    return this.api.get<GranteeOrganization[]>(`${ACCESS}/grantees`, query({ search, organizationType }));
  }

  getGrantableActions(filters: GrantableActionsQuery): Observable<GrantableAction[]> {
    return this.api.get<GrantableAction[]>(`${ACCESS}/grantable-actions`, query(filters));
  }

  // Terceros por defecto (M1-13)
  getDefaults(): Observable<DefaultGrantee[]> {
    return this.api.get<DefaultGrantee[]>(`${ACCESS}/defaults`);
  }

  createDefault(request: CreateDefaultGranteeRequest): Observable<DefaultGrantee> {
    return this.api.post<DefaultGrantee>(`${ACCESS}/defaults`, request);
  }

  updateDefault(id: string, request: UpdateDefaultGranteeRequest): Observable<DefaultGrantee> {
    return this.api.put<DefaultGrantee>(`${ACCESS}/defaults/${id}`, request);
  }

  deleteDefault(id: string): Observable<void> {
    return this.api.delete<void>(`${ACCESS}/defaults/${id}`);
  }

  // Acceso abierto por número de BL (M1-17)
  getOpenAccess(): Observable<OpenAccessSetting> {
    return this.api.get<OpenAccessSetting>(`${ACCESS}/open-access`);
  }

  updateOpenAccess(request: UpdateOpenAccessRequest): Observable<OpenAccessSetting> {
    return this.api.put<OpenAccessSetting>(`${ACCESS}/open-access`, request);
  }

  // Auditoría (M1-23)
  getAudit(filters: AccessAuditSearch): Observable<PagedResult<AccessAuditEntry>> {
    return this.api.get<PagedResult<AccessAuditEntry>>(`${ACCESS}/audit`, query(filters));
  }

  // Ampliación de visibilidad entre roles (M1-16)
  getWidenings(blNumber: string, includeRevoked = false): Observable<VisibilityWidening[]> {
    return this.api.get<VisibilityWidening[]>(
      `${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/visibility-widenings`,
      { includeRevoked },
    );
  }

  createWidenings(blNumber: string, targetRole: WideningTargetRole, actionCodes: string[]): Observable<VisibilityWidening[]> {
    return this.api.post<VisibilityWidening[]>(
      `${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/visibility-widenings`,
      { targetRole, actionCodes },
    );
  }

  revokeWidening(blNumber: string, id: string): Observable<void> {
    return this.api.post<void>(
      `${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/visibility-widenings/${id}/revoke`,
      {},
    );
  }
}
