import type { AppLang } from '../services/locale.service';

/** Texto administrable por idioma (los campos `...Es`, `...En` y `...Pt` de anuncios, guías, servicios y DG). */
export interface LocalizedText {
  es?: string | null;
  en?: string | null;
  pt?: string | null;
}

/** Respaldo general: pt → es (idioma más cercano) → en. */
export const TEXT_FALLBACK: Record<AppLang, readonly AppLang[]> = {
  es: ['es', 'en', 'pt'],
  en: ['en', 'es', 'pt'],
  pt: ['pt', 'es', 'en'],
};

/** Mercancías peligrosas: pt → en (nombre oficial ONU) → es. */
export const DG_FALLBACK: Record<AppLang, readonly AppLang[]> = {
  es: ['es', 'en', 'pt'],
  en: ['en', 'es', 'pt'],
  pt: ['pt', 'en', 'es'],
};

/** Texto en el idioma activo; si falta, el primero disponible según el orden de respaldo. */
export function pickText(lang: string, text: LocalizedText, order: Record<AppLang, readonly AppLang[]> = TEXT_FALLBACK): string {
  for (const l of order[lang as AppLang] ?? order.es) {
    const value = text[l];
    if (value && value.trim()) return value;
  }
  return '';
}
