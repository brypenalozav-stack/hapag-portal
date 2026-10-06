import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  CreateOrganizationUserRequest,
  JoinRequest,
  OperatingCountry,
  OrganizationDocument,
  OrganizationDocumentType,
  OrganizationProfile,
  OrganizationSummary,
  OrganizationUser,
  UpdateOrganizationUserRequest,
} from '../models/organization.model';

const ME = API_ENDPOINTS.ORGANIZATIONS_ME;

/** Organización del usuario autenticado (M1-02, M1-04, M1-07, M1-08). */
@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly api = inject(ApiService);

  getMine(): Observable<OrganizationSummary> {
    return this.api.get<OrganizationSummary>(ME);
  }

  // Usuarios (M1-02), permiso org.users.manage
  getUsers(): Observable<OrganizationUser[]> {
    return this.api.get<OrganizationUser[]>(`${ME}/users`);
  }

  createUser(data: CreateOrganizationUserRequest): Observable<OrganizationUser> {
    return this.api.post<OrganizationUser>(`${ME}/users`, data);
  }

  updateUser(id: string, data: UpdateOrganizationUserRequest): Observable<OrganizationUser> {
    return this.api.put<OrganizationUser>(`${ME}/users/${id}`, data);
  }

  setUserActive(id: string, isActive: boolean): Observable<void> {
    return this.api.post<void>(`${ME}/users/${id}/active`, { isActive });
  }

  // Solicitudes de vinculación (M1-08), permiso org.requests.approve
  getJoinRequests(): Observable<JoinRequest[]> {
    return this.api.get<JoinRequest[]>(`${ME}/join-requests`);
  }

  approveJoinRequest(userId: string, profile: OrganizationProfile): Observable<unknown> {
    return this.api.post<unknown>(`${ME}/join-requests/${userId}/approve`, { profile });
  }

  rejectJoinRequest(userId: string, reason?: string): Observable<void> {
    return this.api.post<void>(`${ME}/join-requests/${userId}/reject`, reason ? { reason } : {});
  }

  // País de operación (M1-04)
  getOperatingCountry(): Observable<OperatingCountry> {
    return this.api.get<OperatingCountry>(`${ME}/operating-country`);
  }

  setOperatingCountry(country: 'CL' | 'BO'): Observable<OperatingCountry> {
    return this.api.put<OperatingCountry>(`${ME}/operating-country`, { country });
  }

  // Documentación de respaldo (M1-07)
  getDocuments(): Observable<OrganizationDocument[]> {
    return this.api.get<OrganizationDocument[]>(`${ME}/documents`);
  }

  uploadDocument(documentType: OrganizationDocumentType, file: File): Observable<OrganizationDocument> {
    const body = new FormData();
    body.append('documentType', documentType);
    body.append('file', file, file.name);
    return this.api.post<OrganizationDocument>(`${ME}/documents`, body);
  }
}
