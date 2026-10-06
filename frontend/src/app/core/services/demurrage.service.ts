import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { CalculateDemurrageRequest, DemurrageCalculation, DemurrageStatus } from '../models/demurrage.model';

/** Demurrage por estado del BL (M3-18), calculadora, MHD (M3-02) y demoras anticipadas de Bolivia (M3-16). */
@Injectable({ providedIn: 'root' })
export class DemurrageService {
  private readonly api = inject(ApiService);

  getStatus(blNumber: string): Observable<DemurrageStatus> {
    return this.api.get<DemurrageStatus>(`${API_ENDPOINTS.DEMURRAGE}/${encodeURIComponent(blNumber)}/status`);
  }

  /** Calcula (vista previa con `save: false`) o guarda el cálculo; bloqueado si existe factura. */
  calculate(blNumber: string, request: CalculateDemurrageRequest): Observable<DemurrageCalculation> {
    return this.api.post<DemurrageCalculation>(`${API_ENDPOINTS.DEMURRAGE}/${encodeURIComponent(blNumber)}/calculate`, request);
  }

  /** Genera (idempotente) el cargo de demoras anticipadas listo para el carro (M3-16). */
  requestAdvance(blNumber: string): Observable<DemurrageStatus> {
    return this.api.post<DemurrageStatus>(`${API_ENDPOINTS.DEMURRAGE}/${encodeURIComponent(blNumber)}/advance-demurrage`, {});
  }
}
