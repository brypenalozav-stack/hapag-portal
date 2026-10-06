import { Page, Request, Route } from '@playwright/test';
import type { NotificationItem, NotificationPreference } from '../../src/app/core/models/notification.model';
import type { AnnouncementAdmin, AnnouncementRequest, AnnouncementSnapshot } from '../../src/app/core/models/announcement.model';
import type { Guide, GuideDefinition, GuideRequest, GuideSnapshot } from '../../src/app/core/models/guide.model';
import type {
  AdminOverview,
  ImpersonationRequestEntry,
  ImpersonationSession,
  ImpersonationTarget,
} from '../../src/app/core/models/administration.model';
import type { ExceptionReport, TransactionReport } from '../../src/app/core/models/transaction-report.model';
import type { CounterDetail, CounterRecord, CounterRecordRequest, CounterSnapshot } from '../../src/app/core/models/counter.model';
import type {
  CarrierPreRegistration,
  CarrierPreRegistrationRequest,
  ContactListChange,
  ContactListsView,
  ParentCandidate,
  ParentLink,
} from '../../src/app/core/models/organization-network.model';
import type { MaintainerChange } from '../../src/app/core/models/payment-config.model';
import type { ShipmentDetail, ShipmentListItem } from '../../src/app/core/models/shipment.model';
import type { PagedResult } from '../../src/app/core/models/admin-user.model';
import { Idioma, ORGANIZACION_ADMIN, ORGANIZACION_PRUEBA, PERMISOS_ADMIN, PERMISOS_ORG_ADMIN, USUARIO_ADMIN, USUARIO_PRUEBA, tokenDePrueba } from './session';

/**
 * Backend simulado de la Ola I (Fase 2): bandeja de notificaciones con acciones y preferencias de correo (M1-25),
 * comunicados (M1-26), guías (M1-27), área de administración (M8-05), vista como cliente con escrituras bloqueadas
 * (M8-08), reportería de transacciones y excepciones (M9-01), Counter (M8-09), listas de contactos (M1-06),
 * transportistas pre-creados (M1-09), empresa matriz y su revisión interna (M1-21), y el conflicto del registro con una
 * cuenta pre-creada. Guarda estado por página.
 */

const AHORA = '2026-10-06T12:00:00Z';

/** Opciones de la simulación de la Ola I. */
export interface OpcionesOlaI {
  /** La organización de prueba es empresa matriz de una filial visible (M1-21). */
  matriz?: boolean;
  /** El sistema de origen de las listas de contactos no responde (M1-06). */
  contactosCaidos?: boolean;
  /** La guía del carro se ofrece (estado null): nueva o con pasos cambiados (M1-27). */
  guiaNueva?: boolean;
  /** Hay una sesión de vista como cliente activa (la siembra `sembrarImpersonacion`, M8-08). */
  impersonando?: boolean;
}

/** Solicitante de vinculación con una acción pendiente en la bandeja (M1-08, M1-25). */
export const SOLICITANTE = { userId: 'u0000000-0000-4000-8000-000000000003', name: 'Paula Fuentes' };
export const NOTIFICACION_SOLICITUD = 'n1000000-0000-4000-8000-000000000001';
export const NOTIFICACION_MATRIZ = 'n1000000-0000-4000-8000-000000000020';
/** Comunicados sembrados: Chile importación, Bolivia exportación y un borrador. */
export const COMUNICADO = {
  CL_IMPORT: 'a1000000-0000-4000-8000-000000000001',
  BO_EXPORT: 'a1000000-0000-4000-8000-000000000002',
  BORRADOR: 'a1000000-0000-4000-8000-000000000003',
} as const;
export const GUIA_CARRO = 'cart-checkout';
/** Sesión terminada de vista como cliente, con una escritura bloqueada (M8-08). */
export const SESION_TERMINADA = 'i1000000-0000-4000-8000-000000000001';
/** Counter: un BL enviado a Nexus y otro con el envío fallido (M8-09). */
export const BL_COUNTER_ENVIADO = 'HLCUARI260300830';
export const BL_COUNTER_FALLIDO = 'HLCUARI260100045';
/** Transportista pre-creado con un BL pendiente de activación (M1-09). */
export const TRANSPORTISTA = { email: 'contacto@transportescordillera.cl', taxId: '76543210-K', name: 'Transportes Cordillera Ltda.' };
/** Empresa matriz (M1-21): la filial visible y su BL, y la solicitud pendiente de revisión interna. */
export const FILIAL = { id: 'c2000000-0000-4000-8000-000000000021', name: 'Comercial Filial Andes SpA', taxId: '76999888-1' };
export const BL_FILIAL = 'HLCUVAL260500777';
export const VINCULO_PENDIENTE = 'l1000000-0000-4000-8000-000000000002';
export const MATRIZ_CANDIDATA = { id: 'c2000000-0000-4000-8000-000000000090', name: 'Grupo Demo Holding S.A.', taxId: '96111222-3' };

/** Token de la vista como cliente: los permisos del cliente y el id de la sesión (`imp_sid`). */
function tokenImpersonacion(): string {
  const codificar = (valor: unknown) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  return `${codificar({ alg: 'HS256', typ: 'JWT' })}.${codificar({ sub: 'impersonado', permission: PERMISOS_ORG_ADMIN, imp_sid: 'activa' })}.firma-imp`;
}
export const TOKEN_IMPERSONACION = tokenImpersonacion();

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

function paginar<T>(items: T[], url: URL, tamano = 20): PagedResult<T> {
  const page = Number(url.searchParams.get('page') ?? '1') || 1;
  const pageSize = Number(url.searchParams.get('pageSize') ?? String(tamano)) || tamano;
  return { items: items.slice((page - 1) * pageSize, page * pageSize), total: items.length, page, pageSize };
}

/** Permisos del JWT de la solicitud (solo para decidir qué ve un interno en el detalle). */
function permisos(request: Request): string[] {
  const token = request.headers()['authorization']?.replace(/^Bearer /, '') ?? '';
  try {
    const payload = JSON.parse(Buffer.from(token.split('.')[1] ?? '', 'base64url').toString('utf-8')) as { permission?: string[] };
    return payload.permission ?? [];
  } catch {
    return [];
  }
}

// ---------------------------------------------------------------------------
// Bandeja (M1-25)
// ---------------------------------------------------------------------------

function notificacionesIniciales(): NotificationItem[] {
  return [
    {
      id: NOTIFICACION_SOLICITUD, type: 'JoinRequestReceived', title: 'New join request', module: 'Organization',
      body: 'Paula Fuentes (solicitud@example.com) pidió unirse a la organización.', isRead: false, createdAt: '2026-10-06T10:00:00Z',
      link: { entityType: 'JoinRequest', entityId: SOLICITANTE.userId, reference: SOLICITANTE.name },
      action: { type: 'ApproveJoinRequest', targetId: SOLICITANTE.userId, available: true }, emailSent: true,
    },
    {
      id: 'n1000000-0000-4000-8000-000000000002', type: 'JoinRequestReceived', title: 'New join request', module: 'Organization',
      body: 'Jorge Soto pidió unirse a la organización.', isRead: true, createdAt: '2026-10-02T09:00:00Z', readAt: '2026-10-02T10:00:00Z',
      link: { entityType: 'JoinRequest', entityId: 'u0000000-0000-4000-8000-000000000009', reference: 'Jorge Soto' },
      action: { type: 'ApproveJoinRequest', targetId: 'u0000000-0000-4000-8000-000000000009', available: false, resolvedAt: '2026-10-02T11:00:00Z' },
    },
    {
      id: 'n1000000-0000-4000-8000-000000000003', type: 'DocumentIssued', title: 'Document issued', module: 'Documents',
      body: 'Se emitió la copia valorada del BL HLCU0000001.', isRead: false, createdAt: '2026-10-05T16:00:00Z',
      link: { entityType: 'ShipmentDocument', entityId: 'd1', reference: 'Copia valorada', blNumber: 'HLCU0000001' },
      action: { type: 'OpenDocument', targetId: 'HLCU0000001', available: true },
    },
    {
      id: 'n1000000-0000-4000-8000-000000000004', type: 'AccessRevokedByCascade', title: 'Acceso revocado en cadena', module: 'Access',
      body: 'El acceso sobre HLCU0000001 fue revocado porque se revocó o venció el acceso del que dependía.', isRead: false,
      createdAt: '2026-10-04T10:00:00Z', link: { entityType: 'Shipment', entityId: 'HLCU0000001', reference: 'HLCU0000001', blNumber: 'HLCU0000001' },
      action: { type: 'OpenAccessGrants', available: true },
    },
    {
      id: 'n1000000-0000-4000-8000-000000000005', type: 'AnnouncementPublished', title: 'New announcement', module: 'Announcements',
      body: 'Nuevo horario del depósito de vacíos en San Antonio.', isRead: true, createdAt: '2026-10-03T12:00:00Z',
      link: { entityType: 'Announcement', entityId: COMUNICADO.CL_IMPORT, reference: 'Nuevo horario del depósito de vacíos' },
      action: { type: 'OpenAnnouncement', targetId: COMUNICADO.CL_IMPORT, available: true },
    },
  ];
}

