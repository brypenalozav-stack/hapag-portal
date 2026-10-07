import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { FASE2 } from '../fixtures/funcionalidades';
import {
  BL_BO_SIN_UNIDADES,
  BL_CAMBIO_PAGADO,
  BL_DROP_OFF,
  BL_HIJO,
  BL_SELLOS,
  BL_XOM,
  BOOKING_SELLOS,
  idDefinicion,
} from '../fixtures/ola-g-mocks';
import type { ServiceDefinitionInput, ServiceRequestDetail } from '../../src/app/core/models/service-request.model';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';
import { cargarSeccionesDiferidas } from '../fixtures/detalle';

/**
 * Fase 2, Ola G (pruebas funcionales con el backend simulado):
 * - M3-09: Drop Off con contenedores, datos de facturación y aceptación de la tarifa → pendiente de aprobación ED; el
 *   equipo ED la aprueba en la bandeja interna y el cliente ve el cargo pendiente de pago y lo agrega al carro (M5-01).
 * - M3-13: la cotización del BL hijo fuera de plazo muestra el tramo vigente, las horas desde el hito y el total.
 * - M3-07: la solicitud de sellos exige los datos de facturación antes de habilitar el pago.
 * - M3-10: XOM no se ofrece fuera de Bolivia; en Bolivia sin contenedores aparece no disponible con el motivo.
 * - M3-06: el historial de cambios de almacén muestra la razón social y el RUT de quien pagó.
 * - M2-03 / M2-04: el mantenedor agrega un campo de selección al formulario de un servicio y lo guarda (NF-15).
 */

const POLITE = 'div[aria-live="polite"]';

