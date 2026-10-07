import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { FASE2 } from '../fixtures/funcionalidades';
import { OpcionesOlaD } from '../fixtures/ola-d-mocks';
import {
  BL_COUNTER_FALLIDO,
  BL_FILIAL,
  FILIAL,
  GUIA_CARRO,
  NOTIFICACION_SOLICITUD,
  OpcionesOlaI,
  SOLICITANTE,
  TRANSPORTISTA,
} from '../fixtures/ola-i-mocks';
import { ORGANIZACION_PRUEBA, sembrarIdioma, sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Fase 2, Ola I (pruebas funcionales con el backend simulado):
 * - M1-25: aprobar una solicitud de vinculación desde la bandeja (la acción queda deshabilitada al resolverse) y guardar
 *   una preferencia de correo; los tipos obligatorios se muestran bloqueados.
 * - M1-26: el cliente ve los comunicados de su país y operación, con la fecha de publicación; el administrador publica
 *   un borrador.
 * - M1-27: la guía del carro recorre sus pasos y queda completada; Escape la cierra como descartada.
 * - M8-08: vista como cliente: inicio, banner permanente, escritura bloqueada y término que restaura la sesión del
 *   administrador.
 * - M9-01: exportación del reporte de transacciones con los filtros y el idioma.
 * - M8-09: reintento del envío a Nexus de un registro de Counter fallido.
 * - M1-06: actualización de una lista de distribución, con el historial.
 * - M1-09: pre-creación de un transportista con BL asignados y asignación posterior; el registro detecta la cuenta
 *   pre-creada.
 * - M1-21: la matriz ve el BL de la filial con la organización de origen y filtra por filial.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrir(page: Page, ruta: string, opciones: OpcionesOlaD & OpcionesOlaI = {}, sesion: 'cliente' | 'admin' | 'ninguna' = 'cliente'): Promise<void> {
  await simularApi(page, { ...opciones, features: FASE2 });
  if (sesion === 'admin') await sembrarSesionAdmin(page, 'es');
  else if (sesion === 'cliente') await sembrarSesion(page, { lang: 'es' });
  else await sembrarIdioma(page, 'es');
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

test('bandeja: aprobar la solicitud de vinculación desde la notificación y verla resuelta (M1-25, M1-08)', async ({ page }) => {
  await abrir(page, '/notifications');
  await expect(page.getByTestId('navbar-notifications')).toHaveAccessibleName('Notificaciones, 3 sin leer, 1 requiere acción');
  await expect(page.getByTestId('notifications-by-module')).toContainText('Organización: 1 sin leer');

  // Solo las que requieren acción.
  await page.getByLabel('Solo las que requieren acción').check();
  await page.getByRole('button', { name: 'Buscar' }).click();
  await expect(page.getByTestId('notifications-count')).toHaveText('1 notificación; 1 requiere acción.');

  const tarjeta = page.getByTestId(`notification-${NOTIFICACION_SOLICITUD}`);
  await expect(tarjeta.getByRole('heading')).toContainText('Nueva solicitud de vinculación a la organización');
  await expect(tarjeta.getByTestId(`notification-link-${NOTIFICACION_SOLICITUD}`)).toContainText('Solicitud de vinculación: Paula Fuentes');
  await tarjeta.getByLabel('Perfil al aprobar').selectOption('OrgOperator');
  const aprobacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/organizations/me/join-requests/${SOLICITANTE.userId}/approve`));
  await tarjeta.getByRole('button', { name: 'Aprobar la solicitud de Paula Fuentes' }).click();
  expect((await aprobacion).postDataJSON()).toEqual({ profile: 'OrgOperator' });
  await expect(page.locator(POLITE)).toContainText('Solicitud de Paula Fuentes aprobada.');

  // Resuelta: ya no aparece entre las pendientes; sin el filtro, la acción queda deshabilitada con la fecha.
  await expect(page.locator('app-state-message')).toContainText('No tienes notificaciones.');
  await page.getByRole('button', { name: 'Limpiar' }).click();
  const resuelta = page.getByTestId(`notification-${NOTIFICACION_SOLICITUD}`);
  await expect(resuelta.getByRole('button', { name: 'Aprobar la solicitud de Paula Fuentes' })).toBeDisabled();
  await expect(resuelta.getByTestId(`notification-resolved-${NOTIFICACION_SOLICITUD}`)).toContainText('Ya resuelta el');
});

test('preferencias de correo: activar un tipo y guardar; los obligatorios y los solo de bandeja quedan bloqueados (M1-25)', async ({ page }) => {
  await abrir(page, '/notifications/preferences');
  const obligatorio = page.getByRole('switch', { name: 'Recibir por correo: Solicitud de vinculación aprobada' });
  await expect(obligatorio).toBeChecked();
  await expect(obligatorio).toBeDisabled();
  await expect(page.getByTestId('preference-JoinRequestApproved')).toContainText('Obligatorio: este aviso siempre se envía por correo.');
  await expect(page.getByTestId('preference-DeadlineAtRisk')).toContainText('Solo en la bandeja');

  const documentos = page.getByRole('switch', { name: 'Recibir por correo: Documento del embarque emitido' });
  await expect(documentos).not.toBeChecked();
  await documentos.check();
  await expect(page.getByText('1 cambio por guardar.')).toBeVisible();
  const guardado = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith('/notifications/preferences'));
  await page.getByTestId('notification-preferences-save').click();
  expect((await guardado).postDataJSON()).toEqual({ items: [{ type: 'DocumentIssued', emailEnabled: true }] });
  await expect(page.locator(POLITE)).toContainText('Se guardó 1 preferencia.');
  await expect(documentos).toBeChecked();
  await expect(page.getByText('Sin cambios por guardar.')).toBeVisible();
});

test('comunicados: el cliente ve los de su país y operación con la fecha de publicación (M1-26)', async ({ page }) => {
  await abrir(page, '/dashboard');
  const banner = page.getByTestId('dashboard-announcements');
  await expect(banner).toContainText('Nuevo horario del depósito de vacíos en San Antonio');
  await expect(banner).not.toContainText('Canje de BL de exportación boliviana');

  await banner.getByRole('link').click();
  await expect(page).toHaveURL(/\/announcements$/);
  const lista = page.getByTestId('announcements-list');
  await expect(lista).toContainText('Publicado el');
  await expect(lista).toContainText('Dirigido a: CL · Importación');
  await expect(lista).not.toContainText('Canje de BL');

  // Exportación en Chile: no hay comunicados para esa segmentación.
  await page.locator('#announcements-operation').selectOption('Export');
  await page.getByRole('button', { name: 'Ver comunicados' }).click();
  await expect(page.getByRole('status')).toContainText('No hay comunicados vigentes para este país y operación.');
  // Bolivia exportación.
  await page.locator('#announcements-country').selectOption('BO');
  await page.getByRole('button', { name: 'Ver comunicados' }).click();
  await expect(page.getByTestId('announcements-list')).toContainText('Canje de BL de exportación boliviana en Arica');
});

test('comunicados: el administrador publica el borrador y queda vigente (M1-26)', async ({ page }) => {
  await abrir(page, '/admin/announcements', {}, 'admin');
  const fila = page.locator('tr').filter({ hasText: 'Mantención programada del portal' });
  await expect(fila).toContainText('Borrador');
  await expect(fila).toContainText('Se avisará a los clientes al publicar');
  const publicacion = page.waitForRequest((r) => r.method() === 'POST' && /\/admin\/announcements\/[^/]+\/publish$/.test(r.url()));
  await fila.getByRole('button', { name: 'Publicar Mantención programada del portal' }).click();
  await publicacion;
  await expect(page.locator(POLITE)).toContainText('Comunicado publicado: Mantención programada del portal.');
  await expect(fila).toContainText('Publicado');
  await expect(fila).toContainText('Vigente: los clientes lo ven ahora');
});

test('modo guía: recorre los pasos del carro y queda completada; Escape la cierra (M1-27)', async ({ page }) => {
  await abrir(page, '/cart', { guiaNueva: true });
  await expect(page.getByTestId('guide-offer')).toContainText('Guía disponible: Pagar desde el carro.');

  const lanzar = page.getByTestId(`guide-launch-${GUIA_CARRO}`);
  await lanzar.click();
  const paso = page.getByTestId('guide-step');
  await expect(page.getByRole('dialog', { name: 'Sus ítems por moneda' })).toBeVisible();
  await expect(page.locator('#hl-guide-title')).toBeFocused();
  await expect(page.getByTestId('guide-progress')).toHaveText('Pagar desde el carro: paso 1 de 5');
  await expect(page.getByTestId('guide-back')).toBeDisabled();
  for (const titulo of ['RUT de facturación', 'Moneda de pago', 'Medio de pago', 'Revisar y pagar']) {
    await page.getByTestId('guide-next').click();
    await expect(paso.getByRole('heading')).toHaveText(titulo);
  }
  await expect(page.getByTestId('guide-target-missing')).toHaveCount(0);
  await expect(page.getByTestId('guide-next')).toHaveText('Terminar');
  const estado = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith(`/guides/${GUIA_CARRO}/state`));
  await page.getByTestId('guide-next').click();
  expect((await estado).postDataJSON()).toEqual({ status: 'Completed', lastStep: 5 });
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.locator(POLITE)).toContainText('Guía completada: Pagar desde el carro.');
  await expect(page.getByTestId('guide-offer')).toHaveCount(0);
  await expect(page.getByTestId(`guide-launch-${GUIA_CARRO}`)).toBeFocused();

  // Se vuelve a lanzar desde el botón y Escape la cierra como descartada.
  await page.getByTestId(`guide-launch-${GUIA_CARRO}`).click();
  await expect(page.getByTestId('guide-step')).toBeVisible();
  const descarte = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith(`/guides/${GUIA_CARRO}/state`));
  await page.keyboard.press('Escape');
  expect((await descarte).postDataJSON()).toEqual({ status: 'Dismissed', lastStep: 1 });
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('vista como cliente: inicio, banner, escritura bloqueada y término que restaura la sesión del administrador (M8-08)', async ({ page }) => {
  await abrir(page, '/admin/impersonation', {}, 'admin');
  await page.getByLabel('Organización', { exact: true }).fill('Andes');
  await page.getByRole('button', { name: 'Buscar', exact: true }).first().click();
  await page.getByRole('radio', { name: new RegExp(ORGANIZACION_PRUEBA.name) }).check();
  await expect(page.getByTestId('impersonation-target-consulta@importadoraandes.cl').getByRole('radio')).toBeDisabled();
  await page.getByRole('radio', { name: 'Carla Rojas' }).check();
  await page.getByLabel('Motivo').fill('Ticket SOP-4520: el cliente no ve su BL');
  const inicio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/admin/impersonation/sessions'));
  await page.getByTestId('impersonation-start-submit').click();
  expect((await inicio).postDataJSON()).toEqual({
    organizationId: ORGANIZACION_PRUEBA.id,
    userId: 'u0000000-0000-4000-8000-000000000001',
    reason: 'Ticket SOP-4520: el cliente no ve su BL',
  });

  // El portal del cliente con el banner permanente.
  await expect(page).toHaveURL(/\/dashboard$/);
  const banner = page.getByTestId('impersonation-banner');
  await expect(banner.getByTestId('impersonation-banner-title')).toHaveText(`Viendo como Carla Rojas (${ORGANIZACION_PRUEBA.name}) — solo lectura`);
  await expect(banner.getByTestId('impersonation-banner-time')).toContainText('Quedan 30 minutos · Iniciada por admin@hapag-lloyd.cl');
  await expect(page.getByRole('link', { name: 'Inicio de administración' })).toHaveCount(0);

  // Escritura: el servidor la bloquea y el banner lo informa.
  await page.getByTestId('navbar-notifications').click();
  await expect(page).toHaveURL(/\/notifications$/);
  const bloqueo = page.waitForResponse((r) => r.url().endsWith('/notifications/read-all') && r.status() === 403);
  await page.getByTestId('notifications-mark-all').click();
  await bloqueo;
  await expect(page.getByTestId('impersonation-blocked')).toHaveText('Acción bloqueada: la vista como cliente es de solo lectura y no permite pagar, solicitar ni modificar datos.');
  await expect(page.getByTestId('impersonation-blocked')).toHaveAttribute('role', 'alert');

  // Terminar: vuelve a la sesión del administrador.
  const termino = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/impersonation/end'));
  await banner.getByTestId('impersonation-end').click();
  expect((await termino).headers()['authorization']).toContain('firma-imp');
  await expect(page).toHaveURL(/\/admin\/impersonation$/);
  await expect(page.getByTestId('impersonation-banner')).toHaveCount(0);
  await expect(page.getByTestId('impersonation-ended')).toContainText('Terminó la vista como cliente. Volvió a su sesión de administrador.');
  // El menú vuelve a ser el del administrador (grupo Administración).
  await expect(page.getByRole('navigation', { name: 'Menú principal' }).getByRole('button', { name: 'Administración' })).toBeVisible();
  const ultima = page.locator('tr[data-testid^="impersonation-session-"]').first();
  await expect(ultima).toContainText('Carla Rojas');
  await expect(ultima).toContainText('Terminada');
  await expect(ultima).toContainText('1 bloqueada');
});

test('reportería: exportar transacciones con los filtros y el idioma (M9-01)', async ({ page }) => {
  await abrir(page, '/admin/reports/transactions', {}, 'admin');
  await expect(page.getByTestId('tx-totals')).toContainText('Total en CLP');
  await expect(page.getByTestId('tx-summary-THC-CLP')).toContainText('Cargos locales');
  await page.getByLabel('País', { exact: true }).selectOption('CL');
  await page.getByLabel('Categoría').selectOption('LocalCharge');
  await page.getByRole('button', { name: 'Consultar' }).click();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);

  const exportacion = page.waitForRequest((r) => r.url().includes('/api/v1/admin/reports/transactions/export'));
  const archivo = page.waitForEvent('download');
  await page.getByTestId('tx-export-xlsx').click();
  const pedido = new URL((await exportacion).url());
  expect(pedido.searchParams.get('format')).toBe('xlsx');
  expect(pedido.searchParams.get('language')).toBe('es');
  expect(pedido.searchParams.get('country')).toBe('CL');
  expect(pedido.searchParams.get('category')).toBe('LocalCharge');
  expect(pedido.searchParams.get('from')).toBe('2026-10-01');
  expect(pedido.searchParams.has('page')).toBe(false);
  expect((await archivo).suggestedFilename()).toBe('transacciones-20261001-20261006.xlsx');
  await expect(page.locator(POLITE)).toContainText('Se descargó transacciones-20261001-20261006.xlsx.');

  // Excepciones: aviso de IPO no disponible.
  await page.getByRole('link', { name: 'Ver las excepciones aplicadas' }).click();
  await expect(page.getByTestId('ex-ipo-unavailable')).toContainText('Nexus no respondió');
  await expect(page.getByTestId('ex-summary-CreditImputation')).toContainText('Imputación a crédito');
});

test('Counter: reintentar el envío a Nexus de un registro fallido (M8-09)', async ({ page }) => {
  await abrir(page, '/admin/counter', {}, 'admin');
  const fila = page.getByTestId(`counter-row-${BL_COUNTER_FALLIDO}`);
  await expect(fila).toContainText('Fallido');
  await expect(fila).toContainText('Nexus: timeout');
  const reintento = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/admin/counter/${BL_COUNTER_FALLIDO}/sync`));
  await fila.getByRole('button', { name: `Reintentar el envío a Nexus del BL ${BL_COUNTER_FALLIDO}` }).click();
  await reintento;
  await expect(page.locator(POLITE)).toContainText(`Counter del BL ${BL_COUNTER_FALLIDO} enviado a Nexus.`);
  await expect(fila).toContainText('Enviado');

  // Registrar el desconsolidado y verlo en el historial.
  await fila.getByRole('button', { name: `Editar el Counter del BL ${BL_COUNTER_FALLIDO}` }).click();
  await expect(page.locator('#counter-detail-title')).toBeFocused();
  await page.getByTestId('counter-form').getByRole('checkbox', { name: 'Desconsolidado' }).check();
  const guardado = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith(`/admin/counter/${BL_COUNTER_FALLIDO}`));
  await page.getByTestId('counter-save').click();
  const body = (await guardado).postDataJSON();
  expect(body).toMatchObject({ country: 'BO', deconsolidated: true, deconsolidatedAt: null, hblReceived: false });
  await expect(page.locator(POLITE)).toContainText(`Counter del BL ${BL_COUNTER_FALLIDO} guardado y enviado a Nexus.`);
  await page.getByTestId('counter-detail').getByRole('button', { name: 'Historial' }).click();
  await expect(page.getByTestId('counter-history')).toContainText('Modificación');
});

