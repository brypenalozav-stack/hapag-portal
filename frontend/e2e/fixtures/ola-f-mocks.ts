import { Request, Route } from '@playwright/test';
import type { Dashboard } from '../../src/app/core/models/dashboard.model';
import type {
  ShipmentDetail,
  ShipmentIssuance,
  ShipmentListItem,
  ShipmentPublication,
  ShipmentPublicationRule,
  ShipmentPublicationRuleChange,
  ShipmentTatc,
  TatcBatch,
  TatcBatchItem,
} from '../../src/app/core/models/shipment.model';
import type {
  AssistantMailbox,
  AssistantMessage,
  AssistantSession,
  KnowledgeArticle,
  KnowledgeArticleChange,
} from '../../src/app/core/models/assistant.model';
import type { DangerousGood, DangerousGoodSearchResult } from '../../src/app/core/models/dangerous-good.model';
import type { PagedResult } from '../../src/app/core/models/admin-user.model';
import { ORGANIZACION_PRUEBA, USUARIO_PRUEBA } from './session';
import { ITEM } from './ola-d-mocks';

/**
 * Backend simulado de la Ola F (dashboard M1-05, publicación por DIFU M2-01, emisión del BL M2-02, Dispute M2-05,
 * TATC M2-09, asistente M10-01 a M10-05 y buscador DG M10-06). Guarda estado por página: la conversación con el
 * asistente, las solicitudes masivas de TATC y las altas de los mantenedores.
 */

/** BL de importación con emisión liberada por télex y TATC emitido parcialmente (BL01 de la demo). */
export const BL_TATC = 'HLCUVAL250100123';
/** BL con FIS y el sistema de TATC caídos: último estado de emisión conocido y TATC no disponible (NF-11). */
export const BL_ORIGEN_CAIDO = 'HLCUSAI260501240';
/** BL no publicado por falta de DIFU del destino final (Antofagasta), solo visible para el administrador. */
export const BL_NO_PUBLICADO = 'HLCUSAI260601410';
/** Solicitud masiva de TATC anterior de la organización. */
export const LOTE_TATC = 't7000000-0000-4000-8000-000000000001';
/** URL de demostración del módulo de Dispute (CT-DISP). */
export const URL_DISPUTE = 'https://www.hapag-lloyd.com/';
/** Casilla de derivación de Chile. */
export const CASILLA_CL = 'clservice@hapag-lloyd.com';
/** Documento del repositorio de la Ola E que ofrecen el dashboard y el asistente (cupón de retiro). */
const DOCUMENTO_CUPON = { bl: 'HLCU0000001', id: 'd6000000-0000-4000-8000-000000000002', numero: 'CGO-20261005-2B3C4D5E' };
const RUTA_CUPON = `/api/v1/documents/${DOCUMENTO_CUPON.bl}/${DOCUMENTO_CUPON.id}/download`;

const AHORA = '2026-10-06T12:00:00Z';

function blId(bl: string): string {
  return `3f0c2a1e-0000-4000-8000-${bl.slice(-12).padStart(12, '0')}`;
}

// ---------------------------------------------------------------------------
// Dashboard (M1-05)
// ---------------------------------------------------------------------------

