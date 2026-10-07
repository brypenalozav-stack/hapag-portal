import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ClientTable } from '../../utils/client-table';

/**
 * Filtro rápido de una tabla en el navegador (ClientTable): campo de búsqueda con etiqueta visible y el conteo de
 * resultados como región de estado, para que el lector de pantalla oiga cuántas filas quedan.
 */
@Component({
  selector: 'app-table-filter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  template: `
    <div class="hl-table-filter">
      <label class="form-label small fw-semibold mb-1" [for]="inputId()">{{ 'shared.tableFilter.label' | transloco }}</label>
      <div class="d-flex flex-wrap align-items-center gap-2">
        <input type="search" class="form-control form-control-sm hl-table-filter__input" [id]="inputId()" autocomplete="off"
               [value]="table().query()" (input)="onInput($event)" [attr.aria-describedby]="inputId() + '-count'"
               [placeholder]="'shared.tableFilter.placeholder' | transloco" />
        <span class="small text-muted" [id]="inputId() + '-count'" role="status">
          {{ 'shared.tableFilter.count' | transloco: { shown: table().total(), total: totalOf() } }}
        </span>
      </div>
    </div>
  `,
  styles: `
    .hl-table-filter {
      padding: 0.75rem 1rem;
    }

    .hl-table-filter__input {
      max-width: 22rem;
    }
  `,
})
export class TableFilterComponent {
  /** ClientTable de cualquier tipo de fila. */
  readonly table = input.required<ClientTable<unknown>>();
  /** Id único del campo cuando hay más de un filtro en la pantalla. */
  readonly inputId = input('table-filter');
  /** Total sin filtrar (para "N de M"). */
  readonly totalOf = input.required<number>();

  onInput(event: Event): void {
    this.table().setQuery((event.target as HTMLInputElement).value);
  }
}
