import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { FASE2 } from '../fixtures/funcionalidades';
import { ITEM, OpcionesOlaD } from '../fixtures/ola-d-mocks';
import { BL_GATE_OUT_POR_PAGAR, ESTADO, NUEVA_RAZON, PAGO_DEPOSITO, RECIBO_ANTICIPO, TOKEN_ACEPTACION } from '../fixtures/ola-h-mocks';
import { ORGANIZACION_PRUEBA, sembrarIdioma, sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Fase 2, Ola H (pruebas funcionales con el backend simulado):
 * - M7-03: estado de cuenta con resumen, antigüedad y última actualización; filtros y exportación con los filtros aplicados.
 * - M7-03 / M5-01: dos facturas elegidas van al carro en una sola llamada (POST /cart/items/batch); sin crédito no se ve la
 *   forma de pago por ítem.
 * - M5-10: el cliente con crédito paga un ítem ahora e imputa otro a crédito; el total se divide y lo imputado queda como
 *   saldo pendiente.
 * - M5-06: el cliente adjunta el comprobante, Finanzas lo rechaza con motivo, el cliente ve el motivo y sube otro, y Finanzas
 *   lo verifica.
 * - M3-11: refacturación desde la factura (datos, aprobación, envío) y aceptación de la nueva razón social por el enlace
 *   público, sin sesión.
 * - M3-19 / M7-01: la factura emitida tras el anticipo aparece cubierta por el recibo; la refacturada, vinculada a la nueva.
 * - Ola G: el dashboard muestra la solicitud de servicio y el filtro de servicios lee las definiciones del servidor.
 */

const POLITE = 'div[aria-live="polite"]';
const PDF = { name: 'comprobante.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4 comprobante de prueba') };

async function abrir(page: Page, ruta: string, opciones: OpcionesOlaD = {}, sesion: 'cliente' | 'admin' | 'ninguna' = 'cliente'): Promise<void> {
  await simularApi(page, { ...opciones, features: FASE2 });
  if (sesion === 'admin') await sembrarSesionAdmin(page, 'es');
  else if (sesion === 'cliente') await sembrarSesion(page, { lang: 'es' });
  else await sembrarIdioma(page, 'es');
  await ir(page, ruta);
}

