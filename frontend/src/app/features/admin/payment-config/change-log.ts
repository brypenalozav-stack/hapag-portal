import { Component, input } from '@angular/core';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { MaintainerChange } from '../../../core/models/payment-config.model';
import { MAINTAINER_ACTION_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Registro de cambios de los mantenedores de pagos (NF-15, M8-07 criterio 8): usuario, fecha y hora,
 * acción y los valores anterior y nuevo, campo por campo.
 */
@Component({
  selector: 'app-change-log',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe],
  template: `
    <div class="table-responsive" tabindex="0" role="region" [attr.aria-label]="captionKey() | transloco">
      <table class="hl-table">
        <caption class="visually-hidden">{{ captionKey() | transloco }}</caption>
        <thead>
          <tr>
            <th scope="col">{{ 'admin.paymentConfig.history.col.date' | transloco }}</th>
            <th scope="col">{{ 'admin.paymentConfig.history.col.user' | transloco }}</th>
            <th scope="col">{{ 'admin.paymentConfig.history.col.action' | transloco }}</th>
            <th scope="col">{{ 'admin.paymentConfig.history.col.previous' | transloco }}</th>
            <th scope="col">{{ 'admin.paymentConfig.history.col.current' | transloco }}</th>
          </tr>
        </thead>
        <tbody>
          @for (change of changes(); track change.id) {
            <tr>
              <td>{{ change.changedAt | hlDate: 'datetime' }}</td>
              <td>{{ change.changedBy }}</td>
              <td>{{ change.action | codeLabel: actionKeys }}</td>
              <td class="small">
                @for (entry of entries(change.previous); track entry.field) {
                  <span class="d-block">{{ 'admin.paymentConfig.history.field' | transloco: entry }}</span>
                } @empty {
                  <span aria-hidden="true">—</span>
                  <span class="visually-hidden">{{ 'admin.paymentConfig.history.none' | transloco }}</span>
                }
              </td>
              <td class="small">
                @for (entry of entries(change.current); track entry.field) {
                  <span class="d-block">{{ 'admin.paymentConfig.history.field' | transloco: entry }}</span>
                } @empty {
                  <span aria-hidden="true">—</span>
                  <span class="visually-hidden">{{ 'admin.paymentConfig.history.none' | transloco }}</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class ChangeLogComponent {
  changes = input.required<MaintainerChange<object>[]>();
  /** Clave del texto de la tabla (caption y nombre de la región). */
  captionKey = input.required<string>();
  /** Campo del valor guardado → clave de su nombre; los campos sin clave no se muestran. */
  fieldKeys = input.required<Record<string, string>>();

  readonly actionKeys = MAINTAINER_ACTION_KEYS;

  entries(snapshot: object | null | undefined): { field: string; value: string }[] {
    if (!snapshot) return [];
    const keys = this.fieldKeys();
    return Object.entries(snapshot)
      .filter(([field]) => keys[field])
      .map(([field, value]) => ({ field: translate(keys[field]), value: this.format(value) }));
  }

  private format(value: unknown): string {
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'boolean') return translate(value ? 'admin.paymentConfig.history.yes' : 'admin.paymentConfig.history.no');
    if (Array.isArray(value)) return value.join(', ');
    return String(value);
  }
}
