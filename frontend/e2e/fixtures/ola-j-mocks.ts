import { Request, Route } from '@playwright/test';
import type { PagedResult } from '../../src/app/core/models/admin-user.model';
import type {
  FreightCertificateContext,
  ReleaseLetterContext,
  ReleaseLetterContainerOption,
  ReleaseLetterRequest,
  ReleaseLetterRequestBody,
  ReleaseLetterTatc,
  ShipmentDocument,
} from '../../src/app/core/models/document.model';
import type {
  ServiceInputField,
  ServiceRequestDetail,
  ServiceRequestEvent,
  ServiceRequestStatus,
  ServiceRequestSummary,
} from '../../src/app/core/models/service-request.model';
import type { AssistantAction, AssistantMessage } from '../../src/app/core/models/assistant.model';
import type {
  ApiClient,
  ApiClientKey,
  ApiClientRequestLog,
  ApiClientScope,
  CreateApiClientRequest,
} from '../../src/app/core/models/api-client.model';
import { ORGANIZACION_PRUEBA, USUARIO_ADMIN, USUARIO_PRUEBA } from './session';
import type { SimulacionOlaE } from './ola-e-mocks';

/**
 * Backend simulado de la Ola J (Fase 2): certificado de flete de Bolivia sin pago ni carro (M6-02), carta de liberación y
 * desconsolidado con su aprobación por Customer Service y el TATC de las unidades (M6-08, M2-09), entrega de documentos
 * por el asistente con la descarga por la entrega de la conversación (M10-04) y la administración del canal Web Service:
 * clientes, clave mostrada una vez, rotación, revocación y bitácora (M3-17). Guarda estado por página; los documentos
 * emitidos se publican en el repositorio simulado de la Ola E.
 */

const AHORA = '2026-10-06T12:00:00Z';
const ZONA_BO = 'America/La_Paz';

/** Bolivia importación con certificado de flete completado (mismos datos que la demo del backend). */
export const BL_FLETE = 'HLCUIQQ260200078';
/** Bolivia importación con una carta de liberación pendiente de aprobación (HLXU8899001). */
export const BL_LIBERACION = 'HLCUARI260100045';
export const SOLICITUD_FLETE = { id: 'ffffffff-0021-0021-0021-000000000010', numero: 'SRV-20261005-5E1A0010' } as const;
export const SOLICITUD_CARTA = { id: 'ffffffff-0021-0021-0021-000000000011', numero: 'SRV-20261005-5E1A0011' } as const;
export const CERTIFICADO_FLETE = 'CFL-20261005-3B4C5D6E';
/** Unidades del BL de la carta: la primera ya está en la carta pendiente. */
export const UNIDADES = { PENDIENTE: 'HLXU8899001', CON_TATC: 'HLXU8899002', SIN_TATC: 'HLXU8899003' } as const;
export const TRANSPORTISTA_REGISTRADO = { id: 'c3000000-0000-4000-8000-000000000044', name: 'Transportes Illimani SRL', taxId: '4455667018' };

/** Asistente: documentos que entrega (M10-04). */
export const SESION_ASISTENTE = 's7000000-0000-4000-8000-000000000001';
export const ENTREGA = { COPIA: 'e8000000-0000-4000-8000-000000000001', PERDIDA: 'e8000000-0000-4000-8000-000000000002' } as const;
export const COPIA_ENTREGADA = 'CBL-20261003-7E8F9A0B';

/** Canal Web Service (M3-17). */
export const CLIENTE_WS = { id: 'a5000000-0000-4000-8000-000000000001', nombre: 'ERP Importadora Demo', prefijo: '4pnrf9yxigh5' } as const;
export const CLIENTE_WS_REVOCADO = 'a5000000-0000-4000-8000-000000000002';
export const CLAVE_NUEVA = 'hlws_n3wk3yprefx1_ClaveDePruebaQueSeMuestraUnaSolaVez0123456789';
export const CLAVE_ROTADA = 'hlws_r0tat3dkey22_ClaveRotadaDePruebaQueSeMuestraUnaVez9876543';

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

async function problema(route: Route, status: number, title: string, detail: string, errors?: Record<string, string[]>): Promise<void> {
  await route.fulfill({ status, contentType: 'application/problem+json', body: JSON.stringify({ title, detail, status, ...(errors ? { errors } : {}) }) });
}

async function pdf(route: Route, fileName: string): Promise<void> {
  await route.fulfill({
    status: 200,
    contentType: 'application/pdf',
    headers: { 'Content-Disposition': `attachment; filename="${fileName}"; filename*=UTF-8''${encodeURIComponent(fileName)}` },
    body: Buffer.from('%PDF-1.4 documento de prueba'),
  });
}

function blId(bl: string): string {
  return `3f0c2a1e-0000-4000-8000-${bl.slice(-12).padStart(12, '0')}`;
}

function documento(bl: string, datos: Partial<ShipmentDocument> & Pick<ShipmentDocument, 'id' | 'documentType' | 'documentNumber'>): ShipmentDocument {
  return {
    status: 'Issued', billOfLadingId: blId(bl), blNumber: bl, bookingNumber: 'HLCUBKG2602078', country: 'BO', containerNumbers: [],
    issuedAt: AHORA, origin: 'Request', issuedForOrganizationId: ORGANIZACION_PRUEBA.id, issuedForOrganizationName: ORGANIZACION_PRUEBA.name,
    fileName: `${datos.documentNumber}.pdf`, contentType: 'application/pdf', sizeBytes: 50211,
    contentHash: '7c1e4b9a2d3f5e6a8b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a', verificationCode: 'B7Q2-F4X9-L3M8-T6R1',
    signed: false, recipientEmails: [], retainUntil: '2036-10-06T12:00:00Z', ...datos,
  };
}

