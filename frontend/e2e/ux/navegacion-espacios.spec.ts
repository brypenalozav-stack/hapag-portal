import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, BL_SIN_FLETE, simularApi } from '../fixtures/api-mocks';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Menú y barra superior (cierre de Fase 1, fase 4 del plan de UX):
 * - el cliente ve un menú por tareas de cinco entradas (Inicio, Embarques, Pagos, Documentos y trámites, Ayuda);
 * - los perfiles internos alternan entre "Portal clientes" y "Backoffice"; el backoffice solo muestra grupos internos, la
 *   elección se recuerda y entrar a una ruta /admin cambia de espacio;
 * - la búsqueda universal lleva del número de BL, booking o contenedor al detalle del embarque;
 * - grupos, menú del usuario y búsqueda móvil siguen el patrón de divulgación (Enter abre, Esc cierra y devuelve el foco);
 * - axe sin infracciones en la barra y el menú, en tema claro y oscuro.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

function menuPrincipal(page: Page) {
  return page.getByRole('navigation', { name: 'Menú principal' });
}

/** Nombres de las entradas de primer nivel del menú de escritorio. */
async function entradas(page: Page): Promise<string[]> {
  const textos = await page.locator('.hl-mainnav__bar > li > .hl-mainnav__top').allInnerTexts();
  return textos.map((t) => t.trim());
}

async function abrirCliente(page: Page, ruta = '/dashboard'): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
}

async function abrirAdmin(page: Page, ruta = '/dashboard'): Promise<void> {
  await simularApi(page);
  await sembrarSesionAdmin(page, 'es');
  await page.goto(ruta);
  await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible();
}

test('el cliente ve como máximo cinco entradas por tarea y sin funciones internas ni selector de espacio', async ({ page }) => {
  await abrirCliente(page);
  const nombres = await entradas(page);
  expect(nombres.length).toBeLessThanOrEqual(5);
  expect(nombres).toEqual(['Inicio', 'Embarques', 'Pagos', 'Documentos y trámites', 'Ayuda']);
  await expect(page.getByTestId('workspace-switcher')).toHaveCount(0);
  await expect(page.locator('a[href^="/admin"]')).toHaveCount(0);

  // Ningún grupo repite su nombre en su único enlace; Mi organización está en el menú del usuario.
  const menu = menuPrincipal(page);
  await menu.getByRole('button', { name: 'Embarques' }).click();
  await expect(menu.getByRole('link', { name: 'Mis embarques' })).toBeVisible();
  await expect(menu.getByRole('link', { name: 'Consulta de BL' })).toBeVisible();
  await expect(menu.locator('a[href="/organization"]')).toHaveCount(0);
  await page.getByRole('button', { name: /Menú de usuario/ }).click();
  await expect(page.getByTestId('user-menu-organization')).toBeVisible();

  // Tema e idioma están en las preferencias del menú del usuario, no en la barra.
  await expect(page.locator('#hl-theme')).toHaveCount(0);
  await expect(page.locator('#hl-user-menu').getByRole('group', { name: 'Tema' })).toBeVisible();
  await expect(page.locator('#hl-user-menu').getByRole('group', { name: 'Idioma' })).toBeVisible();
});

test('el perfil interno entra al backoffice, cambia al portal de clientes y la elección se recuerda', async ({ page }) => {
  await abrirAdmin(page);
  const selector = page.getByTestId('workspace-switcher');
  const backoffice = selector.getByRole('button', { name: 'Backoffice' });
  const clientes = selector.getByRole('button', { name: 'Portal clientes' });
  await expect(backoffice).toHaveAttribute('aria-pressed', 'true');
  expect(await entradas(page)).toEqual(['Inicio', 'Operación', 'Finanzas', 'Reportes y auditoría', 'Administración']);
  await expect(menuPrincipal(page).locator('a[href="/cart"], a[href="/invoices"], a[href="/faq"]')).toHaveCount(0);

  await clientes.click();
  await expect(clientes).toHaveAttribute('aria-pressed', 'true');
  const cliente = await entradas(page);
  expect(cliente).toContain('Embarques');
  expect(cliente).not.toContain('Operación');
  expect(cliente).not.toContain('Administración');
  await expect(menuPrincipal(page).locator('a[href^="/admin"]')).toHaveCount(0);

  // La elección se recuerda al volver a cargar.
  await page.reload();
  await expect(page.getByTestId('workspace-switcher').getByRole('button', { name: 'Portal clientes' })).toHaveAttribute('aria-pressed', 'true');

  // Entrar a una ruta del backoffice cambia de espacio.
  await page.goto('/admin/users');
  await expect(page.getByTestId('workspace-switcher').getByRole('button', { name: 'Backoffice' })).toHaveAttribute('aria-pressed', 'true');
  await expect(menuPrincipal(page).getByRole('button', { name: 'Administración' })).toHaveClass(/is-active/);
});

