import { readFileSync } from 'fs';
import path from 'path';
import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {
  BL_ACCESO_ABIERTO,
  BL_BOLIVIA,
  BL_CAMBIO_GRATIS,
  BL_DEM_CALCULADO,
  BL_DEM_FACTURADO,
  BL_DEM_SIN_CALCULO,
  BL_DEM_SIN_DEUDA,
  BL_EXENTO,
  BL_EXPORTACION,
  BL_FFWW,
  BL_NEXUS_CAIDO,
  BL_PRUEBA,
  BL_SIN_FLETE,
  LOTE_CAMBIO_ALMACEN,
  ORGANIZACION_EN_REVISION,
  TARIFA_TRAMOS,
  simularApi,
} from '../fixtures/api-mocks';
import { IDIOMAS, Idioma, sembrarIdioma, sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Fase 5a: axe (WCAG 2.0/2.1/2.2 A y AA) sobre las pantallas principales.
 * Fase 5b: cada pantalla se recorre en español y en inglés (hl_lang) y se comprueba html[lang].
 * axe-baseline.json lista, por pantalla, las reglas que fallan hoy: la prueba falla solo si
 * aparece una regla nueva, en cualquiera de los dos idiomas. La Fase 5c corrige las pantallas
 * y deja la línea base en {}.
 * Fase 1, Ola A: registro y solicitud de vinculación, embarques (M2-06, M2-07), Mi organización
 * (M1-02, M1-07, M1-08) y, con sesión de administrador interno, organizaciones (M8-04) y matriz de
 * accesos (M1-11). La consulta de BL (/bills-of-lading) redirige a /shipments.
 * Fase 1, Ola B: vista única de accesos y permisos de Mi organización (M1-24) con sus pestañas
 * (accesos, terceros por defecto, acceso abierto, acceso por booking y auditoría), el diálogo de
 * otorgamiento abierto, el detalle con la sección "Accesos" y el BL visto por acceso abierto
 * (M1-17, M1-18) y la bandeja de notificaciones. `preparar` lleva la pantalla al estado a revisar.
 * Fase 1, Ola C: cargos con reglas de Nexus (exentos, aplicados, carta FFWW que bloquea, Nexus
 * caído), demurrage en cada estado de M3-18 y las demoras anticipadas de Bolivia, cotización del
 * cambio de almacén (tarifas y gratuito), avance de la solicitud masiva y, con sesión interna, los
 * mantenedores de tarifas (listado, editor con tramos e historial) y de reglas internas.
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

const LINEA_BASE: Record<string, string[]> = JSON.parse(
  readFileSync(path.join(__dirname, 'axe-baseline.json'), 'utf-8'),
);

type Sesion = 'ninguna' | 'cliente' | 'admin';

/** Consulta la cotización del cambio de almacén de un BL (M3-04). */
function cotizarCambio(bl: string): (page: Page) => Promise<void> {
  return async (page) => {
    await page.locator('#warehouse-bl').fill(bl);
    await page.locator('form:has(#warehouse-bl) button[type="submit"]').click();
    await expect(page.locator('#warehouse-to')).toBeVisible();
  };
}

/** Abre una pestaña de la vista única de accesos (M1-24) por su id. */
function pestanaAccesos(id: string): (page: Page) => Promise<void> {
  return async (page) => {
    const pestana = page.locator(`#access-tab-${id}`);
    await pestana.click();
    await expect(pestana).toHaveAttribute('aria-selected', 'true');
    await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  };
}

const PANTALLAS: { id: string; ruta: string; sesion: Sesion; preparar?: (page: Page) => Promise<void> }[] = [
  { id: 'login', ruta: '/login', sesion: 'ninguna' },
  { id: 'register', ruta: '/register', sesion: 'ninguna' },
  { id: 'register-join', ruta: '/register/join', sesion: 'ninguna' },
  { id: 'dashboard', ruta: '/dashboard', sesion: 'cliente' },
  { id: 'shipments', ruta: '/shipments', sesion: 'cliente' },
  { id: 'shipment-detail', ruta: `/shipments/${BL_PRUEBA.blNumber}`, sesion: 'cliente' },
  { id: 'shipment-detail-export', ruta: `/shipments/${BL_EXPORTACION}`, sesion: 'cliente' },
  { id: 'shipment-detail-no-freight', ruta: `/shipments/${BL_SIN_FLETE}`, sesion: 'cliente' },
  { id: 'organization', ruta: '/organization', sesion: 'cliente' },
  { id: 'payment-list', ruta: '/payments', sesion: 'cliente' },
  { id: 'payment-form', ruta: `/payments/new/${BL_PRUEBA.id}`, sesion: 'cliente' },
  { id: 'admin-organizations', ruta: '/admin/organizations', sesion: 'admin' },
  { id: 'admin-organization-review', ruta: `/admin/organizations/${ORGANIZACION_EN_REVISION}`, sesion: 'admin' },
  { id: 'admin-access-matrix', ruta: '/admin/access-matrix', sesion: 'admin' },
  // Ola B
  {
    id: 'organization-grant-dialog',
    ruta: '/organization',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('app-access-grants').getByRole('button', { name: /^(Otorgar acceso|Grant access)$/ }).click();
      await expect(page.getByRole('dialog')).toBeVisible();
      await expect(page.locator('app-loading-spinner')).toHaveCount(0);
    },
  },
  { id: 'organization-access-defaults', ruta: '/organization', sesion: 'cliente', preparar: pestanaAccesos('defaults') },
  { id: 'organization-open-access', ruta: '/organization', sesion: 'cliente', preparar: pestanaAccesos('openAccess') },
  { id: 'organization-early-booking', ruta: '/organization', sesion: 'cliente', preparar: pestanaAccesos('earlyBooking') },
  { id: 'organization-access-audit', ruta: '/organization', sesion: 'cliente', preparar: pestanaAccesos('audit') },
  { id: 'shipment-detail-open-access', ruta: `/shipments/${BL_ACCESO_ABIERTO}`, sesion: 'cliente' },
  { id: 'notifications', ruta: '/notifications', sesion: 'cliente' },
  // Ola C
  { id: 'charges-exempt', ruta: `/charges/${BL_EXENTO}`, sesion: 'cliente' },
  {
    id: 'charges-exempt-applied',
    ruta: `/charges/${BL_EXENTO}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('app-charges-panel button.btn-hl-orange').click();
      await expect(page.locator('app-charges-panel .alert-success')).toBeVisible();
    },
  },
  { id: 'charges-ffww-blocked', ruta: `/charges/${BL_FFWW}`, sesion: 'cliente' },
  { id: 'charges-nexus-down', ruta: `/charges/${BL_NEXUS_CAIDO}`, sesion: 'cliente' },
  { id: 'demurrage-invoiced', ruta: `/demurrage/${BL_DEM_FACTURADO}`, sesion: 'cliente' },
  { id: 'demurrage-calculated', ruta: `/demurrage/${BL_DEM_CALCULADO}`, sesion: 'cliente' },
  {
    id: 'demurrage-not-calculated',
    ruta: `/demurrage/${BL_DEM_SIN_CALCULO}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('app-demurrage-panel button.btn-hl-orange').click();
      await page.locator('app-demurrage-calculator button[type="button"]').click();
      await expect(page.locator('app-demurrage-calculator table')).toBeVisible();
    },
  },
  { id: 'demurrage-no-debt', ruta: `/demurrage/${BL_DEM_SIN_DEUDA}`, sesion: 'cliente' },
  {
    id: 'demurrage-bolivia-advance',
    ruta: `/demurrage/${BL_BOLIVIA}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('exchange-rate-note')).toBeVisible();
    },
  },
  { id: 'warehouse-quote', ruta: '/warehouse', sesion: 'cliente', preparar: cotizarCambio(BL_PRUEBA.blNumber) },
  { id: 'warehouse-quote-free', ruta: '/warehouse', sesion: 'cliente', preparar: cotizarCambio(BL_CAMBIO_GRATIS) },
  {
    id: 'warehouse-bulk-progress',
    ruta: `/warehouse/bulk/${LOTE_CAMBIO_ALMACEN}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('warehouse-progress-failed')).toHaveText('1', { timeout: 10_000 });
      await expect(page.getByTestId('warehouse-progress-succeeded')).toHaveText('2', { timeout: 10_000 });
    },
  },
  { id: 'admin-tariffs', ruta: '/admin/tariffs', sesion: 'admin' },
  { id: 'admin-tariff-editor', ruta: `/admin/tariffs/${TARIFA_TRAMOS}`, sesion: 'admin' },
  { id: 'admin-tariff-new', ruta: '/admin/tariffs/new', sesion: 'admin' },
  {
    id: 'admin-internal-rules',
    ruta: '/admin/internal-charge-rules',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().locator('button').nth(1).click();
      await expect(page.locator('#rule-history-title')).toBeFocused();
    },
  },
  {
    id: 'admin-internal-rules-form',
    ruta: '/admin/internal-charge-rules',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('.hl-page-header button').click();
      await page.locator('form:has(#rule-type) button[type="submit"]').click();
      await expect(page.locator('form:has(#rule-type) .alert-danger')).toBeFocused();
    },
  },
];

