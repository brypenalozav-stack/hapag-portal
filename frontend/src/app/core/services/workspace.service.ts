import { Injectable, computed, inject, linkedSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './auth.service';

/** Espacio de trabajo del menú: el portal de clientes o el backoffice de los perfiles internos. */
export type Workspace = 'customer' | 'backoffice';

const WORKSPACE_KEY = 'hl_workspace';

/** Rutas del backoffice: entrar a una de ellas cambia el espacio automáticamente. */
export function isBackofficeUrl(url: string): boolean {
  const path = url.split(/[?#]/)[0];
  return path === '/admin' || path.startsWith('/admin/');
}

/**
 * Espacio de trabajo activo (cierre de Fase 1, menú). Los perfiles internos alternan entre "Portal clientes" y
 * "Backoffice" con el selector de la barra superior; el menú de cada espacio no mezcla funciones del otro. La elección
 * se recuerda por usuario (`hl_workspace:<id>`); sin elección guardada, el perfil interno entra al backoffice. Abrir
 * una ruta /admin pasa al backoffice. Los clientes siempre están en el portal de clientes.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Elección del usuario actual (null: sin elección guardada); se relee al cambiar de usuario o al terminar la vista como cliente. */
  private readonly chosen = linkedSignal<Workspace | null>(() => this.load(this.auth.currentUser()?.id));

  /** El usuario puede alternar entre espacios (perfil interno, también fuera de la vista como cliente). */
  readonly canSwitch = computed(() => this.auth.isInternal());

  readonly current = computed<Workspace>(() => (this.canSwitch() ? (this.chosen() ?? 'backoffice') : 'customer'));

  constructor() {
    if (this.canSwitch() && isBackofficeUrl(this.router.url)) this.set('backoffice');
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe((e) => {
        if (this.canSwitch() && isBackofficeUrl(e.urlAfterRedirects) && this.current() !== 'backoffice') this.set('backoffice');
      });
  }

  set(workspace: Workspace): void {
    this.chosen.set(workspace);
    const id = this.auth.currentUser()?.id;
    if (!id) return;
    try {
      localStorage.setItem(`${WORKSPACE_KEY}:${id}`, workspace);
    } catch {
      // Sin almacenamiento disponible (modo privado o cuota): la elección vale solo para esta sesión.
    }
  }

  private load(userId: string | undefined): Workspace | null {
    if (!userId) return null;
    try {
      const value = localStorage.getItem(`${WORKSPACE_KEY}:${userId}`);
      return value === 'customer' || value === 'backoffice' ? value : null;
    } catch {
      return null;
    }
  }
}