function dashboard(url: URL): Dashboard {
  const operacion = url.searchParams.get('operation');
  const pais = url.searchParams.get('country');
  return {
    organizationId: ORGANIZACION_PRUEBA.id,
    country: pais,
    operation: operacion,
    generatedAt: AHORA,
    requests: {
      inProgress: 1,
      items: [
        {
          kind: 'WarehouseChangeBatch', id: 'b2000000-0000-4000-8000-000000000001', reference: '3 BL a Bodega 1', blNumber: null,
          status: 'Processing', inProgress: true, createdAt: '2026-10-05T15:00:00Z', completedAt: null,
          target: { kind: 'WarehouseChangeBatch', id: 'b2000000-0000-4000-8000-000000000001' },
        },
        {
          kind: 'TatcBatch', id: LOTE_TATC, reference: 'CLSAI 4/5', blNumber: null, status: 'CompletedWithErrors', inProgress: false,
          createdAt: '2026-10-04T13:00:00Z', completedAt: '2026-10-04T13:00:02Z', target: { kind: 'TatcBatch', id: LOTE_TATC },
        },
        {
          kind: 'BlCopy', id: 'd6000000-0000-4000-8000-000000000003', reference: 'CBL-20261003-7E8F9A0B', blNumber: 'HLCU0000001',
          status: 'Issued', inProgress: false, createdAt: '2026-10-03T12:30:00Z', completedAt: '2026-10-03T12:30:05Z',
          target: { kind: 'Documents', blNumber: 'HLCU0000001' },
        },
      ],
    },
    pendingPayments: {
      count: 4,
      totals: [{ currency: 'CLP', total: 948500 }],
      items: [
        {
          itemType: 'Invoice', sourceId: ITEM.FACTURA_VENCIDA, blNumber: null, bookingNumber: null, country: 'CL', conceptCode: 'INVOICE',
          description: 'HL-CL-2026-003987', totalAmount: 595000, currency: 'CLP', status: 'Overdue', dueDate: '2026-09-30', inCart: false,
          target: { kind: 'Invoice', id: ITEM.FACTURA_VENCIDA },
        },
        {
          itemType: 'LocalCharge', sourceId: ITEM.GATE_IN, blNumber: 'HLCU0000001', bookingNumber: 'BKG26030010', country: 'CL',
          conceptCode: 'GATE_IN', description: 'Gate In', totalAmount: 59500, currency: 'CLP', status: 'Pending', inCart: false,
          target: { kind: 'Charges', blNumber: 'HLCU0000001', id: ITEM.GATE_IN },
        },
        {
          itemType: 'LocalCharge', sourceId: ITEM.THC, blNumber: 'HLCU0000001', bookingNumber: 'BKG26030010', country: 'CL',
          conceptCode: 'THC', description: 'Terminal Handling Charge', totalAmount: 119000, currency: 'CLP', status: 'Pending', inCart: true,
          target: { kind: 'Charges', blNumber: 'HLCU0000001', id: ITEM.THC },
        },
        {
          itemType: 'Demurrage', sourceId: ITEM.LINEA_DEMURRAGE, blNumber: BL_TATC, bookingNumber: null, country: 'CL',
          conceptCode: 'DEMURRAGE', description: 'HLXU3034002', totalAmount: 175000, currency: 'CLP', status: 'Pending', inCart: false,
          target: { kind: 'Demurrage', blNumber: BL_TATC },
        },
      ],
      truncated: false,
    },
    documents: [
      {
        id: DOCUMENTO_CUPON.id, documentType: 'GateOutCoupon', documentNumber: DOCUMENTO_CUPON.numero, blNumber: DOCUMENTO_CUPON.bl,
        issuedAt: '2026-10-05T14:10:00Z', downloadPath: RUTA_CUPON,
      },
    ],
    indicators: {
      totalShipments: operacion === 'EXPORT' ? 2 : 8,
      byStatus: operacion === 'EXPORT'
        ? [{ key: 'GateIn', count: 1 }, { key: 'OnBoard', count: 1 }]
        : [{ key: 'Arrived', count: 5 }, { key: 'InTransit', count: 2 }, { key: 'GateIn', count: 1 }],
      byOperation: [{ key: 'EXPORT', count: 2 }, { key: 'IMPORT', count: operacion === 'EXPORT' ? 0 : 6 }],
      withPendingCharges: 6,
      upcomingArrivals: [
        { blNumber: 'HLCU0000001', bookingNumber: 'BKG26030010', operation: 'IMPORT', vessel: 'Valparaiso Express', voyage: '2614W', port: 'San Antonio (CLSAI)', date: '2026-10-18T08:00:00Z' },
      ],
      upcomingDepartures: [
        { blNumber: 'HLCUSAI260300610', bookingNumber: 'BKG26030061', operation: 'EXPORT', vessel: 'Santos Express', voyage: '2615E', port: 'San Antonio (CLSAI)', date: '2026-10-12T10:00:00Z' },
      ],
      demurrageAtRisk: {
        count: 2,
        items: [
          { blNumber: 'HLCUSAI260400910', state: 'InvoicedWithDebt', pendingLines: 1, amounts: [{ currency: 'CLP', total: 510000 }] },
          { blNumber: BL_TATC, state: 'CalculatedUnpaid', pendingLines: 2, amounts: [{ currency: 'CLP', total: 175000 }] },
        ],
      },
      evaluatedShipments: 8,
      shipmentsTruncated: false,
    },
  };
}

// ---------------------------------------------------------------------------
// Embarques: emisión (M2-02), publicación (M2-01) y TATC (M2-09)
// ---------------------------------------------------------------------------

const PUBLICADO: ShipmentPublication = { published: true, reasonCode: 'NO_RULE' };

function emision(bl: string, datos: Partial<ShipmentIssuance>): ShipmentIssuance {
  return {
    blNumber: bl, country: 'CL', operation: 'IMPORT', available: true, source: 'FIS', retrievedAt: AHORA, ...datos,
  };
}

