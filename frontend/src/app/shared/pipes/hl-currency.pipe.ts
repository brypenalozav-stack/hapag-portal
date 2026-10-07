import { Pipe, PipeTransform, inject } from '@angular/core';
import { getNumberOfCurrencyDigits } from '@angular/common';
import { LocaleService } from '../../core/services/locale.service';
import { getNumberFormat, toNumber } from './intl-cache';

/**
 * Monto con su código ISO 4217 (Q8, `currencyDisplay: 'code'`) y los decimales propios de la
 * moneda: CLP sin decimales; BOB, USD y EUR con 2. Impuro para reaccionar al cambio de idioma.
 */
@Pipe({ name: 'hlCurrency', standalone: true, pure: false })
export class HlCurrencyPipe implements PipeTransform {
  private readonly localeService = inject(LocaleService);

  transform(value: number | string | null | undefined, currencyCode: string | null | undefined): string {
    const n = toNumber(value);
    if (n === null) return '';

    const locale = this.localeService.locale();
    const code = (currencyCode ?? '').trim().toUpperCase();
    const digits = getNumberOfCurrencyDigits(code);

    if (!/^[A-Z]{3}$/.test(code)) {
      // Sin código válido no se puede usar style: 'currency'; se muestra solo el número.
      return getNumberFormat(locale, { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(n);
    }

    return getNumberFormat(locale, {
      style: 'currency',
      currency: code,
      currencyDisplay: 'code',
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
    }).format(n);
  }
}
