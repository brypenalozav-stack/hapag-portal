import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { FASE2 } from '../fixtures/funcionalidades';
import { BL_TATC, dashboard } from '../fixtures/ola-f-mocks';
import { sembrarSesion } from '../fixtures/session';
import type { Dashboard, DashboardPayable } from '../../src/app/core/models/dashboard.model';

/**
 * Dashboard orientado a la acción (cierre de Fase 1, UX): arriba "Requiere su acción", con los pendientes agrupados
 * por BL y ordenados por urgencia, una acción por fila y "Ver todo"; vacía, "Todo al día". Tres indicadores sin
 * repetir el demurrage, como máximo 4 accesos rápidos y los indicadores operativos a pedido.
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

const URL_BASE = new URL('http://localhost/api/v1/dashboard');
const CARTA_RECHAZADA = 'SRV-20261004-5E1A0010';
const BL_CARTA = 'HLCULPB260900010';

type Ajuste = (d: Dashboard) => Dashboard;

async function abrir(page: Page, ajuste: Ajuste = (d) => d, opciones: Parameters<typeof simularApi>[1] = {}): Promise<void> {
  await simularApi(page, opciones);
  await page.route(
    (url) => url.pathname.endsWith('/api/v1/dashboard'),
    (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(ajuste(dashboard(URL_BASE))) }),
  );
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/dashboard');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

/** Pendientes de pago de otros BL: con ellos la bandeja tiene más grupos que la vista previa. */
function conMasBl(d: Dashboard): Dashboard {
  const extra: DashboardPayable[] = Array.from({ length: 5 }, (_, i) => ({
    itemType: 'LocalCharge', sourceId: `e1000000-0000-4000-8000-00000000000${i}`, blNumber: `HLCUSAI26090${i}010`, bookingNumber: null,
    country: 'CL', conceptCode: 'GATE_IN', description: 'Gate In', totalAmount: 59500, currency: 'CLP', status: 'Pending', inCart: false,
    target: { kind: 'Charges', blNumber: `HLCUSAI26090${i}010` },
  }));
  return { ...d, pendingPayments: { ...d.pendingPayments, count: d.pendingPayments.count + 5, items: [...d.pendingPayments.items, ...extra] } };
}

/** Sin nada pendiente: ni cargos, ni demurrage, ni solicitudes que esperen al cliente. */
function alDia(d: Dashboard): Dashboard {
  return {
    ...d,
    pendingPayments: { count: 0, totals: [], items: [], truncated: false },
    indicators: { ...d.indicators, demurrageAtRisk: { count: 0, items: [] } },
  };
}

/** Una carta de liberación rechazada: hay que subir una nueva. */
function conCartaRechazada(d: Dashboard): Dashboard {
  return {
    ...d,
    requests: {
      ...d.requests,
      items: [
        {
          kind: 'ServiceRequest', id: 's9000000-0000-4000-8000-000000000010', reference: CARTA_RECHAZADA, blNumber: BL_CARTA,
          status: 'Rejected', inProgress: false, createdAt: '2026-10-04T10:00:00Z',
          target: { kind: 'ReleaseLetter', blNumber: BL_CARTA, id: 's9000000-0000-4000-8000-000000000010' },
        },
        ...d.requests.items,
      ],
    },
  };
}

async function ordenDeFilas(page: Page): Promise<(string | null)[]> {
  return page.locator('#dashboard-actions-list > li').evaluateAll((filas) => filas.map((f) => f.getAttribute('data-testid')));
}