async function abrir(page: Page, ruta: string, sesion: 'cliente' | 'admin' = 'cliente'): Promise<void> {
  await simularApi(page, { features: FASE2 });
  if (sesion === 'admin') await sembrarSesionAdmin(page, 'es');
  else await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  // Detalle del BL: los grupos bajo el pliegue se cargan al entrar en pantalla (@defer on viewport).
  if (/^\/shipments\/[^/?]+$/.test(ruta)) await cargarSeccionesDiferidas(page);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

/** Navega dentro de la misma simulación (el estado del backend simulado se conserva). */
async function ir(page: Page, ruta: string): Promise<void> {
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('Drop Off: solicitud con contenedores y tarifa aceptada, aprobación ED y pago desde el carro (M3-09, M5-01)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_DROP_OFF}`);

  // Servicios disponibles del BL, con la estimación del cobro.
  const seccion = page.getByTestId('available-services');
  await expect(seccion.getByRole('heading', { name: 'Servicios disponibles' })).toBeVisible();
  const dropOff = seccion.getByTestId('available-service-DROP_OFF_SCL');
  await expect(dropOff).toContainText('Lo aprueba: Equipo ED.');
  await expect(dropOff).toContainText('Pide aceptar la tarifa antes de enviar.');
  await expect(dropOff.getByTestId('available-quote-DROP_OFF_SCL')).toContainText(/428\.400/);
  await dropOff.getByRole('link', { name: 'Solicitar Drop Off SCL' }).click();
  await expect(page).toHaveURL(new RegExp(`/service-requests/new\\?bl=${BL_DROP_OFF}&code=DROP_OFF_SCL$`));
  await expect(page.getByTestId('srv-form')).toBeVisible();
  await expect(page.locator('[aria-current="step"]')).toHaveText('Paso 1: Datos del servicio');

  // Paso 1: sin datos, el resumen de errores enlaza cada campo.
  await page.getByRole('button', { name: 'Continuar' }).click();
  const resumen = page.getByTestId('srv-error-summary');
  await expect(resumen).toBeFocused();
  await expect(resumen).toContainText('Seleccione al menos un contenedor en Contenedores.');
  await expect(resumen).toContainText('Ingrese Fecha de devolución.');
  await expect(resumen).toContainText('Seleccione Depósito de devolución.');
  await resumen.getByRole('link', { name: 'Ingrese Fecha de devolución.' }).click();
  await expect(page.locator('#srv-field-returnDate')).toBeFocused();
  await expect(page.locator('#srv-field-returnDate')).toHaveAttribute('aria-invalid', 'true');

  await page.getByLabel('HLXU3034003 (40HC)').check();
  await page.locator('#srv-field-returnDate').fill('2026-10-20');
  await page.locator('#srv-field-depot').selectOption('SCL_QUILICURA');
  await page.getByRole('button', { name: 'Continuar' }).click();

  // Paso 2: facturación, con los datos de la organización ya cargados.
  await expect(page.locator('#srv-step-title')).toBeFocused();
  await expect(page.locator('#srv-step-title')).toHaveText('Datos de facturación');
  await expect(page.locator('#srv-billing-taxId')).toHaveValue('76.123.456-7');
  await page.locator('#srv-billing-address').fill('Av. Apoquindo 4500, Las Condes');
  await page.getByRole('button', { name: 'Continuar' }).click();

  // Paso 3: revisión con la tarifa vigente; sin aceptarla no se envía.
  await expect(page.locator('#srv-step-title')).toHaveText('Revisión y envío');
  await expect(page.getByTestId('quote-total')).toContainText(/214\.200/);
  await expect(page.getByTestId('srv-review-billing')).toContainText('76.123.456-7');
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  await expect(page.getByTestId('srv-error-summary')).toContainText('Debe aceptar la tarifa para enviar la solicitud.');
  await expect(page.locator('#srv-accept-tariff')).toHaveAttribute('aria-invalid', 'true');

  await page.getByLabel(/Acepto la tarifa del servicio por un total de/).check();
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/service-requests'));
  const respuesta = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/api/v1/service-requests'));
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  expect((await envio).postDataJSON()).toMatchObject({
    definitionCode: 'DROP_OFF_SCL',
    blNumber: BL_DROP_OFF,
    submit: true,
    acceptTariff: true,
    acceptedTotal: 214200,
    inputValues: { containers: ['HLXU3034003'], returnDate: '2026-10-20', depot: 'SCL_QUILICURA' },
    billing: { taxId: '76.123.456-7', address: 'Av. Apoquindo 4500, Las Condes' },
  });
  const creada = (await (await respuesta).json()) as ServiceRequestDetail;

  await expect(page.locator('#srv-result-title')).toBeFocused();
  await expect(page.getByTestId('srv-result-status')).toHaveText('Pendiente de aprobación');
  await expect(page.getByTestId('srv-result')).toContainText('La revisará Equipo ED.');
  await expect(page.locator(POLITE)).toContainText(`Solicitud ${creada.requestNumber} enviada. Estado: Pendiente de aprobación.`);

  // Equipo ED: la solicitud está en la bandeja y la aprueba.
  await sembrarSesionAdmin(page, 'es');
  await ir(page, '/admin/service-requests');
  const fila = page.getByTestId(`queue-row-${creada.requestNumber}`);
  await expect(fila).toContainText('Pendiente de aprobación');
  await expect(fila).toContainText('Equipo ED');
  await fila.getByRole('link', { name: `Revisar la solicitud ${creada.requestNumber}` }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Revisar la solicitud ${creada.requestNumber}`);
  const aprobacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/admin/service-requests/${creada.id}/approve`));
  await page.getByRole('button', { name: 'Aprobar la solicitud' }).click();
  await aprobacion;
  await expect(page.locator('#srv-review-done')).toHaveText(`Solicitud ${creada.requestNumber} aprobada.`);
  await expect(page.getByTestId('srv-status')).toHaveText('Pendiente de pago');

  // Cliente: ve el cargo pendiente de pago y lo agrega al carro.
  await sembrarSesion(page, { lang: 'es' });
  await ir(page, `/service-requests/${creada.id}`);
  await expect(page.getByTestId('srv-status')).toHaveText('Pendiente de pago');
  await expect(page.getByTestId('service-timeline')).toContainText('Aprobada');
  const carro = page.getByTestId('navbar-cart');
  await expect(carro).toHaveAccessibleName('Carro de compra, 3 ítems');
  await page.getByTestId('service-payment-add').click();
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await expect(dialogo.locator('#add-to-cart-billing')).toBeVisible();
  await expect(dialogo).toContainText(/214\.200/);
  const agregar = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/cart/items'));
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  expect((await agregar).postDataJSON()).toMatchObject({ itemType: 'LocalCharge', billingTaxId: '76123456-7', paymentCurrency: 'CLP' });
  await expect(dialogo).toHaveCount(0);
  await expect(page.getByTestId('service-payment-in-cart')).toHaveText('En el carro');
  await expect(carro).toHaveAccessibleName('Carro de compra, 4 ítems');
});

