import { Request, Route } from '@playwright/test';
import type {
  AccountPayables,
  BillingOption,
  Cart,
  CartGroup,
  CartItem,
  CheckoutResult,
  PayableItem,
  PaymentBlockStatus,
  PaymentMethod,
  PaymentStatusDetail,
  PaymentSummary,
} from '../../src/app/core/models/cart.model';
import type { Invoice, InvoiceList, InvoiceOrganization } from '../../src/app/core/models/invoice.model';
import type { PaymentHistoryItem } from '../../src/app/core/models/payment.model';
import type {
  MaintainerChange,
  PaymentBlockWindow,
  PaymentCurrencyConfig,
  PaymentOperation,
  PaymentReconciliation,
} from '../../src/app/core/models/payment-config.model';
import type { CommercialConditions } from '../../src/app/core/models/charges.model';
import { ORGANIZACION_PRUEBA } from './session';

/**
 * Backend simulado de la Ola D (carro, pagos, facturas, historial y configuración de pagos). A diferencia
 * del resto de los mocks, guarda estado por página: el carro cambia al agregar, convertir, quitar y pagar,
 * los pagos avanzan de estado en cada consulta y la clave de idempotencia devuelve el mismo resultado.
 */

/** Opciones de la simulación (por prueba). */
export interface OpcionesOlaD {
  /** La organización tiene crédito en Nexus (M5-07): usa "Pagar desde mi cuenta". */
  credito?: boolean;
  /** Ventana de bloqueo de pagos activa (M8-07). */
  bloqueo?: boolean;
  /** Cantidad de cierres iniciales que no responden (503): el cliente debe reintentar con la misma clave. */
  cierresSinRespuesta?: number;
  /** Nexus no responde: no se pueden validar las condiciones al agregar al carro (NF-11). */
  nexusCaido?: boolean;
}

export const MENSAJE_BLOQUEO = 'Pagos suspendidos por cierre contable hasta las 23:59.';

const BL_PRUEBA_ID = '3f0c2a1e-0000-4000-8000-000000000001';
const BL_PRUEBA_NUM = 'HLCU0000001';
const HOY = '2026-10-05';

/** RUT de la propia organización y del mandante que le otorgó acceso (M5-09). */
export const RUT_PROPIO = '76123456-7';
export const RUT_MANDANTE = '77222333-4';
export const NOMBRE_MANDANTE = 'Comercial Mandante SpA';
/** RUT de la agencia que pagó bajo mandato (M7-02). */
export const RUT_AGENCIA = '96555444-3';

const FACTURACION_PROPIA: BillingOption = { taxId: RUT_PROPIO, name: ORGANIZACION_PRUEBA.name, organizationId: ORGANIZACION_PRUEBA.id, source: 'Own' };
const FACTURACION_MANDANTE: BillingOption = {
  taxId: RUT_MANDANTE,
  name: NOMBRE_MANDANTE,
  organizationId: 'c1000000-0000-4000-8000-000000000010',
  source: 'Grant',
  accessGrantId: 'a1000000-0000-4000-8000-000000000010',
};

/** Ids de los ítems pagables usados por las pruebas. */
export const ITEM = {
  THC: 'c3000000-0000-4000-8000-000000000011',
  GATE_IN: 'c3000000-0000-4000-8000-000000000012',
  IPO: 'c3000000-0000-4000-8000-000000000013',
  MHD: 'd3000000-0000-4000-8000-000000000101',
  MHD_BO: 'd3000000-0000-4000-8000-000000000201',
  ADELANTO_BO: 'd3000000-0000-4000-8000-000000000301',
  LINEA_DEMURRAGE: 'd3000000-0000-4000-8000-000000000011',
  CAMBIO_ALMACEN: 'w3000000-0000-4000-8000-000000000001',
  FACTURA_DEMURRAGE: 'i4000000-0000-4000-8000-000000000915',
  FACTURA_VENCIDA: 'i4000000-0000-4000-8000-000000003987',
  FACTURA_PAGADA: 'i4000000-0000-4000-8000-000000004120',
  FACTURA_SIN_FOLIO: 'i4000000-0000-4000-8000-000000004601',
  NOTA_CREDITO: 'i4000000-0000-4000-8000-000000004200',
  THC_CREDITO: 'c3000000-0000-4000-8000-000000000021',
  FLETE_CREDITO: '3f0c2a1e-0000-4000-8000-000000001130',
} as const;

/** Pagos de demostración para las pantallas de resultado (NF-02, NF-12). */
export const PAGO = {
  CONFIRMADO: 'p5000000-0000-4000-8000-000000000101',
  FALLIDO: 'p5000000-0000-4000-8000-000000000102',
  EN_PROCESO: 'p5000000-0000-4000-8000-000000000103',
  BOLETA_EMITIDA: 'p5000000-0000-4000-8000-000000000104',
  BOLETA_POR_EMITIR: 'p5000000-0000-4000-8000-000000000105',
  MANDATO: 'h5000000-0000-4000-8000-000000000003',
} as const;

const TASAS: Record<string, number> = { 'USD>CLP': 950, 'EUR>CLP': 1030, 'USD>BOB': 6.96 };

function tasa(from: string, to: string): number {
  return from === to ? 1 : (TASAS[`${from}>${to}`] ?? 1);
}

function redondear(monto: number, moneda: string): number {
  return moneda === 'CLP' ? Math.round(monto) : Math.round(monto * 100) / 100;
}

function pagable(datos: Partial<PayableItem> & Pick<PayableItem, 'itemType' | 'sourceId' | 'conceptCode' | 'totalAmount'>): PayableItem {
  return {
    blId: BL_PRUEBA_ID,
    blNumber: BL_PRUEBA_NUM,
    bookingNumber: 'BKG26030010',
    country: 'CL',
    conceptName: datos.conceptCode,
    description: null,
    amount: datos.totalAmount,
    taxAmount: 0,
    currency: 'CLP',
    allowedCurrencies: ['CLP'],
    defaultPaymentCurrency: datos.currency ?? 'CLP',
    billingOptions: [FACTURACION_PROPIA],
    ...datos,
  };
}