/** Navega dentro de la misma simulación (el estado del backend simulado se conserva). */
async function ir(page: Page, ruta: string): Promise<void> {
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

test('estado de cuenta: resumen, antigüedad, filtros y exportación con los filtros aplicados (M7-03)', async ({ page }) => {
  await abrir(page, '/account-statement');

  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Estado de cuenta');
  await expect(page.getByTestId('statement-last-updated')).toContainText('Última actualización de las facturas:');
  const clp = page.getByTestId('statement-summary-CLP');
  await expect(clp.getByTestId('statement-overdue')).toHaveText(/595\.000/);
  await expect(page.getByTestId('statement-aging')).toContainText('1 a 30 días');
  await expect(page.getByTestId('statement-aging')).toContainText('91 días o más');
  // Facturado, no facturado y anticipos, por separado; lo próximo a vencer, destacado.
  await expect(page.getByTestId('statement-group-Invoiced')).toContainText('100245');
  await expect(page.getByTestId('statement-group-Uninvoiced')).toContainText('SRV-20261005-5E1A0002');
  await expect(page.getByTestId('statement-line-100310').getByTestId('statement-due-soon')).toHaveText('Por vencer');
  await expect(page.getByTestId('statement-line-100318').getByTestId('statement-covered-by')).toContainText(RECIBO_ANTICIPO);
  await expect(page.getByTestId('statement-advances')).toContainText('PAY-20261002-A1C2E3F4');
  // Sin crédito no se ofrece la forma de pago por ítem (M5-10).
  await expect(page.getByTestId('statement-checkout')).toHaveCount(0);

  // Filtros: estado vencido y moneda.
  const consulta = page.waitForRequest((r) => r.method() === 'GET' && /\/api\/v1\/account-statement\?/.test(r.url()));
  await page.getByLabel('Estado', { exact: true }).selectOption('Overdue');
  await page.getByLabel('Moneda', { exact: true }).selectOption('CLP');
  await page.getByRole('button', { name: 'Buscar' }).click();
  const url = new URL((await consulta).url());
  expect(url.searchParams.get('status')).toBe('Overdue');
  expect(url.searchParams.get('currency')).toBe('CLP');
  await expect(page.getByTestId('statement-count')).toHaveText('1 línea');
  await expect(page.getByTestId('statement-group-Uninvoiced')).toContainText('No hay cargos pendientes de facturación con estos filtros.');
  // El resumen sigue mostrando toda la cuenta.
  await expect(clp.getByTestId('statement-overdue')).toHaveText(/595\.000/);

  // Exportación con los mismos filtros, en el idioma de la interfaz.
  const exportacion = page.waitForRequest((r) => r.url().includes('/api/v1/account-statement/export'));
  const archivo = page.waitForEvent('download');
  await page.getByTestId('statement-export-xlsx').click();
  const pedido = new URL((await exportacion).url());
  expect(pedido.searchParams.get('format')).toBe('xlsx');
  expect(pedido.searchParams.get('language')).toBe('es');
  expect(pedido.searchParams.get('status')).toBe('Overdue');
  expect((await archivo).suggestedFilename()).toBe('estado-de-cuenta-76123456-7-20261006.xlsx');
  await expect(page.locator(POLITE)).toContainText('Se descargó estado-de-cuenta-76123456-7-20261006.xlsx.');
});

test('dos facturas elegidas en el estado de cuenta van al carro en una sola transacción (M7-03, M5-01)', async ({ page }) => {
  await abrir(page, '/account-statement');
  const carro = page.getByTestId('navbar-cart');
  await expect(carro).toHaveAccessibleName('Carro de compra, 3 ítems');

  await page.getByLabel('Elegir 100245, Factura, BL HLCU0000001').check();
  await page.getByLabel('Elegir 100310, Factura, BL HLCUVAP260601930').check();
  await expect(page.getByTestId('statement-selection')).toHaveText('2 documentos elegidos.');

  const lote = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/cart/items/batch'));
  await page.getByRole('button', { name: 'Agregar 2 al carro' }).click();
  expect((await lote).postDataJSON()).toEqual({
    items: [
      { itemType: 'Invoice', sourceId: ITEM.FACTURA_VENCIDA },
      { itemType: 'Invoice', sourceId: ESTADO.FACTURA_POR_VENCER },
    ],
  });
  const resultado = page.getByTestId('statement-cart-result');
  await expect(resultado.getByRole('heading')).toHaveText('Se agregaron 2 documentos al carro.');
  await expect(resultado.getByRole('heading')).toBeFocused();
  await expect(carro).toHaveAccessibleName('Carro de compra, 5 ítems');
  await expect(page.getByTestId('statement-line-100245')).toContainText('En el carro');

  await resultado.getByRole('link', { name: 'Ir al carro y pagar' }).click();
  await expect(page).toHaveURL(/\/cart$/);
  await expect(page.getByTestId('cart-group-CL-CLP')).toContainText('HL-CL-2026-004310');
});

test('cliente con crédito: paga un ítem ahora e imputa otro a la línea de crédito (M5-10)', async ({ page }) => {
  await abrir(page, '/account-statement', { credito: true });
  await expect(page.getByTestId('statement-credit')).toContainText(/4\.168\.950/);
  await expect(page.getByTestId('statement-group-CreditImputed')).toContainText('CRI-20261003-C9D8E7F6');

  await page.getByTestId('statement-line-THC').getByRole('checkbox').check();
  await page.getByTestId('statement-line-GATE_OUT').getByRole('checkbox').check();
  const checkout = page.getByTestId('statement-checkout');
  const gateOut = checkout.getByTestId('checkout-item-GATE_OUT');
  await expect(gateOut.getByRole('radio', { name: 'Pagar ahora' })).toBeChecked();
  await gateOut.getByRole('radio', { name: 'Imputar a crédito' }).check();

  // El total se divide entre el pago inmediato y lo imputado a crédito.
  await expect(checkout.getByTestId('checkout-pay-now-totals')).toHaveText(/119\.000/);
  await expect(checkout.getByTestId('checkout-credit-totals')).toHaveText(/59\.500/);
  await expect(checkout).toContainText('Pago inmediato (1 ítem)');
  await expect(checkout).toContainText('A la línea de crédito (1 ítem)');

  await checkout.getByRole('radio', { name: 'Botón Banco de Chile, en línea' }).check();
  await checkout.getByRole('button', { name: 'Revisar 2 ítems' }).click();
  const confirmacion = checkout.getByTestId('checkout-confirm');
  await expect(confirmacion.getByRole('heading')).toBeFocused();
  await expect(confirmacion).toContainText('Pagará ahora 1 ítem en CLP con Botón Banco de Chile.');
  await expect(confirmacion).toContainText('Imputará 1 ítem a su línea de crédito.');

  const cierre = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/account-statement/checkout'));
  await confirmacion.getByRole('button', { name: 'Confirmar' }).click();
  const solicitud = await cierre;
  expect(solicitud.headers()['idempotency-key']).toMatch(/^[0-9a-f-]{36}$/);
  expect(solicitud.postDataJSON()).toEqual({
    items: [
      { itemType: 'LocalCharge', sourceId: ITEM.THC_CREDITO, billingTaxId: null, mode: 'PayNow' },
      { itemType: 'LocalCharge', sourceId: ESTADO.GATE_OUT, billingTaxId: null, mode: 'Credit' },
    ],
    paymentCurrency: 'CLP',
    paymentMethodCode: 'BANK_BUTTON_BCH',
  });

  const resultado = page.getByTestId('statement-result');
  await expect(resultado.getByRole('heading', { level: 2 })).toBeFocused();
  await expect(resultado.getByTestId('statement-result-credit')).toContainText('1 ítem imputado a la línea de crédito');
  await expect(resultado.getByTestId('statement-result-payment')).toContainText('Pago PAY-20261006-0000AC01 creado');
  // Lo imputado queda como saldo pendiente del estado de cuenta.
  await expect(page.getByTestId('statement-group-CreditImputed')).toContainText('GATE_OUT');
  await resultado.getByRole('button', { name: 'Continuar al pago' }).click();
  await expect(page).toHaveURL(/\/payments\/[^/]+\/result$/);
});

test('comprobante de depósito: rechazo con motivo, nuevo comprobante y verificación de Finanzas (M5-06)', async ({ page }) => {
  await abrir(page, `/payment-history/${PAGO_DEPOSITO.SIN_COMPROBANTE}`);
  const seccion = page.getByTestId('deposit-proofs');
  await expect(seccion.getByTestId('deposit-proof-awaiting')).toBeVisible();

  // Sin archivo, el resumen de errores enlaza el campo.
  await seccion.getByRole('button', { name: 'Enviar comprobante' }).click();
  await expect(seccion.getByTestId('deposit-proof-errors')).toBeFocused();
  await expect(seccion.locator('#proof-file')).toHaveAttribute('aria-invalid', 'true');

  await seccion.locator('#proof-file').setInputFiles(PDF);
  await seccion.getByLabel('Número de operación o referencia').fill('BCI-990011');
  await seccion.getByLabel('Monto depositado (CLP)').fill('119000');
  const carga = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/payments/${PAGO_DEPOSITO.SIN_COMPROBANTE}/deposit-proofs`));
  await seccion.getByRole('button', { name: 'Enviar comprobante' }).click();
  expect((await carga).postDataBuffer()?.toString('latin1')).toContain('BCI-990011');
  await expect(seccion.getByTestId('deposit-proof-submitted')).toBeVisible();
  await expect(seccion.getByTestId('deposit-proof-form')).toHaveCount(0);
  await expect(page.locator(POLITE)).toContainText('Se envió el comprobante comprobante.pdf.');

  // Finanzas rechaza con motivo.
  await sembrarSesionAdmin(page, 'es');
  await ir(page, '/admin/payments/deposit-proofs');
  await page.getByTestId('proof-row-PAY-20261003-9C8B7A6D').getByRole('button', { name: 'Revisar el comprobante del pago PAY-20261003-9C8B7A6D' }).click();
  await expect(page.locator('#proof-review-title')).toBeFocused();
  await page.getByLabel('Rechazar el comprobante').check();
  await page.getByRole('button', { name: 'Registrar la revisión' }).click();
  await expect(page.locator('#proof-review-reason')).toBeFocused();
  await page.locator('#proof-review-reason').fill('El comprobante no muestra el número de operación.');
  await page.getByRole('button', { name: 'Registrar la revisión' }).click();
  await expect(page.getByTestId('proof-queue-message')).toHaveText('Comprobante del pago PAY-20261003-9C8B7A6D rechazado: el cliente puede subir uno nuevo.');

  // El cliente ve el motivo y sube un comprobante nuevo.
  await sembrarSesion(page, { lang: 'es' });
  await ir(page, `/payment-history/${PAGO_DEPOSITO.SIN_COMPROBANTE}`);
  await expect(seccion.getByTestId('deposit-proof-rejected')).toContainText('Motivo: El comprobante no muestra el número de operación.');
  await expect(seccion.getByRole('heading', { name: 'Adjuntar un comprobante nuevo' })).toBeVisible();
  await seccion.locator('#proof-file').setInputFiles({ ...PDF, name: 'comprobante-2.pdf' });
  await seccion.getByLabel('Número de operación o referencia').fill('BCI-990012');
  await seccion.getByRole('button', { name: 'Enviar comprobante' }).click();
  await expect(seccion.getByTestId('deposit-proof-submitted')).toBeVisible();
  await expect(seccion.getByTestId('deposit-proof-Rejected')).toHaveCount(1);
  await expect(seccion.getByTestId('deposit-proof-Submitted')).toHaveCount(1);

  // Finanzas verifica: el pago queda confirmado.
  await sembrarSesionAdmin(page, 'es');
  await ir(page, '/admin/payments/deposit-proofs');
  await page.getByTestId('proof-row-PAY-20261003-9C8B7A6D').getByRole('button', { name: /^Revisar el comprobante/ }).click();
  await page.getByRole('button', { name: 'Registrar la revisión' }).click();
  await expect(page.getByTestId('proof-queue-message')).toHaveText(
    'Comprobante del pago PAY-20261003-9C8B7A6D verificado: el pago quedó confirmado (comprobante RCP-20261006-7F7F7F7F).');

  await sembrarSesion(page, { lang: 'es' });
  await ir(page, `/payment-history/${PAGO_DEPOSITO.SIN_COMPROBANTE}`);
  await expect(seccion.getByTestId('deposit-proof-verified')).toBeVisible();
});

test('refacturación IAO desde la factura: datos, aprobación y envío (M3-11)', async ({ page }) => {
  await abrir(page, '/invoices');
  await page.getByRole('link', { name: 'Refacturar la factura 100245 a otra razón social' }).click();
  await expect(page).toHaveURL(new RegExp(`/reinvoicing/new\\?invoiceId=${ITEM.FACTURA_VENCIDA}$`));
  await expect(page.locator('[aria-current="step"]')).toHaveText('Paso 1: Nueva razón social');
  await expect(page.getByTestId('reinvoicing-quote-total')).toHaveText(/124\.750/);

  // El RUT de la nueva razón social debe ser distinto del facturado.
  await page.getByLabel('RUT de la nueva razón social (obligatorio)').fill('76.123.456-7');
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  const errores = page.getByTestId('reinvoicing-errors');
  await expect(errores).toBeFocused();
  await expect(errores).toContainText('El RUT de la nueva razón social debe ser distinto del RUT facturado.');
  await expect(errores).toContainText('Ingrese la razón social.');

  await page.getByLabel('RUT de la nueva razón social (obligatorio)').fill('78.555.444-2');
  await page.getByLabel('Razón social (obligatoria)').fill('Logística Sur SpA');
  await page.getByLabel('Dirección (obligatoria)').fill('Av. Pedro Montt 120, Valparaíso');
  await page.getByLabel('Correo de facturación (obligatorio)').fill('facturas@logisticasur.cl');
  const creacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/reinvoicing'));
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  expect((await creacion).postDataJSON()).toMatchObject({
    invoiceId: ITEM.FACTURA_VENCIDA,
    billing: { taxId: '78.555.444-2', name: 'Logística Sur SpA', email: 'facturas@logisticasur.cl' },
    acceptorEmail: null,
  });
  await expect(page).toHaveURL(/\/reinvoicing\/s9000000-/);
  await expect(page.locator('[aria-current="step"]')).toHaveText('Paso 2: Aprobación y envío');

  // Sin aprobación ni aceptación del cobro no se envía.
  await page.getByRole('button', { name: 'Enviar la refacturación' }).click();
  const envioErrores = page.getByTestId('reinvoicing-submit-errors');
  await expect(envioErrores).toBeFocused();
  await expect(envioErrores).toContainText('Adjunte la aprobación de la nueva razón social.');
  await expect(envioErrores).toContainText('Acepte el cobro para enviar.');

  await page.locator('#reinvoicing-approval-file').setInputFiles({ ...PDF, name: 'aprobacion-logistica-sur.pdf' });
  await page.getByRole('button', { name: 'Adjuntar la aprobación' }).click();
  await expect(page.getByTestId('reinvoicing-approvals')).toContainText('aprobacion-logistica-sur.pdf');
  await page.getByLabel(/Acepto el cobro de la refacturación y de la pérdida de IVA por un total de/).check();
  const envio = page.waitForRequest((r) => r.method() === 'POST' && /\/reinvoicing\/[^/]+\/submit$/.test(r.url()));
  await page.getByRole('button', { name: 'Enviar la refacturación' }).click();
  expect((await envio).postDataJSON()).toEqual({ acceptTariff: true, acceptedTotal: 124750 });
  await expect(page.getByTestId('reinvoicing-message')).toContainText('Se envió el enlace de aceptación a facturas@logisticasur.cl.');
  await expect(page.locator('[aria-current="step"]')).toHaveText('Paso 3: Pago y aceptación');
  await expect(page.getByTestId('reinvoicing-acceptance-status')).toHaveText('Aceptación pendiente');
  await expect(page.getByTestId('service-payment')).toContainText('Refacturación IAO');
  await expect(page.getByTestId('service-payment')).toContainText('Pérdida de IVA');
});

test('la nueva razón social acepta el cobro por el enlace público, sin sesión (M3-11)', async ({ page }) => {
  await abrir(page, `/reinvoicing/acceptance/${TOKEN_ACEPTACION}`, {}, 'ninguna');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Aceptación de cobro por refacturación');
  await expect(page.getByTestId('acceptance-new-entity')).toHaveText(`${NUEVA_RAZON.name} (${NUEVA_RAZON.taxId})`);
  await expect(page.getByTestId('acceptance-total')).toHaveText(/52\.550/);
  await expect(page.getByTestId('acceptance-status')).toHaveText('Aceptación pendiente');

  await page.getByRole('button', { name: 'Enviar respuesta' }).click();
  await expect(page.getByTestId('acceptance-errors')).toBeFocused();
  await expect(page.getByTestId('acceptance-errors')).toContainText('Elija si acepta o rechaza el cobro.');

  await page.getByLabel(/^Acepto el cobro por un total de/).check();
  await page.getByLabel('Su nombre (obligatorio)').fill('María Soto');
  await page.getByLabel('RUT de la empresa (obligatorio)').fill('76.123.456-7');
  await page.getByRole('button', { name: 'Enviar respuesta' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'Ingrese el RUT de la nueva razón social.' })).toBeVisible();

  await page.getByLabel('RUT de la empresa (obligatorio)').fill('77.888.999-1');
  const respuesta = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/reinvoicing/acceptance/${TOKEN_ACEPTACION}`));
  await page.getByRole('button', { name: 'Enviar respuesta' }).click();
  expect((await respuesta).postDataJSON()).toEqual({ accept: true, name: 'María Soto', taxId: '77.888.999-1', reason: null });
  await expect(page.getByRole('heading', { name: 'Cobro aceptado' })).toBeFocused();
  await expect(page.getByTestId('acceptance-status')).toHaveText('Aceptada');
  await expect(page.locator(POLITE)).toContainText('Aceptación de la solicitud SRV-20261005-5E1A0009 registrada.');
  await expect(page.getByTestId('acceptance-form')).toHaveCount(0);
});

