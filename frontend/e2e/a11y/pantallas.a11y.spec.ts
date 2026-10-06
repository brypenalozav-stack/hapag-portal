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
import { OpcionesOlaD, PAGO } from '../fixtures/ola-d-mocks';
import {
  BL_CARTA,
  BL_CLD_BLOQUEADO,
  BL_CLD_EMITIBLE,
  BL_DOCUMENTOS,
  BL_DOCUMENTOS_CAIDO,
  BL_SOLO_SHIPPER,
  PAGO_CON_DOCUMENTOS,
} from '../fixtures/ola-e-mocks';
import { BL_ORIGEN_CAIDO, BL_TATC } from '../fixtures/ola-f-mocks';
import {
  BL_DROP_OFF,
  BL_FORMULARIO,
  BL_HIJO,
  BOOKING_SELLOS,
  CAMBIO_PAGADO,
  SERVICIO_FORMULARIO,
  SOLICITUD,
  idDefinicion,
} from '../fixtures/ola-g-mocks';
import { ITEM } from '../fixtures/ola-d-mocks';
import { PAGO_DEPOSITO, REFACTURACION, REFACTURACION_BORRADOR, TOKEN_ACEPTACION } from '../fixtures/ola-h-mocks';
import { BL_COUNTER_FALLIDO, GUIA_CARRO, OpcionesOlaI, SESION_TERMINADA, TRANSPORTISTA, sembrarImpersonacion } from '../fixtures/ola-i-mocks';
import { IDIOMAS, Idioma, Tema, sembrarIdioma, sembrarSesion, sembrarSesionAdmin, sembrarTema } from '../fixtures/session';

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
 * Fase 1, Ola D: carro con dos monedas, diálogo de agregar al carro, confirmación del pago, carro con
 * los pagos bloqueados, estados del resultado del pago, pago desde la cuenta (crédito), facturas,
 * historial de pagos y su detalle y, con sesión interna, los mantenedores de monedas, medios y bloqueos
 * de pago y las herramientas de Finanzas. El listado de pagos y el pago por BL anteriores se retiraron.
 * Fase 1, Ola E: documentos del embarque (M6-09) con documentos, vacío y con el repositorio caído (NF-11),
 * diálogo de la copia del BL (M6-05), formulario de la carta de responsabilidad con errores (M6-06), CLD
 * bloqueado y emitible (M6-07), solicitud del certificado de transbordo con el diálogo del carro (M6-01) y
 * el resultado de un pago que emite documentos.
 * Fase 1, Ola F: dashboard consolidado (M1-05), detalle con emisión del BL y TATC (M2-02, M2-09) y con los sistemas
 * de origen caídos (NF-11), solicitud masiva de TATC con resultado, listado del administrador con los no publicados
 * y mantenedor de reglas de publicación con su registro (M2-01), asistente abierto con una conversación que incluye
 * "no disponible", rechazo y derivación a la casilla, y el cierre con respaldo (M10-01 a M10-05), buscador DG con
 * resultados, no clasificado y sin coincidencias (M10-06) y los mantenedores de la base de conocimiento, casillas y
 * base DG. Las pantallas de la Ola F se revisan también con el tema oscuro (M11-07, `tema: 'dark'`).
 * Fase 2, Ola G (tema claro y oscuro): servicios disponibles del embarque con los no disponibles y sus motivos, formulario
 * dinámico con todos los tipos de campo y su resumen de errores, facturación con errores, cotización fuera de plazo con
 * tramo y la de Drop Off con aceptación de tarifa, mis solicitudes, detalle con línea de tiempo y documento de salida,
 * detalle pendiente de pago con la anulación, historial del cambio de almacén y su trazabilidad, bandeja interna y
 * revisión, y el mantenedor de definiciones (listado, editor con un campo de selección e historial, y errores).
 * Fase 2, Ola H (tema claro y oscuro): estado de cuenta sin crédito con documentos elegidos y con crédito, filtros con la
 * antigüedad, cierre por ítem con crédito en la confirmación, comprobante de depósito rechazado y la carga con errores,
 * bandeja de Finanzas en la revisión, anticipos con el cruce manual, refacturación IAO (cotización con errores, factura no
 * elegible, borrador con errores, seguimiento), página pública de aceptación (errores y enlace no válido) y el mantenedor
 * de conceptos imputables a crédito (historial y formulario con errores).
 * Fase 2, Ola I (tema claro y oscuro): bandeja con acciones pendientes y resueltas, preferencias de correo, comunicados del
 * cliente y su mantenedor (historial, formulario con errores y vista previa), paso de la guía del carro abierto, área de
 * administración, vista como cliente (registro de una sesión y banner con una escritura bloqueada), reportes de
 * transacciones y excepciones, Counter (listado, registro con el envío fallido e historial), listas de contactos con un
 * error, pre-creación de transportista con errores, empresa matriz (filial y matriz con el listado de embarques),
 * revisión interna de vinculaciones con el rechazo abierto y la solicitud de vinculación con una cuenta pre-creada.
 */

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