/** Lo que valida GET /cart/item-options para cada ítem (por id o por número de factura). */
const PAGABLES: Record<string, PayableItem> = {
  [`LocalCharge:${ITEM.THC}`]: pagable({ itemType: 'LocalCharge', sourceId: ITEM.THC, conceptCode: 'THC', description: 'Terminal Handling Charge', amount: 100000, taxAmount: 19000, totalAmount: 119000 }),
  [`LocalCharge:${ITEM.GATE_IN}`]: pagable({ itemType: 'LocalCharge', sourceId: ITEM.GATE_IN, conceptCode: 'GATE_IN', amount: 50000, taxAmount: 9500, totalAmount: 59500 }),
  [`LocalCharge:${ITEM.IPO}`]: pagable({
    itemType: 'LocalCharge', sourceId: ITEM.IPO, conceptCode: 'IPO', totalAmount: 150, currency: 'USD',
    allowedCurrencies: ['USD', 'CLP'], defaultPaymentCurrency: 'USD', billingOptions: [FACTURACION_PROPIA, FACTURACION_MANDANTE],
  }),
  [`LocalCharge:${ITEM.MHD}`]: pagable({
    itemType: 'LocalCharge', sourceId: ITEM.MHD, conceptCode: 'MHD', blNumber: 'HLCUSAI260400910', totalAmount: 450, currency: 'USD',
    allowedCurrencies: ['USD', 'CLP'], defaultPaymentCurrency: 'USD',
  }),
  [`LocalCharge:${ITEM.ADELANTO_BO}`]: pagable({
    itemType: 'LocalCharge', sourceId: ITEM.ADELANTO_BO, conceptCode: 'ADVANCE_DEMURRAGE_BO', blNumber: 'HLCUARI260100045', country: 'BO',
    totalAmount: 150, currency: 'USD', allowedCurrencies: ['USD', 'BOB'], defaultPaymentCurrency: 'USD',
  }),
  [`Demurrage:${ITEM.LINEA_DEMURRAGE}`]: pagable({
    itemType: 'Demurrage', sourceId: ITEM.LINEA_DEMURRAGE, conceptCode: 'DEMURRAGE', blNumber: 'HLCUVAL250100123',
    description: 'HLXU3034002', totalAmount: 175000, allowedCurrencies: ['CLP', 'USD'],
  }),
  [`Invoice:FAC-DEM-2026-0915`]: pagable({
    itemType: 'Invoice', sourceId: ITEM.FACTURA_DEMURRAGE, conceptCode: 'INVOICE', blNumber: 'HLCUSAI260400910',
    description: 'FAC-DEM-2026-0915', totalAmount: 510000,
  }),
  [`Invoice:${ITEM.FACTURA_VENCIDA}`]: pagable({ itemType: 'Invoice', sourceId: ITEM.FACTURA_VENCIDA, conceptCode: 'INVOICE', description: 'HL-CL-2026-003987', totalAmount: 595000 }),
  [`Invoice:${ITEM.FACTURA_SIN_FOLIO}`]: pagable({
    itemType: 'Invoice', sourceId: ITEM.FACTURA_SIN_FOLIO, conceptCode: 'INVOICE', description: 'HL-CL-2026-004601', totalAmount: 1250,
    currency: 'USD', allowedCurrencies: ['USD', 'CLP'], defaultPaymentCurrency: 'USD',
  }),
  [`Freight:${BL_PRUEBA_ID}`]: pagable({
    itemType: 'Freight', sourceId: BL_PRUEBA_ID, conceptCode: 'FREIGHT', totalAmount: 2450, currency: 'USD',
    allowedCurrencies: ['USD', 'EUR', 'CLP'], defaultPaymentCurrency: 'USD',
  }),
  [`WarehouseChange:${ITEM.CAMBIO_ALMACEN}`]: pagable({ itemType: 'WarehouseChange', sourceId: ITEM.CAMBIO_ALMACEN, conceptCode: 'WAREHOUSE_CHANGE', totalAmount: 9940 }),
};

function metodo(n: number, code: string, name: string, kind: 'Online' | 'Deposit', currencies: string[], datos: Partial<PaymentMethod> = {}): PaymentMethod {
  return {
    id: `m6000000-0000-4000-8000-${String(n).padStart(12, '0')}`,
    code,
    name,
    description: null,
    country: 'CL',
    kind,
    providerKey: kind === 'Online' ? 'Khipu' : null,
    currencies,
    isEnabled: true,
    displayOrder: 10,
    createdAt: '2026-10-01T12:00:00Z',
    modifiedAt: null,
    ...datos,
  };
}

const METODOS: PaymentMethod[] = [
  metodo(1, 'KHIPU', 'Khipu', 'Online', ['CLP'], { description: 'Transferencia simplificada desde su banco.', displayOrder: 1 }),
  metodo(2, 'BANK_BUTTON_BCH', 'Botón Banco de Chile', 'Online', ['CLP', 'USD'], { providerKey: 'BancoChile', displayOrder: 2 }),
  metodo(3, 'DEPOSIT', 'Depósito bancario con boleta', 'Deposit', ['CLP', 'USD', 'EUR'], { description: 'Emita la boleta y deposite; Finanzas confirma el abono.', displayOrder: 9 }),
  metodo(4, 'DIGITAL_USD', 'Dólares digitales', 'Online', ['USD'], { isEnabled: false, displayOrder: 20 }),
];

function metodosPara(country: string, currency: string): PaymentMethod[] {
  if (country === 'BO') return [metodo(5, 'DEPOSIT', 'Depósito bancario con boleta', 'Deposit', ['BOB', 'USD'], { country: 'BO' })].filter((m) => m.currencies.includes(currency));
  return METODOS.filter((m) => m.isEnabled && m.currencies.includes(currency));
}

function estadoBloqueo(country: string, bloqueado: boolean): PaymentBlockStatus {
  return {
    country,
    timeZone: country === 'BO' ? 'America/La_Paz' : 'America/Santiago',
    blocked: bloqueado,
    windowId: bloqueado ? 'b7000000-0000-4000-8000-000000000001' : null,
    message: bloqueado ? MENSAJE_BLOQUEO : null,
    endDate: bloqueado ? HOY : null,
    endTime: bloqueado ? '23:59:00' : null,
    evaluatedAt: '2026-10-05T18:00:00Z',
  };
}

function itemCarro(item: PayableItem, billing: BillingOption, paymentCurrency: string, id: string): CartItem {
  const rate = tasa(item.currency, paymentCurrency);
  return {
    id,
    itemType: item.itemType,
    sourceId: item.sourceId,
    blId: item.blId,
    blNumber: item.blNumber,
    bookingNumber: item.bookingNumber,
    country: item.country,
    conceptCode: item.conceptCode,
    conceptName: item.conceptName,
    description: item.description,
    amount: item.amount,
    taxAmount: item.taxAmount,
    totalAmount: item.totalAmount,
    currency: item.currency,
    paymentCurrency,
    paymentAmount: redondear(item.totalAmount * rate, paymentCurrency),
    exchangeRate: item.currency !== paymentCurrency
      ? { fromCurrency: item.currency, toCurrency: paymentCurrency, rate, effectiveDate: HOY, source: 'NEXUS' }
      : null,
    allowedCurrencies: item.allowedCurrencies,
    billingTaxId: billing.taxId,
    billingName: billing.name,
    onBehalfOfOrganizationId: billing.source === 'Grant' ? billing.organizationId : null,
    lockedByPaymentId: null,
    addedAt: '2026-10-05T14:00:00Z',
  };
}

