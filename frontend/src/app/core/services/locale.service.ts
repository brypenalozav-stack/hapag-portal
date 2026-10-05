import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

export type AppLang = 'es' | 'en';
export type AppLocale = 'es-CL' | 'es-BO' | 'en';

export const AVAILABLE_LANGS: readonly AppLang[] = ['es', 'en'];
export const DEFAULT_LANG: AppLang = 'es';

const LANG_KEY = 'hl_lang';

/** Huso horario IANA por país (Q8, DC3): los formatos se presentan en el huso del país del usuario. */
const TIME_ZONES: Record<'CL' | 'BO', string> = {
  CL: 'America/Santiago',
  BO: 'America/La_Paz',
};

/**
 * Idioma activo de la interfaz (ES/EN) y locale de formato por país (Q8).
 * - `lang` se guarda en localStorage (`hl_lang`) y cambia en caliente, sin recargar.
 * - `locale` combina idioma y país: es-CL, es-BO o en.
 * - `timeZone` es el huso del país del usuario (America/Santiago o America/La_Paz).
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly transloco = inject(TranslocoService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);

  readonly lang = signal<AppLang>(this.loadLang());

  readonly locale = computed((): AppLocale => {
    if (this.lang() === 'en') return 'en';
    return this.auth.getCountry() === 'BO' ? 'es-BO' : 'es-CL';
  });

  readonly timeZone = computed(() => TIME_ZONES[this.auth.getCountry()]);

  /** Aplica el idioma guardado y precarga sus textos antes del primer render (app initializer). */
  init(): Promise<void> {
    this.apply(this.lang());
    return firstValueFrom(this.transloco.load(this.lang())).then(
      () => undefined,
      (err: unknown) => console.warn('No se pudieron cargar los textos del idioma:', err),
    );
  }

  setLang(lang: AppLang): void {
    if (!AVAILABLE_LANGS.includes(lang)) return;
    this.lang.set(lang);
    try {
      localStorage.setItem(LANG_KEY, lang);
    } catch {
      // Sin almacenamiento disponible (modo privado o cuota): el idioma vale solo para esta sesión.
    }
    this.apply(lang);
  }

  private apply(lang: AppLang): void {
    this.transloco.setActiveLang(lang);
    this.document.documentElement.lang = lang;
  }

  private loadLang(): AppLang {
    try {
      const stored = localStorage.getItem(LANG_KEY);
      return AVAILABLE_LANGS.includes(stored as AppLang) ? (stored as AppLang) : DEFAULT_LANG;
    } catch {
      return DEFAULT_LANG;
    }
  }
}
