import path from 'path';
import { pathToFileURL } from 'url';
import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Fase 4: accesibilidad del prototipo navegable (docs/prototipo/portal-2.0-prototipo.html).
 * 4 pantallas × ES/EN × claro/oscuro con axe (WCAG 2.0/2.1/2.2 A y AA) más pruebas de teclado,
 * idioma en caliente, tema persistente, flujo de pago, estado de error NF-11 y reflow a 320 px.
 */

const ARCHIVO = path.resolve(__dirname, '../../../docs/prototipo/portal-2.0-prototipo.html');
const URL_PROTOTIPO = pathToFileURL(ARCHIVO).href;
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

type Idioma = 'es' | 'en';
type Tema = 'light' | 'dark';

const PANTALLAS = [
  { id: 'dashboard', hash: '#/dashboard' },
  { id: 'embarques', hash: '#/embarques' },
  // HLCU0000003 abre con el servicio de tracking caído: cubre el estado de error NF-11 con Reintentar.
  { id: 'detalle-bl', hash: '#/bl/HLCU0000003' },
  { id: 'carro', hash: '#/carro' },
];

async function abrir(page: Page, hash: string, idioma: Idioma = 'es', tema: Tema = 'light') {
  await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
  await page.goto(URL_PROTOTIPO + hash);
  const html = page.locator('html');
  await expect(html).toHaveAttribute('data-hl-ready', '1');
  if (idioma === 'en') {
    await page.getByRole('button', { name: 'English (EN)' }).click();
  }
  await expect(html).toHaveAttribute('lang', idioma);
  await expect(html).toHaveAttribute('data-bs-theme', tema);
  await page.evaluate(() => document.fonts.ready);
}

async function sinViolaciones(page: Page) {
  const resultado = await new AxeBuilder({ page }).withTags(TAGS).analyze();
  const resumen = resultado.violations.map(
    (v) => `${v.id} (${v.impact}): ${v.help}\n    ${v.nodes.map((n) => n.target.join(' ')).join('\n    ')}`,
  );
  expect(resumen, resumen.join('\n')).toEqual([]);
}

for (const idioma of ['es', 'en'] as Idioma[]) {
  for (const tema of ['light', 'dark'] as Tema[]) {
    for (const p of PANTALLAS) {
      test(`axe: ${p.id} · ${idioma} · ${tema}`, async ({ page }) => {
        await abrir(page, p.hash, idioma, tema);
        await expect(page.locator(`[data-screen="${p.id}"]`)).toBeVisible();
        await expect(page.locator('h1:visible')).toHaveCount(1);
        await sinViolaciones(page);
      });
    }
  }
}

test('el primer Tab enfoca el skip-link y este lleva al contenido principal', async ({ page }) => {
  await abrir(page, '#/dashboard');
  await page.keyboard.press('Tab');
  const skip = page.locator('.hl-skip-link');
  await expect(skip).toBeFocused();
  await expect(skip).toBeInViewport();
  await page.keyboard.press('Enter');
  await expect(page.locator('#contenido-principal')).toBeFocused();
  await expect(page.locator('[data-screen="dashboard"]')).toBeVisible();
});

test('el cambio de idioma no recarga la página, actualiza lang y persiste en hl_lang', async ({ page }) => {
  await abrir(page, '#/embarques');
  await page.evaluate(() => ((window as unknown as { __hlMarcador: string }).__hlMarcador = 'sin-recarga'));
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Embarques');

  await page.getByRole('button', { name: 'English (EN)' }).click();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Shipments');
  await expect(page.getByRole('button', { name: 'English (EN)' })).toHaveAttribute('aria-pressed', 'true');
  await expect(page).toHaveTitle(/prototype/);
  expect(await page.evaluate(() => (window as unknown as { __hlMarcador?: string }).__hlMarcador)).toBe('sin-recarga');
  expect(await page.evaluate(() => localStorage.getItem('hl_lang'))).toBe('en');

  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Shipments');
});

