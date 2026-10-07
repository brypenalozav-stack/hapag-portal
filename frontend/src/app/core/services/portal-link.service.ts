import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { DisputeLink, ExternalLinks } from '../models/portal-link.model';

/** Estado de los enlaces externos: sin consultar, en curso, leídos o sin respuesta. */
export type ExternalLinksState = 'idle' | 'loading' | 'ready' | 'error';

/**
 * Enlaces a sitios de Hapag-Lloyd configurados por país (M1-04):
 * - Dispute de productos digitales (M2-05): se lee una vez por sesión y país y lo comparten el menú y el dashboard;
 *   sin URL configurada no se muestra.
 * - Enlaces externos (portal de devoluciones y tarifarios oficiales): se leen la primera vez que una página los pide
 *   (`loadExternalLinks`) y quedan en memoria para la sesión y el país; sin respuesta las páginas informan que el
 *   acceso no está disponible por ahora.
 */
@Injectable({ providedIn: 'root' })
export class PortalLinkService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  private readonly link = signal<DisputeLink | null>(null);

  /** URL del módulo de Dispute, o null si no está configurada (o aún no se consulta). */
  readonly disputeUrl = computed(() => {
    const link = this.link();
    return link?.configured && link.url ? link.url : null;
  });

  private readonly external = signal<ExternalLinks | null>(null);
  private readonly externalState = signal<ExternalLinksState>('idle');
  private externalRequest?: Subscription;
  /** Sesión y país de los enlaces externos en memoria (o en consulta); null si no se han pedido. */
  private externalKey: string | null = null;

  /** Portal de devoluciones y tarifarios del país; null mientras se consulta o si no hubo respuesta. */
  readonly externalLinks = this.external.asReadonly();
  readonly externalLinksState = this.externalState.asReadonly();

  constructor() {
    // Al iniciar o cambiar la sesión o el país se vuelve a leer el enlace de Dispute; los enlaces externos se
    // descartan y se vuelven a pedir cuando una página los necesite.
    effect(() => {
      const authenticated = this.auth.isAuthenticated();
      const country = this.auth.currentUser()?.country;
      untracked(() => {
        this.link.set(null);
        if (this.externalKey !== null && this.externalKey !== this.sessionKey()) this.resetExternal();
        if (authenticated) this.load(country);
      });
    });
  }

  /** Lee los enlaces externos del país si aún no están en memoria (o si la consulta anterior falló). */
  loadExternalLinks(): void {
    const state = this.externalState();
    if (state === 'loading' || state === 'ready' || !this.auth.isAuthenticated()) return;
    const country = this.auth.currentUser()?.country;
    this.externalKey = this.sessionKey();
    this.externalState.set('loading');
    this.externalRequest = this.api
      .get<ExternalLinks>(API_ENDPOINTS.CONFIG_EXTERNAL_LINKS, country ? { country } : undefined)
      .subscribe({
        next: (links) => {
          // Una respuesta sin la forma esperada se trata como no disponible.
          const valid = !!links && typeof links === 'object' && !!links.refunds && Array.isArray(links.localTariffs);
          this.external.set(valid ? links : null);
          this.externalState.set(valid ? 'ready' : 'error');
        },
        // Sin respuesta las páginas lo informan con calma: no bloquea el resto del portal (NF-11).
        error: () => {
          this.external.set(null);
          this.externalState.set('error');
        },
      });
  }

  private sessionKey(): string {
    const user = this.auth.currentUser();
    return `${user?.id ?? ''}|${user?.country ?? ''}`;
  }

  private resetExternal(): void {
    this.externalRequest?.unsubscribe();
    this.externalRequest = undefined;
    this.externalKey = null;
    this.external.set(null);
    this.externalState.set('idle');
  }

  private load(country: string | undefined): void {
    this.api.get<DisputeLink>(API_ENDPOINTS.CONFIG_DISPUTE_LINK, country ? { country } : undefined).subscribe({
      next: (link) => this.link.set(link),
      // Sin respuesta el acceso no se muestra: no bloquea el resto del portal (NF-11).
      error: () => this.link.set(null),
    });
  }
}