test('listas de contactos: actualizar los correos de un reporte y verlo en el historial (M1-06)', async ({ page }) => {
  await abrir(page, '/organization');
  const fila = page.getByTestId('contact-list-BL_COPIES');
  await expect(fila).toContainText('documentos@importadoraandes.cl');
  await fila.getByRole('button', { name: 'Editar los correos de Copias de BL' }).click();
  const campo = page.locator('#contact-emails-BL_COPIES');
  await expect(campo).toBeFocused();
  await campo.fill('Documentos@ImportadoraAndes.cl\ncopias@importadoraandes.cl, documentos@importadoraandes.cl');
  const guardado = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith('/organizations/me/contact-lists/BL_COPIES'));
  await fila.getByRole('button', { name: 'Guardar' }).click();
  expect((await guardado).postDataJSON()).toEqual({ emails: ['documentos@importadoraandes.cl', 'copias@importadoraandes.cl'] });
  await expect(page.locator(POLITE)).toContainText('Copias de BL: lista actualizada con 2 correos.');
  await expect(fila).toContainText('copias@importadoraandes.cl');
  await expect(page.getByTestId('contact-lists-history').locator('tbody tr').first()).toContainText('Enviado al origen');
});

test('transportista pre-creado: crear con BL, asignar otro y la solicitud de registro detecta la cuenta (M1-09)', async ({ page }) => {
  await abrir(page, '/organization');
  await expect(page.getByTestId(`carrier-${TRANSPORTISTA.taxId}`)).toContainText('Pendiente de activación');
  await page.getByTestId('carrier-new').click();
  await page.getByLabel('Razón social').fill('Fletes del Sur SpA');
  await page.getByLabel('RUT o NIT').fill('76111222-3');
  await page.getByLabel('Correo de contacto').fill('Operaciones@FletesDelSur.cl');
  await page.getByTestId('carrier-form').getByLabel('Números de BL').fill('hlcu0000001, HLCU9999999');
  const creacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/organizations/me/carriers'));
  await page.getByTestId('carrier-submit').click();
  expect((await creacion).postDataJSON()).toMatchObject({
    legalName: 'Fletes del Sur SpA', taxId: '76111222-3', email: 'operaciones@fletesdelsur.cl', country: 'CL',
    blNumbers: ['HLCU0000001', 'HLCU9999999'], bookingNumbers: [],
  });
  const resultado = page.getByTestId('carrier-result');
  await expect(resultado.getByRole('heading')).toHaveText('Fletes del Sur SpA pre-creado; 1 embarque asignado.');
  await expect(resultado).toContainText('Se envió la invitación a operaciones@fletesdelsur.cl.');
  await expect(resultado).toContainText('HLCU9999999');

  // Asignar un booking al transportista pendiente.
  const cordillera = page.getByTestId(`carrier-${TRANSPORTISTA.taxId}`);
  await cordillera.getByRole('button', { name: `Asignar BL o bookings a ${TRANSPORTISTA.name}` }).click();
  await cordillera.getByLabel('Números de booking').fill('BKG26039999');
  const asignacion = page.waitForRequest((r) => r.method() === 'POST' && /\/organizations\/me\/carriers\/[^/]+\/assignments$/.test(r.url()));
  await cordillera.getByTestId('carrier-assign-submit').click();
  expect((await asignacion).postDataJSON()).toEqual({ blNumbers: [], bookingNumbers: ['BKG26039999'] });
  await expect(page.getByTestId(`carrier-${TRANSPORTISTA.taxId}`)).toContainText('BKG26039999');
});

