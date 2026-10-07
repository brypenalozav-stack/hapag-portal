import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  isDevMode,
  provideAppInitializer,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withPreloading } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideTransloco } from '@jsverse/transloco';
import { provideTranslocoMessageformat } from '@jsverse/transloco-messageformat';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { TranslocoHttpLoader } from './core/i18n/transloco-loader';
import { LocaleService } from './core/services/locale.service';
import { IdlePreloadingStrategy } from './core/routing/idle-preloading.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Las pantallas más usadas se precargan cuando el navegador está desocupado (data.preload).
    provideRouter(routes, withComponentInputBinding(), withPreloading(IdlePreloadingStrategy)),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideTransloco({
      config: {
        availableLangs: ['es', 'en'],
        defaultLang: 'es',
        fallbackLang: 'es',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    // Plurales ICU (guía UI/a11y/i18n §5.5).
    provideTranslocoMessageformat({ locales: ['es', 'en'] }),
    { provide: LOCALE_ID, useValue: 'es-CL' },
    // Idioma guardado (hl_lang) y sus textos listos antes del primer render.
    provideAppInitializer(() => inject(LocaleService).init()),
  ],
};
