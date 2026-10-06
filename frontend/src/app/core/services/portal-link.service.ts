import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { DisputeLink } from '../models/portal-link.model';

/**
 * Enlace al módulo de Dispute de productos digitales (M2-05) del país de operación del usuario (M1-04). Se lee
 * una vez por sesión y país y lo comparten el menú lateral y el dashboard; sin URL configurada no se muestra.
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

  constructor() {
    // Al iniciar o cambiar la sesión o el país se vuelve a leer el enlace.
    effect(() => {
      const authenticated = this.auth.isAuthenticated();
      const country = this.auth.currentUser()?.country;
      untracked(() => {
        this.link.set(null);
        if (authenticated) this.load(country);
      });
    });
  }

  private load(country: string | undefined): void {
    this.api.get<DisputeLink>(API_ENDPOINTS.CONFIG_DISPUTE_LINK, country ? { country } : undefined).subscribe({
      next: (link) => this.link.set(link),
      // Sin respuesta el acceso no se muestra: no bloquea el resto del portal (NF-11).
      error: () => this.link.set(null),
    });
  }
}
