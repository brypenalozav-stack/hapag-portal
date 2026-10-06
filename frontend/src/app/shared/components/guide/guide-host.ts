import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  OnInit,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { filter } from 'rxjs';
import { GuideService } from '../../../core/services/guide.service';
import { LocaleService } from '../../../core/services/locale.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { Guide, GuideStep } from '../../../core/models/guide.model';

/** Atributo que marca en la interfaz el elemento que señala un paso (`elementKey` del paso). */
const GUIDE_ATTRIBUTE = 'data-guide-key';
/** Intentos (cada 100 ms) para encontrar el elemento después de navegar o de que la pantalla cargue sus datos. */
const FIND_ATTEMPTS = 30;
const FIND_INTERVAL_MS = 100;
/** Alto estimado del recuadro del paso, para decidir si va debajo o encima del elemento. */
const POPOVER_HEIGHT = 260;
const POPOVER_MAX_WIDTH = 360;
const MARGIN = 12;

interface Rect {
  top: number;
  left: number;
  width: number;
  height: number;
}

interface PopoverPosition {
  top: number | null;
  bottom: number | null;
  left: number;
  width: number;
}

/**
 * Modo guía del portal (Fase 2, Ola I, M1-27), sin librerías: lee las guías activas de la pantalla, ofrece la nueva o
 * cambiada y la vuelve a lanzar desde el botón "Guía". Cada paso señala el elemento con el atributo `data-guide-key`
 * (navega a la ruta del paso si hace falta) y muestra el recuadro en un `<dialog>` modal: el foco queda atrapado en el
 * recuadro, Escape cierra la guía y el foco vuelve al botón. Respeta la reducción de movimiento. Completar o cerrar la
 * guía se guarda en el servidor; la guía se puede desactivar en cualquier momento.
 */