const LINEA_BASE: Record<string, string[]> = JSON.parse(
  readFileSync(path.join(__dirname, 'axe-baseline.json'), 'utf-8'),
);

type Sesion = 'ninguna' | 'cliente' | 'admin' | 'impersonacion';

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

/** Elige el primer medio de pago del carro en CLP y abre la confirmación (M5-03, WCAG 3.3.4). */
async function confirmarCarro(page: Page): Promise<void> {
  const grupo = page.getByTestId('cart-group-CL-CLP');
  await grupo.locator('input[type="radio"]').first().check();
  await grupo.locator('button.btn-hl-orange').first().click();
  await expect(page.getByTestId('cart-confirm-pay')).toBeVisible();
}

/** Abre el historial de la primera fila de un mantenedor de pagos (botón en la posición indicada). */
function abrirHistorial(titulo: string, boton: number): (page: Page) => Promise<void> {
  return async (page) => {
    await page.locator('main table tbody tr').first().locator('button').nth(boton).click();
    await expect(page.locator(titulo)).toBeFocused();
  };
}

/** Abre el asistente y conversa: dato con acciones, BL no disponible, rechazo y pregunta sin respuesta (M10-01 a M10-03). */
async function conversar(page: Page): Promise<void> {
  await page.getByTestId('assistant-launcher').click();
  const panel = page.getByTestId('assistant-panel');
  await expect(panel.getByTestId('assistant-reply')).toHaveCount(1);
  const preguntas = [`estado del BL ${BL_TATC}`, 'estado del BL HLCU999999', '¿me recomienda un abogado?', 'pregunta sin respuesta'];
  for (const [i, texto] of preguntas.entries()) {
    await panel.locator('#assistant-message').fill(texto);
    await panel.locator('#assistant-message').press('Enter');
    await expect(panel.getByTestId('assistant-reply')).toHaveCount(i + 2);
  }
  await expect(panel.getByTestId('assistant-typing')).toHaveCount(0);
}

/** Envía la solicitud masiva de TATC de San Antonio con un BL ya emitido y uno desconocido (M2-09). */
async function solicitarTatc(page: Page): Promise<void> {
  await page.locator('#tatc-location').fill('CLSAI');
  await page.locator('#tatc-bls').fill([BL_TATC, 'HLCUSAI260401020', 'XYZ123'].join('\n'));
  await page.locator('form:has(#tatc-location) button[type="submit"]').click();
  await expect(page.locator('#tatc-result-title')).toBeFocused();
}

/** Busca en el buscador de mercancías peligrosas (M10-06). */
function buscarDg(texto: string): (page: Page) => Promise<void> {
  return async (page) => {
    await page.locator('#dg-query').fill(texto);
    await page.locator('#dg-query').press('Enter');
    await expect(page.locator('#dg-result-title')).toBeFocused();
  };
}