async function abrir(page: Page, ruta: string, sesion: Sesion, lang: Idioma): Promise<void> {
  await simularApi(page);
  if (sesion === 'cliente') {
    await sembrarSesion(page, { lang });
  } else if (sesion === 'admin') {
    await sembrarSesionAdmin(page, lang);
  } else {
    await sembrarIdioma(page, lang);
  }
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
}

for (const lang of IDIOMAS) {
  for (const p of PANTALLAS) {
    test(`axe: ${p.id} [${lang}]`, async ({ page }) => {
      await abrir(page, p.ruta, p.sesion, lang);
      await p.preparar?.(page);
      await expect(page).toHaveURL(new RegExp(`${p.ruta}$`));
      await expect(page.locator('html')).toHaveAttribute('lang', lang);

      const resultado = await new AxeBuilder({ page }).withTags(TAGS).analyze();
      const conocidas = new Set(LINEA_BASE[p.id] ?? []);
      const nuevas = resultado.violations.filter((v) => !conocidas.has(v.id));
      const resumen = nuevas.map(
        (v) =>
          `${v.id} (${v.impact}): ${v.help}\n    ${v.nodes.map((n) => n.target.join(' ')).join('\n    ')}`,
      );

      expect(resumen, `Reglas axe nuevas en ${p.id} [${lang}]:\n${resumen.join('\n')}`).toEqual([]);
    });
  }
}
