import { Signal, computed, signal } from '@angular/core';
import { translate } from '@jsverse/transloco';
import { SortState } from '../components/sort-header/sort-header';

type SortValue = string | number | boolean | Date | null | undefined;

export interface ClientTableOptions<T> {
  /** Texto en el que busca el filtro (se compara sin mayúsculas ni tildes). */
  searchText: (row: T) => string;
  /** Columnas ordenables: nombre → valor de la fila. */
  sortValues?: Record<string, (row: T) => SortValue>;
  pageSize?: number;
}

/** Minúsculas y sin tildes: «Gestión» y «gestion» coinciden. */
export function normalizeSearch(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}

function compare(a: SortValue, b: SortValue): number {
  if (a === b) return 0;
  if (a === null || a === undefined || a === '') return 1;
  if (b === null || b === undefined || b === '') return -1;
  if (a instanceof Date && b instanceof Date) return a.getTime() - b.getTime();
  if (typeof a === 'number' && typeof b === 'number') return a - b;
  if (typeof a === 'boolean' && typeof b === 'boolean') return Number(a) - Number(b);
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' });
}

/**
 * Filtro, orden y paginación en el navegador para listados que llegan completos (mantenedores y listas cortas).
 * Los listados grandes usan paginación y orden del servidor; esta utilidad no los reemplaza.
 * Uso: `readonly table = new ClientTable(this.items, { searchText: (r) => r.name, sortValues: { name: (r) => r.name } })`
 * y en la plantilla `@for (row of table.rows(); ...)`, `<app-table-filter [table]="table" />` y `<app-paginator>`.
 */
export class ClientTable<T> {
  readonly query = signal('');
  readonly sort = signal<SortState | null>(null);
  private readonly requestedPage = signal(1);
  /** Página actual, acotada a las páginas que existen (al filtrar o recargar con menos filas). */
  readonly page: Signal<number>;
  readonly pageSize: ReturnType<typeof signal<number>>;

  readonly filtered: Signal<readonly T[]>;
  readonly total: Signal<number>;
  readonly rows: Signal<readonly T[]>;
  /** Hay más de una página o se eligió otro tamaño: mostrar el paginador. */
  readonly paged: Signal<boolean>;

  constructor(
    source: Signal<readonly T[]>,
    options: ClientTableOptions<T>,
  ) {
    const defaultSize = options.pageSize ?? 20;
    this.pageSize = signal(defaultSize);

    this.filtered = computed(() => {
      const q = normalizeSearch(this.query());
      const all = source() ?? [];
      return q ? all.filter((row) => normalizeSearch(options.searchText(row)).includes(q)) : all;
    });

    const sorted = computed(() => {
      const s = this.sort();
      const value = s ? options.sortValues?.[s.column] : undefined;
      if (!s || !value) return this.filtered();
      const dir = s.direction === 'asc' ? 1 : -1;
      return [...this.filtered()].sort((a, b) => dir * compare(value(a), value(b)));
    });

    this.total = computed(() => sorted().length);
    this.page = computed(() => Math.min(this.requestedPage(), Math.max(1, Math.ceil(this.total() / this.pageSize()))));
    this.rows = computed(() => {
      const size = this.pageSize();
      const page = this.page();
      return sorted().slice((page - 1) * size, page * size);
    });
    this.paged = computed(() => this.total() > this.pageSize() || this.pageSize() !== defaultSize);
  }

  setQuery(query: string): void {
    this.query.set(query);
    this.requestedPage.set(1);
  }

  setSort(sort: SortState | null): void {
    this.sort.set(sort);
    this.requestedPage.set(1);
  }

  setPage(page: number): void {
    this.requestedPage.set(page);
  }

  setPageSize(size: number): void {
    this.pageSize.set(size);
    this.requestedPage.set(1);
  }
}

/** Texto traducido de un código (mapa código → clave Transloco) para buscar u ordenar; sin clave, el código. */
export function codeText(code: string | null | undefined, keys: Record<string, string>): string {
  if (!code) return '';
  const key = keys[code];
  return key ? translate(key) : code;
}