/** Pantallas de la Ola F que se revisan en tema claro y oscuro (M11-07). */
const PANTALLAS_OLA_F: { id: string; ruta: string; sesion: Sesion; preparar?: (page: Page) => Promise<void> }[] = [
  {
    id: 'dashboard-consolidated',
    ruta: '/dashboard',
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('dashboard-pending')).toBeVisible();
      await expect(page.getByTestId('dispute-link-card')).toBeVisible();
    },
  },
  {
    id: 'shipment-detail-issuance-tatc',
    ruta: `/shipments/${BL_TATC}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('issuance-status')).toBeVisible();
      await expect(page.getByTestId('tatc-status')).toBeVisible();
    },
  },
  {
    id: 'shipment-detail-sources-down',
    ruta: `/shipments/${BL_ORIGEN_CAIDO}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('issuance-last-known')).toBeVisible();
      await expect(page.getByTestId('tatc-unavailable')).toBeVisible();
    },
  },
  { id: 'tatc-bulk-result', ruta: '/tatc', sesion: 'cliente', preparar: solicitarTatc },
  {
    id: 'admin-publication-rules',
    ruta: '/admin/publication-rules',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().locator('button').nth(1).click();
      await expect(page.locator('#publication-history-title')).toBeFocused();
    },
  },
  {
    id: 'admin-shipments-unpublished',
    ruta: '/shipments?published=false',
    sesion: 'admin',
    preparar: async (page) => {
      await expect(page.getByTestId('publication-HLCUSAI260601410')).toBeVisible();
    },
  },
  { id: 'assistant-conversation', ruta: '/dashboard', sesion: 'cliente', preparar: conversar },
  {
    id: 'assistant-end-form',
    ruta: '/dashboard',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('assistant-launcher').click();
      const panel = page.getByTestId('assistant-panel');
      await expect(panel.getByTestId('assistant-reply')).toHaveCount(1);
      await panel.locator('.hl-assistant__header button').first().click();
      await panel.locator('#assistant-transcript').check();
      await panel.locator('#assistant-recipient-other').check();
      await panel.locator('[data-testid="assistant-end-form"] button[type="submit"]').click();
      await expect(panel.locator('#assistant-other-email')).toBeFocused();
    },
  },
  { id: 'dangerous-goods-results', ruta: '/dangerous-goods', sesion: 'cliente', preparar: buscarDg('bencina') },
  { id: 'dangerous-goods-not-classified', ruta: '/dangerous-goods', sesion: 'cliente', preparar: buscarDg('agua') },
  { id: 'dangerous-goods-no-match', ruta: '/dangerous-goods', sesion: 'cliente', preparar: buscarDg('xyzabc') },
  {
    id: 'admin-assistant-knowledge',
    ruta: '/admin/assistant-knowledge',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().locator('button').nth(1).click();
      await expect(page.locator('#kb-history-title')).toBeFocused();
    },
  },
  {
    id: 'admin-assistant-knowledge-form',
    ruta: '/admin/assistant-knowledge',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('.hl-page-header button').click();
      await page.locator('form:has(#kb-title) button[type="submit"]').click();
      await expect(page.locator('form:has(#kb-title) .alert-danger')).toBeFocused();
    },
  },
  { id: 'admin-assistant-mailboxes', ruta: '/admin/assistant-mailboxes', sesion: 'admin' },
  { id: 'admin-dangerous-goods-import', ruta: '/admin/dangerous-goods', sesion: 'admin' },
];

/** Lleva la solicitud del formulario al último paso (revisión y cotización). */
function hastaRevision(llenar: (page: Page) => Promise<void>): (page: Page) => Promise<void> {
  return async (page) => {
    await llenar(page);
    await page.getByTestId('srv-next').click();
    await page.locator('#srv-billing-address').fill('Av. Apoquindo 4500, Las Condes');
    await page.getByTestId('srv-next').click();
    await expect(page.getByTestId('service-quote')).toBeVisible();
  };
}

