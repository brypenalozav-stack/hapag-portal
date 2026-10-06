import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  ShipmentPublicationRule,
  ShipmentPublicationRuleChange,
  ShipmentPublicationRuleRequest,
} from '../models/shipment.model';

const BASE = API_ENDPOINTS.SHIPMENT_PUBLICATION_RULES;

/**
 * Mantenedor interno de las reglas de publicación por DIFU de destino final (M2-01, permiso
 * maintainers.manage), con su registro de cambios (NF-15).
 */
@Injectable({ providedIn: 'root' })
export class PublicationRuleService {
  private readonly api = inject(ApiService);

  getRules(country: string, includeInactive: boolean): Observable<ShipmentPublicationRule[]> {
    return this.api.get<ShipmentPublicationRule[]>(BASE, {
      ...(country ? { country } : {}),
      ...(includeInactive ? { includeInactive: true } : {}),
    });
  }

  createRule(body: ShipmentPublicationRuleRequest): Observable<ShipmentPublicationRule> {
    return this.api.post<ShipmentPublicationRule>(BASE, body);
  }

  updateRule(id: string, body: ShipmentPublicationRuleRequest): Observable<ShipmentPublicationRule> {
    return this.api.put<ShipmentPublicationRule>(`${BASE}/${id}`, body);
  }

  /** Desactiva la regla; el registro de cambios se conserva. */
  deactivateRule(id: string): Observable<void> {
    return this.api.delete<void>(`${BASE}/${id}`);
  }

  getHistory(id: string): Observable<ShipmentPublicationRuleChange[]> {
    return this.api.get<ShipmentPublicationRuleChange[]>(`${BASE}/${id}/history`);
  }
}
