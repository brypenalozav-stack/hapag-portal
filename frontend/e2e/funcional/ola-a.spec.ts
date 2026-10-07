import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { BL_EXPORTACION, BL_PRUEBA, BL_SIN_FLETE, simularApi } from '../fixtures/api-mocks';
import { sembrarIdioma, sembrarSesion } from '../fixtures/session';
import { cargarSeccionesDiferidas } from '../fixtures/detalle';

/**
 * Fase 1, Ola A (pruebas funcionales con el backend simulado):
 * - M1-07: registro de una organización con su tipo; queda pendiente de aprobación.
 * - M1-08: el ingreso de una solicitud de vinculación no aprobada muestra un mensaje claro.
 * - M2-06 / M2-07: listado de embarques con separación importación/exportación que se mantiene
 *   durante la navegación; la consulta de BL antigua redirige al detalle nuevo.
 * - M1-11: el detalle no muestra el flete cuando el servidor lo envía en null.
 * - M1-04: el selector de país cambia el país de operación y renueva el token.
 */

async function abrirConSesion(page: Page, ruta: string): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  // Detalle del BL: los grupos bajo el pliegue se cargan al entrar en pantalla (@defer on viewport).
  if (/^\/(shipments|bills-of-lading)\/[^/?]+$/.test(ruta)) await cargarSeccionesDiferidas(page);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

test('registro de organización con tipo: queda pendiente de aprobación (M1-07)', async ({ page }) => {
  await simularApi(page);
  await sembrarIdioma(page, 'es');
  await page.goto('/register');

  await page.getByText('Freight forwarder (FFWW)').click();
  await expect(page.getByLabel('Freight forwarder (FFWW)')).toBeChecked();

  await page.getByLabel('Razón social').fill('Logística Andina Ltda.');
  await page.getByLabel('RUT', { exact: true }).fill('77.888.999-0');
  await page.getByLabel('Teléfono').fill('+56 2 2777 8888');
  await page.getByLabel('Nombre del contacto').fill('Andrés');
  await page.getByLabel('Apellido del contacto').fill('Molina');
  await page.getByLabel('Correo Electrónico').fill('admin@logisticaandina.cl');
  await page.getByLabel('Contraseña', { exact: true }).fill('Clave#2026');
  await page.getByLabel('Confirmar Contraseña').fill('Clave#2026');

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/auth/register'));
  await page.getByRole('button', { name: 'Crear Cuenta' }).click();
  const cuerpo = (await envio).postDataJSON();
  expect(cuerpo).toMatchObject({
    organizationType: 'FreightForwarder',
    name: 'Logística Andina Ltda.',
    taxId: '77.888.999-0',
    country: 'CL',
    contactFirstName: 'Andrés',
    contactLastName: 'Molina',
  });

  const resultado = page.getByRole('status').filter({ hasText: 'pendiente de aprobación' });
  await expect(resultado).toBeVisible();
  await expect(resultado).toContainText('Logística Andina Ltda.');
  await expect(page.getByRole('link', { name: 'Ir al inicio de sesión' })).toBeVisible();
});

test('el ingreso con solicitud de vinculación pendiente muestra un mensaje claro (M1-08)', async ({ page }) => {
  await simularApi(page);
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      status: 400,
      contentType: 'application/problem+json',
      body: JSON.stringify({ title: 'User.PendingApproval', status: 400 }),
    }),
  );
  await sembrarIdioma(page, 'es');
  await page.goto('/login');

  await page.getByLabel('Correo Electrónico').fill('solicitud@importadorademo.cl');
  await page.getByLabel('Contraseña').fill('Admin123!');
  await page.getByRole('button', { name: 'Iniciar Sesión' }).click();

  await expect(page.getByRole('alert')).toContainText('aún no ha sido aprobada');
  await expect(page).toHaveURL(/\/login$/);
});

test('embarques: el filtro importación/exportación filtra y se mantiene al navegar (M2-06, M2-07)', async ({ page }) => {
  await abrirConSesion(page, '/shipments');

  const filas = page.locator('main table tbody tr');
  await expect(filas).toHaveCount(4);
  const todas = page.getByRole('button', { name: 'Todas' });
  await expect(todas).toHaveAttribute('aria-pressed', 'true');

  const consulta = page.waitForRequest(
    (r) => r.method() === 'GET' && /\/api\/v1\/shipments\?/.test(r.url()) && r.url().includes('operation=EXPORT'),
  );
  const exportacion = page.getByRole('button', { name: 'Exportación' });
  await exportacion.click();
  await consulta;

  await expect(exportacion).toHaveAttribute('aria-pressed', 'true');
  await expect(page).toHaveURL(/operation=EXPORT/);
  await expect(filas).toHaveCount(2);
  await expect(filas.filter({ hasText: BL_EXPORTACION })).toHaveCount(1);
  await expect(filas.filter({ hasText: BL_PRUEBA.blNumber })).toHaveCount(0);

  // La selección se mantiene al volver al listado desde el menú (M2-07).
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await menu.getByRole('button', { name: 'Pagos y facturación' }).click();
  await menu.getByRole('link', { name: 'Historial de pagos' }).click();
  await expect(page).toHaveURL(/\/payment-history$/);
  await menu.getByRole('button', { name: 'Embarques' }).click();
  await menu.getByRole('link', { name: 'Embarques', exact: true }).click();
  await expect(page).toHaveURL(/\/shipments$/);
  await expect(page.getByRole('button', { name: 'Exportación' })).toHaveAttribute('aria-pressed', 'true');
  await expect(filas).toHaveCount(2);

  // Importación muestra solo los embarques de importación.
  await page.getByRole('button', { name: 'Importación' }).click();
  await expect(page).toHaveURL(/operation=IMPORT/);
  await expect(filas).toHaveCount(2);
  await expect(filas.filter({ hasText: BL_PRUEBA.blNumber })).toHaveCount(1);
});

