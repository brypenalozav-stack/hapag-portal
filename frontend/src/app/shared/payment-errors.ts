import { translate } from '@jsverse/transloco';
import { apiErrorCode, apiErrorDetail, apiErrorKey } from '../core/http/api-error';
import { PAYMENT_ERRORS } from '../core/i18n/labels';
import { isServiceUnavailable } from './components/state-message/state-message';

/** Errores del cierre que el servidor informa por ítem, con el prefijo "<BL> <concepto>: " en `detail`. */
const ITEM_ERROR_PREFIX = /^(.{1,80}?): /;

/**
 * Mensaje traducido de un error de carro o pago (Ola D):
 * - `Payment.Blocked` muestra el mensaje que Hapag-Lloyd configuró para la ventana de bloqueo (M8-07);
 * - con `itemPrefix`, los errores de un ítem al pagar indican de qué BL y concepto se trata;
 * - sin conexión o con 5xx, el texto de servicio no disponible (NF-11).
 * Los textos del servidor vienen en inglés y no se muestran tal cual (salvo el mensaje configurado).
 */
export function paymentErrorMessage(err: unknown, fallbackKey: string, options: { itemPrefix?: boolean } = {}): string {
  if (isServiceUnavailable(err)) return translate('common.paymentErrors.unavailable');
  const code = apiErrorCode(err);
  const detail = apiErrorDetail(err);
  if (code === 'Payment.Blocked' && detail) {
    return translate('common.paymentErrors.blockedWithMessage', { message: detail });
  }
  const message = translate(apiErrorKey(err, PAYMENT_ERRORS, fallbackKey));
  const item = options.itemPrefix && detail ? ITEM_ERROR_PREFIX.exec(detail)?.[1] : null;
  return item ? translate('common.paymentErrors.forItem', { item, message }) : message;
}
