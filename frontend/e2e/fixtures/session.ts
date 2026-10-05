import { Page } from '@playwright/test';
import type { Client } from '../../src/app/core/models/client.model';

/** Usuario cliente de Chile con el que se siembra la sesión de las pantallas autenticadas. */
export const USUARIO_PRUEBA: Client = {
  id: '7d1c2b3a-0000-4000-8000-000000000001',
  name: 'Importadora Andes SpA',
  email: 'cliente.prueba@example.com',
  taxId: '76.123.456-7',
  phone: '+56 2 2345 6789',
  country: 'CL',
  type: 'CLIENT',
  role: 'USER',
  isActive: true,
  createdAt: '2026-01-15T12:00:00Z',
};

export type Idioma = 'es' | 'en';

/** Idiomas de la interfaz (Q8): las pruebas de pantallas se repiten en cada uno. */
export const IDIOMAS: readonly Idioma[] = ['es', 'en'];

export interface OpcionesSesion {
  /** Idioma sembrado en `hl_lang` (lo lee LocaleService al arrancar). */
  lang?: Idioma;
}

/** Siembra el idioma en localStorage (`hl_lang`) antes de que cargue la aplicación. */
export async function sembrarIdioma(page: Page, lang: Idioma): Promise<void> {
  await page.addInitScript((idioma) => {
    localStorage.setItem('hl_lang', idioma);
  }, lang);
}

/**
 * Siembra la sesión en localStorage antes de que cargue la aplicación, con las mismas claves
 * que usa AuthService (`hl_token`, `hl_user`) y, si se indica, el idioma (`hl_lang`).
 */
export async function sembrarSesion(page: Page, opciones: OpcionesSesion = {}): Promise<void> {
  await page.addInitScript(
    ({ usuario }) => {
      localStorage.setItem('hl_token', 'token-de-prueba');
      localStorage.setItem('hl_user', JSON.stringify(usuario));
    },
    { usuario: USUARIO_PRUEBA },
  );
  if (opciones.lang) {
    await sembrarIdioma(page, opciones.lang);
  }
}