test('el tema sigue prefers-color-scheme y el selector persiste en hl_theme', async ({ page }) => {
  await abrir(page, '#/dashboard', 'es', 'dark');
  await page.getByLabel('Tema').selectOption('light');
  await expect(page.locator('html')).toHaveAttribute('data-bs-theme', 'light');
  expect(await page.evaluate(() => localStorage.getItem('hl_theme'))).toBe('light');
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('data-bs-theme', 'light');

  await page.getByLabel('Tema').selectOption('auto');
  await expect(page.locator('html')).toHaveAttribute('data-bs-theme', 'dark');
  await page.emulateMedia({ colorScheme: 'light' });
  await expect(page.locator('html')).toHaveAttribute('data-bs-theme', 'light');
});

test('carro: las pestañas de moneda se recorren con flechas', async ({ page }) => {
  await abrir(page, '#/carro');
  const clp = page.getByRole('tab', { name: /^CLP/ });
  await expect(clp).toHaveAttribute('aria-selected', 'true');
  await clp.focus();
  await page.keyboard.press('ArrowRight');
  const usd = page.getByRole('tab', { name: /^USD/ });
  await expect(usd).toBeFocused();
  await expect(usd).toHaveAttribute('aria-selected', 'true');
  await page.keyboard.press('End');
  const eur = page.getByRole('tab', { name: /^EUR/ });
  await expect(eur).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('#panel-EUR [role="status"]')).toContainText('No hay datos');
  await sinViolaciones(page);
});

for (const tema of ['light', 'dark'] as Tema[]) {
  test(`carro: el pago se confirma y se anuncia por aria-live · ${tema}`, async ({ page }) => {
    await abrir(page, '#/carro', 'es', tema);
    await page.locator('#panel-CLP').getByRole('button', { name: /^Pagar CLP/ }).click();
    await expect(page.getByRole('heading', { name: 'Confirma el pago' })).toBeFocused();
    await sinViolaciones(page);

    await page.getByLabel('Medio de pago').selectOption('khipu');
    await page.getByRole('button', { name: 'Confirmar pago' }).click();
    const vivo = page.locator('#hl-live-polite');
    await expect(vivo).toHaveAttribute('aria-live', 'polite');
    await expect(vivo).toContainText('Pago confirmado: CLP 1.743.500 con Khipu');
    await expect(page.getByRole('tab', { name: /^CLP/ })).toBeFocused();
    await expect(page.locator('#panel-CLP [role="status"]')).toContainText('No hay datos');
    await sinViolaciones(page);
  });
}

test('detalle de BL: el error NF-11 se recupera con Reintentar', async ({ page }) => {
  await abrir(page, '#/bl/HLCU0000003');
  const alerta = page.getByRole('alert');
  await expect(alerta).toContainText('Servicio temporalmente no disponible');
  await alerta.getByRole('button', { name: 'Reintentar' }).click();
  await expect(page.getByRole('table', { name: 'Eventos de tracking del BL HLCU0000003' })).toBeVisible();
  await expect(page.locator('#hl-live-polite')).toContainText('Eventos de tracking cargados');
  await sinViolaciones(page);
});

test('embarques: búsqueda sin resultados muestra el estado vacío NF-11', async ({ page }) => {
  await abrir(page, '#/embarques', 'en');
  await page.getByLabel('BL number').fill('HLCU9999999');
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'No data' })).toBeVisible();
  await expect(page.locator('#hl-live-polite')).toContainText('0 shipments found');
  await sinViolaciones(page);
});

test('formatos Q8: fechas y montos según idioma', async ({ page }) => {
  await abrir(page, '#/carro');
  await expect(page.locator('#panel-CLP tfoot')).toContainText('CLP 1.743.500');
  await page.getByRole('button', { name: 'English (EN)' }).click();
  await expect(page.locator('#panel-CLP tfoot')).toContainText('CLP 1,743,500');
  await page.goto(URL_PROTOTIPO + '#/bl/HLCU0000001');
  await expect(page.locator('dd time').first()).toHaveText('14 Nov 2026');
  await page.getByRole('button', { name: 'Español (ES)' }).click();
  await expect(page.locator('dd time').first()).toHaveText('14-11-2026');
});

for (const p of PANTALLAS) {
  test(`reflow a 320 px sin desplazamiento horizontal: ${p.id}`, async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await abrir(page, p.hash);
    const desborde = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(desborde).toBeLessThanOrEqual(0);
  });
}
