import { Request, Route } from '@playwright/test';
import type {
  AccountCheckoutItem,
  AccountCheckoutResult,
  AccountStatement,
  ChargeSettlement,
  CreditImputation,
  CreditImputationRule,
  CreditImputationRuleSnapshot,
  StatementLine,
  StatementSummary,
} from '../../src/app/core/models/account-statement.model';
import type { PaymentStatusDetail, PaymentSummary } from '../../src/app/core/models/cart.model';
import type { ShipmentCharges } from '../../src/app/core/models/charges.model';
import type { DepositProof, DepositProofQueueItem, PaymentDepositProofs } from '../../src/app/core/models/deposit-proof.model';
import type { Invoice } from '../../src/app/core/models/invoice.model';
import type { MaintainerChange } from '../../src/app/core/models/payment-config.model';
import type {
  InvoiceReissue,
  ReinvoicingAcceptanceView,
  ReinvoicingDetail,
  ReinvoicingQuote,
} from '../../src/app/core/models/reinvoicing.model';
import type { ServiceBillingData, ServiceDefinitionOption, ServiceRequestDetail, ServiceRequestStatus } from '../../src/app/core/models/service-request.model';
import { ORGANIZACION_PRUEBA, USUARIO_ADMIN, USUARIO_PRUEBA } from './session';
import { ITEM, OpcionesOlaD, PAGO, RUT_MANDANTE, RUT_PROPIO, SimulacionOlaD } from './ola-d-mocks';
import { dashboard } from './ola-f-mocks';

/**
 * Backend simulado de la Ola H (Fase 2): estado de cuenta en línea (M7-03) con selección múltiple al carro y forma de
 * pago por ítem con crédito (M5-10), comprobantes de depósito con la bandeja de Finanzas (M5-06), anticipos y su cruce
 * (M3-19, NF-04), mantenedor de conceptos imputables a crédito (NF-15), refacturación IAO con la página pública de
 * aceptación (M3-11) y los ajustes de la Ola G (definiciones para los filtros y solicitudes en el dashboard). Guarda estado
 * por página; agrega al carro de la Ola D los ítems elegidos y registra las facturas cubiertas y refacturadas.
 */

const AHORA = '2026-10-06T12:00:00Z';
const HOY = '2026-10-06';
const ZONA = 'America/Santiago';
const PDF = '%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF';

/** Facturas y cargos del estado de cuenta de la organización de prueba. */
export const ESTADO = {
  FACTURA_POR_VENCER: 'i4000000-0000-4000-8000-000000004310',
  FACTURA_CUBIERTA: 'i4000000-0000-4000-8000-000000004318',
  FACTURA_REEMPLAZADA: 'i4000000-0000-4000-8000-000000004198',
  FACTURA_NUEVA: 'i4000000-0000-4000-8000-000000004402',
  GATE_OUT: 'c3000000-0000-4000-8000-000000000031',
  ISPS_IMPUTADO: 'c3000000-0000-4000-8000-000000000032',
} as const;

/** BL de exportación con el Gate Out pagado por adelantado y su factura cubierta (M3-19). */
export const BL_GATE_OUT_ANTICIPADO = 'HLCUSAI260701810';
export const RECIBO_ANTICIPO = 'RGO-20261001-1A2B3C4D';
/** BL de exportación antes del zarpe con el Gate Out por pagar (pago anticipado por la agencia de aduanas, M3-19). */
export const BL_GATE_OUT_POR_PAGAR = 'HLCUSAI260801820';

/** Pagos con boleta de depósito para los comprobantes (M5-06). */
export const PAGO_DEPOSITO = {
  /** Resultado del pago: comprobante rechazado, espera uno nuevo. */
  RECHAZADO: PAGO.BOLETA_EMITIDA,
  /** Historial: boleta emitida sin comprobante aún. */
  SIN_COMPROBANTE: 'h5000000-0000-4000-8000-000000000004',
  /** De otra organización: comprobante en revisión en la bandeja de Finanzas. */
  EN_REVISION: 'p5000000-0000-4000-8000-000000000106',
} as const;

/** Refacturación IAO sembrada (pendiente de pago y de aceptación) y su enlace de demostración. */
export const REFACTURACION = 's9000000-0000-4000-8000-000000000009';
/** Refacturación en borrador, sin la aprobación adjunta. */
export const REFACTURACION_BORRADOR = 's9000000-0000-4000-8000-000000000010';
export const TOKEN_ACEPTACION = 'IAO-DEMO-ACCEPT-2026-10';
export const NUEVA_RAZON = { name: 'Comercial Austral SpA', taxId: '77888999-1' };

const ORG_MANDANTE_ID = 'c1000000-0000-4000-8000-000000000010';

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

function normalizarRut(valor: string | null | undefined): string {
  return (valor ?? '').replace(/[.\s-]/g, '').toUpperCase();
}

// ---------------------------------------------------------------------------
// Estado de cuenta (M7-03)
// ---------------------------------------------------------------------------

function linea(datos: Partial<StatementLine> & Pick<StatementLine, 'key' | 'kind' | 'documentType' | 'status' | 'itemType' | 'sourceId' | 'number' | 'conceptCode' | 'totalAmount'>): StatementLine {
  return {
    dueSoon: false,
    conceptName: datos.conceptCode,
    blNumber: 'HLCU0000001',
    bookingNumber: 'BKG26030010',
    legalName: ORGANIZACION_PRUEBA.name,
    taxId: RUT_PROPIO,
    referenceDate: '2026-09-15',
    amount: datos.totalAmount,
    taxAmount: 0,
    balance: datos.totalAmount,
    currency: 'CLP',
    payable: true,
    inCart: false,
    inPayment: false,
    canImputeToCredit: false,
    ...datos,
  };
}

