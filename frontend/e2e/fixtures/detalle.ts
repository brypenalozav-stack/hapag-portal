import { Page } from '@playwright/test';
import { expect } from './app';

/**
 * Detalle del BL (cierre de Fase 1, UX): los grupos bajo el pliegue se cargan al entrar en pantalla (`@defer on
 * viewport`). Las pruebas que revisan esas secciones las traen a la vista una a una, como haría quien recorre la
 * página, y vuelven arriba.
 */
export async function cargarSeccionesDiferidas(page: Page): Promise<void> {
  // Sin embarque (error o no encontrado) no hay grupos que cargar.
  await expect(page.locator('app-shipment-detail h1')).toHaveCount(1);
  await expect(page.locator('app-shipment-detail app-loading-spinner')).toHaveCount(0);
  if ((await page.getByTestId('bl-summary-header').count()) === 0) return;
  const marcadores = page.locator('[data-defer-placeholder]');
  for (let i = 0; i < 10; i++) {
    const pendientes = await marcadores.count();
    if (pendientes === 0) break;
    await marcadores.first().scrollIntoViewIfNeeded();
    await expect.poll(() => marcadores.count()).toBeLessThan(pendientes);
  }
  await page.evaluate(() => window.scrollTo(0, 0));
}
