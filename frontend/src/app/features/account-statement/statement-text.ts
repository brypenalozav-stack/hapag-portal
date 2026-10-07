import { translate } from '@jsverse/transloco';
import { StatementAgingBucket, StatementLine } from '../../core/models/account-statement.model';
import { CHARGE_CONCEPT_KEYS } from '../../core/i18n/labels';

export interface CurrencyAmount {
  currency: string;
  total: number;
}

/** Concepto de la línea en el idioma activo; un concepto sin clave muestra el nombre que informa el servidor. */
export function lineConcept(line: StatementLine): string {
  const key = CHARGE_CONCEPT_KEYS[line.conceptCode];
  return key ? translate(key) : (line.conceptName || line.conceptCode);
}

/** Saldo de las líneas por moneda (sin conversión: cada cargo en su moneda). */
export function totalsByCurrency(lines: StatementLine[]): CurrencyAmount[] {
  const map = new Map<string, number>();
  for (const line of lines) map.set(line.currency, (map.get(line.currency) ?? 0) + line.balance);
  return [...map.entries()].map(([currency, total]) => ({ currency, total }));
}

/** Nombre del tramo de antigüedad: no vencido, "1 a 30 días", "más de 90 días". */
export function agingBucketLabel(bucket: StatementAgingBucket): string {
  if (bucket.code === 'CURRENT') return translate('accountStatement.aging.current');
  if (bucket.toDays === null || bucket.toDays === undefined) {
    return translate('accountStatement.aging.over', { from: bucket.fromDays ?? 0 });
  }
  return translate('accountStatement.aging.range', { from: bucket.fromDays ?? 0, to: bucket.toDays });
}