function evento(n: number, from: ServiceRequestStatus | null, to: ServiceRequestStatus, occurredAt: string, actorName: string, actorKind: ServiceRequestEvent['actorKind'], notes?: string): ServiceRequestEvent {
  return { id: `e8000000-0000-4000-8000-${String(n).padStart(12, '0')}`, fromStatus: from, toStatus: to, occurredAt, actorName, actorKind, ...(notes ? { notes } : {}) };
}

const ESQUEMA_FLETE: ServiceInputField[] = [
  { key: 'consigneeName', labelEs: 'Consignatario', labelEn: 'Consignee', type: 'text', required: true, maxLength: 200 },
  { key: 'consigneeTaxId', labelEs: 'NIT o documento', labelEn: 'Tax ID or ID document', type: 'text', required: true, maxLength: 30 },
  {
    key: 'purpose', labelEs: 'Finalidad', labelEn: 'Purpose', type: 'select', required: true,
    options: [
      { value: 'CUSTOMS', labelEs: 'Aduana', labelEn: 'Customs' }, { value: 'INSURANCE', labelEs: 'Seguro', labelEn: 'Insurance' },
      { value: 'BANK', labelEs: 'Banco', labelEn: 'Bank' }, { value: 'OTHER', labelEs: 'Otra', labelEn: 'Other' },
    ],
  },
  { key: 'recipient', labelEs: 'Dirigido a', labelEn: 'Addressed to', type: 'text', maxLength: 200 },
  { key: 'notes', labelEs: 'Observaciones', labelEn: 'Remarks', type: 'textarea', maxLength: 1000 },
];

/** Formulario de la carta (el mismo del seed del backend, M6-08). */
const ESQUEMA_CARTA: ServiceInputField[] = [
  { key: 'containers', labelEs: 'Contenedores', labelEn: 'Containers', type: 'containers', required: true },
  {
    key: 'legalEntityType', labelEs: 'Tipo de sociedad', labelEn: 'Legal entity type', type: 'select', required: true,
    options: [{ value: 'COMPANY', labelEs: 'Empresa', labelEn: 'Company' }, { value: 'NATURAL_PERSON', labelEs: 'Persona natural', labelEn: 'Natural person' }],
  },
  { key: 'consigneeName', labelEs: 'Consignatario (razón social o nombre)', labelEn: 'Consignee (legal name or name)', type: 'text', required: true, maxLength: 200 },
  { key: 'consigneeTaxId', labelEs: 'NIT o documento de identidad', labelEn: 'Tax ID or ID document', type: 'text', required: true, maxLength: 30 },
  { key: 'consigneeAddress', labelEs: 'Domicilio', labelEn: 'Address', type: 'text', maxLength: 300 },
  { key: 'legalRepresentativeName', labelEs: 'Representante legal', labelEn: 'Legal representative', type: 'text', maxLength: 200 },
  { key: 'legalRepresentativeId', labelEs: 'Documento del representante', labelEn: "Representative's ID", type: 'text', maxLength: 30 },
  { key: 'carrierName', labelEs: 'Transportista', labelEn: 'Carrier', type: 'text', required: true, maxLength: 200 },
  { key: 'carrierTaxId', labelEs: 'NIT / RUT del transportista', labelEn: 'Carrier tax ID', type: 'text', required: true, maxLength: 30 },
  { key: 'driverName', labelEs: 'Conductor', labelEn: 'Driver', type: 'text', maxLength: 200 },
  { key: 'driverId', labelEs: 'Documento del conductor', labelEn: "Driver's ID", type: 'text', maxLength: 30 },
  { key: 'truckPlate', labelEs: 'Patente', labelEn: 'Truck plate', type: 'text', maxLength: 20 },
  { key: 'observations', labelEs: 'Observaciones', labelEn: 'Remarks', type: 'textarea', maxLength: 1000 },
];

function solicitud(code: 'FREIGHT_CERTIFICATE' | 'RELEASE_LETTER', bl: string, datos: Partial<ServiceRequestDetail> & Pick<ServiceRequestDetail, 'id' | 'requestNumber' | 'status'>): ServiceRequestDetail {
  const carta = code === 'RELEASE_LETTER';
  return {
    definitionId: carta ? 'f0000000-0000-4000-8000-000000000141' : 'f0000000-0000-4000-8000-000000000140', definitionCode: code,
    nameEs: carta ? 'Carta de liberación y desconsolidado' : 'Certificado de flete',
    nameEn: carta ? 'Release and deconsolidation letter' : 'Freight certificate',
    organizationId: ORGANIZACION_PRUEBA.id, organizationName: ORGANIZACION_PRUEBA.name, requestedByEmail: USUARIO_PRUEBA.email,
    billOfLadingId: blId(bl), blNumber: bl, bookingNumber: carta ? 'HLCUBKG2601045' : 'HLCUBKG2602078', country: 'BO', operation: 'IMPORT',
    timeZone: ZONA_BO, containerNumbers: [], inputSchema: carta ? ESQUEMA_CARTA : ESQUEMA_FLETE, inputValues: {}, billing: {},
    billingDataRequired: false, tariffAcceptanceRequired: false, statusChangedAt: AHORA, approvalTeam: carta ? 'CustomerService' : 'None',
    fulfillmentTeam: 'None', requiresOutputDocument: false, createdAt: AHORA, charges: [], canEdit: false, canSubmit: false, canCancel: false,
    attachments: [], timeline: [], ...datos,
  };
}

