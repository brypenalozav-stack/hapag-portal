import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, Injector, NgZone, afterNextRender, computed, effect, inject, input, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { scrollToHeading, stickyOffset } from '../../scroll-to-heading';

interface SectionLink {
  id: string;
  /** Texto leído del título (listado automático) o clave de traducción (entradas explícitas). */
  label?: string;
  labelKey?: string;
}

/** Entrada explícita del índice: id del título al que lleva y clave de su texto. */
export interface SectionNavItem {
  id: string;
  labelKey: string;
}

/**
 * "Ir a" de una página larga: barra con un enlace por sección. Sin `items`, lista cada `h2[id]` del contenedor
 * (incluidas las que aparecen después, como paneles que cargan solos); con `items`, solo esas entradas (p. ej. los
 * grupos del detalle del BL). Marca la sección visible con aria-current y, al elegir una, desplaza la página bajo las
 * barras fijas y lleva el foco a su título (WCAG 2.4.1, 2.4.3). `embedded` la deja dentro de otro encabezado fijo.
 */
@Component({
  selector: 'app-section-nav',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  template: `
    @if (links().length > minLinks()) {
      <nav class="hl-section-nav" [class.hl-section-nav--embedded]="embedded()" [attr.aria-label]="'shared.sectionNav.label' | transloco" data-testid="section-nav">
        <span class="hl-section-nav__title" aria-hidden="true">{{ 'shared.sectionNav.title' | transloco }}</span>
        <ul>
          @for (l of links(); track l.id) {
            <li>
              <a [href]="'#' + l.id" [class.is-active]="active() === l.id" [attr.aria-current]="active() === l.id ? 'location' : null"
                 (click)="go($event, l.id)">{{ l.labelKey ? (l.labelKey | transloco) : l.label }}</a>
            </li>
          }
        </ul>
      </nav>
    }
  `,
  styles: `
    .hl-section-nav {
      position: sticky;
      top: var(--navbar-height);
      z-index: 1010;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin: 0 -1.5rem 1.25rem;
      padding: 0.5rem 1.5rem;
      border-bottom: 1px solid var(--hl-border);
      background-color: var(--bs-body-bg);
      box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
    }

    .hl-section-nav--embedded {
      position: static;
      margin: 0;
      padding: 0.4rem 0 0;
      border-bottom: 0;
      background-color: transparent;
      box-shadow: none;
    }

    .hl-section-nav__title {
      flex-shrink: 0;
      color: var(--hl-text-muted);
      font-size: 0.75rem;
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
    }

    ul {
      display: flex;
      gap: 0.25rem;
      margin: 0;
      padding: 0;
      overflow-x: auto;
      list-style: none;
      scrollbar-width: thin;
    }

    a {
      display: inline-block;
      padding: 0.35rem 0.7rem;
      border-radius: 50rem;
      color: var(--bs-body-color);
      font-size: 0.85rem;
      font-weight: 500;
      text-decoration: none;
      white-space: nowrap;

      &:hover {
        background-color: var(--hl-surface-muted);
        text-decoration: underline;
      }

      &.is-active {
        background-color: var(--hl-dark);
        color: var(--hl-white);
      }
    }
  `,
})
export class SectionNavComponent {
  /** Contenedor cuyas secciones se listan (por defecto, el contenido principal). */
  readonly container = input<HTMLElement | null>(null);
  /** Entradas explícitas (en orden); sin ellas se listan los `h2[id]` del contenedor. */
  readonly items = input<SectionNavItem[] | null>(null);
  /** Dentro de un encabezado fijo: la barra no es fija por sí misma. */
  readonly embedded = input(false);

  private readonly injector = inject(Injector);
  private readonly scanned = signal<SectionLink[]>([]);
  readonly links = computed<SectionLink[]>(() => this.items() ?? this.scanned());
  /** Con entradas explícitas basta con dos; el listado automático aparece desde tres secciones. */
  readonly minLinks = computed(() => (this.items() ? 1 : 2));
  readonly active = signal<string | null>(null);

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  /** Títulos cuya posición decide la sección actual. */
  private headings: HTMLElement[] = [];
  /** Tras elegir una sección, la marca se mantiene mientras la página se acomoda. */
  private lockedUntil = 0;
  private frame = 0;

  constructor() {
    const destroyRef = inject(DestroyRef);
    const zone = inject(NgZone);
    afterNextRender(() => {
      const onScroll = () => {
        if (this.frame) return;
        this.frame = requestAnimationFrame(() => {
          this.frame = 0;
          const id = this.current();
          if (id !== this.active()) zone.run(() => this.active.set(id));
        });
      };
      zone.runOutsideAngular(() => window.addEventListener('scroll', onScroll, { passive: true }));
      destroyRef.onDestroy(() => {
        window.removeEventListener('scroll', onScroll);
        cancelAnimationFrame(this.frame);
      });
    });
    // Con entradas explícitas se siguen sus títulos (ya están en la página: lo diferido es el contenido de cada grupo).
    effect(() => {
      const items = this.items();
      if (items) {
        afterNextRender(() => {
          this.headings = items.map((i) => document.getElementById(i.id)).filter((h): h is HTMLElement => !!h);
          this.active.set(this.current());
        }, { injector: this.injector });
      }
    });
    // El enlace de la sección actual queda a la vista en la barra (se desplaza en horizontal en pantallas chicas).
    effect(() => {
      if (!this.active()) return;
      afterNextRender(() => {
        const link = this.host.nativeElement.querySelector<HTMLElement>('a.is-active');
        const list = link?.closest('ul');
        if (link && list) list.scrollTo({ left: link.offsetLeft - list.offsetLeft - (list.clientWidth - link.offsetWidth) / 2 });
      }, { injector: this.injector });
    });
    afterNextRender(() => {
      if (this.items()) return;
      const root = this.container() ?? document.getElementById('contenido-principal');
      if (!root) return;
      const scan = () => this.scan(root);
      scan();
      // Paneles que cargan después y cambios de idioma (el texto de los títulos cambia).
      const mutations = new MutationObserver(scan);
      mutations.observe(root, { childList: true, subtree: true, characterData: true });
      destroyRef.onDestroy(() => mutations.disconnect());
    });
  }

  go(event: Event, id: string): void {
    event.preventDefault();
    if (scrollToHeading(id)) {
      this.lockedUntil = performance.now() + 1700;
      this.active.set(id);
    }
  }

  /** Sección actual: el último título que ya pasó bajo las barras fijas (o el primero, arriba de todo). */
  private current(): string | null {
    if (performance.now() < this.lockedUntil) return this.active();
    const limit = stickyOffset() + 48;
    let id: string | null = this.headings[0]?.id ?? null;
    for (const h of this.headings) {
      if (h.getBoundingClientRect().top <= limit) id = h.id;
      else break;
    }
    return id;
  }

  private scan(root: HTMLElement): void {
    const headings = [...root.querySelectorAll<HTMLElement>('h2[id]')].filter((h) => !h.closest('dialog, .hl-assistant'));
    const next = headings.map((h) => ({ id: h.id, label: (h.textContent ?? '').replace(/\s+/g, ' ').trim() })).filter((l) => l.label);
    const current = this.scanned();
    if (next.length === current.length && next.every((l, i) => l.id === current[i].id && l.label === current[i].label)) return;
    this.scanned.set(next);
    this.headings = headings;
  }
}
