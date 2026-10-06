import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

/** Tamaños de página ofrecidos por defecto; el backend acepta hasta 100. */
export const PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50, 100];

type PageSlot = number | 'gap';

/**
 * Paginación común de las tablas: rango visible ("21–40 de 345"), filas por página, primera/anterior,
 * números con elipsis, siguiente/última. La página actual lleva aria-current="page".
 * Es controlado: no guarda estado, emite `pageChange` y `pageSizeChange` y el padre vuelve a consultar.
 */
@Component({
  selector: 'app-paginator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  templateUrl: './paginator.html',
  styleUrl: './paginator.scss',
})
export class PaginatorComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  /** Nombre accesible del <nav> (ya traducido). */
  readonly label = input.required<string>();
  /** Opciones de filas por página; vacío oculta el selector. */
  readonly pageSizeOptions = input<readonly number[]>(PAGE_SIZE_OPTIONS);
  /** Prefijo de ids y data-testid cuando hay más de un paginador en la pantalla. */
  readonly idPrefix = input('pager');

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / Math.max(1, this.pageSize()))));
  readonly from = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  readonly to = computed(() => Math.min(this.total(), this.page() * this.pageSize()));

  /** Hasta 7 posiciones: siempre la primera y la última, la actual con sus vecinas y elipsis entre medio. */
  readonly slots = computed<PageSlot[]>(() => {
    const pages = this.pages();
    const current = Math.min(this.page(), pages);
    if (pages <= 7) return Array.from({ length: pages }, (_, i) => i + 1);
    const start = Math.max(2, Math.min(current - 1, pages - 4));
    const end = Math.min(pages - 1, Math.max(current + 1, 5));
    const slots: PageSlot[] = [1];
    if (start > 2) slots.push('gap');
    for (let p = start; p <= end; p++) slots.push(p);
    if (end < pages - 1) slots.push('gap');
    slots.push(pages);
    return slots;
  });

  go(page: number): void {
    if (page < 1 || page > this.pages() || page === this.page()) return;
    this.pageChange.emit(page);
  }

  onPageSize(event: Event): void {
    const size = Number((event.target as HTMLSelectElement).value);
    if (Number.isInteger(size) && size > 0 && size !== this.pageSize()) this.pageSizeChange.emit(size);
  }
}