function notificacionesAdmin(): NotificationItem[] {
  return [
    {
      id: NOTIFICACION_MATRIZ, type: 'ParentLinkRequested', title: 'Parent link requested', module: 'Administration',
      body: 'Comercial Altiplano S.R.L. pidió vincularse con Grupo Demo Holding S.A.', isRead: false, createdAt: '2026-10-06T09:00:00Z',
      link: { entityType: 'ParentLink', entityId: VINCULO_PENDIENTE, reference: 'Comercial Altiplano → Grupo Demo Holding' },
      action: { type: 'ReviewParentLink', targetId: VINCULO_PENDIENTE, available: true },
    },
  ];
}

const CATALOGO: [string, string, boolean, boolean, boolean][] = [
  ['JoinRequestReceived', 'Organization', true, true, false],
  ['JoinRequestApproved', 'Organization', true, true, true],
  ['JoinRequestRejected', 'Organization', true, true, true],
  ['AccessGranted', 'Access', true, true, false],
  ['AccessRevokedByCascade', 'Access', true, true, false],
  ['PaymentConfirmed', 'Payments', true, true, false],
  ['DocumentIssued', 'Documents', true, true, false],
  ['DepositProofSubmitted', 'Finance', false, false, false],
  ['ServiceRequestApproved', 'Services', true, true, false],
  ['AnnouncementPublished', 'Announcements', true, false, false],
  ['DeadlineAtRisk', 'Deadlines', false, false, false],
];

function preferenciasIniciales(): NotificationPreference[] {
  return CATALOGO.map(([type, module, emailAvailable, emailDefault, emailMandatory]) => {
    // Sembrado de la demo: DocumentIssued sin correo y AnnouncementPublished con correo.
    const custom = type === 'DocumentIssued' ? false : type === 'AnnouncementPublished' ? true : null;
    return {
      type, module, emailAvailable, emailMandatory, emailDefault,
      emailEnabled: emailMandatory || (emailAvailable && (custom ?? emailDefault)),
      isCustomized: custom !== null,
    };
  });
}

// ---------------------------------------------------------------------------
// Comunicados (M1-26)
// ---------------------------------------------------------------------------

function comunicado(datos: Partial<AnnouncementAdmin> & Pick<AnnouncementAdmin, 'id' | 'titleEs' | 'titleEn' | 'countries' | 'operation' | 'status'>): AnnouncementAdmin {
  return {
    bodyEs: 'Texto del comunicado.', bodyEn: 'Announcement text.', severity: 'Info', isCurrent: datos.status === 'Published',
    notifyOnPublish: false, createdAt: '2026-10-01T12:00:00Z', createdBy: USUARIO_ADMIN.email, validFrom: null, validTo: null,
    publishedAt: datos.status === 'Published' ? '2026-10-03T12:00:00Z' : null,
    publishedBy: datos.status === 'Published' ? USUARIO_ADMIN.email : null,
    ...datos,
  };
}

function comunicadosIniciales(): AnnouncementAdmin[] {
  return [
    comunicado({
      id: COMUNICADO.CL_IMPORT, status: 'Published', countries: ['CL'], operation: 'Import', severity: 'Important',
      titleEs: 'Nuevo horario del depósito de vacíos en San Antonio', titleEn: 'New hours at the San Antonio empty container depot',
      bodyEs: 'Desde el lunes 12 de octubre el depósito atiende de 08:00 a 20:00.', bodyEn: 'From Monday, October 12, the depot opens from 08:00 to 20:00.',
      validTo: '2026-12-31T23:59:00Z', notifyOnPublish: true, notifiedAt: '2026-10-03T12:00:00Z',
    }),
    comunicado({
      id: COMUNICADO.BO_EXPORT, status: 'Published', countries: ['BO'], operation: 'Export',
      titleEs: 'Canje de BL de exportación boliviana en Arica', titleEn: 'Bolivian export BL exchange in Arica',
      bodyEs: 'El canje de BL de exportación se realiza en la oficina de Arica.', bodyEn: 'Export BL exchange takes place at the Arica office.',
    }),
    comunicado({
      id: COMUNICADO.BORRADOR, status: 'Draft', countries: ['CL', 'BO'], operation: 'Both', notifyOnPublish: true,
      titleEs: 'Mantención programada del portal', titleEn: 'Scheduled portal maintenance',
      bodyEs: 'El portal no estará disponible el domingo de 02:00 a 04:00.', bodyEn: 'The portal will be unavailable on Sunday from 02:00 to 04:00.',
    }),
  ];
}

function instantaneaComunicado(a: AnnouncementAdmin): AnnouncementSnapshot {
  return {
    titleEs: a.titleEs, titleEn: a.titleEn, countries: a.countries, operation: a.operation, severity: a.severity,
    validFrom: a.validFrom ?? null, validTo: a.validTo ?? null, status: a.status, notifyOnPublish: a.notifyOnPublish,
  };
}

// ---------------------------------------------------------------------------
// Guías (M1-27)
// ---------------------------------------------------------------------------

function guiaCarro(nueva: boolean): GuideDefinition {
  const paso = (order: number, elementKey: string, titleEs: string, titleEn: string, textEs: string, textEn: string) =>
    ({ order, route: '/cart', elementKey, titleEs, titleEn, textEs, textEn });
  return {
    id: 'g1000000-0000-4000-8000-000000000001', code: GUIA_CARRO, nameEs: 'Pagar desde el carro', nameEn: 'Paying from the cart',
    descriptionEs: 'Cómo revisar los ítems, elegir el RUT, la moneda y el medio de pago.',
    descriptionEn: 'How to review the items and choose the tax ID, currency and payment method.',
    route: '/cart', version: 2, audience: 'Client', isActive: true, displayOrder: 1,
    status: nueva ? null : 'Completed', lastStep: nueva ? null : 5, statusAt: nueva ? null : '2026-10-01T12:00:00Z',
    createdAt: '2026-09-30T12:00:00Z', createdBy: USUARIO_ADMIN.email,
    steps: [
      paso(1, 'cart.items', 'Sus ítems por moneda', 'Your items by currency', 'El carro separa los ítems por país y moneda de pago.', 'The cart groups items by country and payment currency.'),
      paso(2, 'cart.billing-tax-id', 'RUT de facturación', 'Billing tax ID', 'Cada ítem muestra el RUT al que se factura.', 'Each item shows the tax ID it is billed to.'),
      paso(3, 'cart.currency', 'Moneda de pago', 'Payment currency', 'Si el cargo lo permite, elija la moneda de pago.', 'If the charge allows it, choose the payment currency.'),
      paso(4, 'cart.payment-method', 'Medio de pago', 'Payment method', 'Elija el medio de pago habilitado para la moneda.', 'Choose a payment method enabled for the currency.'),
      paso(5, 'cart.checkout', 'Revisar y pagar', 'Review and pay', 'Revise el resumen y confirme el pago.', 'Review the summary and confirm the payment.'),
    ],
  };
}

