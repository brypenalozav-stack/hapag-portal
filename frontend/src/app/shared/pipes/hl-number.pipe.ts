import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';
import { getNumberFormat, toNumber } from './intl-cache';

const DIGITS_INFO = /^(\d+)?\.((\d+))?(-(\d+))?$/;

/**
 * Número con los separadores del idioma y país del usuario (Q8). `digitsInfo` sigue la
 * convención de Angular: `{minIntegerDigits}.{minFractionDigits}-{maxFractionDigits}`
 * (por defecto `1.0-3`). Impuro para reaccionar al cambio de idioma en caliente.
 */
@Pipe({ name: 'hlNumber', standalone: true, pure: false })
export class HlNumberPipe implements PipeTransform {
  private readonly localeService = inject(LocaleService);

  transform(value: number | string | null | undefined, digitsInfo = '1.0-3'): string {
    const n = toNumber(value);
    if (n === null) return '';

    const match = DIGITS_INFO.exec(digitsInfo);
    const minimumIntegerDigits = match?.[1] ? Number(match[1]) : 1;
    const minimumFractionDigits = match?.[3] ? Number(match[3]) : 0;
    const maximumFractionDigits = Math.max(
      minimumFractionDigits,
      match?.[5] ? Number(match[5]) : 3,
    );

    return getNumberFormat(this.localeService.locale(), {
      minimumIntegerDigits,
      minimumFractionDigits,
      maximumFractionDigits,
    }).format(n);
  }
}
