import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import {
  BL_FLETE,
  BL_LIBERACION,
  CERTIFICADO_FLETE,
  CLAVE_NUEVA,
  CLAVE_ROTADA,
  CLIENTE_WS,
  COPIA_ENTREGADA,
  ENTREGA,
  SESION_ASISTENTE,
  SOLICITUD_CARTA,
  SOLICITUD_FLETE,
  TRANSPORTISTA_REGISTRADO,
  UNIDADES,
} from '../fixtures/ola-j-mocks';
import { USUARIO_PRUEBA, sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Fase 2, Ola J (pruebas funcionales con el backend simulado):
 * - M6-02: certificado de flete de Bolivia sin pago ni carro: datos precargados, error de la finalidad, emisión firmada
 *   con descarga y su registro en el repositorio y en las solicitudes.
 * - M6-08: carta de liberación y desconsolidado: unidades con su TATC (la que ya está en una carta pendiente no se puede
 *   elegir), empresa con transportista registrado, persona natural con datos del transportista y sus errores, seguimiento
 *   pendiente de aprobación y anulación; Customer Service ve el TATC y el Counter y, al aprobar, se emite la carta.
 * - M10-04: el asistente entrega una copia del BL para descargar (con el nombre del archivo del servidor), explica cuando
 *   el acceso se perdió al descargar y cuando el documento no está disponible para el usuario.
 * - M3-17: canal Web Service: alta con la clave mostrada una sola vez (desaparece al salir de la pantalla), rotación con
 *   período de gracia, revocación de una clave y del cliente con confirmación, y bitácora filtrada por resultado.
 */

const POLITE = 'div[aria-live="polite"]';
const ASSERTIVE = 'div[aria-live="assertive"]';

async function abrir(page: Page, ruta: string, sesion: 'cliente' | 'admin' = 'cliente'): Promise<void> {
  await simularApi(page);
  if (sesion === 'admin') await sembrarSesionAdmin(page, 'es');
  else await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('certificado de flete: sin pago ni carro, se emite firmado, se descarga y queda en el repositorio (M6-02)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_FLETE}/documents`);
  const panel = page.getByTestId('freight-panel');
  await expect(panel.getByTestId('freight-no-payment')).toContainText('Sin pago en esta entrega');
  await expect(panel.getByTestId('freight-amount')).toContainText('Flete que se certifica');
  await expect(panel.getByTestId(`freight-request-${SOLICITUD_FLETE.numero}`)).toContainText('Completada');
  await expect(page.getByTestId(`document-${CERTIFICADO_FLETE}`)).toContainText('Certificado de flete');

  await panel.getByTestId('freight-request').click();
  const dialogo = page.getByTestId('freight-dialog');
  await expect(dialogo.getByLabel('Nombre o razón social')).toHaveValue('Comercial Altiplano SRL');
  await expect(dialogo.getByLabel('NIT o documento de identidad')).toHaveValue('1023456017');
  await dialogo.getByTestId('freight-submit').click();
  await expect(dialogo.locator('.alert-danger').first()).toBeFocused();
  await expect(dialogo.locator('.alert-danger').first()).toContainText('Elija la finalidad del certificado.');

  await dialogo.getByLabel('Finalidad').selectOption('CUSTOMS');
  await dialogo.getByLabel('Dirigido a (opcional)').fill('Aduana Nacional de Bolivia');
  const solicitud = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_FLETE}/freight-certificate`));
  await dialogo.getByTestId('freight-submit').click();
  expect((await solicitud).postDataJSON()).toEqual({
    consigneeName: 'Comercial Altiplano SRL', consigneeTaxId: '1023456017', purpose: 'CUSTOMS',
    recipient: 'Aduana Nacional de Bolivia', notes: null, sendEmail: true,
  });
  const resultado = dialogo.getByTestId('freight-result');
  await expect(resultado.locator('h3')).toBeFocused();
  await expect(resultado.getByTestId('freight-result-text')).toContainText(`Se envió a: ${USUARIO_PRUEBA.email}.`);
  await expect(resultado.getByTestId('freight-result-signature')).toContainText('Firmado electrónicamente');
  const numero = (await resultado.getByTestId('freight-result-text').textContent())?.match(/CFL-\d{8}-\d{8}/)?.[0] ?? '';
  expect(numero).not.toBe('');
  await expect(page.locator(POLITE)).toContainText(`Se emitió el certificado ${numero}`);

  const archivo = page.waitForEvent('download');
  await dialogo.getByTestId('freight-download').click();
  expect((await archivo).suggestedFilename()).toBe(`${numero}.pdf`);

  await dialogo.getByRole('button', { name: 'Listo' }).click();
  await expect(page.getByTestId('documents-result')).toContainText(`Certificado de flete ${numero} emitido y firmado`);
  await expect(page.getByTestId(`document-${numero}`)).toBeVisible();
  await expect(panel.getByTestId('freight-requests').locator('tbody tr')).toHaveCount(2);
});

test('carta de liberación de una empresa con transportista registrado: TATC de las unidades y seguimiento pendiente (M6-08)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_LIBERACION}/documents`);
  await page.getByTestId('release-letter-open').click();
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_LIBERACION}/release-letter$`));
  await expect(page.getByTestId('release-provisional')).toContainText('provisorias hasta su validación con el área legal');

  // La unidad ya incluida en la carta pendiente no se puede elegir.
  await expect(page.locator(`#release-container-${UNIDADES.PENDIENTE}`)).toBeDisabled();
  await expect(page.getByTestId(`release-container-${UNIDADES.PENDIENTE}`)).toContainText(`Ya está en la solicitud ${SOLICITUD_CARTA.numero}`);
  await expect(page.getByTestId(`release-container-${UNIDADES.CON_TATC}`)).toContainText('TATC: Emitido (N.º TATC-BO-0045-02)');
  await page.locator(`#release-container-${UNIDADES.CON_TATC}`).check();
  await page.locator(`#release-container-${UNIDADES.SIN_TATC}`).check();
  await expect(page.getByTestId('release-selected-tatc')).toContainText('TATC de las 2 unidades elegidas');

  // Empresa: el domicilio, el representante y su documento son obligatorios.
  await expect(page.locator('#release-entity-COMPANY')).toBeChecked();
  await page.getByTestId('release-submit').click();
  const resumen = page.locator('form[data-testid="release-form"] .alert-danger').first();
  await expect(resumen).toBeFocused();
  await expect(resumen).toContainText('Ingrese el domicilio de la empresa.');
  await expect(resumen).toContainText('Ingrese el nombre del representante legal.');
  await expect(resumen).toContainText('Elija el transportista registrado.');

  await page.getByLabel('NIT', { exact: true }).fill('1023456017');
  await page.getByLabel('Domicilio', { exact: true }).fill('Av. Arce 2631, La Paz');
  await page.getByLabel('Representante legal', { exact: true }).fill('Marcela Quispe');
  await page.getByLabel('Documento del representante legal').fill('4876512 LP');
  await page.locator('#release-carrier-select').selectOption(TRANSPORTISTA_REGISTRADO.id);
  await page.getByLabel('Placa del camión (opcional)').fill('2345-ABC');

  const solicitud = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_LIBERACION}/release-letter`));
  await page.getByTestId('release-submit').click();
  expect((await solicitud).postDataJSON()).toEqual({
    containers: [UNIDADES.CON_TATC, UNIDADES.SIN_TATC], legalEntityType: 'COMPANY',
    consigneeName: 'Comercial Altiplano SRL', consigneeTaxId: '1023456017', consigneeAddress: 'Av. Arce 2631, La Paz',
    legalRepresentativeName: 'Marcela Quispe', legalRepresentativeId: '4876512 LP',
    carrierOrganizationId: TRANSPORTISTA_REGISTRADO.id, carrierName: null, carrierTaxId: null,
    driverName: null, driverId: null, truckPlate: '2345-ABC', observations: null,
  });

  // Seguimiento: pendiente de Customer Service, con el TATC registrado al enviar.
  await expect(page).toHaveURL(/\/release-letters\/ffffffff-0021-0021-0021-0000000000\d\d$/);
  await expect(page.locator(POLITE)).toContainText('queda pendiente de aprobación de Customer Service');
  await expect(page.getByTestId('release-pending')).toContainText('Pendiente de aprobación de Customer Service');
  const tatc = page.getByTestId('release-tatc-submission');
  await expect(tatc).toContainText('Emitido');
  await expect(tatc).toContainText(UNIDADES.SIN_TATC);
  await expect(page.getByTestId('release-letter-data')).toContainText('Registrado en el portal');
});

test('carta de liberación de una persona natural: datos exigidos y transportista con sus datos (M6-08)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_LIBERACION}/release-letter`);
  await page.locator('#release-entity-NATURAL_PERSON').check();
  await expect(page.locator('#release-rep-name')).toHaveCount(0);
  await expect(page.getByLabel('Domicilio (opcional)')).toBeVisible();
  await page.locator('#release-carrier-free').check();
  await page.locator(`#release-container-${UNIDADES.SIN_TATC}`).check();
  await page.getByTestId('release-submit').click();
  const resumen = page.locator('form[data-testid="release-form"] .alert-danger').first();
  await expect(resumen).toContainText('Ingrese el documento de identidad del consignatario.');
  await expect(resumen).toContainText('Ingrese el nombre o la razón social del transportista.');
  await expect(resumen).toContainText('Ingrese el NIT del transportista.');
  await expect(resumen).not.toContainText('representante');
  await expect(page.locator('#release-carrier-name')).toHaveAttribute('aria-invalid', 'true');

  // El enlace del resumen lleva al campo.
  await resumen.getByRole('link', { name: 'Ingrese el NIT del transportista.' }).click();
  await expect(page.locator('#release-carrier-tax-id')).toBeFocused();

  await page.getByLabel('Nombre completo').fill('Juan Mamani');
  await page.getByLabel('Documento de identidad (CI)').fill('6543210 LP');
  await page.getByLabel('Nombre o razón social del transportista').fill('Transportes Sajama');
  await page.getByLabel('NIT del transportista').fill('3344556017');
  const solicitud = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_LIBERACION}/release-letter`));
  await page.getByTestId('release-submit').click();
  const body = (await solicitud).postDataJSON();
  expect(body).toMatchObject({
    containers: [UNIDADES.SIN_TATC], legalEntityType: 'NATURAL_PERSON', consigneeName: 'Juan Mamani', consigneeTaxId: '6543210 LP',
    legalRepresentativeName: null, legalRepresentativeId: null, carrierOrganizationId: null,
    carrierName: 'Transportes Sajama', carrierTaxId: '3344556017',
  });
  await expect(page.getByTestId('release-letter-data')).toContainText('Persona natural');
  await expect(page.getByTestId('release-letter-data')).toContainText('Datos ingresados en la solicitud');
});

test('seguimiento de la carta: el cliente la anula antes de la aprobación (M6-08)', async ({ page }) => {
  await abrir(page, `/release-letters/${SOLICITUD_CARTA.id}`);
  await expect(page.getByTestId('release-pending')).toBeVisible();
  await expect(page.getByTestId('release-tatc-submission')).toContainText('No emitido');
  await page.getByRole('button', { name: 'Anular la solicitud' }).click();
  await expect(page.locator('#release-cancel-title')).toBeFocused();
  await page.getByLabel('Motivo (opcional)').fill('Se retirará en otra fecha.');
  const anulacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/service-requests/${SOLICITUD_CARTA.id}/cancel`));
  await page.getByRole('button', { name: 'Sí, anular' }).click();
  expect((await anulacion).postDataJSON()).toEqual({ reason: 'Se retirará en otra fecha.' });
  await expect(page.locator(POLITE)).toContainText(`Solicitud ${SOLICITUD_CARTA.numero} anulada.`);
  await expect(page.getByTestId('srv-status')).toHaveText('Anulada');
  await expect(page.getByTestId('release-pending')).toHaveCount(0);
});

