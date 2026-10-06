import { Page, Request } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, simularApi } from '../fixtures/api-mocks';
import { ITEM, MENSAJE_BLOQUEO, OpcionesOlaD, PAGO, RUT_AGENCIA, RUT_MANDANTE, RUT_PROPIO } from '../fixtures/ola-d-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Fase 1, Ola D (pruebas funcionales con el backend simulado):
 * - M5-01 / M5-09: agregar un cargo al carro eligiendo el RUT de facturación entre los habilitados.
 * - M5-01 / NF-11: un error de validación del servidor (Nexus caído) se muestra sin agregar nada.
 * - M5-08 / M5-04 / M5-05: el carro se separa por moneda de pago, cada sección con su subtotal, y un
 *   ítem se convierte a otra moneda habilitada con el tipo de cambio de Nexus.
 * - NF-01 / NF-02 / NF-12: un reintento del cierre reutiliza la clave de idempotencia; el resultado se
 *   consulta hasta la confirmación y se anuncia.
 * - M8-07: con una ventana de bloqueo activa el carro se consulta, pero no se puede pagar.
 * - M5-07: un cliente con crédito no ve el carro sino "Pagar desde mi cuenta" y paga varios ítems juntos.
 * - M5-02: la boleta de depósito ya emitida no se puede anular desde el portal.
 * - M7-01: la descarga múltiple de facturas pide el zip solo con facturas con folio.
 * - M7-02: el historial distingue el RUT del pagador del RUT de facturación.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrir(page: Page, ruta: string, opciones: OpcionesOlaD = {}): Promise<void> {
  await simularApi(page, opciones);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('agregar un cargo al carro con el RUT de facturación elegido (M5-01, M5-09)', async ({ page }) => {
  await abrir(page, `/charges/${BL_PRUEBA.blNumber}`);
  await expect(page.getByRole('link', { name: 'Carro de compra, 3 ítems' })).toBeVisible();

  await page.getByRole('button', { name: 'Agregar IPO al carro' }).click();
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  const rut = dialogo.getByLabel('RUT de facturación (obligatorio)');
  // Solo los RUT habilitados: el propio y el del mandante del acceso otorgado.
  await expect(rut.locator('option')).toHaveText([
    'Elija el RUT de facturación',
    `Importadora Andes SpA (${RUT_PROPIO}), mi organización`,
    `Comercial Mandante SpA (${RUT_MANDANTE}), mandante del acceso otorgado`,
  ]);
  const moneda = dialogo.getByLabel('Moneda de pago (obligatorio)');
  await expect(moneda.locator('option')).toHaveText(['USD', 'CLP']);
  await expect(moneda).toHaveValue('USD');

  // Sin RUT elegido no se envía nada: resumen de errores con enlace al campo.
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  const resumen = dialogo.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await resumen.getByRole('link', { name: 'Elija el RUT de facturación.' }).click();
  await expect(rut).toBeFocused();

  await rut.selectOption(RUT_MANDANTE);
  await moneda.selectOption('CLP');
  // M5-05: la conversión muestra el tipo de cambio de Nexus.
  await expect(dialogo.getByTestId('exchange-rate-note')).toContainText('Tipo de cambio USD→CLP: 950,00');

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/cart/items'));
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  expect((await envio).postDataJSON()).toEqual({
    itemType: 'LocalCharge',
    sourceId: ITEM.IPO,
    reference: null,
    billingTaxId: RUT_MANDANTE,
    paymentCurrency: 'CLP',
  });

  await expect(page.locator(POLITE)).toHaveText('IPO se agregó al carro en CLP.');
  await expect(dialogo).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Carro de compra, 4 ítems' })).toBeVisible();
  await expect(page.locator('app-charges-panel table tbody tr').filter({ hasText: 'IPO' })).toContainText('En el carro');

  // En el carro, el ítem queda en la sección CLP con el RUT del mandante.
  await page.getByRole('link', { name: 'Carro de compra, 4 ítems' }).click();
  const clp = page.getByTestId('cart-group-CL-CLP');
  const ipo = clp.getByRole('row').filter({ hasText: 'IPO' });
  await expect(ipo.getByTestId('cart-item-billing')).toHaveText(`RUT ${RUT_MANDANTE}`);
  await expect(ipo).toContainText(/CLP\s142\.500/);
});

test('los errores de validación del servidor se muestran sin agregar nada (M5-01, NF-11)', async ({ page }) => {
  await abrir(page, `/charges/${BL_PRUEBA.blNumber}`, { nexusCaido: true });

  const agregar = page.waitForRequest((r) => r.url().includes('/api/v1/cart/item-options?') && r.url().includes(`sourceId=${ITEM.IPO}`));
  await page.getByRole('button', { name: 'Agregar IPO al carro' }).click();
  await agregar;
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await expect(dialogo.getByTestId('add-to-cart-error')).toHaveText(
    'Nexus no está disponible: no se pueden validar las condiciones comerciales. No se agregó ni se cobró nada. Intente más tarde.',
  );
  await expect(page.locator('div[aria-live="assertive"]')).toContainText('Nexus no está disponible');
  await expect(dialogo.getByRole('button', { name: 'Agregar al carro' })).toHaveCount(0);
  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await expect(dialogo).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Carro de compra, 3 ítems' })).toBeVisible();
});

test('el carro se separa por moneda de pago, cada una con su subtotal (M5-08, M5-04, M5-05)', async ({ page }) => {
  await abrir(page, '/cart');

  const clp = page.getByRole('region', { name: 'Pago en CLP, Chile' });
  const usd = page.getByRole('region', { name: 'Pago en USD, Chile' });
  await expect(clp).toBeVisible();
  await expect(usd).toBeVisible();
  // No es una lista mixta con un total único: cada sección tiene su subtotal y su propio pago.
  await expect(page.getByTestId('cart-subtotal-CL-CLP')).toHaveText(/^CLP\s128\.940$/);
  await expect(page.getByTestId('cart-subtotal-CL-USD')).toHaveText(/^USD\s450,00$/);
  await expect(clp.getByRole('button', { name: /^Revisar y pagar CLP\s128\.940$/ })).toBeVisible();
  await expect(usd.getByRole('button', { name: /^Revisar y pagar USD\s450,00$/ })).toBeVisible();
  // Detalle por tipo de servicio (M5-01).
  await expect(clp.getByRole('row').filter({ hasText: 'THC' })).toContainText('Cargo local');
  await expect(clp.getByRole('row').filter({ hasText: 'Cambio de almacén' })).toContainText('BL HLCU0000001');
  // Solo los medios habilitados para la moneda (M5-03): Khipu no acepta USD.
  await expect(usd.getByRole('radio', { name: /Khipu/ })).toHaveCount(0);
  await expect(clp.getByRole('radio', { name: /Khipu/ })).toHaveCount(1);

  // El MHD en USD se convierte a CLP con el tipo de cambio de Nexus y pasa a ese carro.
  const conversion = page.waitForRequest((r) => r.method() === 'PUT' && /\/api\/v1\/cart\/items\/[^/]+\/currency$/.test(r.url()));
  await usd.getByLabel('Moneda de pago de MHD').selectOption('CLP');
  expect((await conversion).postDataJSON()).toEqual({ paymentCurrency: 'CLP' });
  await expect(page.locator(POLITE)).toHaveText('MHD pasó al carro en CLP.');
  await expect(page.getByRole('region', { name: 'Pago en USD, Chile' })).toHaveCount(0);
  const mhd = clp.getByRole('row').filter({ hasText: 'MHD' });
  await expect(mhd.getByTestId('cart-item-rate')).toContainText('Tipo de cambio USD→CLP: 950,00, vigente al 05-10-2026');
  await expect(page.getByTestId('cart-subtotal-CL-CLP')).toHaveText(/^CLP\s556\.440$/);
});

test('un reintento del pago reutiliza la clave de idempotencia y el resultado se anuncia (NF-01, NF-02)', async ({ page }) => {
  await abrir(page, '/cart', { cierresSinRespuesta: 1 });
  const cierres: Request[] = [];
  page.on('request', (r) => {
    if (r.method() === 'POST' && r.url().endsWith('/api/v1/cart/checkout')) cierres.push(r);
  });

  const clp = page.getByTestId('cart-group-CL-CLP');
  await clp.getByRole('radio', { name: 'Khipu (En línea)' }).check();
  await clp.getByRole('button', { name: /^Revisar y pagar/ }).click();
  await expect(clp.getByRole('heading', { name: 'Confirme el pago' })).toBeFocused();
  await expect(clp).toContainText('2 ítems en un solo pago');

  // Primer intento: la plataforma no responde. No se sabe si llegó: se reintenta con la misma clave.
  await clp.getByTestId('cart-confirm-pay').click();
  await expect(clp.getByTestId('cart-checkout-error')).toHaveText(
    'No pudimos confirmar si el pago se recibió. Reintente: la misma solicitud nunca se cobra dos veces.',
  );
  // Doble clic en el reintento: el botón se deshabilita mientras se envía y la clave es la misma.
  await clp.getByRole('button', { name: /^Reintentar pago de CLP\s128\.940$/ }).dblclick();

  await expect(page).toHaveURL(/\/payments\/p5000000-0000-4000-8000-000000000001\/result\?ref=PAY-20261005-00000001$/);
  expect(cierres.length).toBeGreaterThanOrEqual(2);
  const claves = cierres.map((r) => r.headers()['idempotency-key']);
  expect(claves[0]).toMatch(/^[0-9a-f-]{36}$/);
  expect(new Set(claves).size).toBe(1);
  expect(cierres[0].postDataJSON()).toEqual({ country: 'CL', paymentCurrency: 'CLP', paymentMethodCode: 'KHIPU' });

  // Resultado: estado único que se consulta hasta la confirmación (NF-02, NF-12).
  await expect(page.getByTestId('payment-result-status')).toHaveText('Confirmado', { timeout: 10_000 });
  await expect(page.locator(POLITE)).toHaveText('Pago PAY-20261005-00000001 confirmado. Comprobante RCP-20261005-ABCD1234.');
  await expect(page.getByTestId('payment-result-message')).toContainText('Comprobante RCP-20261005-ABCD1234');
  await expect(page.getByRole('link', { name: 'Carro de compra, 1 ítem' })).toBeVisible();
});

test('con la ventana de bloqueo activa se consulta el carro pero no se puede pagar (M8-07)', async ({ page }) => {
  await abrir(page, '/cart', { bloqueo: true });

  const clp = page.getByTestId('cart-group-CL-CLP');
  await expect(clp.getByTestId('payment-block-banner')).toContainText('Pagos suspendidos temporalmente');
  await expect(clp.getByTestId('payment-block-banner')).toContainText(MENSAJE_BLOQUEO);
  await expect(clp.getByTestId('payment-block-banner')).toContainText('Hasta el 05-10-2026 a las 23:59 (America/Santiago).');
  await expect(clp.getByRole('button', { name: /^Revisar y pagar/ })).toBeDisabled();
  await expect(page.getByTestId('cart-group-CL-USD').getByRole('button', { name: /^Revisar y pagar/ })).toBeDisabled();
  await expect(clp).toContainText('No se puede pagar mientras los pagos estén suspendidos.');
  // La consulta sigue disponible.
  await expect(clp.getByRole('table')).toContainText('THC');
});

test('un cliente con crédito no ve el carro y paga desde su cuenta (M5-07)', async ({ page }) => {
  await abrir(page, `/charges/${BL_PRUEBA.blNumber}`, { credito: true });

  await expect(page.getByRole('link', { name: /^Carro de compra/ })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /al carro$/ })).toHaveCount(0);
  await expect(page.locator('app-charges-panel').getByRole('link', { name: 'Pagar desde mi cuenta' }).first()).toBeVisible();

  await page.getByTestId('navbar-account-payments').click();
  await expect(page).toHaveURL(/\/account-payments$/);
  await expect(page.getByTestId('account-payments-conditions')).toContainText('condición de crédito a 30 días según Nexus');
  await expect(page.getByRole('table', { name: /Cargos, fletes, demurrage/ }).getByRole('row')).toHaveCount(4);

  await page.getByLabel('Elegir todos').check();
  await expect(page.getByTestId('account-payments-selection')).toHaveText('3 ítems elegidos.');
  await expect(page.getByLabel('Moneda de pago')).toHaveValue('CLP');
  await page.getByRole('radio', { name: 'Botón Banco de Chile (En línea)' }).check();
  await page.getByRole('button', { name: 'Revisar y pagar 3 ítems' }).click();
  await expect(page.getByRole('heading', { name: 'Confirme el pago' })).toBeFocused();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/account-payments/checkout'));
  await page.getByRole('button', { name: 'Confirmar pago' }).click();
  const solicitud = await envio;
  expect(solicitud.headers()['idempotency-key']).toMatch(/^[0-9a-f-]{36}$/);
  expect(solicitud.postDataJSON()).toEqual({
    items: [
      { itemType: 'LocalCharge', sourceId: ITEM.THC_CREDITO, billingTaxId: null },
      { itemType: 'Freight', sourceId: ITEM.FLETE_CREDITO, billingTaxId: null },
      { itemType: 'Invoice', sourceId: ITEM.FACTURA_VENCIDA, billingTaxId: null },
    ],
    paymentCurrency: 'CLP',
    paymentMethodCode: 'BANK_BUTTON_BCH',
  });
  await expect(page).toHaveURL(/\/payments\/[^/]+\/result/);
});

