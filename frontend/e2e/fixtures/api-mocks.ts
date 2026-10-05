import { Page, Route } from '@playwright/test';
import type {
  BillOfLading,
  BLChargesResponse,
  DemurrageCharge,
  LocalCharge,
} from '../../src/app/core/models/bl.model';
import type { NotificationItem } from '../../src/app/core/models/notification.model';
import type { Payment } from '../../src/app/core/models/payment.model';
import type { Receipt } from '../../src/app/core/services/receipt.service';
import type { PagedResult } from '../../src/app/core/models/admin-user.model';
import type { ShipmentDetail, ShipmentListItem } from '../../src/app/core/models/shipment.model';
import type {
  AdminOrganizationDetail,
  AdminOrganizationItem,
  JoinRequest,
  OrganizationDocument,
  OrganizationUser,
} from '../../src/app/core/models/organization.model';
import type { AccessMatrixAction, AccessMatrixLevel } from '../../src/app/core/models/access-matrix.model';
import { ORGANIZACION_PRUEBA, USUARIO_PRUEBA } from './session';

/** Datos ficticios y deterministas para las pantallas recorridas por las pruebas. */
export const BL_PRUEBA: BillOfLading = {
  id: '3f0c2a1e-0000-4000-8000-000000000001',
  blNumber: 'HLCU0000001',
  type: 'IMPORT',
  vessel: 'Valparaiso Express',
  voyage: '2614W',
  portOfLoading: 'Hamburg',
  portOfDischarge: 'San Antonio',
  placeOfDelivery: 'Santiago',
  etd: '2026-09-20T10:00:00Z',
  eta: '2026-10-18T08:00:00Z',
  status: 'IN_TRANSIT',
  freightAmount: 2450,
  freightCurrency: 'USD',
  freightStatus: 'PENDING',
  country: 'CL',
  clientId: USUARIO_PRUEBA.id,
  clientName: USUARIO_PRUEBA.name,
  containers: [
    {
      id: 'c0000000-0000-4000-8000-000000000001',
      containerNumber: 'HLXU1234567',
      type: 'DRY',
      size: '40',
      sealNumber: 'SL-889912',
      weight: 18250,
      packages: 320,
      description: 'Repuestos industriales',
    },
  ],
  createdAt: '2026-09-18T15:30:00Z',
};

const CARGOS_LOCALES: LocalCharge[] = [
  {
    id: 'a0000000-0000-4000-8000-000000000001',
    blId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    chargeCode: 'THC',
    description: 'Terminal Handling Charge',
    amount: 180,
    currency: 'USD',
    taxAmount: 34.2,
    totalAmount: 214.2,
    status: 'PENDING',
    country: 'CL',
  },
];

const DEMURRAGE: DemurrageCharge[] = [
  {
    id: 'd0000000-0000-4000-8000-000000000001',
    blId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    containerNumber: 'HLXU1234567',
    freeDays: 7,
    demurrageDays: 2,
    dailyRate: 85,
    currency: 'USD',
    totalAmount: 170,
    status: 'PENDING',
    isExempt: false,
    country: 'CL',
  },
];

const CARGOS_BL: BLChargesResponse = {
  blNumber: BL_PRUEBA.blNumber,
  localCharges: CARGOS_LOCALES,
  demurrageCharges: DEMURRAGE,
};