function lineasIniciales(credito: boolean): StatementLine[] {
  const lineas: StatementLine[] = [
    linea({
      key: `Invoice:${ITEM.FACTURA_VENCIDA}`, kind: 'Invoiced', documentType: 'Invoice', status: 'Overdue', itemType: 'Invoice',
      sourceId: ITEM.FACTURA_VENCIDA, number: '100245', siiNumber: '100245', sourceNumber: 'HL-CL-2026-003987', conceptCode: 'INVOICE',
      conceptName: 'Factura', issueDate: '2026-09-15', dueDate: '2026-09-30', daysOverdue: 6, agingBucket: '1-30',
      amount: 500000, taxAmount: 95000, totalAmount: 595000,
    }),
    linea({
      key: `Invoice:${ESTADO.FACTURA_POR_VENCER}`, kind: 'Invoiced', documentType: 'Invoice', status: 'Pending', itemType: 'Invoice',
      sourceId: ESTADO.FACTURA_POR_VENCER, number: '100310', siiNumber: '100310', sourceNumber: 'HL-CL-2026-004310', conceptCode: 'INVOICE',
      conceptName: 'Factura', blNumber: 'HLCUVAP260601930', bookingNumber: null, issueDate: '2026-09-26', referenceDate: '2026-09-26',
      dueDate: '2026-10-10', dueSoon: true, agingBucket: 'CURRENT', amount: 200000, taxAmount: 38000, totalAmount: 238000,
    }),
    linea({
      key: `Invoice:${ITEM.FACTURA_SIN_FOLIO}`, kind: 'Invoiced', documentType: 'ExemptInvoice', status: 'Pending', itemType: 'Invoice',
      sourceId: ITEM.FACTURA_SIN_FOLIO, number: 'HL-CL-2026-004601', sourceNumber: 'HL-CL-2026-004601', conceptCode: 'INVOICE',
      conceptName: 'Factura', issueDate: '2026-10-02', referenceDate: '2026-10-02', dueDate: '2026-11-01', agingBucket: 'CURRENT',
      totalAmount: 1250, currency: 'USD',
    }),
    linea({
      key: `Invoice:${ESTADO.FACTURA_CUBIERTA}`, kind: 'Invoiced', documentType: 'Invoice', status: 'Covered', itemType: 'Invoice',
      sourceId: ESTADO.FACTURA_CUBIERTA, number: '100318', siiNumber: '100318', sourceNumber: 'HL-CL-2026-004318', conceptCode: 'INVOICE',
      conceptName: 'Factura', blNumber: BL_GATE_OUT_ANTICIPADO, bookingNumber: 'HLCUBKG2607181', issueDate: '2026-10-04',
      referenceDate: '2026-10-04', dueDate: '2026-11-03', amount: 50000, taxAmount: 9500, totalAmount: 59500, balance: 0, payable: false,
      coveredBy: {
        kind: 'Advance', paymentId: 'p5000000-0000-4000-8000-000000000201', paymentNumber: 'PAY-20261001-D4E5F6A7',
        receiptNumber: 'RCP-20261001-E8F9A0B1', receiptDocumentId: 'd6000000-0000-4000-8000-000000000201', receiptDocumentNumber: RECIBO_ANTICIPO,
        amount: 59500, currency: 'CLP', settledAt: '2026-10-01T15:00:00Z', matchedAt: '2026-10-04T12:00:00Z',
      },
    }),
    linea({
      key: `LocalCharge:${credito ? ITEM.THC_CREDITO : ITEM.THC}`, kind: 'Uninvoiced', documentType: 'LocalCharge', status: 'Uninvoiced',
      itemType: 'LocalCharge', sourceId: credito ? ITEM.THC_CREDITO : ITEM.THC, number: 'THC', conceptCode: 'THC', conceptName: 'THC',
      blNumber: credito ? 'HLCUVAP260401130' : 'HLCU0000001', referenceDate: '2026-10-01', amount: 100000, taxAmount: 19000, totalAmount: 119000,
      canImputeToCredit: credito,
    }),
    linea({
      key: `LocalCharge:${ESTADO.GATE_OUT}`, kind: 'Uninvoiced', documentType: 'LocalCharge', status: 'Uninvoiced', itemType: 'LocalCharge',
      sourceId: ESTADO.GATE_OUT, number: 'GATE_OUT', conceptCode: 'GATE_OUT', conceptName: 'Gate Out', blNumber: 'HLCUVAP260601930',
      bookingNumber: null, referenceDate: '2026-10-03', amount: 50000, taxAmount: 9500, totalAmount: 59500, canImputeToCredit: credito,
    }),
    linea({
      key: `Demurrage:${ITEM.LINEA_DEMURRAGE}`, kind: 'Uninvoiced', documentType: 'Demurrage', status: 'Uninvoiced', itemType: 'Demurrage',
      sourceId: ITEM.LINEA_DEMURRAGE, number: 'DEMURRAGE', conceptCode: 'DEMURRAGE', conceptName: 'Demurrage', blNumber: 'HLCUVAL250100123',
      bookingNumber: null, description: 'HLXU3034002', referenceDate: '2026-10-05', totalAmount: 175000,
    }),
    linea({
      key: 'LocalCharge:c9000000-0000-4000-8000-000000000002', kind: 'Uninvoiced', documentType: 'ServiceCharge', status: 'Uninvoiced',
      itemType: 'LocalCharge', sourceId: 'c9000000-0000-4000-8000-000000000002', number: 'SRV-20261005-5E1A0002', conceptCode: 'SEAL_MANAGEMENT',
      conceptName: 'Gestión de sellos', blNumber: 'HLCUSAI260300610', bookingNumber: 'HLCUBKG2603061', serviceRequestNumber: 'SRV-20261005-5E1A0002',
      referenceDate: '2026-10-05', amount: 30000, taxAmount: 5700, totalAmount: 35700,
    }),
  ];
  if (credito) {
    lineas.push(linea({
      key: `LocalCharge:${ESTADO.ISPS_IMPUTADO}`, kind: 'CreditImputed', documentType: 'LocalCharge', status: 'CreditImputed',
      itemType: 'LocalCharge', sourceId: ESTADO.ISPS_IMPUTADO, number: 'ISPS', conceptCode: 'ISPS', conceptName: 'ISPS',
      blNumber: 'HLCUVAP260601930', bookingNumber: null, creditImputationNumber: 'CRI-20261003-C9D8E7F6', referenceDate: '2026-10-03',
      amount: 20000, taxAmount: 3800, totalAmount: 23800, payable: false,
    }));
  }
  return lineas;
}

function anticipo(datos: Partial<ChargeSettlement> & Pick<ChargeSettlement, 'id' | 'kind' | 'status' | 'paymentNumber' | 'conceptCode' | 'amount'>): ChargeSettlement {
  return {
    paymentId: 'p5000000-0000-4000-8000-000000000202',
    receiptNumber: null,
    payerOrganizationId: ORGANIZACION_PRUEBA.id,
    payerTaxId: RUT_PROPIO,
    payerName: ORGANIZACION_PRUEBA.name,
    billingTaxId: RUT_PROPIO,
    billingName: ORGANIZACION_PRUEBA.name,
    blNumber: 'HLCUVAP260601930',
    country: 'CL',
    itemType: 'LocalCharge',
    sourceId: 'c3000000-0000-4000-8000-000000000033',
    currency: 'CLP',
    paidAmount: datos.amount,
    paidCurrency: 'CLP',
    settledAt: '2026-10-02T14:00:00Z',
    ...datos,
  };
}

function anticiposIniciales(): ChargeSettlement[] {
  return [
    anticipo({
      id: 'x9000000-0000-4000-8000-000000000001', kind: 'Advance', status: 'Open', paymentNumber: 'PAY-20261002-A1C2E3F4',
      receiptNumber: 'RCP-20261002-B2C3D4E5', conceptCode: 'BL_FEE', description: 'BL Fee', amount: 41650,
    }),
    anticipo({
      id: 'x9000000-0000-4000-8000-000000000002', kind: 'Advance', status: 'Matched', paymentId: 'p5000000-0000-4000-8000-000000000201',
      paymentNumber: 'PAY-20261001-D4E5F6A7', receiptNumber: 'RCP-20261001-E8F9A0B1', payerOrganizationId: 'c1000000-0000-4000-8000-000000000020',
      payerTaxId: '96555444-3', payerName: 'Agencia Marítima del Pacífico', blNumber: BL_GATE_OUT_ANTICIPADO, bookingNumber: 'HLCUBKG2607181',
      conceptCode: 'GATE_OUT', amount: 59500, settledAt: '2026-10-01T15:00:00Z', receiptDocumentId: 'd6000000-0000-4000-8000-000000000201',
      receiptDocumentNumber: RECIBO_ANTICIPO, matchedInvoiceId: ESTADO.FACTURA_CUBIERTA, matchedInvoiceNumber: '100318',
      matchedAt: '2026-10-04T12:00:00Z', matchedBy: 'SYSTEM',
    }),
  ];
}

function imputacionSembrada(): ChargeSettlement {
  return anticipo({
    id: 'x9000000-0000-4000-8000-000000000003', kind: 'CreditImputation', status: 'Open', paymentId: 'q9000000-0000-4000-8000-000000000001',
    paymentNumber: 'CRI-20261003-C9D8E7F6', conceptCode: 'ISPS', amount: 23800, sourceId: ESTADO.ISPS_IMPUTADO, settledAt: '2026-10-03T13:00:00Z',
  });
}

/** Resumen y antigüedad calculados sobre toda la cuenta (sin filtros). */
function resumen(lineas: StatementLine[], anticipos: ChargeSettlement[]): StatementSummary[] {
  const monedas = [...new Set(lineas.map((l) => l.currency))];
  return monedas.map((currency) => {
    const de = lineas.filter((l) => l.currency === currency);
    const sumar = (xs: StatementLine[]) => xs.reduce((s, l) => s + l.balance, 0);
    const facturadas = de.filter((l) => l.kind === 'Invoiced');
    const overdue = sumar(facturadas.filter((l) => l.status === 'Overdue'));
    const invoicedBalance = sumar(facturadas);
    const uninvoiced = sumar(de.filter((l) => l.kind === 'Uninvoiced'));
    const creditImputed = sumar(de.filter((l) => l.kind === 'CreditImputed'));
    return {
      currency,
      totalBalance: invoicedBalance + uninvoiced + creditImputed,
      invoicedBalance,
      overdue,
      dueSoon: sumar(facturadas.filter((l) => l.dueSoon)),
      notYetDue: invoicedBalance - overdue,
      uninvoiced,
      creditImputed,
      advancesUnapplied: anticipos.filter((a) => a.kind === 'Advance' && a.status === 'Open' && a.currency === currency).reduce((s, a) => s + a.amount, 0),
    };
  });
}

