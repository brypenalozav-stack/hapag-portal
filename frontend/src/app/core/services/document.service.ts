import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  BlCopyRequest,
  DocumentDelivery,
  FreightCertificateContext,
  FreightCertificateRequest,
  FreightCertificateResult,
  NoDebtEligibility,
  ReleaseLetterContext,
  ReleaseLetterRequest,
  ReleaseLetterRequestBody,
  ResponsibilityLetterRequest,
  ResponsibilityLetterTerms,
  ShipmentDocument,
  ShipmentDocuments,
  TransshipmentRequest,
} from '../models/document.model';

const BASE = API_ENDPOINTS.DOCUMENTS;

/** Prefijo de las rutas de descarga absolutas que informa el servidor (`/api/v1/...`). */
const API_PREFIX = /^\/?api\/v1\//;

/**
 * Repositorio documental del embarque (M6-09) y solicitudes de documentos: copia del BL (M6-05), carta de
 * responsabilidad (M6-06), certificado de libre deuda (M6-07) y certificado de transbordo (M6-01). Fase 2, Ola J:
 * certificado de flete (M6-02) y carta de liberación y desconsolidado (M6-08) de Bolivia. El servidor aplica la
 * matriz de M1-11 y registra cada descarga y envío (NF-14).
 */
@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly api = inject(ApiService);

  getDocuments(blNumber: string): Observable<ShipmentDocuments> {
    return this.api.get<ShipmentDocuments>(`${BASE}/${encodeURIComponent(blNumber)}`);
  }

  download(blNumber: string, documentId: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/${encodeURIComponent(blNumber)}/${documentId}/download`);
  }

  /** Comprobante o factura relacionados, por la ruta absoluta que informa el servidor. */
  downloadRelated(downloadPath: string): Observable<Blob> {
    return this.api.getBlob(downloadPath.replace(API_PREFIX, ''));
  }

  /** Reenvía el PDF al correo registrado de la organización del usuario (M6-05). */
  resend(blNumber: string, documentId: string): Observable<DocumentDelivery> {
    return this.api.post<DocumentDelivery>(`${BASE}/${encodeURIComponent(blNumber)}/${documentId}/send`, {});
  }

  requestBlCopy(blNumber: string, request: BlCopyRequest): Observable<DocumentDelivery> {
    return this.api.post<DocumentDelivery>(`${BASE}/${encodeURIComponent(blNumber)}/bl-copy`, request);
  }

  getLetterTerms(): Observable<ResponsibilityLetterTerms> {
    return this.api.get<ResponsibilityLetterTerms>(`${BASE}/responsibility-letter/terms`);
  }

  /** Emite la carta; una carta vigente levanta el bloqueo de M4-04 sobre el BL. */
  issueResponsibilityLetter(blNumber: string, request: ResponsibilityLetterRequest): Observable<ShipmentDocument> {
    return this.api.post<ShipmentDocument>(`${BASE}/${encodeURIComponent(blNumber)}/responsibility-letter`, request);
  }

  getNoDebtEligibility(blNumber: string): Observable<NoDebtEligibility> {
    return this.api.get<NoDebtEligibility>(`${BASE}/${encodeURIComponent(blNumber)}/no-debt-certificate`);
  }

  issueNoDebtCertificate(blNumber: string): Observable<ShipmentDocument> {
    return this.api.post<ShipmentDocument>(`${BASE}/${encodeURIComponent(blNumber)}/no-debt-certificate`, {});
  }

  /** Crea (o devuelve) el cargo del certificado de transbordo, que se paga con el carro (M6-01). */
  requestTransshipmentCertificate(blNumber: string): Observable<TransshipmentRequest> {
    return this.api.post<TransshipmentRequest>(`${BASE}/${encodeURIComponent(blNumber)}/transshipment-certificate`, {});
  }

  /** Gestión del certificado de flete del BL (M6-02): flete, consignatario, finalidades, solicitudes y certificados. */
  getFreightCertificate(blNumber: string): Observable<FreightCertificateContext> {
    return this.api.get<FreightCertificateContext>(`${BASE}/${encodeURIComponent(blNumber)}/freight-certificate`);
  }

  /** Registra la solicitud y emite el certificado firmado, sin pago ni carro en esta entrega. */
  requestFreightCertificate(blNumber: string, request: FreightCertificateRequest): Observable<FreightCertificateResult> {
    return this.api.post<FreightCertificateResult>(`${BASE}/${encodeURIComponent(blNumber)}/freight-certificate`, request);
  }

  /** Gestión de la carta de liberación y desconsolidado (M6-08): unidades con su TATC, transportistas y solicitudes. */
  getReleaseLetter(blNumber: string): Observable<ReleaseLetterContext> {
    return this.api.get<ReleaseLetterContext>(`${BASE}/${encodeURIComponent(blNumber)}/release-letter`);
  }

  /** Envía la carta a la aprobación de Customer Service; la carta se emite al aprobarse. */
  requestReleaseLetter(blNumber: string, request: ReleaseLetterRequestBody): Observable<ReleaseLetterRequest> {
    return this.api.post<ReleaseLetterRequest>(`${BASE}/${encodeURIComponent(blNumber)}/release-letter`, request);
  }

  /** Carta de la organización (como solicitante o mandante); otra organización recibe 404. */
  getReleaseLetterRequest(id: string): Observable<ReleaseLetterRequest> {
    return this.api.get<ReleaseLetterRequest>(`${BASE}/release-letter/requests/${id}`);
  }
}