async function revisarAxe(page: Page, nombre: string): Promise<void> {
  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe ${nombre} ${tema}`).toEqual([]);
  }
}

test('"Requiere su acción" agrupa por BL, ordena por urgencia y ofrece una acción por fila', async ({ page }) => {
  await abrir(page, conCartaRechazada);
  const bandeja = page.getByTestId('dashboard-actions');
  await expect(bandeja.getByRole('heading', { level: 2 })).toHaveText('Requiere su acción');
  // Primero lo vencido, luego lo urgente (demurrage) y al final lo pendiente.
  expect(await ordenDeFilas(page)).toEqual([
    'dashboard-action-HL-CL-2026-003987',
    'dashboard-action-HLCUSAI260400910',
    `dashboard-action-${BL_TATC}`,
    'dashboard-action-HLCU0000001',
    `dashboard-action-${BL_CARTA}`,
  ]);
  await expect(bandeja.getByTestId('dashboard-actions-count')).toHaveText('5 pendientes');

  const factura = page.getByTestId('dashboard-action-HL-CL-2026-003987');
  await expect(factura.getByRole('heading', { level: 3 })).toHaveText('Factura HL-CL-2026-003987');
  await expect(factura).toContainText('Vencido');
  await expect(factura).toContainText('1 factura por pagar');

  // Los dos cargos del BL en una sola fila, con el BL que lleva a su estado de liberación.
  const bl = page.getByTestId('dashboard-action-HLCU0000001');
  await expect(bl).toContainText('2 cargos por pagar');
  await expect(bl).toContainText('Pendiente');
  await expect(bl.getByRole('link', { name: 'Ver el estado de liberación del BL HLCU0000001' })).toHaveAttribute('href', '/bl-status/HLCU0000001');
  await expect(bl.getByRole('button', { name: 'Agregar al carro: BL HLCU0000001' })).toBeVisible();

  // El demurrage aparece una sola vez: la línea por pagar no se repite como "en riesgo".
  const tatc = page.getByTestId(`dashboard-action-${BL_TATC}`);
  await expect(tatc).toContainText('1 línea de demurrage por pagar');
  await expect(tatc).not.toContainText('Demurrage en riesgo');
  const riesgo = page.getByTestId('dashboard-action-HLCUSAI260400910');
  await expect(riesgo).toContainText('Demurrage en riesgo: Facturado con deuda');
  await expect(riesgo).toContainText('Urgente');
  await expect(riesgo.getByRole('link', { name: 'Revisar demurrage: BL HLCUSAI260400910' })).toHaveAttribute('href', '/demurrage/HLCUSAI260400910');
  await expect(page.locator('main').getByText(/Demurrage en riesgo/)).toHaveCount(1);

  // La carta de liberación rechazada pide subir una nueva, y no se repite en "Mis gestiones".
  const carta = page.getByTestId(`dashboard-action-${BL_CARTA}`);
  await expect(carta).toContainText(`Carta de liberación ${CARTA_RECHAZADA} rechazada`);
  await expect(carta.getByRole('link', { name: `Subir nueva carta: BL ${BL_CARTA}` })).toHaveAttribute('href', `/release-letter?bl=${BL_CARTA}`);
  await expect(page.getByRole('region', { name: /Gestiones en curso/ })).not.toContainText(CARTA_RECHAZADA);

  // Tres indicadores, sin demurrage.
  const resumen = page.getByRole('region', { name: 'Resumen' });
  await expect(resumen.getByRole('listitem').filter({ has: page.locator('.stat-value') })).toHaveCount(3);
  await expect(resumen).not.toContainText('Demurrage');

  await revisarAxe(page, 'dashboard con pendientes');
});

test('"Ver todo" muestra el resto de los pendientes y "Ver menos" vuelve a la vista previa', async ({ page }) => {
  await abrir(page, conMasBl);
  const filas = page.locator('#dashboard-actions-list > li');
  await expect(filas).toHaveCount(5);
  const verTodo = page.getByRole('button', { name: 'Ver todo (9)' });
  await expect(verTodo).toHaveAttribute('aria-expanded', 'false');
  await expect(verTodo).toHaveAttribute('aria-controls', 'dashboard-actions-list');

  await verTodo.click();
  await expect(filas).toHaveCount(9);
  const verMenos = page.getByRole('button', { name: 'Ver menos' });
  await expect(verMenos).toHaveAttribute('aria-expanded', 'true');
  await expect(verMenos).toBeFocused();

  await verMenos.click();
  await expect(filas).toHaveCount(5);
});

test('sin pendientes muestra "Todo al día"', async ({ page }) => {
  await abrir(page, alDia);
  const vacio = page.getByTestId('dashboard-actions-empty');
  await expect(vacio).toContainText('Todo al día');
  await expect(vacio).toHaveAttribute('role', 'status');
  await expect(page.locator('#dashboard-actions-list')).toHaveCount(0);
  await expect(page.getByTestId('dashboard-actions-count')).toHaveCount(0);
  await revisarAxe(page, 'dashboard al día');
});

test('como máximo 4 accesos rápidos', async ({ page }) => {
  await abrir(page);
  const atajos = page.getByRole('navigation', { name: '¿Qué necesita hacer?' });
  await expect(atajos.getByRole('listitem')).toHaveCount(4);
  await expect(page.getByTestId('dispute-link-card')).toBeVisible();
});

test('con las funciones de Fase 2 encendidas siguen siendo 4 accesos rápidos', async ({ page }) => {
  await abrir(page, (d) => d, { features: FASE2 });
  const atajos = page.getByRole('navigation', { name: '¿Qué necesita hacer?' });
  await expect(atajos.getByRole('link', { name: /Solicitar un servicio/ })).toBeVisible();
  await expect(atajos.getByRole('listitem')).toHaveCount(4);
});

test('los indicadores operativos se muestran a pedido', async ({ page }) => {
  await abrir(page);
  const boton = page.getByRole('button', { name: 'Indicadores operativos' });
  await expect(boton).toHaveAttribute('aria-expanded', 'false');
  await expect(page.getByTestId('dashboard-status-chart')).toHaveCount(0);

  await boton.click();
  await expect(boton).toHaveAttribute('aria-expanded', 'true');
  await expect(page.getByTestId('dashboard-status-chart')).toBeVisible();
  await expect(page.getByRole('heading', { level: 3, name: 'Próximos arribos' })).toBeVisible();
  await revisarAxe(page, 'dashboard con indicadores');
});
