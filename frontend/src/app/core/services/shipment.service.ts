import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { PagedResult } from '../models/admin-user.model';
import { ShipmentDetail, ShipmentListItem, ShipmentSearch } from '../models/shipment.model';
import { ShipmentAssociation } from '../models/access.model';

/** Listado y detalle de embarques (M2-06, M2-07); el servidor filtra por los accesos (M1-11). */
@Injectable({ providedIn: 'root' })
export class ShipmentService {
  private readonly api = inject(ApiService);

  search(filters: ShipmentSearch): Observable<PagedResult<ShipmentListItem>> {
    const query: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = value as string | number;
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
}
