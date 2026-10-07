import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { EMBARQUES, simularApi } from '../fixtures/api-mocks';
import { BL_LIBERACION, UNIDADES } from '../fixtures/ola-j-mocks';
import { ORGANIZACION_PRUEBA, sembrarSesion } from '../fixtures/session';
import type { ShipmentListItem } from '../../src/app/core/models/shipment.model';

/**
 * Carta de liberación y desconsolidado (M6-08) en su propia página: selector de BL de importación de Bolivia, la
 * información del BL y el formulario de la carta del BL elegido; el BL viaja en ?bl=. El acceso del menú solo se
 * muestra a quien opera en Bolivia.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

/** BL de importación de Bolivia del mock (la carta no aplica a este) y el BL con la carta simulada (Ola J). */
const BL_OTRO = EMBARQUES.find((e) => e.country === 'BO' && e.operation === 'IMPORT') as ShipmentListItem;
const BL_CARTA: ShipmentListItem = {
  ...BL_OTRO,
  id: '3f0c2a1e-0000-4000-8000-000000000045',
  blNumber: BL_LIBERACION,
  bookingNumber: 'HLCUBKG2601045',
  vessel: 'Santos Express',
  voyage: '2603E',
  portOfDischarge: 'Arica',
  eta: '2026-10-01T08:00:00Z',
};

async function abrir(page: Page, ruta: string, embarques: ShipmentListItem[] = [BL_CARTA, BL_OTRO]): Promise<URL[]> {
  const consultas: URL[] = [];
  await simularApi(page);
  await page.route(
    (url) => url.pathname.endsWith('/api/v1/shipments'),
    async (route) => {
      const url = new URL(route.request().url());
      consultas.push(url);
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: embarques, total: embarques.length, page: 1, pageSize: 100 }),
      });
    },
  );
  await page.route(`**/api/v1/shipments/${BL_LIBERACION}`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        ...BL_CARTA,
        placeOfDelivery: 'La Paz',
        shipper: 'Shanghai Export Co.',
        consignee: 'Comercial Altiplano SRL',
        allowedActions: ['shipment.view'],
        canOperate: true,
        canSelfAssociate: false,
        requiresAssociationForPayment: false,
        freight: null,
        localCharges: null,
        demurrageCharges: null,
        serviceOrders: [],
        containers: Object.values(UNIDADES).map((n, i) => ({
          id: `c-${i}`, containerNumber: n, type: i === 0 ? 'DV' : 'HC', size: i === 0 ? '20' : '40', sealNumber: '', weight: 0, packages: 0, description: '',
        })),
      }),
    }),
  );
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  return consultas;
}

test('el menú lleva a la carta de liberación; el selector muestra la información del BL y su formulario', async ({ page }) => {
  const consultas = await abrir(page, '/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await menu.getByRole('button', { name: 'Documentos y trámites' }).click();
  await menu.getByRole('link', { name: 'Carta de liberación' }).click();
  await expect(page).toHaveURL(/\/release-letter$/);

  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Carta de liberación y desconsolidado');
  const selector = page.getByTestId('release-letter-bl-select');
  await expect(selector).toHaveAccessibleName('BL de importación (Bolivia)');
  await expect(page.getByTestId('release-letter-prompt')).toBeVisible();
  // Solo los BL de importación de Bolivia del usuario.
  const consulta = consultas.at(-1);
  expect(consulta?.searchParams.get('operation')).toBe('IMPORT');
  expect(consulta?.searchParams.get('country')).toBe('BO');
  expect(consulta?.searchParams.get('pageSize')).toBe('100');

  await selector.selectOption(BL_LIBERACION);
  await expect(page).toHaveURL(new RegExp(`/release-letter\\?bl=${BL_LIBERACION}$`));

  const info = page.getByTestId('release-letter-bl-info');
  await expect(info.getByRole('heading', { level: 2 })).toHaveText('Información del BL');
  await expect(page.getByTestId('release-letter-info-bl')).toHaveText(BL_LIBERACION);
  await expect(page.getByTestId('release-letter-info-vessel')).toContainText('Santos Express / 2603E');
  await expect(page.getByTestId('release-letter-info-consignee')).toHaveText('Comercial Altiplano SRL');
  await expect(page.getByTestId('release-letter-info-containers').getByRole('listitem')).toHaveCount(3);

  // El formulario del BL, sin su propio encabezado: la página conserva un solo h1.
  await expect(page.getByTestId('release-form')).toBeVisible();
  await expect(page.getByTestId(`release-container-${UNIDADES.CON_TATC}`)).toBeVisible();
  await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe carta de liberación ${tema}`).toEqual([]);
  }
});

test('un enlace con ?bl= abre el BL elegido', async ({ page }) => {
  await abrir(page, `/release-letter?bl=${BL_LIBERACION}`);
  await expect(page.getByTestId('release-letter-bl-select')).toHaveValue(BL_LIBERACION);
  await expect(page.getByTestId('release-letter-info-bl')).toHaveText(BL_LIBERACION);
  await expect(page.getByTestId('release-form')).toBeVisible();
});

test('con un solo BL queda elegido', async ({ page }) => {
  await abrir(page, '/release-letter', [BL_CARTA]);
  await expect(page).toHaveURL(new RegExp(`\\?bl=${BL_LIBERACION}$`));
  await expect(page.getByTestId('release-letter-bl-select')).toHaveValue(BL_LIBERACION);
  await expect(page.getByTestId('release-form')).toBeVisible();
});

test('sin BL de importación de Bolivia muestra un estado vacío con acceso a los embarques', async ({ page }) => {
  await abrir(page, '/release-letter', []);
  const vacio = page.getByTestId('release-letter-empty');
  await expect(vacio.getByRole('heading', { level: 2 })).toHaveText('No tiene BL de importación en Bolivia');
  await expect(vacio.getByRole('link', { name: 'Ver mis embarques' })).toHaveAttribute('href', '/shipments');
  await expect(page.getByTestId('release-letter-bl-select')).toHaveCount(0);
  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe carta de liberación vacía ${tema}`).toEqual([]);
  }
});

test('quien no opera en Bolivia no ve el acceso en el menú', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es', organizacion: { ...ORGANIZACION_PRUEBA, operatingCountries: ['CL'] } });
  await page.goto('/dashboard');
  const menu = page.getByRole('navigation', { name: 'Menú principal' });
  await menu.getByRole('button', { name: 'Documentos y trámites' }).click();
  await expect(menu.getByRole('link', { name: 'Tarifas locales' })).toBeVisible();
  await expect(menu.getByRole('link', { name: 'Carta de liberación' })).toHaveCount(0);
});
