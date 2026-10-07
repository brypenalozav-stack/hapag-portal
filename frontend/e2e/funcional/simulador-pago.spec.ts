import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { OpcionesOlaD } from '../fixtures/ola-d-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Simulador de pago en modo de prueba (docs/integraciones/pasarelas-pago.md, «Simulador en modo de prueba»): con la
 * pasarela en Dummy, el cierre lleva a /payments/simulator, que imita a la pasarela (nombre, logo y color del
 * proveedor) y avisa que no hay cobro real. El usuario elige pagar, rechazar, dejar pendiente o cancelar; el portal
 * aplica el resultado (POST payments/simulator/{referencia}) y vuelve a la página de resultado, que verifica el pago.
 * La URL de retorno debe ser del propio portal (sin redirección abierta).
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const REFERENCIA = 'PAY-20261005-00000001';

async function abrir(page: Page, ruta: string, opciones: OpcionesOlaD = {}): Promise<void> {
  await simularApi(page, { simuladorPago: true, ...opciones });
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

/** Cierra el grupo CLP del carro con el medio indicado y espera el simulador. */
async function pagarCon(page: Page, medio: string): Promise<void> {
  const clp = page.getByTestId('cart-group-CL-CLP');
  await clp.getByRole('radio', { name: medio }).check();
  await clp.getByRole('button', { name: /^Revisar y pagar/ }).click();
  await clp.getByTestId('cart-confirm-pay').click();
  await expect(page).toHaveURL(/\/payments\/simulator\?/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Confirme su pago');
}

function simulador(params: Record<string, string>): string {
  return `/payments/simulator?${new URLSearchParams(params).toString()}`;
}

test('el pago con Khipu en modo de prueba pasa por el simulador y «Pagar» lo confirma', async ({ page }) => {
  await abrir(page, '/cart');
  const simulaciones: unknown[] = [];
  page.on('request', (r) => {
    if (r.method() === 'POST' && r.url().endsWith(`/api/v1/payments/simulator/${REFERENCIA}`)) simulaciones.push(r.postDataJSON());
  });

  await pagarCon(page, 'Khipu, en línea');

  await expect(page.getByRole('heading', { level: 1 })).toBeFocused();
  await expect(page.getByTestId('payment-simulator-banner')).toHaveText(
    'Simulador de pago — modo de prueba. No se realiza ningún cargo real.',
  );
  await expect(page.getByTestId('payment-simulator-provider')).toContainText('Khipu');
  await expect(page.getByTestId('payment-simulator-provider').locator('img')).toHaveAttribute('src', '/payment-methods/khipu.png');
  await expect(page.getByTestId('payment-simulator-merchant')).toHaveText('Hapag-Lloyd');
  await expect(page.getByTestId('payment-simulator-amount')).toContainText('128.940');
  await expect(page.getByTestId('payment-simulator-reference')).toHaveText(REFERENCIA);

  await page.getByRole('button', { name: 'Pagar', exact: true }).click();

  await expect(page).toHaveURL(new RegExp(`/payments/[^/]+/result\\?ref=${REFERENCIA}$`));
  await expect(page.getByTestId('payment-result-status')).toHaveText('Confirmado');
  expect(simulaciones).toEqual([{ outcome: 'approved' }]);
});

test('el botón Banco de Chile en modo de prueba usa el simulador y «Rechazar el pago» lo deja fallido', async ({ page }) => {
  await abrir(page, '/cart');

  await pagarCon(page, 'Botón Banco de Chile, en línea');
  await expect(page.getByTestId('payment-simulator-provider')).toContainText('Banco de Chile');
  await expect(page.getByTestId('payment-simulator-provider').locator('img')).toHaveAttribute('src', '/payment-methods/bank-button-bch.png');

  await page.getByRole('button', { name: 'Rechazar el pago' }).click();

  await expect(page).toHaveURL(/\/payments\/[^/]+\/result/);
  await expect(page.getByTestId('payment-result-status')).toHaveText('Fallido, sin cobro');
});

test('un pago con resultado final no se vuelve a simular: se informa y se ofrece ver su estado', async ({ page }) => {
  await abrir(page, '/cart');

  await pagarCon(page, 'Khipu, en línea');
  await page.getByRole('button', { name: 'Rechazar el pago' }).click();
  await expect(page.getByTestId('payment-result-status')).toHaveText('Fallido, sin cobro');

  // El usuario vuelve atrás al simulador e intenta pagar el pago ya rechazado.
  await page.goBack();
  await expect(page).toHaveURL(/\/payments\/simulator\?/);
  await page.getByRole('button', { name: 'Pagar', exact: true }).click();

  await expect(page.getByTestId('payment-simulator-error')).toHaveText(
    'Este pago ya tiene un resultado final y el simulador no puede cambiarlo. Revise el estado del pago.',
  );
  await expect(page.getByRole('button', { name: 'Pagar', exact: true })).toHaveCount(0);
  const verEstado = page.getByRole('button', { name: 'Ver el estado del pago' });
  await expect(verEstado).toBeFocused();
  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe simulador con resultado final ${tema}`).toEqual([]);
  }

  await verEstado.click();
  await expect(page).toHaveURL(new RegExp(`/payments/[^/]+/result\\?ref=${REFERENCIA}$`));
  await expect(page.getByTestId('payment-result-status')).toHaveText('Fallido, sin cobro');
});

test('«Dejar pendiente» vuelve al portal con el pago en proceso', async ({ page }) => {
  await abrir(page, '/cart');

  await pagarCon(page, 'Khipu, en línea');
  await page.getByRole('button', { name: 'Dejar pendiente' }).click();

  await expect(page).toHaveURL(/\/payments\/[^/]+\/result/);
  await expect(page.getByTestId('payment-result-status')).toHaveText('En proceso en la plataforma');
});

test('«Cancelar y volver al portal» vuelve a la página de resultado sin cobro', async ({ page }) => {
  await abrir(page, '/cart');

  await pagarCon(page, 'Khipu, en línea');
  await page.getByRole('button', { name: 'Cancelar y volver al portal' }).click();

  await expect(page).toHaveURL(/\/payments\/[^/]+\/result/);
  await expect(page.getByTestId('payment-result-status')).toHaveText('Fallido, sin cobro');
});

for (const retorno of ['https://sitio-ajeno.example/payments/x/result', '//sitio-ajeno.example/payments/x/result', 'javascript:alert(1)', '/dashboard']) {
  test(`el simulador rechaza una URL de retorno fuera de los pagos del portal (${retorno})`, async ({ page }) => {
    await abrir(page, simulador({ provider: 'Khipu', ref: REFERENCIA, amount: '1000', currency: 'CLP', returnUrl: retorno }));

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Simulador de pago');
    await expect(page.getByTestId('payment-simulator-invalid')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pagar', exact: true })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Ir al historial de pagos' })).toHaveAttribute('href', '/payment-history');
  });
}

test('si la pasarela del pago no está en modo de prueba, el simulador lo informa y no sale de la página', async ({ page }) => {
  await abrir(page, simulador({ provider: 'Bci', ref: 'PAY-OTRO', amount: '1000', currency: 'CLP', returnUrl: '/payments/x/result?ref=PAY-OTRO' }));

  await page.getByRole('button', { name: 'Pagar', exact: true }).click();

  await expect(page.getByTestId('payment-simulator-error')).toHaveText(
    'El simulador no está disponible para este pago: su medio de pago no está en modo de prueba.',
  );
  await expect(page).toHaveURL(/\/payments\/simulator\?/);
});

for (const [provider, nombre] of [['Khipu', 'Khipu'], ['Santander', 'Getnet · Botón Santander'], ['Bci', 'Bci Pagos'], ['BancoChile', 'Banco de Chile']]) {
  test(`simulador de ${nombre}: accesible en tema claro y oscuro y sin desplazamiento horizontal en móvil`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await abrir(page, simulador({ provider, ref: REFERENCIA, amount: '128940', currency: 'CLP', returnUrl: `/payments/x/result?ref=${REFERENCIA}` }));
    await expect(page.getByTestId('payment-simulator-provider')).toContainText(nombre);
    for (const nombreBoton of ['Pagar', 'Rechazar el pago', 'Dejar pendiente', 'Cancelar y volver al portal']) {
      await expect(page.getByRole('button', { name: nombreBoton, exact: true })).toBeVisible();
    }

    const ancho = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(ancho).toBeLessThanOrEqual(0);

    for (const tema of ['light', 'dark'] as const) {
      await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
      const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
      expect(axe.violations, `axe simulador ${provider} ${tema}`).toEqual([]);
    }
  });
}
