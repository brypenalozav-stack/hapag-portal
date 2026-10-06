import { translate } from '@jsverse/transloco';
import { apiErrorKey } from '../core/http/api-error';
import { API_CLIENT_ERRORS } from '../core/i18n/labels';
import { isServiceUnavailable } from './components/state-message/state-message';

/**
 * Mensaje traducido de un error de la administración del canal Web Service (Fase 2, Ola J, M3-17). Sin conexión o con
 * 5xx, el texto de servicio no disponible (NF-11). Los textos del servidor vienen en inglés y no se muestran tal cual.
 */
export function apiClientErrorMessage(err: unknown, fallbackKey: string): string {
  if (isServiceUnavailable(err)) return translate('common.apiClientErrors.unavailable');
  return translate(apiErrorKey(err, API_CLIENT_ERRORS, fallbackKey));
}