const PAGOS: Payment[] = [
  {
    id: 'p0000000-0000-4000-8000-000000000001',
    paymentNumber: 'PAY-CL-2026-000123',
    type: 'Freight',
    method: 'BankTransfer',
    amount: 2450,
    taxAmount: 0,
    totalAmount: 2450,
    currency: 'USD',
    status: 'CONFIRMED',
    blNumber: BL_PRUEBA.blNumber,
    blId: BL_PRUEBA.id,
    clientId: USUARIO_PRUEBA.id,
    clientName: USUARIO_PRUEBA.name,
    country: 'CL',
    createdAt: '2026-09-25T13:10:00Z',
    confirmedAt: '2026-09-25T13:12:00Z',
    details: [],
  },
  {
    id: 'p0000000-0000-4000-8000-000000000002',
    paymentNumber: 'PAY-CL-2026-000124',
    type: 'LocalCharges',
    method: 'Khipu',
    amount: 180,
    taxAmount: 34.2,
    totalAmount: 214.2,
    currency: 'USD',
    status: 'PENDING',
    blNumber: BL_PRUEBA.blNumber,
    blId: BL_PRUEBA.id,
    clientId: USUARIO_PRUEBA.id,
    clientName: USUARIO_PRUEBA.name,
    country: 'CL',
    createdAt: '2026-10-01T09:45:00Z',
    details: [],
  },
  {
    // Monto en CLP (sin decimales, Q8) para la prueba de cambio de idioma.
    id: 'p0000000-0000-4000-8000-000000000003',
    paymentNumber: 'PAY-CL-2026-000125',
    type: 'LocalCharges',
    method: 'BankTransfer',
    amount: 1037451,
    taxAmount: 197116,
    totalAmount: 1234567,
    currency: 'CLP',
    status: 'CONFIRMED',
    blNumber: BL_PRUEBA.blNumber,
    blId: BL_PRUEBA.id,
    clientId: USUARIO_PRUEBA.id,
    clientName: USUARIO_PRUEBA.name,
    country: 'CL',
    createdAt: '2026-10-02T15:20:00Z',
    confirmedAt: '2026-10-02T15:22:00Z',
    details: [],
  },
];

const NOTIFICACIONES: NotificationItem[] = [
  {
    id: 'n0000000-0000-4000-8000-000000000001',
    type: 'PAYMENT_CONFIRMED',
    title: 'Pago confirmado',
    body: 'El pago PAY-CL-2026-000123 fue confirmado.',
    isRead: false,
    createdAt: '2026-09-25T13:12:00Z',
  },
];

const RECIBOS: Receipt[] = [
  {
    id: 'r0000000-0000-4000-8000-000000000001',
    receiptNumber: 'REC-CL-2026-000045',
    paymentId: PAGOS[0].id,
    paymentNumber: PAGOS[0].paymentNumber,
    amount: 2450,
    taxAmount: 0,
    totalAmount: 2450,
    currency: 'USD',
    clientName: USUARIO_PRUEBA.name,
    clientTaxId: USUARIO_PRUEBA.taxId,
    country: 'CL',
    issuedAt: '2026-09-25T13:15:00Z',
  },
];

// ---------------------------------------------------------------------------
// Fase 1, Ola A: embarques (M2-06, M2-07), organización (M1-02, M1-04, M1-07, M1-08),
// bandeja interna de organizaciones (M8-04) y matriz de accesos (M1-11).
// ---------------------------------------------------------------------------

/** BL de exportación con órdenes de servicio (CL-EXP-13). */
export const BL_EXPORTACION = 'HLCUSAI260300610';
/** BL donde la organización es solo shipper: la matriz no le muestra el flete (bloque null). */
export const BL_SIN_FLETE = 'HLCUVAP260300720';

