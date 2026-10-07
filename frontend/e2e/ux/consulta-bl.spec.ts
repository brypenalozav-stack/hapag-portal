import { Page, Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/**
 * Consulta BL (M2-09, CL-IMP-13, BO-IMP-13): paso a paso de los requisitos de liberación por país y, con todo
 * cumplido, el TATC (solicitud automática una vez y comprobante).
 */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const BL = 'HLCUVAL250100123';
const AHORA = '2026-10-06T12:00:00Z';

type Paso = { code: string; status: string; reason?: string | null; action?: string; actionAllowed?: boolean; pendingAmounts?: { currency: string; total: number }[]; items?: unknown[] };

function paso(p: Paso) {
  return { reason: null, action: 'None', actionAllowed: false, pendingAmounts: [], items: [], ...p };
}

function estado(opciones: {
  country?: string;
  steps: Paso[];
  released: boolean;
  tatc?: Partial<Record<string, unknown>>;
  containers?: unknown[];
  notices?: string[];
}) {
  const steps = opciones.steps.map(paso);
  return {
    blId: '00000000-0000-4000-8000-000000000001',
    blNumber: BL,
    bookingNumber: 'HLCUBKG2501001',
    country: opciones.country ?? 'CL',
    operation: 'IMPORT',
    status: 'Arrived',
    vessel: 'Hamburg Express',
    voyage: '025E',
    portOfLoading: 'Shanghai',
    portOfDischarge: 'San Antonio',
    portOfDischargeCode: 'CLSAI',
    finalDestinationCode: null,
    eta: '2026-10-05T00:00:00Z',
    consignee: 'Importadora Demo SpA',
    timeZone: 'America/Santiago',
    applicable: true,
    steps,
    completedSteps: steps.filter((s) => s.status === 'Done' || s.status === 'NotRequired').length,
    totalSteps: steps.length,
    released: opciones.released,
    containers: opciones.containers ?? [
      { containerNumber: 'HLXU1234567', containerType: '40HC', isShipperOwned: false, demurrageStatus: 'Pending', demurrageAmount: 225000, demurrageCurrency: 'CLP', tatcNumber: null, tatcStatus: 'NotIssued', tatcIssuedAt: null, warehouseCode: null, tatcPendingReasons: ['PAYMENT_PENDING'] },
      { containerNumber: 'HLXU7654321', containerType: '20DV', isShipperOwned: true, demurrageStatus: null, demurrageAmount: null, demurrageCurrency: null, tatcNumber: null, tatcStatus: null, tatcIssuedAt: null, warehouseCode: null, tatcPendingReasons: [] },
    ],
    tatc: {
      unlocked: opciones.released, available: true, status: 'NotIssued', errorCode: null, sourceUpdatedAt: AHORA, retrievedAt: AHORA,
      windowHours: 72, availableFrom: '2026-10-02T00:00:00Z', windowOpen: true, canRequest: false, lastRequestedAt: null,
      lastRequestStatus: null, lastRequestReason: null, ...opciones.tatc,
    },
    notices: opciones.notices ?? ['TATC_WINDOW_72H', 'SOW_NO_TATC'],
    evaluatedAt: AHORA,
  };
}

async function abrir(page: Page, respuestas: unknown[]): Promise<Request[]> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  const solicitudes: Request[] = [];
  let llamada = 0;
  await page.route(`**/api/v1/shipments/${BL}/release-status`, (route) =>
    route.fulfill({ json: respuestas[Math.min(llamada++, respuestas.length - 1)] }),
  );
  await page.route(`**/api/v1/shipments/${BL}/release-status/tatc`, (route) => {
    solicitudes.push(route.request());
    return route.fulfill({
      json: { id: 'b1', country: 'CL', locationCode: 'CLSAI', status: 'Completed', sourceRequestId: 'TATC-REQ-1', errorCode: null, totalItems: 1, acceptedItems: 1, rejectedItems: 0, requestedBy: 'demo', createdAt: AHORA, completedAt: AHORA, items: null },
    });
  });
  return solicitudes;
}

