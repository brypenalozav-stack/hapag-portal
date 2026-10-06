import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { ServiceRequestAttachment } from '../models/service-request.model';
import {
  CreateReinvoicing,
  REINVOICING_APPROVAL_FIELD,
  ReinvoicingAcceptanceResponse,
  ReinvoicingAcceptanceView,
  ReinvoicingDetail,
  ReinvoicingQuote,
} from '../models/reinvoicing.model';

const BASE = API_ENDPOINTS.REINVOICING;

/**
 * Refacturación IAO con pérdida de IVA (Fase 2, Ola H, M3-11): cotización desde una factura, borrador con los datos de la
 * nueva razón social, aprobación adjunta (adjunto genérico de la solicitud), envío con la tarifa aceptada y reenvío del
 * enlace de aceptación. El enlace (`acceptance/{token}`) es público: lo responde la nueva razón social sin sesión.
 */
@Injectable({ providedIn: 'root' })
export class ReinvoicingService {
  private readonly api = inject(ApiService);

  quote(invoiceId: string): Observable<ReinvoicingQuote> {
    return this.api.get<ReinvoicingQuote>(`${BASE}/quote`, { invoiceId });
  }

  create(body: CreateReinvoicing): Observable<ReinvoicingDetail> {
    return this.api.post<ReinvoicingDetail>(BASE, body);
  }

  get(id: string): Observable<ReinvoicingDetail> {
    return this.api.get<ReinvoicingDetail>(`${BASE}/${id}`);
  }

  /** Aprobación de la nueva razón social (PDF, PNG o JPEG de hasta 10 MB); solo en borrador. */
  uploadApproval(requestId: string, file: File): Observable<ServiceRequestAttachment> {
    const body = new FormData();
    body.append('fieldKey', REINVOICING_APPROVAL_FIELD);
    body.append('file', file, file.name);
    return this.api.post<ServiceRequestAttachment>(`${API_ENDPOINTS.SERVICE_REQUESTS}/${requestId}/attachments`, body);
  }

  /** Envía con la tarifa aceptada: genera los dos cargos y el enlace de aceptación. */
  submit(id: string, acceptedTotal: number): Observable<ReinvoicingDetail> {
    return this.api.post<ReinvoicingDetail>(`${BASE}/${id}/submit`, { acceptTariff: true, acceptedTotal });
  }

  /** Enlace nuevo (el anterior deja de funcionar); solo mientras la aceptación está pendiente. */
  resendAcceptance(id: string): Observable<ReinvoicingDetail> {
    return this.api.post<ReinvoicingDetail>(`${BASE}/${id}/acceptance/resend`, {});
  }

  getAcceptance(token: string): Observable<ReinvoicingAcceptanceView> {
    return this.api.get<ReinvoicingAcceptanceView>(`${BASE}/acceptance/${encodeURIComponent(token)}`);
  }

  respondAcceptance(token: string, body: ReinvoicingAcceptanceResponse): Observable<ReinvoicingAcceptanceView> {
    return this.api.post<ReinvoicingAcceptanceView>(`${BASE}/acceptance/${encodeURIComponent(token)}`, body);
  }
}