const EMBARQUES: ShipmentListItem[] = [
  {
    id: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    bookingNumber: 'BKG26030010',
    vessel: BL_PRUEBA.vessel,
    voyage: BL_PRUEBA.voyage,
    status: 'InTransit',
    operation: 'IMPORT',
    country: 'CL',
    portOfLoading: BL_PRUEBA.portOfLoading,
    portOfDischarge: BL_PRUEBA.portOfDischarge,
    etd: BL_PRUEBA.etd,
    eta: BL_PRUEBA.eta,
    roles: ['Customer', 'Consignee'],
    accessSource: 'Own',
    hasPendingCharges: true,
  },
  {
    id: '3f0c2a1e-0000-4000-8000-000000000006',
    blNumber: BL_EXPORTACION,
    bookingNumber: 'BKG26030061',
    vessel: 'Santos Express',
    voyage: '2615E',
    status: 'GateIn',
    operation: 'EXPORT',
    country: 'CL',
    portOfLoading: 'San Antonio',
    portOfDischarge: 'Rotterdam',
    etd: '2026-10-12T10:00:00Z',
    eta: '2026-11-10T08:00:00Z',
    roles: ['Customer', 'Shipper'],
    accessSource: 'Own',
    hasPendingCharges: false,
  },
  {
    id: '3f0c2a1e-0000-4000-8000-000000000007',
    blNumber: BL_SIN_FLETE,
    bookingNumber: 'BKG26030072',
    vessel: 'Valparaiso Express',
    voyage: '2616E',
    status: 'OnBoard',
    operation: 'EXPORT',
    country: 'CL',
    portOfLoading: 'Valparaiso',
    portOfDischarge: 'Hamburg',
    etd: '2026-10-08T10:00:00Z',
    eta: '2026-11-05T08:00:00Z',
    roles: ['Shipper'],
    accessSource: 'Own',
    hasPendingCharges: false,
  },
  {
    id: '3f0c2a1e-0000-4000-8000-000000000008',
    blNumber: 'HLCUARI260300830',
    bookingNumber: 'BKG26030083',
    vessel: 'Arica Bridge',
    voyage: '2617W',
    status: 'Arrived',
    operation: 'IMPORT',
    country: 'BO',
    portOfLoading: 'Shanghai',
    portOfDischarge: 'Arica',
    etd: '2026-08-20T10:00:00Z',
    eta: '2026-09-28T08:00:00Z',
    roles: ['Consignee'],
    accessSource: 'Own',
    hasPendingCharges: false,
  },
];

function detalleBase(item: ShipmentListItem): ShipmentDetail {
  return {
    id: item.id,
    blNumber: item.blNumber,
    bookingNumber: item.bookingNumber,
    operation: item.operation,
    status: item.status,
    country: item.country,
    vessel: item.vessel,
    voyage: item.voyage,
    portOfLoading: item.portOfLoading,
    portOfDischarge: item.portOfDischarge,
    placeOfDelivery: 'Santiago',
    etd: item.etd,
    eta: item.eta,
    shipper: 'Exportadora del Sur SpA',
    consignee: USUARIO_PRUEBA.name,
    roles: item.roles,
    accessSource: item.accessSource,
    allowedActions: [],
    canOperate: true,
    freight: null,
    containers: BL_PRUEBA.containers,
    localCharges: null,
    demurrageCharges: null,
    serviceOrders: [],
  };
}

const DETALLES: Record<string, ShipmentDetail> = {
  // Importación como consignee: flete, cargos locales y demurrage, con acciones de pago.
  [BL_PRUEBA.blNumber]: {
    ...detalleBase(EMBARQUES[0]),
    allowedActions: [
      'shipment.view',
      'freight.pay',
      'local-charges-mandatory.pay',
      'local-charges-on-demand.pay',
      'import-demurrage.pay',
      'warehouse-change.request',
    ],
    freight: { amount: BL_PRUEBA.freightAmount, currency: BL_PRUEBA.freightCurrency, status: 'PENDING' },
    localCharges: CARGOS_LOCALES,
    demurrageCharges: DEMURRAGE,
  },
  // Exportación con órdenes de servicio generadas.
  [BL_EXPORTACION]: {
    ...detalleBase(EMBARQUES[1]),
    allowedActions: ['shipment.view', 'freight.pay', 'local-charges-mandatory.pay'],
    freight: { amount: 1980, currency: 'USD', status: 'PAID' },
    localCharges: [],
    serviceOrders: [
      {
        id: 'o0000000-0000-4000-8000-000000000001',
        orderNumber: 'ODS-CL-2026-000310',
        orderType: 'GateIn',
        status: 'Completed',
        requestedAt: '2026-10-01T12:00:00Z',
        completedAt: '2026-10-02T15:30:00Z',
      },
    ],
  },
  // Solo shipper: la matriz no habilita el flete (X (o)); llega en null y no se muestra.
  [BL_SIN_FLETE]: {
    ...detalleBase(EMBARQUES[2]),
    allowedActions: ['shipment.view', 'local-charges-mandatory.pay'],
    localCharges: [],
  },
};

