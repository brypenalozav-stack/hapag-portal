import { test, expect, Page } from '@playwright/test';
import {
  BL_BOLIVIA,
  BL_CAMBIO_GRATIS,
  BL_CREDITO,
  BL_DEM_FACTURADO,
  BL_DEM_SIN_CALCULO,
  BL_EXENTO,
  BL_FFWW,
  BL_NEXUS_CAIDO,
  BL_PRUEBA,
  LOTE_CAMBIO_ALMACEN,
  TARIFA_TRAMOS,
  simularApi,
} from '../fixtures/api-mocks';
import { sembrarSesion, sembrarSesionAdmin } from '../fixtures/session';

/**
 * Fase 1, Ola C (pruebas funcionales con el backend simulado):
 * - M4-01 / M4-02: con todos los cargos exentos según Nexus, "Aplicar reglas" completa el proceso
 *   sin pasar por el carro, con la trazabilidad a la condición de Nexus.
 * (Agregar al carro y pagar se prueban con el carro real de la Ola D en ola-d.spec.ts.)
 * - M4-03 / M8-02: el IPO no aparece para un pagador con crédito.
 * - M4-04 / M8-03: la carta de responsabilidad FFWW bloquea las acciones.
 * - NF-11: con Nexus caído no se muestran cargos como definitivos.
 * - M3-18: con factura emitida no hay calculadora; sin cálculo, la calculadora envía la vista previa.
 * - M3-16 / M5-05: demoras anticipadas de Bolivia con el CLD bloqueado y el tipo de cambio.
 * - M3-04: el cambio de almacén gratuito se completa sin cobro; el servidor fija el monto.
 * - M3-05: la solicitud masiva lleva a su avance, que se actualiza y se anuncia.
 * - M8-01 / NF-15: la edición de una tarifa envía los tramos y muestra el registro de cambios.
 * - M8-02: Mi organización muestra el crédito y la condición FFWW leídos de Nexus.
 */

const POLITE = 'div[aria-live="polite"]';

