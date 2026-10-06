import { translate } from '@jsverse/transloco';
import { apiErrorKey } from '../core/http/api-error';
import { ADMINISTRATION_ERRORS } from '../core/i18n/labels';
import { isServiceUnavailable } from './components/state-message/state-message';

/**
 * Mensaje traducido de un error de la Ola I (Fase 2): bandeja, comunicados, guías, vista como cliente, Counter,
 * contactos, transportistas pre-creados y empresa matriz. Sin conexión o con 5xx, el texto de servicio no disponible
 * (NF-11). Los textos del servidor vienen en inglés y no se muestran tal cual.
 */
export function adminErrorMessage(err: unknown, fallbackKey: string): string {
  if (isServiceUnavailable(err)) return translate('common.adminErrors.unavailable');
  return translate(apiErrorKey(err, ADMINISTRATION_ERRORS, fallbackKey));
}
