import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, simularApi } from '../fixtures/api-mocks';
import { sembrarIdioma, sembrarSesion } from '../fixtures/session';
import { cargarSeccionesDiferidas } from '../fixtures/detalle';

/**
 * Fase 5c: navegación con teclado del shell (guía UI/a11y/i18n §3.5) y cabeceras de tabla (§3.1).
 * - El enlace para saltar al contenido es el primer foco y Enter lleva el foco a <main>.
 * - A 375 px, el botón de menú alterna aria-expanded y controla el menú lateral.
 * - Cada th de cabecera de las tablas visitadas tiene scope.
 */

async function abrir(page: Page, ruta: string, autenticada = true): Promise<void> {
  await simularApi(page);
  if (autenticada) {
    await sembrarSesion(page, { lang: 'es' });
  } else {
    await sembrarIdioma(page, 'es');
  }
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  // Detalle del BL: los grupos diferidos se traen a la vista para revisar todas sus tablas.
  if (/^\/shipments\/[^/?]+$/.test(ruta)) await cargarSeccionesDiferidas(page);
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

for (const { ruta, autenticada } of [
  { ruta: '/login', autenticada: false },
  { ruta: '/dashboard', autenticada: true },
]) {
  test(`el enlace para saltar al contenido es el primer foco y lleva a main [${ruta}]`, async ({ page }) => {
    await abrir(page, ruta, autenticada);

    await page.keyboard.press('Tab');
    const salto = page.locator('a.hl-skip-link');
    await expect(salto).toBeFocused();
    await expect(salto).toBeVisible();
    await expect(salto).toHaveAttribute('href', '#contenido-principal');

    await page.keyboard.press('Enter');
    const main = page.locator('main#contenido-principal');
    await expect(main).toBeFocused();
    await expect(main).toHaveAttribute('tabindex', '-1');
  });
}

test('a 375 px el botón de menú alterna aria-expanded y el menú lateral', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await abrir(page, '/dashboard');

  const menu = page.locator('header button[aria-controls="hl-sidebar"]');
  const lateral = page.locator('#hl-sidebar');
  await expect(menu).toBeVisible();
  await expect(menu).toHaveAccessibleName('Menú de navegación');
  await expect(menu).toHaveAttribute('aria-expanded', 'false');
  await expect(lateral).toBeHidden();

  await menu.focus();
  await page.keyboard.press('Enter');
  await expect(menu).toHaveAttribute('aria-expanded', 'true');
  await expect(lateral).toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Menú principal' })).toBeVisible();

  // Escape cierra el menú lateral.
  await page.keyboard.press('Escape');
  await expect(menu).toHaveAttribute('aria-expanded', 'false');
  await expect(lateral).toBeHidden();

  await page.keyboard.press('Enter');
  await expect(menu).toHaveAttribute('aria-expanded', 'true');
  await menu.click();
  await expect(menu).toHaveAttribute('aria-expanded', 'false');
});

for (const ruta of [
  '/shipments',
  `/shipments/${BL_PRUEBA.blNumber}`,
  '/organization',
  '/payment-history',
  '/invoices',
  '/cart',
]) {
  test(`cada th de cabecera tiene scope y cada tabla tiene caption [${ruta}]`, async ({ page }) => {
    await abrir(page, ruta);

    const tablas = page.locator('main table');
    await expect(tablas.first()).toBeVisible();
    const total = await tablas.count();
    for (let i = 0; i < total; i++) {
      const tabla = tablas.nth(i);
      await expect(tabla.locator('caption')).toHaveCount(1);
      const cabeceras = tabla.locator('thead th');
      expect(await cabeceras.count()).toBeGreaterThan(0);
      const sinScope = await cabeceras.evaluateAll(
        (ths) => ths.filter((th) => !th.hasAttribute('scope')).map((th) => th.textContent?.trim()),
      );
      expect(sinScope, `th sin scope en ${ruta}`).toEqual([]);
    }
  });
}
