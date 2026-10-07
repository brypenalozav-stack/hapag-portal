import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, simularApi } from '../fixtures/api-mocks';
import { FASE2 } from '../fixtures/funcionalidades';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Cierre de Fase 1: con los flags por defecto (GET /config/features) no aparece ningún punto de entrada de Fase 2 (menú,
 * barra superior, dashboard, detalle del embarque, organización) y una visita directa a una ruta de Fase 2 lleva al
 * dashboard. La carta de liberación (M6-08) y Counter (M8-09) quedan activos por decisión del usuario; la bandeja interna
 * de solicitudes sigue disponible para revisar las cartas.
 */

/** Enlaces de Fase 2 que ve el cliente cuando están encendidos. */
const RUTAS_CLIENTE = [
  '/account-statement',
  '/service-requests',
  '/service-requests/new',
  '/service-orders',
  '/announcements',
  '/notifications',
  '/warehouse/history',
];

/** Enlaces de Fase 2 del menú interno. */
const RUTAS_ADMIN = [
  '/admin',
  '/admin/service-definitions',
  '/admin/impersonation',
  '/admin/api-clients',
  '/admin/deadlines',
  '/admin/payments/deposit-proofs',
  '/admin/payments/settlements',
  '/admin/credit-imputation-rules',
  '/admin/reports/transactions',
  '/admin/reports/exceptions',
  '/admin/announcements',
  '/admin/guides',
  '/admin/organization-links',
];

async function sinEnlaces(page: Page, rutas: readonly string[]): Promise<void> {
  for (const ruta of rutas) {
    await expect(page.locator(`a[href="${ruta}"]`), `enlace a ${ruta}`).toHaveCount(0);
  }
}

test('el cliente no ve los puntos de entrada de Fase 2 con los flags por defecto', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await expect(menu.getByRole('button', { name: 'Embarques' })).toBeVisible();

  await sinEnlaces(page, RUTAS_CLIENTE);
  await expect(page.getByTestId('navbar-notifications')).toHaveCount(0);
  await expect(page.locator('app-announcements-banner')).toHaveCount(0);
  await expect(page.locator('app-guide-host')).toHaveCount(0);

  // Detalle del embarque: sin servicios on demand; organización: sin listas, transportistas ni empresa matriz.
  await page.goto(`/shipments/${BL_PRUEBA.blNumber}`);
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByTestId('available-services')).toHaveCount(0);
  await page.goto('/organization');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByTestId('contact-lists')).toHaveCount(0);
  await expect(page.getByTestId('carriers')).toHaveCount(0);
  await expect(page.getByTestId('parent-company')).toHaveCount(0);
});

test('el cliente que entra directo a una ruta de Fase 2 vuelve al dashboard', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  for (const ruta of ['/service-requests', '/account-statement', '/announcements', '/notifications', '/warehouse/history', '/service-orders']) {
    await page.goto(ruta);
    await expect(page, `visita a ${ruta}`).toHaveURL(/\/dashboard$/);
  }
});

test('el administrador no ve Fase 2 en el menú; la bandeja de solicitudes queda para la carta de liberación', async ({ page }) => {
  await simularApi(page);
  await sembrarSesionAdmin(page, 'es');
  await page.goto('/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await expect(menu.getByRole('button', { name: 'Administración' })).toBeVisible();

  await sinEnlaces(page, RUTAS_ADMIN);
  await expect(page.locator('a[href="/admin/service-requests"]').first()).toBeAttached();
  await expect(page.locator('a[href="/admin/counter"]').first()).toBeAttached();

  for (const ruta of ['/admin', '/admin/service-definitions', '/admin/impersonation', '/admin/api-clients', '/admin/reports/transactions']) {
    await page.goto(ruta);
    await expect(page, `visita a ${ruta}`).toHaveURL(/\/dashboard$/);
  }

  await page.goto('/admin/service-requests');
  await expect(page).toHaveURL(/\/admin\/service-requests$/);
});

test('con los flags encendidos por configuración vuelven los puntos de entrada de Fase 2', async ({ page }) => {
  await simularApi(page, { features: FASE2 });
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  await expect(page.getByTestId('navbar-notifications')).toBeVisible();
  await expect(page.locator('a[href="/service-requests/new"]').first()).toBeAttached();
  await page.goto('/account-statement');
  await expect(page).toHaveURL(/\/account-statement$/);
});
