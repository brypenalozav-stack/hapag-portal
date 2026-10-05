import { Pipe, PipeTransform, inject } from '@angular/core';
import { AppLocale, LocaleService } from '../../core/services/locale.service';
import { getDateTimeFormat } from './intl-cache';

/** `date`: solo fecha; `datetime`: fecha y hora (HH:mm); `datetimeSeconds`: fecha y hora (HH:mm:ss). */
export type HlDateFormat = 'date' | 'datetime' | 'datetimeSeconds';

/** Separador de la fecha por locale (Q8): es-CL dd-MM-yyyy, es-BO dd/MM/yyyy, en dd MMM yyyy. */
const SEPARATOR: Record<AppLocale, string> = {
  'es-CL': '-',
  'es-BO': '/',
  en: ' ',
};

/**
 * Fecha (y hora) en el formato del idioma y país del usuario (Q8), en el huso del país
 * (America/Santiago o America/La_Paz). Las horas van en 24 h y llevan el huso abreviado
 * (`timeZoneName: 'short'`). Impuro para reaccionar al cambio de idioma en caliente.
 */
@Pipe({ name: 'hlDate', standalone: true, pure: false })
export class HlDatePipe implements PipeTransform {
  private readonly localeService = inject(LocaleService);

  transform(value: string | number | Date | null | undefined, format: HlDateFormat = 'date'): string {
    if (value === null || value === undefined || value === '') return '';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const locale = this.localeService.locale();
    const withTime = format !== 'date';
    const withSeconds = format === 'datetimeSeconds';

    const options: Intl.DateTimeFormatOptions = {
      timeZone: this.localeService.timeZone(),
      day: '2-digit',
      month: locale === 'en' ? 'short' : '2-digit',
      year: 'numeric',
      ...(withTime
        ? {
            hour: '2-digit',
            minute: '2-digit',
            ...(withSeconds ? { second: '2-digit' as const } : {}),
            hourCycle: 'h23' as const,
            timeZoneName: 'short' as const,
          }
        : {}),
    };

    // Se arma desde las partes para fijar el orden y el separador de Q8 en cualquier navegador.
    const parts: Partial<Record<Intl.DateTimeFormatPartTypes, string>> = {};
    for (const part of getDateTimeFormat(locale, options).formatToParts(date)) {
      parts[part.type] = part.value;
    }

    const day = [parts.day, parts.month, parts.year].join(SEPARATOR[locale]);
    if (!withTime) return day;

    const time = withSeconds
      ? `${parts.hour}:${parts.minute}:${parts.second}`
      : `${parts.hour}:${parts.minute}`;
    return `${day} ${time} ${parts.timeZoneName}`;
  }
}