function detalle(bl: string, datos: Partial<ShipmentDetail>): ShipmentDetail {
  return {
    id: blId(bl), blNumber: bl, bookingNumber: 'BKG25010012', operation: 'IMPORT', status: 'Arrived', country: 'CL',
    vessel: 'Valparaiso Express', voyage: '2501W', portOfLoading: 'Shanghai', portOfDischarge: 'San Antonio', placeOfDelivery: 'Santiago',
    etd: '2026-02-20T10:00:00Z', eta: '2026-04-04T21:00:00Z', shipper: 'Shanghai Export Co.', consignee: USUARIO_PRUEBA.name,
    roles: ['Customer', 'Consignee'], accessSource: 'Own',
    allowedActions: ['shipment.view', 'bl-issuance.view', 'tatc.download'], canOperate: true, canSelfAssociate: false,
    requiresAssociationForPayment: false, freight: null,
    containers: [
      { id: 'c7000000-0000-4000-8000-000000000001', containerNumber: 'HLXU3034002', type: 'DRY', size: '20', sealNumber: 'SL-100201', weight: 18000, packages: 300, description: 'Repuestos' },
      { id: 'c7000000-0000-4000-8000-000000000002', containerNumber: 'HLXU3034003', type: 'HC', size: '40', sealNumber: 'SL-100202', weight: 21000, packages: 410, description: 'Repuestos' },
    ],
    localCharges: null, demurrageCharges: null, serviceOrders: [], portOfDischargeCode: 'CLSAI', finalDestinationCode: 'CLSCL',
    ...datos,
  };
}

const DETALLES: Record<string, ShipmentDetail> = {
  [BL_TATC]: detalle(BL_TATC, {
    issuance: emision(BL_TATC, {
      documentType: 'BL', status: 'TelexReleased', sourceStatus: 'TELEX_RELEASED', statusAt: '2026-04-02T14:00:00Z', issuancePlace: 'CNSHA',
    }),
    publication: PUBLICADO,
  }),
  [BL_ORIGEN_CAIDO]: detalle(BL_ORIGEN_CAIDO, {
    bookingNumber: 'BKG26050124',
    issuance: emision(BL_ORIGEN_CAIDO, {
      available: false, errorCode: 'Integration.Unavailable',
      lastKnown: { documentType: 'EBL', eblPlatform: 'WAVE', status: 'Pending', statusAt: '2026-05-02T12:00:00Z' },
    }),
  }),
};

function tatc(bl: string): ShipmentTatc {
  if (bl === BL_ORIGEN_CAIDO) {
    return {
      blNumber: bl, bookingNumber: 'BKG26050124', country: 'CL', blStatus: 'Arrived', available: false, containers: [],
      retrievedAt: AHORA, errorCode: 'Integration.Unavailable', canRequestGeneration: false,
    };
  }
  return {
    blNumber: bl, bookingNumber: 'BKG25010012', country: 'CL', blStatus: 'Arrived', eta: '2026-04-04T21:00:00Z', available: true,
    status: 'PartiallyIssued',
    containers: [
      { containerNumber: 'HLXU3034002', tatcNumber: 'TATC-SAI-2026-004512', status: 'Issued', sourceStatus: 'ISSUED', issuedAt: '2026-04-06T13:00:00Z', warehouseCode: 'ALM-SAI-01', pendingReasons: [] },
      { containerNumber: 'HLXU3034003', status: 'NotIssued', sourceStatus: 'NOT_ISSUED', pendingReasons: ['PAYMENT_PENDING', 'MHD_PENDING'] },
    ],
    sourceUpdatedAt: '2026-10-06T11:55:00Z', retrievedAt: AHORA, canRequestGeneration: true,
  };
}

/** BL no publicado (M2-01): solo lo lista el administrador con el filtro de no publicados. */
const ITEM_NO_PUBLICADO: ShipmentListItem = {
  id: blId(BL_NO_PUBLICADO), blNumber: BL_NO_PUBLICADO, bookingNumber: 'BKG26060141', vessel: 'Antofagasta Bridge', voyage: '2606S',
  status: 'InTransit', operation: 'IMPORT', country: 'CL', portOfLoading: 'Busan', portOfDischarge: 'San Antonio',
  etd: '2026-09-01T10:00:00Z', eta: '2026-10-20T08:00:00Z', roles: [], accessSource: 'Admin', hasPendingCharges: false,
  issuance: { documentType: 'BL', status: 'Issued', statusAt: '2026-09-02T12:00:00Z' },
  publication: {
    published: false, reasonCode: 'DIFU_MISSING', ruleId: 'r7000000-0000-4000-8000-000000000001', finalDestinationCode: 'CLANF',
    portOfDischargeCode: 'CLSAI',
  },
};