async function abrirConSesion(page: Page, ruta: string): Promise<void> {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

async function abrirComoAdmin(page: Page, ruta: string): Promise<void> {
  await simularApi(page);
  await sembrarSesionAdmin(page, 'es');
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
}

test('con todo exento según Nexus, aplicar reglas completa el proceso sin carro (M4-01, M4-02)', async ({ page }) => {
  await abrirConSesion(page, `/charges/${BL_EXENTO}`);

  const tabla = page.getByRole('table', { name: `Cargos locales del BL ${BL_EXENTO} con el resultado de las reglas` });
  const gateIn = tabla.getByRole('row').filter({ hasText: 'Gate In' });
  await expect(gateIn).toContainText('Exento');
  // Trazabilidad a la condición informada por Nexus: figura, RUT y vigencia.
  await expect(gateIn).toContainText('Exento según Nexus: Consignatario del BL Master, 76000001-1, vigente desde 01-01-2026.');
  await expect(page.getByText('No hay montos por pagar: todos los cargos aplicables están exentos.')).toBeVisible();
  await expect(page.getByRole('button', { name: /Agregar .* al carro/ })).toHaveCount(0);

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/charges/${BL_EXENTO}/apply-rules`));
  await page.getByRole('button', { name: 'Aplicar exenciones y completar' }).click();
  await envio;

  await expect(page.getByRole('heading', { name: 'Proceso completado sin pago' })).toBeFocused();
  const confirmacion = 'Todos los cargos aplicables quedaron exentos según Nexus. No se agregó nada al carro de compra.';
  await expect(page.locator(POLITE)).toHaveText(confirmacion);
  await expect(gateIn).toContainText('Aplicada el');
  await expect(page.getByRole('button', { name: 'Aplicar exenciones y completar' })).toHaveCount(0);
  // Nada se agregó al carro (M4-02: sin boleta de valor cero): sigue con los 3 ítems iniciales.
  await expect(page.getByRole('link', { name: 'Carro de compra, 3 ítems' })).toBeVisible();
});

test('el IPO no aparece para un pagador con crédito y sí para uno sin crédito (M4-03, M8-02)', async ({ page }) => {
  await abrirConSesion(page, `/charges/${BL_CREDITO}`);

  const filas = page.locator('app-charges-panel table tbody tr');
  await expect(filas).toHaveCount(1);
  await expect(filas.filter({ hasText: 'IPO' })).toHaveCount(0);
  await expect(page.getByTestId('charges-ipo-excluded')).toHaveText(
    'El recargo IPO no se cobra: la organización tiene condición de crédito en Nexus.',
  );

  await page.goto(`/charges/${BL_FFWW}`);
  await expect(page.locator('app-charges-panel table tbody tr').filter({ hasText: 'IPO' })).toHaveCount(1);
  // El IPO en USD muestra su equivalente en CLP con el tipo de cambio de Nexus (M5-05).
  await expect(page.locator('app-charges-panel table tbody tr').filter({ hasText: 'IPO' })).toContainText('tipo de cambio 950,00');
});

test('la carta de responsabilidad FFWW faltante bloquea las acciones (M4-04, M8-03)', async ({ page }) => {
  await abrirConSesion(page, `/charges/${BL_FFWW}`);

  const aviso = page.getByRole('note').filter({ hasText: 'Carta de responsabilidad obligatoria' });
  await expect(aviso).toBeVisible();
  await expect(aviso).toContainText('Estado de la carta: falta.');
  await expect(aviso.getByRole('button', { name: 'Generar carta de responsabilidad' })).toBeDisabled();

  await expect(page.getByRole('button', { name: /Agregar .* al carro/ })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /^Pagar / })).toHaveCount(0);
  await expect(page.locator('app-charges-panel table tbody').getByText('Bloqueado: falta la carta de responsabilidad')).toHaveCount(2);
});

test('con Nexus caído no se muestran cargos como definitivos (NF-11)', async ({ page }) => {
  await abrirConSesion(page, `/charges/${BL_NEXUS_CAIDO}`);

  const alerta = page.locator('app-charges-panel').getByRole('alert');
  await expect(alerta).toContainText('Nexus no está disponible en este momento');
  await expect(alerta.getByRole('button', { name: 'Reintentar' })).toBeVisible();
  await expect(page.locator('app-charges-panel table')).toHaveCount(0);
});

test('con factura de demurrage emitida no hay calculadora; sin cálculo sí (M3-18)', async ({ page }) => {
  await abrirConSesion(page, `/demurrage/${BL_DEM_FACTURADO}`);

  await expect(page.getByTestId('demurrage-state')).toHaveText('Estado: Facturado con deuda vigente');
  const facturas = page.getByRole('table', { name: 'Facturas de demurrage con deuda vigente' });
  await expect(facturas.getByRole('row').filter({ hasText: 'FAC-DEM-2026-0915' })).toContainText('CLP');
  // Ola D: la factura se paga desde el carro (botón que abre el diálogo de agregar).
  await expect(page.getByRole('button', { name: 'Pagar factura FAC-DEM-2026-0915' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Calcular demurrage' })).toHaveCount(0);
  await expect(page.locator('app-demurrage-calculator')).toHaveCount(0);
  await expect(page.getByTestId('demurrage-calculator-blocked')).toBeVisible();
  // MHD entre los conceptos de la pestaña de demurrage (M3-02).
  await expect(page.getByRole('table', { name: 'Conceptos de la pestaña de demurrage' })).toContainText('MHD');

  // Sin cálculo: datos para calcular y calculadora con vista previa (save=false).
  await page.goto(`/demurrage/${BL_DEM_SIN_CALCULO}`);
  await expect(page.getByTestId('demurrage-state')).toHaveText('Estado: Aún no calculado');
  await page.getByRole('button', { name: 'Calcular demurrage' }).click();
  await page.getByLabel('HLXU3034003 (40HC)').uncheck();
  const vista = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/demurrage/${BL_DEM_SIN_CALCULO}/calculate`));
  await page.getByRole('button', { name: 'Vista previa' }).click();
  expect((await vista).postDataJSON()).toEqual({ untilDate: '2026-10-05', containerNumbers: ['HLXU3034002'], save: false });
  await expect(page.getByText('Vista previa: el cálculo no se guardó.')).toBeVisible();

  // Guardar deja el BL en "calculado y no pagado", con la acción de agregar al carro.
  await page.getByRole('button', { name: 'Calcular y guardar' }).click();
  await expect(page.getByTestId('demurrage-state')).toHaveText('Estado: Calculado y no pagado');
  await expect(page.getByRole('button', { name: 'Agregar al carro' })).toBeVisible();
});