test('la búsqueda universal lleva del BL, booking o contenedor al detalle del embarque', async ({ page }) => {
  await abrirCliente(page);
  const campo = page.getByRole('searchbox', { name: 'Buscador de embarques' });

  // BL: Enter envía la búsqueda y abre el detalle.
  await campo.fill(BL_PRUEBA.blNumber.toLowerCase());
  await campo.press('Enter');
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_PRUEBA.blNumber}$`));
  await expect(campo).toHaveValue('');

  // Booking: un único resultado lleva al detalle de su BL.
  await campo.fill('BKG26030072');
  await page.getByRole('button', { name: 'Ir al embarque' }).click();
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_SIN_FLETE}$`));

  // Contenedor: el BL sale de GET /demurrage/container/{n} (filtrado por los accesos en el servidor).
  await page.route('**/api/v1/demurrage/container/**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ blNumber: BL_PRUEBA.blNumber, containerNumber: 'MSKU1234567' }]) }),
  );
  await campo.fill('MSKU 123456 7');
  await campo.press('Enter');
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_PRUEBA.blNumber}$`));

  // Sin resultados: aviso en la región viva, sin navegar.
  await campo.fill('ZZZ999');
  await campo.press('Enter');
  await expect(page.getByTestId('global-search-message')).toContainText('No encontramos un embarque');
  await expect(page).toHaveURL(new RegExp(`/shipments/${BL_PRUEBA.blNumber}$`));

  // Atajo "/": lleva el foco al campo desde la página.
  await page.locator('main').click({ position: { x: 5, y: 5 } });
  await page.keyboard.press('/');
  await expect(campo).toBeFocused();
});

test('teclado: grupos, menú del usuario y búsqueda móvil se abren con Enter y Esc devuelve el foco', async ({ page }) => {
  await abrirCliente(page);
  const pagos = menuPrincipal(page).getByRole('button', { name: 'Pagos' });
  await pagos.focus();
  await page.keyboard.press('Enter');
  await expect(pagos).toHaveAttribute('aria-expanded', 'true');
  await expect(menuPrincipal(page).getByRole('link', { name: 'Facturas' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(pagos).toHaveAttribute('aria-expanded', 'false');
  await expect(pagos).toBeFocused();

  const usuario = page.getByRole('button', { name: /Menú de usuario/ });
  await usuario.focus();
  await page.keyboard.press('Enter');
  await expect(usuario).toHaveAttribute('aria-expanded', 'true');
  await page.keyboard.press('Escape');
  await expect(usuario).toHaveAttribute('aria-expanded', 'false');
  await expect(usuario).toBeFocused();

  // Pantalla angosta: la lupa despliega el campo; Esc lo cierra y devuelve el foco a la lupa.
  await page.setViewportSize({ width: 375, height: 812 });
  const lupa = page.getByTestId('search-toggle');
  await expect(lupa).toBeVisible();
  await expect(page.getByRole('searchbox', { name: 'Buscador de embarques' })).toBeHidden();
  await lupa.focus();
  await page.keyboard.press('Enter');
  await expect(lupa).toHaveAttribute('aria-expanded', 'true');
  const campo = page.getByRole('searchbox', { name: 'Buscador de embarques' });
  await expect(campo).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(lupa).toHaveAttribute('aria-expanded', 'false');
  await expect(lupa).toBeFocused();
});

for (const perfil of ['cliente', 'interno'] as const) {
  test(`axe sin infracciones en la barra y el menú (${perfil}), tema claro y oscuro`, async ({ page }) => {
    if (perfil === 'cliente') await abrirCliente(page);
    else await abrirAdmin(page);
    const grupo = perfil === 'cliente' ? 'Documentos y trámites' : 'Administración';
    for (const tema of ['light', 'dark'] as const) {
      // Sin animaciones (como las demás pruebas de axe): con el panel aún apareciendo, axe medía el texto a medio fundido.
      await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
      await menuPrincipal(page).getByRole('button', { name: grupo }).click();
      // Los enlaces del panel con texto ya traducido (el de Dispute llega después, con la configuración del país).
      for (const enlace of await page.locator('header .hl-mega:visible a').all()) await expect(enlace).not.toHaveText('');
      const menu = await new AxeBuilder({ page }).include('header').withTags(TAGS).analyze();
      expect(menu.violations, `axe menú ${perfil} ${tema}`).toEqual([]);
      await page.keyboard.press('Escape');

      await page.getByRole('button', { name: /Menú de usuario/ }).click();
      const usuario = await new AxeBuilder({ page }).include('header').withTags(TAGS).analyze();
      expect(usuario.violations, `axe menú del usuario ${perfil} ${tema}`).toEqual([]);
      await page.keyboard.press('Escape');
    }
  });
}