function resumenPago(datos: Partial<PaymentSummary> & Pick<PaymentSummary, 'id' | 'paymentNumber' | 'status'>): PaymentSummary {
  return {
    origin: 'Cart',
    country: 'CL',
    currency: 'CLP',
    amount: 100000,
    taxAmount: 19000,
    totalAmount: 119000,
    method: 'KHIPU',
    paymentMethodCode: 'KHIPU',
    externalReference: datos.paymentNumber,
    createdAt: '2026-10-05T15:00:00Z',
    items: [
      {
        id: `${datos.id.slice(0, 30)}01`, itemType: 'LocalCharge', sourceId: ITEM.THC, conceptCode: 'THC', description: 'Terminal Handling Charge',
        blNumber: BL_PRUEBA_NUM, bookingNumber: 'BKG26030010', amount: 100000, taxAmount: 19000, totalAmount: 119000, currency: 'CLP',
        originalAmount: 119000, originalCurrency: 'CLP', billingTaxId: RUT_PROPIO, billingName: ORGANIZACION_PRUEBA.name,
      },
    ],
    ...datos,
  };
}

function historial(estados: string[], fallo?: string): PaymentStatusDetail['history'] {
  return estados.map((toStatus, i) => ({
    fromStatus: i === 0 ? null : (estados[i - 1] as PaymentSummary['status']),
    toStatus: toStatus as PaymentSummary['status'],
    changedAt: `2026-10-05T15:0${i}:00Z`,
    changedBy: i === 0 ? 'cliente.prueba@example.com' : 'SYSTEM',
    reason: i === estados.length - 1 ? (fallo ?? null) : null,
  }));
}

/** Pagos de demostración con su estado fijo. */
function pagosFijos(): Record<string, PaymentStatusDetail> {
  return {
    [PAGO.CONFIRMADO]: {
      payment: resumenPago({ id: PAGO.CONFIRMADO, paymentNumber: 'PAY-20260912-1A2B3C4D', status: 'Confirmed', receiptNumber: 'RCP-20260912-5F6E7D8C', confirmedAt: '2026-10-05T15:02:00Z' }),
      history: historial(['Pending', 'Processing', 'Confirmed']),
      canCancel: false,
      cancelDeniedReason: 'FINAL',
      canIssueSlip: false,
      releasePending: false,
    },
    [PAGO.FALLIDO]: {
      payment: resumenPago({ id: PAGO.FALLIDO, paymentNumber: 'PAY-20261004-3D2C1B0A', status: 'Failed', failureReason: 'PROVIDER_UNAVAILABLE' }),
      history: historial(['Pending', 'Failed'], 'PROVIDER_UNAVAILABLE'),
      canCancel: false,
      cancelDeniedReason: 'FINAL',
      canIssueSlip: false,
      releasePending: false,
    },
    [PAGO.EN_PROCESO]: {
      payment: resumenPago({ id: PAGO.EN_PROCESO, paymentNumber: 'PAY-20261005-7A7A7A7A', status: 'Processing', providerReference: 'PRV-PAY-20261005-7A7A7A7A' }),
      history: historial(['Pending', 'Processing']),
      canCancel: false,
      cancelDeniedReason: 'IN_PROGRESS',
      canIssueSlip: false,
      releasePending: false,
    },
    [PAGO.BOLETA_EMITIDA]: {
      payment: resumenPago({
        id: PAGO.BOLETA_EMITIDA, paymentNumber: 'PAY-20261003-9C8B7A6D', status: 'PendingVerification', method: 'DEPOSIT', paymentMethodCode: 'DEPOSIT',
        slipNumber: 'BOL-20261003-1F2E3D4C', slipIssuedAt: '2026-10-03T14:00:00Z',
      }),
      history: historial(['Pending', 'PendingVerification']),
      canCancel: false,
      cancelDeniedReason: 'SLIP_ISSUED',
      canIssueSlip: false,
      releasePending: false,
    },
  };
}

function organizacionFacturas(datos: Partial<InvoiceOrganization> = {}): InvoiceOrganization {
  return { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO, isOwn: true, ...datos };
}

const ORG_MANDANTE_FACTURAS = organizacionFacturas({ id: FACTURACION_MANDANTE.organizationId as string, name: NOMBRE_MANDANTE, taxId: RUT_MANDANTE, isOwn: false });

function factura(datos: Partial<Invoice> & Pick<Invoice, 'id' | 'sourceNumber'>): Invoice {
  return {
    siiNumber: null,
    documentType: 'Invoice',
    issueDate: '2026-09-15',
    dueDate: '2026-10-15',
    blId: BL_PRUEBA_ID,
    blNumber: BL_PRUEBA_NUM,
    bookingNumber: 'BKG26030010',
    legalName: ORGANIZACION_PRUEBA.name,
    taxId: RUT_PROPIO,
    netAmount: 500000,
    taxAmount: 95000,
    totalAmount: 595000,
    currency: 'CLP',
    status: 'Pending',
    siiStatus: 'Accepted',
    canDownload: false,
    isPayable: false,
    inCart: false,
    syncedAt: '2026-10-05T12:00:00Z',
    ...datos,
  };
}

const FACTURAS: Invoice[] = [
  factura({ id: ITEM.FACTURA_VENCIDA, siiNumber: '100245', sourceNumber: 'HL-CL-2026-003987', dueDate: '2026-09-30', status: 'Overdue', canDownload: true, isPayable: true }),
  factura({ id: ITEM.FACTURA_PAGADA, siiNumber: '100301', sourceNumber: 'HL-CL-2026-004120', status: 'Paid', canDownload: true, issueDate: '2026-09-20', dueDate: '2026-10-20' }),
  factura({ id: ITEM.FACTURA_SIN_FOLIO, sourceNumber: 'HL-CL-2026-004601', currency: 'USD', netAmount: 1250, taxAmount: 0, totalAmount: 1250, isPayable: true, issueDate: '2026-10-02', dueDate: '2026-11-01' }),
  factura({ id: ITEM.NOTA_CREDITO, siiNumber: '100310', sourceNumber: 'HL-CL-2026-004200', documentType: 'CreditNote', status: 'Paid', canDownload: true, totalAmount: 59500, netAmount: 50000, taxAmount: 9500 }),
];

const FACTURAS_MANDANTE: Invoice[] = [
  factura({ id: 'i4000000-0000-4000-8000-000000009001', siiNumber: '200100', sourceNumber: 'HL-CL-2026-009001', legalName: NOMBRE_MANDANTE, taxId: RUT_MANDANTE, canDownload: true }),
];