// ---------------------------------------------------------------------------
// Vista como cliente (M8-08)
// ---------------------------------------------------------------------------

const ACTOR = { userId: USUARIO_ADMIN.id, email: USUARIO_ADMIN.email, fullName: USUARIO_ADMIN.name };
const SUJETO = { userId: 'u0000000-0000-4000-8000-000000000001', email: USUARIO_PRUEBA.email, fullName: 'Carla Rojas' };
const ORG_SESION = { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: ORGANIZACION_PRUEBA.taxId, organizationType: 'Customer' };

function sesionTerminada(): ImpersonationSession {
  return {
    id: SESION_TERMINADA, actor: ACTOR, subject: SUJETO, organization: ORG_SESION, reason: 'Ticket SOP-4512: el cliente no ve un BL',
    status: 'Ended', startedAt: '2026-10-05T14:00:00Z', expiresAt: '2026-10-05T14:30:00Z', endedAt: '2026-10-05T14:12:00Z',
    endReason: 'Manual', durationSeconds: 720, requestCount: 9, blockedCount: 1, readOnly: true, allowedActions: [],
  };
}

/** Sesión activa de vista como cliente que vence en 30 minutos desde ahora. */
export function sesionActiva(id = 'i1000000-0000-4000-8000-000000000099'): ImpersonationSession {
  const inicio = Date.now();
  return {
    id, actor: ACTOR, subject: SUJETO, organization: ORG_SESION, reason: 'Ticket SOP-4520: revisar permisos', status: 'Active',
    startedAt: new Date(inicio).toISOString(), expiresAt: new Date(inicio + 30 * 60_000).toISOString(), requestCount: 0, blockedCount: 0,
    readOnly: true, allowedActions: [],
  };
}

/**
 * Siembra una sesión de vista como cliente en curso (M8-08): el token del cliente, la sesión de impersonación y la del
 * administrador guardada para restaurarla, con las mismas claves que usa AuthService.
 */
export async function sembrarImpersonacion(page: Page, lang?: Idioma): Promise<void> {
  await page.addInitScript(
    ({ token, usuario, organizacion, sesion, admin }) => {
      localStorage.setItem('hl_token', token);
      localStorage.removeItem('hl_refresh_token');
      localStorage.setItem('hl_user', JSON.stringify(usuario));
      localStorage.setItem('hl_organization', JSON.stringify(organizacion));
      localStorage.setItem('hl_impersonation', JSON.stringify(sesion));
      localStorage.setItem('hl_admin_session', JSON.stringify(admin));
    },
    {
      token: TOKEN_IMPERSONACION,
      usuario: USUARIO_PRUEBA,
      organizacion: ORGANIZACION_PRUEBA,
      sesion: sesionActiva(),
      admin: {
        token: tokenDePrueba(PERMISOS_ADMIN),
        refreshToken: 'refresh-de-prueba',
        user: JSON.stringify(USUARIO_ADMIN),
        organization: JSON.stringify(ORGANIZACION_ADMIN),
      },
    },
  );
  if (lang) {
    await page.addInitScript((idioma) => localStorage.setItem('hl_lang', idioma), lang);
  }
}

function solicitudesSesion(id: string): ImpersonationRequestEntry[] {
  return [
    { id: `${id}-1`, action: 'Started', timestamp: '2026-10-05T14:00:00Z', method: 'POST', path: '/api/v1/admin/impersonation/sessions', statusCode: 201 },
    { id: `${id}-2`, action: 'Request', timestamp: '2026-10-05T14:01:10Z', method: 'GET', path: '/api/v1/shipments', statusCode: 200 },
    { id: `${id}-3`, action: 'BlockedWrite', timestamp: '2026-10-05T14:05:42Z', method: 'POST', path: '/api/v1/cart/items', statusCode: 403 },
    { id: `${id}-4`, action: 'Ended', timestamp: '2026-10-05T14:12:00Z', method: 'POST', path: '/api/v1/impersonation/end', statusCode: 200 },
  ];
}

// ---------------------------------------------------------------------------
// Reportería (M9-01)
// ---------------------------------------------------------------------------

function reporteTransacciones(url: URL): TransactionReport {
  const from = url.searchParams.get('from') || '2026-10-01';
  const to = url.searchParams.get('to') || '2026-10-06';
  const filas = [
    {
      paymentId: 'p7000000-0000-4000-8000-000000000001', paymentNumber: 'PAY-20261002-A1B2C3D4', receiptNumber: 'R-000123',
      confirmedAt: '2026-10-02T15:00:00Z', country: 'CL', origin: 'Cart', paymentMethodCode: 'WEBPAY',
      organization: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: ORGANIZACION_PRUEBA.taxId },
      blNumber: 'HLCU0000001', itemType: 'LocalCharge', category: 'LocalCharge', service: 'THC', serviceName: 'Terminal Handling Charge',
      conceptCode: 'THC', billingTaxId: '76123456-7', currency: 'CLP', amount: 180000, taxAmount: 34200, total: 214200,
    },
    {
      paymentId: 'p7000000-0000-4000-8000-000000000002', paymentNumber: 'PAY-20261003-E5F6A7B8', receiptNumber: 'R-000124',
      confirmedAt: '2026-10-03T11:00:00Z', country: 'CL', origin: 'Cart', paymentMethodCode: 'WEBPAY',
      organization: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: ORGANIZACION_PRUEBA.taxId },
      blNumber: 'HLCUVAP260601930', itemType: 'ServiceRequest', category: 'OnDemandService', service: 'DROP_OFF_SCL', serviceName: 'Drop Off SCL',
      serviceRequestNumber: 'SRV-20261003-5E1A0004', billingTaxId: '76123456-7', currency: 'USD', amount: 120, taxAmount: 0, total: 120,
      originalAmount: 108000, originalCurrency: 'CLP',
    },
  ];
  return {
    from, to, country: url.searchParams.get('country'), timeZone: 'America/Santiago', generatedAt: AHORA,
    summary: [
      { category: 'LocalCharge', service: 'THC', serviceName: 'Terminal Handling Charge', currency: 'CLP', transactions: 1, items: 1, amount: 180000, taxAmount: 34200, total: 214200 },
      { category: 'OnDemandService', service: 'DROP_OFF_SCL', serviceName: 'Drop Off SCL', currency: 'USD', transactions: 1, items: 1, amount: 120, taxAmount: 0, total: 120 },
    ],
    totals: [{ currency: 'CLP', transactions: 1, items: 1, total: 214200 }, { currency: 'USD', transactions: 1, items: 1, total: 120 }],
    items: paginar(filas, url, 50),
  };
}

function reporteExcepciones(url: URL): ExceptionReport {
  const from = url.searchParams.get('from') || '2026-10-01';
  const to = url.searchParams.get('to') || '2026-10-06';
  const org = { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: ORGANIZACION_PRUEBA.taxId };
  const filas = [
    { type: 'GateInExemption', occurredAt: '2026-10-02T10:00:00Z', country: 'CL', blNumber: 'HLCU0000001', organization: org, conceptCode: 'GATE_IN', amount: 45000, currency: 'CLP', source: 'NEXUS', reference: 'EXE-2026-0042' },
    { type: 'FreeWarehouseChange', occurredAt: '2026-10-03T12:00:00Z', country: 'CL', blNumber: 'HLCUVAP260601930', organization: org, source: 'PORTAL', reference: 'WC-20261003-01' },
    { type: 'CreditImputation', occurredAt: '2026-10-04T09:00:00Z', country: 'CL', blNumber: 'HLCU0000001', organization: org, conceptCode: 'ISPS', amount: 25000, currency: 'CLP', source: 'PORTAL', reference: 'CRI-20261004-AB12CD34' },
  ];
  return {
    from, to, country: url.searchParams.get('country'), timeZone: 'America/Santiago', generatedAt: AHORA, ipoExclusionsAvailable: false,
    summary: [
      { type: 'GateInExemption', currency: 'CLP', count: 1, amount: 45000 },
      { type: 'FreeWarehouseChange', currency: null, count: 1, amount: 0 },
      { type: 'CreditImputation', currency: 'CLP', count: 1, amount: 25000 },
    ],
    items: paginar(filas, url, 50),
  };
}

