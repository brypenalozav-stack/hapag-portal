import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { ReleaseStatus } from '../models/release-status.model';
import { TatcBatch } from '../models/shipment.model';

/** Consulta de BL con los requisitos de liberación, solicitud del TATC y su comprobante (M2-09). */
@Injectable({ providedIn: 'root' })
export class ReleaseStatusService {
  private readonly api = inject(ApiService);

  get(blNumber: string): Observable<ReleaseStatus> {
    return this.api.get<ReleaseStatus>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/release-status`);
  }

  /** Solicita el TATC de un BL con los requisitos cumplidos (una solicitud de un solo BL al sistema de TATC). */
  requestTatc(blNumber: string): Observable<TatcBatch> {
    return this.api.post<TatcBatch>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/release-status/tatc`, {});
  }

  /** Comprobante PDF de los TATC emitidos del BL, o de un contenedor. */
  downloadVoucher(blNumber: string, containerNumber?: string): Observable<Blob> {
    return this.api.getBlob(
      `${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/tatc/voucher`,
      containerNumber ? { container: containerNumber } : undefined,
    );
  }
}
