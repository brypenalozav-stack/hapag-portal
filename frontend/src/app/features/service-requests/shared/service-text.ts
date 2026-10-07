import { HttpErrorResponse } from '@angular/common/http';
import { translate } from '@jsverse/transloco';
import { apiErrorKey } from '../../../core/http/api-error';
import { SERVICE_REQUEST_ERRORS } from '../../../core/i18n/labels';
import {
  OUTPUT_FIELD_KEY,
  SERVICE_FORM_LIMITS,
  ServiceInputField,
  ServiceInputValue,
  ServiceInputValues,
} from '../../../core/models/service-request.model';
import { isServiceUnavailable } from '../../../shared/components/state-message/state-message';

/** Idioma de la interfaz: los textos configurados en el mantenedor llegan en español e inglés. */
export type ServiceLang = 'es' | 'en';

/** Texto en el idioma activo; si falta en inglés, se usa el español. */
export function localized(lang: string, es: string | null | undefined, en: string | null | undefined): string {
  return (lang === 'en' ? en || es : es || en) ?? '';
}

export function fieldLabel(field: ServiceInputField, lang: string): string {
  return localized(lang, field.labelEs, field.labelEn);
}

export function fieldHelp(field: ServiceInputField, lang: string): string {
  return localized(lang, field.helpEs, field.helpEn);
}

/** Largo máximo de un texto: el del campo o el que aplica el servidor por defecto. */
export function maxLengthOf(field: ServiceInputField): number {
  return field.maxLength ?? (field.type === 'textarea' ? SERVICE_FORM_LIMITS.DEFAULT_TEXTAREA_LENGTH : SERVICE_FORM_LIMITS.DEFAULT_TEXT_LENGTH);
}

/** Valor de un campo para leer (selecciones con su texto, contenedores separados por coma). */
export function displayValue(field: ServiceInputField, value: ServiceInputValue | undefined, lang: string): string {
  if (value === undefined || value === null || value === '') return '';
  if (Array.isArray(value)) return value.join(', ');
  if (field.type === 'select') {
    const option = (field.options ?? []).find((o) => o.value === value);
    return option ? localized(lang, option.labelEs, option.labelEn) : String(value);
  }
  return String(value);
}

/** Campos que se responden en el formulario (los `file` se responden con adjuntos). */
export function hasValue(values: ServiceInputValues, key: string): boolean {
  const v = values[key];
  return Array.isArray(v) ? v.length > 0 : v !== undefined && v !== null && String(v).trim() !== '';
}

export function isOutput(fieldKey: string): boolean {
  return fieldKey === OUTPUT_FIELD_KEY;
}

/** Tamaño de un archivo en KB o MB, con un decimal. */
export function fileSize(bytes: number): string {
  return bytes >= 1024 * 1024
    ? translate('serviceRequests.files.sizeMb', { size: (bytes / (1024 * 1024)).toFixed(1) })
    : translate('serviceRequests.files.sizeKb', { size: Math.max(1, Math.round(bytes / 1024)) });
}

/** Mensaje traducido de un error de la Ola G; sin conexión o con 5xx, el texto de servicio no disponible (NF-11). */
export function serviceErrorMessage(err: unknown, fallbackKey: string): string {
  if (isServiceUnavailable(err)) return translate('common.serviceRequestErrors.unavailable');
  return translate(apiErrorKey(err, SERVICE_REQUEST_ERRORS, fallbackKey));
}

/**
 * Errores de validación del servidor por campo del formulario (`errors: { "InputValues.<clave>": [...] }`): devuelve
 * las claves de los campos con error. Los mensajes del servidor vienen en inglés; la pantalla muestra los propios.
 */
export function inputErrorKeys(err: unknown): string[] {
  if (!(err instanceof HttpErrorResponse) || err.status !== 400) return [];
  const errors = (err.error as { errors?: Record<string, unknown> } | null)?.errors;
  if (!errors || typeof errors !== 'object') return [];
  return Object.keys(errors)
    .map((k) => /^inputValues\.(.+)$/i.exec(k)?.[1])
    .filter((k): k is string => !!k);
}

/** Campos de facturación con error de validación del servidor (`Billing.<campo>`). */
export function billingErrorKeys(err: unknown): string[] {
  if (!(err instanceof HttpErrorResponse) || err.status !== 400) return [];
  const errors = (err.error as { errors?: Record<string, unknown> } | null)?.errors;
  if (!errors || typeof errors !== 'object') return [];
  return Object.keys(errors)
    .map((k) => /^billing\.(.+)$/i.exec(k)?.[1])
    .filter((k): k is string => !!k)
    .map((k) => k.charAt(0).toLowerCase() + k.slice(1));
}
