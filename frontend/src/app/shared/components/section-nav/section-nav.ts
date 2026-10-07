import { ChangeDetectionStrategy, Component, DestroyRef, afterNextRender, inject, input, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

interface SectionLink {
  id: string;
  label: string;
}

/**
 * "Ir a" de una página larga: barra fija bajo el encabezado con un enlace por sección (cada `h2[id]` del contenedor,
 * incluidas las que aparecen después, como paneles que cargan solos). Marca la sección visible con aria-current y,
 * al elegir una, desplaza la página y lleva el foco a su título (WCAG 2.4.1, 2.4.3).
 */
@Component({
  selector: 'app-section-nav',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  template: `
    @if (links().length > 2) {
      <nav class="hl-section-nav" [attr.aria-label]="'shared.sectionNav.label' | transloco" data-testid="section-nav">
        <span class="hl-section-nav__title" aria-hidden="true">{{ 'shared.sectionNav.title' | transloco }}</span>
        <ul>
          @for (l of links(); track l.id) {
            <li>
              <a [href]="'#' + l.id" [class.is-active]="active() === l.id" [attr.aria-current]="active() === l.id ? 'location' : null"
                 (click)="go($event, l.id)">{{ l.label }}</a>
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

  readonly links = signal<SectionLink[]>([]);
  readonly active = signal<string | null>(null);

  private observer?: IntersectionObserver;

  constructor() {
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      const root = this.container() ?? document.getElementById('contenido-principal');
      if (!root) return;
      const scan = () => this.scan(root);
      scan();
      // Paneles que cargan después y cambios de idioma (el texto de los títulos cambia).
      const mutations = new MutationObserver(scan);
      mutations.observe(root, { childList: true, subtree: true, characterData: true });
      destroyRef.onDestroy(() => {
        mutations.disconnect();
        this.observer?.disconnect();
      });
    });
  }

  go(event: Event, id: string): void {
    event.preventDefault();
    const heading = document.getElementById(id);
    if (!heading) return;
    if (!heading.hasAttribute('tabindex')) heading.setAttribute('tabindex', '-1');
    const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
    heading.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' });
    heading.focus({ preventScroll: true });
    this.active.set(id);
  }

  private scan(root: HTMLElement): void {
    const headings = [...root.querySelectorAll<HTMLElement>('h2[id]')].filter((h) => !h.closest('dialog, .hl-assistant'));
    const next = headings.map((h) => ({ id: h.id, label: (h.textContent ?? '').replace(/\s+/g, ' ').trim() })).filter((l) => l.label);
    const current = this.links();
    if (next.length === current.length && next.every((l, i) => l.id === current[i].id && l.label === current[i].label)) return;
    this.links.set(next);

    this.observer?.disconnect();
    this.observer = new IntersectionObserver(
      (entries) => {
        const visible = entries.filter((e) => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible[0]) this.active.set(visible[0].target.id);
      },
      { rootMargin: '-120px 0px -60% 0px' },
    );
    headings.forEach((h) => this.observer!.observe(h));
  }
}