function resumen(r: ServiceRequestDetail): ServiceRequestSummary {
  return {
    id: r.id, requestNumber: r.requestNumber, definitionCode: r.definitionCode, nameEs: r.nameEs, nameEn: r.nameEn, blNumber: r.blNumber,
    bookingNumber: r.bookingNumber, country: r.country, operation: r.operation, status: r.status, assignedTeam: r.assignedTeam ?? null,
    totalAmount: 0, currency: null, isExempt: false, organizationName: r.organizationName, requestedByEmail: r.requestedByEmail,
    createdAt: r.createdAt, statusChangedAt: r.statusChangedAt,
  };
}

// ---------------------------------------------------------------------------
// TATC de las unidades del BL de la carta (M2-09)
// ---------------------------------------------------------------------------

const TATC_UNIDADES: Record<string, ReleaseLetterTatc['containers'][number]> = {
  [UNIDADES.PENDIENTE]: { containerNumber: UNIDADES.PENDIENTE, status: 'NotIssued', sourceStatus: 'NOT_ISSUED', pendingReasons: ['PAYMENT_PENDING', 'MHD_PENDING'] },
  [UNIDADES.CON_TATC]: { containerNumber: UNIDADES.CON_TATC, status: 'Issued', sourceStatus: 'ISSUED', tatcNumber: 'TATC-BO-0045-02', pendingReasons: [] },
  [UNIDADES.SIN_TATC]: { containerNumber: UNIDADES.SIN_TATC, status: 'PreTatc', sourceStatus: 'PRE_TATC', pendingReasons: ['DOCUMENT_PENDING'] },
};

function tatc(unidades: string[], checkedAt: string): ReleaseLetterTatc {
  const containers = unidades.map((u) => TATC_UNIDADES[u] ?? { containerNumber: u, status: 'NotRegistered' as const, pendingReasons: [] });
  const issued = containers.filter((c) => c.status === 'Issued').length;
  const status = issued === containers.length ? 'Issued' : issued > 0 ? 'PartiallyIssued' : containers[0]?.status ?? 'Unknown';
  return { available: true, status, checkedAt, containers };
}

function cartaPendiente(): ServiceRequestDetail {
  return solicitud('RELEASE_LETTER', BL_LIBERACION, {
    id: SOLICITUD_CARTA.id, requestNumber: SOLICITUD_CARTA.numero, status: 'PendingApproval', assignedTeam: 'CustomerService',
    containerNumbers: [UNIDADES.PENDIENTE], createdAt: '2026-10-05T14:00:00Z', submittedAt: '2026-10-05T14:00:00Z',
    statusChangedAt: '2026-10-05T14:00:00Z', canCancel: true,
    inputValues: {
      containers: [UNIDADES.PENDIENTE], legalEntityType: 'COMPANY', consigneeName: 'Comercial Altiplano SRL', consigneeTaxId: '1023456017',
      consigneeAddress: 'Av. Arce 2631, La Paz', legalRepresentativeName: 'Marcela Quispe', legalRepresentativeId: '4876512 LP',
      carrierName: 'Transportes Illimani SRL', carrierTaxId: '4455667018',
    },
    timeline: [
      evento(1, null, 'Draft', '2026-10-05T14:00:00Z', USUARIO_PRUEBA.email, 'Client'),
      evento(2, 'Draft', 'Submitted', '2026-10-05T14:00:00Z', USUARIO_PRUEBA.email, 'Client'),
      evento(3, 'Submitted', 'PendingApproval', '2026-10-05T14:00:01Z', 'SYSTEM', 'System'),
    ],
  });
}

interface CartaGuardada {
  request: ServiceRequestDetail;
  legalEntityType: 'COMPANY' | 'NATURAL_PERSON';
  carrierOrganizationId: string | null;
  tatcAtSubmission: ReleaseLetterTatc;
  tatcAtApproval: ReleaseLetterTatc | null;
  document: ShipmentDocument | null;
}

// ---------------------------------------------------------------------------
// Canal Web Service (M3-17)
// ---------------------------------------------------------------------------

function clave(id: string, prefix: string, datos: Partial<ApiClientKey> = {}): ApiClientKey {
  return { id, prefix, createdAt: '2026-10-01T12:00:00Z', createdBy: USUARIO_ADMIN.email, lastUsedAt: '2026-10-06T10:15:00Z', active: true, ...datos };
}

function clientesIniciales(): ApiClient[] {
  const base = {
    organizationId: ORGANIZACION_PRUEBA.id, organizationName: ORGANIZACION_PRUEBA.name, organizationTaxId: ORGANIZACION_PRUEBA.taxId,
    createdAt: '2026-10-01T12:00:00Z', createdBy: USUARIO_ADMIN.email,
  };
  return [
    {
      ...base, id: CLIENTE_WS.id, name: CLIENTE_WS.nombre, technicalUserId: 'u5000000-0000-4000-8000-000000000001',
      technicalUserEmail: 'ws-erp-importadora-demo@tecnico.hapag-portal', status: 'Active', scopes: ['responsibility-letter', 'warehouse-change'],
      rateLimitPerMinute: 60, signatory: { name: 'Daniela Demo', taxId: '12.345.678-9', position: 'Jefa de Operaciones', email: 'daniela@importadoraandes.cl' },
      technicalContactEmail: 'ti@importadoraandes.cl', lastUsedAt: '2026-10-06T10:15:00Z',
      keys: [clave('k5000000-0000-4000-8000-000000000001', CLIENTE_WS.prefijo)],
    },
    {
      ...base, id: CLIENTE_WS_REVOCADO, name: 'Integración anterior', technicalUserId: 'u5000000-0000-4000-8000-000000000002',
      technicalUserEmail: 'ws-integracion-anterior@tecnico.hapag-portal', status: 'Revoked', scopes: ['warehouse-change'], rateLimitPerMinute: 30,
      signatory: {}, revokedAt: '2026-10-03T09:00:00Z', revokedBy: USUARIO_ADMIN.email, revocationReason: 'Reemplazada por el ERP nuevo.',
      keys: [clave('k5000000-0000-4000-8000-000000000002', 'old7prefix0a', { active: false, revokedAt: '2026-10-03T09:00:00Z', revokedBy: USUARIO_ADMIN.email })],
    },
  ];
}