function paginar<T>(items: T[], url: URL): PagedResult<T> {
  const page = Number(url.searchParams.get('page') ?? '1') || 1;
  const pageSize = Number(url.searchParams.get('pageSize') ?? '20') || 20;
  return { items: items.slice((page - 1) * pageSize, page * pageSize), total: items.length, page, pageSize };
}

/** GET /shipments con los filtros de M2-06 y la operación de M2-07. */
function buscarEmbarques(url: URL): PagedResult<ShipmentListItem> {
  const texto = (clave: string) => url.searchParams.get(clave)?.trim().toLowerCase() ?? '';
  const operacion = url.searchParams.get('operation');
  const pais = url.searchParams.get('country');
  const filtrados = EMBARQUES.filter(
    (e) =>
      (!operacion || e.operation === operacion) &&
      (!pais || e.country === pais) &&
      e.blNumber.toLowerCase().includes(texto('blNumber')) &&
      (e.bookingNumber ?? '').toLowerCase().includes(texto('bookingNumber')) &&
      (e.vessel ?? '').toLowerCase().includes(texto('vessel')) &&
      (e.voyage ?? '').toLowerCase().includes(texto('voyage')) &&
      e.status.toLowerCase().includes(texto('status')),
  );
  return paginar(filtrados, url);
}

const USUARIOS_ORGANIZACION: OrganizationUser[] = [
  {
    id: 'u0000000-0000-4000-8000-000000000001',
    email: USUARIO_PRUEBA.email,
    firstName: 'Carla',
    lastName: 'Rojas',
    fullName: 'Carla Rojas',
    phone: USUARIO_PRUEBA.phone,
    profile: 'OrgAdmin',
    isActive: true,
    membershipStatus: 'Active',
    lastLoginAt: '2026-10-05T12:30:00Z',
    createdAt: '2026-01-15T12:00:00Z',
  },
  {
    id: 'u0000000-0000-4000-8000-000000000002',
    email: 'consulta@example.com',
    firstName: 'Diego',
    lastName: 'Soto',
    fullName: 'Diego Soto',
    phone: null,
    profile: 'OrgViewer',
    isActive: false,
    membershipStatus: 'Active',
    lastLoginAt: null,
    createdAt: '2026-03-02T12:00:00Z',
  },
];

const SOLICITUDES: JoinRequest[] = [
  {
    userId: 'u0000000-0000-4000-8000-000000000003',
    email: 'solicitud@example.com',
    fullName: 'Paula Fuentes',
    phone: '+56 9 8765 4321',
    requestedAt: '2026-10-04T14:00:00Z',
    organizationId: ORGANIZACION_PRUEBA.id,
    organizationName: ORGANIZACION_PRUEBA.name,
  },
];

const DOCUMENTOS: OrganizationDocument[] = [
  {
    id: 'f0000000-0000-4000-8000-000000000001',
    documentType: 'TaxCertificate',
    fileName: 'certificado-tributario.pdf',
    contentType: 'application/pdf',
    sizeBytes: 245760,
    uploadedAt: '2026-01-16T10:00:00Z',
  },
];

