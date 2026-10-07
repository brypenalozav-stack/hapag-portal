import { HttpErrorResponse } from '@angular/common/http';
import { translate } from '@jsverse/transloco';
import { apiErrorCode, apiErrorKey } from '../core/http/api-error';
import { DOCUMENT_ERRORS } from '../core/i18n/labels';
import { isServiceUnavailable } from './components/state-message/state-message';

/**
 * Mensaje traducido de un error de documentos (Ola E):
 * - sin conexión o con 5xx, el texto de servicio no disponible (NF-11);
 * - un 403 sin código conocido, el de acción no permitida para el rol o el perfil (M1-11);
 * - los códigos conocidos de DOCUMENT_ERRORS; el resto, `fallbackKey`.
 * Los textos del servidor vienen en inglés y no se muestran tal cual.
 */
export function documentErrorMessage(err: unknown, fallbackKey: string): string {
  if (isServiceUnavailable(err)) return translate('common.documentErrors.unavailable');
  if (!apiErrorCode(err) && err instanceof HttpErrorResponse && err.status === 403) {
    return translate('common.documentErrors.forbidden');
  }
  return translate(apiErrorKey(err, DOCUMENT_ERRORS, fallbackKey));
}
