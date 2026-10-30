import type { AppLang } from '../services/locale.service';

/** Idioma de las planillas: el de la interfaz (el servidor acepta es, en y pt). */
export function exportLanguage(lang: AppLang): 'es' | 'en' | 'pt' {
  return lang;
}

/** Prefijos de los archivos exportados por idioma, como los arma el servidor (sin tildes ni cedilla). */
export const EXPORT_PREFIXES: Record<AppLang, { accountStatement: string; transactions: string; exceptions: string }> = {
  es: { accountStatement: 'estado-de-cuenta', transactions: 'transacciones', exceptions: 'excepciones' },
  en: { accountStatement: 'account-statement', transactions: 'transactions', exceptions: 'exceptions' },
  pt: { accountStatement: 'extrato-de-conta', transactions: 'transacoes', exceptions: 'excecoes' },
};
