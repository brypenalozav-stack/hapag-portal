import { test, expect, Page } from '@playwright/test';
import {
  ACCESO_AGENCIA,
  AGENCIA,
  BL_ACCESO_ABIERTO,
  BL_EXPORTACION,
  BL_PRUEBA,
  BL_SIN_FLETE,
  simularApi,
} from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Fase 1, Ola B (pruebas funcionales con el backend simulado):
 * - M1-12 / M1-15 / M1-24: otorgamiento individual con permisos limitados desde el detalle del BL
 *   (se revisa el cuerpo enviado) y masivo con el conteo de registros actualizados.
 * - M1-22: la revocación avisa de la revocación en cadena y anuncia lo revocado.
 * - M1-24 / M1-14: edición en el lugar de la vigencia.
 * - M1-13: terceros por defecto (agregar, editar y quitar).
 * - M1-17: acceso abierto (activar con el nivel base) y búsqueda de BL por número.
 * - M1-18: autoasociación a un BL visto por acceso abierto, con el aviso previo al pago.
 * - M1-23: auditoría filtrada por BL.
 * - Bandeja: títulos de las notificaciones de acceso traducidos.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrirConSesion(page: Page, ruta: string, lang: 'es' | 'en' = 'es'): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

/** Busca y elige al destinatario en el selector del flujo único. */
async function elegirDestinatario(contenedor: ReturnType<Page['locator']>, texto: string, nombre: string): Promise<void> {
  await contenedor.getByLabel('Razón social o RUT/NIT').fill(texto);
  await contenedor.getByRole('button', { name: 'Buscar destinatario' }).click();
  await contenedor.getByRole('button', { name: `Elegir a ${nombre}` }).click();
  await expect(contenedor.getByText(`Destinatario: ${nombre}`)).toBeVisible();
}

/** Abre una pestaña de la vista única de accesos. */
async function pestana(page: Page, nombre: string): Promise<void> {
  const tab = page.getByRole('tab', { name: nombre });
  await tab.click();
  await expect(tab).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('otorgamiento individual con permisos limitados desde el detalle del BL (M1-12, M1-15)', async ({ page }) => {
  await abrirConSesion(page, `/shipments/${BL_PRUEBA.blNumber}`);

  const seccion = page.getByRole('region', { name: 'Accesos', exact: true });
  await expect(seccion.getByRole('cell', { name: AGENCIA.name })).toBeVisible();
  await seccion.getByRole('button', { name: 'Otorgar acceso a este BL' }).click();

  const dialogo = page.getByRole('dialog', { name: `Otorgar acceso al BL ${BL_PRUEBA.blNumber}` });
  await expect(dialogo).toBeVisible();
  await elegirDestinatario(dialogo, 'Agencia', AGENCIA.name);

  await dialogo.getByLabel('Hasta una fecha').check();
  await dialogo.getByLabel('Vigente hasta').fill('2027-03-31');

  await dialogo.getByLabel('Elegir permisos').check();
  // Lo que el otorgante no posee aparece deshabilitado (M1-12: no otorgar más de lo que se posee).
  await expect(dialogo.getByLabel('Generar y descargar carta de responsabilidad')).toBeDisabled();
  await expect(dialogo.getByLabel('Ver listado y detalle de BL o booking')).toBeChecked();
  await dialogo.getByLabel('Ver el seguimiento del embarque').check();
  await dialogo.getByLabel('Consultar y pagar demurrage de importación').check();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/access/grants'));
  await dialogo.getByRole('button', { name: 'Otorgar acceso a 1 embarque' }).click();
  const cuerpo = (await envio).postDataJSON();
  expect(cuerpo).toMatchObject({
    granteeOrganizationId: AGENCIA.id,
    blNumbers: [BL_PRUEBA.blNumber],
    validityType: 'UntilDate',
  });
  expect(typeof cuerpo.validTo).toBe('string');
  expect([...cuerpo.actionCodes].sort()).toEqual(['import-demurrage.pay', 'tracking.view']);
  expect(cuerpo.isMandate).toBeUndefined();

  await expect(dialogo.getByRole('heading', { name: 'Resultado del otorgamiento' })).toBeVisible();
  await expect(dialogo.getByTestId('grant-result-updated')).toHaveText('1');
  await dialogo.getByRole('button', { name: 'Listo' }).click();
  await expect(dialogo).toBeHidden();
});

test('otorgamiento masivo: confirma registros actualizados, creados, modificados y omitidos (M1-12)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  await page.locator('app-access-grants').getByRole('button', { name: 'Otorgar acceso', exact: true }).click();
  const dialogo = page.getByRole('dialog', { name: 'Otorgar acceso' });
  await expect(dialogo.locator('app-loading-spinner')).toHaveCount(0);
  await elegirDestinatario(dialogo, 'Agencia', AGENCIA.name);

  for (const bl of [BL_PRUEBA.blNumber, BL_EXPORTACION, BL_SIN_FLETE]) {
    await dialogo.getByLabel(`Seleccionar el BL ${bl}`).check();
  }
  await expect(dialogo.getByText('3 embarques seleccionados')).toBeVisible();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/access/grants/bulk'));
  await dialogo.getByRole('button', { name: 'Otorgar acceso a 3 embarques' }).click();
  const cuerpo = (await envio).postDataJSON();
  expect(cuerpo.blNumbers).toEqual([BL_PRUEBA.blNumber, BL_EXPORTACION, BL_SIN_FLETE]);
  expect(cuerpo.actionCodes).toBeNull();
  expect(cuerpo.validityType).toBe('Indefinite');

  const resumen = 'Se actualizaron 2 de 3 registros: 1 creados, 1 modificados y 1 omitidos.';
  await expect(dialogo.getByRole('status').filter({ hasText: resumen })).toBeVisible();
  await expect(dialogo.getByText(`${BL_SIN_FLETE}: No puede otorgar un permiso`)).toBeVisible();
  await expect(page.locator(POLITE)).toHaveText(resumen);
});