test('demoras anticipadas de Bolivia: CLD bloqueado, tipo de cambio y cargo para el carro (M3-16, M5-05)', async ({ page }) => {
  await abrirConSesion(page, `/demurrage/${BL_BOLIVIA}`);

  await expect(page.getByTestId('demurrage-cld-blocked')).toBeVisible();
  await expect(page.getByTestId('exchange-rate-note')).toContainText('Tipo de cambio USD→BOB: 6,96');
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith(`/api/v1/demurrage/${BL_BOLIVIA}/advance-demurrage`));
  await page.getByRole('button', { name: 'Generar cargo de demoras anticipadas' }).click();
  await envio;
  await expect(page.locator(POLITE)).toHaveText('Se generó el cargo de demoras anticipadas.');
  // Ola D: el cargo se agrega al carro con su RUT de facturación y moneda de pago (M5-09, M5-04).
  await page.getByRole('button', { name: 'Agregar demoras anticipadas al carro' }).click();
  const dialogo = page.getByRole('dialog', { name: 'Agregar al carro' });
  await expect(dialogo.getByLabel('Moneda de pago (obligatorio)')).toHaveValue('USD');
  await dialogo.getByRole('button', { name: 'Agregar al carro' }).click();
  await expect(page.locator(POLITE)).toHaveText('Demoras anticipadas se agregó al carro en USD.');
});

test('el cambio de almacén gratuito se completa sin cobro (M3-04)', async ({ page }) => {
  await abrirConSesion(page, `/warehouse?bl=${BL_CAMBIO_GRATIS}`);

  await expect(page.getByTestId('warehouse-free-entitlement')).toContainText('Tiene derecho a un cambio de almacén gratuito');
  await page.getByLabel('Almacén de destino', { exact: true }).fill('Bodega Central');

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/warehouse-changes/requests'));
  await page.getByRole('button', { name: 'Solicitar cambio gratuito' }).click();
  const cuerpo = (await envio).postDataJSON();
  expect(cuerpo).toEqual({ blNumber: BL_CAMBIO_GRATIS, containerNumber: null, fromWarehouse: null, toWarehouse: 'Bodega Central', tariffCode: null });
  // El monto lo determina el servidor: el cliente no lo envía.
  expect(cuerpo.amount).toBeUndefined();

  await expect(page.getByRole('heading', { name: 'Cambio de almacén completado' })).toBeFocused();
  await expect(page.getByRole('status').filter({ hasText: 'sin cobro y sin intervención de Customer Service' })).toBeVisible();
  await expect(page.locator(POLITE)).toHaveText(`Cambio de almacén del BL ${BL_CAMBIO_GRATIS} completado sin cobro.`);
});

test('sin cambio gratuito se ofrecen las tarifas KTE/KTF y el destino es obligatorio (M3-04, M8-01)', async ({ page }) => {
  await abrirConSesion(page, '/warehouse');

  await page.getByLabel('Número de BL').fill(BL_PRUEBA.blNumber);
  await page.getByRole('button', { name: 'Consultar condiciones' }).click();
  await expect(page.getByRole('radio', { name: /^KTE/ })).toBeChecked();
  await page.getByRole('radio', { name: /^KTF/ }).check();

  await page.getByRole('button', { name: 'Solicitar cambio de almacén' }).click();
  const resumen = page.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await resumen.getByRole('link', { name: 'Indique el almacén de destino.' }).click();
  await expect(page.getByLabel('Almacén de destino', { exact: true })).toBeFocused();

  await page.getByLabel('Almacén de destino', { exact: true }).fill('Bodega Norte');
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/warehouse-changes/requests'));
  await page.getByRole('button', { name: 'Solicitar cambio de almacén' }).click();
  expect((await envio).postDataJSON()).toMatchObject({ blNumber: BL_PRUEBA.blNumber, tariffCode: 'KTF', toWarehouse: 'Bodega Norte' });
  await expect(page.getByRole('heading', { name: 'Solicitud registrada, pendiente de pago' })).toBeFocused();
});

