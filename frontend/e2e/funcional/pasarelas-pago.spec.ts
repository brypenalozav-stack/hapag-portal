import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { OpcionesOlaD, URL_BANCO } from '../fixtures/ola-d-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Pasarelas de pago (M5-03, docs/integraciones/pasarelas-pago.md):
 * - un botón bancario que exige un formulario firmado lleva al pagador al sitio del banco con un POST de los campos
 *   que entregó el portal, sin que el usuario haga nada más (y con el botón "Continuar al banco" de respaldo);
 * - si ese paso se recarga, el formulario ya no está y se ofrece ver el estado del pago;
 * - al volver de la pasarela, la página de resultado pide al portal consultar a la pasarela (POST verify).
 */

async function abrir(page: Page, ruta: string, opciones: OpcionesOlaD = {}): Promise<void> {
  await simularApi(page, opciones);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('el botón bancario con formulario firmado envía el pago al sitio del banco por POST', async ({ page }) => {
  await abrir(page, '/cart', { formularioBanco: true });
  await page.route(`${URL_BANCO}**`, (route) =>
    route.fulfill({ status: 200, contentType: 'text/html', body: '<!doctype html><html lang="es"><title>Banco</title><h1>Banco de prueba</h1></html>' }),
  );

  const clp = page.getByTestId('cart-group-CL-CLP');
  await clp.getByRole('radio', { name: 'Botón Banco de Chile, en línea' }).check();
  await clp.getByRole('button', { name: /^Revisar y pagar/ }).click();

  const alBanco = page.waitForRequest((r) => r.url().startsWith(URL_BANCO));
  await clp.getByTestId('cart-confirm-pay').click();
  const envio = await alBanco;

  expect(envio.method()).toBe('POST');
  const campos = new URLSearchParams(envio.postData() ?? '');
  expect(campos.get('convenio')).toBe('CONV-1');
  expect(campos.get('orden')).toBe('PAY-20261005-00000001');
  expect(campos.get('monto')).toBe('128940');
  expect(campos.get('firma')).toBe('f1a2b3');
  await expect(page).toHaveURL(URL_BANCO);
  await expect(page.getByRole('heading', { name: 'Banco de prueba' })).toBeVisible();
});

test('el paso al banco recargado ofrece ver el estado del pago', async ({ page }) => {
  await abrir(page, '/payments/p5000000-0000-4000-8000-000000000001/redirect');

  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Pago en el sitio del banco');
  await expect(page.getByTestId('payment-redirect-missing')).toHaveText('Este paso ya no está disponible. Revise el estado de su pago.');
  await expect(page.getByRole('link', { name: 'Ver el estado del pago' }))
    .toHaveAttribute('href', '/payments/p5000000-0000-4000-8000-000000000001/result');
});

test('al volver de la pasarela el resultado pide al portal consultar a la pasarela', async ({ page }) => {
  await abrir(page, '/cart');
  const verificaciones: string[] = [];
  page.on('request', (r) => {
    if (r.method() === 'POST' && /\/api\/v1\/payments\/[^/]+\/verify$/.test(r.url())) verificaciones.push(r.url());
  });

  const clp = page.getByTestId('cart-group-CL-CLP');
  await clp.getByRole('radio', { name: 'Khipu, en línea' }).check();
  await clp.getByRole('button', { name: /^Revisar y pagar/ }).click();
  await clp.getByTestId('cart-confirm-pay').click();

  await expect(page).toHaveURL(/\/payments\/[^/]+\/result/);
  await expect(page.getByTestId('payment-result-status')).toHaveText('Confirmado', { timeout: 10_000 });
  expect(verificaciones).toHaveLength(1);
});