const ORGANIZACIONES_ADMIN: AdminOrganizationItem[] = [
  {
    id: 'b0000000-0000-4000-8000-000000000001',
    name: 'Logística Andina Ltda.',
    taxId: '77.888.999-0',
    taxIdType: 'RUT',
    country: 'CL',
    organizationType: 'FreightForwarder',
    status: 'PendingArCheck',
    matchCode: null,
    email: 'admin@logisticaandina.cl',
    phone: '+56 2 2777 8888',
    userCount: 1,
    createdAt: '2026-10-03T09:00:00Z',
  },
  {
    id: ORGANIZACION_PRUEBA.id,
    name: ORGANIZACION_PRUEBA.name,
    taxId: ORGANIZACION_PRUEBA.taxId,
    taxIdType: 'RUT',
    country: 'CL',
    organizationType: 'Customer',
    status: 'Approved',
    matchCode: ORGANIZACION_PRUEBA.matchCode,
    email: USUARIO_PRUEBA.email,
    phone: USUARIO_PRUEBA.phone,
    userCount: 2,
    createdAt: '2026-01-15T12:00:00Z',
  },
];

/** Organización pendiente del control con AR (M8-04). */
export const ORGANIZACION_EN_REVISION = ORGANIZACIONES_ADMIN[0].id;

const DETALLE_ORGANIZACION_ADMIN: AdminOrganizationDetail = {
  ...ORGANIZACIONES_ADMIN[0],
  operatingCountries: ['CL'],
  address: 'Av. Apoquindo 1234',
  city: 'Santiago',
  validatedAt: '2026-10-04T10:00:00Z',
  validatedBy: 'admin@hapag-lloyd.cl',
  arCheckedAt: null,
  arCheckedBy: null,
  arReference: null,
  approvedAt: null,
  rejectedAt: null,
  reviewNotes: null,
  users: [
    {
      id: 'u0000000-0000-4000-8000-000000000010',
      email: 'admin@logisticaandina.cl',
      firstName: 'Andrés',
      lastName: 'Molina',
      fullName: 'Andrés Molina',
      phone: null,
      profile: 'OrgAdmin',
      isActive: true,
      membershipStatus: 'Active',
      lastLoginAt: null,
      createdAt: '2026-10-03T09:00:00Z',
    },
  ],
  documents: DOCUMENTOS,
};

/** Niveles base de una fila en el orden de las columnas: Customer, Shipper, Consignee, Tercero, AGA, Transportista. */
function niveles(...valores: AccessMatrixLevel['level'][]): AccessMatrixLevel[] {
  const roles: AccessMatrixLevel['role'][] = ['Customer', 'Shipper', 'Consignee', 'ThirdParty', 'CustomsAgency', 'Carrier'];
  return roles.map((role, i) => ({ role, organizationType: null, level: valores[i] }));
}

const MATRIZ: AccessMatrixAction[] = [
  {
    code: 'shipment.view',
    name: 'Ver listado y detalle de BL o booking',
    category: 'Information',
    kind: 'View',
    scope: 'Shipment',
    displayOrder: 1,
    isActive: true,
    levels: niveles('Allowed', 'Allowed', 'Allowed', 'Allowed', 'Allowed', 'Allowed'),
  },
  {
    code: 'freight.pay',
    name: 'Visualizar y pagar montos de flete',
    category: 'Information',
    kind: 'Operate',
    scope: 'Shipment',
    displayOrder: 9,
    isActive: true,
    levels: niveles('Allowed', 'OnGrant', 'Allowed', 'OnGrant', 'OnGrant', 'OnGrant'),
  },
  {
    code: 'responsibility-letter.generate',
    name: 'Generar y descargar carta de responsabilidad',
    category: 'Information',
    kind: 'Operate',
    scope: 'Shipment',
    displayOrder: 15,
    isActive: true,
    levels: [
      ...niveles('Denied', 'Denied', 'Denied', 'Denied', 'Denied', 'Denied'),
      { role: 'Consignee', organizationType: 'FreightForwarder', level: 'Allowed' },
    ],
  },
  {
    code: 'access.grant',
    name: 'Otorgar acceso a un BL o booking, individual o masivo',
    category: 'Administration',
    kind: 'Operate',
    scope: 'Shipment',
    displayOrder: 25,
    isActive: true,
    levels: niveles('Allowed', 'Allowed', 'Allowed', 'Allowed', 'Denied', 'Denied'),
  },
];

