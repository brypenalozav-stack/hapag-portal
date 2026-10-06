import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { Guide, GuideDefinition, GuideRequest, GuideSnapshot } from '../models/guide.model';
import { MaintainerChange } from '../models/payment-config.model';

const ADMIN = API_ENDPOINTS.ADMIN_GUIDES;

/** Guías del portal (Fase 2, Ola I, M1-27): las activas por pantalla, el avance del usuario y el mantenedor interno. */
@Injectable({ providedIn: 'root' })
export class GuideService {
  private readonly api = inject(ApiService);

  getForRoute(route: string): Observable<Guide[]> {
    return this.api.get<Guide[]>(API_ENDPOINTS.GUIDES, { route });
  }

  /** Completada, descartada o `Reset` para volver a ofrecerla. */
  saveState(code: string, status: 'Completed' | 'Dismissed' | 'Reset', lastStep?: number): Observable<Guide> {
    return this.api.put<Guide>(`${API_ENDPOINTS.GUIDES}/${encodeURIComponent(code)}/state`, lastStep === undefined ? { status } : { status, lastStep });
  }

  search(): Observable<GuideDefinition[]> {
    return this.api.get<GuideDefinition[]>(ADMIN);
  }

  create(body: GuideRequest): Observable<GuideDefinition> {
    return this.api.post<GuideDefinition>(ADMIN, body);
  }

  update(id: string, body: GuideRequest): Observable<GuideDefinition> {
    return this.api.put<GuideDefinition>(`${ADMIN}/${id}`, body);
  }

  remove(id: string): Observable<void> {
    return this.api.delete<void>(`${ADMIN}/${id}`);
  }

  getHistory(id: string): Observable<MaintainerChange<GuideSnapshot>[]> {
    return this.api.get<MaintainerChange<GuideSnapshot>[]>(`${ADMIN}/${id}/history`);
  }
}
