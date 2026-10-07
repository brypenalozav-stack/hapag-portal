import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { URL_TARIFAS, enlacesExternos, simularApi } from '../fixtures/api-mocks';
import { Idioma, sembrarSesion } from '../fixtures/session';

/**
 * Enlaces externos del país (GET /config/external-links):
 * - Tarifas locales: tarjetas que abren los tarifarios oficiales en una pestaña nueva, tarifario sin URL, acordeones.
 * - Devoluciones: "disponible pronto" sin URL configurada (hoy) y portal incrustado con la URL configurada.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const URL_DEVOLUCIONES = 'https://devoluciones.example.com/portal';

async function revisarAxe(page: Page, nombre: string): Promise<void> {
  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe ${nombre} ${tema}`).toEqual([]);
  }
}

/** Backend simulado con la respuesta de enlaces externos indicada (o un error HTTP). */
async function abrir(
  page: Page,
  ruta: string,
  opciones: { refunds?: string | null; sinTarifa?: string[]; estado?: number; lang?: Idioma } = {},
): Promise<string[]> {
  const consultas: string[] = [];
  await simularApi(page);
  await page.route('**/api/v1/config/external-links**', async (route) => {
    const url = new URL(route.request().url());
    consultas.push(url.search);
    if (opciones.estado) {
      await route.fulfill({ status: opciones.estado, contentType: 'application/json', body: '{}' });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(enlacesExternos(url.searchParams.get('country') ?? 'CL', opciones)),
    });
  });
  // El portal externo de devoluciones, simulado: una página mínima y válida.
  await page.route(`${URL_DEVOLUCIONES}**`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'text/html; charset=utf-8',
      body: '<!doctype html><html lang="es"><head><title>Devoluciones</title></head><body><main><h1>Portal de devoluciones</h1></main></body></html>',
    }),
  );
  await sembrarSesion(page, { lang: opciones.lang ?? 'es' });
  await page.goto(ruta);
  return consultas;
}

test.describe('Tarifas locales', () => {
  test('el menú lleva a la página y cada tarjeta abre el tarifario oficial en una pestaña nueva', async ({ page }) => {
    const consultas = await abrir(page, '/dashboard');
    const menu = page.getByRole('navigation', { name: 'Menú principal' });
    await menu.getByRole('button', { name: 'Servicios' }).click();
    await menu.getByRole('link', { name: 'Tarifas locales' }).click();
    await expect(page).toHaveURL(/\/local-tariffs$/);

    await expect(page.getByRole('heading', { level: 1, name: 'Tarifas locales' })).toBeVisible();
    await expect(page.getByTestId('local-tariffs-notice')).toContainText('Este portal no aloja las tarifas');

    const tarjetas = [
      { code: 'INLAND_CL', titulo: 'Tarifario Inland (Chile)' },
      { code: 'DEMURRAGE_DETENTION', titulo: 'Demurrage / Detention (Latinoamérica)' },
      { code: 'LOCAL_CHARGES', titulo: 'Recargos locales / Service fees' },
    ] as const;
    for (const t of tarjetas) {
      const tarjeta = page.getByTestId(`local-tariffs-card-${t.code}`);
      await expect(tarjeta.getByRole('heading', { level: 3 })).toHaveText(t.titulo);
      const enlace = tarjeta.getByRole('link');
      await expect(enlace).toHaveAttribute('href', URL_TARIFAS[t.code]);
      await expect(enlace).toHaveAttribute('target', '_blank');
      await expect(enlace).toHaveAttribute('rel', 'noopener noreferrer');
      await expect(enlace).toHaveAccessibleName(`Abrir tarifario: ${t.titulo} (se abre en una pestaña nueva)`);
      await expect(tarjeta).toContainText('Sitio: hapag-lloyd.com');
    }
    // Una sola consulta por sesión y país, con el país del usuario.
    expect(consultas).toEqual(['?country=CL']);

    await revisarAxe(page, 'tarifas locales');
  });

  test('un tarifario sin URL se muestra como no disponible, sin enlace', async ({ page }) => {
    await abrir(page, '/local-tariffs', { sinTarifa: ['LOCAL_CHARGES'] });
    const tarjeta = page.getByTestId('local-tariffs-card-LOCAL_CHARGES');
    await expect(tarjeta).toContainText('Enlace no disponible por ahora');
    await expect(tarjeta.getByRole('link')).toHaveCount(0);
    await expect(page.getByTestId('local-tariffs-card-INLAND_CL').getByRole('link')).toHaveCount(1);
  });

  test('los acordeones se abren con teclado', async ({ page }) => {
    await abrir(page, '/local-tariffs');
    const guia = page.getByTestId('local-tariffs-guide');
    const resumen = guia.locator('summary');
    await expect(resumen).toHaveText('¿Cómo elegir el tarifario correcto?');
    await expect(guia).not.toHaveAttribute('open', '');
    await resumen.focus();
    await page.keyboard.press('Enter');
    await expect(guia).toHaveAttribute('open', '');
    await expect(guia).toContainText('cargos por retención de contenedores en puertos de Latinoamérica');

    const consideraciones = page.getByTestId('local-tariffs-considerations');
    await consideraciones.locator('summary').focus();
    await page.keyboard.press('Space');
    await expect(consideraciones).toContainText('La vigencia de las tarifas la rige exclusivamente el sitio oficial de Hapag-Lloyd.');
    await revisarAxe(page, 'tarifas locales con acordeones abiertos');
  });

  test('sin respuesta del servidor informa con calma y permite reintentar', async ({ page }) => {
    await abrir(page, '/local-tariffs', { estado: 503 });
    await expect(page.getByRole('alert')).toContainText('No pudimos cargar los enlaces a los tarifarios');
    await expect(page.getByRole('button', { name: 'Reintentar' })).toBeVisible();
  });
});

