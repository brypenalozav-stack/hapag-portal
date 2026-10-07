import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/** Barra "Ir a" del detalle del embarque (app-section-nav): un enlace por grupo, foco en el título elegido. */
test('el detalle del embarque ofrece "Ir a" con los grupos y lleva el foco al título elegido', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(`/shipments/${BL_PRUEBA.blNumber}`);
  const nav = page.getByRole('navigation', { name: 'Secciones de esta página' });
  await expect(nav).toBeVisible();
  await expect(nav.getByRole('link', { name: 'Resumen' })).toBeVisible();

  const enlace = nav.getByRole('link', { name: 'Contenedores' });
  await enlace.click();
  await expect(page.locator('#bl-group-containers')).toBeFocused();
  await expect(enlace).toHaveAttribute('aria-current', 'location');

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('[data-testid="section-nav"]').withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
    expect(axe.violations, `axe ${tema}`).toEqual([]);
  }
});