test('la solicitud de vinculación con una cuenta pre-creada dirige al ingreso y reenvía la invitación (M1-09)', async ({ page }) => {
  await abrir(page, '/register/join', {}, 'ninguna');
  await page.locator('#joinTaxId').fill(TRANSPORTISTA.taxId);
  await page.locator('#joinFirstName').fill('Rodrigo');
  await page.locator('#joinLastName').fill('Valdés');
  await page.locator('#joinEmail').fill(TRANSPORTISTA.email);
  await page.locator('#joinPassword').fill('Clave-Segura-2026');
  await page.locator('#joinConfirmPassword').fill('Clave-Segura-2026');
  await page.locator('form button[type="submit"]').click();
  const aviso = page.getByTestId('pre-created-notice');
  await expect(aviso.getByRole('heading')).toHaveText('Ya existe una cuenta para este transportista');
  await expect(aviso.getByRole('link', { name: 'Ir al ingreso' })).toHaveAttribute('href', '/login');
  const reenvio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/auth/register/pre-created/resend-invitation'));
  await aviso.getByTestId('pre-created-resend').click();
  expect((await reenvio).postDataJSON()).toEqual({ email: TRANSPORTISTA.email });
  await expect(aviso.getByRole('status')).toHaveText('Si existe una cuenta pre-creada con ese correo, enviamos una nueva invitación.');
});

