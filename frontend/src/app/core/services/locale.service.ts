import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom, take } from 'rxjs';
import { AuthService } from './auth.service';
import { LoadingOverlayService } from './loading-overlay.service';

export type AppLang = 'es' | 'en' | 'pt';
export type AppLocale = 'es-CL' | 'es-BO' | 'en' | 'pt-BR';

export const AVAILABLE_LANGS: readonly AppLang[] = ['es', 'en', 'pt'];
export const DEFAULT_LANG: AppLang = 'es';
/** Atributo lang del documento: pt-BR para que los lectores de pantalla usen la voz de Brasil. */
export const HTML_LANG: Record<AppLang, string> = { es: 'es', en: 'en', pt: 'pt-BR' };
/** Opciones del selector: código visible y clave del nombre accesible. */
export const LANGUAGE_OPTIONS: readonly { lang: AppLang; code: string; nameKey: string }[] = [
  { lang: 'es', code: 'ES', nameKey: 'shared.navbar.language.es' },
  { lang: 'en', code: 'EN', nameKey: 'shared.navbar.language.en' },
  { lang: 'pt', code: 'PT', nameKey: 'shared.navbar.language.pt' },
];

const LANG_KEY = 'hl_lang';

/** Huso horario IANA por país (Q8, DC3): los formatos se presentan en el huso del país del usuario. */
const TIME_ZONES: Record<'CL' | 'BO', string> = {
  CL: 'America/Santiago',
  BO: 'America/La_Paz',
};

/**
 * Idioma activo de la interfaz (ES/EN/PT) y locale de formato por país (Q8).
 * - `lang` se guarda en localStorage (`hl_lang`) y cambia en caliente, sin recargar.
 * - `locale` combina idioma y país: es-CL, es-BO, en o pt-BR (en y pt-BR no dependen del país).
 * - `timeZone` es el huso del país del usuario (America/Santiago o America/La_Paz).
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly transloco = inject(TranslocoService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly overlay = inject(LoadingOverlayService);

  readonly lang = signal<AppLang>(this.loadLang());

  readonly locale = computed((): AppLocale => {
    if (this.lang() === 'en') return 'en';
    if (this.lang() === 'pt') return 'pt-BR';
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

  /**
   * Cambia el idioma con la pantalla de carga global: primero se cargan los textos del idioma nuevo y
   * recién entonces se activa, para que el sitio nunca quede a medio traducir.
   */
  setLang(lang: AppLang): void {
    if (!AVAILABLE_LANGS.includes(lang) || lang === this.lang()) return;
    const end = this.overlay.begin('shared.globalLoader.changingLanguage');
    this.transloco.load(lang).pipe(take(1)).subscribe({
      next: () => this.activate(lang),
      error: (err: unknown) => {
        console.warn('No se pudieron cargar los textos del idioma:', err);
        end();
      },
      complete: end,
    });
  }

  private activate(lang: AppLang): void {
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
    this.document.documentElement.lang = HTML_LANG[lang];
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
