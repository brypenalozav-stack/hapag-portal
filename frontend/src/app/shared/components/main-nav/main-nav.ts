import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { filter, map } from 'rxjs';
import { PERMISSIONS } from '../../../core/constants/app.constants';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { FeatureService } from '../../../core/services/feature.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { DisputeLinkComponent } from '../dispute-link/dispute-link';
import { WorkspaceSwitcherComponent } from '../workspace-switcher/workspace-switcher';
import { MENUS, MenuContext, MenuGroup, MenuLink } from './main-nav.config';

/** Espera antes de abrir o cerrar un panel con el puntero: evita aperturas al pasar de largo. */
const HOVER_OPEN_MS = 120;
const HOVER_CLOSE_MS = 220;

type RouteLink = Extract<MenuLink, { kind: 'route' }>;

/**
 * Menú principal del portal.
 * - Escritorio (≥ 992 px): barra horizontal con paneles desplegables por grupo (mega menú). Cada grupo es un botón
 *   de divulgación (aria-expanded/aria-controls); se abre con clic, Enter/Espacio o al detener el puntero, y se
 *   cierra con Esc (el foco vuelve al botón), al hacer clic fuera, al salir el foco o al navegar.
 * - Móvil: panel lateral #hl-sidebar con los grupos en acordeón; lo abre el botón de menú de la barra superior.
 * Ambas variantes muestran el menú del espacio de trabajo activo (portal de clientes o backoffice, WorkspaceService) y
 * solo lo que el perfil puede usar (mismas reglas que las rutas protegidas).
 */
@Component({
  selector: 'app-main-nav',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, RouterLink, TranslocoPipe, DisputeLinkComponent, WorkspaceSwitcherComponent],
  templateUrl: './main-nav.html',
  styleUrl: './main-nav.scss',
  host: {
    '(document:keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class MainNavComponent {
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly features = inject(FeatureService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly workspaces = inject(WorkspaceService);

  /** Panel lateral móvil abierto (lo controla el botón de menú de la barra superior). */
  readonly mobileOpen = input(false);
  readonly closed = output<void>();

  /** Grupo con el panel abierto en escritorio. */
  readonly openGroup = signal<string | null>(null);
  /** Grupos expandidos en el acordeón móvil. */
  readonly expanded = signal<ReadonlySet<string>>(new Set());

  private hoverTimer?: ReturnType<typeof setTimeout>;
  private readonly canHover = typeof matchMedia === 'function' && matchMedia('(hover: hover) and (pointer: fine)').matches;

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  private readonly context = computed<MenuContext>(() => ({
    internal: this.auth.isInternal(),
    admin: this.auth.isAdmin(),
    cartEnabled: this.cart.cartEnabled(),
    accountPaymentsEnabled: this.cart.accountPaymentsEnabled(),
    bolivia: this.auth.getCountry() === 'BO' || this.auth.operatingCountries().includes('BO'),
    can: (...keys) => this.auth.hasPermission(...keys.map((k) => PERMISSIONS[k])),
    feature: (...names) => this.features.enabled(...names),
  }));

  /** Grupos y columnas del espacio activo con al menos un destino visible para el perfil. */
  readonly groups = computed<MenuGroup[]>(() => {
    const c = this.context();
    return MENUS[this.workspaces.current()].map((g) => ({
      ...g,
      columns: g.columns
        .map((col) => ({ ...col, links: col.links.filter((l) => l.kind === 'dispute' || !l.visible || l.visible(c)) }))
        .filter((col) => col.links.length > 0),
    })).filter((g) => !!g.route || g.columns.length > 0);
  });

  constructor() {
    // Al navegar se cierra el panel abierto y el acordeón móvil muestra el grupo de la página actual.
    effect(() => {
      this.url();
      this.openGroup.set(null);
      const active = this.groups().find((g) => this.isGroupActive(g));
      if (active) this.expanded.update((set) => new Set([...set, active.id]));
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(this.hoverTimer));
  }

  asRoute(link: MenuLink): RouteLink | null {
    return link.kind === 'route' ? link : null;
  }

  isLinkActive(link: RouteLink): boolean {
    const path = this.url().split(/[?#]/)[0];
    return path === link.route || (!link.exact && path.startsWith(link.route + '/'));
  }

  isGroupActive(group: MenuGroup): boolean {
    if (group.route) return this.isLinkActive({ kind: 'route', key: '', route: group.route, icon: [] });
    return group.columns.some((c) => c.links.some((l) => l.kind === 'route' && this.isLinkActive(l)));
  }

  // --- Escritorio

  toggle(group: MenuGroup): void {
    clearTimeout(this.hoverTimer);
    this.openGroup.update((id) => (id === group.id ? null : group.id));
  }

  hoverOpen(group: MenuGroup): void {
    if (!this.canHover) return;
    clearTimeout(this.hoverTimer);
    if (group.route) {
      this.hoverTimer = setTimeout(() => this.openGroup.set(null), HOVER_CLOSE_MS);
      return;
    }
    // Con un panel ya abierto, el cambio entre grupos es inmediato.
    const delay = this.openGroup() ? 0 : HOVER_OPEN_MS;
    this.hoverTimer = setTimeout(() => this.openGroup.set(group.id), delay);
  }

  hoverClose(): void {
    if (!this.canHover) return;
    clearTimeout(this.hoverTimer);
    this.hoverTimer = setTimeout(() => this.openGroup.set(null), HOVER_CLOSE_MS);
  }

  keepOpen(): void {
    clearTimeout(this.hoverTimer);
  }

  /** El foco salió del grupo (Tab más allá del último enlace): se cierra su panel. */
  onGroupFocusOut(event: FocusEvent, group: MenuGroup): void {
    const next = event.relatedTarget as Node | null;
    const item = event.currentTarget as HTMLElement;
    if (this.openGroup() === group.id && next && !item.contains(next)) this.openGroup.set(null);
  }

  onEscape(): void {
    const open = this.openGroup();
    if (open) {
      this.openGroup.set(null);
      this.host.nativeElement.querySelector<HTMLElement>(`#hl-menu-trigger-${open}`)?.focus();
      return;
    }
    if (this.mobileOpen()) this.closed.emit();
  }

  onDocumentClick(event: MouseEvent): void {
    if (this.openGroup() && !this.host.nativeElement.querySelector('.hl-mainnav')?.contains(event.target as Node)) {
      this.openGroup.set(null);
    }
  }

  // --- Móvil

  toggleSection(group: MenuGroup): void {
    this.expanded.update((set) => {
      const next = new Set(set);
      if (next.has(group.id)) next.delete(group.id);
      else next.add(group.id);
      return next;
    });
  }

  onMobileLink(): void {
    this.closed.emit();
  }

  closeMobile(): void {
    this.closed.emit();
  }
}