type RespuestaGet = unknown | ((url: URL) => unknown);

/** Respuestas por ruta relativa a /api/v1/ (método GET); una función recibe la URL con sus query params. */
const RESPUESTAS: Record<string, RespuestaGet> = {
  'bills-of-lading/my': [BL_PRUEBA],
  [`bills-of-lading/${BL_PRUEBA.blNumber}`]: BL_PRUEBA,
  [`bills-of-lading/${BL_PRUEBA.blNumber}/charges`]: CARGOS_BL,
  [`bills-of-lading/${BL_PRUEBA.blNumber}/demurrage`]: DEMURRAGE,
  'payments/my': PAGOS,
  'notifications/unread-count': { count: NOTIFICACIONES.filter((n) => !n.isRead).length },
  notifications: NOTIFICACIONES,
  'receipts/my': RECIBOS,
  shipments: buscarEmbarques,
  ...Object.fromEntries(Object.entries(DETALLES).map(([bl, detalle]) => [`shipments/${bl}`, detalle])),
  'organizations/me': ORGANIZACION_PRUEBA,
  'organizations/me/users': USUARIOS_ORGANIZACION,
  'organizations/me/join-requests': SOLICITUDES,
  'organizations/me/documents': DOCUMENTOS,
  'organizations/me/operating-country': { country: 'CL', availableCountries: ['CL', 'BO'], canChange: true },
  'admin/organizations': (url: URL) => {
    const estado = url.searchParams.get('status');
    return paginar(
      ORGANIZACIONES_ADMIN.filter((o) => !estado || o.status === estado),
      url,
    );
  },
  [`admin/organizations/${ORGANIZACION_EN_REVISION}`]: DETALLE_ORGANIZACION_ADMIN,
  'access-matrix': MATRIZ,
};

/** Respuestas de escritura por `MÉTODO ruta`; el resto responde 200 sin cuerpo. */
const ESCRITURAS: Record<string, { status: number; body?: unknown }> = {
  'POST auth/register': { status: 201, body: { ...USUARIO_PRUEBA, id: '7d1c2b3a-0000-4000-8000-000000000002' } },
  'POST auth/register/join': {
    status: 202,
    body: { userId: 'u0000000-0000-4000-8000-000000000004', organizationName: ORGANIZACION_PRUEBA.name, status: 'Pending' },
  },
  'POST auth/logout': { status: 204 },
  'POST auth/refresh-token': {
    status: 200,
    body: {
      token: 'token-renovado',
      refreshToken: 'refresh-renovado',
      expiresIn: 60,
      user: { ...USUARIO_PRUEBA, country: 'BO' },
      organization: ORGANIZACION_PRUEBA,
    },
  },
  'PUT organizations/me/operating-country': {
    status: 200,
    body: { country: 'BO', availableCountries: ['CL', 'BO'], canChange: true },
  },
};

/** Intercepta /api/v1/**: rutas conocidas con datos ficticios; cualquier otra GET responde []. */
export async function simularApi(page: Page): Promise<void> {
  await page.route('**/api/v1/**', async (route: Route) => {
    const request = route.request();
    const url = new URL(request.url());
    const ruta = url.pathname.replace(/^.*\/api\/v1\//, '').replace(/\/$/, '');
    const metodo = request.method();

    if (metodo !== 'GET') {
      const escritura = ESCRITURAS[`${metodo} ${ruta}`];
      await route.fulfill({
        status: escritura?.status ?? 200,
        contentType: 'application/json',
        body: escritura?.body === undefined ? '' : JSON.stringify(escritura.body),
      });
      return;
    }

    const respuesta = ruta in RESPUESTAS ? RESPUESTAS[ruta] : [];
    const cuerpo = typeof respuesta === 'function' ? respuesta(url) : respuesta;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(cuerpo),
    });
  });
}