function bitacora(): ApiClientRequestLog[] {
  const fila = (n: number, datos: Partial<ApiClientRequestLog> & Pick<ApiClientRequestLog, 'operation' | 'method' | 'path' | 'outcome'>): ApiClientRequestLog => ({
    id: `r5000000-0000-4000-8000-${String(n).padStart(12, '0')}`, keyId: 'k5000000-0000-4000-8000-000000000001',
    receivedAt: `2026-10-06T10:${String(10 + n).padStart(2, '0')}:00Z`, completedAt: `2026-10-06T10:${String(10 + n).padStart(2, '0')}:01Z`,
    durationMs: 120 + n * 10, sourceAddress: '200.1.2.3', ...datos,
  });
  return [
    fila(5, { operation: 'WarehouseChange', method: 'POST', path: '/api/ws/v1/warehouse-changes', outcome: 'Accepted', statusCode: 201, idempotencyKey: 'erp-wc-0005', targetType: 'WarehouseChange', targetReference: 'WC-20261006-0005', blNumber: 'HLCUVAL250100123' }),
    fila(4, { operation: 'ResponsibilityLetter', method: 'POST', path: '/api/ws/v1/responsibility-letters', outcome: 'Rejected', statusCode: 403, errorCode: 'Error.Forbidden', idempotencyKey: 'erp-rl-0004', blNumber: 'HLCUVAL250100123' }),
    fila(3, { operation: 'WarehouseChangeBatch', method: 'POST', path: '/api/ws/v1/warehouse-changes/bulk', outcome: 'Failed', statusCode: 503, errorCode: 'Integration.Unavailable', idempotencyKey: 'erp-bulk-0003' }),
    fila(2, { operation: 'GetRequest', method: 'GET', path: '/api/ws/v1/requests/r5000000-0000-4000-8000-000000000001', outcome: 'Accepted', statusCode: 200 }),
    fila(1, { operation: 'ClientInfo', method: 'GET', path: '/api/ws/v1/me', outcome: 'Accepted', statusCode: 200 }),
  ];
}

// ---------------------------------------------------------------------------
// Asistente: entrega de documentos (M10-04)
// ---------------------------------------------------------------------------

function respuestaEntrega(texto: string, sesion: string, n: number): { reply: AssistantMessage; mailboxEmail?: string } | null {
  const t = texto.toLowerCase();
  if (!/env[ií]a|send me|entr[eé]game|desc[aá]rgame/.test(t)) return null;
  const mensaje = (datos: Partial<AssistantMessage> & Pick<AssistantMessage, 'content'>): AssistantMessage => ({
    id: `a8000000-0000-4000-8000-${String(n).padStart(12, '0')}`, sequence: n, role: 'Assistant', citations: [], actions: [],
    engineFallback: false, engine: 'Rules', elapsedMs: 18, createdAt: AHORA, intent: 'DocumentDelivery', ...datos,
  });
  const ruta = (entrega: string) => `/api/v1/assistant/sessions/${sesion}/deliveries/${entrega}/download`;
  if (t.includes('collect')) {
    const casilla = 'cs.chile@hapag-lloyd.com';
    return {
      mailboxEmail: casilla,
      reply: mensaje({
        answerType: 'NotAvailable',
        content: 'No hay un comprobante collect emitido del BL HLCUSAI260501240 que usted pueda ver.',
        actions: [{ type: 'ContactMailbox', label: `Escribir a ${casilla}`, path: `mailto:${casilla}` }],
      }),
    };
  }
  const acciones: AssistantAction[] = [];
  if (t.includes('copia')) {
    acciones.push({ type: 'DownloadDocument', label: `Descargar Copia de BL no valorada ${COPIA_ENTREGADA}`, blNumber: 'HLCU0000001', id: 'd6000000-0000-4000-8000-000000000003', path: ruta(ENTREGA.COPIA) });
  }
  if (t.includes('transbordo')) {
    acciones.push({ type: 'DownloadDocument', label: 'Descargar Certificado de transbordo CTB-20261004-5A1B2C3D', blNumber: 'HLCU0000001', id: 'd6000000-0000-4000-8000-000000000001', path: ruta(ENTREGA.PERDIDA) });
  }
  return {
    reply: mensaje({
      answerType: 'Data',
      content: acciones.length === 1
        ? 'Este es el documento emitido del BL HLCU0000001 que usted puede descargar:'
        : `Estos son los ${acciones.length} documentos emitidos del BL HLCU0000001 que usted puede descargar:`,
      citations: [{ kind: 'Documents', reference: 'HLCU0000001', path: '/api/v1/documents/HLCU0000001' }],
      actions: acciones,
    }),
  };
}

// ---------------------------------------------------------------------------
// Simulación con estado
// ---------------------------------------------------------------------------

/** Estado del backend simulado de la Ola J para una página. */
export class SimulacionOlaJ {
  private readonly solicitudesFlete: ServiceRequestDetail[];
  private readonly certificados: ShipmentDocument[];
  private readonly cartas: CartaGuardada[];
  private clientes = clientesIniciales();
  private readonly registros = new Map<string, ApiClientRequestLog[]>([[CLIENTE_WS.id, bitacora()]]);
  private siguiente = 30;
  private secuenciaAsistente = 500;

