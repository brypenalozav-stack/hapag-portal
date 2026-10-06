import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { ServiceDefinition, ServiceDefinitionChange, ServiceDefinitionInput } from '../models/service-request.model';

const BASE = API_ENDPOINTS.SERVICE_DEFINITIONS;

/**
 * Mantenedor de definiciones de servicios on demand (M2-03, M2-04; permiso `maintainers.manage`): condiciones,
 * formulario tipado, cobro, plazos y equipos, con registro de cambios (NF-15). La tarifa se mantiene en /tariffs.
 */
@Injectable({ providedIn: 'root' })
export class ServiceDefinitionService {
  private readonly api = inject(ApiService);

  list(options: { includeInactive?: boolean; country?: string; operation?: string } = {}): Observable<ServiceDefinition[]> {
    return this.api.get<ServiceDefinition[]>(BASE, {
      ...(options.includeInactive ? { includeInactive: true } : {}),
      ...(options.country ? { country: options.country } : {}),
      ...(options.operation ? { operation: options.operation } : {}),
    });
  }

  get(id: string): Observable<ServiceDefinition> {
    return this.api.get<ServiceDefinition>(`${BASE}/${id}`);
  }

  create(body: ServiceDefinitionInput): Observable<ServiceDefinition> {
    return this.api.post<ServiceDefinition>(BASE, body);
  }

  /** También reactiva con `isActive: true`. */
  update(id: string, body: ServiceDefinitionInput): Observable<ServiceDefinition> {
    return this.api.put<ServiceDefinition>(`${BASE}/${id}`, body);
  }

  /** Desactiva la definición; el registro de cambios se conserva. */
  deactivate(id: string): Observable<void> {
    return this.api.delete<void>(`${BASE}/${id}`);
  }

  history(id: string): Observable<ServiceDefinitionChange[]> {
    return this.api.get<ServiceDefinitionChange[]>(`${BASE}/${id}/history`);
  }
}