const CHILE_PENDIENTE: Paso[] = [
  { code: 'FREIGHT', status: 'Done' },
  { code: 'LOCAL_CHARGES', status: 'Pending', action: 'PayCharges', actionAllowed: true, pendingAmounts: [{ currency: 'CLP', total: 95000 }],
    items: [{ code: 'GATE_IN', label: 'Gate In', reference: null, status: 'Pending', satisfied: false, amount: 95000, currency: 'CLP' }] },
  { code: 'RESPONSIBILITY_LETTER', status: 'NotRequired', reason: 'NOT_FFWW', action: 'IssueResponsibilityLetter', actionAllowed: true },
  { code: 'DEMURRAGE', status: 'Pending', reason: 'CALCULATED_UNPAID', action: 'PayDemurrage', actionAllowed: true, pendingAmounts: [{ currency: 'CLP', total: 225000 }] },
];

test('Chile: el camino muestra cada requisito con su estado y su acción, y el TATC queda bloqueado', async ({ page }) => {
  await abrir(page, [estado({ steps: CHILE_PENDIENTE, released: false })]);
  await page.goto('/bl-status');

  // Antes de consultar se explica qué se revisa en cada país.
  await expect(page.getByRole('heading', { name: '¿Qué revisamos para liberar su carga?' })).toBeVisible();

  await page.getByLabel('Número de BL').fill(BL.toLowerCase());
  await page.getByTestId('release-status-submit').click();
  await expect(page).toHaveURL(new RegExp(`/bl-status/${BL}$`));

  await expect(page.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '50');
  await expect(page.getByTestId('release-status-overall')).toHaveText(/Liberación pendiente/);
  await expect(page.getByTestId('release-status-next')).toContainText('Gate In, EDS y recargos del BL');

  const recargos = page.getByTestId('release-step-LOCAL_CHARGES');
  await expect(recargos.getByTestId('release-step-status')).toHaveText('Pendiente');
  await expect(recargos.getByRole('link', { name: /Pagar los recargos/ })).toHaveAttribute('href', `/charges/${BL}`);
  await recargos.getByText('Ver detalle (1)').click();
  await expect(recargos.getByText('Gate In', { exact: true })).toBeVisible();

  await expect(page.getByTestId('release-step-RESPONSIBILITY_LETTER').getByTestId('release-step-status')).toHaveText('No aplica');
  await expect(page.getByTestId('release-step-DEMURRAGE').getByRole('link', { name: /Pagar el demurrage/ })).toHaveAttribute('href', `/demurrage/${BL}`);

  const tatc = page.getByTestId('release-step-TATC');
  await expect(tatc.getByTestId('release-tatc-status')).toHaveText('Bloqueado');
  await expect(tatc).toContainText('Faltan 2');

  // Avisos y detalle por contenedor (SOW sin TATC).
  await expect(page.getByTestId('release-notice-TATC_WINDOW_72H')).toContainText('72 horas');
  await expect(page.getByTestId('release-notice-SOW_NO_TATC')).toBeVisible();
  await expect(page.getByTestId('release-container-HLXU7654321')).toContainText('Sin TATC (SOW)');

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema });
    const axe = await new AxeBuilder({ page }).include('#contenido-principal').withTags(TAGS).analyze();
    expect(axe.violations, `axe consulta BL ${tema}`).toEqual([]);
  }
});

test('Chile: con todo cumplido el portal solicita el TATC una vez y lo informa', async ({ page }) => {
  const listo = estado({
    steps: CHILE_PENDIENTE.map((s) => ({ ...s, status: s.status === 'Pending' ? 'Done' : s.status, pendingAmounts: [] })),
    released: true,
    tatc: { canRequest: true },
  });
  const solicitado = estado({
    steps: CHILE_PENDIENTE.map((s) => ({ ...s, status: s.status === 'Pending' ? 'Done' : s.status, pendingAmounts: [] })),
    released: true,
    tatc: { canRequest: false, lastRequestedAt: AHORA, lastRequestStatus: 'Accepted' },
    notices: ['TATC_WINDOW_72H', 'TATC_REQUESTED'],
  });
  const solicitudes = await abrir(page, [listo, solicitado]);
  await page.goto(`/bl-status/${BL}`);

  await expect(page.getByTestId('release-status-overall')).toHaveText(/Lista para liberar/);
  await expect(page.getByTestId('release-tatc-requested')).toBeVisible();
  await expect(page.getByTestId('toast')).toContainText(`solicitamos el TATC del BL ${BL}`);
  expect(solicitudes).toHaveLength(1);
});

