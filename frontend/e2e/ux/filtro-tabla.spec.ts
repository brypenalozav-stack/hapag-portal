import { Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesionAdmin } from '../fixtures/session';

/**
 * Filtro rápido, orden por columna y paginación en el navegador (ClientTable + app-table-filter + appSortHeader +
 * app-paginator) en el mantenedor de tarifas, que carga el listado completo sin paginar en el servidor.
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const TOTAL = 25;

function tarifa(id: number, conceptCode: string, code: string, amount: number) {
  return {
    id: `f3000000-0000-4000-8000-${String(id).padStart(12, '0')}`,
    conceptCode,
    conceptName: null,
    code,
    country: 'CL',
    currency: 'CLP',
    containerType: null,
    description: null,
    amount,
    tierUnit: 'None',
    tierMode: 'Flat',
    tiers: [],
    validFrom: '2026-10-01',
    validTo: null,
    isActive: true,
    createdAt: '2026-09-30T12:00:00Z',
    createdBy: 'admin@hapag-lloyd.cl',
    modifiedAt: null,
    modifiedBy: null,
  };
}

/** 25 tarifas: 3 de «Cambio de almacén» (KTF primero, KTE y KTG al final) y 22 de «Llegada tardía» desordenadas. */
const TARIFAS = [
  tarifa(1, 'WAREHOUSE_CHANGE', 'KTF', 110910),
  ...Array.from({ length: 22 }, (_, i) => {
    const n = ((i * 7) % 22) + 1;
    return tarifa(100 + n, 'LATE_ARRIVAL', `LA-${String(n).padStart(2, '0')}`, 1000 * n);
  }),
  tarifa(2, 'WAREHOUSE_CHANGE', 'KTE', 9940),
  tarifa(3, 'WAREHOUSE_CHANGE', 'KTG', 5000),
];

async function abrirTarifas(page: Page): Promise<void> {
  await simularApi(page);
  await page.route(
    (url) => url.pathname.endsWith('/api/v1/tariffs'),
    (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(TARIFAS) }),
  );
  await sembrarSesionAdmin(page, 'es');
  await page.goto('/admin/tariffs');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('el filtro reduce las filas sin distinguir tildes y anuncia «N de M»', async ({ page }) => {
  await abrirTarifas(page);
  const tabla = page.getByRole('table', { name: 'Tarifas de cargos locales' });
  const filas = tabla.locator('tbody tr');
  const filtro = page.getByLabel('Filtrar la tabla');
  const conteo = page.getByRole('status').filter({ hasText: `de ${TOTAL}` });

  await expect(conteo).toHaveText(`${TOTAL} de ${TOTAL}`);
  await expect(filas).toHaveCount(20);

  // «almacen» sin tilde encuentra «Cambio de almacén».
  await filtro.fill('almacen');
  await expect(conteo).toHaveText(`3 de ${TOTAL}`);
  await expect(filas).toHaveCount(3);
  for (const fila of await filas.all()) await expect(fila).toContainText('Cambio de almacén');
  await expect(page.getByRole('navigation', { name: 'Paginación de tarifas' })).toHaveCount(0);

  await filtro.fill('LLEGADA TARDÍA');
  await expect(conteo).toHaveText(`22 de ${TOTAL}`);

  await filtro.fill('no existe');
  await expect(conteo).toHaveText(`0 de ${TOTAL}`);
  await expect(filas).toHaveCount(1);
  await expect(filas.first()).toHaveText('Ninguna fila coincide con el filtro.');

  await filtro.fill('');
  await expect(conteo).toHaveText(`${TOTAL} de ${TOTAL}`);
});

test('ordenar por columna reordena las filas y expone aria-sort', async ({ page }) => {
  await abrirTarifas(page);
  const tabla = page.getByRole('table', { name: 'Tarifas de cargos locales' });
  const primerCodigo = tabla.locator('tbody tr').first().getByRole('cell').nth(1);
  const codigo = tabla.getByRole('columnheader', { name: /^Código/ });

  await expect(primerCodigo).toHaveText('KTF');
  await expect(codigo).toHaveAttribute('aria-sort', 'none');

  await codigo.getByRole('button').click();
  await expect(codigo).toHaveAttribute('aria-sort', 'ascending');
  await expect(primerCodigo).toHaveText('KTE');

  await codigo.getByRole('button').click();
  await expect(codigo).toHaveAttribute('aria-sort', 'descending');
  await expect(primerCodigo).toHaveText('LA-22');

  await codigo.getByRole('button').click();
  await expect(codigo).toHaveAttribute('aria-sort', 'none');
  await expect(primerCodigo).toHaveText('KTF');
});

test('la paginación muestra la página 2 y el filtro vuelve a la primera', async ({ page }) => {
  await abrirTarifas(page);
  const filas = page.getByRole('table', { name: 'Tarifas de cargos locales' }).locator('tbody tr');
  const paginador = page.getByRole('navigation', { name: 'Paginación de tarifas' });

  await expect(paginador.getByTestId('tariffs-pager-range')).toHaveText(`Mostrando 1–20 de ${TOTAL}`);
  await paginador.getByRole('button', { name: 'Ir a la página 2' }).click();
  await expect(paginador.getByTestId('tariffs-pager-range')).toHaveText(`Mostrando 21–25 de ${TOTAL}`);
  await expect(paginador.getByRole('button', { name: 'Ir a la página 2' })).toHaveAttribute('aria-current', 'page');
  await expect(filas).toHaveCount(5);

  await page.getByLabel('Filtrar la tabla').fill('LA-');
  await expect(paginador.getByTestId('tariffs-pager-range')).toHaveText('Mostrando 1–20 de 22');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 1' })).toHaveAttribute('aria-current', 'page');
});

test('filtro, tabla y paginador sin violaciones de axe en tema claro y oscuro', async ({ page }) => {
  await abrirTarifas(page);
  await page.getByLabel('Filtrar la tabla').fill('a');
  await page.getByRole('table', { name: 'Tarifas de cargos locales' }).getByRole('columnheader', { name: /^Concepto/ }).getByRole('button').click();

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ reducedMotion: 'reduce', colorScheme: tema });
    const axe = await new AxeBuilder({ page }).include('app-tariffs .hl-card:has(table)').withTags(TAGS).analyze();
    expect(axe.violations, `axe filtro de tabla ${tema}`).toEqual([]);
  }
});
