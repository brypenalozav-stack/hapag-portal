import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { EMBARQUES, simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Paginador común (PaginatorComponent) en el listado de embarques: rango visible, números de página con
 * aria-current, y filas por página, que viaja al servidor y vuelve a la primera página.
 */

const TOTAL = 45;

/** 45 embarques a partir de los del mock, para tener varias páginas; respeta page y pageSize. */
async function simularMuchosEmbarques(page: Page): Promise<URL[]> {
  const consultas: URL[] = [];
  const items = Array.from({ length: TOTAL }, (_, i) => {
    const base = EMBARQUES[i % EMBARQUES.length];
    return { ...base, id: `${base.id}-${i}`, blNumber: `HLCUPAG26${String(i + 1).padStart(6, '0')}` };
  });
  await page.route(
    (url) => url.pathname.endsWith('/api/v1/shipments'),
    async (route) => {
      const url = new URL(route.request().url());
      consultas.push(url);
      const numero = Number(url.searchParams.get('page') ?? '1') || 1;
      const tamano = Number(url.searchParams.get('pageSize') ?? '20') || 20;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: items.slice((numero - 1) * tamano, numero * tamano), total: TOTAL, page: numero, pageSize: tamano }),
      });
    },
  );
  return consultas;
}

async function abrirEmbarques(page: Page): Promise<URL[]> {
  await simularApi(page);
  const consultas = await simularMuchosEmbarques(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/shipments');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
  return consultas;
}

test('muestra el rango y navega a la página 2 con aria-current en la página actual', async ({ page }) => {
  const consultas = await abrirEmbarques(page);
  const paginador = page.getByRole('navigation', { name: 'Paginación de embarques' });

  await expect(paginador.getByTestId('shipments-pager-range')).toHaveText('Mostrando 1–20 de 45');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 1' })).toHaveAttribute('aria-current', 'page');
  await expect(paginador.getByRole('button', { name: 'Anterior' })).toBeDisabled();

  await paginador.getByRole('button', { name: 'Ir a la página 2' }).click();
  await expect(paginador.getByTestId('shipments-pager-range')).toHaveText('Mostrando 21–40 de 45');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 2' })).toHaveAttribute('aria-current', 'page');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 1' })).not.toHaveAttribute('aria-current', 'page');
  expect(consultas.at(-1)?.searchParams.get('page')).toBe('2');
  await expect(page).toHaveURL(/[?&]page=2/);

  await paginador.getByRole('button', { name: 'Siguiente' }).click();
  await expect(paginador.getByTestId('shipments-pager-range')).toHaveText('Mostrando 41–45 de 45');
  await expect(paginador.getByRole('button', { name: 'Siguiente' })).toBeDisabled();
});

test('cambiar filas por página envía pageSize y vuelve a la primera página', async ({ page }) => {
  const consultas = await abrirEmbarques(page);
  const paginador = page.getByRole('navigation', { name: 'Paginación de embarques' });

  await paginador.getByRole('button', { name: 'Ir a la página 2' }).click();
  await expect(paginador.getByTestId('shipments-pager-range')).toHaveText('Mostrando 21–40 de 45');

  await paginador.getByLabel('Filas por página').selectOption('10');
  await expect(paginador.getByTestId('shipments-pager-range')).toHaveText('Mostrando 1–10 de 45');
  const ultima = consultas.at(-1);
  expect(ultima?.searchParams.get('pageSize')).toBe('10');
  expect(ultima?.searchParams.get('page')).toBe('1');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 1' })).toHaveAttribute('aria-current', 'page');
  await expect(paginador.getByRole('button', { name: 'Ir a la página 5' })).toBeVisible();
  await expect(page).toHaveURL(/[?&]pageSize=10/);
  await expect(page).not.toHaveURL(/[?&]page=2/);
});