test('facturas: cubierta por el recibo del anticipo y refacturación vinculada (M3-19, M7-01, M3-11)', async ({ page }) => {
  await abrir(page, '/invoices');
  const cubierta = page.getByTestId('invoice-row-100318');
  await expect(cubierta.getByTestId('invoice-covered-by')).toHaveText(`Cubierta por el recibo ${RECIBO_ANTICIPO} (pago PAY-20261001-D4E5F6A7)`);
  await expect(cubierta).toContainText('Pagada');
  await expect(cubierta.getByRole('link', { name: 'Ver el recibo en los documentos del embarque' })).toHaveAttribute('href', '/shipments/HLCUSAI260701810/documents');

  const reemplazada = page.getByTestId('invoice-row-100198');
  await expect(reemplazada).toContainText('Reemplazada');
  await expect(reemplazada.getByRole('link', { name: /^Refacturar/ })).toHaveCount(0);
  await reemplazada.getByRole('button', { name: 'Reemplazada por la factura 100402' }).click();
  await expect(page.getByTestId('invoice-row-100402')).toBeFocused();
  await expect(page.getByTestId('invoice-row-100402').getByTestId('invoice-supersedes')).toHaveText('Reemplaza a la factura 100198');
  await expect(page.getByTestId('invoices-statement-link')).toHaveAttribute('href', '/account-statement');
});

