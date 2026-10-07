import { HttpErrorResponse } from '@angular/common/http';
import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type StateMessageKind = 'empty' | 'error';

/**
 * Error que muestra el estado `error` de NF-11: HTTP 5xx o estado 0 (sin conexión).
 * Los demás errores (por ejemplo 404) los sigue tratando cada pantalla con su propio mensaje.
 */
export function isServiceUnavailable(err: unknown): boolean {
  return err instanceof HttpErrorResponse && (err.status === 0 || err.status >= 500);
}

/**
 * Estados vacío y error (NF-11; guía UI/a11y/i18n §3.4).
 * - `empty`: HTTP 200 con lista vacía; `role="status"`, sin acción.
 * - `error`: HTTP 5xx o 0; `role="alert"` y botón Reintentar, que emite `retry`.
 * `messageKey` es la clave Transloco del texto; por defecto, el texto genérico de cada estado.
 */
@Component({
  selector: 'app-state-message',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    @if (kind() === 'error') {
      <div class="hl-card p-5 text-center hl-state-message hl-state-message--error" role="alert">
        <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="48" height="48" fill="currentColor" class="mb-3" viewBox="0 0 16 16">
          <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5m.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2"/>
        </svg>
        <p class="mb-3">{{ (messageKey() ?? 'shared.stateMessage.error') | transloco }}</p>
        <button type="button" class="btn btn-outline-primary" (click)="retry.emit()">
          {{ 'shared.stateMessage.retry' | transloco }}
        </button>
      </div>
    } @else {
      <div class="hl-card p-5 text-center hl-state-message" role="status">
        <svg aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg" width="48" height="48" fill="currentColor" class="mb-3" viewBox="0 0 16 16">
          <path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z"/>
        </svg>
        <p class="text-muted mb-0">{{ (messageKey() ?? 'shared.stateMessage.empty') | transloco }}</p>
      </div>
    }
  `,
})
export class StateMessageComponent {
  kind = input.required<StateMessageKind>();
  messageKey = input<string>();
  retry = output<void>();
}