test('la solicitud masiva lleva a su avance, que se actualiza y se anuncia (M3-05)', async ({ page }) => {
  await abrirConSesion(page, '/warehouse');

  await page.getByLabel('Almacén de destino común (opcional)').fill('Bodega 1');
  await page.getByLabel('Lista de solicitudes').fill(`BL;destino\n${BL_CAMBIO_GRATIS}\n${BL_PRUEBA.blNumber};Bodega 2;HLXU1234567\nHLCUXXX000000`);
  await expect(page.getByText('3 líneas listas para enviar')).toBeVisible();

  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/warehouse-changes/bulk'));
  await page.getByRole('button', { name: 'Enviar solicitud masiva (3 líneas)' }).click();
  expect((await envio).postDataJSON()).toEqual({
    items: [
      { blNumber: BL_CAMBIO_GRATIS, containerNumber: null, fromWarehouse: null, toWarehouse: 'Bodega 1', tariffCode: null },
      { blNumber: BL_PRUEBA.blNumber, containerNumber: 'HLXU1234567', fromWarehouse: null, toWarehouse: 'Bodega 2', tariffCode: null },
      { blNumber: 'HLCUXXX000000', containerNumber: null, fromWarehouse: null, toWarehouse: 'Bodega 1', tariffCode: null },
    ],
  });

  await expect(page).toHaveURL(new RegExp(`/warehouse/bulk/${LOTE_CAMBIO_ALMACEN}$`));
  const barra = page.getByRole('progressbar', { name: 'Avance de la solicitud masiva' });
  await expect(page.getByTestId('warehouse-progress-text')).toHaveText('1 de 3 líneas procesadas (33 %)');
  await expect(barra).toHaveAttribute('aria-valuenow', '33');
  await expect(page.locator(POLITE)).toHaveText('Avance de la solicitud masiva: 1 de 3 líneas procesadas (33 %).');

  await expect(page.getByTestId('warehouse-progress-text')).toHaveText('3 de 3 líneas procesadas (100 %)', { timeout: 10_000 });
  await expect(barra).toHaveAttribute('aria-valuenow', '100');
  await expect(page.getByRole('heading', { name: 'Estado: Terminada con errores' })).toBeVisible();
  await expect(page.locator(POLITE)).toHaveText('Solicitud masiva terminada: 2 correctas y 1 con error de 3 líneas.');
  const lineas = page.getByRole('table', { name: 'Resultado de cada línea de la solicitud masiva' });
  await expect(lineas.getByRole('row').filter({ hasText: 'HLCUXXX000000' })).toContainText('No encontramos el BL');
});

test('editar una tarifa envía los tramos y muestra el registro de cambios (M8-01, NF-15)', async ({ page }) => {
  await abrirComoAdmin(page, `/admin/tariffs/${TARIFA_TRAMOS}`);

  // Registro de cambios: usuario, fecha, valor anterior y nuevo.
  const historial = page.getByRole('table', { name: 'Registro de cambios de la tarifa' });
  const modificacion = historial.getByRole('row').filter({ hasText: 'Modificación' });
  await expect(modificacion).toContainText('tarifas@hapag-lloyd.cl');
  await expect(modificacion.getByRole('cell').nth(3)).toContainText('25 o más: USD 180,00');
  await expect(modificacion.getByRole('cell').nth(4)).toContainText('49 o más: USD 350,00');

  // Un tramo abierto que no es el último se rechaza antes de enviar.
  await page.getByLabel('Hasta, tramo 2').fill('');
  await page.getByRole('button', { name: 'Guardar cambios' }).click();
  const resumen = page.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen).toBeFocused();
  await expect(resumen.getByRole('link', { name: 'Tramo 2: solo el último tramo puede quedar abierto.' })).toBeVisible();
  await page.getByLabel('Hasta, tramo 2').fill('48');

  // Cierra el último tramo y agrega uno nuevo abierto.
  await page.getByLabel('Hasta, tramo 3').fill('72');
  await page.getByLabel('Valor, tramo 3').fill('400');
  await page.getByRole('button', { name: 'Agregar tramo' }).click();
  await expect(page.getByLabel('Desde, tramo 4')).toHaveValue('73');
  await expect(page.getByLabel('Desde, tramo 4')).toBeFocused();
  await page.getByLabel('Valor, tramo 4').fill('550,5');

  const envio = page.waitForRequest((r) => r.method() === 'PUT' && r.url().endsWith(`/api/v1/tariffs/${TARIFA_TRAMOS}`));
  const historia = page.waitForRequest((r) => r.method() === 'GET' && r.url().endsWith(`/api/v1/tariffs/${TARIFA_TRAMOS}/history`));
  await page.getByRole('button', { name: 'Guardar cambios' }).click();
  const cuerpo = (await envio).postDataJSON();
  expect(cuerpo).toMatchObject({ conceptCode: 'LATE_ARRIVAL', country: 'CL', currency: 'USD', tierUnit: 'Hours', tierMode: 'Flat', validFrom: '2026-10-01', validTo: null });
  expect(cuerpo.tiers).toEqual([
    { fromUnit: 0, toUnit: 24, amount: 100 },
    { fromUnit: 25, toUnit: 48, amount: 200 },
    { fromUnit: 49, toUnit: 72, amount: 400 },
    { fromUnit: 73, toUnit: null, amount: 550.5 },
  ]);
  await historia;
  await expect(page.getByRole('status').filter({ hasText: 'Tarifa actualizada. El cambio quedó en el registro.' })).toBeVisible();
});

