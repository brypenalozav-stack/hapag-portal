import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
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

  /**
   * BL accesibles del contenedor (GET /demurrage/container/{n}): el servidor filtra por los accesos del usuario. Lo usa la
   * búsqueda de la barra superior para llevar del número de contenedor al detalle de su BL.
   */
  getBlsByContainer(containerNumber: string): Observable<string[]> {
    return this.api
      .get<{ blNumber: string }[]>(`${API_ENDPOINTS.DEMURRAGE}/container/${encodeURIComponent(containerNumber)}`)
      .pipe(map((charges) => [...new Set(charges.map((c) => c.blNumber).filter((bl) => !!bl))]));
  }

  /** Genera (idempotente) el cargo de demoras anticipadas listo para el carro (M3-16). */
  requestAdvance(blNumber: string): Observable<DemurrageStatus> {
    return this.api.post<DemurrageStatus>(`${API_ENDPOINTS.DEMURRAGE}/${encodeURIComponent(blNumber)}/advance-demurrage`, {});
  }
}