  constructor(private readonly olaE: SimulacionOlaE) {
    this.solicitudesFlete = [solicitud('FREIGHT_CERTIFICATE', BL_FLETE, {
      id: SOLICITUD_FLETE.id, requestNumber: SOLICITUD_FLETE.numero, status: 'Completed', createdAt: '2026-10-05T13:00:00Z',
      completedAt: '2026-10-05T13:00:02Z', statusChangedAt: '2026-10-05T13:00:02Z',
    })];
    this.certificados = [documento(BL_FLETE, {
      id: 'd8000000-0000-4000-8000-000000000010', documentType: 'FreightCertificate', documentNumber: CERTIFICADO_FLETE,
      issuedAt: '2026-10-05T13:00:02Z', signed: true, signatureId: 'SIG-DUMMY-0010', signatureProvider: 'Dummy', signatureLevel: 'Advanced',
      signedAt: '2026-10-05T13:00:02Z', recipientEmails: [USUARIO_PRUEBA.email], deliveredAt: '2026-10-05T13:00:05Z',
    })];
    this.cartas = [{
      request: cartaPendiente(), legalEntityType: 'COMPANY', carrierOrganizationId: null,
      tatcAtSubmission: tatc([UNIDADES.PENDIENTE], '2026-10-05T14:00:00Z'), tatcAtApproval: null, document: null,
    }];
    for (const d of this.certificados) this.olaE.publicar(BL_FLETE, d);
  }

  private numero(): { id: string; numero: string; n: number } {
    const n = this.siguiente++;
    return { id: `ffffffff-0021-0021-0021-${String(n).padStart(12, '0')}`, numero: `SRV-20261006-5E1A${String(n).padStart(4, '0')}`, n };
  }

  private carta(id: string): CartaGuardada | undefined {
    return this.cartas.find((c) => c.request.id === id);
  }

  private vistaCarta(c: CartaGuardada, interna: boolean): ReleaseLetterRequest {
    return {
      request: interna ? { ...c.request, canCancel: false } : c.request,
      legalEntityType: c.legalEntityType,
      ...(c.carrierOrganizationId ? { carrierOrganizationId: c.carrierOrganizationId } : {}),
      tatcAtSubmission: c.tatcAtSubmission,
      ...(c.tatcAtApproval ? { tatcAtApproval: c.tatcAtApproval } : {}),
      ...(interna
        ? {
          tatcNow: tatc(c.request.containerNumbers, AHORA),
          counter: { exchangeDate: '2026-10-04', hblReceived: true, hblReceivedAt: '2026-10-04', deconsolidated: true, deconsolidatedAt: '2026-10-05' },
        }
        : {}),
      requiresIssuedTatc: false,
      ...(c.document ? { document: c.document } : {}),
    };
  }

  private contextoFlete(bl: string): FreightCertificateContext {
    return {
      blId: blId(bl), blNumber: bl, bookingNumber: 'HLCUBKG2602078', country: 'BO', applicable: true, canRequest: true,
      paymentMode: 'Free', requiresPayment: false, freight: { amount: 1950, currency: 'USD' },
      consignee: { name: 'Comercial Altiplano SRL', taxId: '1023456017' }, purposes: ['CUSTOMS', 'INSURANCE', 'BANK', 'OTHER'],
      requests: this.solicitudesFlete.filter((r) => r.blNumber === bl).map(resumen),
      documents: this.certificados.filter((d) => d.blNumber === bl),
    };
  }

  private contextoCarta(bl: string): ReleaseLetterContext {
    const pendientes = new Map<string, string>();
    for (const c of this.cartas.filter((x) => x.request.status === 'PendingApproval')) {
      for (const u of c.request.containerNumbers) pendientes.set(u, c.request.requestNumber);
    }
    const containers: ReleaseLetterContainerOption[] = [
      { containerNumber: UNIDADES.PENDIENTE, containerType: '20DV', status: 'Discharged' },
      { containerNumber: UNIDADES.CON_TATC, containerType: '40HC', status: 'Discharged' },
      { containerNumber: UNIDADES.SIN_TATC, containerType: '40HC', status: 'Discharged' },
    ].map((c) => ({ ...c, tatc: TATC_UNIDADES[c.containerNumber], ...(pendientes.has(c.containerNumber) ? { pendingRequestNumber: pendientes.get(c.containerNumber) } : {}) }));
    return {
      blId: blId(bl), blNumber: bl, bookingNumber: 'HLCUBKG2601045', country: 'BO', applicable: true, canRequest: true, requiresIssuedTatc: false,
      legalEntityTypes: ['COMPANY', 'NATURAL_PERSON'], consignee: { name: 'Comercial Altiplano SRL' }, containers,
      tatc: tatc(containers.map((c) => c.containerNumber), AHORA),
      carriers: [
        { organizationId: TRANSPORTISTA_REGISTRADO.id, name: TRANSPORTISTA_REGISTRADO.name, taxId: TRANSPORTISTA_REGISTRADO.taxId, source: 'Grant' },
        { organizationId: 'c3000000-0000-4000-8000-000000000045', name: 'Transportes Cordillera Ltda.', taxId: '76543210-K', source: 'PreCreated' },
      ],
      requests: this.cartas.filter((c) => c.request.blNumber === bl).map((c) => resumen(c.request)),
      documents: this.cartas.filter((c) => c.document).map((c) => c.document as ShipmentDocument),
    };
  }

