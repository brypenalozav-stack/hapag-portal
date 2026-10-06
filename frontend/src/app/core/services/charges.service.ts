import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  ApplyChargeRulesResult,
  CommercialConditions,
  ExchangeRate,
  ShipmentCharges,
} from '../models/charges.model';

/**
 * Cargos de un BL con las reglas de Nexus (M4-01 a M4-04, M3-01), condiciones comerciales de la
 * organización (M8-02, M8-03) y tipo de cambio (M5-05). El servidor filtra por los accesos (NF-05).
 */
@Injectable({ providedIn: 'root' })
export class ChargesService {
  private readonly api = inject(ApiService);

  getCharges(blNumber: string): Observable<ShipmentCharges> {
    return this.api.get<ShipmentCharges>(`${API_ENDPOINTS.CHARGES}/${encodeURIComponent(blNumber)}`);
  }

  /** Registra las exenciones de Nexus; si todo queda exento, el proceso termina sin carro (M4-02). */
  applyRules(blNumber: string): Observable<ApplyChargeRulesResult> {
    return this.api.post<ApplyChargeRulesResult>(`${API_ENDPOINTS.CHARGES}/${encodeURIComponent(blNumber)}/apply-rules`, {});
  }

  getCommercialConditions(): Observable<CommercialConditions> {
    return this.api.get<CommercialConditions>(API_ENDPOINTS.COMMERCIAL_CONDITIONS);
  }

  /** Tipo de cambio vigente según Nexus; sin fecha, el de hoy en el país del usuario. */
  getExchangeRate(from: string, to: string, date?: string): Observable<ExchangeRate> {
    return this.api.get<ExchangeRate>(API_ENDPOINTS.EXCHANGE_RATES, { from, to, ...(date ? { date } : {}) });
  }
}