// ---------------------------------------------------------------------------
// Counter (M8-09)
// ---------------------------------------------------------------------------

function registroCounter(datos: Partial<CounterRecord> & Pick<CounterRecord, 'id' | 'blNumber' | 'syncStatus'>): CounterRecord {
  return {
    billOfLadingId: `bl-${datos.blNumber}`, country: 'BO', exchangeDate: '2026-10-01', hblReceived: true, hblReceivedAt: '2026-10-01',
    deconsolidated: false, deconsolidatedAt: null, notes: null, recordedBy: USUARIO_ADMIN.email, recordedAt: '2026-10-01T15:00:00Z',
    createdAt: '2026-10-01T15:00:00Z', createdBy: USUARIO_ADMIN.email, ...datos,
  };
}

function instantaneaCounter(r: CounterRecord): CounterSnapshot {
  return {
    country: r.country, exchangeDate: r.exchangeDate ?? null, hblReceived: r.hblReceived, hblReceivedAt: r.hblReceivedAt ?? null,
    deconsolidated: r.deconsolidated, deconsolidatedAt: r.deconsolidatedAt ?? null, notes: r.notes ?? null, syncStatus: r.syncStatus,
  };
}

// ---------------------------------------------------------------------------
// Empresa matriz (M1-21) y transportistas (M1-09)
// ---------------------------------------------------------------------------

function vinculo(datos: Partial<ParentLink> & Pick<ParentLink, 'id' | 'status'>): ParentLink {
  return {
    organization: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, organizationType: 'Customer', country: 'CL' },
    parent: { id: MATRIZ_CANDIDATA.id, name: MATRIZ_CANDIDATA.name, organizationType: 'Customer', country: 'CL' },
    visibilityEnabled: true, requestedAt: '2026-10-01T12:00:00Z', requestedBy: USUARIO_PRUEBA.email, ...datos,
  };
}

function embarqueFilial(): ShipmentListItem {
  return {
    id: '3f0c2a1e-0000-4000-8000-000000000777', blNumber: BL_FILIAL, bookingNumber: 'BKG26050777', vessel: 'Santos Express', voyage: '2618S',
    status: 'InTransit', operation: 'IMPORT', country: 'CL', portOfLoading: 'Shanghai', portOfDischarge: 'Valparaíso', etd: '2026-09-01T10:00:00Z',
    eta: '2026-10-20T08:00:00Z', roles: ['Consignee'], accessSource: 'Parent', hasPendingCharges: false,
    originOrganization: { id: FILIAL.id, name: FILIAL.name, taxId: FILIAL.taxId },
  };
}

/** Estado del backend simulado de la Ola I para una página. */
export class SimulacionOlaI {
  private notificaciones = notificacionesIniciales();
  private readonly notificacionesInternas = notificacionesAdmin();
  private preferencias = preferenciasIniciales();
  private comunicados = comunicadosIniciales();
  private readonly historialComunicados = new Map<string, MaintainerChange<AnnouncementSnapshot>[]>();
  private guia: GuideDefinition;
  private guias: GuideDefinition[];
  private sesiones: ImpersonationSession[] = [sesionTerminada()];
  private counter: CounterRecord[] = [
    registroCounter({ id: 'k1', blNumber: BL_COUNTER_ENVIADO, syncStatus: 'Synced', syncedAt: '2026-10-01T15:00:05Z', sourceReference: 'CNT-BO-000830', deconsolidated: true, deconsolidatedAt: '2026-10-02' }),
    registroCounter({ id: 'k2', blNumber: BL_COUNTER_FALLIDO, syncStatus: 'Failed', syncError: 'Nexus: timeout', exchangeDate: '2026-10-04', hblReceived: false, hblReceivedAt: null }),
  ];
  private readonly historialCounter = new Map<string, MaintainerChange<CounterSnapshot>[]>();
  private contactos: ContactListsView;
  private historialContactos: ContactListChange[];
  private transportistas: CarrierPreRegistration[];
  private vinculoPropio: ParentLink | null;
  private vinculosInternos: ParentLink[];
  private readonly matriz: boolean;
  private siguiente = 100;

  constructor(opciones: OpcionesOlaI = {}) {
    this.matriz = !!opciones.matriz;
    if (opciones.impersonando) this.sesiones.unshift(sesionActiva());
    this.guia = guiaCarro(!!opciones.guiaNueva);
    this.guias = [this.guia];
    for (const a of this.comunicados) {
      this.historialComunicados.set(a.id, [{ id: `h-${a.id}`, entityId: a.id, action: 'Created', changedAt: a.createdAt, changedBy: USUARIO_ADMIN.email, previous: null, current: instantaneaComunicado(a) }]);
    }
    for (const r of this.counter) {
      this.historialCounter.set(r.blNumber, [{ id: `h-${r.id}`, entityId: r.id, action: 'Created', changedAt: r.createdAt, changedBy: USUARIO_ADMIN.email, previous: null, current: instantaneaCounter(r) }]);
    }
    this.contactos = {
      organizationId: ORGANIZACION_PRUEBA.id, matchCode: ORGANIZACION_PRUEBA.matchCode, available: !opciones.contactosCaidos, canEdit: true,
      reportTypes: ['ARRIVAL_NOTICE', 'BL_COPIES', 'INVOICES', 'FREE_TIME'],
      lists: opciones.contactosCaidos ? [] : [
        { reportType: 'ARRIVAL_NOTICE', emails: ['avisos@importadoraandes.cl', 'operaciones@importadoraandes.cl'], updatedAt: '2026-09-20T12:00:00Z', updatedBy: USUARIO_PRUEBA.email },
        { reportType: 'BL_COPIES', emails: ['documentos@importadoraandes.cl'] },
        { reportType: 'INVOICES', emails: ['facturas@importadoraandes.cl'] },
        { reportType: 'FREE_TIME', emails: [] },
      ],
      ...(opciones.contactosCaidos ? { errorCode: 'Integration.Unavailable' } : {}),
    };
    this.historialContactos = [{
      id: 'ch1', reportType: 'ARRIVAL_NOTICE', previousEmails: ['avisos@importadoraandes.cl'],
      newEmails: ['avisos@importadoraandes.cl', 'operaciones@importadoraandes.cl'], status: 'Propagated', sourceReference: 'P0060-AN-1',
      changedBy: USUARIO_PRUEBA.email, changedAt: '2026-09-20T12:00:00Z',
    }];
    this.transportistas = [{
      id: 't1000000-0000-4000-8000-000000000001',
      carrier: { id: 'c3000000-0000-4000-8000-000000000001', name: TRANSPORTISTA.name, organizationType: 'Carrier', country: 'CL' },
      legalName: TRANSPORTISTA.name, taxId: TRANSPORTISTA.taxId, email: TRANSPORTISTA.email, country: 'CL', status: 'Pending', durationDays: 90,
      createdAt: '2026-10-04T12:00:00Z', requestedBy: USUARIO_PRUEBA.email, invitationSentAt: '2026-10-04T12:00:00Z',
      assignments: [{ grantId: 'g-ct-1', blNumber: 'HLCUVAL250200456', status: 'PendingActivation', createdAt: '2026-10-04T12:00:00Z' }],
    }];
    this.vinculoPropio = this.matriz ? null : vinculo({
      id: 'l1000000-0000-4000-8000-000000000001', status: 'Active', decidedAt: '2026-10-02T12:00:00Z', decidedBy: USUARIO_ADMIN.email,
      visibilityChangedAt: '2026-10-02T12:00:00Z', visibilityChangedBy: USUARIO_PRUEBA.email,
    });
    this.vinculosInternos = [vinculo({
      id: VINCULO_PENDIENTE, status: 'Pending', requestedAt: '2026-10-06T09:00:00Z', requestedBy: 'demo@altiplano.bo',
      organization: { id: 'c2000000-0000-4000-8000-000000000031', name: 'Comercial Altiplano S.R.L.', organizationType: 'Customer', country: 'BO' },
      notes: 'Somos filial del grupo desde 2024.',
    })];
  }