/** Estado de emisión del listado (último conocido) por BL. */
const EMISION_LISTADO: Record<string, ShipmentListItem['issuance']> = {
  HLCU0000001: { documentType: 'BL', status: 'Issued', statusAt: '2026-09-21T12:00:00Z' },
  HLCUSAI260300610: { documentType: 'SWB', status: 'Issued', statusAt: '2026-10-02T12:00:00Z' },
  HLCUVAP260300720: { documentType: 'EBL', eblPlatform: 'WAVE', status: 'Pending', statusAt: '2026-10-03T12:00:00Z' },
};

// ---------------------------------------------------------------------------
// Solicitud masiva de TATC (M2-09)
// ---------------------------------------------------------------------------

function lote(id: string, items: TatcBatchItem[], datos: Partial<TatcBatch> = {}): TatcBatch {
  const aceptados = items.filter((i) => i.status === 'Accepted').length;
  return {
    id, country: 'CL', locationCode: 'CLSAI', status: aceptados === items.length ? 'Completed' : aceptados === 0 ? 'Failed' : 'CompletedWithErrors',
    sourceRequestId: aceptados > 0 ? 'TATC-REQ-8C3B8C78' : null, totalItems: items.length, acceptedItems: aceptados,
    rejectedItems: items.filter((i) => i.status === 'Rejected').length, requestedBy: USUARIO_PRUEBA.email,
    createdAt: '2026-10-04T13:00:00Z', completedAt: '2026-10-04T13:00:02Z', items, ...datos,
  };
}

/** Resultado de un BL de la solicitud según los datos de la demo (BL12 ya emitido, BL desconocidos no encontrados). */
function lineaTatc(bl: string, lineNumber: number, vistos: Set<string>): TatcBatchItem {
  if (vistos.has(bl)) return { lineNumber, blNumber: bl, status: 'Failed', reasonCode: 'DUPLICATE' };
  vistos.add(bl);
  if (bl === 'HLCUSAI260401020') return { lineNumber, blNumber: bl, billOfLadingId: blId(bl), status: 'Rejected', reasonCode: 'ALREADY_ISSUED' };
  if (!bl.startsWith('HLCU') || bl === BL_NO_PUBLICADO) return { lineNumber, blNumber: bl, status: 'Failed', reasonCode: 'NOT_FOUND' };
  return { lineNumber, blNumber: bl, billOfLadingId: blId(bl), status: 'Accepted' };
}

// ---------------------------------------------------------------------------
// Reglas de publicación (M2-01)
// ---------------------------------------------------------------------------

const REGLA_ANF = 'r7000000-0000-4000-8000-000000000001';

const REGLAS: ShipmentPublicationRule[] = [
  {
    id: REGLA_ANF, country: 'CL', finalDestinationCode: 'CLANF', finalDestinationName: 'Antofagasta', dischargePortCode: 'CLSAI',
    description: 'Distribución por DIFU desde San Antonio', isActive: true, createdAt: '2026-10-01T12:00:00Z', createdBy: 'admin@hapag-lloyd.cl',
    modifiedAt: '2026-10-03T09:00:00Z', modifiedBy: 'admin@hapag-lloyd.cl',
  },
  {
    id: 'r7000000-0000-4000-8000-000000000002', country: 'CL', finalDestinationCode: 'CLPUQ', finalDestinationName: 'Punta Arenas',
    dischargePortCode: 'CLSAI', description: null, isActive: true, createdAt: '2026-10-01T12:00:00Z', createdBy: 'admin@hapag-lloyd.cl',
  },
];

const HISTORIAL_REGLA: ShipmentPublicationRuleChange[] = [
  {
    id: 'h7000000-0000-4000-8000-000000000002', ruleId: REGLA_ANF, action: 'Updated', changedAt: '2026-10-03T09:00:00Z',
    changedBy: 'admin@hapag-lloyd.cl',
    previous: { country: 'CL', finalDestinationCode: 'CLANF', finalDestinationName: 'Antofagasta', dischargePortCode: null, isActive: true },
    current: { country: 'CL', finalDestinationCode: 'CLANF', finalDestinationName: 'Antofagasta', dischargePortCode: 'CLSAI', description: 'Distribución por DIFU desde San Antonio', isActive: true },
  },
  {
    id: 'h7000000-0000-4000-8000-000000000001', ruleId: REGLA_ANF, action: 'Created', changedAt: '2026-10-01T12:00:00Z',
    changedBy: 'admin@hapag-lloyd.cl', previous: null,
    current: { country: 'CL', finalDestinationCode: 'CLANF', finalDestinationName: 'Antofagasta', dischargePortCode: null, isActive: true },
  },
];

// ---------------------------------------------------------------------------
// Asistente (M10-01 a M10-05), base de conocimiento y casillas
// ---------------------------------------------------------------------------