test.describe('Devoluciones', () => {
  test('sin URL configurada muestra "disponible pronto" con los canales de Atención a Clientes', async ({ page }) => {
    await abrir(page, '/dashboard');
    const menu = page.getByRole('navigation', { name: 'Menú principal' });
    await menu.getByRole('button', { name: 'Pagos y facturación' }).click();
    await menu.getByRole('link', { name: 'Devoluciones' }).click();
    await expect(page).toHaveURL(/\/refunds$/);

    const pronto = page.getByTestId('refunds-coming-soon');
    await expect(pronto.getByRole('heading', { level: 2 })).toHaveText('Devoluciones estará disponible pronto');
    await expect(pronto).toContainText('contacte a Atención a Clientes');
    await expect(page.getByTestId('refunds-contact-email')).toHaveText('Chile@service.hlag.com');
    await expect(page.getByTestId('refunds-contact-phone')).toHaveText('+56 2 2618 8214');
    // Sin marco vacío ni enlaces que confundan.
    await expect(page.getByTestId('refunds-frame')).toHaveCount(0);
    await expect(page.getByTestId('refunds-open-new')).toHaveCount(0);
    await expect(pronto.getByRole('link')).toHaveCount(0);

    await revisarAxe(page, 'devoluciones pronto');
  });

  test('en inglés el estado "disponible pronto" se traduce', async ({ page }) => {
    await abrir(page, '/refunds', { lang: 'en' });
    await expect(page.getByTestId('refunds-coming-soon').getByRole('heading', { level: 2 })).toHaveText('Refunds will be available soon');
    await expect(page.getByTestId('refunds-contact')).toContainText('Customer Service');
  });

  test('con URL configurada muestra el portal incrustado y la opción de abrirlo en una ventana nueva', async ({ page }) => {
    await abrir(page, '/refunds', { refunds: URL_DEVOLUCIONES });
    await expect(page.getByTestId('refunds-coming-soon')).toHaveCount(0);

    const marco = page.getByTestId('refunds-frame');
    await expect(marco).toHaveAttribute('title', 'Portal de devoluciones de Hapag-Lloyd');
    await expect(marco).toHaveAttribute('src', URL_DEVOLUCIONES);
    await expect(marco).toHaveAttribute('referrerpolicy', 'no-referrer');
    await expect(marco).toHaveAttribute('loading', 'lazy');
    await expect(marco).toHaveAttribute('sandbox', /allow-forms allow-scripts allow-same-origin allow-popups allow-popups-to-escape-sandbox allow-downloads/);
    await expect(page.getByTestId('refunds-external-badge')).toHaveText('Externo');
    await expect(page.getByTestId('refunds-portal')).toContainText('Este módulo lo provee un sistema externo');

    const abrirAparte = page.getByTestId('refunds-open-new');
    await expect(abrirAparte).toHaveAttribute('href', URL_DEVOLUCIONES);
    await expect(abrirAparte).toHaveAttribute('target', '_blank');
    await expect(abrirAparte).toHaveAttribute('rel', 'noopener noreferrer');
    await expect(abrirAparte).toHaveAccessibleName('Abrir en ventana nueva (se abre en una ventana nueva)');

    // El aviso de carga se retira al cargar el marco; la sugerencia queda visible.
    await expect(page.frameLocator('[data-testid="refunds-frame"]').getByRole('heading', { name: 'Portal de devoluciones' })).toBeVisible();
    await expect(page.getByTestId('refunds-frame-loading')).toHaveCount(0);
    await expect(page.getByTestId('refunds-hint')).toContainText('ábralo en una ventana nueva');

    await revisarAxe(page, 'devoluciones portal');
  });
});
