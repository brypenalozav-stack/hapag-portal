import { Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Orden por columna en el servidor (SortHeaderComponent): el encabezado expone aria-sort, el clic recorre
 * ascendente → descendente → orden por defecto, la consulta lleva `sort` y `direction` y vuelve a la página 1.
 * En embarques el orden queda en la URL (`sort`, `dir`).
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

/** Registra las consultas a un listado; la respuesta la sigue dando el mock común (que ignora el orden). */
async function espiar(page: Page, ruta: string): Promise<URL[]> {
  const consultas: URL[] = [];
  await page.route(
    (url) => url.pathname.endsWith(ruta),
    async (route) => {
      if (route.request().method() === 'GET') consultas.push(new URL(route.request().url()));
      await route.fallback();
    },
  );
  return consultas;
}

async function esperarListado(page: Page): Promise<void> {
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);
}

async function abrirEmbarques(page: Page): Promise<URL[]> {
  await simularApi(page);
  const consultas = await espiar(page, '/api/v1/shipments');
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/shipments');
  await esperarListado(page);
  return consultas;
}

test('el encabezado Nave ordena ascendente, descendente y vuelve al orden por defecto', async ({ page }) => {
  const consultas = await abrirEmbarques(page);
  const nave = page.getByRole('columnheader', { name: /^Nave/ });
  await expect(nave).toHaveAttribute('aria-sort', 'none');

  await nave.getByRole('button').click();
  await expect(page.getByRole('columnheader', { name: /^Nave/ })).toHaveAttribute('aria-sort', 'ascending');
  await expect.poll(() => consultas.at(-1)?.searchParams.get('direction')).toBe('asc');
  let ultima = consultas.at(-1);
  expect(ultima?.searchParams.get('sort')).toBe('vessel');
  expect(ultima?.searchParams.get('page')).toBe('1');
  await expect(page).toHaveURL(/[?&]sort=vessel/);
  await expect(page).toHaveURL(/[?&]dir=asc/);

  await page.getByRole('columnheader', { name: /^Nave/ }).getByRole('button').click();
  await expect(page.getByRole('columnheader', { name: /^Nave/ })).toHaveAttribute('aria-sort', 'descending');
  await expect.poll(() => consultas.at(-1)?.searchParams.get('direction')).toBe('desc');
  expect(consultas.at(-1)?.searchParams.get('sort')).toBe('vessel');
  await expect(page).toHaveURL(/[?&]dir=desc/);

  const antes = consultas.length;
  await page.getByRole('columnheader', { name: /^Nave/ }).getByRole('button').click();
  await expect(page.getByRole('columnheader', { name: /^Nave/ })).toHaveAttribute('aria-sort', 'none');
  await expect.poll(() => consultas.length).toBeGreaterThan(antes);
  ultima = consultas.at(-1);
  expect(ultima?.searchParams.has('sort')).toBe(false);
  expect(ultima?.searchParams.has('direction')).toBe(false);
  await expect(page).not.toHaveURL(/[?&]sort=/);
  await expect(page).not.toHaveURL(/[?&]dir=/);
});

test('el orden de embarques se lee de la URL al abrir', async ({ page }) => {
  await simularApi(page);
  const consultas = await espiar(page, '/api/v1/shipments');
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/shipments?sort=vessel&dir=desc');
  await esperarListado(page);
  await expect(page.getByRole('columnheader', { name: /^Nave/ })).toHaveAttribute('aria-sort', 'descending');
  const ultima = consultas.at(-1);
  expect(ultima?.searchParams.get('sort')).toBe('vessel');
  expect(ultima?.searchParams.get('direction')).toBe('desc');
});

test('en usuarios internos un clic en el encabezado envía el orden al servidor', async ({ page }) => {
  await simularApi(page);
  // El mock común no tiene el listado de usuarios internos: se responde aquí, sin ordenar.
  const consultas: URL[] = [];
  await page.route(
    (url) => url.pathname.endsWith('/api/v1/users'),
    async (route) => {
      const url = new URL(route.request().url());
      consultas.push(url);
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'u-1', displayId: 1001, fullName: 'Ana Rojas', email: 'ana.rojas@hlag.com', roles: ['Admin'], isActive: true },
            { id: 'u-2', displayId: 1002, fullName: 'Bruno Díaz', email: 'bruno.diaz@hlag.com', roles: ['Operador'], isActive: true },
          ],
          total: 2,
          page: Number(url.searchParams.get('page') ?? '1'),
          pageSize: Number(url.searchParams.get('pageSize') ?? '10'),
        }),
      });
    },
  );
  await sembrarSesionAdmin(page, 'es');
  await page.goto('/admin/users');
  await esperarListado(page);

  const correo = page.getByRole('columnheader', { name: /^Correo electrónico/ });
  await correo.getByRole('button').click();
  await expect(page.getByRole('columnheader', { name: /^Correo electrónico/ })).toHaveAttribute('aria-sort', 'ascending');
  await expect.poll(() => consultas.at(-1)?.searchParams.get('sort')).toBe('email');
  expect(consultas.at(-1)?.searchParams.get('direction')).toBe('asc');
  expect(consultas.at(-1)?.searchParams.get('page')).toBe('1');
});

test('la tabla de embarques con un encabezado ordenado no tiene violaciones de axe (claro y oscuro)', async ({ page }) => {
  await abrirEmbarques(page);
  await page.getByRole('columnheader', { name: /^Nave/ }).getByRole('button').click();
  await expect(page.getByRole('columnheader', { name: /^Nave/ })).toHaveAttribute('aria-sort', 'ascending');
  await expect(page.getByTestId('table-skeleton')).toHaveCount(0);

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page })
      .include('[role="region"][aria-label="Embarques accesibles"]')
      .withTags(TAGS)
      .analyze();
    expect(axe.violations, `axe tabla ordenada ${tema}`).toEqual([]);
  }
});