const ARTICULO_ALMACEN = 'k7000000-0000-4000-8000-000000000001';

const ARTICULOS: KnowledgeArticle[] = [
  {
    id: ARTICULO_ALMACEN, country: 'CL', topic: 'SHIPPING', title: '¿Cómo funciona el cambio de almacén?',
    content: 'El cambio de almacén se solicita desde Servicios > Cambio de almacén.\n- Requisito: BL de importación arribado.\n- Costo: según la tarifa vigente, salvo cambio gratuito.',
    keywords: 'almacén, traslado, depósito', sortOrder: 10, isActive: true, createdAt: '2026-10-01T12:00:00Z', createdBy: 'admin@hapag-lloyd.cl',
    modifiedAt: '2026-10-02T12:00:00Z', modifiedBy: 'admin@hapag-lloyd.cl',
  },
  {
    id: 'k7000000-0000-4000-8000-000000000002', country: 'CL', topic: 'DEMURRAGE', title: '¿Cuándo se cobra demurrage?',
    content: 'El demurrage se cobra por cada día que el contenedor permanece en el terminal después de los días libres.',
    keywords: null, sortOrder: 20, isActive: true, createdAt: '2026-10-01T12:00:00Z', createdBy: 'admin@hapag-lloyd.cl',
  },
  {
    id: 'k7000000-0000-4000-8000-000000000003', country: 'BO', topic: 'DOCUMENTATION', title: '¿Cómo obtengo el certificado de libre deuda?',
    content: 'El CLD se emite desde los documentos del embarque cuando no hay deuda pendiente.', keywords: 'CLD, libre deuda',
    sortOrder: 10, isActive: true, createdAt: '2026-10-01T12:00:00Z', createdBy: 'admin@hapag-lloyd.cl',
  },
];

const HISTORIAL_ARTICULO: KnowledgeArticleChange[] = [
  {
    id: 'h7000000-0000-4000-8000-000000000011', articleId: ARTICULO_ALMACEN, action: 'Updated', changedAt: '2026-10-02T12:00:00Z',
    changedBy: 'admin@hapag-lloyd.cl',
    previous: { country: 'CL', topic: 'GENERAL', title: '¿Cómo funciona el cambio de almacén?', content: 'Se solicita desde Servicios.', sortOrder: 10, isActive: true },
    current: { country: 'CL', topic: 'SHIPPING', title: ARTICULOS[0].title, content: ARTICULOS[0].content, keywords: ARTICULOS[0].keywords, sortOrder: 10, isActive: true },
  },
];

const CASILLAS: AssistantMailbox[] = [
  { id: 'm7000000-0000-4000-8000-000000000001', country: 'CL', topic: 'GENERAL', email: CASILLA_CL, notes: 'Customer Service Chile', isActive: true, modifiedAt: '2026-10-01T12:00:00Z', modifiedBy: 'admin@hapag-lloyd.cl' },
  { id: 'm7000000-0000-4000-8000-000000000002', country: 'BO', topic: 'GENERAL', email: 'boservice@hapag-lloyd.com', notes: null, isActive: true },
];

const AVISO_ASISTENTE = 'El asistente responde con la base de conocimiento de Hapag-Lloyd y con los datos del portal a los que usted tiene acceso. No entrega recomendaciones comerciales ni legales.';

function mensaje(n: number, datos: Partial<AssistantMessage> & Pick<AssistantMessage, 'role' | 'content'>): AssistantMessage {
  return {
    id: `a7000000-0000-4000-8000-${String(n).padStart(12, '0')}`, sequence: n, citations: [], actions: [], engineFallback: false,
    createdAt: AHORA, ...datos,
  };
}

