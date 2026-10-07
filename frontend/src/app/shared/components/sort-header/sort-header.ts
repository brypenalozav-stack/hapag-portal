import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type SortDirection = 'asc' | 'desc';

/** Orden activo de una tabla; `null` es el orden por defecto del listado. */
export interface SortState {
  column: string;
  direction: SortDirection;
}

/** Ciclo de un encabezado: ascendente → descendente → orden por defecto. */
export function nextSort(current: SortState | null, column: string): SortState | null {
  if (current?.column !== column) return { column, direction: 'asc' };
  return current.direction === 'asc' ? { column, direction: 'desc' } : null;
}

/** Parámetros de consulta del orden (`sort`, `direction`) que aceptan los listados del backend. */
export function sortParams(sort: SortState | null): { sort?: string; direction?: SortDirection } {
  return sort ? { sort: sort.column, direction: sort.direction } : {};
}

/**
 * Encabezado de columna ordenable: `<th scope="col" appSortHeader="vessel" [sort]="sort()" (sortChange)="...">`.
 * El contenido del <th> queda dentro de un botón; el <th> expone aria-sort (ascending/descending/none) y el
 * botón describe la acción siguiente para lectores de pantalla.
 */
@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'th[appSortHeader]',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  host: {
    '[attr.aria-sort]': 'ariaSort()',
    class: 'hl-sortable',
  },
  template: `
    <button type="button" class="hl-sort-btn" (click)="toggle()">
      <ng-content />
      <svg class="hl-sort-btn__icon" [class.is-active]="direction() !== null" aria-hidden="true" focusable="false"
           xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
        @switch (direction()) {
          @case ('asc') {
            <path d="m7.247 4.86-4.796 5.481c-.566.647-.106 1.659.753 1.659h9.592a1 1 0 0 0 .753-1.659l-4.796-5.48a1 1 0 0 0-1.506 0z"/>
          }
          @case ('desc') {
            <path d="M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/>
          }
          @default {
            <path d="M3.204 6.5h9.592a.5.5 0 0 0 .376-.83L8.376.19a.5.5 0 0 0-.752 0L2.828 5.67a.5.5 0 0 0 .376.83m0 3h9.592a.5.5 0 0 1 .376.83l-4.796 5.48a.5.5 0 0 1-.752 0L2.828 10.33a.5.5 0 0 1 .376-.83"/>
          }
        }
      </svg>
      <span class="visually-hidden">{{ hint() | transloco }}</span>
    </button>
  `,
})
export class SortHeaderComponent {
  /** Nombre de la columna en el backend (lista blanca de cada listado). */
  readonly appSortHeader = input.required<string>();
  readonly sort = input<SortState | null>(null);
  readonly sortChange = output<SortState | null>();

  readonly direction = computed<SortDirection | null>(() =>
    this.sort()?.column === this.appSortHeader() ? (this.sort()?.direction ?? null) : null,
  );

  readonly ariaSort = computed(() => {
    const d = this.direction();
    return d === 'asc' ? 'ascending' : d === 'desc' ? 'descending' : 'none';
  });

  /** Lo que hará el próximo clic. */
  readonly hint = computed(() => {
    const d = this.direction();
    return d === null ? 'shared.sort.ascending' : d === 'asc' ? 'shared.sort.descending' : 'shared.sort.reset';
  });

  toggle(): void {
    this.sortChange.emit(nextSort(this.sort(), this.appSortHeader()));
  }
}