@Component({
  selector: 'app-guide-host',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './guide-host.html',
  styleUrl: './guide-host.scss',
  host: {
    '(window:resize)': 'reposition()',
    '(window:scroll)': 'reposition()',
  },
})
export class GuideHostComponent implements OnInit {
  private readonly service = inject(GuideService);
  private readonly router = inject(Router);
  private readonly locale = inject(LocaleService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Guías activas de la pantalla actual. */
  guides = signal<Guide[]>([]);
  /** Guía nueva o cambiada que se ofrece (estado null en el servidor), hasta que se inicie o se descarte. */
  offered = computed(() => this.guides().find((g) => !g.status && !this.declined().has(g.code)) ?? null);
  private readonly declined = signal(new Set<string>());

  active = signal<Guide | null>(null);
  index = signal(0);
  target = signal<Rect | null>(null);
  searching = signal(false);
  private currentPath = '';
  private targetElement: HTMLElement | null = null;
  private findTimer: ReturnType<typeof setTimeout> | undefined;

  steps = computed(() => [...(this.active()?.steps ?? [])].sort((a, b) => a.order - b.order));
  step = computed<GuideStep | null>(() => this.steps()[this.index()] ?? null);
  isLast = computed(() => this.index() >= this.steps().length - 1);
  position = computed<PopoverPosition>(() => this.computePosition(this.target()));

  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');
  private readonly stepTitle = viewChild<ElementRef<HTMLElement>>('stepTitle');
  private readonly launcher = viewChild<ElementRef<HTMLButtonElement>>('launcher');

  ngOnInit(): void {
    this.onRoute(this.router.url);
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((e) => this.onRoute(e.urlAfterRedirects));
    this.destroyRef.onDestroy(() => clearTimeout(this.findTimer));
  }

  name(g: Guide): string {
    return this.locale.lang() === 'en' ? g.nameEn : g.nameEs;
  }

  description(g: Guide): string {
    return (this.locale.lang() === 'en' ? g.descriptionEn : g.descriptionEs) ?? '';
  }

  stepTitleText(s: GuideStep): string {
    return this.locale.lang() === 'en' ? s.titleEn : s.titleEs;
  }

  stepText(s: GuideStep): string {
    return this.locale.lang() === 'en' ? s.textEn : s.textEs;
  }

  start(guide: Guide): void {
    if (guide.steps.length === 0) return;
    this.active.set(guide);
    this.index.set(0);
    this.showStep();
  }

  /** "Ahora no": la guía ofrecida no se vuelve a ofrecer (se puede lanzar desde el botón Guía). */
  decline(guide: Guide): void {
    this.declined.update((s) => new Set(s).add(guide.code));
    this.saveState(guide, 'Dismissed', undefined);
  }

  next(): void {
    if (this.isLast()) {
      this.finish();
      return;
    }
    this.index.update((i) => i + 1);
    this.showStep();
  }

  back(): void {
    if (this.index() === 0) return;
    this.index.update((i) => i - 1);
    this.showStep();
  }

  /** Escape (evento `cancel` del diálogo) o el botón Cerrar: la guía se desactiva y queda como descartada. */
  onCancel(event: Event): void {
    event.preventDefault();
    this.close();
  }

  close(): void {
    const guide = this.active();
    if (!guide) return;
    this.saveState(guide, 'Dismissed', this.step()?.order);
    this.announcer.announce(translate('shared.guide.closed', { name: this.name(guide) }));
    this.teardown();
  }

  private finish(): void {
    const guide = this.active();
    if (!guide) return;
    this.saveState(guide, 'Completed', this.step()?.order);
    this.announcer.announce(translate('shared.guide.completed', { name: this.name(guide) }));
    this.teardown();
  }

  /** Recalcula la posición del recuadro (scroll, cambio de tamaño). */
  reposition(): void {
    if (!this.active() || !this.targetElement) return;
    this.target.set(this.rectOf(this.targetElement));
  }

  private onRoute(url: string): void {
    const path = url.split(/[?#]/)[0] || '/';
    if (path === this.currentPath) return;
    this.currentPath = path;
    // Una guía en curso puede cambiar de pantalla entre pasos: no se recargan las guías mientras dura.
    if (this.active()) return;
    this.service.getForRoute(path).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (guides) => this.guides.set(guides.filter((g) => g.steps.length > 0)),
      error: () => this.guides.set([]),
    });
  }

  private showStep(): void {
    const step = this.step();
    if (!step) return;
    clearTimeout(this.findTimer);
    this.targetElement = null;
    this.target.set(null);
    this.searching.set(true);
    const path = this.router.url.split(/[?#]/)[0];
    if (step.route && step.route !== path) {
      this.router.navigateByUrl(step.route).then(() => this.findTarget(step, 0));
    } else {
      this.findTarget(step, 0);
    }
    this.openDialog();
  }

  private findTarget(step: GuideStep, attempt: number): void {
    const element = this.findVisible(step.elementKey);
    if (element) {
      this.searching.set(false);
      this.targetElement = element;
      element.scrollIntoView({ block: 'center', behavior: this.reducedMotion() ? 'auto' : 'smooth' });
      this.target.set(this.rectOf(element));
      // Tras el desplazamiento suave, la posición final.
      this.findTimer = setTimeout(() => this.reposition(), this.reducedMotion() ? 0 : 400);
      return;
    }
    if (attempt >= FIND_ATTEMPTS) {
      // El elemento no está en la pantalla (sin datos, sin permiso): el paso se muestra igual, centrado.
      this.searching.set(false);
      return;
    }
    this.findTimer = setTimeout(() => this.findTarget(step, attempt + 1), FIND_INTERVAL_MS);
  }

  /** Primer elemento visible con la clave (un mismo elemento puede repetirse, p. ej. por moneda en el carro). */
  private findVisible(key: string): HTMLElement | null {
    const all = Array.from(document.querySelectorAll<HTMLElement>(`[${GUIDE_ATTRIBUTE}]`))
      .filter((el) => el.getAttribute(GUIDE_ATTRIBUTE) === key);
    return all.find((el) => el.getClientRects().length > 0) ?? null;
  }

  private openDialog(): void {
    afterNextRender({
      write: () => {
        const dialog = this.dialog()?.nativeElement;
        if (dialog && !dialog.open) dialog.showModal();
        this.stepTitle()?.nativeElement.focus();
      },
    }, { injector: this.injector });
  }

  private teardown(): void {
    clearTimeout(this.findTimer);
    this.dialog()?.nativeElement.close();
    this.active.set(null);
    this.target.set(null);
    this.targetElement = null;
    this.searching.set(false);
    // Las guías se vuelven a leer (estado guardado) para la pantalla en la que terminó.
    this.currentPath = '';
    this.onRoute(this.router.url);
    afterNextRender({ read: () => this.launcher()?.nativeElement.focus() }, { injector: this.injector });
  }

  private saveState(guide: Guide, status: 'Completed' | 'Dismissed', lastStep: number | undefined): void {
    this.service.saveState(guide.code, status, lastStep).subscribe({
      next: (saved) => this.guides.update((list) => list.map((g) => (g.code === saved.code ? { ...g, ...saved } : g))),
      error: () => { /* el avance no guardado solo hace que la guía se vuelva a ofrecer */ },
    });
  }

  private rectOf(element: HTMLElement): Rect {
    const r = element.getBoundingClientRect();
    return { top: r.top, left: r.left, width: r.width, height: r.height };
  }

  private computePosition(rect: Rect | null): PopoverPosition {
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const width = Math.min(POPOVER_MAX_WIDTH, vw - 2 * MARGIN);
    if (!rect) {
      return { top: Math.max(MARGIN, (vh - POPOVER_HEIGHT) / 2), bottom: null, left: (vw - width) / 2, width };
    }
    const left = Math.min(Math.max(MARGIN, rect.left), vw - width - MARGIN);
    const below = rect.top + rect.height + MARGIN;
    if (below + POPOVER_HEIGHT <= vh) return { top: below, bottom: null, left, width };
    if (rect.top - MARGIN - POPOVER_HEIGHT >= 0) return { top: null, bottom: vh - rect.top + MARGIN, left, width };
    return { top: null, bottom: MARGIN, left, width };
  }

  private reducedMotion(): boolean {
    return typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