test('la boleta de depósito no se puede anular después de emitida (M5-02)', async ({ page }) => {
  await abrir(page, '/cart');

  const clp = page.getByTestId('cart-group-CL-CLP');
  await clp.getByRole('radio', { name: 'Depósito bancario con boleta (Depósito con boleta)' }).check();
  await clp.getByRole('button', { name: /^Revisar y pagar/ }).click();
  await clp.getByTestId('cart-confirm-pay').click();

  await expect(page).toHaveURL(/\/payments\/p5000000-0000-4000-8000-000000000001\/result$/);
  await expect(page.getByTestId('payment-result-message')).toContainText('Boleta de depósito por emitir');
  // Antes de emitirla, el cliente puede anularla.
  await expect(page.getByRole('button', { name: 'Anular pago' })).toBeVisible();

  const emision = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/issue-slip'));
  await page.getByRole('button', { name: 'Emitir boleta' }).click();
  await emision;
  await expect(page.getByRole('heading', { name: 'Boleta emitida' })).toBeFocused();
  await expect(page.getByTestId('payment-result-slip')).toContainText('Boleta BOL-20261005-00000001 emitida');
  await expect(page.locator(POLITE)).toHaveText('Boleta BOL-20261005-00000001 del pago PAY-20261005-00000001 emitida. Finanzas verificará el depósito.');
  // Emitida, ya no se puede anular desde el portal: solo Finanzas.
  await expect(page.getByRole('button', { name: 'Anular pago' })).toHaveCount(0);
  await expect(page.getByTestId('payment-result-cancel-denied')).toHaveText(
    'La boleta ya fue emitida: no puede anularla desde el portal. Si necesita anularla, contacte a Finanzas.',
  );
  await expect(page.getByTestId('payment-result-status')).toHaveText('Boleta emitida, en verificación');
});

