import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Subscription } from 'rxjs';
import { ShipmentDetail, ShipmentListItem } from '../../core/models/shipment.model';
import { ShipmentService } from '../../core/services/shipment.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { ReleaseLetterFormComponent } from '../documents/release-letter/release-letter-form';
import { StateMessageComponent } from '../../shared/components/state-message/state-message';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';

/** Huso de la operación de Bolivia (NF-22). */
const BOLIVIA_TIME_ZONE = 'America/La_Paz';
/** BL de importación de Bolivia que se listan en el selector (el servidor filtra por los accesos, M1-11). */
const PAGE_SIZE = 100;
/** Con más BL que estos se muestra el campo de búsqueda sobre el selector. */
const FILTER_THRESHOLD = 6;

const LOAD_STATE = { LOADING: 'loading', READY: 'ready', ERROR: 'error' } as const;
type LoadState = (typeof LOAD_STATE)[keyof typeof LOAD_STATE];

/** Trazos de los íconos de Bootstrap Icons (16×16) que usa la página. */
const ICONS = {
  empty: [
    'M8.186 1.113a.5.5 0 0 0-.372 0L1.846 3.5l2.404.961L10.404 2zm3.564 1.426L5.596 5 8 5.961 14.154 3.5zm3.25 1.7-6.5 2.6v7.922l6.5-2.6V4.24zM7.5 14.762V6.838L1 4.239v7.923zM7.443.184a1.5 1.5 0 0 1 1.114 0l7.129 2.852A.5.5 0 0 1 16 3.5v8.662a1 1 0 0 1-.629.928l-7.185 2.874a.5.5 0 0 1-.372 0L.63 13.09a1 1 0 0 1-.63-.928V3.5a.5.5 0 0 1 .314-.464z',
  ],
  search: [
    'M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001q.044.06.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1 1 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0',
  ],
  pointer: [
    'M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14m0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16',
    'M8 4a.5.5 0 0 1 .5.5v5.793l2.146-2.147a.5.5 0 0 1 .708.708l-3 3a.5.5 0 0 1-.708 0l-3-3a.5.5 0 1 1 .708-.708L7.5 10.293V4.5A.5.5 0 0 1 8 4',
  ],
} as const;

/**
 * Carta de liberación y desconsolidado (M6-08, Bolivia importación) en una sola página: el cliente elige uno de sus BL
 * de importación de Bolivia, ve su información (nave de arribo, puerto, consignatario y contenedores) y solicita la
 * carta con el mismo formulario del BL (`app-release-letter-form`, sin su encabezado). El BL elegido viaja en `?bl=`
 * para enlazar y volver a la misma selección.
 */
@Component({
  selector: 'app-release-letter-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe, HlDatePipe, StateMessageComponent, ReleaseLetterFormComponent],
  templateUrl: './release-letter-page.html',
  styleUrl: './release-letter-page.scss',
})
export class ReleaseLetterPageComponent {
  private readonly shipments = inject(ShipmentService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** BL de la URL (`?bl=`), enlazado por el router (withComponentInputBinding). */
  readonly bl = input<string | undefined>(undefined);

  readonly icons = ICONS;
  readonly timeZone = BOLIVIA_TIME_ZONE;

  readonly listState = signal<LoadState>(LOAD_STATE.LOADING);
  readonly items = signal<ShipmentListItem[]>([]);
  readonly query = signal('');

  readonly detail = signal<ShipmentDetail | null>(null);
  readonly detailState = signal<LoadState>(LOAD_STATE.LOADING);
  private detailRequest?: Subscription;

  /** BL elegido, en mayúsculas; null sin selección. */
  readonly selected = computed(() => this.bl()?.trim().toUpperCase() || null);

  readonly showFilter = computed(() => this.items().length > FILTER_THRESHOLD);

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.items();
    return this.items().filter((s) =>
      [s.blNumber, s.vessel, s.voyage, s.bookingNumber].some((v) => (v ?? '').toLowerCase().includes(q)),
    );
  });

  /** Opciones del selector: las que coinciden con la búsqueda y, siempre, la elegida. */
  readonly options = computed(() => {
    const list = this.filtered();
    const sel = this.selected();
    if (!sel || list.some((s) => s.blNumber === sel)) return list;
    const chosen = this.items().find((s) => s.blNumber === sel);
    return chosen ? [chosen, ...list] : list;
  });

  /** Fila del listado del BL elegido (datos de respaldo si el detalle no responde). */
  readonly selectedItem = computed(() => this.items().find((s) => s.blNumber === this.selected()) ?? null);

  constructor() {
    this.loadList();
    // Cada BL elegido carga su información; el formulario se crea de nuevo para ese BL.
    effect(() => {
      const bl = this.selected();
      untracked(() => this.loadDetail(bl));
    });
  }

  loadList(): void {
    this.listState.set(LOAD_STATE.LOADING);
    this.shipments
      .search({ operation: 'IMPORT', country: 'BO', page: 1, pageSize: PAGE_SIZE })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const items = (result?.items ?? []).filter((s) => s.operation === 'IMPORT' && s.country === 'BO');
          this.items.set(items);
          this.listState.set(LOAD_STATE.READY);
          // Con un solo BL queda elegido (la URL lo refleja sin agregar una entrada al historial).
          if (!this.selected() && items.length === 1) this.select(items[0].blNumber, true);
        },
        error: () => this.listState.set(LOAD_STATE.ERROR),
      });
  }

  private loadDetail(bl: string | null): void {
    this.detailRequest?.unsubscribe();
    this.detail.set(null);
    if (!bl) return;
    this.detailState.set(LOAD_STATE.LOADING);
    this.detailRequest = this.shipments
      .getByBl(bl)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.detail.set(detail);
          this.detailState.set(LOAD_STATE.READY);
        },
        error: () => this.detailState.set(LOAD_STATE.ERROR),
      });
  }

  optionLabel(s: ShipmentListItem): string {
    const vessel = [s.vessel, s.voyage].filter(Boolean).join(' / ');
    return vessel ? translate('releaseLetterPage.select.option', { bl: s.blNumber, vessel }) : s.blNumber;
  }

  onQuery(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  onSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value && value !== this.selected()) this.select(value, false);
  }

  private select(bl: string, replaceUrl: boolean): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { bl }, queryParamsHandling: 'merge', replaceUrl });
    this.announcer.announce(translate('releaseLetterPage.select.announce', { bl }));
  }
}