  private id(prefijo: string): string {
    this.siguiente++;
    return `${prefijo}000000-0000-4000-8000-${String(this.siguiente).padStart(12, '0')}`;
  }

  private bandeja(interno: boolean): NotificationItem[] {
    return interno ? this.notificacionesInternas : this.notificaciones;
  }

  private filtrarBandeja(lista: NotificationItem[], url: URL): NotificationItem[] {
    const p = (k: string) => url.searchParams.get(k) ?? '';
    return lista.filter((n) =>
      (!p('module') || n.module === p('module')) &&
      (!p('type') || n.type === p('type')) &&
      (!p('blNumber') || (n.link?.blNumber ?? '').includes(p('blNumber'))) &&
      (p('onlyUnread') !== 'true' || !n.isRead) &&
      (p('onlyActionable') !== 'true' || !!n.action?.available && ['ApproveJoinRequest', 'ReviewOrganization', 'ReviewParentLink', 'VerifyDepositProof', 'UploadDepositProof'].includes(n.action.type)),
    );
  }

  private noLeidas(lista: NotificationItem[]) {
    const pendientes = lista.filter((n) => !n.isRead);
    const byModule: Record<string, number> = {};
    for (const n of pendientes) byModule[n.module ?? 'General'] = (byModule[n.module ?? 'General'] ?? 0) + 1;
    const resolubles = ['ApproveJoinRequest', 'ReviewOrganization', 'ReviewParentLink', 'VerifyDepositProof', 'UploadDepositProof'];
    return { count: pendientes.length, byModule, actionable: pendientes.filter((n) => !!n.action?.available && resolubles.includes(n.action.type)).length };
  }

  /** Guía para el usuario (sin los campos del mantenedor). */
  private guiaUsuario(g: GuideDefinition): Guide {
    return {
      code: g.code, nameEs: g.nameEs, nameEn: g.nameEn, descriptionEs: g.descriptionEs, descriptionEn: g.descriptionEn, route: g.route,
      version: g.version, steps: g.steps, status: g.status, lastStep: g.lastStep, statusAt: g.statusAt,
    };
  }

  /** Agrega al listado de embarques los BL de la filial visible para la matriz (M1-21) y aplica el filtro por organización. */
  ajustar(ruta: string, url: URL, cuerpoRespuesta: unknown): unknown {
    if (!this.matriz || ruta !== 'shipments') return cuerpoRespuesta;
    const r = cuerpoRespuesta as PagedResult<ShipmentListItem>;
    const organizacion = url.searchParams.get('organizationId');
    const propios = organizacion && organizacion !== ORGANIZACION_PRUEBA.id ? [] : r.items;
    const filial = !organizacion || organizacion === FILIAL.id ? [embarqueFilial()] : [];
    const items = [...propios, ...filial];
    return { ...r, items, total: items.length };
  }