test('la descarga múltiple de facturas pide el zip solo con facturas con folio (M7-01)', async ({ page }) => {
  await abrir(page, '/invoices');

  // Más de una organización: se elige y no se mezclan.
  await expect(page.getByLabel('Organización').locator('option')).toHaveCount(2);
  const tabla = page.getByRole('table', { name: 'Facturas de Importadora Andes SpA' });
  const vencida = tabla.getByRole('row').filter({ hasText: 'HL-CL-2026-003987' });
  // Número SII y número del sistema de origen diferenciados.
  await expect(vencida.getByTestId('invoice-sii')).toHaveText('100245');
  await expect(vencida.getByTestId('invoice-source')).toHaveText('HL-CL-2026-003987');
  await expect(vencida).toContainText('Vencida');
  await expect(page.getByTestId('invoices-last-updated')).toContainText('Última actualización: 05-10-2026');

  // Sin folio no se puede elegir para la descarga múltiple.
  await expect(page.getByLabel('La factura HL-CL-2026-004601 no tiene folio SII: no se puede descargar')).toBeDisabled();

  await page.getByLabel('Elegir la factura 100245 para descargar').check();
  await page.getByLabel('Elegir la factura 100301 para descargar').check();
  const descarga = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/invoices/download'));
  await page.getByRole('button', { name: 'Descargar 2 facturas' }).click();
  expect((await descarga).postDataJSON()).toEqual({ ids: [ITEM.FACTURA_VENCIDA, ITEM.FACTURA_PAGADA] });
  await expect(page.locator(POLITE)).toHaveText('Se descargaron 2 facturas.');

  // Una factura habilitada para pago se agrega al carro.
  await vencida.getByRole('button', { name: 'Agregar la factura 100245 al carro' }).click();
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  await expect(page.locator(POLITE)).toHaveText('Factura 100245 se agregó al carro en CLP.');
  await expect(vencida).toContainText('En el carro');
});