/** Pantallas de la Ola G que se revisan en tema claro y oscuro (M11-07). */
const PANTALLAS_OLA_G: { id: string; ruta: string; sesion: Sesion; preparar?: (page: Page) => Promise<void> }[] = [
  {
    id: 'service-available-with-reasons',
    ruta: `/service-requests/new?bl=${BL_HIJO}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('#available-include-unavailable').check();
      await expect(page.getByTestId('available-reasons-SEAL_MANAGEMENT')).toBeVisible();
    },
  },
  {
    id: 'shipment-detail-available-services',
    ruta: `/shipments/${BL_DROP_OFF}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('available-service-DROP_OFF_SCL')).toBeVisible();
    },
  },
  {
    id: 'service-form-all-fields-errors',
    ruta: `/service-requests/new?bl=${BL_FORMULARIO}&code=${SERVICIO_FORMULARIO}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('srv-next').click();
      await expect(page.getByTestId('srv-error-summary')).toBeFocused();
    },
  },
  {
    id: 'service-form-billing-errors',
    ruta: `/service-requests/new?booking=${BOOKING_SELLOS}&code=SEAL_MANAGEMENT`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('#srv-field-containers-0').check();
      await page.locator('#srv-field-sealNumbers').fill('SL-100');
      await page.getByTestId('srv-next').click();
      await page.locator('#srv-billing-taxId').fill('');
      await page.getByTestId('srv-next').click();
      await expect(page.getByTestId('srv-error-summary')).toBeFocused();
    },
  },
  {
    id: 'service-quote-late-tier',
    ruta: `/service-requests/new?bl=${BL_HIJO}&code=BL_HOUSE_TRANSMISSION`,
    sesion: 'cliente',
    preparar: hastaRevision(async (page) => {
      await page.locator('#srv-field-houseBlNumbers').fill('HB-2610-01');
    }),
  },
  {
    id: 'service-quote-acceptance',
    ruta: `/service-requests/new?bl=${BL_DROP_OFF}&code=DROP_OFF_SCL`,
    sesion: 'cliente',
    preparar: hastaRevision(async (page) => {
      await page.locator('#srv-field-containers-1').check();
      await page.locator('#srv-field-returnDate').fill('2026-10-20');
      await page.locator('#srv-field-depot').selectOption('SCL_PUDAHUEL');
    }),
  },
  { id: 'service-requests-list', ruta: '/service-requests', sesion: 'cliente' },
  {
    id: 'service-request-detail-timeline',
    ruta: `/service-requests/${SOLICITUD.BL_HIJO_COMPLETADA}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('service-timeline')).toBeVisible();
      await expect(page.getByTestId('srv-output')).toBeVisible();
    },
  },
  {
    id: 'service-request-detail-payment',
    ruta: `/service-requests/${SOLICITUD.SELLOS_POR_PAGAR}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('service-payment-add')).toBeVisible();
      await page.locator('#srv-cancel-start').click();
      await expect(page.locator('#srv-cancel-title')).toBeFocused();
    },
  },
  { id: 'warehouse-history', ruta: '/warehouse/history', sesion: 'cliente' },
  { id: 'warehouse-history-detail', ruta: `/warehouse/history/${CAMBIO_PAGADO}`, sesion: 'cliente' },
  { id: 'admin-service-requests-queue', ruta: '/admin/service-requests', sesion: 'admin' },
  {
    id: 'admin-service-request-review',
    ruta: `/admin/service-requests/${SOLICITUD.DROP_OFF_PENDIENTE}`,
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('form:has(#srv-reject-reason) button[type="submit"]').click();
      await expect(page.locator('#srv-reject-reason')).toBeFocused();
    },
  },
  { id: 'admin-service-definitions', ruta: '/admin/service-definitions', sesion: 'admin' },
  {
    id: 'admin-service-definition-editor',
    ruta: `/admin/service-definitions/${idDefinicion('DROP_OFF_SCL')}`,
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('#def-add-field').click();
      await expect(page.locator('#def-field-4-key')).toBeFocused();
      await page.locator('#def-field-4-type').selectOption('select');
      await expect(page.locator('#def-field-4-option-0-value')).toBeVisible();
      await expect(page.getByTestId('definition-history')).toBeVisible();
    },
  },
  {
    id: 'admin-service-definition-new-errors',
    ruta: '/admin/service-definitions/new',
    sesion: 'admin',
    preparar: async (page) => {
      await page.getByTestId('definition-submit').click();
      await expect(page.locator('form .alert-danger').first()).toBeFocused();
    },
  },
];

/** Elige líneas del estado de cuenta por su número (columna de selección). */
function elegirLineas(...numeros: string[]): (page: Page) => Promise<void> {
  return async (page) => {
    for (const numero of numeros) await page.getByTestId(`statement-line-${numero}`).locator('input[type="checkbox"]').check();
  };
}

/** Pantallas de la Ola H que se revisan en tema claro y oscuro (M11-07). */
const PANTALLAS_OLA_H: { id: string; ruta: string; sesion: Sesion; opciones?: OpcionesOlaD; preparar?: (page: Page) => Promise<void> }[] = [
  {
    id: 'statement-cart-selection',
    ruta: '/account-statement',
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('statement-aging')).toBeVisible();
      await elegirLineas('100245', '100310')(page);
      await expect(page.getByTestId('statement-selection')).toBeVisible();
    },
  },
  {
    id: 'statement-filters-due-soon',
    ruta: '/account-statement',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('#statement-status').selectOption('DueSoon');
      await page.locator('form[role="search"] button[type="submit"]').click();
      await expect(page.getByTestId('statement-due-soon')).toHaveCount(1);
    },
  },
  {
    id: 'statement-credit',
    ruta: '/account-statement',
    sesion: 'cliente',
    opciones: { credito: true },
    preparar: async (page) => {
      await expect(page.getByTestId('statement-credit')).toBeVisible();
      await expect(page.getByTestId('statement-group-CreditImputed')).toBeVisible();
    },
  },
  {
    id: 'statement-credit-checkout',
    ruta: '/account-statement',
    sesion: 'cliente',
    opciones: { credito: true },
    preparar: async (page) => {
      await elegirLineas('THC', 'GATE_OUT', 'DEMURRAGE')(page);
      await page.getByTestId('checkout-item-GATE_OUT').getByRole('radio').nth(1).check();
      await page.getByTestId('statement-checkout').locator('input[name="statement-checkout-method"]').first().check();
      await page.getByTestId('statement-checkout').locator('button.btn-hl-orange').click();
      await expect(page.locator('#statement-checkout-confirm-title')).toBeFocused();
    },
  },
  {
    id: 'deposit-proof-rejected',
    ruta: `/payments/${PAGO_DEPOSITO.RECHAZADO}/result`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('deposit-proof-rejected')).toBeVisible();
      await expect(page.getByTestId('deposit-proof-form')).toBeVisible();
    },
  },
  {
    id: 'deposit-proof-upload-errors',
    ruta: `/payment-history/${PAGO_DEPOSITO.SIN_COMPROBANTE}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('#proof-amount').fill('abc');
      await page.getByTestId('deposit-proof-submit').click();
      await expect(page.getByTestId('deposit-proof-errors')).toBeFocused();
    },
  },
  {
    id: 'admin-deposit-proofs-review',
    ruta: '/admin/payments/deposit-proofs',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().locator('button').last().click();
      await expect(page.locator('#proof-review-title')).toBeFocused();
      await page.locator('#proof-decision-reject').check();
      await page.getByTestId('proof-review-submit').click();
      await expect(page.locator('#proof-review-reason')).toBeFocused();
    },
  },
  {
    id: 'admin-settlements-manual',
    ruta: '/admin/payments/settlements',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().locator('button').click();
      await expect(page.locator('#settlement-manual-title')).toBeFocused();
      await page.locator('form:has(#settlement-invoice) button[type="submit"]').click();
      await expect(page.locator('form:has(#settlement-invoice) .alert-danger').first()).toBeFocused();
    },
  },
  {
    id: 'reinvoicing-new-errors',
    ruta: `/reinvoicing/new?invoiceId=${ITEM.FACTURA_VENCIDA}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('reinvoicing-quote')).toBeVisible();
      await page.getByTestId('reinvoicing-create').click();
      await expect(page.getByTestId('reinvoicing-errors')).toBeFocused();
    },
  },
  {
    id: 'reinvoicing-new-ineligible',
    ruta: `/reinvoicing/new?invoiceId=${ITEM.FACTURA_PAGADA}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('reinvoicing-ineligible')).toBeVisible();
    },
  },
  {
    id: 'reinvoicing-draft-errors',
    ruta: `/reinvoicing/${REFACTURACION_BORRADOR}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('reinvoicing-submit').click();
      await expect(page.getByTestId('reinvoicing-submit-errors')).toBeFocused();
    },
  },
  {
    id: 'reinvoicing-status-pending',
    ruta: `/reinvoicing/${REFACTURACION}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('reinvoicing-acceptance')).toBeVisible();
      await expect(page.getByTestId('service-payment')).toBeVisible();
    },
  },
  {
    id: 'reinvoicing-acceptance-errors',
    ruta: `/reinvoicing/acceptance/${TOKEN_ACEPTACION}`,
    sesion: 'ninguna',
    preparar: async (page) => {
      await page.getByTestId('acceptance-submit').click();
      await expect(page.getByTestId('acceptance-errors')).toBeFocused();
    },
  },
  {
    id: 'reinvoicing-acceptance-invalid',
    ruta: '/reinvoicing/acceptance/ENLACE-NO-VALIDO',
    sesion: 'ninguna',
    preparar: async (page) => {
      await expect(page.getByTestId('acceptance-error')).toBeVisible();
    },
  },
  { id: 'admin-credit-rules-history', ruta: '/admin/credit-imputation-rules', sesion: 'admin', preparar: abrirHistorial('#credit-rule-history-title', 1) },
  {
    id: 'admin-credit-rules-form',
    ruta: '/admin/credit-imputation-rules',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('.hl-page-header button').click();
      await page.locator('form:has(#credit-rule-nexus) button[type="submit"]').click();
      await expect(page.locator('form:has(#credit-rule-nexus) .alert-danger').first()).toBeFocused();
    },
  },
];

