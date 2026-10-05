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

export interface OpcionesSesion {
  /** Idioma sembrado en `hl_lang` (lo usa la capa i18n de la Fase 5b). */
  lang?: 'es' | 'en';
}

/**
 * Siembra la sesión en localStorage antes de que cargue la aplicación, con las mismas claves
 * que usa AuthService (`hl_token`, `hl_user`).
 */
export async function sembrarSesion(page: Page, opciones: OpcionesSesion = {}): Promise<void> {
  await page.addInitScript(
    ({ usuario, lang }) => {
      localStorage.setItem('hl_token', 'token-de-prueba');
      localStorage.setItem('hl_user', JSON.stringify(usuario));
      if (lang) {
        localStorage.setItem('hl_lang', lang);
      }
    },
    { usuario: USUARIO_PRUEBA, lang: opciones.lang },
  );
}