test('embarques: el filtro por booking acota el listado (M2-06)', async ({ page }) => {
  await abrirConSesion(page, '/shipments');

  await page.getByLabel('Booking').fill('BKG26030072');
  await page.getByRole('button', { name: 'Buscar' }).click();

  await expect(page).toHaveURL(/bookingNumber=BKG26030072/);
  const filas = page.locator('main table tbody tr');
  await expect(filas).toHaveCount(1);
  await expect(filas.first()).toContainText(BL_SIN_FLETE);
});

test('el detalle oculta el flete cuando el servidor lo envía en null (M1-11, M2-06)', async ({ page }) => {
  await abrirConSesion(page, `/shipments/${BL_SIN_FLETE}`);

  await expect(page.getByRole('heading', { level: 1 })).toContainText(BL_SIN_FLETE);
  await expect(page.getByRole('heading', { name: 'Datos del embarque' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Flete' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Agregar flete al carro' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Demurrage' })).toHaveCount(0);
  // Exportación: muestra la sección de órdenes de servicio (CL-EXP-13).
  await expect(page.getByRole('heading', { name: 'Órdenes de servicio (ODS)' })).toBeVisible();

  // Con permiso (consignee, puede operar) el flete y su botón de pago (por el carro, Ola D) sí aparecen.
  await page.goto(`/shipments/${BL_PRUEBA.blNumber}`);
  await cargarSeccionesDiferidas(page);
  await expect(page.getByRole('heading', { name: 'Flete' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Agregar flete al carro' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Demurrage' })).toBeVisible();
});

test('la consulta de BL antigua redirige al detalle del embarque', async ({ page }) => {
  await abrirConSesion(page, `/bills-of-lading/${BL_PRUEBA.blNumber}`);
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_PRUEBA.blNumber}$`));
});

test('el selector de país cambia el país de operación y renueva el token (M1-04)', async ({ page }) => {
  await abrirConSesion(page, '/dashboard');

  const grupo = page.getByRole('group', { name: 'País de operación' });
  const bolivia = grupo.getByRole('button', { name: 'BO, Bolivia' });
  await expect(grupo.getByRole('button', { name: 'CL, Chile' })).toHaveAttribute('aria-pressed', 'true');
  await expect(bolivia).toHaveAttribute('aria-pressed', 'false');

  const cambio = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith('/organizations/me/operating-country'));
  const renovacion = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/auth/refresh-token'));
  await bolivia.click();
  expect((await cambio).postDataJSON()).toEqual({ country: 'BO' });
  await renovacion;

  await expect(bolivia).toHaveAttribute('aria-pressed', 'true');
  expect(await page.evaluate(() => JSON.parse(localStorage.getItem('hl_user') ?? '{}').country)).toBe('BO');
});

test('cerrar sesión llama al servidor y limpia la sesión local (M1-10)', async ({ page }) => {
  await abrirConSesion(page, '/dashboard');

  const salida = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/auth/logout'));
  await page.getByRole('button', { name: /Menú de usuario/ }).click();
  await page.getByRole('button', { name: 'Cerrar Sesión' }).click();
  const peticion = await salida;
  expect(peticion.postDataJSON()).toEqual({ refreshToken: 'refresh-de-prueba' });

  await expect(page).toHaveURL(/\/login$/);
  expect(await page.evaluate(() => localStorage.getItem('hl_token'))).toBeNull();
});

test('Mi organización muestra usuarios y solicitudes solo con los permisos del perfil (M1-02, M1-08)', async ({ page }) => {
  await abrirConSesion(page, '/organization');

  await expect(page.getByRole('heading', { name: 'Datos de la organización' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Documentación de respaldo' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Solicitudes para unirse a la organización' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Usuarios de la organización' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Aprobar a Paula Fuentes' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Activar a Diego Soto' })).toBeVisible();
});

test('un perfil de consulta no ve la gestión de usuarios ni la bandeja de solicitudes', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es', permisos: [] });
  await page.goto('/organization');

  await expect(page.getByRole('heading', { name: 'Datos de la organización' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Usuarios de la organización' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Solicitudes para unirse a la organización' })).toHaveCount(0);
});