test('BL hijo fuera de plazo: la cotización muestra el tramo vigente y las horas desde el hito (M3-13)', async ({ page }) => {
  await abrir(page, `/service-requests/new?bl=${BL_HIJO}&code=BL_HOUSE_TRANSMISSION`);
  await expect(page.getByTestId('srv-shipment')).toContainText(`BL ${BL_HIJO}`);
  await page.locator('#srv-field-houseBlNumbers').fill('HB-2610-01, HB-2610-02');
  await page.getByRole('button', { name: 'Continuar' }).click();
  await page.locator('#srv-billing-address').fill('Av. Apoquindo 4500, Las Condes');
  const cotizacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/service-requests/quote'));
  await page.getByRole('button', { name: 'Continuar' }).click();
  expect((await cotizacion).postDataJSON()).toEqual({
    blNumber: BL_HIJO,
    definitionCode: 'BL_HOUSE_TRANSMISSION',
    inputValues: { houseBlNumbers: 'HB-2610-01, HB-2610-02' },
  });

  const quote = page.getByTestId('service-quote');
  await expect(quote.getByTestId('quote-timing')).toHaveText('Fuera de plazo');
  await expect(quote.getByTestId('quote-tariff')).toContainText('FUERA_PLAZO');
  await expect(quote.getByTestId('quote-measure')).toHaveText('117 (Horas)');
  await expect(quote.getByTestId('quote-tier')).toContainText('De 73 a 117 (Horas)');
  await expect(quote.getByTestId('quote-total')).toContainText(/USD\s250,00/);
  await expect(quote).toContainText('Zarpe de la nave (ETD)');
  // El BL hijo no exige aceptar la tarifa: no hay casilla de aceptación.
  await expect(page.locator('#srv-accept-tariff')).toHaveCount(0);
  await expect(page.locator(POLITE)).toHaveText('Valor calculado. Revise el total antes de enviar.');
});

test('Sellos: se exigen los datos de facturación antes de habilitar el pago (M3-07)', async ({ page }) => {
  await abrir(page, `/service-requests/new?booking=${BOOKING_SELLOS}&code=SEAL_MANAGEMENT`);
  await expect(page.getByTestId('srv-shipment')).toContainText(`BL ${BL_SELLOS}`);
  await expect(page.getByTestId('srv-shipment')).toContainText(`booking ${BOOKING_SELLOS}`);
  await page.getByLabel('HLXU2603061 (40HC)').check();
  await page.locator('#srv-field-sealNumbers').fill('SL-100, SL-101');
  await page.getByRole('button', { name: 'Continuar' }).click();

  // Facturación incompleta o con formato incorrecto: no se avanza.
  await page.locator('#srv-billing-taxId').fill('');
  await page.locator('#srv-billing-email').fill('facturacion');
  await page.getByRole('button', { name: 'Continuar' }).click();
  const resumen = page.getByTestId('srv-error-summary');
  await expect(resumen).toBeFocused();
  await expect(resumen).toContainText('Ingrese el RUT o NIT de facturación.');
  await expect(resumen).toContainText('Ingrese la dirección.');
  await expect(resumen).toContainText('El correo no es válido. Use el formato nombre@empresa.cl.');
  await expect(page.locator('#srv-billing-taxId')).toHaveAttribute('aria-invalid', 'true');
  await expect(page.locator('#srv-step-title')).toHaveText('Datos de facturación');

  await page.locator('#srv-billing-taxId').fill('76.123.456-7');
  await page.locator('#srv-billing-address').fill('Av. Apoquindo 4500, Las Condes');
  await page.locator('#srv-billing-email').fill('facturacion@andes.cl');
  await page.getByRole('button', { name: 'Continuar' }).click();
  await expect(page.getByTestId('quote-total')).toContainText(/14\.280/);

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/service-requests'));
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  expect((await envio).postDataJSON()).toMatchObject({
    definitionCode: 'SEAL_MANAGEMENT',
    bookingNumber: BOOKING_SELLOS,
    billing: { taxId: '76.123.456-7', name: 'Importadora Andes SpA', address: 'Av. Apoquindo 4500, Las Condes', email: 'facturacion@andes.cl' },
    inputValues: { containers: ['HLXU2603061'], sealNumbers: 'SL-100, SL-101' },
  });
  await expect(page.getByTestId('srv-result-status')).toHaveText('Pendiente de pago');
  await expect(page.getByTestId('service-payment')).toContainText('Gestión de sellos');
  await expect(page.getByTestId('service-payment-add')).toBeVisible();
});

