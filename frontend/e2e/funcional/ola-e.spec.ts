import { test, expect, Page } from '@playwright/test';
import { simularApi } from '../fixtures/api-mocks';
import { RUT_PROPIO } from '../fixtures/ola-d-mocks';
import {
  BL_CARTA,
  BL_CLD_BLOQUEADO,
  BL_CLD_EMITIBLE,
  BL_DOCUMENTOS,
  BL_SOLO_SHIPPER,
  BL_TRANSBORDO_PAGADO,
  CARGO_TRANSBORDO,
  DOC,
  PAGO_CON_DOCUMENTOS,
} from '../fixtures/ola-e-mocks';
import { USUARIO_PRUEBA, sembrarSesion } from '../fixtures/session';

/**
 * Fase 1, Ola E (pruebas funcionales con el backend simulado):
 * - M6-05 / M1-11: el shipper solo puede pedir la copia no valorada; la copia se envía al correo registrado
 *   y queda en el repositorio.
 * - M6-06 / M4-04: la carta de responsabilidad se emite desde el aviso de los cargos y levanta el bloqueo FFWW.
 * - M6-07 / M3-16: el CLD bloqueado lista lo pendiente y dónde pagarlo; sin deuda se emite firmado.
 * - M6-09 / NF-14: la descarga pide el PDF con la sesión y el reenvío usa el correo registrado.
 * - M6-01: solicitar el certificado de transbordo crea un cargo que se agrega al carro.
 * - El resultado de un pago que emite documentos enlaza a los documentos de cada BL.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrir(page: Page, ruta: string): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('el shipper solo ve la copia no valorada y la recibe por correo (M6-05, M1-11)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_SOLO_SHIPPER}`);

  const seccion = page.getByRole('region', { name: 'Documentos', exact: true });
  await expect(seccion.getByText('Aún no hay documentos emitidos para este embarque.')).toBeVisible();
  await seccion.getByRole('button', { name: 'Solicitar copia del BL' }).click();

  const dialogo = page.getByRole('dialog', { name: `Copia del BL ${BL_SOLO_SHIPPER}` });
  // Solo la versión que la matriz de M1-11 habilita al shipper, ya elegida.
  await expect(dialogo.getByRole('radio')).toHaveCount(1);
  await expect(dialogo.getByRole('radio', { name: 'No valorada' })).toBeChecked();
  await expect(dialogo.getByRole('radio', { name: 'Valorada', exact: true })).toHaveCount(0);
  await expect(dialogo).toContainText('Su rol en este BL solo permite la copia no valorada.');
  await expect(dialogo.getByRole('checkbox', { name: 'Enviar también al correo registrado de mi organización' })).toBeChecked();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_SOLO_SHIPPER}/bl-copy`));
  await dialogo.getByRole('button', { name: 'Solicitar copia' }).click();
  expect((await envio).postDataJSON()).toEqual({ valued: false, sendEmail: true });

  const resultado = `Copia CBL-20261006-00000010 emitida y enviada a ${USUARIO_PRUEBA.email}.`;
  await expect(dialogo.getByRole('heading', { name: 'Copia emitida' })).toBeFocused();
  await expect(dialogo.getByTestId('bl-copy-result')).toHaveText(resultado);
  await expect(page.locator(POLITE)).toHaveText(resultado);
  await dialogo.getByRole('button', { name: 'Listo' }).click();
  await expect(dialogo).toHaveCount(0);

  // Queda en el repositorio del embarque (M6-09).
  const fila = seccion.getByTestId('document-CBL-20261006-00000010');
  await expect(fila).toContainText('Copia del BL no valorada');
  await expect(fila).toContainText(`Enviado a ${USUARIO_PRUEBA.email}`);
  await expect(seccion.getByTestId('documents-result')).toHaveText('Copia del BL CBL-20261006-00000010 registrada en los documentos del embarque.');
});

test('la carta de responsabilidad levanta el bloqueo FFWW de los cargos (M6-06, M4-04)', async ({ page }) => {
  await abrir(page, `/charges/${BL_CARTA}`);

  const aviso = page.getByRole('note').filter({ hasText: 'Carta de responsabilidad obligatoria' });
  await expect(aviso).toBeVisible();
  await expect(page.getByRole('button', { name: /Agregar .* al carro/ })).toHaveCount(0);
  await aviso.getByRole('button', { name: 'Generar carta de responsabilidad' }).click();

  const dialogo = page.getByRole('dialog', { name: `Carta de responsabilidad del BL ${BL_CARTA}` });
  await expect(dialogo.getByRole('region', { name: /Carta de responsabilidad del freight forwarder \(versión CARTA-RESP-2026-10\)/ })).toContainText('asume la responsabilidad');
  // El correo de contacto se propone con el del usuario (WCAG 3.3.7).
  await expect(dialogo.getByLabel('Correo de contacto (obligatorio)')).toHaveValue(USUARIO_PRUEBA.email);

  // Sin datos ni términos aceptados no se envía nada: resumen de errores con enlace al campo.
  await dialogo.getByRole('button', { name: 'Emitir y descargar la carta' }).click();
  const resumen = dialogo.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await expect(resumen.getByRole('link')).toHaveText([
    'Ingrese el nombre del firmante.',
    'Ingrese el RUT o documento del firmante, por ejemplo 12.345.678-5.',
    'Ingrese el cargo del firmante.',
    'Acepte los términos de la carta.',
  ]);
  await resumen.getByRole('link', { name: 'Ingrese el nombre del firmante.' }).click();
  await expect(dialogo.getByLabel('Nombre del firmante (obligatorio)')).toBeFocused();

  await dialogo.getByLabel('Nombre del firmante (obligatorio)').fill('Felipe Forwarder');
  await dialogo.getByLabel('RUT o documento del firmante (obligatorio)').fill('12.345.678-5');
  await dialogo.getByLabel('Cargo del firmante (obligatorio)').fill('Gerente de Operaciones');
  await dialogo.getByLabel('Descripción de la carga (opcional)').fill('Muebles');
  await dialogo.getByLabel('Acepto los términos de la carta, versión CARTA-RESP-2026-10 (obligatorio)').check();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_CARTA}/responsibility-letter`));
  const descarga = page.waitForRequest((r) => /\/api\/v1\/documents\/[^/]+\/[^/]+\/download$/.test(r.url()));
  const archivo = page.waitForEvent('download');
  await dialogo.getByRole('button', { name: 'Emitir y descargar la carta' }).click();
  expect((await envio).postDataJSON()).toEqual({
    signatoryName: 'Felipe Forwarder',
    signatoryTaxId: '12.345.678-5',
    signatoryPosition: 'Gerente de Operaciones',
    contactEmail: USUARIO_PRUEBA.email,
    contactPhone: null,
    cargoDescription: 'Muebles',
    observations: null,
    acceptTerms: true,
    termsVersion: 'CARTA-RESP-2026-10',
  });
  await descarga;
  expect((await archivo).suggestedFilename()).toBe('CRE-20261006-00000010.pdf');
  await expect(dialogo).toHaveCount(0);

  // Con la carta vigente los cargos se vuelven a leer sin el bloqueo (M4-04).
  const emitida = 'Carta de responsabilidad CRE-20261006-00000010 emitida y descargada. El bloqueo de este BL se levantó.';
  await expect(page.locator(POLITE)).toHaveText(emitida);
  await expect(page.getByTestId('charges-letter-issued')).toHaveText(emitida);
  await expect(page.getByRole('note').filter({ hasText: 'Carta de responsabilidad obligatoria' })).toHaveCount(0);
  await expect(page.getByText('Bloqueado: falta la carta de responsabilidad')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Agregar THC al carro' })).toBeVisible();
});

test('el CLD bloqueado lista lo pendiente y, sin deuda, se emite firmado (M6-07, M3-16)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_CLD_BLOQUEADO}/documents`);

  const bloqueo = page.getByTestId('no-debt-blocked');
  await expect(bloqueo.getByRole('heading', { name: 'No se puede emitir el CLD' })).toBeVisible();
  await expect(bloqueo.getByRole('listitem')).toHaveCount(4);
  const cargos = bloqueo.getByTestId('no-debt-blocker-PENDING_CHARGES');
  await expect(cargos).toContainText('Cargos locales pendientes');
  await expect(cargos).toContainText('Referencias: THC, Tránsito, MHD');
  await expect(cargos).toContainText(/BOB\s2\.226,10/);
  await expect(cargos).toContainText(/USD\s450,00/);
  await expect(cargos.getByRole('link', { name: `Pagar los cargos locales del BL ${BL_CLD_BLOQUEADO}` })).toHaveAttribute('href', `/charges/${BL_CLD_BLOQUEADO}`);
  await expect(bloqueo.getByTestId('no-debt-blocker-PENDING_DEMURRAGE')).toContainText('Referencias: HLXU8899001');
  await expect(bloqueo.getByTestId('no-debt-blocker-PENDING_INVOICES')).toContainText('Referencias: 2026-000812');
  // Demoras anticipadas obligatorias aún no solicitadas (M3-16): se piden y pagan en el demurrage del BL.
  const adelanto = bloqueo.getByTestId('no-debt-blocker-ADVANCE_DEMURRAGE');
  await expect(adelanto).toContainText('Demoras anticipadas obligatorias sin pagar');
  await expect(adelanto).toContainText('Referencias: Cargo no generado');
  await expect(adelanto.getByRole('link', { name: `Solicitar y pagar las demoras anticipadas del BL ${BL_CLD_BLOQUEADO}` }))
    .toHaveAttribute('href', `/demurrage/${BL_CLD_BLOQUEADO}`);
  await expect(page.getByRole('button', { name: 'Emitir CLD' })).toHaveCount(0);

  // Sin deuda: el CLD se emite con firma electrónica, se descarga y queda en el repositorio.
  await page.goto(`/shipments/${BL_CLD_EMITIBLE}/documents`);
  await expect(page.getByTestId('no-debt-eligible')).toHaveText('El embarque no registra deuda pendiente: puede emitir el CLD.');
  const emision = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_CLD_EMITIBLE}/no-debt-certificate`));
  const descarga = page.waitForRequest((r) => /\/api\/v1\/documents\/[^/]+\/[^/]+\/download$/.test(r.url()));
  await page.getByRole('button', { name: 'Emitir CLD' }).click();
  await emision;
  await descarga;
  const emitido = 'CLD CLD-20261006-00000010 emitido con firma electrónica y descargado. Quedó en los documentos del embarque.';
  await expect(page.locator(POLITE)).toHaveText(emitido);
  await expect(page.getByTestId('documents-result')).toHaveText(emitido);
  const fila = page.getByTestId('document-CLD-20261006-00000010');
  await expect(fila).toContainText('CLD (certificado de libre deuda)');
  // Huso de la operación de Bolivia (NF-22).
  await expect(fila.getByTestId('document-signature')).toContainText('Firmado electrónicamente el 06-10-2026 08:30');
});

test('la descarga pide el PDF con la sesión y el reenvío usa el correo registrado (M6-09, M6-05, NF-14)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_DOCUMENTOS}/documents`);

  const tabla = page.getByRole('table', { name: `Documentos del BL ${BL_DOCUMENTOS}` });
  await expect(tabla.getByRole('row')).toHaveCount(4);
  // Cupón de retiro asociado a las unidades pagadas (M6-03).
  await expect(page.getByTestId(`document-${DOC.CUPON}`)).toContainText('Unidades: HLXU1234567');
  const transbordo = page.getByTestId(`document-${DOC.TRANSBORDO}`);
  await expect(transbordo.getByTestId('document-signature')).toHaveText('Con firma electrónica: se firma al descargarlo por primera vez');

  const descarga = page.waitForRequest((r) => r.method() === 'GET' && r.url().endsWith(`/api/v1/documents/${BL_DOCUMENTOS}/d6000000-0000-4000-8000-000000000001/download`));
  const archivo = page.waitForEvent('download');
  await transbordo.getByRole('button', { name: `Descargar Certificado de transbordo ${DOC.TRANSBORDO}` }).click();
  expect((await descarga).headers()['authorization']).toMatch(/^Bearer /);
  expect((await archivo).suggestedFilename()).toBe(`${DOC.TRANSBORDO}.pdf`);
  await expect(page.locator(POLITE)).toHaveText(`Se descargó ${DOC.TRANSBORDO}.`);
  // La primera descarga lo generó y firmó: el repositorio se actualiza.
  await expect(transbordo.getByTestId('document-signature')).toContainText('Firmado electrónicamente el 06-10-2026 09:00');

  // Reenvío al correo registrado de la organización, sin destinatarios libres.
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_DOCUMENTOS}/d6000000-0000-4000-8000-000000000003/send`));
  await page.getByRole('button', { name: `Reenviar ${DOC.COPIA} al correo registrado de su organización` }).click();
  expect((await envio).postDataJSON()).toEqual({});
  const reenviado = `${DOC.COPIA} se envió a ${USUARIO_PRUEBA.email}.`;
  await expect(page.getByTestId('documents-result')).toHaveText(reenviado);
  await expect(page.locator(POLITE)).toHaveText(reenviado);

  // Comprobantes y facturas del BL, por su propia ruta de descarga.
  const comprobante = page.waitForRequest((r) => r.url().endsWith('/api/v1/payment-history/h5000000-0000-4000-8000-000000000001/receipt'));
  await page.getByRole('button', { name: 'Descargar Comprobante de pago RCP-20260925-0A1B2C3D' }).click();
  await comprobante;
  const factura = page.waitForRequest((r) => r.url().endsWith('/api/v1/invoices/i4000000-0000-4000-8000-000000003987/pdf'));
  await page.getByRole('button', { name: 'Descargar Factura 100245' }).click();
  await factura;
});

test('solicitar el certificado de transbordo agrega su cargo al carro (M6-01)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_DOCUMENTOS}`);
  await expect(page.getByRole('link', { name: 'Carro de compra, 3 ítems' })).toBeVisible();

  const panel = page.getByRole('group', { name: 'Certificado de transbordo' });
  await expect(panel).toContainText('se envía por correo: en importación, a UMAR y a su organización');
  const solicitud = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/documents/${BL_DOCUMENTOS}/transshipment-certificate`));
  await panel.getByRole('button', { name: 'Solicitar certificado de transbordo' }).click();
  await solicitud;

  // El cargo se agrega con el diálogo de siempre (RUT de facturación y moneda, M5-09).
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await expect(dialogo.getByRole('table')).toContainText('Certificado de transbordo');
  await expect(dialogo.getByRole('table')).toContainText(/CLP\s41\.650/);
  const agregar = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/cart/items'));
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  expect((await agregar).postDataJSON()).toEqual({
    itemType: 'LocalCharge',
    sourceId: CARGO_TRANSBORDO,
    reference: null,
    billingTaxId: RUT_PROPIO,
    paymentCurrency: 'CLP',
  });
  await expect(page.locator(POLITE)).toHaveText('Certificado de transbordo se agregó al carro en CLP.');
  await expect(dialogo).toHaveCount(0);
  await expect(panel.getByTestId('transshipment-charge')).toHaveText(/Cargo del servicio: CLP\s41\.650, IVA de CLP\s6\.650 incluido\./);
  await expect(panel).toContainText('El cargo está en el carro de compra.');
  await expect(page.getByRole('link', { name: 'Carro de compra, 4 ítems' })).toBeVisible();
});

test('el resultado de un pago que emite documentos enlaza a los documentos del BL (M6-01, M6-03, M6-09)', async ({ page }) => {
  await abrir(page, `/payments/${PAGO_CON_DOCUMENTOS}/result`);

  const documentos = page.getByTestId('payment-result-documents');
  await expect(documentos.getByRole('heading', { name: 'Documentos del embarque' })).toBeVisible();
  await expect(documentos.getByRole('link')).toHaveText([
    `Ver los documentos del BL ${BL_TRANSBORDO_PAGADO}`,
    `Ver los documentos del BL ${BL_DOCUMENTOS}`,
  ]);
  await documentos.getByRole('link', { name: `Ver los documentos del BL ${BL_DOCUMENTOS}` }).click();
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_DOCUMENTOS}/documents$`));
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Documentos del BL ${BL_DOCUMENTOS}`);
  await expect(page.getByTestId(`document-${DOC.CUPON}`)).toContainText('Cupón de retiro de Gate Out');
});