test('la agencia de aduanas ve cómo funciona el pago anticipado del Gate Out de exportación (M3-19)', async ({ page }) => {
  await simularApi(page, { features: FASE2 });
  await sembrarSesion(page, { lang: 'es', organizacion: { ...ORGANIZACION_PRUEBA, organizationType: 'CustomsAgency' } });
  await ir(page, `/charges/${BL_GATE_OUT_POR_PAGAR}`);
  const aviso = page.getByTestId('charges-gate-out-advance');
  await expect(aviso.getByRole('heading')).toHaveText('Pago anticipado de Gate Out');
  await expect(aviso).toContainText('el portal emite un recibo con el embarque, las unidades y el pagador');

  // Un cliente que no es agencia de aduanas no ve el aviso.
  await sembrarSesion(page, { lang: 'es' });
  await ir(page, `/charges/${BL_GATE_OUT_POR_PAGAR}`);
  await expect(page.getByRole('cell', { name: 'Gate Out', exact: true })).toBeVisible();
  await expect(page.getByTestId('charges-gate-out-advance')).toHaveCount(0);
});

test('dashboard con la solicitud de servicio y filtro de servicios desde el servidor (Ola G)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const gestion = page.getByRole('link', { name: 'Ver Solicitud de servicio SRV-20261005-5E1A0002' });
  await expect(gestion).toHaveAttribute('href', '/service-requests/s9000000-0000-4000-8000-000000000002');
  await expect(page.getByRole('link', { name: /Ver mi estado de cuenta/ })).toBeVisible();

  const definiciones = page.waitForRequest((r) => r.url().endsWith('/api/v1/service-requests/definitions'));
  await ir(page, '/service-requests');
  await definiciones;
  const servicio = page.getByLabel('Servicio', { exact: true });
  await expect(servicio.locator('option', { hasText: 'Refacturación IAO' })).toHaveCount(1);
  await expect(servicio.locator('option', { hasText: 'Gestión de sellos' })).toHaveCount(1);
});