/** Respuesta del asistente según la pregunta (mismos casos que la demo del backend). */
function responder(texto: string, n: number): { reply: AssistantMessage; mailboxEmail?: string } {
  const t = texto.toLowerCase();
  const casilla = { type: 'ContactMailbox' as const, label: `Escribir a ${CASILLA_CL}`, path: `mailto:${CASILLA_CL}` };
  if (t.includes(BL_TATC.toLowerCase())) {
    return {
      reply: mensaje(n, {
        role: 'Assistant', intent: 'ShipmentStatus', answerType: 'Data', engine: 'Rules', elapsedMs: 21,
        content: `Estado del BL ${BL_TATC}:\n- Estado: Arribado\n- Nave: Valparaiso Express 2501W\n- ETA: 04-04-2026 18:00 (America/Santiago)\n- Emisión: BL liberado por télex`,
        citations: [{ kind: 'Shipment', reference: BL_TATC, path: `/api/v1/shipments/${BL_TATC}` }],
        actions: [
          { type: 'OpenShipment', label: `Ver el detalle del BL ${BL_TATC}`, blNumber: BL_TATC, id: blId(BL_TATC) },
          { type: 'DownloadDocument', label: `Descargar el cupón de retiro ${DOCUMENTO_CUPON.numero}`, path: RUTA_CUPON, blNumber: DOCUMENTO_CUPON.bl, id: DOCUMENTO_CUPON.id },
        ],
      }),
    };
  }
  if (/abogad|legal|recomiend|conviene|tarifa.*(anterior|histór|pasad)/.test(t)) {
    return {
      mailboxEmail: CASILLA_CL,
      reply: mensaje(n, {
        role: 'Assistant', intent: 'OutOfScope', answerType: 'Refused', engine: 'Rules', elapsedMs: 4,
        content: 'No puedo entregar recomendaciones comerciales ni legales, ni comparaciones de tarifas históricas. Para esa consulta escriba a Customer Service.',
        actions: [casilla],
      }),
    };
  }
  if (/hlcu9/.test(t)) {
    return {
      mailboxEmail: CASILLA_CL,
      reply: mensaje(n, {
        role: 'Assistant', intent: 'ShipmentStatus', answerType: 'NotAvailable', engine: 'Rules', elapsedMs: 9,
        content: 'La información solicitada no está disponible: el BL no existe o no tiene acceso a él.',
        actions: [casilla],
      }),
    };
  }
  if (/almac[eé]n/.test(t)) {
    return {
      reply: mensaje(n, {
        role: 'Assistant', intent: 'Knowledge', answerType: 'Knowledge', engine: 'Rules', elapsedMs: 6,
        content: `${ARTICULOS[0].title}\n${ARTICULOS[0].content}`,
        citations: [{ kind: 'KnowledgeArticle', reference: ARTICULO_ALMACEN, title: ARTICULOS[0].title }],
      }),
    };
  }
  return {
    mailboxEmail: CASILLA_CL,
    reply: mensaje(n, {
      role: 'Assistant', intent: 'Knowledge', answerType: 'NoAnswer', engine: 'Rules', elapsedMs: 5,
      content: 'No tengo una respuesta para esa consulta. La derivamos a Customer Service.',
      actions: [casilla],
    }),
  };
}

// ---------------------------------------------------------------------------
// Mercancías peligrosas (M10-06)
// ---------------------------------------------------------------------------

const GASOLINA: DangerousGood = {
  id: 'g7000000-0000-4000-8000-000000000001', unNumber: 'UN1203', properShippingNameEs: 'Gasolina', properShippingNameEn: 'Gasoline (motor spirit)',
  hazardClass: '3', hazardClassNameEs: 'Líquidos inflamables', hazardClassNameEn: 'Flammable liquids', packingGroup: 'II', classified: true, source: 'SAMPLE',
};

const AGUA: DangerousGood = {
  id: 'g7000000-0000-4000-8000-000000000002', properShippingNameEs: 'Agua mineral embotellada', properShippingNameEn: 'Bottled mineral water',
  notes: 'Carga general, no clasificada como mercancía peligrosa.', classified: false, source: 'SAMPLE',
};

const AVISO_DG = 'Resultado de carácter informativo. No constituye la aprobación operacional del embarque de carga peligrosa.';
const FUENTE_DG = 'Muestra de referencia basada en la Reglamentación Modelo de las Naciones Unidas.';

function buscarDg(q: string): DangerousGoodSearchResult {
  const t = q.toLowerCase();
  if (/bencina|gasolina|1203/.test(t)) {
    return { query: q, resultCode: 'CLASSIFIED', classified: true, message: `Se encontraron 1 clasificaciones de mercancía peligrosa para «${q}».`, items: [GASOLINA], disclaimer: AVISO_DG, dataSource: FUENTE_DG };
  }
  if (/agua/.test(t)) {
    return { query: q, resultCode: 'NOT_CLASSIFIED', classified: false, message: `«${q}» figura como carga no clasificada como mercancía peligrosa.`, items: [AGUA], disclaimer: AVISO_DG, dataSource: FUENTE_DG };
  }
  return { query: q, resultCode: 'NO_MATCH', classified: false, message: `No se encontró una clasificación para «${q}» en la base de referencia.`, items: [], disclaimer: AVISO_DG, dataSource: FUENTE_DG };
}

// ---------------------------------------------------------------------------
// Simulación con estado
// ---------------------------------------------------------------------------

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

