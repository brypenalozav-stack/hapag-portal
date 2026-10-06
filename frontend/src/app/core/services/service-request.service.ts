import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import {
  AdminServiceRequestFilters,
  AvailableServices,
  CreateServiceRequest,
  QuoteServiceRequest,
  ServiceDefinitionOption,
  ServiceQuote,
  ServiceReference,
  ServiceRequestAttachment,
  ServiceRequestDetail,
  ServiceRequestFilters,
  ServiceRequestSummary,
  SubmitServiceRequest,
  UpdateServiceRequest,
} from '../models/service-request.model';

const BASE = API_ENDPOINTS.SERVICE_REQUESTS;
const ADMIN = API_ENDPOINTS.ADMIN_SERVICE_REQUESTS;

function reference(ref: ServiceReference): Record<string, string> {
  return ref.bookingNumber && !ref.blNumber
    ? { bookingNumber: ref.bookingNumber.trim() }
    : { blNumber: (ref.blNumber ?? '').trim() };
}

function filters(f: AdminServiceRequestFilters): Record<string, string | number> {
  return {
    ...(f.status ? { status: f.status } : {}),
    ...(f.definitionCode ? { definitionCode: f.definitionCode } : {}),
    ...(f.blNumber ? { blNumber: f.blNumber.trim() } : {}),
    ...(f.team ? { team: f.team } : {}),
    ...(f.country ? { country: f.country } : {}),
    ...(f.organizationId ? { organizationId: f.organizationId } : {}),
    page: f.page ?? 1,
    pageSize: f.pageSize ?? 20,
  };
}

/**
 * Servicios on demand del cliente (Fase 2, Ola G; M2-03, M2-04, M3-07 a M3-15): servicios disponibles para un BL o
 * booking, cotización con el tramo vigente, solicitud (borrador, envío, anulación), adjuntos y seguimiento. El
 * servidor decide disponibilidad, cobro y permisos (M1-11, NF-05); el cargo generado se paga desde el carro (M5-01).
 */
@Injectable({ providedIn: 'root' })
export class ServiceRequestService {
  private readonly api = inject(ApiService);

  /** Definiciones activas en orden de presentación, para los filtros (cualquier usuario autenticado). */
  definitions(options: { country?: string; operation?: string } = {}): Observable<ServiceDefinitionOption[]> {
    return this.api.get<ServiceDefinitionOption[]>(`${BASE}/definitions`, {
      ...(options.country ? { country: options.country } : {}),
      ...(options.operation ? { operation: options.operation } : {}),
    });
  }

  getAvailable(ref: ServiceReference, includeUnavailable = false): Observable<AvailableServices> {
    return this.api.get<AvailableServices>(`${BASE}/available`, { ...reference(ref), includeUnavailable });
  }

  /** Precio si se enviara ahora: tramo vigente, dentro o fuera de plazo y exenciones. */
  quote(body: QuoteServiceRequest): Observable<ServiceQuote> {
    return this.api.post<ServiceQuote>(`${BASE}/quote`, body);
  }

  create(body: CreateServiceRequest): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(BASE, body);
  }

  /** Solo borradores. */
  update(id: string, body: UpdateServiceRequest): Observable<ServiceRequestDetail> {
    return this.api.put<ServiceRequestDetail>(`${BASE}/${id}`, body);
  }

  submit(id: string, body: SubmitServiceRequest): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${BASE}/${id}/submit`, body);
  }

  /** Antes del pago; el cargo generado se retira y sale del carro. */
  cancel(id: string, reason: string | null): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${BASE}/${id}/cancel`, reason ? { reason } : {});
  }

  /** Adjunto de un campo `file` del borrador (PDF, PNG o JPEG de hasta 10 MB). */
  uploadAttachment(id: string, fieldKey: string, file: File): Observable<ServiceRequestAttachment> {
    const body = new FormData();
    body.append('fieldKey', fieldKey);
    body.append('file', file, file.name);
    return this.api.post<ServiceRequestAttachment>(`${BASE}/${id}/attachments`, body);
  }

  /** Adjunto o documento de salida (`_output`). */
  downloadAttachment(id: string, attachmentId: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/${id}/attachments/${attachmentId}`);
  }

  /** Solicitudes de la organización, como solicitante o como mandante; `blNumber` también busca el booking. */
  list(f: ServiceRequestFilters): Observable<PagedResult<ServiceRequestSummary>> {
    return this.api.get<PagedResult<ServiceRequestSummary>>(BASE, filters(f));
  }

  get(id: string): Observable<ServiceRequestDetail> {
    return this.api.get<ServiceRequestDetail>(`${BASE}/${id}`);
  }
}

/**
 * Bandeja interna de solicitudes (permiso `service-requests.process`): equipos ED y Customer Service aprueban o
 * rechazan, agregan notas, suben el documento de salida y completan. Cada cambio de estado avisa al cliente.
 */
@Injectable({ providedIn: 'root' })
export class AdminServiceRequestService {
  private readonly api = inject(ApiService);

  /** Sin estado, BL ni organización trae solo lo que hay que atender, del cambio más antiguo al más reciente. */
  list(f: AdminServiceRequestFilters): Observable<PagedResult<ServiceRequestSummary>> {
    return this.api.get<PagedResult<ServiceRequestSummary>>(ADMIN, filters(f));
  }

  get(id: string): Observable<ServiceRequestDetail> {
    return this.api.get<ServiceRequestDetail>(`${ADMIN}/${id}`);
  }

  approve(id: string, notes: string | null): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${ADMIN}/${id}/approve`, notes ? { notes } : {});
  }

  reject(id: string, reason: string): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${ADMIN}/${id}/reject`, { reason });
  }

  complete(id: string, notes: string | null): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${ADMIN}/${id}/complete`, notes ? { notes } : {});
  }

  addNote(id: string, notes: string): Observable<ServiceRequestDetail> {
    return this.api.post<ServiceRequestDetail>(`${ADMIN}/${id}/notes`, { notes });
  }

  uploadOutput(id: string, file: File): Observable<ServiceRequestAttachment> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.api.post<ServiceRequestAttachment>(`${ADMIN}/${id}/attachments`, body);
  }

  downloadAttachment(id: string, attachmentId: string): Observable<Blob> {
    return this.api.getBlob(`${ADMIN}/${id}/attachments/${attachmentId}`);
  }
}
