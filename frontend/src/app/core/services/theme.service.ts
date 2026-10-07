import { DOCUMENT, DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

/** Preferencia de tema del usuario: claro, oscuro o el del sistema operativo. */
export type ThemePreference = 'light' | 'dark' | 'auto';

export const THEME_PREFERENCES: readonly ThemePreference[] = ['auto', 'light', 'dark'];

const THEME_KEY = 'hl_theme';
const DARK_QUERY = '(prefers-color-scheme: dark)';

/**
 * Tema claro u oscuro (M11-07) construido sobre los tokens de la guía (M11-01): la preferencia se guarda en
 * `hl_theme` y se aplica como `data-bs-theme` en `<html>` (Bootstrap y `[data-bs-theme="dark"]` de
 * styles.scss). Sin preferencia guardada (`auto`) sigue al sistema operativo y cambia con él. index.html
 * aplica el mismo tema antes del primer pintado para evitar el destello del tema equivocado.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.(DARK_QUERY) ?? null;

  readonly preference = signal<ThemePreference>(this.load());
  private readonly systemDark = signal(this.media?.matches ?? false);

  /** Tema efectivo aplicado a la página. */
  readonly theme = computed<'light' | 'dark'>(() => {
    const pref = this.preference();
    return pref === 'dark' || (pref === 'auto' && this.systemDark()) ? 'dark' : 'light';
  });

  constructor() {
    const onChange = (event: MediaQueryListEvent) => {
      this.systemDark.set(event.matches);
      this.apply();
    };
    this.media?.addEventListener?.('change', onChange);
    inject(DestroyRef).onDestroy(() => this.media?.removeEventListener?.('change', onChange));
    this.apply();
  }

  setPreference(preference: ThemePreference): void {
    if (!THEME_PREFERENCES.includes(preference)) return;
    this.preference.set(preference);
    try {
      localStorage.setItem(THEME_KEY, preference);
    } catch {
      // Sin almacenamiento disponible (modo privado o cuota): la preferencia vale solo para esta sesión.
    }
    this.apply();
  }

  private apply(): void {
    const root = this.document.documentElement;
    root.setAttribute('data-bs-theme', this.theme());
    root.setAttribute('data-hl-theme-pref', this.preference());
  }

  private load(): ThemePreference {
    try {
      const stored = localStorage.getItem(THEME_KEY);
      return THEME_PREFERENCES.includes(stored as ThemePreference) ? (stored as ThemePreference) : 'auto';
    } catch {
      return 'auto';
    }
  }
}
