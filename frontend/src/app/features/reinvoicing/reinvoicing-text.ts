import { translate } from '@jsverse/transloco';
import { apiErrorKey } from '../../core/http/api-error';
import { REINVOICING_ERRORS } from '../../core/i18n/labels';
import { isServiceUnavailable } from '../../shared/components/state-message/state-message';

/** Mensaje traducido de un error de la refacturación IAO; sin conexión o con 5xx, el de servicio no disponible (NF-11). */
export function reinvoicingErrorMessage(err: unknown, fallbackKey: string): string {
  if (isServiceUnavailable(err)) return translate('common.reinvoicingErrors.unavailable');
  return translate(apiErrorKey(err, REINVOICING_ERRORS, fallbackKey));
}

/** Motivo de no elegibilidad de la cotización (código de error del servidor). */
export function ineligibleReason(code: string | null | undefined): string {
  return translate((code && REINVOICING_ERRORS[code]) || 'common.reinvoicingErrors.invoiceNotEligible');
}

/** RUT sin puntos, guion ni espacios, en mayúsculas, para comparar. */
export function normalizeTaxId(value: string | null | undefined): string {
  return (value ?? '').replace(/[.\s-]/g, '').toUpperCase();
}

/** Correo con forma válida (la validación completa la hace el servidor). */
export function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}
