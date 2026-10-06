import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

/**
 * Carga de una tabla: filas de relleno con la forma del listado mientras llegan los datos. Las filas son
 * decorativas; el estado se anuncia una vez con role="status".
 */
@Component({
  selector: 'app-table-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  template: `
    <div class="hl-table-skeleton" role="status" data-testid="table-skeleton">
      <span class="visually-hidden">{{ 'shared.loadingSpinner.label' | transloco }}</span>
      <div class="hl-table-skeleton__head" aria-hidden="true"></div>
      @for (r of rowList(); track r) {
        <div class="hl-table-skeleton__row" aria-hidden="true" [style.grid-template-columns]="'repeat(' + columns() + ', minmax(0, 1fr))'">
          @for (c of columnList(); track c) {
            <span class="hl-skeleton" [style.width.%]="widthOf(r, c)"></span>
          }
        </div>
      }
    </div>
  `,
  styles: `
    .hl-table-skeleton__head {
      height: 2.75rem;
      border-radius: 0.5rem 0.5rem 0 0;
      background-color: var(--hl-dark);
      opacity: 0.85;
    }

    .hl-table-skeleton__row {
      display: grid;
      gap: 1.5rem;
      align-items: center;
      padding: 1.05rem 1rem;
      border-bottom: 1px solid var(--hl-border);
    }
  `,
})
export class TableSkeletonComponent {
  readonly columns = input(6);
  readonly rows = input(6);

  readonly rowList = computed(() => Array.from({ length: this.rows() }, (_, i) => i));
  readonly columnList = computed(() => Array.from({ length: this.columns() }, (_, i) => i));

  /** Anchos variados pero fijos (sin azar): el relleno no "salta" entre renders. */
  widthOf(row: number, col: number): number {
    return 45 + ((row * 7 + col * 13) % 50);
  }
}
