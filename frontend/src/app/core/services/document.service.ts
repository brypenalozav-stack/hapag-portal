import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  BlCopyRequest,
  DocumentDelivery,
  NoDebtEligibility,
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
 * responsabilidad (M6-06), certificado de libre deuda (M6-07) y certificado de transbordo (M6-01). El
 * servidor aplica la matriz de M1-11 y registra cada descarga y envío (NF-14).
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
}
