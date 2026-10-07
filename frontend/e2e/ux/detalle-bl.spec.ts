import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { BL_PRUEBA, liberacionPrueba, simularApi } from '../fixtures/api-mocks';
import { BL_TATC } from '../fixtures/ola-f-mocks';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';
import { cargarSeccionesDiferidas } from '../fixtures/detalle';

/**
 * Detalle del BL canónico (cierre de Fase 1, UX): encabezado fijo con la próxima acción de liberación, índice "Ir a"
 * por grupos (Resumen, Contenedores, Cargos y pagos, Documentos, Accesos y servicios, Interno) y grupos bajo el pliegue
 * cargados al entrar en pantalla.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const DETALLE = `/shipments/${BL_PRUEBA.blNumber}`;

test('el encabezado fijo resume el BL y la próxima acción, y se compacta al desplazar', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(DETALLE);

  const encabezado = page.getByTestId('bl-summary-header');
  await expect(encabezado.getByRole('heading', { level: 1 })).toHaveText(`Embarque ${BL_PRUEBA.blNumber}`);
  await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
  await expect(encabezado).toContainText('Valparaiso Express / 2614W');
  await expect(encabezado).toContainText('Hamburg → San Antonio');

  const accion = page.getByTestId('bl-next-action');
  await expect(accion.getByRole('heading', { name: 'Próxima acción' })).toBeVisible();
  await expect(page.getByTestId('bl-next-overall')).toHaveText('Liberación pendiente');
  await expect(page.getByTestId('bl-next-progress')).toHaveText('2 de 4 requisitos cumplidos');
  await expect(page.getByTestId('bl-next-step')).toContainText('Falta pagar el flete del BL.');
  await expect(accion.getByRole('link', { name: 'Ver estado de liberación y TATC' })).toHaveAttribute('href', `/bl-status/${BL_PRUEBA.blNumber}`);

  // El flete se paga en este mismo detalle: el botón lleva al grupo "Cargos y pagos" y enfoca su título.
  await page.getByTestId('bl-next-action-button').click();
  await expect(page.locator('#bl-group-charges')).toBeFocused();
  await expect(page.getByRole('heading', { name: 'Flete', exact: true })).toBeVisible();

  // Desplazado, el encabezado queda como barra delgada y no tapa el título del grupo.
  await expect(encabezado).toHaveClass(/is-collapsed/);
  const barra = await encabezado.boundingBox();
  const titulo = await page.locator('#bl-group-charges').boundingBox();
  expect(barra!.height).toBeLessThan(130);
  expect(titulo!.y).toBeGreaterThanOrEqual(barra!.y + barra!.height);

  // Al volver arriba (con la rueda del mouse, que también suelta el ajuste de posición del salto) se abre de nuevo.
  await page.mouse.move(400, 500);
  await page.mouse.wheel(0, -20000);
  await expect(encabezado).not.toHaveClass(/is-collapsed/);
});

test('con todos los requisitos cumplidos o sin consulta de liberación el encabezado se muestra igual', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  let respuesta: { status: number; json?: unknown } = { status: 200, json: liberacionPrueba(true) };
  await page.route(`**/api/v1/shipments/${BL_PRUEBA.blNumber}/release-status`, (r) => r.fulfill(respuesta));

  await page.goto(DETALLE);
  await expect(page.getByTestId('bl-next-overall')).toHaveText('Lista para liberar');
  await expect(page.getByTestId('bl-next-progress')).toHaveText('4 de 4 requisitos cumplidos');
  await expect(page.getByTestId('bl-next-action-button')).toHaveCount(0);

  // 403: el perfil no ve la consulta; el encabezado sigue con su enlace a la consulta completa.
  respuesta = { status: 403, json: { message: 'Forbidden' } };
  await page.reload();
  await expect(page.getByTestId('bl-summary-header').getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByTestId('bl-next-action')).toContainText('No pudimos consultar los requisitos de liberación');

  // No aplica: sin bloque de próxima acción.
  respuesta = { status: 200, json: { ...liberacionPrueba(), applicable: false } };
  await page.reload();
  await expect(page.getByTestId('bl-summary-header').getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByTestId('bl-next-action')).toHaveCount(0);
});

test('el índice agrupa las secciones, lleva al título del grupo y carga lo diferido al llegar', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  const documentos: string[] = [];
  page.on('request', (r) => {
    if (r.url().includes(`/api/v1/documents/${BL_PRUEBA.blNumber}`)) documentos.push(r.url());
  });
  await page.goto(DETALLE);

  const nav = page.getByRole('navigation', { name: 'Secciones de esta página' });
  await expect(nav.getByRole('link')).toHaveText(['Resumen', 'Contenedores', 'Cargos y pagos', 'Documentos', 'Accesos y servicios']);
  // El cliente no ve el grupo interno.
  await expect(page.getByTestId('bl-group-internal')).toHaveCount(0);

  // Carga inicial menor: los documentos no se piden hasta llegar a su grupo.
  await expect(page.getByTestId('bl-group-documents').locator('[data-defer-placeholder]')).toHaveCount(1);
  expect(documentos).toEqual([]);

  const enlace = nav.getByRole('link', { name: 'Documentos' });
  await enlace.click();
  await expect(page.locator('#bl-group-documents')).toBeFocused();
  await expect(enlace).toHaveAttribute('aria-current', 'location');
  await expect(page.getByTestId('shipment-documents')).toBeVisible();
  expect(documentos.length).toBeGreaterThan(0);

  // Los detalles secundarios del resumen están plegados y se abren con un botón con aria-expanded.
  const mas = page.getByTestId('shipment-more-toggle');
  await mas.scrollIntoViewIfNeeded();
  await expect(mas).toHaveAccessibleName('Partes y roles del embarque');
  await expect(mas).toHaveAttribute('aria-expanded', 'false');
  await expect(page.getByRole('heading', { name: 'Partes del embarque' })).toHaveCount(0);
  await mas.click();
  await expect(mas).toHaveAttribute('aria-expanded', 'true');
  await expect(page.getByRole('heading', { name: 'Partes del embarque' })).toBeVisible();
});

test('el perfil interno ve el grupo Interno con la publicación por DIFU', async ({ page }) => {
  await simularApi(page);
  await sembrarSesionAdmin(page, 'es');
  await page.goto(`/shipments/${BL_TATC}`);
  const nav = page.getByRole('navigation', { name: 'Secciones de esta página' });
  await nav.getByRole('link', { name: 'Interno' }).click();
  await expect(page.locator('#bl-group-internal')).toBeFocused();
  await expect(page.getByTestId('shipment-publication')).toBeVisible();
});

for (const tema of ['light', 'dark'] as const) {
  test(`detalle del BL sin violaciones de axe (${tema})`, async ({ page }) => {
    await page.emulateMedia({ colorScheme: tema });
    await simularApi(page);
    await sembrarSesion(page, { lang: 'es' });
    await page.goto(DETALLE);
    await expect(page.getByTestId('bl-next-progress')).toBeVisible();
    await cargarSeccionesDiferidas(page);
    await page.getByTestId('shipment-more-toggle').click();
    const arriba = await new AxeBuilder({ page }).withTags(TAGS).analyze();
    expect(arriba.violations, 'arriba').toEqual([]);

    // Compacto, tras saltar a un grupo.
    await page.getByRole('navigation', { name: 'Secciones de esta página' }).getByRole('link', { name: 'Cargos y pagos' }).click();
    await expect(page.getByTestId('bl-summary-header')).toHaveClass(/is-collapsed/);
    const compacto = await new AxeBuilder({ page }).include('[data-testid="bl-summary-header"]').withTags(TAGS).analyze();
    expect(compacto.violations, 'compacto').toEqual([]);
  });
}