/** Fase 2, Ola I: bandeja, comunicados, guía, administración, vista como cliente, reportería, Counter y organización. */
const PANTALLAS_OLA_I: { id: string; ruta: string; sesion: Sesion; opciones?: OpcionesOlaD & OpcionesOlaI; preparar?: (page: Page) => Promise<void> }[] = [
  {
    id: 'notifications-inbox',
    ruta: '/notifications',
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('notification-resolved-n1000000-0000-4000-8000-000000000002')).toBeVisible();
      await expect(page.getByTestId('notifications-by-module')).toBeVisible();
    },
  },
  { id: 'notifications-preferences', ruta: '/notifications/preferences', sesion: 'cliente' },
  { id: 'announcements', ruta: '/announcements', sesion: 'cliente' },
  { id: 'admin-announcements', ruta: '/admin/announcements', sesion: 'admin', preparar: abrirHistorial('#announcement-history-title', 2) },
  {
    id: 'admin-announcements-form',
    ruta: '/admin/announcements',
    sesion: 'admin',
    preparar: async (page) => {
      await page.getByTestId('announcement-new').click();
      await page.locator('#announcement-country-cl').uncheck();
      await page.locator('form[data-testid="announcement-form"] button[type="submit"]').click();
      await expect(page.locator('form[data-testid="announcement-form"] .alert-danger').first()).toBeFocused();
      await page.getByTestId('announcement-preview-toggle').click();
      await expect(page.getByTestId('announcement-preview')).toBeVisible();
    },
  },
  {
    id: 'guide-step-open',
    ruta: '/cart',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId(`guide-launch-${GUIA_CARRO}`).click();
      await expect(page.getByTestId('guide-step')).toBeVisible();
      await expect(page.locator('#hl-guide-title')).toBeFocused();
      await page.getByTestId('guide-next').click();
      await expect(page.getByTestId('guide-progress')).toContainText('2');
    },
  },
  { id: 'admin-home', ruta: '/admin', sesion: 'admin' },
  {
    id: 'admin-impersonation',
    ruta: '/admin/impersonation',
    sesion: 'admin',
    preparar: async (page) => {
      await page.getByTestId(`impersonation-session-${SESION_TERMINADA}`).getByRole('button').first().click();
      await expect(page.locator('#impersonation-audit-title')).toBeFocused();
    },
  },
  {
    id: 'impersonation-banner-blocked',
    ruta: '/notifications',
    sesion: 'impersonacion',
    opciones: { impersonando: true },
    preparar: async (page) => {
      await expect(page.getByTestId('impersonation-banner')).toBeVisible();
      await page.getByTestId('notifications-mark-all').click();
      await expect(page.getByTestId('impersonation-blocked')).toBeVisible();
    },
  },
  { id: 'admin-reports-transactions', ruta: '/admin/reports/transactions', sesion: 'admin' },
  { id: 'admin-reports-exceptions', ruta: '/admin/reports/exceptions', sesion: 'admin' },
  { id: 'admin-counter', ruta: '/admin/counter', sesion: 'admin' },
  {
    id: 'admin-counter-edit',
    ruta: '/admin/counter',
    sesion: 'admin',
    preparar: async (page) => {
      await page.getByTestId(`counter-row-${BL_COUNTER_FALLIDO}`).getByRole('button').first().click();
      await expect(page.getByTestId('counter-sync-failed')).toBeVisible();
      await expect(page.locator('#counter-detail-title')).toBeFocused();
    },
  },
  { id: 'admin-counter-history', ruta: '/admin/counter', sesion: 'admin', preparar: abrirHistorial('#counter-history-title', 1) },
  {
    id: 'organization-contact-lists',
    ruta: '/organization',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('contact-list-ARRIVAL_NOTICE').getByRole('button').click();
      await page.locator('#contact-emails-ARRIVAL_NOTICE').fill('avisos@importadoraandes.cl, correo-no-valido');
      await page.getByTestId('contact-list-ARRIVAL_NOTICE').locator('button[type="submit"]').click();
      await expect(page.locator('#contact-emails-error-ARRIVAL_NOTICE')).toBeVisible();
    },
  },
  {
    id: 'organization-carriers-form',
    ruta: '/organization',
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('carrier-new').click();
      await page.getByTestId('carrier-submit').click();
      await expect(page.locator('form[data-testid="carrier-form"] .alert-danger').first()).toBeFocused();
    },
  },
  {
    id: 'organization-parent-subsidiary',
    ruta: '/organization',
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('parent-link')).toBeVisible();
      await expect(page.getByTestId('contact-lists-history')).toBeVisible();
    },
  },
  {
    id: 'organization-parent-parent',
    ruta: '/organization',
    sesion: 'cliente',
    opciones: { matriz: true },
    preparar: async (page) => {
      await expect(page.getByTestId('parent-subsidiaries')).toBeVisible();
      await page.locator('#parent-search').fill('Holding');
      await page.getByTestId('parent-request').getByRole('button').first().click();
      await expect(page.getByTestId('parent-request').locator('fieldset')).toBeVisible();
    },
  },
  {
    id: 'shipments-parent',
    ruta: '/shipments',
    sesion: 'cliente',
    opciones: { matriz: true },
    preparar: async (page) => {
      await expect(page.getByTestId('shipments-filter-organization')).toBeVisible();
      await expect(page.locator('[data-testid^="origin-"]').first()).toBeVisible();
    },
  },
  {
    id: 'admin-organization-links',
    ruta: '/admin/organization-links',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('main table tbody tr').first().getByRole('button').nth(1).click();
      await page.getByTestId('links-reject').locator('button[type="submit"]').click();
      await expect(page.locator('#links-reject-error')).toBeVisible();
    },
  },
  {
    id: 'register-join-pre-created',
    ruta: '/register/join',
    sesion: 'ninguna',
    preparar: async (page) => {
      await page.locator('#joinTaxId').fill(TRANSPORTISTA.taxId);
      await page.locator('#joinFirstName').fill('Rodrigo');
      await page.locator('#joinLastName').fill('Valdés');
      await page.locator('#joinEmail').fill(TRANSPORTISTA.email);
      await page.locator('#joinPassword').fill('Clave-Segura-2026');
      await page.locator('#joinConfirmPassword').fill('Clave-Segura-2026');
      await page.locator('form button[type="submit"]').click();
      await expect(page.getByTestId('pre-created-notice')).toBeVisible();
    },
  },
];