/** Estado del backend simulado de la Ola F para una página. */
export class SimulacionOlaF {
  private sesion: AssistantSession | null = null;
  private secuencia = 1;
  private readonly lotes: TatcBatch[] = [
    lote(LOTE_TATC, [
      { lineNumber: 1, blNumber: BL_TATC, billOfLadingId: blId(BL_TATC), status: 'Accepted' },
      { lineNumber: 2, blNumber: 'HLCUSAI260401020', billOfLadingId: blId('HLCUSAI260401020'), status: 'Rejected', reasonCode: 'ALREADY_ISSUED' },
    ]),
  ];
  private siguienteLote = 2;

  /** Ajusta una respuesta GET de otra ola: el listado de embarques trae el estado de emisión y, para el administrador, la publicación. */
  ajustar(ruta: string, respuesta: unknown): unknown {
    if (ruta !== 'shipments') return respuesta;
    const pagina = respuesta as PagedResult<ShipmentListItem>;
    return {
      ...pagina,
      items: pagina.items.map((i) => ({ ...i, issuance: EMISION_LISTADO[i.blNumber] ?? i.issuance, publication: PUBLICADO })),
    };
  }

  /** Responde la ruta si es de la Ola F; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodo: string, url: URL): Promise<boolean> {
    const request = route.request();

    if (ruta === 'dashboard' && metodo === 'GET') {
      await json(route, 200, dashboard(url));
      return true;
    }
    if (ruta === 'config/dispute-link') {
      await json(route, 200, { country: url.searchParams.get('country') ?? 'CL', url: URL_DISPUTE, configured: true, source: 'AppSettings', opensInNewTab: true });
      return true;
    }

    // Listado del administrador con el filtro de publicación (M2-01).
    if (ruta === 'shipments' && metodo === 'GET' && url.searchParams.get('published') === 'false') {
      await json(route, 200, { items: [ITEM_NO_PUBLICADO], total: 1, page: 1, pageSize: 20 });
      return true;
    }

    // Solicitud masiva de TATC (M2-09).
    if (ruta === 'shipments/tatc-batches') {
      if (metodo === 'POST') {
        const body = cuerpo(request) as { locationCode: string; blNumbers: string[] };
        const vistos = new Set<string>();
        const items = body.blNumbers.map((bl, i) => lineaTatc(bl.toUpperCase(), i + 1, vistos));
        const nuevo = lote(`t7000000-0000-4000-8000-${String(this.siguienteLote++).padStart(12, '0')}`, items, {
          locationCode: body.locationCode, createdAt: AHORA, completedAt: AHORA,
        });
        this.lotes.unshift(nuevo);
        await json(route, 200, nuevo);
      } else {
        await json(route, 200, this.lotes.map((l) => ({ ...l, items: null })));
      }
      return true;
    }
    let m = ruta.match(/^shipments\/tatc-batches\/([^/]+)$/);
    if (m) {
      const encontrado = this.lotes.find((l) => l.id === m?.[1]);
      if (encontrado) await json(route, 200, encontrado);
      else await problema(route, 404, 'TatcBatch.NotFound', 'Not found.');
      return true;
    }
    m = ruta.match(/^shipments\/([^/]+)\/tatc$/);
    if (m) {
      await json(route, 200, tatc(m[1]));
      return true;
    }
    m = ruta.match(/^shipments\/([^/]+)\/issuance$/);
    if (m) {
      const d = DETALLES[m[1]];
      await json(route, 200, d?.issuance ?? emision(m[1], { available: false, errorCode: 'Integration.Unavailable' }));
      return true;
    }
    m = ruta.match(/^shipments\/([^/]+)$/);
    if (m && metodo === 'GET' && DETALLES[m[1]]) {
      await json(route, 200, DETALLES[m[1]]);
      return true;
    }

    // Reglas de publicación (M2-01).
    if (ruta === 'shipment-publication-rules') {
      if (metodo === 'POST') {
        const body = cuerpo(request) as Partial<ShipmentPublicationRule>;
        await json(route, 201, { ...REGLAS[0], ...body, id: 'r7000000-0000-4000-8000-000000000009', createdAt: AHORA, modifiedAt: null, modifiedBy: null });
      } else {
        await json(route, 200, REGLAS);
      }
      return true;
    }
    m = ruta.match(/^shipment-publication-rules\/([^/]+)\/history$/);
    if (m) {
      await json(route, 200, m[1] === REGLA_ANF ? HISTORIAL_REGLA : []);
      return true;
    }
    m = ruta.match(/^shipment-publication-rules\/([^/]+)$/);
    if (m) {
      if (metodo === 'DELETE') await json(route, 204);
      else await json(route, 200, { ...(REGLAS.find((r) => r.id === m?.[1]) ?? REGLAS[0]), ...(cuerpo(request) as object), modifiedAt: AHORA });
      return true;
    }

    // Asistente (M10-01 a M10-05).
    if (ruta === 'assistant/sessions' && metodo === 'POST') {
      this.secuencia = 1;
      this.sesion = {
        id: 's7000000-0000-4000-8000-000000000001', country: 'CL', language: 'es', status: 'Active', engineMode: 'Rules',
        startedAt: AHORA, lastActivityAt: AHORA, idleTimeoutMinutes: 30, userEmail: USUARIO_PRUEBA.email, disclaimer: AVISO_ASISTENTE,
        messages: [mensaje(this.secuencia++, {
          role: 'Assistant', intent: 'Greeting', answerType: 'Greeting', engine: 'Rules',
          content: 'Hola, soy el asistente del portal. Puedo ayudarle con procesos, el estado de sus embarques, documentos y cargos.',
        })],
      };
      await json(route, 200, this.sesion);
      return true;
    }
    m = ruta.match(/^assistant\/sessions\/([^/]+)$/);
    if (m && metodo === 'GET') {
      if (this.sesion?.id === m[1]) await json(route, 200, this.sesion);
      else await problema(route, 404, 'AssistantSession.NotFound', 'Not found.');
      return true;
    }
    m = ruta.match(/^assistant\/sessions\/([^/]+)\/messages$/);
    if (m && metodo === 'POST') {
      const texto = (cuerpo(request) as { message: string }).message;
      if (!this.sesion || this.sesion.status !== 'Active') {
        await problema(route, 400, 'AssistantSession.Ended', 'The assistant session has ended.');
        return true;
      }
      if (texto.includes('#429')) {
        await problema(route, 429, 'Assistant.RateLimited', 'Too many messages in a short time.');
        return true;
      }
      const usuario = mensaje(this.secuencia++, { role: 'User', content: texto });
      const { reply, mailboxEmail } = responder(texto, this.secuencia++);
      this.sesion = { ...this.sesion, messages: [...this.sesion.messages, usuario, reply] };
      // Pequeña espera: deja ver el estado "escribiendo".
      await new Promise((r) => setTimeout(r, 300));
      await json(route, 200, {
        sessionId: this.sesion.id, userMessage: usuario, reply, ...(mailboxEmail ? { mailboxEmail } : {}),
        responseTargetMs: 3000, withinResponseTarget: true,
      });
      return true;
    }
    m = ruta.match(/^assistant\/sessions\/([^/]+)\/end$/);
    if (m && metodo === 'POST') {
      const body = cuerpo(request) as { sendTranscript: boolean; email: string | null };
      if (this.sesion) this.sesion = { ...this.sesion, status: 'Ended', endedAt: AHORA };
      await json(route, 200, {
        sessionId: m[1], endedAt: AHORA, transcriptSent: body.sendTranscript,
        ...(body.sendTranscript ? { transcriptSentTo: body.email ?? USUARIO_PRUEBA.email } : {}),
      });
      return true;
    }
    if (ruta === 'assistant/knowledge') {
      if (metodo === 'POST') await json(route, 200, { ...ARTICULOS[0], ...(cuerpo(request) as object), id: 'k7000000-0000-4000-8000-000000000009' });
      else await json(route, 200, ARTICULOS.filter((a) => !url.searchParams.get('country') || a.country === url.searchParams.get('country')));
      return true;
    }
    m = ruta.match(/^assistant\/knowledge\/([^/]+)\/history$/);
    if (m) {
      await json(route, 200, m[1] === ARTICULO_ALMACEN ? HISTORIAL_ARTICULO : []);
      return true;
    }
    m = ruta.match(/^assistant\/knowledge\/([^/]+)$/);
    if (m) {
      if (metodo === 'DELETE') await json(route, 204);
      else await json(route, 200, { ...(ARTICULOS.find((a) => a.id === m?.[1]) ?? ARTICULOS[0]), ...(cuerpo(request) as object), modifiedAt: AHORA });
      return true;
    }
    if (ruta === 'assistant/mailboxes') {
      if (metodo === 'PUT') await json(route, 200, { ...CASILLAS[0], ...(cuerpo(request) as object), modifiedAt: AHORA });
      else await json(route, 200, CASILLAS);
      return true;
    }

    // Mercancías peligrosas (M10-06).
    if (ruta === 'dangerous-goods/search') {
      await json(route, 200, buscarDg(url.searchParams.get('q') ?? ''));
      return true;
    }
    if (ruta === 'dangerous-goods/import' && metodo === 'POST') {
      await json(route, 200, { totalRows: 3, created: 1, updated: 1, skipped: 1, errors: [{ line: 4, reason: "The column 'class' is empty." }] });
      return true;
    }

    return false;
  }
}
