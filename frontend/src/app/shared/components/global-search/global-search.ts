import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, Injector, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable, map, of, switchMap } from 'rxjs';
import { DemurrageService } from '../../../core/services/demurrage.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ShipmentService } from '../../../core/services/shipment.service';

/** Número de contenedor ISO 6346 (4 letras y 7 dígitos). HLCU es el prefijo de los BL de Hapag-Lloyd, no de contenedores. */
const CONTAINER = /^[A-Z]{3}[UJZ]\d{7}$/;
const BL_PREFIX = 'HLCU';

type SearchMessage = 'invalid' | 'notFound' | 'error';

/** Destino de la búsqueda: el detalle de un BL o el listado de embarques filtrado. */
type Target = { bl: string } | { list: 'blNumber' | 'bookingNumber' } | null;

const MESSAGES: Record<SearchMessage, string> = {
  invalid: 'shared.search.invalid',
  notFound: 'shared.search.notFound',
  error: 'shared.search.error',
};

/**
 * Búsqueda universal de la barra superior: un número de BL, booking o contenedor lleva al detalle del embarque (o al
 * listado filtrado si hay varios resultados). Reutiliza el listado de embarques (GET /shipments, que ya filtra por los
 * accesos del usuario) y, para contenedores, GET /demurrage/container/{n}. En pantallas angostas es un botón que
 * despliega el campo; la tecla "/" lleva el foco al campo desde cualquier parte de la página.
 */
@Component({
  selector: 'app-global-search',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  templateUrl: './global-search.html',
  styles: [':host { display: contents; }'],
  host: {
    '(document:keydown)': 'onShortcut($event)',
  },
})
export class GlobalSearchComponent {
  private readonly shipments = inject(ShipmentService);
  private readonly demurrage = inject(DemurrageService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly input = viewChild.required<ElementRef<HTMLInputElement>>('field');
  private readonly toggleButton = viewChild.required<ElementRef<HTMLButtonElement>>('toggleBtn');

  /** Panel desplegado en pantallas angostas. */
  readonly open = signal(false);
  readonly searching = signal(false);
  readonly message = signal<string | null>(null);
  readonly term = signal('');
  readonly messageKeys = MESSAGES;

  toggle(): void {
    this.open.update((v) => !v);
    if (this.open()) this.focusAfterRender();
  }

  onInput(event: Event): void {
    this.term.set((event.target as HTMLInputElement).value);
    this.message.set(null);
  }

  /** Esc dentro del campo: limpia el aviso y, en pantallas angostas, cierra el panel y devuelve el foco al botón. */
  onEscape(event: Event): void {
    this.message.set(null);
    if (!this.open()) return;
    event.stopPropagation();
    this.open.set(false);
    this.toggleButton().nativeElement.focus();
  }

  /** Tecla "/" fuera de un campo de texto: enfoca la búsqueda. */
  onShortcut(event: KeyboardEvent): void {
    if (event.key !== '/' || event.ctrlKey || event.metaKey || event.altKey || event.defaultPrevented) return;
    const target = event.target as HTMLElement | null;
    if (target?.closest('input, textarea, select, [contenteditable=""], [contenteditable="true"]')) return;
    event.preventDefault();
    if (this.isVisible()) {
      this.input().nativeElement.focus();
      return;
    }
    this.open.set(true);
    this.focusAfterRender();
  }

  submit(event: Event): void {
    event.preventDefault();
    const value = this.term().replace(/\s+/g, '').toUpperCase();
    if (value.length < 4) {
      this.showMessage(MESSAGES.invalid);
      this.input().nativeElement.focus();
      return;
    }
    this.searching.set(true);
    this.message.set(null);
    this.resolve(value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (target) => {
          this.searching.set(false);
          if (!target) {
            this.showMessage(MESSAGES.notFound);
            return;
          }
          this.term.set('');
          this.open.set(false);
          if ('bl' in target) this.router.navigate(['/shipments', target.bl]);
          else this.router.navigate(['/shipments'], { queryParams: { operation: 'ALL', [target.list]: value } });
        },
        error: () => {
          this.searching.set(false);
          this.showMessage(MESSAGES.error);
        },
      });
  }

  private resolve(value: string): Observable<Target> {
    if (CONTAINER.test(value) && !value.startsWith(BL_PREFIX)) {
      return this.demurrage.getBlsByContainer(value).pipe(map((bls) => (bls.length ? { bl: bls[0] } : null)));
    }
    return this.find('blNumber', value).pipe(
      switchMap((byBl) => (byBl ? of(byBl) : this.find('bookingNumber', value))),
      // Un BL de Hapag-Lloyd que no está en el listado puede abrirse por acceso abierto (M1-17): el detalle lo valida.
      map((target) => target ?? (value.startsWith(BL_PREFIX) ? { bl: value } : null)),
    );
  }

  /** Un resultado (o una coincidencia exacta) lleva al detalle; varios, al listado filtrado; ninguno, a null. */
  private find(field: 'blNumber' | 'bookingNumber', value: string): Observable<Target> {
    return this.shipments.search({ [field]: value, page: 1, pageSize: 2 }).pipe(
      map((result) => {
        const exact = result.items.find((s) => (field === 'blNumber' ? s.blNumber : s.bookingNumber)?.toUpperCase() === value);
        if (exact && (result.total === 1 || field === 'blNumber')) return { bl: exact.blNumber };
        if (result.total === 1 && result.items[0]) return { bl: result.items[0].blNumber };
        return result.total > 1 ? { list: field } : null;
      }),
    );
  }

  /** Muestra el aviso bajo el campo y lo anuncia en la región viva global (LiveAnnouncerService). */
  private showMessage(key: string): void {
    this.message.set(key);
    this.announcer.announce(translate(key));
  }

  /** Enfoca el campo cuando ya está visible (tras desplegar el panel en pantallas angostas). */
  private focusAfterRender(): void {
    afterNextRender(() => this.input().nativeElement.focus(), { injector: this.injector });
  }

  /** El campo se ve sin desplegar el panel (pantallas medianas y grandes, ≥ 768 px como en styles.scss). */
  private isVisible(): boolean {
    return typeof matchMedia !== 'function' || matchMedia('(min-width: 768px)').matches;
  }
}
