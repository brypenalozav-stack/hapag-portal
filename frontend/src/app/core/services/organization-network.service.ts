import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  CarrierPreRegistration,
  CarrierPreRegistrationRequest,
  CarrierPreRegistrationResult,
  ContactList,
  ContactListChange,
  ContactListsView,
  ParentCandidate,
  ParentCompanyView,
  ParentLink,
} from '../models/organization-network.model';

const ME = API_ENDPOINTS.ORGANIZATIONS_ME;
const LINKS = API_ENDPOINTS.ADMIN_ORGANIZATION_LINKS;

/**
 * Fase 2, Ola I, en Mi organización: listas de distribución de contactos por tipo de reporte (M1-06), transportistas
 * pre-creados con sus BL y bookings asignados (M1-09) y la vinculación con la empresa matriz (M1-21), con la revisión
 * interna de esas vinculaciones (`organizations.review`). El servidor vuelve a exigir cada permiso.
 */
@Injectable({ providedIn: 'root' })
export class OrganizationNetworkService {
  private readonly api = inject(ApiService);

  // Listas de distribución (M1-06)
  getContactLists(): Observable<ContactListsView> {
    return this.api.get<ContactListsView>(`${ME}/contact-lists`);
  }

  updateContactList(reportType: string, emails: string[]): Observable<ContactList> {
    return this.api.put<ContactList>(`${ME}/contact-lists/${encodeURIComponent(reportType)}`, { emails });
  }

  getContactListHistory(reportType?: string): Observable<ContactListChange[]> {
    return this.api.get<ContactListChange[]>(`${ME}/contact-lists/history`, reportType ? { reportType } : {});
  }

  // Transportistas pre-creados (M1-09)
  getCarriers(): Observable<CarrierPreRegistration[]> {
    return this.api.get<CarrierPreRegistration[]>(`${ME}/carriers`);
  }

  preCreateCarrier(body: CarrierPreRegistrationRequest): Observable<CarrierPreRegistrationResult> {
    return this.api.post<CarrierPreRegistrationResult>(`${ME}/carriers`, body);
  }

  assignToCarrier(id: string, body: { blNumbers?: string[]; bookingNumbers?: string[] }): Observable<CarrierPreRegistrationResult> {
    return this.api.post<CarrierPreRegistrationResult>(`${ME}/carriers/${id}/assignments`, body);
  }

  /** Reenvía la invitación de una cuenta pre-creada; responde 202 siempre (no revela si existe). */
  resendPreCreatedInvitation(email: string): Observable<void> {
    return this.api.post<void>(API_ENDPOINTS.AUTH_RESEND_PRE_CREATED_INVITATION, { email });
  }

  // Empresa matriz (M1-21)
  getParentCompany(): Observable<ParentCompanyView> {
    return this.api.get<ParentCompanyView>(`${ME}/parent-company`);
  }

  searchParentCandidates(search: string): Observable<ParentCandidate[]> {
    return this.api.get<ParentCandidate[]>(`${ME}/parent-company/candidates`, { search });
  }

  requestParentLink(body: { parentOrganizationId: string; enableVisibility: boolean; notes?: string }): Observable<ParentLink> {
    return this.api.post<ParentLink>(`${ME}/parent-company`, body);
  }

  setParentVisibility(enabled: boolean): Observable<ParentLink> {
    return this.api.put<ParentLink>(`${ME}/parent-company/visibility`, { enabled });
  }

  removeParentLink(): Observable<void> {
    return this.api.delete<void>(`${ME}/parent-company`);
  }

  // Revisión interna (organizations.review)
  getParentLinks(status?: string): Observable<ParentLink[]> {
    return this.api.get<ParentLink[]>(LINKS, status ? { status } : {});
  }

  approveParentLink(id: string, notes?: string): Observable<ParentLink> {
    return this.api.post<ParentLink>(`${LINKS}/${id}/approve`, notes ? { notes } : {});
  }

  rejectParentLink(id: string, reason: string): Observable<ParentLink> {
    return this.api.post<ParentLink>(`${LINKS}/${id}/reject`, { reason });
  }
}
