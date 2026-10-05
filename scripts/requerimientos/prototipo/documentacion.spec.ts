import path from 'path';
import { pathToFileURL } from 'url';
import { test, expect } from '@playwright/test';

/**
 * Fase 7: render de docs/documentacion.html (abierto como file://, igual que con doble clic).
 * Recorre las pestañas Funcional, Usuario y Técnica y comprueba que cada diagrama Mermaid tiene su SVG,
 * que no hay "Syntax error" dentro de <main> y que la navegación lateral lista las secciones nuevas.
 * mermaid.run devuelve una Promise: sus fallos llegan como rechazos no capturados (pageerror).
 */

const ARCHIVO = path.resolve(__dirname, '../../../docs/documentacion.html');
const URL_DOCUMENTACION = pathToFileURL(ARCHIVO).href;

const PESTANAS: { tab: string; secciones: string[] }[] = [
  { tab: 'funcional', secciones: ['f-req-portal2', 'f-flujo-carga', 'f-flujo-aduana', 'f-flujo-plazos'] },
  { tab: 'usuario', secciones: ['u-idioma-teclado'] },
  { tab: 'tecnica', secciones: ['t-integraciones', 't-ui-i18n-a11y'] },
];

test('los diagramas Mermaid se renderizan en las 3 pestañas sin errores', async ({ page }) => {
  const erroresConsola: string[] = [];
  const erroresPagina: string[] = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') erroresConsola.push(msg.text());
  });
  page.on('pageerror', (err) => erroresPagina.push(err.message));

  await page.goto(URL_DOCUMENTACION);

  for (const { tab, secciones } of PESTANAS) {
    await page.locator(`.tabs button[data-tab="${tab}"]`).click();
    const panel = page.locator(`.panel[data-tab="${tab}"]`);
    await expect(page.locator(`.panel[data-tab="${tab}"].active`)).toBeVisible();

    const diagramas = panel.locator('.mermaid');
    expect(await diagramas.count(), `diagramas en ${tab}`).toBeGreaterThan(0);
    await expect
      .poll(
        () =>
          panel.evaluate((p) =>
            Array.from(p.querySelectorAll('.mermaid'))
              .filter((m) => !m.querySelector('svg'))
              .map((m) => m.closest('section')?.id ?? '(sin sección)'),
          ),
        { message: `diagramas sin svg en ${tab}`, timeout: 20_000 },
      )
      .toEqual([]);

    await expect(page.locator('main').getByText('Syntax error')).toHaveCount(0);

    for (const id of secciones) {
      await expect(page.locator(`#sidenav a[href="#${id}"]`), `enlace a #${id}`).toHaveCount(1);
    }
  }

  expect(erroresConsola.filter((t) => t.startsWith('Mermaid:'))).toEqual([]);
  expect(erroresPagina).toEqual([]);
});