function pagoHistorial(datos: Partial<PaymentHistoryItem> & Pick<PaymentHistoryItem, 'id' | 'paymentNumber'>): PaymentHistoryItem {
  return {
    documentNumber: null,
    receiptNumber: null,
    slipNumber: null,
    paymentDate: '2026-09-12T15:00:00Z',
    status: 'Confirmed',
    origin: 'Cart',
    method: 'KHIPU',
    methodName: 'Khipu',
    country: 'CL',
    currency: 'CLP',
    amount: 100000,
    taxAmount: 19000,
    totalAmount: 119000,
    payerTaxId: RUT_PROPIO,
    payerName: ORGANIZACION_PRUEBA.name,
    billingTaxIds: [RUT_PROPIO],
    payerDiffersFromBilling: false,
    blNumbers: [BL_PRUEBA_NUM],
    bookingNumbers: ['BKG26030010'],
    onBehalfOf: null,
    executedBy: 'cliente.prueba@example.com',
    receiptAvailable: true,
    items: resumenPago({ id: datos.id, paymentNumber: datos.paymentNumber, status: 'Confirmed' }).items,
    ...datos,
  };
}

const HISTORIAL: PaymentHistoryItem[] = [
  pagoHistorial({
    id: 'h5000000-0000-4000-8000-000000000005', paymentNumber: 'PAY-CL-2026-000125', documentNumber: 'RCP-20261002-0C0C0C0C',
    receiptNumber: 'RCP-20261002-0C0C0C0C', paymentDate: '2026-10-02T15:20:00Z', amount: 1037451, taxAmount: 197116, totalAmount: 1234567,
  }),
  pagoHistorial({
    id: 'h5000000-0000-4000-8000-000000000004', paymentNumber: 'PAY-20261003-9C8B7A6D', documentNumber: 'BOL-20261003-1F2E3D4C',
    slipNumber: 'BOL-20261003-1F2E3D4C', paymentDate: '2026-10-03T14:00:00Z', status: 'PendingVerification', method: 'DEPOSIT', methodName: 'Depósito bancario con boleta',
  }),
  pagoHistorial({
    id: PAGO.MANDATO, paymentNumber: 'PAY-20260920-5E6F7A8B', documentNumber: 'RCP-20260920-2B2B2B2B', receiptNumber: 'RCP-20260920-2B2B2B2B',
    paymentDate: '2026-09-20T16:30:00Z', method: 'BANK_BUTTON_BCH', methodName: 'Botón Banco de Chile', amount: 4940000, taxAmount: 0, totalAmount: 4940000,
    payerTaxId: RUT_AGENCIA, payerName: 'Agencia Marítima del Pacífico', billingTaxIds: [RUT_PROPIO], payerDiffersFromBilling: true,
    blNumbers: ['HLCUVAL250200456'], bookingNumbers: [], executedBy: 'agente@maritimpacifico.cl',
    onBehalfOf: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
    items: [{
      id: 'h5000000-0000-4000-8000-000000000301', itemType: 'Freight', conceptCode: 'FREIGHT', description: 'Flete', blNumber: 'HLCUVAL250200456',
      amount: 4940000, taxAmount: 0, totalAmount: 4940000, currency: 'CLP', originalAmount: 5200, originalCurrency: 'USD', exchangeRate: 950,
      billingTaxId: RUT_PROPIO, billingName: ORGANIZACION_PRUEBA.name,
    }],
  }),
  pagoHistorial({
    id: 'h5000000-0000-4000-8000-000000000001', paymentNumber: 'PAY-CL-2026-000123', documentNumber: 'RCP-20260925-0A1B2C3D',
    receiptNumber: 'RCP-20260925-0A1B2C3D', paymentDate: '2026-09-25T13:10:00Z', currency: 'USD', amount: 2450, taxAmount: 0, totalAmount: 2450,
  }),
  pagoHistorial({
    id: 'h5000000-0000-4000-8000-000000000002', paymentNumber: 'PAY-20261004-3D2C1B0A', paymentDate: '2026-10-04T11:00:00Z', status: 'Failed',
    receiptAvailable: false,
  }),
];

function filaMonedas(conceptCode: string, conceptName: string, enabled: string[], configured: boolean): PaymentCurrencyConfig {
  return {
    country: 'CL',
    conceptCode,
    conceptName,
    configured,
    enabledCurrencies: enabled,
    rules: configured ? enabled.map((currency, i) => ({ id: `r8000000-0000-4000-8000-${conceptCode.length}${i}`.padEnd(36, '0'), currency, isEnabled: true, createdAt: '2026-10-01T12:00:00Z' })) : [],
    chileFinanceRules: true,
  };
}

const MONEDAS: PaymentCurrencyConfig[] = [
  filaMonedas('FREIGHT', 'Flete', ['USD', 'EUR', 'CLP'], true),
  filaMonedas('GATE_IN', 'Gate In', ['CLP'], true),
  filaMonedas('EDS', 'EDS', ['CLP'], true),
  filaMonedas('DEMURRAGE', 'Demurrage', ['USD', 'CLP'], true),
  filaMonedas('IPO', 'IPO', ['USD', 'CLP'], false),
];

function cambio<T>(id: string, action: string, previous: T | null, current: T | null, fecha = '2026-10-01T12:00:00Z'): MaintainerChange<T> {
  return { id, entityId: id, action, changedAt: fecha, changedBy: 'admin@hapag-lloyd.cl', changedByUserId: 'u0000000-0000-4000-8000-000000000099', previous, current };
}

function ventana(datos: Partial<PaymentBlockWindow> & Pick<PaymentBlockWindow, 'id' | 'status'>): PaymentBlockWindow {
  return {
    country: 'CL',
    startDate: '2026-10-31',
    startTime: '21:00:00',
    endDate: '2026-11-01',
    endTime: '06:00:00',
    reason: 'Cierre contable mensual',
    clientMessage: 'Los pagos estarán suspendidos por el cierre contable mensual.',
    timeZone: 'America/Santiago',
    createdAt: '2026-10-01T12:00:00Z',
    createdBy: 'admin@hapag-lloyd.cl',
    modifiedAt: null,
    modifiedBy: null,
    ...datos,
  };
}

export const VENTANA_PROGRAMADA = 'b7000000-0000-4000-8000-000000000002';

const VENTANAS: PaymentBlockWindow[] = [
  ventana({ id: VENTANA_PROGRAMADA, status: 'Scheduled', country: null, timeZone: null }),
  ventana({ id: 'b7000000-0000-4000-8000-000000000003', status: 'Scheduled', country: 'BO', startDate: '2026-12-24', startTime: '18:00:00', endDate: '2026-12-26', endTime: '08:00:00', reason: 'Feriado de Navidad', timeZone: 'America/La_Paz' }),
  ventana({ id: 'b7000000-0000-4000-8000-000000000004', status: 'Ended', startDate: '2026-09-30', startTime: '20:00:00', endDate: '2026-09-30', endTime: '23:59:00', reason: 'Cierre de septiembre' }),
];