  async responder(route: Route, ruta: string, metodo: string, url: URL): Promise<boolean> {
    const request = route.request();
    const autorizacion = request.headers()['authorization'] ?? '';
    const impersonando = autorizacion.includes(TOKEN_IMPERSONACION);
    const interno = permisos(request).includes('shipments.view-all');

    // Vista como cliente (M8-08): solo lectura, salvo terminar la sesión o cerrar sesión.
    if (impersonando) {
      const sesion = this.sesiones.find((s) => s.status === 'Active');
      if (ruta === 'impersonation/current' && metodo === 'GET') {
        if (!sesion) await problema(route, 401, 'Impersonation.Ended', 'The impersonation session ended.');
        else await json(route, 200, sesion);
        return true;
      }
      if ((ruta === 'impersonation/end' || ruta === 'auth/logout') && metodo === 'POST') {
        if (sesion) {
          Object.assign(sesion, { status: 'Ended', endedAt: new Date().toISOString(), endReason: ruta === 'auth/logout' ? 'Logout' : 'Manual', durationSeconds: 300, requestCount: sesion.requestCount + 1 });
        }
        await json(route, 200, sesion ?? sesionTerminada());
        return true;
      }
      if (metodo !== 'GET') {
        if (sesion) sesion.blockedCount++;
        await problema(route, 403, 'Impersonation.ReadOnly', 'Impersonation sessions are read only.');
        return true;
      }
      if (sesion) sesion.requestCount++;
    }

    // Registro con una cuenta pre-creada (M1-09).
    if ((ruta === 'auth/register' || ruta === 'auth/register/join') && metodo === 'POST') {
      const datos = cuerpo(request) as { email?: string; taxId?: string } | null;
      const rut = (datos?.taxId ?? '').replace(/[.\s]/g, '').toUpperCase();
      if (datos?.email?.toLowerCase() === TRANSPORTISTA.email || rut === TRANSPORTISTA.taxId) {
        await problema(route, 409, 'Registration.PreCreatedAccountExists', 'A pre-created account exists for this email or tax ID.');
        return true;
      }
      return false;
    }
    if (ruta === 'auth/register/pre-created/resend-invitation' && metodo === 'POST') {
      await json(route, 202);
      return true;
    }

    // Bandeja (M1-25)
    if (ruta === 'notifications' && metodo === 'GET') {
      await json(route, 200, this.filtrarBandeja(this.bandeja(interno), url));
      return true;
    }
    if (ruta === 'notifications/unread-count' && metodo === 'GET') {
      await json(route, 200, this.noLeidas(this.bandeja(interno)));
      return true;
    }
    const leida = /^notifications\/([^/]+)\/read$/.exec(ruta);
    if (leida && metodo === 'POST') {
      const n = this.bandeja(interno).find((i) => i.id === leida[1]);
      if (n) Object.assign(n, { isRead: true, readAt: AHORA });
      await json(route, n ? 204 : 404);
      return true;
    }
    if (ruta === 'notifications/read-all' && metodo === 'POST') {
      const modulo = url.searchParams.get('module');
      const marcar = this.bandeja(interno).filter((n) => !n.isRead && (!modulo || n.module === modulo));
      for (const n of marcar) Object.assign(n, { isRead: true, readAt: AHORA });
      await json(route, 200, { marked: marcar.length });
      return true;
    }
    if (ruta === 'notifications/preferences') {
      if (metodo === 'PUT') {
        const { items } = cuerpo(request) as { items: { type: string; emailEnabled: boolean | null }[] };
        for (const i of items) {
          const p = this.preferencias.find((x) => x.type === i.type);
          if (!p) continue;
          if (!p.emailAvailable || p.emailMandatory) {
            await problema(route, 400, p.emailMandatory ? 'Notification.EmailMandatory' : 'Notification.EmailNotAvailable', 'Locked.');
            return true;
          }
          p.emailEnabled = i.emailEnabled ?? p.emailDefault;
          p.isCustomized = i.emailEnabled !== null;
        }
      }
      await json(route, 200, this.preferencias);
      return true;
    }
    // Acciones desde la bandeja: la solicitud de vinculación queda resuelta (M1-08).
    const vinculacion = /^organizations\/me\/join-requests\/([^/]+)\/(approve|reject)$/.exec(ruta);
    if (vinculacion && metodo === 'POST') {
      for (const n of this.notificaciones) {
        if (n.action?.type === 'ApproveJoinRequest' && n.action.targetId === vinculacion[1]) n.action = { ...n.action, available: false, resolvedAt: AHORA };
      }
      await json(route, 200, { userId: vinculacion[1], status: vinculacion[2] === 'approve' ? 'Active' : 'Rejected' });
      return true;
    }

    // Comunicados (M1-26)
    if (ruta === 'announcements' && metodo === 'GET') {
      const pais = url.searchParams.get('country') || 'CL';
      const operacion = url.searchParams.get('operation');
      const vigentes = this.comunicados.filter((a) => a.status === 'Published' && a.countries.includes(pais as 'CL' | 'BO') &&
        (!operacion || a.operation === operacion || a.operation === 'Both'));
      await json(route, 200, vigentes.map((a) => ({ ...a, publishedAt: a.publishedAt ?? AHORA })));
      return true;
    }
    if (ruta === 'admin/announcements') {
      if (metodo === 'GET') {
        const estado = url.searchParams.get('status');
        const pais = url.searchParams.get('country');
        await json(route, 200, this.comunicados.filter((a) => (!estado || a.status === estado) && (!pais || a.countries.includes(pais as 'CL' | 'BO'))));
        return true;
      }
      if (metodo === 'POST') {
        const datos = cuerpo(request) as AnnouncementRequest;
        const nuevo = comunicado({
          ...datos, id: this.id('a'), status: datos.publish ? 'Published' : 'Draft', severity: datos.severity ?? 'Info', createdAt: AHORA,
          publishedAt: datos.publish ? AHORA : null, publishedBy: datos.publish ? USUARIO_ADMIN.email : null,
        });
        this.comunicados.unshift(nuevo);
        this.historialComunicados.set(nuevo.id, [{ id: `h-${nuevo.id}`, entityId: nuevo.id, action: 'Created', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: null, current: instantaneaComunicado(nuevo) }]);
        await json(route, 201, nuevo);
        return true;
      }
    }
    const adminComunicado = /^admin\/announcements\/([^/]+)(?:\/(publish|unpublish|history))?$/.exec(ruta);
    if (adminComunicado) {
      const a = this.comunicados.find((x) => x.id === adminComunicado[1]);
      if (!a) {
        await problema(route, 404, 'Announcement.NotFound', 'Not found.');
        return true;
      }
      const accion = adminComunicado[2];
      if (accion === 'history') {
        await json(route, 200, this.historialComunicados.get(a.id) ?? []);
        return true;
      }
      const antes = instantaneaComunicado(a);
      if (metodo === 'DELETE') {
        this.comunicados = this.comunicados.filter((x) => x.id !== a.id);
        await json(route, 204);
        return true;
      }
      if (accion === 'publish') {
        if (a.status === 'Published') {
          await problema(route, 400, 'Announcement.InvalidTransition', 'Already published.');
          return true;
        }
        Object.assign(a, { status: 'Published', isCurrent: true, publishedAt: AHORA, publishedBy: USUARIO_ADMIN.email, notifiedAt: a.notifyOnPublish ? AHORA : null });
      } else if (accion === 'unpublish') {
        Object.assign(a, { status: 'Unpublished', isCurrent: false, unpublishedAt: AHORA, unpublishedBy: USUARIO_ADMIN.email });
      } else if (metodo === 'PUT') {
        Object.assign(a, cuerpo(request) as AnnouncementRequest, { modifiedAt: AHORA, modifiedBy: USUARIO_ADMIN.email });
      } else if (metodo === 'GET') {
        await json(route, 200, a);
        return true;
      }
      this.historialComunicados.get(a.id)?.unshift({
        id: this.id('h'), entityId: a.id, action: accion === 'publish' ? 'Published' : accion === 'unpublish' ? 'Unpublished' : 'Updated',
        changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: antes, current: instantaneaComunicado(a),
      });
      await json(route, 200, a);
      return true;
    }

    // Guías (M1-27)
    if (ruta === 'guides' && metodo === 'GET') {
      const pantalla = url.searchParams.get('route');
      await json(route, 200, this.guias.filter((g) => g.isActive && (!pantalla || g.route === pantalla)).map((g) => this.guiaUsuario(g)));
      return true;
    }
    const estadoGuia = /^guides\/([^/]+)\/state$/.exec(ruta);
    if (estadoGuia && metodo === 'PUT') {
      const g = this.guias.find((x) => x.code === estadoGuia[1]);
      const datos = cuerpo(request) as { status: 'Completed' | 'Dismissed' | 'Reset'; lastStep?: number };
      if (g) Object.assign(g, datos.status === 'Reset' ? { status: null, lastStep: null, statusAt: null } : { status: datos.status, lastStep: datos.lastStep ?? null, statusAt: AHORA });
      await json(route, g ? 200 : 404, g ? this.guiaUsuario(g) : undefined);
      return true;
    }
    if (ruta === 'admin/guides') {
      if (metodo === 'GET') {
        await json(route, 200, this.guias);
        return true;
      }
      if (metodo === 'POST') {
        const datos = cuerpo(request) as GuideRequest;
        if (this.guias.some((g) => g.code === datos.code)) {
          await problema(route, 409, 'Guide.AlreadyExists', 'Exists.');
          return true;
        }
        const nueva: GuideDefinition = { ...datos, code: datos.code ?? 'nueva', id: this.id('g'), version: 1, createdAt: AHORA, createdBy: USUARIO_ADMIN.email };
        this.guias.push(nueva);
        await json(route, 201, nueva);
        return true;
      }
    }
    const adminGuia = /^admin\/guides\/([^/]+)(\/history)?$/.exec(ruta);
    if (adminGuia) {
      const g = this.guias.find((x) => x.id === adminGuia[1]);
      if (adminGuia[2]) {
        const snapshot = (x: GuideDefinition): GuideSnapshot => ({ code: x.code, nameEs: x.nameEs, nameEn: x.nameEn, route: x.route, audience: x.audience, isActive: x.isActive, displayOrder: x.displayOrder, version: x.version });
        await json(route, 200, g ? [{ id: 'hg1', entityId: g.id, action: 'Created', changedAt: g.createdAt, changedBy: USUARIO_ADMIN.email, previous: null, current: snapshot(g) }] : []);
        return true;
      }
      if (metodo === 'DELETE') {
        this.guias = this.guias.filter((x) => x.id !== adminGuia[1]);
        await json(route, 204);
        return true;
      }
      if (metodo === 'PUT' && g) {
        Object.assign(g, cuerpo(request) as GuideRequest, { version: g.version + 1, status: null, modifiedAt: AHORA, modifiedBy: USUARIO_ADMIN.email });
        await json(route, 200, g);
        return true;
      }
    }

    // Área de administración (M8-05)
    if (ruta === 'admin/overview' && metodo === 'GET') {
      const overview: AdminOverview = {
        generatedAt: AHORA,
        sections: [
          { code: 'organizations', permission: 'organizations.review', counters: { pendingValidation: 1, pendingArCheck: 1, preCreatedCarriers: this.transportistas.length } },
          { code: 'parent-links', permission: 'organizations.review', counters: { pending: this.vinculosInternos.filter((l) => l.status === 'Pending').length, active: 1 } },
          { code: 'access-matrix', permission: 'access-matrix.manage' },
          { code: 'maintainers', permission: 'maintainers.manage', counters: { tariffs: 12, serviceDefinitions: 9 } },
          { code: 'guides', permission: 'maintainers.manage', counters: { active: this.guias.filter((g) => g.isActive).length } },
          { code: 'payment-blocks', permission: 'payment-blocks.manage', counters: { scheduled: 1 } },
          { code: 'finance', permission: 'payments.finance', counters: { depositProofsToVerify: 1, paymentsPendingVerification: 0, stuckPostPaymentJobs: 0 } },
          { code: 'service-requests', permission: 'service-requests.process', counters: { pendingApproval: 2, inProgress: 1 } },
          { code: 'announcements', permission: 'announcements.manage', counters: { current: this.comunicados.filter((a) => a.status === 'Published').length, drafts: this.comunicados.filter((a) => a.status === 'Draft').length } },
          { code: 'counter', permission: 'counter.manage', counters: { records: this.counter.length, syncFailed: this.counter.filter((r) => r.syncStatus === 'Failed').length } },
          { code: 'reports', permission: 'transactions-report.view' },
          { code: 'impersonation', permission: 'impersonation.use', counters: { activeSessions: this.sesiones.filter((s) => s.status === 'Active').length } },
          { code: 'audit', permission: 'audit.view' },
          { code: 'users', permission: 'users.manage' },
        ],
      };
      await json(route, 200, overview);
      return true;
    }

    // Vista como cliente (M8-08), con la sesión del administrador
    if (ruta === 'admin/impersonation/targets' && metodo === 'GET') {
      const objetivos: ImpersonationTarget[] = [
        { userId: SUJETO.userId, email: SUJETO.email, fullName: SUJETO.fullName, profile: 'OrgAdmin', isActive: true, membershipStatus: 'Active', eligible: true, lastLoginAt: '2026-10-05T12:30:00Z' },
        { userId: 'u0000000-0000-4000-8000-000000000002', email: 'consulta@importadoraandes.cl', fullName: 'Diego Paredes', profile: 'OrgViewer', isActive: false, membershipStatus: 'Active', eligible: false, lastLoginAt: null },
      ];
      await json(route, 200, url.searchParams.get('organizationId') === ORGANIZACION_PRUEBA.id ? objetivos : []);
      return true;
    }
    if (ruta === 'admin/impersonation/sessions') {
      if (metodo === 'POST') {
        const datos = cuerpo(request) as { organizationId: string; userId: string; reason: string };
        for (const s of this.sesiones) if (s.status === 'Active') Object.assign(s, { status: 'Ended', endReason: 'Replaced', endedAt: AHORA });
        const inicio = new Date();
        const sesion: ImpersonationSession = {
          id: this.id('i'), actor: ACTOR, subject: SUJETO, organization: ORG_SESION, reason: datos.reason, status: 'Active',
          startedAt: inicio.toISOString(), expiresAt: new Date(inicio.getTime() + 30 * 60_000).toISOString(), requestCount: 0, blockedCount: 0,
          readOnly: true, allowedActions: [],
        };
        this.sesiones.unshift(sesion);
        await json(route, 201, {
          auth: { token: TOKEN_IMPERSONACION, expiresIn: 30, user: USUARIO_PRUEBA, organization: ORGANIZACION_PRUEBA },
          session: sesion,
        });
        return true;
      }
      const estado = url.searchParams.get('status');
      await json(route, 200, paginar(this.sesiones.filter((s) => !estado || s.status === estado), url));
      return true;
    }
    const sesionAdmin = /^admin\/impersonation\/sessions\/([^/]+)\/(requests|end)$/.exec(ruta);
    if (sesionAdmin) {
      const s = this.sesiones.find((x) => x.id === sesionAdmin[1]);
      if (sesionAdmin[2] === 'requests') {
        await json(route, 200, paginar(s ? solicitudesSesion(s.id) : [], url, 50));
        return true;
      }
      if (!s || s.status !== 'Active') {
        await problema(route, 400, 'Impersonation.NotActive', 'Not active.');
        return true;
      }
      Object.assign(s, { status: 'Ended', endReason: 'Admin', endedAt: AHORA });
      await json(route, 200, s);
      return true;
    }

    // Reportería (M9-01)
    if (ruta === 'admin/reports/transactions' && metodo === 'GET') {
      await json(route, 200, reporteTransacciones(url));
      return true;
    }
    if (ruta === 'admin/reports/exceptions' && metodo === 'GET') {
      await json(route, 200, reporteExcepciones(url));
      return true;
    }
    if (/^admin\/reports\/(transactions|exceptions)\/export$/.test(ruta) && metodo === 'GET') {
      const csv = url.searchParams.get('format') === 'csv';
      await route.fulfill({
        status: 200,
        contentType: csv ? 'text/csv' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        body: csv ? '﻿paymentNumber,service,total\r\nPAY-20261002-A1B2C3D4,THC,214200\r\n' : Buffer.from('PK\u0003\u0004 reporte de prueba'),
      });
      return true;
    }

    // Counter (M8-09)
    if (ruta === 'admin/counter' && metodo === 'GET') {
      const p = (k: string) => url.searchParams.get(k) ?? '';
      const lista = this.counter.filter((r) =>
        r.blNumber.includes(p('blNumber').toUpperCase()) && (!p('country') || r.country === p('country')) &&
        (!p('syncStatus') || r.syncStatus === p('syncStatus')) && (!p('deconsolidated') || String(r.deconsolidated) === p('deconsolidated')));
      await json(route, 200, paginar(lista, url));
      return true;
    }
    const counterBl = /^admin\/counter\/([^/]+?)(?:\/(sync|history))?$/.exec(ruta);
    if (counterBl) {
      const bl = decodeURIComponent(counterBl[1]);
      let registro = this.counter.find((r) => r.blNumber === bl);
      if (counterBl[2] === 'history') {
        await json(route, 200, this.historialCounter.get(bl) ?? []);
        return true;
      }
      if (counterBl[2] === 'sync' && metodo === 'POST') {
        if (!registro) {
          await problema(route, 404, 'BillOfLading.NotFound', 'Not found.');
          return true;
        }
        if (registro.syncStatus === 'Synced') {
          await problema(route, 400, 'Counter.AlreadySynced', 'Already synced.');
          return true;
        }
        Object.assign(registro, { syncStatus: 'Synced', syncedAt: AHORA, syncError: null, sourceReference: 'CNT-BO-000045' });
        await json(route, 200, registro);
        return true;
      }
      if (metodo === 'PUT') {
        const datos = cuerpo(request) as CounterRecordRequest;
        const antes = registro ? instantaneaCounter(registro) : null;
        if (!registro) {
          registro = registroCounter({ id: this.id('k'), blNumber: bl, syncStatus: 'Pending' });
          this.counter.unshift(registro);
        }
        Object.assign(registro, datos, {
          hblReceivedAt: datos.hblReceived ? datos.hblReceivedAt ?? '2026-10-06' : null,
          deconsolidatedAt: datos.deconsolidated ? datos.deconsolidatedAt ?? '2026-10-06' : null,
          syncStatus: 'Synced', syncedAt: AHORA, syncError: null, sourceReference: registro.sourceReference ?? 'CNT-BO-000900',
          modifiedAt: AHORA, modifiedBy: USUARIO_ADMIN.email,
        });
        const historial = this.historialCounter.get(bl) ?? [];
        historial.unshift({ id: this.id('h'), entityId: registro.id, action: antes ? 'Updated' : 'Created', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: antes, current: instantaneaCounter(registro) });
        this.historialCounter.set(bl, historial);
        await json(route, 200, registro);
        return true;
      }
      if (metodo === 'GET') {
        const detalle: CounterDetail = {
          shipment: { id: `bl-${bl}`, blNumber: bl, bookingNumber: 'BKG26100045', country: 'BO', operation: 'IMPORT', vessel: 'Arica Express', voyage: '2610N', portOfDischarge: 'Arica', placeOfDelivery: 'La Paz' },
          record: registro ?? null,
          sourceAvailable: bl !== BL_COUNTER_FALLIDO,
          source: bl !== BL_COUNTER_FALLIDO ? { blNumber: bl, country: 'BO', exchangeDate: registro?.exchangeDate ?? null, hblReceived: registro?.hblReceived ?? false, deconsolidated: registro?.deconsolidated ?? false, sourceReference: registro?.sourceReference ?? null } : null,
          sourceErrorCode: bl === BL_COUNTER_FALLIDO ? 'Integration.Timeout' : null,
        };
        await json(route, 200, detalle);
        return true;
      }
    }

    // Listas de distribución (M1-06)
    if (ruta === 'organizations/me/contact-lists' && metodo === 'GET') {
      await json(route, 200, this.contactos);
      return true;
    }
    if (ruta === 'organizations/me/contact-lists/history' && metodo === 'GET') {
      const tipo = url.searchParams.get('reportType');
      await json(route, 200, this.historialContactos.filter((h) => !tipo || h.reportType === tipo));
      return true;
    }
    const lista = /^organizations\/me\/contact-lists\/([^/]+)$/.exec(ruta);
    if (lista && metodo === 'PUT') {
      const tipo = lista[1];
      const emails = [...new Set(((cuerpo(request) as { emails: string[] }).emails ?? []).map((e) => e.toLowerCase()))];
      const actual = this.contactos.lists.find((l) => l.reportType === tipo);
      const anterior = actual?.emails ?? [];
      const nueva = { reportType: tipo, emails, updatedAt: AHORA, updatedBy: USUARIO_PRUEBA.email };
      this.contactos = { ...this.contactos, lists: [...this.contactos.lists.filter((l) => l.reportType !== tipo), nueva] };
      this.historialContactos.unshift({ id: this.id('c'), reportType: tipo, previousEmails: anterior, newEmails: emails, status: 'Propagated', sourceReference: `P0060-${tipo}`, changedBy: USUARIO_PRUEBA.email, changedAt: AHORA });
      await json(route, 200, nueva);
      return true;
    }

    // Transportistas pre-creados (M1-09)
    if (ruta === 'organizations/me/carriers') {
      if (metodo === 'GET') {
        await json(route, 200, this.transportistas);
        return true;
      }
      if (metodo === 'POST') {
        const datos = cuerpo(request) as CarrierPreRegistrationRequest;
        const existente = this.transportistas.find((t) => t.taxId === datos.taxId || t.email === datos.email);
        const asignaciones = (datos.blNumbers ?? []).filter((b) => b !== 'HLCU9999999').map((b) => ({ grantId: this.id('g'), blNumber: b, status: 'PendingActivation', createdAt: AHORA }));
        const omitidos = (datos.blNumbers ?? []).filter((b) => b === 'HLCU9999999').map((b) => ({ reference: b, code: 'BillOfLading.NotFound', message: 'Not found.' }));
        const registro: CarrierPreRegistration = existente ?? {
          id: this.id('t'), carrier: { id: this.id('c'), name: datos.legalName, organizationType: 'Carrier', country: datos.country ?? 'CL' },
          legalName: datos.legalName, taxId: datos.taxId, email: datos.email, country: datos.country ?? 'CL', status: 'Pending',
          durationDays: datos.durationDays ?? null, createdAt: AHORA, requestedBy: USUARIO_PRUEBA.email, invitationSentAt: AHORA, assignments: [],
        };
        registro.assignments = [...registro.assignments, ...asignaciones];
        if (!existente) this.transportistas.push(registro);
        await json(route, existente ? 200 : 201, { preRegistration: registro, created: !existente, invitationSent: !existente, assigned: asignaciones.length, skipped: omitidos });
        return true;
      }
    }
    const asignacion = /^organizations\/me\/carriers\/([^/]+)\/assignments$/.exec(ruta);
    if (asignacion && metodo === 'POST') {
      const t = this.transportistas.find((x) => x.id === asignacion[1]);
      const datos = cuerpo(request) as { blNumbers?: string[]; bookingNumbers?: string[] };
      const nuevas = [
        ...(datos.blNumbers ?? []).map((b) => ({ grantId: this.id('g'), blNumber: b, status: 'PendingActivation', createdAt: AHORA })),
        ...(datos.bookingNumbers ?? []).map((b) => ({ grantId: this.id('g'), bookingNumber: b, status: 'PendingActivation', createdAt: AHORA })),
      ];
      if (!t) {
        await problema(route, 404, 'CarrierPreCreation.NotFound', 'Not found.');
        return true;
      }
      t.assignments = [...t.assignments, ...nuevas];
      await json(route, 200, { preRegistration: t, created: false, invitationSent: false, assigned: nuevas.length, skipped: [] });
      return true;
    }

    // Empresa matriz (M1-21)
    if (ruta === 'organizations/me/parent-company') {
      if (metodo === 'GET') {
        const subsidiaries = this.matriz ? [vinculo({
          id: 'l1000000-0000-4000-8000-000000000009', status: 'Active',
          organization: { id: FILIAL.id, name: FILIAL.name, organizationType: 'Customer', country: 'CL' },
          parent: { id: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, organizationType: 'Customer', country: 'CL' },
        })] : [];
        await json(route, 200, { link: this.vinculoPropio, subsidiaries, canManage: true });
        return true;
      }
      if (metodo === 'POST') {
        const datos = cuerpo(request) as { parentOrganizationId: string; enableVisibility: boolean; notes?: string };
        this.vinculoPropio = vinculo({ id: this.id('l'), status: 'Pending', visibilityEnabled: datos.enableVisibility, notes: datos.notes ?? null, requestedAt: AHORA });
        await json(route, 201, this.vinculoPropio);
        return true;
      }
      if (metodo === 'DELETE') {
        if (this.vinculoPropio) Object.assign(this.vinculoPropio, { status: 'Removed', endedAt: AHORA, endedBy: USUARIO_PRUEBA.email });
        await json(route, 204);
        return true;
      }
    }
    if (ruta === 'organizations/me/parent-company/visibility' && metodo === 'PUT') {
      const { enabled } = cuerpo(request) as { enabled: boolean };
      if (this.vinculoPropio) Object.assign(this.vinculoPropio, { visibilityEnabled: enabled, visibilityChangedAt: AHORA, visibilityChangedBy: USUARIO_PRUEBA.email });
      await json(route, 200, this.vinculoPropio);
      return true;
    }
    if (ruta === 'organizations/me/parent-company/candidates' && metodo === 'GET') {
      const texto = (url.searchParams.get('search') ?? '').toLowerCase();
      const candidatos: ParentCandidate[] = [{ id: MATRIZ_CANDIDATA.id, name: MATRIZ_CANDIDATA.name, taxId: MATRIZ_CANDIDATA.taxId, organizationType: 'Customer', country: 'CL' }];
      await json(route, 200, candidatos.filter((c) => c.name.toLowerCase().includes(texto) || c.taxId.includes(texto)));
      return true;
    }
    if (ruta === 'admin/organization-links' && metodo === 'GET') {
      const estado = url.searchParams.get('status');
      await json(route, 200, this.vinculosInternos.filter((l) => !estado || l.status === estado));
      return true;
    }
    const decision = /^admin\/organization-links\/([^/]+)\/(approve|reject)$/.exec(ruta);
    if (decision && metodo === 'POST') {
      const l = this.vinculosInternos.find((x) => x.id === decision[1]);
      if (!l || l.status !== 'Pending') {
        await problema(route, 400, 'ParentLink.NotPending', 'Not pending.');
        return true;
      }
      const datos = cuerpo(request) as { notes?: string; reason?: string } | null;
      Object.assign(l, { status: decision[2] === 'approve' ? 'Active' : 'Rejected', decidedAt: AHORA, decidedBy: USUARIO_ADMIN.email, decisionNotes: datos?.reason ?? datos?.notes ?? null });
      for (const n of this.notificacionesInternas) {
        if (n.action?.targetId === l.id) n.action = { ...n.action, available: false, resolvedAt: AHORA };
      }
      await json(route, 200, l);
      return true;
    }

    // Detalle del BL de la filial para la matriz (M1-21) y Counter en el detalle para internos (M8-09).
    if (this.matriz && ruta === `shipments/${BL_FILIAL}` && metodo === 'GET') {
      const e = embarqueFilial();
      const detalle: ShipmentDetail = {
        id: e.id, blNumber: e.blNumber, bookingNumber: e.bookingNumber, operation: e.operation, status: e.status, country: e.country,
        vessel: e.vessel, voyage: e.voyage, portOfLoading: e.portOfLoading, portOfDischarge: e.portOfDischarge, placeOfDelivery: 'Santiago',
        etd: e.etd, eta: e.eta, shipper: 'Shanghai Exports Co.', consignee: FILIAL.name, roles: ['Consignee'], accessSource: 'Parent',
        allowedActions: ['shipment.view'], canOperate: false, canSelfAssociate: false, requiresAssociationForPayment: false, freight: null,
        containers: [], localCharges: null, demurrageCharges: null, serviceOrders: [], originOrganization: e.originOrganization,
      };
      await json(route, 200, detalle);
      return true;
    }
    return false;
  }

  /** Registro de Counter en el detalle del BL para usuarios internos (M8-09). */
  ajustarDetalle(ruta: string, request: Request, cuerpoRespuesta: unknown): unknown {
    const m = /^shipments\/([^/]+)$/.exec(ruta);
    if (!m || !permisos(request).includes('shipments.view-all') || !cuerpoRespuesta || typeof cuerpoRespuesta !== 'object') return cuerpoRespuesta;
    const registro = this.counter.find((r) => r.blNumber === m[1]) ?? (m[1] === 'HLCU0000001' ? this.counter[0] : null);
    return registro ? { ...(cuerpoRespuesta as object), counter: { ...registro, blNumber: m[1] } } : cuerpoRespuesta;
  }
}
