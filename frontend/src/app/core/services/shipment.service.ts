import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import {
  ShipmentDetail,
  ShipmentIssuance,
  ShipmentListItem,
  ShipmentSearch,
  ShipmentTatc,
  TatcBatch,
  TatcBatchRequest,
} from '../models/shipment.model';
import { ShipmentAssociation } from '../models/access.model';

/**
 * Listado y detalle de embarques (M2-06, M2-07); el servidor filtra por los accesos (M1-11) y por las reglas
 * de publicación por DIFU (M2-01). Ola F: estado de emisión del documento de transporte (M2-02) y TATC del BL
 * con su solicitud masiva por localidad (M2-09).
 */
@Injectable({ providedIn: 'root' })
export class ShipmentService {
  private readonly api = inject(ApiService);

  search(filters: ShipmentSearch): Observable<PagedResult<ShipmentListItem>> {
    const query: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = typeof value === 'boolean' ? String(value) : value;
    }
    return this.api.get<PagedResult<ShipmentListItem>>(API_ENDPOINTS.SHIPMENTS, query);
  }

  getByBl(blNumber: string): Observable<ShipmentDetail> {
    return this.api.get<ShipmentDetail>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}`);
  }

  /** Autoasociación a un BL consultado con acceso abierto (M1-18). */
  associate(blNumber: string): Observable<ShipmentAssociation> {
    return this.api.post<ShipmentAssociation>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/associate`, {});
  }

  /** Estado de emisión leído del origen en el momento (M2-02). */
  getIssuance(blNumber: string): Observable<ShipmentIssuance> {
    return this.api.get<ShipmentIssuance>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/issuance`);
  }

  /** Estado del BL y de su TATC por contenedor, leído del sistema de TATC (M2-09). */
  getTatc(blNumber: string): Observable<ShipmentTatc> {
    return this.api.get<ShipmentTatc>(`${API_ENDPOINTS.SHIPMENTS}/${encodeURIComponent(blNumber)}/tatc`);
  }

  /** Generación masiva de TATC para BL de una misma localidad: una sola solicitud al sistema de TATC. */
  requestTatcBatch(request: TatcBatchRequest): Observable<TatcBatch> {
    return this.api.post<TatcBatch>(`${API_ENDPOINTS.SHIPMENTS}/tatc-batches`, request);
  }

  /** Últimas 50 solicitudes masivas de la organización (sin el detalle por BL). */
  getTatcBatches(): Observable<TatcBatch[]> {
    return this.api.get<TatcBatch[]>(`${API_ENDPOINTS.SHIPMENTS}/tatc-batches`);
  }

  getTatcBatch(id: string): Observable<TatcBatch> {
    return this.api.get<TatcBatch>(`${API_ENDPOINTS.SHIPMENTS}/tatc-batches/${id}`);
  }
}
