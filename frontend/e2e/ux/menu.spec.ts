import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Menú principal (app-main-nav): barra con paneles desplegables por grupo en escritorio. Cada grupo es un botón de
 * divulgación; Esc cierra y devuelve el foco; solo se muestran los grupos que el perfil puede usar.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

test('los grupos se abren con teclado, Esc cierra y el foco vuelve al grupo', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  const pagos = menu.getByRole('button', { name: 'Pagos y facturación' });

  await expect(menu.getByRole('link', { name: 'Inicio' })).toHaveAttribute('aria-current', 'page');
  await expect(pagos).toHaveAttribute('aria-expanded', 'false');
  await expect(menu.getByRole('link', { name: 'Facturas' })).toBeHidden();

  await pagos.focus();
  await page.keyboard.press('Enter');
  await expect(pagos).toHaveAttribute('aria-expanded', 'true');
  await expect(menu.getByRole('link', { name: 'Facturas' })).toBeVisible();

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema });
    const axe = await new AxeBuilder({ page }).include('.hl-mainnav').withTags(TAGS).analyze();
    expect(axe.violations, `axe menú ${tema}`).toEqual([]);
  }

  await page.keyboard.press('Escape');
  await expect(pagos).toHaveAttribute('aria-expanded', 'false');
  await expect(pagos).toBeFocused();

  // Navegar cierra el panel y marca el grupo de la página actual.
  await pagos.click();
  await menu.getByRole('link', { name: 'Facturas' }).click();
  await expect(page).toHaveURL(/\/invoices$/);
  await expect(pagos).toHaveAttribute('aria-expanded', 'false');
  await expect(pagos).toHaveClass(/is-active/);
});

test('el cliente no ve Operación ni Administración; el administrador sí', async ({ page, browser }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await expect(menu.getByRole('button', { name: 'Embarques' })).toBeVisible();
  await expect(menu.getByRole('button', { name: 'Operación' })).toHaveCount(0);
  await expect(menu.getByRole('button', { name: 'Administración' })).toHaveCount(0);

  const admin = await browser.newPage();
  await simularApi(admin);
  await sembrarSesionAdmin(admin, 'es');
  await admin.goto('/dashboard');
  const menuAdmin = admin.getByRole('navigation', { name: 'Menú principal' });
  await menuAdmin.getByRole('button', { name: 'Administración' }).click();
  await expect(menuAdmin.getByRole('link', { name: 'Usuarios' })).toBeVisible();
  await admin.close();
});

test('menú del usuario sin Bootstrap JS: abre con clic, Esc cierra y devuelve el foco, navegar lo cierra', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  const boton = page.locator('header button[aria-controls="hl-user-menu"]');
  const lista = page.locator('#hl-user-menu');

  await boton.click();
  await expect(boton).toHaveAttribute('aria-expanded', 'true');
  await expect(lista).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(lista).toBeHidden();
  await expect(boton).toBeFocused();

  await boton.click();
  await lista.getByRole('link', { name: 'Mi perfil' }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(lista).toBeHidden();
});