test('el formulario de otorgamiento muestra el resumen de errores (guía §3.2)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  await page.locator('app-access-grants').getByRole('button', { name: 'Otorgar acceso', exact: true }).click();
  const dialogo = page.getByRole('dialog', { name: 'Otorgar acceso' });
  await dialogo.getByRole('button', { name: /^Otorgar acceso/ }).click();

  const resumen = dialogo.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await expect(resumen.getByRole('link', { name: 'Elija el destinatario del acceso.' })).toBeVisible();
  await expect(resumen.getByRole('link', { name: 'Seleccione al menos un embarque.' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(dialogo).toBeHidden();
});

test('revocar avisa de la revocación en cadena y anuncia lo revocado (M1-22)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  const tabla = page.getByRole('table', { name: 'Accesos otorgados y recibidos por la organización' });
  await expect(tabla.getByRole('cell', { name: /Revocación en cadena/ })).toBeVisible();
  await tabla.getByRole('button', { name: `Revocar el acceso de ${AGENCIA.name} a ${BL_PRUEBA.blNumber}` }).click();

  await expect(page.getByRole('heading', { name: `Revocar el acceso de ${AGENCIA.name} a ${BL_PRUEBA.blNumber}` })).toBeFocused();
  await expect(page.getByRole('note').filter({ hasText: 'en cadena' })).toBeVisible();
  await page.getByLabel('Motivo (opcional)').fill('Término del contrato');

  const envio = page.waitForRequest(
    (r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/access/grants/${ACCESO_AGENCIA.id}/revoke`),
  );
  await page.getByRole('button', { name: 'Confirmar revocación' }).click();
  expect((await envio).postDataJSON()).toEqual({ reason: 'Término del contrato' });
  await expect(page.locator(POLITE)).toHaveText(
    'Revocación terminada: 1 acceso revocado, 2 en cadena y 1 ampliación retirada.',
  );
});

test('revocación masiva de los accesos seleccionados (M1-12, M1-22)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  await page.getByLabel(`Seleccionar el acceso de ${AGENCIA.name} a ${BL_PRUEBA.blNumber}`).check();
  await page.getByLabel(`Seleccionar el acceso de ${AGENCIA.name} a ${BL_EXPORTACION}`).check();
  await page.getByRole('button', { name: 'Revocar 2 seleccionados' }).click();
  await expect(page.getByRole('heading', { name: 'Revocar 2 accesos' })).toBeVisible();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/access/grants/revoke'));
  await page.getByRole('button', { name: 'Confirmar revocación' }).click();
  expect((await envio).postDataJSON().grantIds).toHaveLength(2);
});

test('edición en el lugar de la vigencia de un acceso (M1-14, M1-24)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  await page.getByRole('button', { name: `Editar el acceso de ${AGENCIA.name} a ${BL_PRUEBA.blNumber}` }).click();
  const formulario = page.locator('app-access-grants form').filter({ hasText: 'Guardar cambios' });
  await formulario.getByLabel('Por cantidad de días').check();
  await formulario.getByLabel('Cantidad de días', { exact: true }).fill('30');

  const envio = page.waitForRequest(
    (r) => r.method() === 'PUT' && r.url().endsWith(`/api/v1/access/grants/${ACCESO_AGENCIA.id}`),
  );
  await formulario.getByRole('button', { name: 'Guardar cambios' }).click();
  expect((await envio).postDataJSON()).toEqual({ validityType: 'Duration', durationDays: 30 });
  await expect(page.locator(POLITE)).toContainText('actualizado');
});

test('terceros por defecto: agregar, editar y quitar (M1-13)', async ({ page }) => {
  await abrirConSesion(page, '/organization');
  await pestana(page, 'Terceros por defecto');

  const tabla = page.getByRole('table', { name: 'Terceros que reciben acceso por defecto' });
  await expect(tabla.getByRole('row').filter({ hasText: AGENCIA.name })).toContainText('180 días');

  // Agregar
  await page.getByRole('button', { name: 'Agregar tercero por defecto' }).click();
  const panel = page.locator('app-access-defaults');
  await elegirDestinatario(panel, 'Transportes', 'Transportes Cordillera Ltda.');
  await panel.getByLabel('Duración del acceso en días (opcional)').fill('90');
  const alta = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/access/defaults'));
  await panel.getByRole('button', { name: 'Guardar' }).click();
  expect((await alta).postDataJSON()).toEqual({ granteeOrganizationId: 'c1000000-0000-4000-8000-000000000002', durationDays: 90 });
  await expect(page.locator(POLITE)).toHaveText('Transportes Cordillera Ltda. agregado como tercero por defecto.');

  // Editar: permisos explícitos
  await tabla.getByRole('button', { name: `Editar el tercero por defecto ${AGENCIA.name}` }).click();
  await panel.getByLabel('Elegir permisos').check();
  await panel.getByLabel('Ver el seguimiento del embarque').check();
  const edicion = page.waitForRequest((r) => r.method() === 'PUT' && /\/api\/v1\/access\/defaults\/[^/]+$/.test(r.url()));
  await panel.getByRole('button', { name: 'Guardar' }).click();
  expect((await edicion).postDataJSON()).toEqual({ durationDays: 180, actionCodes: ['tracking.view'] });

  // Quitar
  await tabla.getByRole('button', { name: `Quitar a ${AGENCIA.name} de los terceros por defecto` }).click();
  const baja = page.waitForRequest((r) => r.method() === 'DELETE' && /\/api\/v1\/access\/defaults\/[^/]+$/.test(r.url()));
  await panel.getByRole('button', { name: 'Quitar', exact: true }).click();
  await baja;
  await expect(page.locator(POLITE)).toHaveText(`${AGENCIA.name} ya no es tercero por defecto.`);
});

test('acceso abierto: se activa con un único conjunto de permisos (M1-17)', async ({ page }) => {
  await abrirConSesion(page, '/organization');
  await pestana(page, 'Acceso abierto');

  await expect(page.getByText('El acceso abierto está desactivado')).toBeVisible();
  const interruptor = page.getByRole('switch', { name: 'Activar el acceso abierto por número de BL' });
  await interruptor.check();

  const envio = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith('/api/v1/access/open-access'));
  await page.getByRole('button', { name: 'Guardar configuración' }).click();
  expect((await envio).postDataJSON()).toEqual({ isEnabled: true, resetToBaseLevel: true });
  await expect(page.getByText('El acceso abierto está activado.')).toBeVisible();
  await expect(page.locator(POLITE)).toHaveText('Acceso abierto activado.');
});

test('buscar BL por número y autoasociarse con acceso abierto (M1-17, M1-18)', async ({ page }) => {
  await abrirConSesion(page, '/shipments');

  // El listado muestra el origen del acceso (M2-06): propio y otorgado.
  const filas = page.locator('main table tbody tr');
  await expect(filas.filter({ hasText: 'HLCUARI260300830' })).toContainText('Acceso otorgado');
  await expect(filas.filter({ hasText: BL_ACCESO_ABIERTO })).toHaveCount(0);

  await page.getByLabel('Buscar BL por número').fill(BL_ACCESO_ABIERTO.toLowerCase());
  await page.getByRole('button', { name: 'Abrir BL' }).click();
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_ACCESO_ABIERTO}$`));
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);

  await expect(page.getByText('Acceso abierto', { exact: true })).toBeVisible();
  await expect(page.getByRole('note').filter({ hasText: 'primero debe asociarse' })).toBeVisible();
  // Visto por acceso abierto no se muestra la sección de accesos del BL.
  await expect(page.getByRole('heading', { name: 'Accesos', exact: true })).toHaveCount(0);

  const envio = page.waitForRequest(
    (r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/shipments/${BL_ACCESO_ABIERTO}/associate`),
  );
  await page.getByRole('button', { name: 'Asociarme a este BL' }).click();
  await envio;
  await expect(page.locator(POLITE)).toHaveText(`Su organización quedó asociada al BL ${BL_ACCESO_ABIERTO}.`);
});

test('auditoría de accesos filtrada por BL (M1-23)', async ({ page }) => {
  await abrirConSesion(page, '/organization');
  await pestana(page, 'Auditoría');

  const tabla = page.getByRole('table', { name: 'Eventos de la auditoría de accesos' });
  await expect(tabla.locator('tbody tr')).toHaveCount(4);

  const consulta = page.waitForRequest(
    (r) => r.method() === 'GET' && r.url().includes('/api/v1/access/audit?') && r.url().includes(`blNumber=${BL_PRUEBA.blNumber}`),
  );
  await page.getByLabel('Número de BL').fill(BL_PRUEBA.blNumber);
  await page.getByRole('button', { name: 'Consultar' }).click();
  await consulta;

  await expect(tabla.locator('tbody tr')).toHaveCount(2);
  const cadena = tabla.locator('tbody tr').filter({ hasText: 'Revocación en cadena' });
  await expect(cadena).toContainText('Sistema (automático)');
  await expect(tabla.locator('tbody tr').filter({ hasText: 'Acceso otorgado' })).toContainText('cliente.prueba@example.com');
});

test('la bandeja muestra el título traducido de las notificaciones de acceso', async ({ page }) => {
  await abrirConSesion(page, '/notifications', 'en');
  // La bandeja de la Ola I también lista el tipo en el filtro: el título es el encabezado de la notificación.
  await expect(page.getByRole('heading', { name: /Access revoked by cascade/ })).toBeVisible();
  await expect(page.getByText('Acceso revocado en cadena')).toHaveCount(0);
});

test('un perfil sin org.access.manage ve los accesos en solo lectura (M1-24)', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es', permisos: [] });
  await page.goto('/organization');

  await expect(page.getByRole('heading', { name: 'Accesos y permisos' })).toBeVisible();
  await expect(page.getByRole('note').filter({ hasText: 'consultar los accesos' })).toBeVisible();
  await expect(page.getByRole('table', { name: 'Accesos otorgados y recibidos por la organización' })).toBeVisible();
  await expect(page.locator('app-access-grants').getByRole('button', { name: 'Otorgar acceso', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /^Revocar/ })).toHaveCount(0);
  await expect(page.getByRole('tab', { name: 'Acceso por booking' })).toHaveCount(0);
});