test('XOM: no se ofrece fuera de Bolivia y sin contenedores aparece no disponible con el motivo (M3-10)', async ({ page }) => {
  // Chile: XOM no está entre los servicios del BL, ni siquiera entre los no disponibles.
  await abrir(page, `/service-requests/new?bl=${BL_DROP_OFF}`);
  const seccion = page.getByTestId('available-services');
  await seccion.getByLabel('Mostrar también los no disponibles').check();
  await expect(seccion.getByTestId('available-service-DROP_OFF_SCL')).toBeVisible();
  await expect(seccion.getByTestId('available-service-XOM')).toHaveCount(0);

  // Bolivia sin contenedores: no disponible, con el motivo.
  await ir(page, `/service-requests/new?bl=${BL_BO_SIN_UNIDADES}`);
  await expect(page.getByTestId('available-service-XOM')).toHaveCount(0);
  await page.getByLabel('Mostrar también los no disponibles').check();
  const xom = page.getByTestId('available-service-XOM');
  await expect(xom).toContainText('No disponible');
  await expect(xom.getByTestId('available-reasons-XOM')).toContainText('El embarque no tiene contenedores para este servicio.');
  await expect(xom.getByRole('link', { name: /Solicitar/ })).toHaveCount(0);

  // Bolivia con contenedores: se cobra en bolivianos y no se cobra el contenedor SOC.
  await ir(page, `/service-requests/new?bl=${BL_XOM}`);
  const disponible = page.getByTestId('available-service-XOM');
  await expect(disponible).toContainText('Disponible');
  await expect(disponible.getByTestId('available-quote-XOM')).toContainText(/BOB\s350,00/);
  await disponible.getByRole('link', { name: 'Solicitar Administración de contenedor (XOM)' }).click();
  await expect(page.getByText('Contenedor del embarcador (SOC)')).toBeVisible();
});