const TRAMOS = ['CURRENT', '1-30', '31-60', '61-90', '91+'];

function antiguedad(lineas: StatementLine[]): AccountStatement['aging'] {
  const abiertas = lineas.filter((l) => l.kind === 'Invoiced' && l.balance > 0);
  const monedas = [...new Set(abiertas.map((l) => l.currency))];
  return {
    buckets: [
      { code: 'CURRENT', toDays: 0 },
      { code: '1-30', fromDays: 1, toDays: 30 },
      { code: '31-60', fromDays: 31, toDays: 60 },
      { code: '61-90', fromDays: 61, toDays: 90 },
      { code: '91+', fromDays: 91 },
    ],
    rows: monedas.map((currency) => {
      const amounts = TRAMOS.map((t) => abiertas.filter((l) => l.currency === currency && (l.agingBucket ?? 'CURRENT') === t).reduce((s, l) => s + l.balance, 0));
      return { currency, amounts, total: amounts.reduce((s, a) => s + a, 0) };
    }),
  };
}

function filtrar(lineas: StatementLine[], url: URL): StatementLine[] {
  const p = (k: string) => url.searchParams.get(k)?.trim() ?? '';
  return lineas
    .filter((l) => !p('blNumber') || (l.blNumber ?? '').includes(p('blNumber').toUpperCase()))
    .filter((l) => !p('bookingNumber') || (l.bookingNumber ?? '').includes(p('bookingNumber').toUpperCase()))
    .filter((l) => !p('currency') || l.currency === p('currency'))
    .filter((l) => !p('documentType') || l.documentType === p('documentType'))
    .filter((l) => !p('status') || (p('status') === 'DueSoon' ? l.dueSoon : l.status === p('status')))
    .filter((l) => !p('from') || l.referenceDate >= p('from'))
    .filter((l) => !p('to') || l.referenceDate <= p('to'));
}

// ---------------------------------------------------------------------------
// Comprobantes de depósito (M5-06)
// ---------------------------------------------------------------------------

interface EstadoDeposito {
  paymentNumber: string;
  slipNumber: string;
  payerName: string;
  payerTaxId: string;
  totalAmount: number;
  blNumbers: string[];
  proofs: DepositProof[];
  status: PaymentSummary['status'];
  receiptNumber: string | null;
}

function comprobante(paymentId: string, datos: Partial<DepositProof> & Pick<DepositProof, 'id' | 'status' | 'fileName'>): DepositProof {
  return {
    paymentId,
    contentType: 'application/pdf',
    sizeBytes: 182_000,
    contentHash: 'b5bb9d8014a0f9b1d61e21e796d78dccdf1352f23cd32812f4850b878ae4944c',
    uploadedAt: '2026-10-03T15:00:00Z',
    uploadedBy: USUARIO_PRUEBA.email,
    downloadPath: `/api/v1/payments/${paymentId}/deposit-proofs/${datos.id}/file`,
    ...datos,
  };
}

function depositosIniciales(): Record<string, EstadoDeposito> {
  return {
    [PAGO_DEPOSITO.RECHAZADO]: {
      paymentNumber: 'PAY-20261003-9C8B7A6D', slipNumber: 'BOL-20261003-1F2E3D4C', payerName: ORGANIZACION_PRUEBA.name, payerTaxId: RUT_PROPIO,
      totalAmount: 119000, blNumbers: ['HLCU0000001'], status: 'PendingVerification', receiptNumber: null,
      proofs: [
        comprobante(PAGO_DEPOSITO.RECHAZADO, {
          id: 'y9000000-0000-4000-8000-000000000001', status: 'Rejected', fileName: 'deposito-bci-0310.pdf', bankName: 'BCI',
          bankReference: 'BCI-778812', depositDate: '2026-10-03', depositAmount: 110000, reviewedAt: '2026-10-04T13:00:00Z',
          reviewedBy: USUARIO_ADMIN.email, rejectionReason: 'El monto depositado (CLP 110.000) no coincide con el total de la boleta.',
        }),
      ],
    },
    [PAGO_DEPOSITO.SIN_COMPROBANTE]: {
      paymentNumber: 'PAY-20261003-9C8B7A6D', slipNumber: 'BOL-20261003-1F2E3D4C', payerName: ORGANIZACION_PRUEBA.name, payerTaxId: RUT_PROPIO,
      totalAmount: 119000, blNumbers: ['HLCU0000001'], status: 'PendingVerification', receiptNumber: null, proofs: [],
    },
    [PAGO_DEPOSITO.EN_REVISION]: {
      paymentNumber: 'PAY-20261004-AB12CD34', slipNumber: 'BDP-20261004-5C7D9E1F', payerName: 'Comercial Mandante SpA', payerTaxId: RUT_MANDANTE,
      totalAmount: 428400, blNumbers: ['HLCUVAL250100123'], status: 'PendingVerification', receiptNumber: null,
      proofs: [
        comprobante(PAGO_DEPOSITO.EN_REVISION, {
          id: 'y9000000-0000-4000-8000-000000000002', status: 'Submitted', fileName: 'transferencia-santander.pdf', bankName: 'Santander',
          bankReference: 'STD-0045521', depositDate: '2026-10-04', depositAmount: 428400, uploadedAt: '2026-10-04T16:20:00Z',
          uploadedBy: 'pagos@comercialmandante.cl',
        }),
      ],
    },
  };
}

function resumenDeposito(id: string, e: EstadoDeposito): PaymentStatusDetail {
  const payment: PaymentSummary = {
    id, paymentNumber: e.paymentNumber, status: e.status, origin: 'Cart', country: 'CL', currency: 'CLP', amount: e.totalAmount, taxAmount: 0,
    totalAmount: e.totalAmount, method: 'DEPOSIT', paymentMethodCode: 'DEPOSIT', slipNumber: e.slipNumber, slipIssuedAt: '2026-10-03T14:00:00Z',
    receiptNumber: e.receiptNumber, createdAt: '2026-10-03T13:50:00Z', items: [],
  };
  return {
    payment, history: [], canCancel: false, cancelDeniedReason: e.status === 'Confirmed' ? 'FINAL' : 'SLIP_ISSUED', canIssueSlip: false,
    releasePending: false,
  };
}

// ---------------------------------------------------------------------------
// Conceptos imputables a crédito (M5-10, NF-15)
// ---------------------------------------------------------------------------