  /** Responde la ruta si es de la Ola J; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodo: string, url: URL): Promise<boolean> {
    const request = route.request();

    // Certificado de flete (M6-02).
    let m = ruta.match(/^documents\/([^/]+)\/freight-certificate$/);
    if (m) {
      const bl = m[1];
      if (bl !== BL_FLETE && bl !== BL_LIBERACION) {
        await problema(route, 400, 'FreightCertificate.NotApplicable', 'Only Bolivia imports.');
        return true;
      }
      if (metodo === 'GET') {
        await json(route, 200, this.contextoFlete(bl));
        return true;
      }
      const body = cuerpo(request) as { consigneeName: string; consigneeTaxId: string; purpose: string; recipient: string | null; notes: string | null; sendEmail: boolean };
      if (!body.consigneeName || !body.consigneeTaxId || !body.purpose) {
        await problema(route, 400, 'Validation.Error', 'One or more validation errors occurred.', { ConsigneeName: ['Required.'] });
        return true;
      }
      const { id, numero, n } = this.numero();
      const numeroCertificado = `CFL-20261006-${String(n).padStart(8, '0')}`;
      const doc = documento(bl, {
        id: `d8000000-0000-4000-8000-${String(n).padStart(12, '0')}`, documentType: 'FreightCertificate', documentNumber: numeroCertificado,
        signed: true, signatureId: `SIG-DUMMY-${n}`, signatureProvider: 'Dummy', signatureLevel: 'Advanced', signedAt: AHORA,
        recipientEmails: body.sendEmail ? [USUARIO_PRUEBA.email] : [], deliveredAt: body.sendEmail ? AHORA : null,
      });
      const detalle = solicitud('FREIGHT_CERTIFICATE', bl, {
        id, requestNumber: numero, status: 'Completed', completedAt: AHORA, submittedAt: AHORA,
        inputValues: { consigneeName: body.consigneeName, consigneeTaxId: body.consigneeTaxId, purpose: body.purpose, ...(body.recipient ? { recipient: body.recipient } : {}) },
        timeline: [
          evento(n * 10, null, 'Draft', AHORA, USUARIO_PRUEBA.email, 'Client'),
          evento(n * 10 + 1, 'Draft', 'Submitted', AHORA, USUARIO_PRUEBA.email, 'Client'),
          evento(n * 10 + 2, 'Submitted', 'Completed', AHORA, 'SYSTEM', 'System', `Certificado ${numeroCertificado} emitido.`),
        ],
      });
      this.solicitudesFlete.unshift(detalle);
      this.certificados.unshift(doc);
      this.olaE.publicar(bl, doc);
      await json(route, 200, { request: detalle, document: doc, sentTo: body.sendEmail ? [USUARIO_PRUEBA.email] : [] });
      return true;
    }

    // Carta de liberación y desconsolidado (M6-08).
    m = ruta.match(/^documents\/release-letter\/requests\/([^/]+)$/);
    if (m && metodo === 'GET') {
      const c = this.carta(m[1]);
      if (c) await json(route, 200, this.vistaCarta(c, false));
      else await problema(route, 404, 'ServiceRequest.NotFound', 'Not found.');
      return true;
    }
    m = ruta.match(/^documents\/([^/]+)\/release-letter$/);
    if (m) {
      const bl = m[1];
      if (bl !== BL_LIBERACION) {
        await problema(route, 400, 'ReleaseLetter.NotApplicable', 'Only Bolivia imports.');
        return true;
      }
      if (metodo === 'GET') {
        await json(route, 200, this.contextoCarta(bl));
        return true;
      }
      const body = cuerpo(request) as ReleaseLetterRequestBody;
      const pendientes = new Set(this.cartas.filter((c) => c.request.status === 'PendingApproval').flatMap((c) => c.request.containerNumbers));
      if (body.containers.some((u) => pendientes.has(u))) {
        await problema(route, 409, 'ReleaseLetter.AlreadyRequested', 'A release letter of your organization for some of the selected containers is pending approval.');
        return true;
      }
      if (!body.carrierOrganizationId && (!body.carrierName || !body.carrierTaxId)) {
        await problema(route, 400, 'ReleaseLetter.CarrierRequired', 'Indicate a registered carrier or the carrier name and tax ID.');
        return true;
      }
      const { id, numero, n } = this.numero();
      const carrier = body.carrierOrganizationId ? TRANSPORTISTA_REGISTRADO : { name: body.carrierName ?? '', taxId: body.carrierTaxId ?? '' };
      const detalle = solicitud('RELEASE_LETTER', bl, {
        id, requestNumber: numero, status: 'PendingApproval', assignedTeam: 'CustomerService', containerNumbers: body.containers,
        submittedAt: AHORA, canCancel: true,
        inputValues: {
          containers: body.containers, legalEntityType: body.legalEntityType, consigneeName: body.consigneeName, consigneeTaxId: body.consigneeTaxId,
          ...(body.consigneeAddress ? { consigneeAddress: body.consigneeAddress } : {}),
          ...(body.legalRepresentativeName ? { legalRepresentativeName: body.legalRepresentativeName } : {}),
          ...(body.legalRepresentativeId ? { legalRepresentativeId: body.legalRepresentativeId } : {}),
          carrierName: carrier.name, carrierTaxId: carrier.taxId,
          ...(body.driverName ? { driverName: body.driverName } : {}),
          ...(body.truckPlate ? { truckPlate: body.truckPlate } : {}),
        },
        timeline: [
          evento(n * 10, null, 'Draft', AHORA, USUARIO_PRUEBA.email, 'Client'),
          evento(n * 10 + 1, 'Draft', 'Submitted', AHORA, USUARIO_PRUEBA.email, 'Client'),
          evento(n * 10 + 2, 'Submitted', 'PendingApproval', AHORA, 'SYSTEM', 'System'),
        ],
      });
      const guardada: CartaGuardada = {
        request: detalle, legalEntityType: body.legalEntityType, carrierOrganizationId: body.carrierOrganizationId,
        tatcAtSubmission: tatc(body.containers, AHORA), tatcAtApproval: null, document: null,
      };
      this.cartas.unshift(guardada);
      await json(route, 201, this.vistaCarta(guardada, false));
      return true;
    }

    // Anulación por el cliente antes de la aprobación (endpoint genérico de solicitudes).
    m = ruta.match(/^service-requests\/([^/]+)\/cancel$/);
    if (m && metodo === 'POST' && this.carta(m[1])) {
      const c = this.carta(m[1]) as CartaGuardada;
      c.request = {
        ...c.request, status: 'Cancelled', cancelledAt: AHORA, statusChangedAt: AHORA, canCancel: false, assignedTeam: null,
        timeline: [...c.request.timeline, evento(900, 'PendingApproval', 'Cancelled', AHORA, USUARIO_PRUEBA.email, 'Client')],
      };
      await json(route, 200, c.request);
      return true;
    }

    // Revisión interna de la carta (Customer Service): detalle, panel y aprobación que emite la carta.
    m = ruta.match(/^admin\/service-requests\/([^/]+)(\/(release-letter|approve|reject))?$/);
    if (m && this.carta(m[1])) {
      const c = this.carta(m[1]) as CartaGuardada;
      const accion = m[3];
      const body = (cuerpo(request) as { notes?: string; reason?: string } | null) ?? {};
      if (!accion && metodo === 'GET') {
        await json(route, 200, { ...c.request, canCancel: false });
      } else if (accion === 'release-letter' && metodo === 'GET') {
        await json(route, 200, this.vistaCarta(c, true));
      } else if (accion === 'approve' && c.request.status === 'PendingApproval') {
        const n = this.siguiente++;
        c.document = documento(BL_LIBERACION, {
          id: `d8000000-0000-4000-8000-${String(n).padStart(12, '0')}`, documentType: 'ReleaseLetter',
          documentNumber: `CLB-20261006-${String(n).padStart(8, '0')}`, containerNumbers: c.request.containerNumbers,
          recipientEmails: [USUARIO_PRUEBA.email], deliveredAt: AHORA,
        });
        c.tatcAtApproval = tatc(c.request.containerNumbers, AHORA);
        const notas = `${body.notes ? `${body.notes} ` : ''}Carta ${c.document.documentNumber} emitida.`;
        c.request = {
          ...c.request, status: 'Completed', approvedAt: AHORA, completedAt: AHORA, statusChangedAt: '2026-10-06T12:05:00Z', assignedTeam: null,
          canCancel: false, resolutionNotes: notas,
          timeline: [
            ...c.request.timeline,
            evento(901, 'PendingApproval', 'Approved', AHORA, USUARIO_ADMIN.email, 'Internal', body.notes),
            evento(902, 'Approved', 'Completed', AHORA, 'SYSTEM', 'System', notas),
          ],
        };
        this.olaE.publicar(BL_LIBERACION, c.document);
        await json(route, 200, { ...c.request });
      } else if (accion === 'reject' && c.request.status === 'PendingApproval') {
        c.request = {
          ...c.request, status: 'Rejected', rejectedAt: AHORA, statusChangedAt: AHORA, assignedTeam: null, canCancel: false,
          resolutionNotes: body.reason ?? null,
          timeline: [...c.request.timeline, evento(903, 'PendingApproval', 'Rejected', AHORA, USUARIO_ADMIN.email, 'Internal', body.reason)],
        };
        await json(route, 200, { ...c.request });
      } else {
        await problema(route, 400, 'ServiceRequest.InvalidTransition', 'Invalid transition.');
      }
      return true;
    }

    // Asistente: entrega de documentos (M10-04) y su descarga por la entrega de la conversación.
    m = ruta.match(/^assistant\/sessions\/([^/]+)\/messages$/);
    if (m && metodo === 'POST') {
      const texto = (cuerpo(request) as { message: string }).message;
      const entrega = respuestaEntrega(texto, m[1], this.secuenciaAsistente + 1);
      if (!entrega) return false;
      const usuario: AssistantMessage = {
        id: `a8000000-0000-4000-8000-${String(this.secuenciaAsistente).padStart(12, '0')}`, sequence: this.secuenciaAsistente, role: 'User',
        content: texto, citations: [], actions: [], engineFallback: false, createdAt: AHORA,
      };
      this.secuenciaAsistente += 2;
      await new Promise((r) => setTimeout(r, 200));
      await json(route, 200, {
        sessionId: m[1], userMessage: usuario, reply: entrega.reply, ...(entrega.mailboxEmail ? { mailboxEmail: entrega.mailboxEmail } : {}),
        responseTargetMs: 3000, withinResponseTarget: true,
      });
      return true;
    }
    m = ruta.match(/^assistant\/sessions\/([^/]+)\/deliveries\/([^/]+)\/download$/);
    if (m) {
      if (m[2] === ENTREGA.COPIA) await pdf(route, `${COPIA_ENTREGADA}.pdf`);
      else if (m[2] === ENTREGA.PERDIDA) await problema(route, 404, 'ShipmentDocument.NotFound', 'The document was not found.');
      else await problema(route, 404, 'AssistantDelivery.NotFound', 'The delivery was not found.');
      return true;
    }

    // Canal Web Service: administración de clientes (M3-17).
    if (ruta === 'admin/api-clients') {
      if (metodo === 'GET') {
        const org = url.searchParams.get('organizationId');
        const status = url.searchParams.get('status');
        await json(route, 200, this.clientes.filter((c) => (!org || c.organizationId === org) && (!status || c.status === status)));
      } else {
        await this.crearCliente(route, cuerpo(request) as CreateApiClientRequest);
      }
      return true;
    }
    m = ruta.match(/^admin\/api-clients\/([^/]+)(\/.*)?$/);
    if (m) {
      const cliente = this.clientes.find((c) => c.id === m?.[1]);
      if (!cliente) {
        await problema(route, 404, 'ApiClient.NotFound', 'Not found.');
        return true;
      }
      await this.accionCliente(route, cliente, m[2] ?? '', metodo, url, cuerpo(request));
      return true;
    }

    return false;
  }

  private async crearCliente(route: Route, body: CreateApiClientRequest): Promise<void> {
    if (body.organizationId !== ORGANIZACION_PRUEBA.id) {
      await problema(route, 400, 'ApiClient.OrganizationNotAllowed', 'The organization is not an approved customer.');
      return;
    }
    const n = this.siguiente++;
    const prefijo = CLAVE_NUEVA.split('_')[1];
    const cliente: ApiClient = {
      id: `a5000000-0000-4000-8000-${String(n).padStart(12, '0')}`, name: body.name, organizationId: ORGANIZACION_PRUEBA.id,
      organizationName: ORGANIZACION_PRUEBA.name, organizationTaxId: ORGANIZACION_PRUEBA.taxId, technicalUserId: `u5000000-0000-4000-8000-${String(n).padStart(12, '0')}`,
      technicalUserEmail: `ws-${n}@tecnico.hapag-portal`, status: 'Active', scopes: body.scopes as ApiClientScope[], rateLimitPerMinute: body.rateLimitPerMinute,
      signatory: body.signatory ?? {}, ...(body.technicalContactEmail ? { technicalContactEmail: body.technicalContactEmail } : {}),
      ...(body.notes ? { notes: body.notes } : {}), createdAt: AHORA, createdBy: USUARIO_ADMIN.email,
      keys: [clave(`k5000000-0000-4000-8000-${String(n).padStart(12, '0')}`, prefijo, { createdAt: AHORA, lastUsedAt: null })],
    };
    this.clientes = [...this.clientes, cliente];
    await json(route, 201, { client: cliente, apiKey: CLAVE_NUEVA, keyPrefix: prefijo });
  }

  private async accionCliente(route: Route, cliente: ApiClient, accion: string, metodo: string, url: URL, body: unknown): Promise<void> {
    const reemplazar = (nuevo: ApiClient) => {
      this.clientes = this.clientes.map((c) => (c.id === nuevo.id ? nuevo : c));
      return nuevo;
    };
    if (!accion && metodo === 'GET') {
      await json(route, 200, cliente);
    } else if (accion === '/requests') {
      const page = Number(url.searchParams.get('page') ?? '1') || 1;
      const pageSize = Number(url.searchParams.get('pageSize') ?? '50') || 50;
      const items = this.registros.get(cliente.id) ?? [];
      const pagina: PagedResult<ApiClientRequestLog> = { items: items.slice((page - 1) * pageSize, page * pageSize), total: items.length, page, pageSize };
      await json(route, 200, pagina);
    } else if (accion === '/keys/rotate' && metodo === 'POST') {
      const grace = (body as { graceMinutes?: number } | null)?.graceMinutes ?? 0;
      const prefijo = CLAVE_ROTADA.split('_')[1];
      const vencen = grace > 0 ? new Date(Date.parse(AHORA) + grace * 60_000).toISOString() : null;
      const actualizado = reemplazar({
        ...cliente, modifiedAt: AHORA,
        keys: [
          ...cliente.keys.map((k) => (!k.active ? k : vencen ? { ...k, expiresAt: vencen } : { ...k, active: false, revokedAt: AHORA, revokedBy: USUARIO_ADMIN.email })),
          clave(`k5000000-0000-4000-8000-${String(this.siguiente++).padStart(12, '0')}`, prefijo, { createdAt: AHORA, lastUsedAt: null }),
        ],
      });
      await json(route, 200, { client: actualizado, apiKey: CLAVE_ROTADA, keyPrefix: prefijo });
    } else if (accion.startsWith('/keys/') && accion.endsWith('/revoke') && metodo === 'POST') {
      const keyId = accion.split('/')[2];
      if (!cliente.keys.some((k) => k.id === keyId)) {
        await problema(route, 404, 'ApiClientKey.NotFound', 'Not found.');
        return;
      }
      const actualizado = reemplazar({
        ...cliente, keys: cliente.keys.map((k) => (k.id === keyId ? { ...k, active: false, revokedAt: AHORA, revokedBy: USUARIO_ADMIN.email } : k)),
      });
      await json(route, 200, actualizado);
    } else if (accion === '/revoke' && metodo === 'POST') {
      if (cliente.status === 'Revoked') {
        await problema(route, 400, 'ApiClient.AlreadyRevoked', 'Already revoked.');
        return;
      }
      const reason = (body as { reason?: string } | null)?.reason ?? '';
      const actualizado = reemplazar({
        ...cliente, status: 'Revoked', revokedAt: AHORA, revokedBy: USUARIO_ADMIN.email, revocationReason: reason,
        keys: cliente.keys.map((k) => (k.active ? { ...k, active: false, revokedAt: AHORA, revokedBy: USUARIO_ADMIN.email } : k)),
      });
      await json(route, 200, actualizado);
    } else {
      await problema(route, 404, 'ApiClient.NotFound', 'Not found.');
    }
  }
}
