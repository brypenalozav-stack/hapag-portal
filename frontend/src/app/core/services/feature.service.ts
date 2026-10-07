import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';

/**
 * Flags de funcionalidades del servidor (sección `Features`, GET /config/features). Cada uno agrupa fichas de Fase 2
 * que el cierre de Fase 1 deja apagadas; se reactivan por configuración del backend, sin desplegar el frontend.
 */
export type FeatureName =
  | 'ContactLists'
  | 'Carriers'
  | 'ParentCompany'
  | 'NotificationsInbox'
  | 'Announcements'
  | 'GuideMode'
  | 'OnDemandServices'
  | 'ServiceOrdersPage'
  | 'WarehouseHistory'
  | 'Reinvoicing'
  | 'ApiClients'
  | 'GateOutAdvance'
  | 'DepositProofs'
  | 'CreditImputation'
  | 'FreightCertificate'
  | 'ReleaseLetter'
  | 'AccountStatement'
  | 'AdminHome'
  | 'Impersonation'
  | 'Counter'
  | 'TransactionReports'
  | 'AssistantDelivery'
  | 'DocumentaryDeadlines';

/**
 * Valores mientras no hay respuesta del servidor (o si no responde): los mismos que el backend por defecto. Así los
 * enlaces de Fase 2 no aparecen y desaparecen al cargar la página.
 */
export const DEFAULT_FEATURES: Readonly<Record<FeatureName, boolean>> = {
  ContactLists: false,
  Carriers: false,
  ParentCompany: false,
  NotificationsInbox: false,
  Announcements: false,
  GuideMode: false,
  OnDemandServices: false,
  ServiceOrdersPage: false,
  WarehouseHistory: false,
  Reinvoicing: false,
  ApiClients: false,
  GateOutAdvance: false,
  DepositProofs: false,
  CreditImputation: false,
  FreightCertificate: false,
  ReleaseLetter: true,
  AccountStatement: false,
  AdminHome: false,
  Impersonation: false,
  Counter: true,
  TransactionReports: false,
  AssistantDelivery: false,
  DocumentaryDeadlines: false,
};

/**
 * Flags de funcionalidades leídos una vez del servidor (el endpoint es anónimo: no dependen del usuario y también los
 * usan las rutas públicas). `enabled` es reactivo; `ready()` espera la lectura para los guards de ruta.
 */
@Injectable({ providedIn: 'root' })
export class FeatureService {
  private readonly api = inject(ApiService);

  private readonly flags = signal<Readonly<Record<FeatureName, boolean>>>(DEFAULT_FEATURES);
  private readonly loadedState = signal(false);
  private request$: Observable<void> | null = null;

  /** Ya se leyó la configuración del servidor. */
  readonly loaded = this.loadedState.asReadonly();

  /** Estado de todos los flags. */
  readonly all = computed(() => this.flags());

  constructor() {
    this.ready().subscribe();
  }

  /** Alguno de los flags indicados está encendido. */
  enabled(...names: FeatureName[]): boolean {
    const flags = this.flags();
    return names.some((n) => flags[n] === true);
  }

  /** Completa cuando la configuración está leída (o falló: quedan los valores por defecto). */
  ready(): Observable<void> {
    if (this.loadedState()) return of(undefined);
    if (!this.request$) {
      this.request$ = this.api.get<unknown>(API_ENDPOINTS.CONFIG_FEATURES).pipe(
        map((body) => {
          this.flags.set(FeatureService.parse(body));
          this.loadedState.set(true);
        }),
        // Sin respuesta se mantienen los valores por defecto; se reintenta en la próxima navegación protegida.
        catchError(() => of(undefined)),
        finalize(() => (this.request$ = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }
    return this.request$;
  }

  /** Respuesta del servidor sobre los valores por defecto; lo que no sea booleano se ignora. */
  private static parse(body: unknown): Readonly<Record<FeatureName, boolean>> {
    const result: Record<FeatureName, boolean> = { ...DEFAULT_FEATURES };
    if (!body || typeof body !== 'object' || Array.isArray(body)) return result;
    for (const [key, value] of Object.entries(body as Record<string, unknown>)) {
      const name = (Object.keys(DEFAULT_FEATURES) as FeatureName[]).find((n) => n.toLowerCase() === key.toLowerCase());
      if (name && typeof value === 'boolean') result[name] = value;
    }
    return result;
  }
}