const OPERACIONES: PaymentOperation[] = [
  {
    id: 'o9000000-0000-4000-8000-000000000001', paymentId: PAGO.CONFIRMADO, paymentNumber: 'PAY-20260912-1A2B3C4D', jobType: 'Release', status: 'Stuck',
    attempts: 5, maxAttempts: 5, nextAttemptAt: null, lastAttemptAt: '2026-10-05T14:30:00Z', lastError: 'FIS no respondió (Integration.Unavailable).',
    createdAt: '2026-10-05T14:00:00Z', completedAt: null,
  },
  {
    id: 'o9000000-0000-4000-8000-000000000002', paymentId: PAGO.MANDATO, paymentNumber: 'PAY-20260920-5E6F7A8B', jobType: 'Notify', status: 'Pending',
    attempts: 2, maxAttempts: 5, nextAttemptAt: '2026-10-05T15:10:00Z', lastAttemptAt: '2026-10-05T15:08:00Z', lastError: 'SMTP timeout',
    createdAt: '2026-10-05T15:00:00Z', completedAt: null,
  },
];

const CONCILIACION: PaymentReconciliation[] = [
  {
    paymentId: PAGO.BOLETA_EMITIDA, paymentNumber: 'PAY-20261003-9C8B7A6D', country: 'CL', status: 'PendingVerification', method: 'DEPOSIT', currency: 'CLP',
    totalAmount: 119000, createdAt: '2026-10-03T13:50:00Z', externalReference: 'PAY-20261003-9C8B7A6D', slipNumber: 'BOL-20261003-1F2E3D4C',
    payerTaxId: RUT_PROPIO, reconciliationStatus: 'NotSettled',
  },
  {
    paymentId: PAGO.CONFIRMADO, paymentNumber: 'PAY-20260912-1A2B3C4D', country: 'CL', status: 'Confirmed', method: 'KHIPU', currency: 'CLP', totalAmount: 119000,
    createdAt: '2026-09-12T14:55:00Z', confirmedAt: '2026-09-12T15:00:00Z', externalReference: 'PAY-20260912-1A2B3C4D', providerReference: 'KHP-88123',
    providerTransactionId: 'KHP-TX-99812', receiptNumber: 'RCP-20260912-5F6E7D8C', payerTaxId: RUT_PROPIO, reconciliationStatus: 'Matched',
  },
];

function cuerpo(request: Request): unknown {
  try {
    return request.postDataJSON();
  } catch {
    return null;
  }
}

async function json(route: Route, status: number, body?: unknown): Promise<void> {
  await route.fulfill({ status, contentType: 'application/json', body: body === undefined ? '' : JSON.stringify(body) });
}

async function problema(route: Route, status: number, title: string, detail: string): Promise<void> {
  await route.fulfill({ status, contentType: 'application/problem+json', body: JSON.stringify({ title, detail, status }) });
}

/** Estado del backend simulado de la Ola D para una página. */
export class SimulacionOlaD {
  private items: CartItem[];
  private siguienteItem = 100;
  private siguientePago = 1;
  private cierresSinRespuesta: number;
  private readonly porClave = new Map<string, CheckoutResult>();
  private readonly pagos: Record<string, PaymentStatusDetail> = pagosFijos();
  private readonly consultas = new Map<string, number>();

  constructor(private readonly opciones: OpcionesOlaD = {}) {
    this.cierresSinRespuesta = opciones.cierresSinRespuesta ?? 0;
    // Carro inicial con dos monedas de pago (M5-08): THC y cambio de almacén en CLP; MHD en USD.
    this.items = [
      itemCarro(PAGABLES[`LocalCharge:${ITEM.THC}`], FACTURACION_PROPIA, 'CLP', 'e7000000-0000-4000-8000-000000000001'),
      itemCarro(PAGABLES[`WarehouseChange:${ITEM.CAMBIO_ALMACEN}`], FACTURACION_PROPIA, 'CLP', 'e7000000-0000-4000-8000-000000000002'),
      itemCarro(PAGABLES[`LocalCharge:${ITEM.MHD}`], FACTURACION_PROPIA, 'USD', 'e7000000-0000-4000-8000-000000000003'),
    ];
  }

  private carro(): Cart {
    const grupos = new Map<string, CartItem[]>();
    for (const item of this.items) {
      const clave = `${item.country}|${item.paymentCurrency}`;
      grupos.set(clave, [...(grupos.get(clave) ?? []), item]);
    }
    const groups: CartGroup[] = [...grupos.entries()].map(([clave, items]) => {
      const [country, paymentCurrency] = clave.split('|');
      const libres = items.filter((i) => !i.lockedByPaymentId);
      return {
        country,
        paymentCurrency,
        subtotal: redondear(libres.reduce((s, i) => s + i.paymentAmount, 0), paymentCurrency),
        itemCount: items.length,
        lockedItemCount: items.length - libres.length,
        items,
        paymentMethods: metodosPara(country, paymentCurrency),
        block: estadoBloqueo(country, !!this.opciones.bloqueo),
      };
    });
    return {
      id: 'e6000000-0000-4000-8000-000000000001',
      organizationId: ORGANIZACION_PRUEBA.id,
      itemCount: this.items.length,
      updatedAt: '2026-10-05T14:00:00Z',
      groups,
    };
  }

  private condiciones(): CommercialConditions {
    const credito = !!this.opciones.credito;
    return {
      available: !this.opciones.nexusCaido,
      source: 'NEXUS',
      taxId: RUT_PROPIO,
      matchCode: ORGANIZACION_PRUEBA.matchCode,
      hasCredit: credito,
      creditDays: credito ? 30 : null,
      creditConcepts: credito ? ['LOCAL_CHARGES', 'MHD'] : [],
      creditValidFrom: credito ? '2026-01-01' : null,
      creditValidTo: null,
      isFreightForwarder: false,
      responsibilityLetterRequired: false,
      ipoExcluded: credito,
    };
  }

