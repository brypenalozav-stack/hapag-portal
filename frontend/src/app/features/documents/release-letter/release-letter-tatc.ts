import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ReleaseLetterTatc } from '../../../core/models/document.model';
import { TATC_PENDING_REASON_KEYS, TATC_STATUS_CLASS, TATC_STATUS_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * TATC de las unidades de una carta de liberación (M6-08, M2-09) en un momento: al enviar, al aprobar o consultado
 * ahora (vista interna). Muestra el estado agregado, cuándo se consultó (con la zona, NF-22) y cada unidad con su número
 * de TATC, estado y motivos pendientes; si el sistema de TATC no respondió, lo dice sin mostrar datos como vigentes (NF-11).
 */
@Component({
  selector: 'app-release-letter-tatc',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe],
  template: `
    @let t = tatc();
    <div class="mb-3" [attr.data-testid]="'release-tatc-' + moment()">
      <h3 class="h6 fw-bold mb-1" [id]="'release-tatc-' + moment() + '-title'">{{ titleKey() | transloco }}</h3>
      @if (!t.available) {
        <p class="alert alert-warning py-2 mb-0" role="note">{{ 'documents.releaseLetter.tatc.unavailable' | transloco: { date: (t.checkedAt | hlDate: 'datetime' : timeZone()) } }}</p>
      } @else {
        <p class="small mb-2">
          @if (t.status) {
            <span class="hl-badge me-1" [class]="statusClass[t.status] || 'hl-badge--processing'">{{ t.status | codeLabel: statusKeys }}</span>
          }
          {{ 'documents.releaseLetter.tatc.checkedAt' | transloco: { date: (t.checkedAt | hlDate: 'datetime' : timeZone()) } }}
        </p>
        @if (t.containers.length > 0) {
          <div class="table-responsive" tabindex="0" role="region" [attr.aria-labelledby]="'release-tatc-' + moment() + '-title'">
            <table class="hl-table">
              <caption class="visually-hidden">{{ titleKey() | transloco }}</caption>
              <thead>
                <tr>
                  <th scope="col">{{ 'documents.releaseLetter.tatc.col.container' | transloco }}</th>
                  <th scope="col">{{ 'documents.releaseLetter.tatc.col.number' | transloco }}</th>
                  <th scope="col">{{ 'documents.releaseLetter.tatc.col.status' | transloco }}</th>
                  <th scope="col">{{ 'documents.releaseLetter.tatc.col.pending' | transloco }}</th>
                </tr>
              </thead>
              <tbody>
                @for (c of t.containers; track c.containerNumber) {
                  <tr>
                    <td class="fw-semibold">{{ c.containerNumber }}</td>
                    <td>{{ c.tatcNumber ?? '—' }}</td>
                    <td><span class="hl-badge" [class]="statusClass[c.status] || 'hl-badge--processing'">{{ c.status | codeLabel: statusKeys }}</span></td>
                    <td>
                      @for (reason of c.pendingReasons; track reason) {
                        <span class="d-block small">{{ reason | codeLabel: reasonKeys }}</span>
                      } @empty {
                        <span aria-hidden="true">—</span>
                        <span class="visually-hidden">{{ 'documents.releaseLetter.tatc.noPending' | transloco }}</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </div>
  `,
  styles: [':host { display: block; }'],
})
export class ReleaseLetterTatcComponent {
  tatc = input.required<ReleaseLetterTatc>();
  /** Momento de la consulta: `submission`, `approval` o `now` (identificadores y data-testid). */
  moment = input.required<string>();
  titleKey = input.required<string>();
  timeZone = input<string | null>(null);

  readonly statusKeys = TATC_STATUS_KEYS;
  readonly statusClass = TATC_STATUS_CLASS;
  readonly reasonKeys = TATC_PENDING_REASON_KEYS;
}