test('Customer Service revisa la carta con el TATC y el Counter y, al aprobarla, se emite (M6-08, M8-09)', async ({ page }) => {
  await abrir(page, `/admin/service-requests/${SOLICITUD_CARTA.id}`, 'admin');
  const panel = page.getByTestId('srv-release-letter');
  await expect(panel.getByTestId('srv-release-consignee')).toContainText('Empresa');
  await expect(panel.getByTestId('srv-release-consignee')).toContainText('Marcela Quispe');
  await expect(panel.getByTestId('srv-release-carrier')).toContainText('Datos ingresados en la solicitud');
  await expect(panel.getByTestId('srv-release-carrier')).toContainText('Transportes Illimani SRL');
  await expect(panel.getByTestId('release-tatc-submission')).toContainText('No emitido');
  await expect(panel.getByTestId('release-tatc-now')).toContainText('TATC consultado ahora');
  await expect(panel.getByTestId('srv-release-counter')).toContainText('HBL recibido el');
  await expect(panel.getByTestId('srv-release-counter')).toContainText('Desconsolidado el');
  await expect(page.getByTestId('srv-release-approve-note')).toBeVisible();

  const aprobacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/admin/service-requests/${SOLICITUD_CARTA.id}/approve`));
  await page.getByLabel('Notas para el cliente (opcional)').fill('Unidad lista para retiro.');
  await page.getByRole('button', { name: 'Aprobar la solicitud' }).click();
  expect((await aprobacion).postDataJSON()).toEqual({ notes: 'Unidad lista para retiro.' });
  await expect(page.getByTestId('srv-status')).toHaveText('Completada');
  await expect(panel.getByTestId('srv-release-document')).toContainText(/Carta CLB-20261006-\d{8} emitida/);
  await expect(panel.getByTestId('release-tatc-approval')).toBeVisible();
  await expect(page.getByTestId('srv-resolution')).toContainText('Unidad lista para retiro. Carta CLB-');
});

test('el asistente entrega la copia del BL, explica el acceso perdido y lo que no está disponible (M10-04)', async ({ page }) => {
  await abrir(page, '/dashboard');
  await page.getByTestId('assistant-launcher').click();
  const panel = page.getByTestId('assistant-panel');
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(1);

  await panel.locator('#assistant-message').fill('envíame la copia no valorada y el certificado de transbordo del BL HLCU0000001');
  await panel.locator('#assistant-message').press('Enter');
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(2);
  const respuesta = panel.getByTestId('assistant-reply').last();
  await expect(respuesta.getByTestId('assistant-delivery-hint')).toContainText('2 documentos del repositorio del embarque');
  await expect(page.locator(POLITE)).toContainText('Hay 2 documentos para descargar.');

  // Descarga por la entrega de la conversación, con el nombre que informa el servidor.
  const descarga = page.waitForRequest((r) => r.url().endsWith(`/api/v1/assistant/sessions/${SESION_ASISTENTE}/deliveries/${ENTREGA.COPIA}/download`));
  const archivo = page.waitForEvent('download');
  await respuesta.getByRole('button', { name: `Descargar Copia de BL no valorada ${COPIA_ENTREGADA}` }).click();
  await descarga;
  expect((await archivo).suggestedFilename()).toBe(`${COPIA_ENTREGADA}.pdf`);
  await expect(page.locator(POLITE)).toContainText(`Descargar Copia de BL no valorada ${COPIA_ENTREGADA}`);

  // Los permisos se validan otra vez al descargar: el usuario perdió el acceso al documento.
  await respuesta.getByRole('button', { name: /Certificado de transbordo/ }).click();
  await expect(panel.locator('.alert-danger')).toContainText('no existe o perdió el acceso al embarque');
  await expect(page.locator(ASSERTIVE)).toContainText('perdió el acceso');

  // No disponible: la misma respuesta exista o no el documento.
  await panel.locator('#assistant-message').fill('envíame el comprobante collect del BL HLCUSAI260501240');
  await panel.locator('#assistant-message').press('Enter');
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(3);
  const denegada = panel.getByTestId('assistant-reply').last();
  await expect(denegada).toHaveAttribute('data-answer-type', 'NotAvailable');
  await expect(denegada.getByTestId('assistant-delivery-denied')).toContainText('La respuesta es la misma si el documento no existe');
  await expect(page.locator(POLITE)).toContainText('No hay documentos para descargar.');
});

test('canal Web Service: la clave se muestra una sola vez al crear el cliente y desaparece al salir (M3-17)', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await abrir(page, '/admin/api-clients', 'admin');
  await expect(page.getByTestId('api-channel-doc')).toContainText('docs/integraciones/ws-clientes.openapi.yaml');
  await page.getByTestId('api-client-new').click();
  await page.getByLabel('Organización cliente').fill('Andes');
  await page.getByTestId('api-client-org-search-button').click();
  await page.locator('input[name="api-client-org"]').first().check();
  await page.getByLabel('Nombre del cliente').fill('Integración de pruebas');
  await page.getByLabel('Cambio de almacén').check();
  await page.getByLabel('Límite por minuto').fill('120');
  const alta = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/admin/api-clients'));
  await page.getByTestId('api-client-submit').click();
  expect((await alta).postDataJSON()).toMatchObject({ name: 'Integración de pruebas', scopes: ['warehouse-change'], rateLimitPerMinute: 120, signatory: null });

  const secreto = page.getByTestId('api-client-secret');
  await expect(page.locator('#api-secret-title')).toBeFocused();
  await expect(secreto).toContainText('no se volverá a mostrar');
  await expect(secreto.getByTestId('api-client-secret-value')).toHaveValue(CLAVE_NUEVA);
  await secreto.getByTestId('api-client-secret-copy').click();
  await expect(page.locator(POLITE)).toContainText('Clave copiada al portapapeles.');
  expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(CLAVE_NUEVA);
  await expect(page.getByTestId('api-clients-table')).toContainText('Integración de pruebas');

  // Al salir de la pantalla la clave ya no se puede ver.
  await page.goto('/admin');
  await expect(page.getByTestId('admin-section-api-clients')).toBeVisible();
  await page.getByTestId('admin-section-api-clients').getByRole('link').click();
  await expect(page.locator('h1')).toHaveText('Canal Web Service');
  await expect(page.getByTestId('api-clients-table')).toBeVisible();
  await expect(page.getByTestId('api-client-secret')).toHaveCount(0);
  await expect(page.getByText(CLAVE_NUEVA)).toHaveCount(0);
});

test('canal Web Service: rotación con gracia, revocación de una clave y del cliente con confirmación, y bitácora (M3-17)', async ({ page }) => {
  await abrir(page, '/admin/api-clients', 'admin');
  await page.getByRole('button', { name: `Ver el detalle de ${CLIENTE_WS.nombre}` }).click();
  const detalle = page.getByTestId('api-client-detail');
  await expect(page.locator('#api-client-detail-title')).toBeFocused();

  // Bitácora filtrada por resultado.
  await expect(detalle.getByTestId('api-log-count')).toHaveText('Mostrando 5 de 5 solicitudes de esta página.');
  await detalle.getByLabel('Resultado').selectOption('Rejected');
  await expect(detalle.getByTestId('api-log-count')).toHaveText('Mostrando 1 de 5 solicitudes de esta página.');
  await expect(detalle.getByTestId('api-client-log')).toContainText('Error.Forbidden');

  // Rotación con 60 minutos de gracia: clave nueva mostrada una vez y la anterior con vencimiento.
  await detalle.getByLabel('Período de gracia de las claves actuales (minutos)').fill('60');
  const rotacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/admin/api-clients/${CLIENTE_WS.id}/keys/rotate`));
  await detalle.getByTestId('api-client-rotate-submit').click();
  expect((await rotacion).postDataJSON()).toEqual({ graceMinutes: 60 });
  await expect(page.getByTestId('api-client-secret-value')).toHaveValue(CLAVE_ROTADA);
  await expect(detalle.getByTestId(`api-key-${CLIENTE_WS.prefijo}`)).not.toContainText('Sin vencimiento');
  await page.getByTestId('api-client-secret-dismiss').click();
  await expect(page.getByTestId('api-client-secret')).toHaveCount(0);

  // Revocar una clave pide confirmación; "No, mantenerla" no envía nada.
  let revocaciones = 0;
  page.on('request', (r) => {
    if (r.method() === 'POST' && /\/keys\/[^/]+\/revoke$/.test(r.url())) revocaciones++;
  });
  await detalle.getByRole('button', { name: `Revocar la clave ${CLIENTE_WS.prefijo}` }).click();
  // La confirmación es un modal: abre con el foco en "No, mantenerla" y, al cerrarlo, el foco vuelve al botón.
  const modal = page.getByTestId('app-modal');
  const revocarClave = detalle.getByRole('button', { name: `Revocar la clave ${CLIENTE_WS.prefijo}` });
  await expect(modal.getByRole('heading')).toHaveText(`¿Revocar la clave ${CLIENTE_WS.prefijo}?`);
  await expect(modal.getByRole('button', { name: 'No, mantenerla' })).toBeFocused();
  await modal.getByRole('button', { name: 'No, mantenerla' }).click();
  await expect(modal).toBeHidden();
  await expect(revocarClave).toBeFocused();
  expect(revocaciones).toBe(0);
  await revocarClave.click();
  await modal.getByTestId('app-modal-confirm').click();
  await expect(page.locator(POLITE)).toContainText(`Clave ${CLIENTE_WS.prefijo} revocada.`);
  await expect(detalle.getByTestId(`api-key-${CLIENTE_WS.prefijo}`)).toContainText('Sin uso posible');
  expect(revocaciones).toBe(1);

  // Revocar el cliente: el motivo es obligatorio.
  await detalle.getByTestId('api-client-revoke').click();
  await detalle.getByTestId('api-client-revoke-yes').click();
  await expect(detalle.locator('#api-client-revoke-reason')).toBeFocused();
  await expect(detalle.getByTestId('api-client-revoke-confirm')).toContainText('Ingrese el motivo de la revocación.');
  await detalle.locator('#api-client-revoke-reason').fill('Contrato terminado.');
  const revocacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/admin/api-clients/${CLIENTE_WS.id}/revoke`));
  await detalle.getByTestId('api-client-revoke-yes').click();
  expect((await revocacion).postDataJSON()).toEqual({ reason: 'Contrato terminado.' });
  await expect(detalle.getByTestId('api-client-status')).toHaveText('Revocado');
  await expect(detalle.getByTestId('api-client-revoked')).toContainText('Contrato terminado.');
  await expect(detalle.getByTestId('api-client-rotate')).toHaveCount(0);
});