test('empresa matriz: ve el BL de la filial con su organización de origen y filtra por filial (M1-21)', async ({ page }) => {
  await abrir(page, '/shipments', { matriz: true });
  await expect(page.getByRole('columnheader', { name: 'Organización de origen' })).toBeVisible();
  await expect(page.getByTestId(`origin-${BL_FILIAL}`)).toContainText(FILIAL.name);
  await expect(page.getByTestId('origin-HLCU0000001')).toHaveText('Propia');

  const consulta = page.waitForRequest((r) => r.method() === 'GET' && /\/api\/v1\/shipments\?/.test(r.url()) && r.url().includes('organizationId'));
  await page.getByTestId('shipments-filter-organization').selectOption(FILIAL.id);
  await page.getByRole('button', { name: 'Buscar' }).click();
  expect(new URL((await consulta).url()).searchParams.get('organizationId')).toBe(FILIAL.id);
  await expect(page).toHaveURL(new RegExp(`organizationId=${FILIAL.id}`));
  await expect(page.locator('main table tbody tr')).toHaveCount(1);
  await expect(page.getByTestId(`origin-${BL_FILIAL}`)).toContainText(FILIAL.taxId);

  // Detalle del BL de la filial: solo consulta, con la organización de origen.
  await page.getByRole('link', { name: BL_FILIAL }).click();
  await expect(page.getByTestId('shipment-origin-organization')).toContainText(`BL de la filial ${FILIAL.name}`);
});