  /** Responde la ruta si es de la Ola D; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodoHttp: string, url: URL): Promise<boolean> {
    const request = route.request();

    if (ruta === 'organizations/me/commercial-conditions' && metodoHttp === 'GET') {
      await json(route, 200, this.condiciones());
      return true;
    }

    // Carro (M5-01, M5-08, M5-09, M5-04)
    if (ruta === 'cart' && metodoHttp === 'GET') {
      await json(route, 200, this.carro());
      return true;
    }
    if (ruta === 'cart' && metodoHttp === 'DELETE') {
      const country = url.searchParams.get('country');
      const currency = url.searchParams.get('paymentCurrency');
      this.items = this.items.filter((i) => !!i.lockedByPaymentId || (!!country && i.country !== country) || (!!currency && i.paymentCurrency !== currency));
      await json(route, 200, this.carro());
      return true;
    }
    if (ruta === 'cart/item-options' && metodoHttp === 'GET') {
      const clave = `${url.searchParams.get('itemType')}:${url.searchParams.get('sourceId') ?? url.searchParams.get('reference')}`;
      if (this.opciones.credito) {
        await problema(route, 400, 'Cart.CreditCustomer', 'Customers with credit pay from the account payment view.');
      } else if (this.opciones.nexusCaido) {
        await problema(route, 400, 'ChargeRules.ConditionsUnavailable', 'Nexus did not answer.');
      } else if (PAGABLES[clave]) {
        await json(route, 200, PAGABLES[clave]);
      } else {
        await problema(route, 404, 'PayableItem.NotFound', 'The item to pay was not found.');
      }
      return true;
    }
    if (ruta === 'cart/items' && metodoHttp === 'POST') {
      const body = cuerpo(request) as { itemType: string; sourceId?: string; reference?: string; billingTaxId: string; paymentCurrency?: string };
      const item = PAGABLES[`${body.itemType}:${body.sourceId ?? body.reference}`];
      const billing = item?.billingOptions.find((o) => o.taxId === body.billingTaxId.replace(/\./g, ''));
      const currency = body.paymentCurrency ?? item?.defaultPaymentCurrency;
      if (!item) {
        await problema(route, 404, 'PayableItem.NotFound', 'The item to pay was not found.');
      } else if (this.items.some((i) => i.itemType === item.itemType && i.sourceId === item.sourceId)) {
        await problema(route, 409, 'CartItem.AlreadyExists', 'The item is already in the cart.');
      } else if (!billing) {
        await problema(route, 400, 'Cart.BillingTaxIdNotAllowed', 'The billing tax ID is not enabled for this item.');
      } else if (!currency || !item.allowedCurrencies.includes(currency)) {
        await problema(route, 400, 'Cart.CurrencyNotAllowed', `Enabled currencies: ${item.allowedCurrencies.join(', ')}.`);
      } else {
        this.items.push(itemCarro(item, billing, currency, `e7000000-0000-4000-8000-${String(this.siguienteItem++).padStart(12, '0')}`));
        await json(route, 200, this.carro());
      }
      return true;
    }
    let m = ruta.match(/^cart\/items\/([^/]+)\/currency$/);
    if (m && metodoHttp === 'PUT') {
      const actual = this.items.find((i) => i.id === m?.[1]);
      const currency = (cuerpo(request) as { paymentCurrency: string }).paymentCurrency;
      if (!actual) {
        await problema(route, 404, 'CartItem.NotFound', 'Not found.');
      } else {
        const item = PAGABLES[`${actual.itemType}:${actual.sourceId}`] ?? Object.values(PAGABLES).find((p) => p.sourceId === actual.sourceId);
        const billing = item?.billingOptions.find((o) => o.taxId === actual.billingTaxId) ?? FACTURACION_PROPIA;
        if (item) this.items = this.items.map((i) => (i.id === actual.id ? itemCarro(item, billing, currency, actual.id) : i));
        await json(route, 200, this.carro());
      }
      return true;
    }
    m = ruta.match(/^cart\/items\/([^/]+)$/);
    if (m && metodoHttp === 'DELETE') {
      this.items = this.items.filter((i) => i.id !== m?.[1]);
      await json(route, 200, this.carro());
      return true;
    }
    if ((ruta === 'cart/checkout' || ruta === 'account-payments/checkout') && metodoHttp === 'POST') {
      await this.cerrar(route, request, ruta === 'account-payments/checkout');
      return true;
    }

    // Pago desde la cuenta (M5-07)
    if (ruta === 'account-payments' && metodoHttp === 'GET') {
      if (!this.opciones.credito) {
        await problema(route, 400, 'AccountPayment.NotCreditCustomer', 'Only customers with credit pay from the account payment view; use the cart.');
      } else {
        await json(route, 200, this.cuenta());
      }
      return true;
    }

    // Ciclo de vida del pago (NF-02, M5-02)
    m = ruta.match(/^payments\/([^/]+)\/(status|issue-slip|cancel)$/);
    if (m && m[1] !== 'webhook') {
      await this.ciclo(route, m[1], m[2]);
      return true;
    }
    if (ruta === 'payment-config/methods/available' && metodoHttp === 'GET') {
      await json(route, 200, metodosPara(url.searchParams.get('country') ?? 'CL', url.searchParams.get('currency') ?? 'CLP'));
      return true;
    }
    if (ruta === 'payment-blocks/status' && metodoHttp === 'GET') {
      await json(route, 200, estadoBloqueo(url.searchParams.get('country') ?? 'CL', !!this.opciones.bloqueo));
      return true;
    }

    // Facturas (M7-01)
    if (ruta === 'invoices/organizations') {
      await json(route, 200, [organizacionFacturas(), ORG_MANDANTE_FACTURAS]);
      return true;
    }
    if (ruta === 'invoices' && metodoHttp === 'GET') {
      await json(route, 200, this.facturas(url));
      return true;
    }
    if (ruta === 'invoices/download' && metodoHttp === 'POST') {
      await route.fulfill({ status: 200, contentType: 'application/zip', body: Buffer.from('PK\u0003\u0004 zip de prueba') });
      return true;
    }
    if (ruta === 'invoices/refresh' && metodoHttp === 'POST') {
      await json(route, 200, { organizationId: ORGANIZACION_PRUEBA.id, checked: 4, updated: 1, lastUpdatedAt: '2026-10-05T18:00:00Z' });
      return true;
    }
    if (/^invoices\/[^/]+\/pdf$/.test(ruta) || /^payment-history\/[^/]+\/receipt$/.test(ruta)) {
      await route.fulfill({ status: 200, contentType: 'application/pdf', body: Buffer.from('%PDF-1.4 documento de prueba') });
      return true;
    }

    // Historial de pagos (M7-02)
    if (ruta === 'payment-history') {
      const estado = url.searchParams.get('status');
      const bl = url.searchParams.get('blNumber')?.trim().toUpperCase();
      const items = HISTORIAL.filter((p) => (!estado || p.status === estado) && (!bl || p.blNumbers.some((b) => b.includes(bl))));
      await json(route, 200, { items, total: items.length, page: 1, pageSize: 20 });
      return true;
    }
    m = ruta.match(/^payment-history\/([^/]+)$/);
    if (m) {
      const pago = HISTORIAL.find((p) => p.id === m?.[1]);
      if (pago) await json(route, 200, pago);
      else await problema(route, 404, 'Payment.NotFound', 'Not found.');
      return true;
    }

    // Configuración de pagos (M5-03, M5-04) y bloqueo (M8-07)
    if (ruta.startsWith('payment-config/') || ruta === 'payment-blocks' || ruta.startsWith('payment-blocks/')) {
      await this.configuracion(route, ruta, metodoHttp);
      return true;
    }

    // Finanzas (NF-03, NF-04, M5-02)
    if (ruta.startsWith('admin/payments/')) {
      await this.finanzas(route, ruta, metodoHttp);
      return true;
    }
    return false;
  }

  /** Cierre con clave de idempotencia (NF-01) y bloqueo (M8-07). */
  private async cerrar(route: Route, request: Request, desdeCuenta: boolean): Promise<void> {
    const clave = request.headers()['idempotency-key'];
    const body = cuerpo(request) as { country?: string; paymentCurrency: string; paymentMethodCode: string };
    if (!clave) {
      await route.fulfill({ status: 400, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Validation', errors: { IdempotencyKey: ['Required'] } }) });
      return;
    }
    if (this.opciones.bloqueo) {
      await problema(route, 400, 'Payment.Blocked', MENSAJE_BLOQUEO);
      return;
    }
    const previo = this.porClave.get(clave);
    if (previo) {
      await json(route, 200, { ...previo, replayed: true });
      return;
    }
    if (this.cierresSinRespuesta > 0) {
      this.cierresSinRespuesta--;
      await route.fulfill({ status: 503, contentType: 'text/plain', body: 'Service Unavailable' });
      return;
    }
    const country = body.country ?? 'CL';
    const items = desdeCuenta ? [] : this.items.filter((i) => i.country === country && i.paymentCurrency === body.paymentCurrency && !i.lockedByPaymentId);
    const n = this.siguientePago++;
    const id = `p5000000-0000-4000-8000-${String(n).padStart(12, '0')}`;
    const numero = `PAY-20261005-${String(n).padStart(8, '0')}`;
    const deposito = body.paymentMethodCode === 'DEPOSIT';
    const total = items.reduce((s, i) => s + i.paymentAmount, 0) || 119000;
    const payment: PaymentSummary = resumenPago({
      id, paymentNumber: numero, status: deposito ? 'Pending' : 'Processing', origin: desdeCuenta ? 'Account' : 'Cart', country,
      currency: body.paymentCurrency, totalAmount: total, amount: total, taxAmount: 0, method: body.paymentMethodCode, paymentMethodCode: body.paymentMethodCode,
      slipNumber: deposito ? `BOL-20261005-${String(n).padStart(8, '0')}` : null,
    });
    this.items = this.items.map((i) => (items.includes(i) ? { ...i, lockedByPaymentId: id } : i));
    this.pagos[id] = {
      payment,
      history: historial(deposito ? ['Pending'] : ['Pending', 'Processing']),
      canCancel: deposito,
      cancelDeniedReason: deposito ? null : 'IN_PROGRESS',
      canIssueSlip: deposito,
      releasePending: false,
    };
    const resultado: CheckoutResult = {
      payment,
      nextAction: deposito ? 'IssueSlip' : 'Redirect',
      redirectUrl: deposito ? null : `/payments/${id}/result?ref=${numero}`,
      replayed: false,
    };
    this.porClave.set(clave, resultado);
    await json(route, 200, resultado);
  }

  /** Estado del pago: un pago en línea se confirma a la segunda consulta; la boleta se emite y ya no se anula. */
  private async ciclo(route: Route, id: string, accion: string): Promise<void> {
    const pago = this.pagos[id];
    if (!pago) {
      await problema(route, 404, 'Payment.NotFound', 'Not found.');
      return;
    }
    if (accion === 'status') {
      const consultas = (this.consultas.get(id) ?? 0) + 1;
      this.consultas.set(id, consultas);
      const creado = id.startsWith('p5000000-0000-4000-8000-0000000000') && !Object.values(PAGO).includes(id as never);
      if (creado && pago.payment.status === 'Processing' && consultas >= 2) {
        this.confirmar(id);
      }
      await json(route, 200, this.pagos[id]);
      return;
    }
    if (accion === 'issue-slip') {
      if (this.opciones.bloqueo) {
        await problema(route, 400, 'Payment.Blocked', MENSAJE_BLOQUEO);
        return;
      }
      this.pagos[id] = {
        ...pago,
        payment: { ...pago.payment, status: 'PendingVerification', slipIssuedAt: '2026-10-05T15:05:00Z' },
        history: [...pago.history, { fromStatus: 'Pending', toStatus: 'PendingVerification', changedAt: '2026-10-05T15:05:00Z', changedBy: 'cliente.prueba@example.com', reason: null }],
        canCancel: false,
        cancelDeniedReason: 'SLIP_ISSUED',
        canIssueSlip: false,
      };
      await json(route, 200, this.pagos[id]);
      return;
    }
    // Anulación por el cliente (M5-02): solo antes de emitir la boleta.
    if (pago.payment.status === 'PendingVerification') {
      await problema(route, 400, 'Payment.SlipAlreadyIssued', 'The deposit slip was already issued.');
      return;
    }
    if (pago.payment.status !== 'Pending') {
      await problema(route, 400, 'Payment.InProgress', 'The payment is in progress.');
      return;
    }
    this.pagos[id] = {
      ...pago,
      payment: { ...pago.payment, status: 'Cancelled' },
      history: [...pago.history, { fromStatus: 'Pending', toStatus: 'Cancelled', changedAt: '2026-10-05T15:06:00Z', changedBy: 'cliente.prueba@example.com', reason: null }],
      canCancel: false,
      cancelDeniedReason: 'FINAL',
      canIssueSlip: false,
    };
    this.items = this.items.map((i) => (i.lockedByPaymentId === id ? { ...i, lockedByPaymentId: null } : i));
    await json(route, 200, {});
  }

  private confirmar(id: string): void {
    const pago = this.pagos[id];
    this.pagos[id] = {
      ...pago,
      payment: { ...pago.payment, status: 'Confirmed', receiptNumber: 'RCP-20261005-ABCD1234', confirmedAt: '2026-10-05T15:03:00Z' },
      history: [...pago.history, { fromStatus: 'Processing', toStatus: 'Confirmed', changedAt: '2026-10-05T15:03:00Z', changedBy: 'Khipu', reason: null }],
      canCancel: false,
      cancelDeniedReason: 'FINAL',
      releasePending: true,
    };
    this.items = this.items.filter((i) => i.lockedByPaymentId !== id);
  }

  private cuenta(): AccountPayables {
    const items: PayableItem[] = [
      pagable({ itemType: 'LocalCharge', sourceId: ITEM.THC_CREDITO, conceptCode: 'THC', blNumber: 'HLCUVAP260401130', amount: 100000, taxAmount: 19000, totalAmount: 119000, allowedCurrencies: ['CLP'] }),
      pagable({ itemType: 'Freight', sourceId: ITEM.FLETE_CREDITO, conceptCode: 'FREIGHT', blNumber: 'HLCUVAP260401130', totalAmount: 3100, currency: 'USD', allowedCurrencies: ['USD', 'CLP'], defaultPaymentCurrency: 'USD' }),
      pagable({ itemType: 'Invoice', sourceId: ITEM.FACTURA_VENCIDA, conceptCode: 'INVOICE', description: 'HL-CL-2026-003987', totalAmount: 595000, allowedCurrencies: ['CLP'] }),
    ];
    return {
      organization: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
      conditions: this.condiciones(),
      items,
      totals: [
        { currency: 'CLP', amount: 600000, taxAmount: 114000, total: 714000 },
        { currency: 'USD', amount: 3100, taxAmount: 0, total: 3100 },
      ],
      evaluatedAt: '2026-10-05T18:00:00Z',
    };
  }

  private facturas(url: URL): InvoiceList {
    const propia = (url.searchParams.get('organizationId') ?? ORGANIZACION_PRUEBA.id) === ORGANIZACION_PRUEBA.id;
    const texto = (k: string) => url.searchParams.get(k)?.trim() ?? '';
    const enCarro = new Set(this.items.filter((i) => i.itemType === 'Invoice').map((i) => i.sourceId));
    const items = (propia ? FACTURAS : FACTURAS_MANDANTE)
      .filter((f) => !texto('status') || f.status === texto('status'))
      .filter((f) => !texto('currency') || f.currency === texto('currency'))
      .filter((f) => !texto('documentType') || f.documentType === texto('documentType'))
      .filter((f) => !texto('blNumber') || (f.blNumber ?? '').includes(texto('blNumber')))
      .map((f) => ({ ...f, inCart: enCarro.has(f.id) }));
    return {
      organization: propia ? organizacionFacturas() : ORG_MANDANTE_FACTURAS,
      items,
      total: items.length,
      page: 1,
      pageSize: 20,
      lastUpdatedAt: '2026-10-05T12:00:00Z',
      timeZone: 'America/Santiago',
    };
  }

  private async configuracion(route: Route, ruta: string, metodoHttp: string): Promise<void> {
    const request = route.request();
    if (ruta === 'payment-config/currencies') return json(route, 200, MONEDAS);
    let m = ruta.match(/^payment-config\/currencies\/([^/]+)\/([^/]+)\/history$/);
    if (m) {
      return json(route, 200, [
        cambio('r8100000-0000-4000-8000-000000000002', 'Updated', { country: 'CL', conceptCode: m[2], currency: 'EUR', isEnabled: true }, { country: 'CL', conceptCode: m[2], currency: 'EUR', isEnabled: false }, '2026-10-04T10:00:00Z'),
        cambio('r8100000-0000-4000-8000-000000000001', 'Created', null, { country: 'CL', conceptCode: m[2], currency: 'CLP', isEnabled: true }),
      ]);
    }
    m = ruta.match(/^payment-config\/currencies\/([^/]+)\/([^/]+)$/);
    if (m && metodoHttp === 'PUT') {
      const fila = MONEDAS.find((f) => f.conceptCode === m?.[2]) ?? MONEDAS[0];
      return json(route, 200, { ...fila, configured: true, enabledCurrencies: (cuerpo(request) as { currencies: string[] }).currencies });
    }
    if (m && metodoHttp === 'DELETE') return json(route, 204);
    if (ruta === 'payment-config/methods' && metodoHttp === 'GET') return json(route, 200, METODOS);
    if (ruta === 'payment-config/methods' && metodoHttp === 'POST') {
      return json(route, 200, { ...METODOS[0], ...(cuerpo(request) as object), id: 'm6000000-0000-4000-8000-000000000099' });
    }
    m = ruta.match(/^payment-config\/methods\/([^/]+)\/history$/);
    if (m) {
      const actual = METODOS.find((x) => x.id === m?.[1]) ?? METODOS[0];
      const { code, name, description, country, kind, providerKey, currencies, isEnabled, displayOrder } = actual;
      const snapshot = { code, name, description, country, kind, providerKey, currencies, isEnabled, displayOrder };
      return json(route, 200, [cambio(actual.id, 'Updated', { ...snapshot, providerKey: 'Khipu' }, snapshot, '2026-10-03T09:00:00Z'), cambio(actual.id, 'Created', null, snapshot)]);
    }
    m = ruta.match(/^payment-config\/methods\/([^/]+)$/);
    if (m && metodoHttp === 'PUT') return json(route, 200, { ...(METODOS.find((x) => x.id === m?.[1]) ?? METODOS[0]), ...(cuerpo(request) as object) });
    if (m && metodoHttp === 'DELETE') return json(route, 204);
    if (ruta === 'payment-blocks' && metodoHttp === 'GET') return json(route, 200, VENTANAS);
    if (ruta === 'payment-blocks' && metodoHttp === 'POST') {
      return json(route, 200, ventana({ ...(cuerpo(request) as object), id: 'b7000000-0000-4000-8000-000000000009', status: 'Scheduled' }));
    }
    m = ruta.match(/^payment-blocks\/([^/]+)\/history$/);
    if (m) {
      const v = VENTANAS.find((x) => x.id === m?.[1]) ?? VENTANAS[0];
      const snapshot = { country: v.country, startDate: v.startDate, startTime: v.startTime, endDate: v.endDate, endTime: v.endTime, reason: v.reason, clientMessage: v.clientMessage, isActive: true };
      return json(route, 200, [
        cambio(v.id, 'Updated', { ...snapshot, endTime: '04:00:00' }, snapshot, '2026-10-02T09:00:00Z'),
        cambio(v.id, 'Created', null, { ...snapshot, endTime: '04:00:00' }),
      ]);
    }
    m = ruta.match(/^payment-blocks\/([^/]+)$/);
    if (m && metodoHttp === 'PUT') {
      return json(route, 200, { ...(VENTANAS.find((x) => x.id === m?.[1]) ?? VENTANAS[0]), ...(cuerpo(request) as object), modifiedAt: '2026-10-05T16:00:00Z', modifiedBy: 'admin@hapag-lloyd.cl' });
    }
    if (m && metodoHttp === 'DELETE') return json(route, 204);
    return json(route, 200, []);
  }

  private async finanzas(route: Route, ruta: string, metodoHttp: string): Promise<void> {
    const request = route.request();
    if (ruta === 'admin/payments/operations') return json(route, 200, OPERACIONES);
    let m = ruta.match(/^admin\/payments\/operations\/([^/]+)\/retry$/);
    if (m && metodoHttp === 'POST') {
      const op = OPERACIONES.find((o) => o.id === m?.[1]) ?? OPERACIONES[0];
      return json(route, 200, { ...op, status: 'Succeeded', attempts: 1, completedAt: '2026-10-05T16:00:00Z', lastError: null });
    }
    if (ruta === 'admin/payments/reconciliation') return json(route, 200, CONCILIACION);
    m = ruta.match(/^admin\/payments\/([^/]+)\/cancel$/);
    if (m && metodoHttp === 'POST') {
      const reason = (cuerpo(request) as { reason?: string })?.reason;
      if (!reason) return problema(route, 400, 'Validation', 'Reason is required.');
      const base = this.pagos[m[1]] ?? pagosFijos()[PAGO.BOLETA_EMITIDA];
      return json(route, 200, { ...base, payment: { ...base.payment, status: 'Cancelled' }, canCancel: false, cancelDeniedReason: 'FINAL' });
    }
    return json(route, 200, []);
  }
}