const PANTALLAS: { id: string; ruta: string; sesion: Sesion; tema?: Tema; opciones?: OpcionesOlaD & OpcionesOlaI; preparar?: (page: Page) => Promise<void> }[] = [
  { id: 'login', ruta: '/login', sesion: 'ninguna' },
  { id: 'register', ruta: '/register', sesion: 'ninguna' },
  { id: 'register-join', ruta: '/register/join', sesion: 'ninguna' },
  { id: 'dashboard', ruta: '/dashboard', sesion: 'cliente' },
  { id: 'shipments', ruta: '/shipments', sesion: 'cliente' },
  { id: 'shipment-detail', ruta: `/shipments/${BL_PRUEBA.blNumber}`, sesion: 'cliente' },
  { id: 'shipment-detail-export', ruta: `/shipments/${BL_EXPORTACION}`, sesion: 'cliente' },
  { id: 'shipment-detail-no-freight', ruta: `/shipments/${BL_SIN_FLETE}`, sesion: 'cliente' },
  { id: 'organization', ruta: '/organization', sesion: 'cliente' },
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
  // Ola D
  { id: 'cart-two-currencies', ruta: '/cart', sesion: 'cliente' },
  { id: 'cart-confirm', ruta: '/cart', sesion: 'cliente', preparar: confirmarCarro },
  { id: 'cart-blocked', ruta: '/cart', sesion: 'cliente', opciones: { bloqueo: true } },
  {
    id: 'cart-add-dialog',
    ruta: `/charges/${BL_PRUEBA.blNumber}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.locator('app-charges-panel table tbody tr').filter({ hasText: 'IPO' }).locator('button').click();
      await expect(page.locator('#add-to-cart-billing')).toBeVisible();
    },
  },
  { id: 'payment-result-confirmed', ruta: `/payments/${PAGO.CONFIRMADO}/result`, sesion: 'cliente' },
  { id: 'payment-result-failed', ruta: `/payments/${PAGO.FALLIDO}/result`, sesion: 'cliente' },
  { id: 'payment-result-processing', ruta: `/payments/${PAGO.EN_PROCESO}/result`, sesion: 'cliente' },
  { id: 'payment-result-slip-issued', ruta: `/payments/${PAGO.BOLETA_EMITIDA}/result`, sesion: 'cliente' },
  {
    id: 'account-payments',
    ruta: '/account-payments',
    sesion: 'cliente',
    opciones: { credito: true },
    preparar: async (page) => {
      await page.locator('#account-payments-all').check();
      await expect(page.locator('#account-payments-currency')).toBeVisible();
    },
  },
  { id: 'invoices', ruta: '/invoices', sesion: 'cliente' },
  { id: 'payment-history', ruta: '/payment-history', sesion: 'cliente' },
  { id: 'payment-history-detail', ruta: `/payment-history/${PAGO.MANDATO}`, sesion: 'cliente' },
  { id: 'admin-payment-currencies', ruta: '/admin/payment-currencies', sesion: 'admin', preparar: abrirHistorial('#currencies-history-title', 1) },
  {
    id: 'admin-payment-methods',
    ruta: '/admin/payment-methods',
    sesion: 'admin',
    preparar: async (page) => {
      await page.locator('.hl-page-header button').click();
      await page.locator('form:has(#method-code) button[type="submit"]').click();
      await expect(page.locator('form:has(#method-code) .alert-danger')).toBeFocused();
    },
  },
  { id: 'admin-payment-blocks', ruta: '/admin/payment-blocks', sesion: 'admin', preparar: abrirHistorial('#block-history-title', 2) },
  { id: 'admin-payments-finance', ruta: '/admin/payments-finance', sesion: 'admin' },
  // Ola E
  { id: 'documents', ruta: `/shipments/${BL_DOCUMENTOS}/documents`, sesion: 'cliente' },
  { id: 'documents-empty', ruta: `/shipments/${BL_SOLO_SHIPPER}/documents`, sesion: 'cliente' },
  { id: 'documents-error', ruta: `/shipments/${BL_DOCUMENTOS_CAIDO}/documents`, sesion: 'cliente' },
  {
    id: 'documents-bl-copy-dialog',
    ruta: `/shipments/${BL_DOCUMENTOS}/documents`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('documents-request-copy').click();
      await expect(page.locator('#bl-copy-valued')).toBeVisible();
    },
  },
  {
    id: 'documents-letter-form',
    ruta: `/charges/${BL_CARTA}`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('charges-letter-generate').click();
      await expect(page.getByTestId('letter-terms')).toBeVisible();
      await page.locator('#letter-form button[type="submit"]').click();
      await expect(page.locator('#letter-form .alert-danger')).toBeFocused();
    },
  },
  {
    id: 'documents-cld-blocked',
    ruta: `/shipments/${BL_CLD_BLOQUEADO}/documents`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('no-debt-blocked')).toBeVisible();
    },
  },
  {
    id: 'documents-cld-eligible',
    ruta: `/shipments/${BL_CLD_EMITIBLE}/documents`,
    sesion: 'cliente',
    preparar: async (page) => {
      await expect(page.getByTestId('no-debt-eligible')).toBeVisible();
    },
  },
  {
    id: 'documents-transshipment-request',
    ruta: `/shipments/${BL_DOCUMENTOS}/documents`,
    sesion: 'cliente',
    preparar: async (page) => {
      await page.getByTestId('transshipment-request').click();
      await expect(page.locator('#add-to-cart-billing')).toBeVisible();
    },
  },
  { id: 'payment-result-documents', ruta: `/payments/${PAGO_CON_DOCUMENTOS}/result`, sesion: 'cliente' },
  // Ola F, en tema claro y oscuro (M11-07)
  ...PANTALLAS_OLA_F,
  ...PANTALLAS_OLA_F.map((p) => ({ ...p, id: `${p.id}-dark`, tema: 'dark' as const })),
  // Fase 2, Ola G, en tema claro y oscuro (M11-07)
  ...PANTALLAS_OLA_G,
  ...PANTALLAS_OLA_G.map((p) => ({ ...p, id: `${p.id}-dark`, tema: 'dark' as const })),
  // Fase 2, Ola H, en tema claro y oscuro (M11-07)
  ...PANTALLAS_OLA_H,
  ...PANTALLAS_OLA_H.map((p) => ({ ...p, id: `${p.id}-dark`, tema: 'dark' as const })),
  // Fase 2, Ola I, en tema claro y oscuro (M11-07)
  ...PANTALLAS_OLA_I,
  // La solicitud de vinculación (registro) no tiene variante oscura propia todavía: se revisa en tema claro.
  ...PANTALLAS_OLA_I.filter((p) => p.sesion !== 'ninguna').map((p) => ({ ...p, id: `${p.id}-dark`, tema: 'dark' as const })),
];

async function abrir(page: Page, ruta: string, sesion: Sesion, lang: Idioma, opciones?: OpcionesOlaD & OpcionesOlaI, tema: Tema = 'light'): Promise<void> {
  await simularApi(page, opciones);
  await sembrarTema(page, tema);
  if (sesion === 'cliente') {
    await sembrarSesion(page, { lang });
  } else if (sesion === 'admin') {
    await sembrarSesionAdmin(page, lang);
  } else if (sesion === 'impersonacion') {
    await sembrarImpersonacion(page, lang);
  } else {
    await sembrarIdioma(page, lang);
  }
  await page.emulateMedia({ reducedMotion: 'reduce', colorScheme: tema });
  await page.goto(ruta);
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('app-loading-spinner')).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
}

for (const lang of IDIOMAS) {
  for (const p of PANTALLAS) {
    test(`axe: ${p.id} [${lang}]`, async ({ page }) => {
      await abrir(page, p.ruta, p.sesion, lang, p.opciones, p.tema);
      await p.preparar?.(page);
      await expect(page).toHaveURL(new RegExp(`${p.ruta.replace(/[?]/g, '\\?')}$`));
      await expect(page.locator('html')).toHaveAttribute('lang', lang);
      await expect(page.locator('html')).toHaveAttribute('data-bs-theme', p.tema ?? 'light');

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