test('el listado de tarifas filtra por concepto y lleva al editor (M8-01)', async ({ page }) => {
  await abrirComoAdmin(page, '/admin/tariffs');

  const consulta = page.waitForRequest((r) => r.url().includes('/api/v1/tariffs?') && r.url().includes('concept=LATE_ARRIVAL'));
  await page.getByLabel('Concepto').selectOption('LATE_ARRIVAL');
  await page.getByRole('button', { name: 'Buscar' }).click();
  await consulta;

  const fila = page.getByRole('row').filter({ hasText: 'Llegada tardía' });
  await expect(fila).toContainText('Tramos: 3 (Horas, USD)');
  await fila.getByRole('link', { name: /^Editar la tarifa Llegada tardía/ }).click();
  await expect(page).toHaveURL(new RegExp(`/admin/tariffs/${TARIFA_TRAMOS}$`));
});

test('reglas internas: alta con RUT o Match Code obligatorio e historial (M3-04, M3-16, NF-15)', async ({ page }) => {
  await abrirComoAdmin(page, '/admin/internal-charge-rules');

  await page.getByRole('button', { name: 'Nueva regla' }).click();
  await expect(page.getByRole('heading', { name: 'Nueva regla interna' })).toBeFocused();
  await page.getByRole('button', { name: 'Crear regla' }).click();
  const resumen = page.getByRole('alert').filter({ hasText: 'Revise los siguientes campos' });
  await expect(resumen.getByRole('link', { name: 'Indique el RUT/NIT o el Match Code de la cuenta.' })).toBeVisible();

  await page.getByLabel('Tipo de regla').selectOption('AdvanceDemurrageRequired');
  await page.locator('#rule-country').selectOption('BO');
  await page.getByLabel('Match Code').fill('BOANDE02');
  await page.getByLabel('Vigente desde').fill('2026-11-01');
  const envio = page.waitForRequest((r) => r.method() === 'POST' && r.url().endsWith('/api/v1/internal-charge-rules'));
  await page.getByRole('button', { name: 'Crear regla' }).click();
  expect((await envio).postDataJSON()).toEqual({
    ruleType: 'AdvanceDemurrageRequired',
    country: 'BO',
    taxId: null,
    matchCode: 'BOANDE02',
    accountName: null,
    reason: null,
    maxUsesPerBl: null,
    validFrom: '2026-11-01',
    validTo: null,
  });
  await expect(page.locator(POLITE)).toHaveText('Regla creada.');

  await page.getByRole('button', { name: /^Ver el historial de la regla de Importadora Andes SpA$/ }).click();
  const historial = page.getByRole('table', { name: 'Registro de cambios de la regla' });
  await expect(historial.getByRole('row').filter({ hasText: 'Creación' })).toContainText('admin@hapag-lloyd.cl');
});

test('Mi organización muestra el crédito y la condición FFWW leídos de Nexus (M8-02, M8-03)', async ({ page }) => {
  await simularApi(page, { credito: true });
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/organization');
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);

  const seccion = page.getByRole('region', { name: 'Condiciones comerciales (Nexus)' });
  await expect(seccion.getByTestId('commercial-conditions-credit')).toHaveText('Con crédito a 30 días');
  await expect(seccion.getByTestId('commercial-conditions-ffww')).toHaveText('No');
  await expect(seccion).toContainText('Excluido por condición de crédito');
});
