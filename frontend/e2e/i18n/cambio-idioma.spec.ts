import { test, expect } from '@playwright/test';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Fase 5b: el selector ES/EN de la barra superior cambia el idioma en caliente, sin recargar
 * la página, y los formatos siguen Q8 (CLP sin decimales, USD con 2; fechas por idioma y país).
 */

declare global {
  interface Window {
    __noReload?: boolean;
  }
}

test('cambia de español a inglés sin recargar y reformatea montos y fechas', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/payments');

  const titulo = page.locator('h1').first();
  const filaClp = page.locator('tr', { hasText: 'PAY-CL-2026-000125' });
  const filaUsd = page.locator('tr', { hasText: 'PAY-CL-2026-000123' });
  const montoClp = filaClp.locator('td').nth(3);
  const montoUsd = filaUsd.locator('td').nth(3);
  const fechaUsd = filaUsd.locator('td').nth(6);

  // Español, usuario de Chile (es-CL).
  await expect(titulo).toHaveText('Pagos');
  await expect(page.locator('html')).toHaveAttribute('lang', 'es');
  await expect(montoClp).toContainText('CLP');
  await expect(montoClp).toContainText(/1\.234\.567(?![,\d])/);
  await expect(montoUsd).toContainText('USD');
  await expect(montoUsd).toContainText(/2\.450,00(?!\d)/);
  await expect(fechaUsd).toContainText(/^25-09-2026 10:10 \S+/);

  // Marca en window: si la página se recargara, desaparecería.
  await page.evaluate(() => {
    window.__noReload = true;
  });

  const botonEn = page.getByRole('button', { name: 'English' });
  await expect(botonEn).toHaveAttribute('aria-pressed', 'false');
  await botonEn.click();

  await expect(titulo).toHaveText('Payments');
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(botonEn).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('button', { name: 'Español' })).toHaveAttribute('aria-pressed', 'false');
  expect(await page.evaluate(() => window.__noReload)).toBe(true);

  // Inglés: CLP sigue sin decimales y USD con 2; fecha dd MMM yyyy en el huso de Chile.
  await expect(montoClp).toContainText('CLP');
  await expect(montoClp).toContainText(/1,234,567(?![.\d])/);
  await expect(montoUsd).toContainText('USD');
  await expect(montoUsd).toContainText(/2,450\.00(?!\d)/);
  await expect(fechaUsd).toContainText(/^25 Sep 2026 10:10 \S+/);

  // La elección se recuerda.
  expect(await page.evaluate(() => localStorage.getItem('hl_lang'))).toBe('en');
});