test('el historial distingue el RUT del pagador del RUT de facturación (M7-02)', async ({ page }) => {
  await abrir(page, '/payment-history');

  const mandato = page.getByRole('row').filter({ hasText: 'PAY-20260920-5E6F7A8B' });
  await expect(mandato.getByTestId('history-payer')).toContainText(RUT_AGENCIA);
  await expect(mandato.getByTestId('history-billing')).toContainText(RUT_PROPIO);
  await expect(mandato.getByTestId('history-billing')).toContainText('Distinto del pagador');
  await expect(mandato).toContainText('Botón Banco de Chile');
  // Un pago propio no marca diferencia.
  await expect(page.getByRole('row').filter({ hasText: 'PAY-CL-2026-000123' }).getByTestId('history-billing')).not.toContainText('Distinto');

  await mandato.getByRole('link', { name: 'Ver el detalle de RCP-20260920-2B2B2B2B' }).click();
  await expect(page).toHaveURL(new RegExp(`/payment-history/${PAGO.MANDATO}$`));
  await expect(page.getByTestId('history-detail-payer')).toHaveText(`Agencia Marítima del Pacífico (${RUT_AGENCIA})`);
  await expect(page.getByTestId('history-detail-billing')).toHaveText(RUT_PROPIO);
  await expect(page.getByRole('region', { name: 'Pagador y facturación' })).toContainText('Por mandato de');

  const comprobante = page.waitForRequest((r) => r.url().endsWith(`/api/v1/payment-history/${PAGO.MANDATO}/receipt`));
  await page.getByRole('button', { name: 'Descargar' }).click();
  await comprobante;
});