test('Counter en el detalle del embarque para perfiles internos, con el acceso al registro (M8-09)', async ({ page }) => {
  await abrir(page, '/shipments/HLCU0000001', {}, 'admin');
  const bloque = page.getByTestId('shipment-counter');
  await expect(bloque.getByRole('heading')).toHaveText('Counter');
  await expect(bloque).toContainText('Enviado');
  await bloque.getByRole('link', { name: 'Editar en Counter' }).click();
  await expect(page).toHaveURL(/\/admin\/counter\?bl=HLCU0000001$/);
  await expect(page.getByTestId('counter-detail').getByRole('heading', { level: 2 }).first()).toHaveText('Counter del BL HLCU0000001');
});

test('área de administración: secciones con contadores y accesos directos según permisos (M8-05)', async ({ page }) => {
  await abrir(page, '/dashboard', {}, 'admin');
  await page.getByRole('navigation', { name: 'Menú principal' }).getByRole('button', { name: 'Administración' }).click();
  await page.getByTestId('sidebar-admin-home').click();
  await expect(page).toHaveURL(/\/admin$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Administración');
  await expect(page.getByTestId('admin-counters-counter')).toContainText('1 envío fallido a Nexus');
  await expect(page.getByTestId('admin-counters-parent-links')).toContainText('1 solicitud pendiente');
  await page.getByTestId('admin-section-impersonation').getByRole('link', { name: 'Ver el portal como cliente' }).click();
  await expect(page).toHaveURL(/\/admin\/impersonation$/);
});
