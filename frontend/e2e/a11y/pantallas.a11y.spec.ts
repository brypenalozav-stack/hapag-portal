import { readFileSync } from 'fs';
import path from 'path';
import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { BL_PRUEBA, simularApi } from '../fixtures/api-mocks';
import { IDIOMAS, Idioma, sembrarIdioma, sembrarSesion } from '../fixtures/session';

/**
 * Fase 5a: axe (WCAG 2.0/2.1/2.2 A y AA) sobre las pantallas principales.
 * Fase 5b: cada pantalla se recorre en español y en inglés (hl_lang) y se comprueba html[lang].
 * axe-baseline.json lista, por pantalla, las reglas que fallan hoy: la prueba falla solo si
 * aparece una regla nueva, en cualquiera de los dos idiomas. La Fase 5c corrige las pantallas
 * y deja la línea base en {}.
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

const LINEA_BASE: Record<string, string[]> = JSON.parse(
  readFileSync(path.join(__dirname, 'axe-baseline.json'), 'utf-8'),
);

const PANTALLAS = [
  { id: 'login', ruta: '/login', autenticada: false },
  { id: 'dashboard', ruta: '/dashboard', autenticada: true },
  { id: 'bl-list', ruta: '/bills-of-lading', autenticada: true },
  { id: 'bl-detail', ruta: `/bills-of-lading/${BL_PRUEBA.blNumber}`, autenticada: true },
  { id: 'payment-list', ruta: '/payments', autenticada: true },
  { id: 'payment-form', ruta: `/payments/new/${BL_PRUEBA.id}`, autenticada: true },
];

async function abrir(page: Page, ruta: string, autenticada: boolean, lang: Idioma): Promise<void> {
  await simularApi(page);
  if (autenticada) {
    await sembrarSesion(page, { lang });
  } else {
    await sembrarIdioma(page, lang);
  }
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
}

for (const lang of IDIOMAS) {
  for (const p of PANTALLAS) {
    test(`axe: ${p.id} [${lang}]`, async ({ page }) => {
      await abrir(page, p.ruta, p.autenticada, lang);
      await expect(page).toHaveURL(new RegExp(`${p.ruta}$`));
      await expect(page.locator('html')).toHaveAttribute('lang', lang);

      const resultado = await new AxeBuilder({ page }).withTags(TAGS).analyze();
      const conocidas = new Set(LINEA_BASE[p.id] ?? []);
      const nuevas = resultado.violations.filter((v) => !conocidas.has(v.id));
      const resumen = nuevas.map(
        (v) =>
          `${v.id} (${v.impact}): ${v.help}\n    ${v.nodes.map((n) => n.target.join(' ')).join('\n    ')}`,
      );

      expect(resumen, `Reglas axe nuevas en ${p.id} [${lang}]:\n${resumen.join('\n')}`).toEqual([]);
    });
  }
}