test('TATC emitido: se descarga el comprobante del BL y de cada contenedor', async ({ page }) => {
  const emitido = estado({
    steps: CHILE_PENDIENTE.map((s) => ({ ...s, status: s.status === 'Pending' ? 'Done' : s.status, pendingAmounts: [] })),
    released: true,
    tatc: { status: 'Issued' },
    containers: [
      { containerNumber: 'HLXU1234567', containerType: '40HC', isShipperOwned: false, demurrageStatus: 'Paid', demurrageAmount: 225000, demurrageCurrency: 'CLP', tatcNumber: 'TATC-SAI-2026-004512', tatcStatus: 'Issued', tatcIssuedAt: AHORA, warehouseCode: 'ALM-SAI-01', tatcPendingReasons: [] },
    ],
    notices: ['TATC_WINDOW_72H'],
  });
  await abrir(page, [emitido]);
  const descargas: string[] = [];
  await page.route(`**/api/v1/shipments/${BL}/tatc/voucher**`, (route) => {
    descargas.push(route.request().url());
    return route.fulfill({ body: '%PDF-1.4', contentType: 'application/pdf' });
  });
  await page.goto(`/bl-status/${BL}`);

  await expect(page.getByTestId('release-step-TATC')).toContainText('TATC-SAI-2026-004512');
  await page.getByTestId('release-tatc-voucher-all').click();
  await page.getByRole('button', { name: 'Descargar el comprobante de TATC del contenedor HLXU1234567' }).click();
  await expect.poll(() => descargas.length).toBe(2);
  expect(descargas[1]).toContain('container=HLXU1234567');
});

test('Bolivia: suma demoras anticipadas, certificado de libre deuda y carta de liberación', async ({ page }) => {
  const bolivia = estado({
    country: 'BO',
    released: false,
    steps: [
      { code: 'FREIGHT', status: 'Done' },
      { code: 'LOCAL_CHARGES', status: 'Done' },
      { code: 'RESPONSIBILITY_LETTER', status: 'Done' },
      { code: 'DEMURRAGE', status: 'Done', reason: 'NO_DEMURRAGE' },
      { code: 'ADVANCE_DEMURRAGE', status: 'Done' },
      { code: 'NO_DEBT_CERTIFICATE', status: 'Pending', action: 'RequestNoDebtCertificate', actionAllowed: true },
      { code: 'RELEASE_LETTER', status: 'InProgress', reason: 'AWAITING_APPROVAL',
        items: [{ code: 'REQUEST', label: 'SRV-20261005-0001', reference: 'SRV-20261005-0001', status: 'PendingApproval', satisfied: false, amount: null, currency: null }] },
    ],
  });
  await abrir(page, [bolivia]);
  await page.goto(`/bl-status/${BL}`);

  await expect(page.getByText(/Para Bolivia, además del flete/)).toBeVisible();
  await expect(page.getByTestId('release-step-NO_DEBT_CERTIFICATE').getByRole('link', { name: /Solicitar el certificado/ }))
    .toHaveAttribute('href', `/shipments/${BL}/documents`);
  const carta = page.getByTestId('release-step-RELEASE_LETTER');
  await expect(carta.getByTestId('release-step-status')).toHaveText('En curso');
  await expect(carta).toContainText('en revisión por Customer Service');
  await expect(page.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '71');
});

test('BL inexistente o sin acceso: mensaje claro sin detalle', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.route('**/api/v1/shipments/HLCU0000NOEXISTE/release-status', (route) =>
    route.fulfill({ status: 404, json: { title: 'BillOfLading.NotFound' } }),
  );
  await page.goto('/bl-status/HLCU0000NOEXISTE');
  await expect(page.getByTestId('release-status-not-found')).toContainText('No encontramos ese BL');
});
