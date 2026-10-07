import { Page, Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Modal único de confirmación (ModalService): las acciones que borran, revocan o desactivan piden confirmación
 * en un <dialog> nativo. Cancelar, Esc o el fondo no ejecutan nada; el foco abre en la opción segura y vuelve
 * al botón que lo abrió (WCAG 2.4.3, 3.3.4).
 */

async function abrirCarro(page: Page): Promise<Request[]> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  const bajas: Request[] = [];
  page.on('request', (r) => {
    if (r.method() === 'DELETE' && /\/api\/v1\/cart\/items\/[^/]+$/.test(r.url())) bajas.push(r);
  });
  await page.goto('/cart');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  return bajas;
}

test('quitar un ítem del carro pide confirmación; cancelar y Esc no quitan nada', async ({ page }) => {
  const bajas = await abrirCarro(page);
  const clp = page.getByTestId('cart-group-CL-CLP');
  const quitar = clp.getByRole('button', { name: /^Quitar THC/ });
  const modal = page.getByRole('dialog');

  await quitar.click();
  await expect(modal).toBeVisible();
  await expect(modal.getByRole('heading')).toContainText('¿Quitar «THC');
  await expect(modal.getByRole('button', { name: 'Cancelar' })).toBeFocused();

  // Cancelar: se cierra, el foco vuelve al botón y no se llamó a la API.
  await modal.getByRole('button', { name: 'Cancelar' }).click();
  await expect(modal).toBeHidden();
  await expect(quitar).toBeFocused();

  // Esc también cancela.
  await quitar.click();
  await expect(modal).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(modal).toBeHidden();
  expect(bajas).toHaveLength(0);

  // Confirmar ejecuta la acción.
  await quitar.click();
  await modal.getByRole('button', { name: 'Quitar del carro' }).click();
  await expect(modal).toBeHidden();
  await expect.poll(() => bajas.length).toBe(1);

  // El resultado se ve como aviso breve (toast) y se anuncia una sola vez en la región viva.
  const aviso = page.getByTestId('toast');
  await expect(aviso).toContainText('THC se quitó del carro.');
  await expect(page.locator('div[aria-live="polite"]')).toHaveText('THC se quitó del carro.');
  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('[data-testid="toasts"]').withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
    expect(axe.violations, `axe toast ${tema}`).toEqual([]);
  }
  await aviso.getByRole('button', { name: 'Cerrar aviso' }).click();
  await expect(aviso).toHaveCount(0);
});
