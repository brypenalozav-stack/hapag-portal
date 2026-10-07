import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  ChargeConcept,
  InternalChargeRule,
  InternalChargeRuleChange,
  InternalChargeRuleRequest,
  InternalChargeRuleSearch,
  Tariff,
  TariffChange,
  TariffRequest,
  TariffSearch,
} from '../models/tariff.model';

/** Quita los filtros vacíos antes de enviarlos como query string. */
function query(filters: object): Record<string, string | number | boolean> {
  const params: Record<string, string | number | boolean> = {};
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== null && value !== '') params[key] = value as string | number | boolean;
  }
  return params;
}

/**
 * Mantenedores internos (permiso maintainers.manage): tarifas de cargos locales (M8-01) y reglas
 * internas de cobro (M3-04, M3-16), con su registro de cambios (NF-15).
 */
@Injectable({ providedIn: 'root' })
export class TariffService {
  private readonly api = inject(ApiService);

  // Tarifas (M8-01)
  getTariffs(filters: TariffSearch): Observable<Tariff[]> {
    return this.api.get<Tariff[]>(API_ENDPOINTS.TARIFFS, query(filters));
  }

  getTariff(id: string): Observable<Tariff> {
    return this.api.get<Tariff>(`${API_ENDPOINTS.TARIFFS}/${id}`);
  }

  createTariff(body: TariffRequest): Observable<Tariff> {
    return this.api.post<Tariff>(API_ENDPOINTS.TARIFFS, body);
  }

  updateTariff(id: string, body: TariffRequest): Observable<Tariff> {
    return this.api.put<Tariff>(`${API_ENDPOINTS.TARIFFS}/${id}`, body);
  }

  /** Desactiva la tarifa; el registro de cambios se conserva. */
  deactivateTariff(id: string): Observable<void> {
    return this.api.delete<void>(`${API_ENDPOINTS.TARIFFS}/${id}`);
  }

  getTariffHistory(id: string): Observable<TariffChange[]> {
    return this.api.get<TariffChange[]>(`${API_ENDPOINTS.TARIFFS}/${id}/history`);
  }

  getConcepts(country?: string): Observable<ChargeConcept[]> {
    return this.api.get<ChargeConcept[]>(`${API_ENDPOINTS.TARIFFS}/concepts`, query({ country }));
  }

  // Reglas internas de cobro (M3-04, M3-16)
  getRules(filters: InternalChargeRuleSearch): Observable<InternalChargeRule[]> {
    return this.api.get<InternalChargeRule[]>(API_ENDPOINTS.INTERNAL_CHARGE_RULES, query(filters));
  }

  createRule(body: InternalChargeRuleRequest): Observable<InternalChargeRule> {
    return this.api.post<InternalChargeRule>(API_ENDPOINTS.INTERNAL_CHARGE_RULES, body);
  }

  updateRule(id: string, body: InternalChargeRuleRequest): Observable<InternalChargeRule> {
    return this.api.put<InternalChargeRule>(`${API_ENDPOINTS.INTERNAL_CHARGE_RULES}/${id}`, body);
  }

  deactivateRule(id: string): Observable<void> {
    return this.api.delete<void>(`${API_ENDPOINTS.INTERNAL_CHARGE_RULES}/${id}`);
  }

  getRuleHistory(id: string): Observable<InternalChargeRuleChange[]> {
    return this.api.get<InternalChargeRuleChange[]>(`${API_ENDPOINTS.INTERNAL_CHARGE_RULES}/${id}/history`);
  }
}