test('Historial de cambios de almacén con el RUT de quien pagó y su trazabilidad (M3-06)', async ({ page }) => {
  await abrir(page, '/warehouse');
  await page.getByRole('link', { name: 'Ver historial de cambios' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Historial de cambios de almacén');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);

  const pagado = page.getByTestId(`wh-history-${BL_CAMBIO_PAGADO}`);
  await expect(pagado.getByTestId('wh-history-payer')).toContainText('Importadora Andes SpA');
  await expect(pagado.getByTestId('wh-history-payer')).toContainText('RUT o NIT 76123456-7');
  await expect(pagado).toContainText('Completada');
  await expect(page.getByTestId('wh-history-HLCUSAI260401020')).toContainText('No requiere pago');
  await expect(page.getByTestId('wh-history-HLCUSAI260401020')).toContainText('Solicitud masiva, línea 1');

  // Filtro por BL: el servidor recibe el BL o booking.
  const consulta = page.waitForRequest((r) => r.url().includes('/api/v1/warehouse-changes/history?') && r.url().includes(`blNumber=${BL_CAMBIO_PAGADO}`));
  const filtros = page.getByRole('search', { name: 'Filtros del historial' });
  await filtros.getByLabel('BL o booking').fill(BL_CAMBIO_PAGADO);
  await filtros.getByRole('button', { name: 'Buscar' }).click();
  await consulta;
  await expect(page.locator('main table tbody tr')).toHaveCount(1);

  await pagado.getByRole('link', { name: /Ver el detalle del cambio del BL HLCUSAI260400910/ }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Cambio de almacén del BL ${BL_CAMBIO_PAGADO}`);
  await expect(page.getByTestId('wh-trace-payer')).toContainText('RUT o NIT 76123456-7');
  const traza = page.getByTestId('wh-trace-timeline');
  await expect(traza).toContainText('Cambio de estado del pago');
  await expect(traza).toContainText('PAY-20261002-7A8B9C0D');
  await expect(traza).toContainText('RCP-20261002-7A8B9C0D');
});

test('El mantenedor agrega un campo de selección al formulario del servicio y lo guarda (M2-03, NF-15)', async ({ page }) => {
  await abrir(page, `/admin/service-definitions/${idDefinicion('DROP_OFF_SCL')}`, 'admin');
  await expect(page.locator('#def-code')).toHaveValue('DROP_OFF_SCL');
  await expect(page.getByTestId('definition-history').locator('tbody tr')).toHaveCount(1);

  await page.getByRole('button', { name: 'Agregar campo' }).click();
  const clave = page.locator('#def-field-4-key');
  await expect(clave).toBeFocused();
  await expect(page.locator(POLITE)).toHaveText('Se agregó el campo 5.');

  // Sin datos, el resumen de errores lo indica.
  await page.getByRole('button', { name: 'Guardar cambios' }).click();
  await expect(page.locator('form .alert-danger').first()).toBeFocused();
  await expect(page.locator('form .alert-danger').first()).toContainText('Campo 5: la clave debe empezar con minúscula');

  await clave.fill('urgency');
  await page.locator('#def-field-4-type').selectOption('select');
  await page.locator('#def-field-4-label-es').fill('Urgencia');
  await page.locator('#def-field-4-label-en').fill('Urgency');
  await page.locator('#def-field-4-required').check();
  await page.locator('#def-field-4-option-0-value').fill('HIGH');
  await page.locator('#def-field-4-option-0-label-es').fill('Alta');
  await page.locator('#def-field-4-option-0-label-en').fill('High');
  await page.getByRole('button', { name: 'Agregar una opción al campo urgency' }).click();
  await expect(page.locator('#def-field-4-option-1-value')).toBeFocused();
  await page.locator('#def-field-4-option-1-value').fill('NORMAL');
  await page.locator('#def-field-4-option-1-label-es').fill('Normal');
  await page.locator('#def-field-4-option-1-label-en').fill('Normal');

  // Reordenar: el campo nuevo sube un lugar.
  await page.getByRole('button', { name: 'Subir el campo urgency' }).click();
  await expect(page.locator(POLITE)).toHaveText('El campo urgency quedó en la posición 4 de 5.');
  await expect(page.locator('#def-field-3-key')).toHaveValue('urgency');

  const guardado = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith(`/api/v1/service-definitions/${idDefinicion('DROP_OFF_SCL')}`));
  await page.getByRole('button', { name: 'Guardar cambios' }).click();
  const body = (await guardado).postDataJSON() as ServiceDefinitionInput;
  expect(body.code).toBe('DROP_OFF_SCL');
  expect(body.inputSchema.map((f) => f.key)).toEqual(['containers', 'returnDate', 'depot', 'urgency', 'observations']);
  expect(body.inputSchema[3]).toMatchObject({
    key: 'urgency',
    type: 'select',
    required: true,
    labelEs: 'Urgencia',
    labelEn: 'Urgency',
    options: [
      { value: 'HIGH', labelEs: 'Alta', labelEn: 'High' },
      { value: 'NORMAL', labelEs: 'Normal', labelEn: 'Normal' },
    ],
  });
  expect(body).toMatchObject({ approvalTeam: 'ED', tariffAcceptanceRequired: true, quantityMode: 'PerContainer' });

  await expect(page.getByTestId('definition-saved')).toHaveText('Cambios guardados.');
  const historial = page.getByTestId('definition-history').locator('tbody tr');
  await expect(historial).toHaveCount(2);
  await expect(historial.first()).toContainText('Modificación');
  await expect(historial.first()).toContainText('urgency (Selección)');
});

test('Dashboard y Mis solicitudes: acceso rápido, filtro por estado y envío de un borrador (M1-01, M3-14)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const servicios = page.getByRole('navigation', { name: '¿Qué necesita hacer?' });
  await expect(servicios.getByRole('link', { name: /Solicitar un servicio/ })).toHaveAttribute('href', '/service-requests/new');

  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await menu.getByRole('button', { name: 'Documentos y trámites' }).click();
  await menu.getByRole('link', { name: 'Mis solicitudes' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Mis solicitudes');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('request-row-SRV-20261005-5E1A0005')).toContainText('Rechazada');

  const consulta = page.waitForRequest((r) => r.url().includes('/api/v1/service-requests?') && r.url().includes('status=Draft'));
  await page.getByLabel('Estado').selectOption('Draft');
  await page.getByRole('search', { name: 'Filtros de las solicitudes' }).getByRole('button', { name: 'Buscar' }).click();
  await consulta;
  await expect(page.locator('main table tbody tr')).toHaveCount(1);
  await page.getByRole('link', { name: 'Ver la solicitud SRV-20261005-5E1A0008' }).click();

  // Borrador de matriz fuera de plazo: se continúa, se revisa la tarifa vigente y se envía.
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Solicitud SRV-20261005-5E1A0008');
  await page.getByRole('link', { name: 'Continuar la solicitud' }).click();
  await expect(page.locator('#srv-field-observations')).toHaveValue('Matriz enviada por correo el 29-09.');
  await page.getByRole('button', { name: 'Continuar' }).click();
  await expect(page.locator('#srv-billing-address')).toHaveValue('Av. Apoquindo 4500, Las Condes');
  await page.getByRole('button', { name: 'Continuar' }).click();
  await expect(page.getByTestId('quote-timing')).toHaveText('Fuera de plazo');
  await expect(page.getByTestId('quote-tier')).toContainText('De 6 a 10 (Días corridos)');

  const actualizacion = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith('/api/v1/service-requests/s9000000-0000-4000-8000-000000000008'));
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/service-requests/s9000000-0000-4000-8000-000000000008/submit'));
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  expect((await actualizacion).postDataJSON()).toMatchObject({ inputValues: { observations: 'Matriz enviada por correo el 29-09.' } });
  await envio;
  await expect(page.getByTestId('srv-result-status')).toHaveText('Pendiente de pago');
});
