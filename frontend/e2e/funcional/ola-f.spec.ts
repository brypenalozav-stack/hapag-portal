import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { ITEM, RUT_PROPIO } from '../fixtures/ola-d-mocks';
import { BL_NO_PUBLICADO, BL_TATC, CASILLA_CL, URL_DISPUTE } from '../fixtures/ola-f-mocks';
import { USUARIO_PRUEBA, sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';
import { cargarSeccionesDiferidas } from '../fixtures/detalle';

/**
 * Fase 1, Ola F (pruebas funcionales con el backend simulado):
 * - M1-05: un pendiente del dashboard se agrega al carro y queda marcado "En el carro".
 * - M10-01 / M10-03: el asistente responde el estado de un BL con el enlace al detalle; el panel sigue abierto
 *   con el historial al navegar y Escape lo cierra devolviendo el foco al botón.
 * - M10-02: una consulta fuera de alcance se rechaza y deriva a la casilla de correo.
 * - M10-05: al terminar se pide el respaldo por correo a otra dirección, validada.
 * - M10-06: el buscador DG dice de forma explícita que la carga no está clasificada.
 * - M11-07: el selector de tema aplica data-bs-theme y la preferencia persiste al recargar.
 * - M2-05: el enlace de Dispute abre el sitio externo con rel="noopener noreferrer".
 * - M2-09 / M2-02: detalle con emisión y TATC, y solicitud masiva con el resultado por BL.
 * - M2-01: el administrador filtra los BL no publicados y ve el motivo.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrir(page: Page, ruta: string): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  // Detalle del BL: los grupos bajo el pliegue se cargan al entrar en pantalla (@defer on viewport).
  if (/^\/shipments\/[^/?]+$/.test(ruta)) await cargarSeccionesDiferidas(page);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

async function abrirAsistente(page: Page) {
  await page.getByRole('button', { name: 'Asistente', exact: true }).click();
  const panel = page.getByRole('dialog', { name: 'Asistente del portal' });
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(1);
  await expect(panel.getByLabel('Escriba su pregunta')).toBeFocused();
  return panel;
}

async function preguntar(page: Page, panel: ReturnType<Page['getByRole']>, texto: string, respuestas: number): Promise<void> {
  await panel.getByLabel('Escriba su pregunta').fill(texto);
  await panel.getByRole('button', { name: 'Enviar' }).click();
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(respuestas);
}

test('un pendiente del dashboard se agrega al carro (M1-05, M5-01)', async ({ page }) => {
  await abrir(page, '/dashboard');

  const carro = page.getByTestId('navbar-cart');
  await expect(carro).toHaveAccessibleName('Carro de compra, 3 ítems');
  const fila = page.getByTestId(`dashboard-pending-${ITEM.GATE_IN}`);
  await expect(fila).toContainText('Gate In');
  await expect(page.getByTestId(`dashboard-pending-${ITEM.THC}`)).toContainText('En el carro');
  // La factura vencida se muestra con su vencimiento.
  await expect(page.getByTestId(`dashboard-pending-${ITEM.FACTURA_VENCIDA}`)).toContainText('Vencido');

  await fila.getByRole('button', { name: 'Agregar Gate In HLCU0000001 al carro' }).click();
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await expect(dialogo.locator('#add-to-cart-billing')).toBeVisible();
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/cart/items'));
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  expect((await envio).postDataJSON()).toMatchObject({ itemType: 'LocalCharge', sourceId: ITEM.GATE_IN, billingTaxId: RUT_PROPIO, paymentCurrency: 'CLP' });

  await expect(dialogo).toHaveCount(0);
  await expect(carro).toHaveAccessibleName('Carro de compra, 4 ítems');
  await expect(fila).toContainText('En el carro');
  await expect(fila.getByRole('button', { name: /Agregar/ })).toHaveCount(0);
  // "Ver" lleva a los cargos del BL.
  await expect(fila.getByRole('link', { name: 'Ver Gate In HLCU0000001' })).toHaveAttribute('href', '/charges/HLCU0000001');
});

test('los accesos rápidos nombran los servicios sin términos técnicos (M1-01)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const servicios = page.getByRole('navigation', { name: '¿Qué necesita hacer?' });
  await expect(servicios.getByRole('link', { name: /Pagar cargos y servicios/ })).toHaveAttribute('href', '/cart');
  await expect(servicios.getByRole('link', { name: /Cambiar de almacén/ })).toHaveAttribute('href', '/warehouse');
  await expect(servicios.getByRole('link', { name: /Buscar mercancías peligrosas/ })).toHaveAttribute('href', '/dangerous-goods');
});

test('el asistente responde el estado de un BL con el enlace al detalle (M10-01, M10-03)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const panel = await abrirAsistente(page);

  await panel.getByLabel('Escriba su pregunta').fill(`estado del BL ${BL_TATC}`);
  const envio = page.waitForRequest((r) => r.method() === 'POST' && /\/assistant\/sessions\/[^/]+\/messages$/.test(r.url()));
  await panel.getByLabel('Escriba su pregunta').press('Enter');
  expect((await envio).postDataJSON()).toEqual({ message: `estado del BL ${BL_TATC}` });
  // Estado "escribiendo", anunciado en la región polite.
  await expect(panel.getByTestId('assistant-typing')).toBeVisible();
  await expect(page.locator(POLITE)).toHaveText('El asistente está escribiendo…');

  const respuesta = panel.getByTestId('assistant-reply').last();
  await expect(respuesta).toContainText(`Estado del BL ${BL_TATC}`);
  await expect(respuesta).toContainText('Embarque: HLCUVAL250100123');
  await expect(page.locator(POLITE)).toContainText(`Respuesta del asistente: Estado del BL ${BL_TATC}`);
  await expect(panel.getByTestId('assistant-user-message')).toContainText(`estado del BL ${BL_TATC}`);

  const enlace = respuesta.getByRole('link', { name: `Ver el detalle del BL ${BL_TATC}` });
  await expect(enlace).toHaveAttribute('href', `/shipments/${BL_TATC}`);
  await enlace.click();
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_TATC}$`));
  // El panel sigue abierto y conserva el historial.
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(2);

  // Escape cierra el panel y el foco vuelve al botón.
  await panel.getByLabel('Escriba su pregunta').focus();
  await page.keyboard.press('Escape');
  await expect(panel).toHaveCount(0);
  const boton = page.getByTestId('assistant-launcher');
  await expect(boton).toBeFocused();
  await expect(boton).toHaveAttribute('aria-expanded', 'false');

  // Al volver a abrirlo, la conversación sigue ahí.
  await boton.click();
  await expect(page.getByTestId('assistant-reply')).toHaveCount(2);
});

test('el asistente rechaza la consulta fuera de alcance y deriva a la casilla (M10-02)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const panel = await abrirAsistente(page);

  await preguntar(page, panel, '¿Me recomienda un abogado para un reclamo?', 2);
  const rechazo = panel.getByTestId('assistant-reply').last();
  await expect(rechazo).toHaveAttribute('data-answer-type', 'Refused');
  await expect(rechazo).toContainText('Consulta fuera del alcance del asistente');
  await expect(rechazo).toContainText('No puedo entregar recomendaciones comerciales ni legales');
  await expect(rechazo.getByTestId('assistant-mailbox').getByRole('link', { name: CASILLA_CL })).toHaveAttribute('href', `mailto:${CASILLA_CL}`);

  // Un BL inexistente o sin acceso: "no disponible", también con la casilla.
  await preguntar(page, panel, 'estado del BL HLCU999999', 3);
  const noDisponible = panel.getByTestId('assistant-reply').last();
  await expect(noDisponible).toContainText('Información no disponible');
  await expect(noDisponible.getByTestId('assistant-mailbox')).toBeVisible();

  // Sin respuesta en la base de conocimiento: se deriva sin inventar una respuesta.
  await preguntar(page, panel, '¿cuál es el horario del casino?', 4);
  await expect(panel.getByTestId('assistant-reply').last()).toContainText('Sin respuesta: derivado a la casilla de correo');
});

test('el asistente informa el límite de mensajes y conserva el texto (M10-01, 429)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const panel = await abrirAsistente(page);
  await panel.getByLabel('Escriba su pregunta').fill('mensaje #429');
  await panel.getByRole('button', { name: 'Enviar' }).click();
  await expect(panel.getByTestId('assistant-error')).toHaveText('Envió demasiados mensajes en poco tiempo. Espere un momento y vuelva a intentarlo.');
  await expect(panel.getByLabel('Escriba su pregunta')).toHaveValue('mensaje #429');
});

test('al terminar la conversación se envía el respaldo al correo indicado (M10-05)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const panel = await abrirAsistente(page);
  await preguntar(page, panel, '¿Cómo funciona el cambio de almacén?', 2);

  await panel.getByRole('button', { name: 'Terminar', exact: true }).click();
  await expect(panel.getByRole('heading', { name: 'Terminar la conversación' })).toBeFocused();
  await panel.getByLabel('Enviarme el respaldo de la conversación por correo').check();
  await expect(panel.getByLabel(`Mi correo registrado (${USUARIO_PRUEBA.email})`)).toBeChecked();
  await panel.getByLabel('Otro correo').check();

  // Correo inválido: no se envía y el foco va al campo.
  await panel.getByLabel('Correo para el respaldo (obligatorio)').fill('correo-invalido');
  await panel.getByRole('button', { name: 'Terminar conversación' }).click();
  await expect(panel.getByLabel('Correo para el respaldo (obligatorio)')).toBeFocused();
  await expect(panel.getByText('Ingrese un correo válido, por ejemplo nombre@empresa.cl.')).toBeVisible();

  await panel.getByLabel('Correo para el respaldo (obligatorio)').fill('operaciones@empresa.cl');
  const cierre = page.waitForRequest((r) => r.method() === 'POST' && /\/assistant\/sessions\/[^/]+\/end$/.test(r.url()));
  await panel.getByRole('button', { name: 'Terminar conversación' }).click();
  expect((await cierre).postDataJSON()).toEqual({ sendTranscript: true, email: 'operaciones@empresa.cl' });

  const resultado = panel.getByTestId('assistant-end-result');
  await expect(resultado.getByRole('heading', { name: 'Conversación terminada' })).toBeFocused();
  await expect(resultado).toContainText('Enviamos el respaldo a operaciones@empresa.cl.');
  await expect(page.locator(POLITE)).toHaveText('Conversación terminada. Enviamos el respaldo a operaciones@empresa.cl.');

  // Se puede iniciar otra conversación.
  await resultado.getByRole('button', { name: 'Nueva conversación' }).click();
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(1);
  await expect(panel.getByTestId('assistant-user-message')).toHaveCount(0);
});

test('el buscador DG dice de forma explícita que la carga no está clasificada (M10-06)', async ({ page }) => {
  await abrir(page, '/dangerous-goods');

  // Sin texto no se busca.
  await page.getByRole('button', { name: 'Buscar' }).click();
  await expect(page.getByText('Ingrese el nombre, la descripción o el número ONU de la carga.')).toBeVisible();

  await page.getByLabel('Nombre, descripción o número ONU de la carga').fill('agua');
  await page.getByRole('button', { name: 'Buscar' }).click();
  const resultado = page.getByTestId('dg-result');
  await expect(resultado.getByRole('heading', { name: 'Resultado para «agua»' })).toBeFocused();
  await expect(resultado.getByTestId('dg-result-code')).toHaveText('No clasificada: la carga figura en la base de referencia como no peligrosa.');
  await expect(resultado.getByRole('row').nth(1)).toContainText('No clasificada');
  await expect(resultado.getByTestId('dg-disclaimer')).toContainText('no constituye la aprobación operacional');

  // Clasificada: clase, número ONU y grupo de embalaje.
  await page.getByLabel('Nombre, descripción o número ONU de la carga').fill('UN 1203');
  await page.getByRole('button', { name: 'Buscar' }).click();
  const fila = page.getByTestId('dg-item-UN1203');
  await expect(fila).toContainText('Gasolina');
  await expect(fila).toContainText('Clase 3: Líquidos inflamables');
  await expect(fila).toContainText('II');

  // Sin coincidencias.
  await page.getByLabel('Nombre, descripción o número ONU de la carga').fill('xyzabc');
  await page.getByRole('button', { name: 'Buscar' }).click();
  await expect(page.getByTestId('dg-result-code')).toContainText('Sin coincidencias');
});

test('el selector de tema aplica data-bs-theme y la preferencia persiste (M11-07)', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await abrir(page, '/dashboard');
  const html = page.locator('html');
  const selector = page.getByRole('combobox', { name: 'Tema' });
  await expect(selector).toHaveValue('auto');
  await expect(html).toHaveAttribute('data-bs-theme', 'light');

  await selector.selectOption('dark');
  await expect(html).toHaveAttribute('data-bs-theme', 'dark');
  await expect(page.locator(POLITE)).toHaveText('Tema cambiado: Oscuro.');
  expect(await page.evaluate(() => localStorage.getItem('hl_theme'))).toBe('dark');

  await page.reload();
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(html).toHaveAttribute('data-bs-theme', 'dark');
  await expect(page.getByRole('combobox', { name: 'Tema' })).toHaveValue('dark');

  // "Según el sistema" sigue la preferencia del sistema operativo.
  await page.getByRole('combobox', { name: 'Tema' }).selectOption('auto');
  await expect(html).toHaveAttribute('data-bs-theme', 'light');
  await page.emulateMedia({ colorScheme: 'dark' });
  await expect(html).toHaveAttribute('data-bs-theme', 'dark');
});

test('el enlace de Dispute abre el sitio externo en una pestaña nueva (M2-05)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await menu.getByRole('button', { name: 'Servicios' }).click();
  const enlace = menu
    .getByRole('link', { name: 'Dispute de productos digitales (se abre en una pestaña nueva)' });
  await expect(enlace).toHaveAttribute('href', URL_DISPUTE);
  await expect(enlace).toHaveAttribute('target', '_blank');
  await expect(enlace).toHaveAttribute('rel', 'noopener noreferrer');
  // También desde los accesos rápidos del dashboard.
  await expect(page.getByTestId('dispute-link-card')).toHaveAttribute('rel', 'noopener noreferrer');
});

test('el detalle muestra la emisión y el TATC, y la solicitud masiva informa cada BL (M2-02, M2-09)', async ({ page }) => {
  await abrir(page, `/shipments/${BL_TATC}`);
  // La emisión es un detalle secundario del resumen: se despliega a pedido.
  await page.getByRole('button', { name: 'Partes, roles y emisión del BL' }).click();
  await expect(page.getByTestId('issuance-status')).toHaveText('Liberado por télex');
  await expect(page.getByTestId('shipment-issuance')).toContainText('BL (conocimiento de embarque)');
  await expect(page.getByTestId('tatc-status')).toHaveText('Emitido parcialmente');
  await expect(page.getByTestId('tatc-HLXU3034002')).toContainText('TATC-SAI-2026-004512');
  await expect(page.getByTestId('tatc-HLXU3034003')).toContainText('MHD pendiente de pago');

  await page.getByTestId('shipment-tatc').getByRole('link', { name: 'Solicitar TATC' }).click();
  await expect(page).toHaveURL(new RegExp(`/tatc\\?bl=${BL_TATC}$`));
  await expect(page.getByLabel('BL (obligatorio)')).toHaveValue(BL_TATC);

  // Sin localidad: resumen de errores.
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  const resumen = page.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await expect(resumen.getByRole('link')).toHaveText(['Ingrese la localidad.']);

  await page.getByLabel('Localidad, UN/LOCODE (obligatorio)').fill('clsai');
  await page.getByLabel('BL (obligatorio)').fill(`${BL_TATC}\nHLCUSAI260401020, XYZ123`);
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/shipments/tatc-batches'));
  await page.getByRole('button', { name: 'Enviar solicitud' }).click();
  expect((await envio).postDataJSON()).toEqual({ locationCode: 'CLSAI', blNumbers: [BL_TATC, 'HLCUSAI260401020', 'XYZ123'] });

  const resultado = page.getByTestId('tatc-result');
  await expect(resultado.getByRole('heading', { name: 'Resultado de la solicitud de CLSAI' })).toBeFocused();
  await expect(page.getByTestId('tatc-result-status')).toHaveText('Completada con observaciones');
  await expect(page.getByTestId('tatc-result-counts')).toHaveText('1 aceptados, 2 no aceptados, de 3');
  await expect(page.getByTestId('tatc-item-HLCUSAI260401020')).toContainText('El TATC ya está emitido');
  await expect(page.getByTestId('tatc-item-XYZ123')).toContainText('BL no encontrado o sin acceso');
});

test('el administrador filtra los BL no publicados y ve el motivo (M2-01)', async ({ page }) => {
  await simularApi(page);
  await sembrarSesionAdmin(page, 'es');
  await page.goto('/shipments');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);

  await page.getByLabel('Publicación').selectOption('false');
  const consulta = page.waitForRequest((r) => r.url().includes('/api/v1/shipments?') && r.url().includes('published=false'));
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await consulta;
  const celda = page.getByTestId(`publication-${BL_NO_PUBLICADO}`);
  await expect(celda).toContainText('No publicado');
  await expect(celda).toContainText('No publicado: falta el DIFU del destino final');
});