function regla(n: number, conceptCode: string, conceptName: string): CreditImputationRule {
  return {
    id: `z9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, country: 'CL', conceptCode, conceptName, nexusCreditConcept: 'LOCAL_CHARGES',
    isEnabled: true, notes: 'Regla inicial conservadora: pendiente de confirmación de Finanzas.', createdAt: '2026-10-05T12:00:00Z', createdBy: 'SYSTEM',
  };
}

function instantaneaRegla(r: CreditImputationRule): CreditImputationRuleSnapshot {
  return { country: r.country, conceptCode: r.conceptCode, nexusCreditConcept: r.nexusCreditConcept, isEnabled: r.isEnabled, notes: r.notes ?? null };
}

// ---------------------------------------------------------------------------
// Refacturación IAO (M3-11)
// ---------------------------------------------------------------------------

interface Refacturacion {
  detalle: ReinvoicingDetail;
  token: string | null;
}

function solicitudIao(id: string, numero: string, status: ServiceRequestStatus, billing: ServiceBillingData): ServiceRequestDetail {
  return {
    id, requestNumber: numero, definitionId: 'd9000000-0000-4000-8000-000000000099', definitionCode: 'IAO_REINVOICING',
    nameEs: 'Refacturación IAO', nameEn: 'IAO re-invoicing', organizationId: ORGANIZACION_PRUEBA.id, organizationName: ORGANIZACION_PRUEBA.name,
    requestedByEmail: USUARIO_PRUEBA.email, billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000001', blNumber: 'HLCU0000001',
    bookingNumber: 'BKG26030010', country: 'CL', operation: 'IMPORT', timeZone: ZONA, containerNumbers: [], inputSchema: [], inputValues: {},
    billing, billingDataRequired: true, tariffAcceptanceRequired: true, status, statusChangedAt: AHORA, approvalTeam: 'None',
    fulfillmentTeam: 'None', requiresOutputDocument: false, createdAt: AHORA, charges: [], canEdit: status === 'Draft',
    canSubmit: status === 'Draft', canCancel: status === 'Draft' || status === 'PendingPayment', attachments: [],
    timeline: [{ id: `t${id.slice(1)}`, fromStatus: null, toStatus: 'Draft', occurredAt: AHORA, actorName: USUARIO_PRUEBA.email, actorKind: 'Client' }],
  };
}

function reemision(id: string, datos: Partial<InvoiceReissue>): InvoiceReissue {
  return {
    id, originalInvoiceId: ITEM.FACTURA_VENCIDA, originalSiiNumber: '100245', originalSourceNumber: 'HL-CL-2026-003987',
    originalLegalName: ORGANIZACION_PRUEBA.name, originalTaxId: RUT_PROPIO, vatLossAmount: 95000, feeAmount: 25000, feeTaxAmount: 4750,
    currency: 'CLP', acceptorEmail: 'facturacion@comercialaustral.cl', approvalAttached: false, canResendAcceptance: false, ...datos,
  };
}

function refacturacionSembrada(): Refacturacion {
  const billing: ServiceBillingData = {
    taxId: NUEVA_RAZON.taxId, name: NUEVA_RAZON.name, address: 'Av. Matta 1200, Santiago', email: 'facturacion@comercialaustral.cl',
    activity: 'Comercio al por mayor',
  };
  const request = solicitudIao(REFACTURACION, 'SRV-20261005-5E1A0009', 'PendingPayment', billing);
  request.createdAt = '2026-10-05T14:00:00Z';
  request.submittedAt = '2026-10-05T14:20:00Z';
  request.tariffAcceptedAt = '2026-10-05T14:20:00Z';
  request.statusChangedAt = '2026-10-05T14:20:00Z';
  request.canEdit = false;
  request.canSubmit = false;
  request.canCancel = true;
  request.charges = [
    { chargeId: 'c9000000-0000-4000-8000-000000000091', itemType: 'LocalCharge', conceptCode: 'REINVOICING', amount: 25000, taxAmount: 4750, totalAmount: 29750, currency: 'CLP', status: 'Pending', generated: true },
    { chargeId: 'c9000000-0000-4000-8000-000000000092', itemType: 'LocalCharge', conceptCode: 'VAT_LOSS', amount: 22800, taxAmount: 0, totalAmount: 22800, currency: 'CLP', status: 'Pending', generated: true },
  ];
  request.attachments = [{
    id: 'a9000000-0000-4000-8000-000000000091', fieldKey: 'newCompanyApproval', fileName: 'aprobacion-comercial-austral.pdf', contentType: 'application/pdf',
    sizeBytes: 98_000, uploadedAt: '2026-10-05T14:10:00Z', uploadedBy: USUARIO_PRUEBA.email,
  }];
  request.timeline = [
    { id: 't9000000-0000-4000-8000-000000000091', fromStatus: null, toStatus: 'Draft', occurredAt: '2026-10-05T14:00:00Z', actorName: USUARIO_PRUEBA.email, actorKind: 'Client' },
    { id: 't9000000-0000-4000-8000-000000000092', fromStatus: 'Draft', toStatus: 'Submitted', occurredAt: '2026-10-05T14:20:00Z', actorName: USUARIO_PRUEBA.email, actorKind: 'Client' },
    { id: 't9000000-0000-4000-8000-000000000093', fromStatus: 'Submitted', toStatus: 'PendingPayment', occurredAt: '2026-10-05T14:20:01Z', actorName: 'SYSTEM', actorKind: 'System', notes: 'Enlace de aceptación enviado a facturacion@comercialaustral.cl.' },
  ];
  return {
    token: TOKEN_ACEPTACION,
    detalle: {
      request,
      reissue: reemision('r9000000-0000-4000-8000-000000000009', {
        originalInvoiceId: ESTADO.FACTURA_REEMPLAZADA, originalSiiNumber: '100198', originalSourceNumber: 'HL-CL-2026-004198',
        newLegalName: NUEVA_RAZON.name, newTaxId: NUEVA_RAZON.taxId, vatLossAmount: 22800, acceptanceStatus: 'Pending',
        acceptanceRequestedAt: '2026-10-05T14:20:00Z', acceptanceExpiresAt: '2026-10-20T14:20:00Z', approvalAttached: true, canResendAcceptance: true,
      }),
    },
  };
}

function borradorSembrado(): Refacturacion {
  const billing: ServiceBillingData = {
    taxId: '78111222-3', name: 'Distribuidora Norte Ltda.', address: 'Av. Grecia 800, Antofagasta', email: 'pagos@distnorte.cl', activity: null,
  };
  return {
    token: null,
    detalle: {
      request: solicitudIao(REFACTURACION_BORRADOR, 'SRV-20261006-5E1A0010', 'Draft', billing),
      reissue: reemision('r9000000-0000-4000-8000-000000000010', {
        newLegalName: billing.name, newTaxId: billing.taxId, acceptorEmail: 'pagos@distnorte.cl',
      }),
    },
  };
}

/** Definiciones activas para los filtros (GET /service-requests/definitions). */
const DEFINICIONES: ServiceDefinitionOption[] = [
  ['SEAL_MANAGEMENT', 'Gestión de sellos', 'Seal management'],
  ['LATE_ARRIVAL', 'Late Arrival', 'Late Arrival'],
  ['EARLY_ARRIVAL', 'Early', 'Early arrival'],
  ['DROP_OFF_SCL', 'Drop Off SCL', 'Drop Off SCL'],
  ['XOM', 'Administración de contenedor (XOM)', 'Container administration (XOM)'],
  ['BL_CORRECTION', 'Corrección o aclaración de BL', 'BL correction or clarification'],
  ['BL_HOUSE_TRANSMISSION', 'Transmisión de BL hijo', 'House BL transmission'],
  ['MATRIX_LATE', 'Matriz fuera de plazo', 'Late matrix submission'],
  ['GATE_IN_RETURN', 'Gate In por devolución de unidades', 'Gate In for unit return'],
  ['IAO_REINVOICING', 'Refacturación IAO', 'IAO re-invoicing'],
].map(([code, nameEs, nameEn]): ServiceDefinitionOption => ({
  code, nameEs, nameEn, operations: ['IMPORT', 'EXPORT'], countries: ['CL'], referenceType: 'BL', dedicatedFlow: code === 'IAO_REINVOICING',
}));

/** Cargos del BL de exportación con el Gate Out por pagar (M3-19). */
function cargosGateOut(): ShipmentCharges {
  return {
    blId: '3f0c2a1e-0000-4000-8000-000260801820', blNumber: BL_GATE_OUT_POR_PAGAR, country: 'CL', shipmentType: 'Export', timeZone: ZONA,
    payer: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
    conditions: {
      available: true, source: 'NEXUS', taxId: RUT_PROPIO, hasCredit: false, creditConcepts: [], isFreightForwarder: false,
      responsibilityLetterRequired: false, ipoExcluded: false,
    },
    exemptionFigures: [],
    charges: [{
      chargeId: 'c3000000-0000-4000-8000-000000000041', conceptCode: 'GATE_OUT', conceptName: 'Gate Out', category: 'LocalCharge', amount: 50000,
      taxAmount: 9500, totalAmount: 59500, currency: 'CLP', status: 'Pending', outcome: 'Payable', payableAmount: 50000, payableTaxAmount: 9500,
      payableTotal: 59500, action: 'AddToCart',
    }],
    payableTotals: [{ currency: 'CLP', amount: 50000, taxAmount: 9500, total: 59500 }],
    allApplicableExempt: false, requiresPayment: true, rulesAvailable: true, requirements: [], canProceed: true, evaluatedAt: AHORA,
  };
}

/** Estado del backend simulado de la Ola H para una página. */
export class SimulacionOlaH {
  private readonly credito: boolean;
  private lineas: StatementLine[];
  private readonly anticipos: ChargeSettlement[] = anticiposIniciales();
  private readonly porClave = new Map<string, AccountCheckoutResult>();
  private readonly depositos: Record<string, EstadoDeposito> = depositosIniciales();
  private reglas: CreditImputationRule[] = [regla(1, 'THC', 'THC'), regla(2, 'ISPS', 'ISPS'), regla(3, 'BL_FEE', 'BL Fee'), regla(4, 'GATE_OUT', 'Gate Out')];
  private readonly historialReglas = new Map<string, MaintainerChange<CreditImputationRuleSnapshot>[]>();
  private readonly refacturaciones: Refacturacion[] = [refacturacionSembrada(), borradorSembrado()];
  private siguiente = 20;

  constructor(private readonly olaD: SimulacionOlaD, opciones: OpcionesOlaD = {}) {
    this.credito = !!opciones.credito;
    this.lineas = lineasIniciales(this.credito);
    if (this.credito) this.anticipos.push(imputacionSembrada());
    for (const r of this.reglas) {
      this.historialReglas.set(r.id, [
        {
          id: `h${r.id.slice(1)}`, entityId: r.id, action: 'Created', changedAt: r.createdAt, changedBy: 'SYSTEM', previous: null,
          current: { ...instantaneaRegla(r), isEnabled: false },
        },
        {
          id: `g${r.id.slice(1)}`, entityId: r.id, action: 'Updated', changedAt: '2026-10-05T15:00:00Z', changedBy: USUARIO_ADMIN.email,
          previous: { ...instantaneaRegla(r), isEnabled: false }, current: instantaneaRegla(r),
        },
      ]);
    }
    // La factura por vencer, el Gate Out y el THC se pueden agregar al carro (selección múltiple, M7-03).
    this.olaD.agregarPagable({
      itemType: 'Invoice', sourceId: ESTADO.FACTURA_POR_VENCER, conceptCode: 'INVOICE', description: 'HL-CL-2026-004310', blNumber: 'HLCUVAP260601930',
      amount: 200000, taxAmount: 38000, totalAmount: 238000,
    });
    this.olaD.agregarPagable({ itemType: 'LocalCharge', sourceId: ESTADO.GATE_OUT, conceptCode: 'GATE_OUT', blNumber: 'HLCUVAP260601930', amount: 50000, taxAmount: 9500, totalAmount: 59500 });
    for (const c of this.refacturaciones[0].detalle.request.charges) {
      this.olaD.agregarPagable({
        itemType: 'LocalCharge', sourceId: c.chargeId, conceptCode: c.conceptCode, amount: c.amount, taxAmount: c.taxAmount, totalAmount: c.totalAmount,
        description: 'SRV-20261005-5E1A0009',
      });
    }
    // Facturas de M7-01: la cubierta por el anticipo del Gate Out (M3-19) y el par de la refacturación IAO (M3-11).
    const factura = (datos: Partial<Invoice> & Pick<Invoice, 'id' | 'sourceNumber'>): Invoice => ({
      siiNumber: null, documentType: 'Invoice', issueDate: '2026-10-04', dueDate: '2026-11-03', blId: null, blNumber: 'HLCU0000001',
      bookingNumber: null, legalName: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO, netAmount: 50000, taxAmount: 9500, totalAmount: 59500,
      currency: 'CLP', status: 'Paid', siiStatus: 'Accepted', canDownload: true, isPayable: false, inCart: false, syncedAt: '2026-10-05T12:00:00Z', ...datos,
    });
    this.olaD.agregarFactura(factura({
      id: ESTADO.FACTURA_CUBIERTA, siiNumber: '100318', sourceNumber: 'HL-CL-2026-004318', blNumber: BL_GATE_OUT_ANTICIPADO,
      coveredBy: this.lineas.find((l) => l.sourceId === ESTADO.FACTURA_CUBIERTA)?.coveredBy ?? null,
    }));
    this.olaD.agregarFactura(factura({
      id: ESTADO.FACTURA_REEMPLAZADA, siiNumber: '100198', sourceNumber: 'HL-CL-2026-004198', issueDate: '2026-09-10', dueDate: '2026-10-10',
      netAmount: 120000, taxAmount: 22800, totalAmount: 142800, status: 'Superseded', supersededByInvoiceId: ESTADO.FACTURA_NUEVA,
    }));
    this.olaD.agregarFactura(factura({
      id: ESTADO.FACTURA_NUEVA, siiNumber: '100402', sourceNumber: 'SRV-20261001-5E1A0007', issueDate: '2026-10-02', dueDate: '2026-11-01',
      netAmount: 120000, taxAmount: 22800, totalAmount: 142800, status: 'Pending', legalName: NUEVA_RAZON.name, taxId: NUEVA_RAZON.taxId,
      supersedesInvoiceId: ESTADO.FACTURA_REEMPLAZADA,
    }));
  }

  private estado(url: URL): AccountStatement {
    const propia = (url.searchParams.get('organizationId') ?? ORGANIZACION_PRUEBA.id) === ORGANIZACION_PRUEBA.id;
    const todas = (propia ? this.lineas : this.lineas.filter((l) => l.kind === 'Invoiced' && l.status !== 'Covered').slice(0, 1).map((l) => ({
      ...l, key: `M${l.key}`, legalName: 'Comercial Mandante SpA', taxId: RUT_MANDANTE,
    }))).map((l) => ({ ...l, inCart: this.olaD.enCarro(l.itemType, l.sourceId) }));
    const anticipos = propia ? this.anticipos.filter((a) => a.kind === 'Advance' || a.payerOrganizationId === ORGANIZACION_PRUEBA.id) : [];
    const lineas = filtrar(todas, url);
    const bl = url.searchParams.get('blNumber')?.trim().toUpperCase() ?? '';
    return {
      organization: propia
        ? { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO, isOwn: true }
        : { id: ORG_MANDANTE_ID, name: 'Comercial Mandante SpA', taxId: RUT_MANDANTE, isOwn: false },
      country: 'CL',
      timeZone: ZONA,
      asOf: HOY,
      evaluatedAt: AHORA,
      lastUpdatedAt: '2026-10-06T11:45:00Z',
      conditionsAvailable: true,
      isCreditCustomer: this.credito && propia,
      credit: this.credito && propia
        ? { creditDays: 30, concepts: ['LOCAL_CHARGES', 'MHD'], validFrom: '2026-01-01', limit: 5000000, limitCurrency: 'CLP', used: 831050, available: 4168950 }
        : null,
      dueSoonDays: 7,
      summary: resumen(todas, anticipos),
      aging: antiguedad(todas),
      lines: lineas,
      advances: anticipos.filter((a) => !bl || (a.blNumber ?? '').includes(bl)),
      actions: { paymentChannel: this.credito ? 'Account' : 'Cart', canPay: propia, canImputeToCredit: this.credito && propia },
    };
  }

  /** Cierre con forma de pago por ítem (M5-10): lo imputado pasa a "imputado a crédito"; lo pagado ahora, a un pago. */
  private cerrarCuenta(request: Request): { status: number; body: unknown } {
    const clave = request.headers()['idempotency-key'];
    if (!clave) return { status: 400, body: { title: 'Validation', errors: { IdempotencyKey: ['Required'] } } };
    const previo = this.porClave.get(clave);
    if (previo) return { status: 200, body: { ...previo, replayed: true } };
    if (!this.credito) return { status: 400, body: { title: 'AccountPayment.NotCreditCustomer', detail: 'Not a credit customer.' } };
    const body = cuerpo(request) as { items: AccountCheckoutItem[]; paymentCurrency: string | null; paymentMethodCode: string | null };
    const elegidas = body.items.map((i) => ({ i, l: this.lineas.find((l) => l.itemType === i.itemType && l.sourceId === i.sourceId) }));
    const noElegible = elegidas.find(({ i, l }) => i.mode === 'Credit' && !l?.canImputeToCredit);
    if (noElegible) {
      return { status: 400, body: { title: 'AccountPayment.CreditNotEligible', detail: `${noElegible.l?.blNumber ?? ''} ${noElegible.l?.conceptCode ?? ''}: not eligible.` } };
    }
    const ahora = elegidas.filter(({ i }) => i.mode !== 'Credit');
    if (ahora.length > 0 && (!body.paymentCurrency || !body.paymentMethodCode)) {
      return { status: 400, body: { title: 'AccountPayment.PaymentDataRequired', detail: 'Payment data required.' } };
    }
    const credito = elegidas.filter(({ i }) => i.mode === 'Credit');
    const totales = (xs: typeof elegidas) => {
      const map = new Map<string, number>();
      for (const { l } of xs) if (l) map.set(l.currency, (map.get(l.currency) ?? 0) + l.balance);
      return [...map.entries()].map(([currency, total]) => ({ currency, amount: total, taxAmount: 0, total }));
    };
    const imputaciones: CreditImputation[] = totales(credito).map((t) => {
      const n = this.siguiente++;
      const numero = `CRI-20261006-${String(n).padStart(8, '0')}`;
      const items = credito.filter(({ l }) => l?.currency === t.currency).map(({ l }) => l as StatementLine);
      this.lineas = this.lineas.map<StatementLine>((l) => (items.includes(l)
        ? { ...l, kind: 'CreditImputed', status: 'CreditImputed', creditImputationNumber: numero, payable: false, canImputeToCredit: false, key: `C${l.key}` }
        : l));
      return {
        id: `q9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, number: numero, status: 'Confirmed', country: 'CL', currency: t.currency,
        amount: t.total, taxAmount: 0, totalAmount: t.total, imputedAt: AHORA,
        items: items.map((l, k) => ({
          id: `q9100000-0000-4000-8000-${String(n * 10 + k).padStart(12, '0')}`, itemType: l.itemType, sourceId: l.sourceId, conceptCode: l.conceptCode,
          blNumber: l.blNumber, amount: l.amount, taxAmount: l.taxAmount, totalAmount: l.totalAmount, currency: l.currency,
        })),
      };
    });
    let payment: AccountCheckoutResult['payment'] = null;
    if (ahora.length > 0) {
      const total = ahora.reduce((s, { l }) => s + (l?.balance ?? 0), 0);
      this.lineas = this.lineas.map((l) => (ahora.some((x) => x.l === l) ? { ...l, inPayment: true } : l));
      payment = {
        payment: {
          id: PAGO.EN_PROCESO, paymentNumber: 'PAY-20261006-0000AC01', status: 'Processing', origin: 'Account', country: 'CL',
          currency: body.paymentCurrency ?? 'CLP', amount: total, taxAmount: 0, totalAmount: total, method: body.paymentMethodCode,
          paymentMethodCode: body.paymentMethodCode, createdAt: AHORA, items: [],
        },
        nextAction: 'None',
        redirectUrl: null,
        replayed: false,
      };
    }
    const resultado: AccountCheckoutResult = { payment, creditImputations: imputaciones, payNowTotals: totales(ahora), creditTotals: totales(credito), replayed: false };
    this.porClave.set(clave, resultado);
    return { status: 200, body: resultado };
  }

  private depositoDto(id: string): PaymentDepositProofs | null {
    const e = this.depositos[id];
    if (!e) return null;
    const ultimo = [...e.proofs].sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt))[0];
    const awaitingProof = e.status !== 'Confirmed' && (!ultimo || ultimo.status === 'Rejected');
    return { payment: resumenDeposito(id, e), proofs: e.proofs, awaitingProof, canUpload: awaitingProof };
  }

  private cola(url: URL): DepositProofQueueItem[] {
    const status = url.searchParams.get('status') || 'Submitted';
    return Object.entries(this.depositos)
      .flatMap(([id, e]) => e.proofs.map((proof) => ({ id, e, proof })))
      .filter(({ proof }) => proof.status === status)
      .sort((a, b) => a.proof.uploadedAt.localeCompare(b.proof.uploadedAt))
      .map(({ e, proof }) => ({
        proof, paymentNumber: e.paymentNumber, slipNumber: e.slipNumber, paymentStatus: e.status, country: 'CL', currency: 'CLP',
        totalAmount: e.totalAmount, payerTaxId: e.payerTaxId, payerName: e.payerName, blNumbers: e.blNumbers, paymentCreatedAt: '2026-10-03T13:50:00Z',
        timeZone: ZONA,
      }));
  }

  private refacturacion(id: string): Refacturacion | undefined {
    return this.refacturaciones.find((r) => r.detalle.request.id === id);
  }

  private vistaAceptacion(r: Refacturacion): ReinvoicingAcceptanceView {
    const x = r.detalle.reissue;
    const fee = x.feeAmount + x.feeTaxAmount;
    return {
      requestNumber: r.detalle.request.requestNumber, originalInvoiceNumber: x.originalSiiNumber ?? x.originalSourceNumber,
      originalLegalName: x.originalLegalName, originalTaxId: x.originalTaxId, originalTotal: 142800, originalCurrency: 'CLP',
      newLegalName: x.newLegalName, newTaxId: x.newTaxId, feeTotal: fee, vatLossAmount: x.vatLossAmount, totalAmount: fee + x.vatLossAmount,
      currency: x.currency, acceptanceStatus: x.acceptanceStatus ?? 'Pending', expiresAt: x.acceptanceExpiresAt, acceptedAt: x.acceptedAt,
      declinedAt: x.declinedAt, timeZone: ZONA,
    };
  }

  /** Agrega a las gestiones del dashboard una solicitud de servicio on demand (Fase 2, Ola G/H). */
  private dashboard(url: URL): unknown {
    const base = dashboard(url);
    return {
      ...base,
      requests: {
        inProgress: base.requests.inProgress + 1,
        items: [
          {
            kind: 'ServiceRequest', id: 's9000000-0000-4000-8000-000000000002', reference: 'SRV-20261005-5E1A0002', blNumber: 'HLCUSAI260300610',
            status: 'PendingPayment', inProgress: true, createdAt: '2026-10-05T13:00:00Z',
            target: { kind: 'ServiceRequest', id: 's9000000-0000-4000-8000-000000000002' },
          },
          ...base.requests.items,
        ],
      },
    };
  }

  /** Responde la ruta si es de la Ola H; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodo: string, url: URL): Promise<boolean> {
    const request = route.request();

    // Ola G: definiciones para los filtros y solicitudes en el dashboard.
    if (ruta === 'service-requests/definitions' && metodo === 'GET') {
      await json(route, 200, DEFINICIONES);
      return true;
    }
    if (ruta === 'dashboard' && metodo === 'GET') {
      await json(route, 200, this.dashboard(url));
      return true;
    }

    // Pago anticipado del Gate Out de exportación (M3-19).
    if (ruta === `charges/${BL_GATE_OUT_POR_PAGAR}` && metodo === 'GET') {
      await json(route, 200, cargosGateOut());
      return true;
    }

    // Estado de cuenta (M7-03) y cierre por ítem (M5-10).
    if (ruta === 'account-statement' && metodo === 'GET') {
      await json(route, 200, this.estado(url));
      return true;
    }
    if (ruta === 'account-statement/export' && metodo === 'GET') {
      const csv = url.searchParams.get('format') === 'csv';
      await route.fulfill({
        status: 200,
        contentType: csv ? 'text/csv' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        body: csv ? '﻿kind,number,balance\r\nInvoiced,100245,595000\r\n' : Buffer.from('PK\u0003\u0004 planilla de prueba'),
      });
      return true;
    }
    if (ruta === 'account-statement/checkout' && metodo === 'POST') {
      const r = this.cerrarCuenta(request);
      if (r.status === 200) await json(route, 200, r.body);
      else await route.fulfill({ status: r.status, contentType: 'application/problem+json', body: JSON.stringify(r.body) });
      return true;
    }
    if (ruta === 'cart/items/batch' && metodo === 'POST') {
      const body = cuerpo(request) as { items: { itemType: string; sourceId?: string | null }[] };
      await json(route, 200, this.olaD.agregarVarios(body.items ?? []));
      return true;
    }

    // Comprobantes de depósito (M5-06).
    let m = ruta.match(/^payments\/([^/]+)\/deposit-proofs\/([^/]+)\/file$/);
    if (m && metodo === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/pdf', body: PDF });
      return true;
    }
    m = ruta.match(/^payments\/([^/]+)\/deposit-proofs$/);
    if (m) {
      const id = m[1];
      const e = this.depositos[id];
      if (!e) {
        await problema(route, 400, 'Payment.NotDeposit', 'The payment is not a deposit payment.');
        return true;
      }
      if (metodo === 'POST') {
        const dto = this.depositoDto(id) as PaymentDepositProofs;
        if (!dto.canUpload) {
          await problema(route, 409, 'DepositProof.PendingReview', 'A proof is already under review.');
          return true;
        }
        const datos = request.postDataBuffer()?.toString('latin1') ?? '';
        const campo = (nombre: string) => new RegExp(`name="${nombre}"\\r\\n\\r\\n([^\\r\\n]*)`).exec(datos)?.[1] || null;
        const n = this.siguiente++;
        e.proofs = [...e.proofs, comprobante(id, {
          id: `y9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, status: 'Submitted', fileName: /filename="([^"]+)"/.exec(datos)?.[1] ?? 'comprobante.pdf',
          bankName: campo('bankName'), bankReference: campo('bankReference'), depositDate: campo('depositDate'),
          depositAmount: campo('depositAmount') ? Number(campo('depositAmount')) : null, notes: campo('notes'), uploadedAt: `2026-10-06T12:${String(n).padStart(2, '0')}:00Z`,
        })];
      }
      await json(route, 200, this.depositoDto(id));
      return true;
    }
    if (ruta === 'admin/payments/deposit-proofs' && metodo === 'GET') {
      await json(route, 200, this.cola(url));
      return true;
    }
    m = ruta.match(/^admin\/payments\/deposit-proofs\/([^/]+)\/(verify|reject)$/);
    if (m && metodo === 'POST') {
      const entrada = Object.entries(this.depositos).find(([, e]) => e.proofs.some((p) => p.id === m?.[1]));
      const proof = entrada?.[1].proofs.find((p) => p.id === m?.[1]);
      if (!entrada || !proof) {
        await problema(route, 404, 'DepositProof.NotFound', 'Not found.');
        return true;
      }
      if (proof.status !== 'Submitted') {
        await problema(route, 409, 'DepositProof.NotPendingReview', 'Already reviewed.');
        return true;
      }
      const body = (cuerpo(request) as { notes?: string; reason?: string } | null) ?? {};
      if (m[2] === 'reject' && !body.reason?.trim()) {
        await problema(route, 400, 'Validation', 'Reason is required.');
        return true;
      }
      const [id, e] = entrada;
      const revisado: DepositProof = {
        ...proof, status: m[2] === 'verify' ? 'Verified' : 'Rejected', reviewedAt: AHORA, reviewedBy: USUARIO_ADMIN.email,
        reviewNotes: body.notes ?? null, rejectionReason: m[2] === 'reject' ? (body.reason ?? null) : null,
      };
      e.proofs = e.proofs.map((p) => (p.id === proof.id ? revisado : p));
      if (m[2] === 'verify') {
        e.status = 'Confirmed';
        e.receiptNumber = 'RCP-20261006-7F7F7F7F';
      }
      await json(route, 200, { proof: revisado, payment: resumenDeposito(id, e) });
      return true;
    }

    // Anticipos y su cruce (M7-03, M3-19, NF-04).
    if (ruta === 'admin/payments/settlements' && metodo === 'GET') {
      const p = (k: string) => url.searchParams.get(k)?.trim() ?? '';
      await json(route, 200, [...this.anticipos, ...(this.credito ? [] : [imputacionSembrada()])]
        .filter((a) => !p('status') || a.status === p('status'))
        .filter((a) => !p('kind') || a.kind === p('kind'))
        .filter((a) => !p('country') || a.country === p('country'))
        .filter((a) => !p('blNumber') || (a.blNumber ?? '').includes(p('blNumber').toUpperCase())));
      return true;
    }
    if (ruta === 'admin/payments/settlements/match' && metodo === 'POST') {
      const abiertos = this.anticipos.filter((a) => a.kind === 'Advance' && a.status === 'Open');
      await json(route, 200, { openBefore: abiertos.length, matched: 0, invoiceIds: [], matchedAt: AHORA });
      return true;
    }
    m = ruta.match(/^admin\/payments\/settlements\/([^/]+)\/match$/);
    if (m && metodo === 'POST') {
      const a = this.anticipos.find((x) => x.id === m?.[1]);
      const body = (cuerpo(request) as { invoiceId?: string; note?: string | null } | null) ?? {};
      if (!a) {
        await problema(route, 404, 'Settlement.NotFound', 'Not found.');
      } else if (a.status === 'Matched') {
        await problema(route, 409, 'Settlement.AlreadyMatched', 'Already matched.');
      } else {
        Object.assign(a, {
          status: 'Matched', matchedInvoiceId: body.invoiceId, matchedInvoiceNumber: '100400', matchedAt: AHORA, matchedBy: USUARIO_ADMIN.email,
          matchNote: body.note ?? null,
        });
        await json(route, 200, a);
      }
      return true;
    }

    // Conceptos imputables a crédito (maintainers.manage, NF-15).
    if (ruta === 'payment-config/credit-imputation') {
      if (metodo === 'POST') {
        const body = cuerpo(request) as { country: string; conceptCode: string; nexusCreditConcept: string; isEnabled: boolean; notes: string | null };
        if (this.reglas.some((r) => r.country === body.country && r.conceptCode === body.conceptCode)) {
          await problema(route, 409, 'CreditImputationRule.AlreadyExists', 'Already exists.');
          return true;
        }
        const nueva: CreditImputationRule = {
          ...regla(100 + this.siguiente++, body.conceptCode, body.conceptCode), country: body.country, nexusCreditConcept: body.nexusCreditConcept,
          isEnabled: body.isEnabled, notes: body.notes, createdAt: AHORA, createdBy: USUARIO_ADMIN.email,
        };
        this.reglas.push(nueva);
        this.historialReglas.set(nueva.id, [{ id: `h${nueva.id.slice(1)}`, entityId: nueva.id, action: 'Created', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: null, current: instantaneaRegla(nueva) }]);
        await json(route, 201, nueva);
      } else {
        const country = url.searchParams.get('country');
        await json(route, 200, this.reglas.filter((r) => !country || r.country === country)
          .filter((r) => url.searchParams.get('includeDisabled') === 'true' || r.isEnabled));
      }
      return true;
    }
    m = ruta.match(/^payment-config\/credit-imputation\/([^/]+)\/history$/);
    if (m && metodo === 'GET') {
      await json(route, 200, [...(this.historialReglas.get(m[1]) ?? [])].reverse());
      return true;
    }
    m = ruta.match(/^payment-config\/credit-imputation\/([^/]+)$/);
    if (m && (metodo === 'PUT' || metodo === 'DELETE')) {
      const anterior = this.reglas.find((r) => r.id === m?.[1]);
      if (!anterior) {
        await problema(route, 404, 'CreditImputationRule.NotFound', 'Not found.');
        return true;
      }
      const lista = this.historialReglas.get(anterior.id) ?? [];
      if (metodo === 'DELETE') {
        this.reglas = this.reglas.filter((r) => r.id !== anterior.id);
        lista.push({ id: `d${this.siguiente++}`, entityId: anterior.id, action: 'Deactivated', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: instantaneaRegla(anterior), current: null });
        await json(route, 204);
      } else {
        const body = cuerpo(request) as { nexusCreditConcept: string; isEnabled: boolean; notes: string | null };
        const actual = { ...anterior, ...body, modifiedAt: AHORA, modifiedBy: USUARIO_ADMIN.email };
        this.reglas = this.reglas.map((r) => (r.id === anterior.id ? actual : r));
        lista.push({ id: `u${this.siguiente++}`, entityId: anterior.id, action: 'Updated', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: instantaneaRegla(anterior), current: instantaneaRegla(actual) });
        await json(route, 200, actual);
      }
      this.historialReglas.set(anterior.id, lista);
      return true;
    }

    // Refacturación IAO (M3-11).
    if (ruta === 'reinvoicing/quote' && metodo === 'GET') {
      const invoiceId = url.searchParams.get('invoiceId');
      const elegible = invoiceId === ITEM.FACTURA_VENCIDA;
      const quote: ReinvoicingQuote = {
        invoiceId: invoiceId ?? '', siiNumber: elegible ? '100245' : '100301', sourceNumber: elegible ? 'HL-CL-2026-003987' : 'HL-CL-2026-004120',
        legalName: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO, invoiceTotal: 595000, invoiceTaxAmount: 95000, invoiceCurrency: 'CLP',
        eligible: elegible, ineligibleReason: elegible ? null : 'Reinvoicing.AlreadyRequested',
        fee: { conceptCode: 'REINVOICING', conceptName: 'Refacturación IAO', amount: 25000, taxAmount: 4750, totalAmount: 29750, currency: 'CLP', tariffCode: 'IAO-CL' },
        vatLoss: { conceptCode: 'VAT_LOSS', conceptName: 'Pérdida de IVA', amount: 95000, taxAmount: 0, totalAmount: 95000, currency: 'CLP' },
        totalAmount: 124750, currency: 'CLP', quotedAt: AHORA, timeZone: ZONA,
      };
      await json(route, 200, quote);
      return true;
    }
    if (ruta === 'reinvoicing' && metodo === 'POST') {
      const body = cuerpo(request) as { invoiceId: string; billing: ServiceBillingData; acceptorEmail: string | null };
      const n = this.siguiente++;
      const id = `s9000000-0000-4000-8000-${String(100 + n).padStart(12, '0')}`;
      const detalle: ReinvoicingDetail = {
        request: solicitudIao(id, `SRV-20261006-5E1A${String(100 + n).padStart(4, '0')}`, 'Draft', body.billing),
        reissue: reemision(`r9000000-0000-4000-8000-${String(100 + n).padStart(12, '0')}`, {
          originalInvoiceId: body.invoiceId, newLegalName: body.billing.name, newTaxId: body.billing.taxId,
          acceptorEmail: body.acceptorEmail ?? body.billing.email ?? '',
        }),
      };
      this.refacturaciones.push({ detalle, token: null });
      await json(route, 201, detalle);
      return true;
    }
    m = ruta.match(/^service-requests\/([^/]+)\/attachments$/);
    if (m && metodo === 'POST' && this.refacturacion(m[1])) {
      const r = this.refacturacion(m[1]) as Refacturacion;
      const datos = request.postDataBuffer()?.toString('latin1') ?? '';
      const adjunto = {
        id: `a9000000-0000-4000-8000-${String(200 + this.siguiente++).padStart(12, '0')}`, fieldKey: 'newCompanyApproval',
        fileName: /filename="([^"]+)"/.exec(datos)?.[1] ?? 'aprobacion.pdf', contentType: 'application/pdf', sizeBytes: 64_000, uploadedAt: AHORA,
        uploadedBy: USUARIO_PRUEBA.email,
      };
      r.detalle.request.attachments = [...r.detalle.request.attachments, adjunto];
      r.detalle.reissue.approvalAttached = true;
      await json(route, 200, adjunto);
      return true;
    }
    m = ruta.match(/^reinvoicing\/acceptance\/([^/]+)$/);
    if (m) {
      const r = this.refacturaciones.find((x) => x.token === decodeURIComponent(m?.[1] ?? ''));
      if (!r) {
        await problema(route, 404, 'ReinvoicingAcceptance.NotFound', 'Not found.');
        return true;
      }
      if (metodo === 'POST') {
        const x = r.detalle.reissue;
        const body = cuerpo(request) as { accept: boolean; name: string; taxId: string; reason: string | null };
        if (x.acceptanceStatus !== 'Pending') {
          await problema(route, 400, 'Reinvoicing.AcceptanceNotPending', 'Not pending.');
          return true;
        }
        if (normalizarRut(body.taxId) !== normalizarRut(x.newTaxId)) {
          await problema(route, 400, 'Reinvoicing.AcceptorTaxIdRequired', 'The tax ID must be the new legal entity one.');
          return true;
        }
        if (body.accept) Object.assign(x, { acceptanceStatus: 'Accepted', acceptedAt: AHORA, acceptedByName: body.name, acceptedByTaxId: body.taxId });
        else Object.assign(x, { acceptanceStatus: 'Declined', declinedAt: AHORA, declineReason: body.reason });
        x.canResendAcceptance = false;
      }
      await json(route, 200, this.vistaAceptacion(r));
      return true;
    }
    m = ruta.match(/^reinvoicing\/([^/]+)(\/(submit|acceptance\/resend))?$/);
    if (m && m[1] !== 'quote') {
      const r = this.refacturacion(m[1]);
      if (!r) {
        await problema(route, 404, 'ServiceRequest.NotFound', 'Not found.');
        return true;
      }
      const { request: sr, reissue: x } = r.detalle;
      if (m[3] === 'submit' && metodo === 'POST') {
        const body = cuerpo(request) as { acceptTariff?: boolean; acceptedTotal?: number };
        const total = x.feeAmount + x.feeTaxAmount + x.vatLossAmount;
        if (!x.approvalAttached) {
          await problema(route, 400, 'Reinvoicing.ApprovalRequired', 'Approval required.');
          return true;
        }
        if (!body.acceptTariff) {
          await problema(route, 400, 'ServiceRequest.TariffNotAccepted', 'Not accepted.');
          return true;
        }
        if (body.acceptedTotal !== total) {
          await problema(route, 400, 'ServiceRequest.TariffChanged', 'Changed.');
          return true;
        }
        const n = this.siguiente++;
        sr.charges = [
          { chargeId: `c9000000-0000-4000-8000-${String(300 + n).padStart(12, '0')}`, itemType: 'LocalCharge', conceptCode: 'REINVOICING', amount: x.feeAmount, taxAmount: x.feeTaxAmount, totalAmount: x.feeAmount + x.feeTaxAmount, currency: 'CLP', status: 'Pending', generated: true },
          { chargeId: `c9000000-0000-4000-8000-${String(400 + n).padStart(12, '0')}`, itemType: 'LocalCharge', conceptCode: 'VAT_LOSS', amount: x.vatLossAmount, taxAmount: 0, totalAmount: x.vatLossAmount, currency: 'CLP', status: 'Pending', generated: true },
        ];
        for (const c of sr.charges) {
          this.olaD.agregarPagable({ itemType: 'LocalCharge', sourceId: c.chargeId, conceptCode: c.conceptCode, amount: c.amount, taxAmount: c.taxAmount, totalAmount: c.totalAmount, description: sr.requestNumber });
        }
        Object.assign(sr, { status: 'PendingPayment', submittedAt: AHORA, tariffAcceptedAt: AHORA, canEdit: false, canSubmit: false, canCancel: true });
        sr.timeline = [...sr.timeline,
          { id: `t9000000-0000-4000-8000-${String(500 + n).padStart(12, '0')}`, fromStatus: 'Draft', toStatus: 'Submitted', occurredAt: AHORA, actorName: USUARIO_PRUEBA.email, actorKind: 'Client' },
          { id: `t9000000-0000-4000-8000-${String(600 + n).padStart(12, '0')}`, fromStatus: 'Submitted', toStatus: 'PendingPayment', occurredAt: AHORA, actorName: 'SYSTEM', actorKind: 'System' }];
        Object.assign(x, { acceptanceStatus: 'Pending', acceptanceRequestedAt: AHORA, acceptanceExpiresAt: '2026-10-21T12:00:00Z', canResendAcceptance: true });
        r.token = `IAO-TOKEN-${n}`;
      } else if (m[3] === 'acceptance/resend' && metodo === 'POST') {
        if (x.acceptanceStatus !== 'Pending') {
          await problema(route, 400, 'Reinvoicing.AcceptanceNotPending', 'Not pending.');
          return true;
        }
        x.acceptanceRequestedAt = AHORA;
        x.acceptanceExpiresAt = '2026-10-21T12:00:00Z';
        r.token = `IAO-TOKEN-R${this.siguiente++}`;
      }
      await json(route, 200, r.detalle);
      return true;
    }

    return false;
  }
}
