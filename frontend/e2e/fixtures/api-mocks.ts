import { Page, Route } from '@playwright/test';
import type { BillOfLading, DemurrageCharge, LocalCharge } from '../../src/app/core/models/bl.model';
import type { NotificationItem } from '../../src/app/core/models/notification.model';
import type { Payment } from '../../src/app/core/models/payment.model';
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
import type {
  AccessAuditEntry,
  AccessGrant,
  DefaultGrantee,
  GrantAccessResult,
  GrantableAction,
  GranteeOrganization,
  OpenAccessSetting,
  OrganizationRef,
  VisibilityWidening,
} from '../../src/app/core/models/access.model';
import type {
  CommercialConditions,
  ExchangeRate,
  ExemptionFigure,
  ExemptionTrace,
  RuledCharge,
  ShipmentCharges,
} from '../../src/app/core/models/charges.model';
import type {
  CalculateDemurrageRequest,
  DemurrageCalculation,
  DemurrageConceptCharge,
  DemurrageLine,
  DemurrageStatus,
} from '../../src/app/core/models/demurrage.model';
import type {
  WarehouseChangeBatch,
  WarehouseChangeBatchItem,
  WarehouseChangeDetail,
  WarehouseChangeQuote,
  WarehouseChangeRequest,
} from '../../src/app/core/models/warehouse-change.model';
import type {
  ChargeConcept,
  InternalChargeRule,
  InternalChargeRuleChange,
  Tariff,
  TariffChange,
  TariffRequest,
  TariffSnapshot,
} from '../../src/app/core/models/tariff.model';
import { ORGANIZACION_PRUEBA, USUARIO_PRUEBA } from './session';
import { OpcionesOlaD, SimulacionOlaD } from './ola-d-mocks';
import { SimulacionOlaE } from './ola-e-mocks';
import { SimulacionOlaF } from './ola-f-mocks';
import { SimulacionOlaG } from './ola-g-mocks';
import { SimulacionOlaH } from './ola-h-mocks';
import { OpcionesOlaI, SimulacionOlaI } from './ola-i-mocks';
import { SimulacionOlaJ } from './ola-j-mocks';

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
  {
    id: 'n0000000-0000-4000-8000-000000000002',
    type: 'AccessRevokedByCascade',
    title: 'Acceso revocado en cadena',
    body: 'El acceso sobre HLCU0000001 fue revocado porque se revocó o venció el acceso del que dependía.',
    isRead: false,
    createdAt: '2026-10-04T10:00:00Z',
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
/** Ola B: BL ajeno con acceso abierto activo (M1-17, M1-18); no aparece en el listado. */
export const BL_ACCESO_ABIERTO = 'HLCUVAP260399999';

export const EMBARQUES: ShipmentListItem[] = [
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
    roles: ['ThirdParty'],
    accessSource: 'Grant',
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
    canSelfAssociate: false,
    requiresAssociationForPayment: false,
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
  // Ola B: BL de otro cliente visto solo por acceso abierto (M1-17); permite autoasociarse (M1-18).
  [BL_ACCESO_ABIERTO]: {
    ...detalleBase({
      ...EMBARQUES[0],
      id: '3f0c2a1e-0000-4000-8000-000000000009',
      blNumber: BL_ACCESO_ABIERTO,
      bookingNumber: 'BKG26030099',
      roles: [],
      accessSource: 'OpenAccess',
    }),
    allowedActions: ['shipment.view', 'tracking.view'],
    canOperate: false,
    canSelfAssociate: true,
    requiresAssociationForPayment: true,
    shipper: null,
    consignee: null,
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

// ---------------------------------------------------------------------------
// Fase 1, Ola B: accesos a terceros (M1-03, M1-12 a M1-24).
// ---------------------------------------------------------------------------

const ORG_PROPIA: OrganizationRef = {
  id: ORGANIZACION_PRUEBA.id,
  name: ORGANIZACION_PRUEBA.name,
  organizationType: 'Customer',
  country: 'CL',
};

/** Agencia de aduanas que recibe los accesos de la organización de prueba. */
export const AGENCIA: GranteeOrganization = {
  id: 'c1000000-0000-4000-8000-000000000001',
  name: 'Agencia Marítima del Pacífico',
  taxId: '76.555.444-3',
  organizationType: 'CustomsAgency',
  country: 'CL',
};

const TRANSPORTISTA: GranteeOrganization = {
  id: 'c1000000-0000-4000-8000-000000000002',
  name: 'Transportes Cordillera Ltda.',
  taxId: '77.111.222-3',
  organizationType: 'Carrier',
  country: 'CL',
};

const ORG_PACIFIC: OrganizationRef = {
  id: 'c1000000-0000-4000-8000-000000000003',
  name: 'Pacific Trading S.A.',
  organizationType: 'Customer',
  country: 'CL',
};

const DESTINATARIOS: GranteeOrganization[] = [AGENCIA, TRANSPORTISTA];

function referencia(org: GranteeOrganization): OrganizationRef {
  return { id: org.id, name: org.name, organizationType: org.organizationType, country: org.country };
}

function acceso(datos: Partial<AccessGrant> & Pick<AccessGrant, 'id'>): AccessGrant {
  return {
    direction: 'Given',
    grantor: ORG_PROPIA,
    grantee: referencia(AGENCIA),
    grantorRole: 'Customer',
    billOfLadingId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    bookingNumber: 'BKG26030010',
    grantType: 'Individual',
    intendedRole: null,
    hasExplicitPermissions: false,
    actionCodes: null,
    effectiveActionCodes: ['shipment.view', 'release-requirements.view', 'tracking.view'],
    validityType: 'UntilDate',
    validFrom: '2026-10-01T12:00:00Z',
    validTo: '2027-03-31T23:59:59Z',
    durationDays: null,
    status: 'Active',
    isEffective: true,
    isMandate: false,
    termsVersion: null,
    termsAcceptedAt: null,
    parentGrantId: null,
    defaultGranteeId: null,
    createdAt: '2026-10-01T12:00:00Z',
    endedAt: null,
    endReason: null,
    canEdit: true,
    ...datos,
  };
}

/** Acceso individual con permisos limitados que la organización otorgó a la agencia. */
export const ACCESO_AGENCIA = acceso({
  id: 'a1000000-0000-4000-8000-000000000001',
  hasExplicitPermissions: true,
  actionCodes: ['shipment.view', 'tracking.view', 'import-demurrage.pay'],
  effectiveActionCodes: ['shipment.view', 'tracking.view', 'import-demurrage.pay'],
});

const ACCESOS: AccessGrant[] = [
  ACCESO_AGENCIA,
  // Mandato pendiente de aceptación de términos (M1-03).
  acceso({
    id: 'a1000000-0000-4000-8000-000000000002',
    billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000006',
    blNumber: BL_EXPORTACION,
    bookingNumber: 'BKG26030061',
    hasExplicitPermissions: true,
    actionCodes: ['shipment.view', 'freight.pay'],
    effectiveActionCodes: [],
    status: 'PendingAcceptance',
    isEffective: false,
    isMandate: true,
  }),
  // Acceso recibido de otra organización (sin edición).
  acceso({
    id: 'a1000000-0000-4000-8000-000000000003',
    direction: 'Received',
    grantor: ORG_PACIFIC,
    grantee: ORG_PROPIA,
    billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000008',
    blNumber: 'HLCUARI260300830',
    bookingNumber: 'BKG26030083',
    grantType: 'Default',
    validityType: 'Indefinite',
    validTo: null,
    canEdit: false,
  }),
  // Acceso revocado en cadena (M1-22).
  acceso({
    id: 'a1000000-0000-4000-8000-000000000004',
    grantee: referencia(TRANSPORTISTA),
    status: 'Revoked',
    isEffective: false,
    endedAt: '2026-10-03T09:00:00Z',
    endReason: 'Cascade',
    parentGrantId: 'a1000000-0000-4000-8000-000000000009',
    canEdit: false,
  }),
];

/** GET /access/grants con dirección, estado y BL o booking. */
function buscarAccesos(url: URL): PagedResult<AccessGrant> {
  const direccion = url.searchParams.get('direction');
  const estado = url.searchParams.get('status');
  const ref = url.searchParams.get('reference')?.trim().toLowerCase() ?? '';
  return paginar(
    ACCESOS.filter(
      (a) =>
        (!direccion || a.direction === direccion) &&
        (!estado || a.status === estado) &&
        (!ref || (a.blNumber ?? '').toLowerCase().includes(ref) || (a.bookingNumber ?? '').toLowerCase().includes(ref)),
    ),
    url,
  );
}

/** Acciones del selector de permisos: lo que el otorgante posee y lo que el tercero puede recibir. */
const ACCIONES_OTORGABLES: GrantableAction[] = [
  { code: 'shipment.view', name: 'Ver BL', category: 'Information', kind: 'View', grantorHas: true, grantable: true, includedInBaseLevel: true },
  { code: 'release-requirements.view', name: 'Requisitos', category: 'Information', kind: 'View', grantorHas: true, grantable: true, includedInBaseLevel: true },
  { code: 'tracking.view', name: 'Seguimiento', category: 'Information', kind: 'View', grantorHas: true, grantable: true, includedInBaseLevel: true },
  { code: 'freight.pay', name: 'Flete', category: 'Information', kind: 'Operate', grantorHas: true, grantable: true, includedInBaseLevel: false },
  { code: 'import-demurrage.pay', name: 'Demurrage', category: 'Information', kind: 'Operate', grantorHas: true, grantable: true, includedInBaseLevel: false },
  // El otorgante no lo posee: aparece deshabilitado (M1-12, M1-15).
  { code: 'responsibility-letter.generate', name: 'Carta', category: 'Information', kind: 'Operate', grantorHas: false, grantable: false, includedInBaseLevel: false },
  { code: 'access.grant', name: 'Otorgar acceso', category: 'Administration', kind: 'Operate', grantorHas: true, grantable: false, includedInBaseLevel: false },
];

const DEFECTO: DefaultGrantee[] = [
  {
    id: 'd1000000-0000-4000-8000-000000000001',
    grantee: referencia(AGENCIA),
    actionCodes: null,
    durationDays: 180,
    createdAt: '2026-09-01T12:00:00Z',
    modifiedAt: null,
  },
];

const ACCESO_ABIERTO: OpenAccessSetting = {
  isEnabled: false,
  actionCodes: null,
  effectiveActionCodes: ['shipment.view', 'tracking.view'],
  canManage: true,
  changedAt: null,
};

const AUDITORIA: AccessAuditEntry[] = [
  {
    id: 'e1000000-0000-4000-8000-000000000001',
    occurredAt: '2026-10-01T12:00:00Z',
    eventType: 'GrantCreated',
    billOfLadingId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    bookingNumber: 'BKG26030010',
    accessGrantId: ACCESO_AGENCIA.id,
    visibilityWideningId: null,
    grantor: ORG_PROPIA,
    grantee: referencia(AGENCIA),
    actorUserId: 'u0000000-0000-4000-8000-000000000001',
    actorEmail: USUARIO_PRUEBA.email,
    actorOrganization: ORG_PROPIA,
    details: null,
  },
  {
    id: 'e1000000-0000-4000-8000-000000000002',
    occurredAt: '2026-10-03T09:00:00Z',
    eventType: 'GrantRevokedByCascade',
    billOfLadingId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    bookingNumber: 'BKG26030010',
    accessGrantId: 'a1000000-0000-4000-8000-000000000004',
    visibilityWideningId: null,
    grantor: ORG_PROPIA,
    grantee: referencia(TRANSPORTISTA),
    actorUserId: null,
    actorEmail: null,
    actorOrganization: null,
    details: null,
  },
  {
    id: 'e1000000-0000-4000-8000-000000000003',
    occurredAt: '2026-10-04T15:30:00Z',
    eventType: 'OpenAccessEnabled',
    billOfLadingId: null,
    blNumber: null,
    bookingNumber: null,
    accessGrantId: null,
    visibilityWideningId: null,
    grantor: null,
    grantee: null,
    actorUserId: 'u0000000-0000-4000-8000-000000000001',
    actorEmail: USUARIO_PRUEBA.email,
    actorOrganization: ORG_PROPIA,
    details: null,
  },
  {
    id: 'e1000000-0000-4000-8000-000000000004',
    occurredAt: '2026-10-05T08:00:00Z',
    eventType: 'GrantExpired',
    billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000006',
    blNumber: BL_EXPORTACION,
    bookingNumber: 'BKG26030061',
    accessGrantId: 'a1000000-0000-4000-8000-000000000005',
    visibilityWideningId: null,
    grantor: ORG_PROPIA,
    grantee: referencia(AGENCIA),
    actorUserId: null,
    actorEmail: null,
    actorOrganization: null,
    details: null,
  },
];

/** GET /access/audit con BL o booking. */
function buscarAuditoria(url: URL): PagedResult<AccessAuditEntry> {
  const bl = url.searchParams.get('blNumber')?.trim().toLowerCase() ?? '';
  const booking = url.searchParams.get('bookingNumber')?.trim().toLowerCase() ?? '';
  return paginar(
    AUDITORIA.filter(
      (e) =>
        (!bl || (e.blNumber ?? '').toLowerCase() === bl) &&
        (!booking || (e.bookingNumber ?? '').toLowerCase() === booking),
    ),
    url,
  );
}

const AMPLIACIONES: VisibilityWidening[] = [
  {
    id: 'w1000000-0000-4000-8000-000000000001',
    billOfLadingId: BL_PRUEBA.id,
    blNumber: BL_PRUEBA.blNumber,
    grantor: ORG_PROPIA,
    grantorRole: 'Consignee',
    targetRole: 'Shipper',
    actionCode: 'freight.pay',
    originGrantId: null,
    status: 'Active',
    createdAt: '2026-10-02T12:00:00Z',
    endedAt: null,
    endReason: null,
    canRevoke: true,
  },
];

/** Resultado del otorgamiento: uno individual o el masivo con un omitido (M1-12). */
function resultadoOtorgamiento(cuerpo: { blNumbers?: string[]; bookingNumbers?: string[] }): GrantAccessResult {
  const referencias = [...(cuerpo.blNumbers ?? []), ...(cuerpo.bookingNumbers ?? [])];
  const omitidas = referencias.filter((r) => r === BL_SIN_FLETE);
  const aplicadas = referencias.filter((r) => r !== BL_SIN_FLETE);
  return {
    requested: referencias.length,
    updated: aplicadas.length,
    created: Math.max(aplicadas.length - 1, aplicadas.length === 1 ? 1 : 0),
    modified: aplicadas.length > 1 ? 1 : 0,
    skipped: omitidas.map((reference) => ({
      reference,
      code: 'AccessGrant.ExceedsGrantorLevel',
      message: 'The grantor does not hold one of the requested actions on this shipment.',
    })),
    grants: aplicadas.map((bl, i) => acceso({ id: `a2000000-0000-4000-8000-00000000000${i}`, blNumber: bl })),
  };
}

// ---------------------------------------------------------------------------
// Fase 1, Ola C: cargos con reglas de Nexus (M4-01 a M4-04, M3-01, M5-05), condiciones comerciales
// (M8-02, M8-03), demurrage por estado (M3-18, M3-02, M3-16), cambio de almacén (M3-04, M3-05),
// tarifas (M8-01, NF-15) y reglas internas de cobro.
// ---------------------------------------------------------------------------

/** Gate In y EDS exentos por el consignatario del Master: "Aplicar reglas" completa sin carro (M4-02). */
export const BL_EXENTO = 'HLCUSAI260401020';
/** Pagador con crédito en Nexus: el IPO no llega (M4-03). */
export const BL_CREDITO = 'HLCUVAP260401130';
/** Pagador FFWW: carta de responsabilidad faltante que bloquea el avance (M4-04). */
export const BL_FFWW = 'HLCUSAI260401199';
/** Nexus no responde (NF-11). */
export const BL_NEXUS_CAIDO = 'HLCUSAI260409999';
/** Demurrage facturado con deuda vigente: la calculadora no se habilita (M3-18). */
export const BL_DEM_FACTURADO = 'HLCUSAI260400910';
/** Demurrage calculado y no pagado; también tiene derecho a cambio de almacén gratuito (M3-04). */
export const BL_DEM_CALCULADO = 'HLCUVAL250100123';
/** Demurrage aún no calculado: muestra los datos y la calculadora. */
export const BL_DEM_SIN_CALCULO = BL_EXENTO;
/** Sin demurrage: no registra deuda. */
export const BL_DEM_SIN_DEUDA = BL_EXPORTACION;
/** Bolivia: demoras anticipadas obligatorias con el CLD bloqueado (M3-16). */
export const BL_BOLIVIA = 'HLCUARI260100045';
/** BL con derecho a cambio de almacén gratuito (regla interna del portal). */
export const BL_CAMBIO_GRATIS = BL_DEM_CALCULADO;
/** Solicitud masiva de cambio de almacén cuyo avance se consulta (M3-05). */
export const LOTE_CAMBIO_ALMACEN = 'b2000000-0000-4000-8000-000000000001';
/** Tarifa con tramos de horas (LATE_ARRIVAL) que se edita en las pruebas (M8-01). */
export const TARIFA_TRAMOS = 'f3000000-0000-4000-8000-000000000003';
/** Regla interna de cambio de almacén gratuito. */
export const REGLA_CAMBIO_GRATIS = 'f4000000-0000-4000-8000-000000000001';

const EVALUADO = '2026-10-05T15:00:00Z';

function condiciones(datos: Partial<CommercialConditions> = {}): CommercialConditions {
  return {
    available: true,
    source: 'NEXUS',
    taxId: '76123456-7',
    matchCode: ORGANIZACION_PRUEBA.matchCode,
    hasCredit: false,
    creditDays: null,
    creditConcepts: [],
    creditValidFrom: null,
    creditValidTo: null,
    isFreightForwarder: false,
    responsibilityLetterRequired: false,
    ipoExcluded: false,
    ...datos,
  };
}

function cargo(datos: Partial<RuledCharge> & Pick<RuledCharge, 'chargeId' | 'conceptCode' | 'conceptName'>): RuledCharge {
  return {
    description: null,
    category: 'LocalCharge',
    amount: 100000,
    taxAmount: 19000,
    totalAmount: 119000,
    currency: 'CLP',
    status: 'Pending',
    outcome: 'Payable',
    payableAmount: 100000,
    payableTaxAmount: 19000,
    payableTotal: 119000,
    action: 'AddToCart',
    actionBlockedReason: null,
    exemption: null,
    localCurrency: null,
    ...datos,
  };
}

/** Exención de Nexus del consignatario del BL Master (Delfin) para un concepto (M4-01). */
function exencion(concepto: string, monto: number, aplicada: string | null = null): ExemptionTrace {
  return {
    party: 'MasterConsignee',
    taxId: '76000001-1',
    matchCode: null,
    concept: concepto,
    conditionAmount: null,
    conditionCurrency: null,
    validFrom: '2026-01-01',
    validTo: null,
    source: 'NEXUS',
    exemptAmount: monto,
    appliedAt: aplicada,
  };
}

function cargosBl(blNumber: string, datos: Partial<ShipmentCharges> = {}): ShipmentCharges {
  return {
    blId: `3f0c2a1e-0000-4000-8000-${blNumber.slice(-12).padStart(12, '0')}`,
    blNumber,
    country: 'CL',
    shipmentType: 'Import',
    timeZone: 'America/Santiago',
    payer: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: '76123456-7', matchCode: ORGANIZACION_PRUEBA.matchCode },
    conditions: condiciones(),
    exemptionFigures: [],
    charges: [],
    payableTotals: [],
    allApplicableExempt: false,
    requiresPayment: false,
    rulesAvailable: true,
    requirements: [],
    canProceed: true,
    evaluatedAt: EVALUADO,
    ...datos,
  };
}

const FIGURAS_DELFIN: ExemptionFigure[] = [
  {
    party: 'MasterConsignee',
    name: 'Delfin Logística SpA',
    taxId: '76000001-1',
    matchCode: null,
    available: true,
    exemptions: [
      { concept: 'GATE_IN', validFrom: '2026-01-01', validTo: null },
      { concept: 'EDS', validFrom: '2026-01-01', validTo: null },
    ],
  },
  { party: 'FinalClient', name: ORGANIZACION_PRUEBA.name, taxId: '76123456-7', matchCode: ORGANIZACION_PRUEBA.matchCode, available: true, exemptions: [] },
];

function cargosExentos(aplicada: string | null): ShipmentCharges {
  const exento = (id: string, concepto: string, nombre: string): RuledCharge =>
    cargo({
      chargeId: id,
      conceptCode: concepto,
      conceptName: nombre,
      amount: 95000,
      taxAmount: 18050,
      totalAmount: 113050,
      status: aplicada ? 'Exempt' : 'Pending',
      outcome: 'Exempt',
      payableAmount: 0,
      payableTaxAmount: 0,
      payableTotal: 0,
      action: 'None',
      exemption: exencion(concepto, 95000, aplicada),
    });
  return cargosBl(BL_EXENTO, {
    exemptionFigures: FIGURAS_DELFIN,
    charges: [
      exento('c3000000-0000-4000-8000-000000000001', 'GATE_IN', 'Gate In'),
      exento('c3000000-0000-4000-8000-000000000002', 'EDS', 'EDS'),
    ],
    allApplicableExempt: true,
  });
}

const IPO_USD = (id: string): RuledCharge =>
  cargo({
    chargeId: id,
    conceptCode: 'IPO',
    conceptName: 'IPO',
    amount: 150,
    taxAmount: 0,
    totalAmount: 150,
    currency: 'USD',
    payableAmount: 150,
    payableTaxAmount: 0,
    payableTotal: 150,
    localCurrency: { currency: 'CLP', amount: 142500, rate: 950, effectiveDate: '2026-10-05', source: 'NEXUS' },
  });

const CARGOS_OLA_C: Record<string, ShipmentCharges> = {
  [BL_EXENTO]: cargosExentos(null),
  // BL de prueba: THC a pagar, Gate In exento parcial y el IPO en USD con su equivalente en CLP (M5-05).
  [BL_PRUEBA.blNumber]: cargosBl(BL_PRUEBA.blNumber, {
    blId: BL_PRUEBA.id,
    exemptionFigures: FIGURAS_DELFIN,
    charges: [
      cargo({ chargeId: 'c3000000-0000-4000-8000-000000000011', conceptCode: 'THC', conceptName: 'THC', description: 'Terminal Handling Charge', action: 'Pay' }),
      cargo({
        chargeId: 'c3000000-0000-4000-8000-000000000012',
        conceptCode: 'GATE_IN',
        conceptName: 'Gate In',
        outcome: 'PartiallyExempt',
        payableAmount: 50000,
        payableTaxAmount: 9500,
        payableTotal: 59500,
        exemption: { ...exencion('GATE_IN', 50000), conditionAmount: 50000, conditionCurrency: 'CLP' },
      }),
      IPO_USD('c3000000-0000-4000-8000-000000000013'),
    ],
    payableTotals: [
      { currency: 'CLP', amount: 150000, taxAmount: 28500, total: 178500 },
      { currency: 'USD', amount: 150, taxAmount: 0, total: 150 },
    ],
    requiresPayment: true,
  }),
  // Crédito en Nexus: el IPO no llega del servidor (M4-03, M8-02).
  [BL_CREDITO]: cargosBl(BL_CREDITO, {
    conditions: condiciones({ hasCredit: true, creditDays: 30, creditConcepts: ['LOCAL_CHARGES', 'MHD'], creditValidFrom: '2026-01-01', ipoExcluded: true }),
    charges: [cargo({ chargeId: 'c3000000-0000-4000-8000-000000000021', conceptCode: 'THC', conceptName: 'THC' })],
    payableTotals: [{ currency: 'CLP', amount: 100000, taxAmount: 19000, total: 119000 }],
    requiresPayment: true,
  }),
  // FFWW: el IPO se muestra y la carta de responsabilidad bloquea el avance (M4-04, M8-03).
  [BL_FFWW]: cargosBl(BL_FFWW, {
    conditions: condiciones({ isFreightForwarder: true, responsibilityLetterRequired: true }),
    charges: [
      cargo({ chargeId: 'c3000000-0000-4000-8000-000000000031', conceptCode: 'THC', conceptName: 'THC' }),
      IPO_USD('c3000000-0000-4000-8000-000000000032'),
    ],
    payableTotals: [
      { currency: 'CLP', amount: 100000, taxAmount: 19000, total: 119000 },
      { currency: 'USD', amount: 150, taxAmount: 0, total: 150 },
    ],
    requiresPayment: true,
    requirements: [{ code: 'RESPONSIBILITY_LETTER', status: 'Missing', blocksProcess: true, source: 'NEXUS' }],
    canProceed: false,
  }),
  // Nexus no responde: no se presentan cargos como definitivos (NF-11).
  [BL_NEXUS_CAIDO]: cargosBl(BL_NEXUS_CAIDO, {
    conditions: condiciones({ available: false, errorCode: 'Integration.Unavailable' }),
    charges: [cargo({ chargeId: 'c3000000-0000-4000-8000-000000000041', conceptCode: 'THC', conceptName: 'THC', action: 'None', actionBlockedReason: 'RULES_UNAVAILABLE' })],
    rulesAvailable: false,
  }),
  [BL_EXPORTACION]: cargosBl(BL_EXPORTACION, { shipmentType: 'Export' }),
  [BL_SIN_FLETE]: cargosBl(BL_SIN_FLETE, { shipmentType: 'Export' }),
};

function estadoDemurrage(blNumber: string, datos: Partial<DemurrageStatus>): DemurrageStatus {
  return {
    blId: `3f0c2a1e-0000-4000-8000-${blNumber.slice(-12).padStart(12, '0')}`,
    blNumber,
    country: 'CL',
    timeZone: 'America/Santiago',
    state: 'NoDemurrage',
    action: 'None',
    actionAllowed: true,
    actionBlockedReason: null,
    calculatorEnabled: false,
    messageCode: null,
    invoices: [],
    lines: [],
    calculationInputs: null,
    otherConcepts: [],
    mhd: null,
    advance: { required: false, status: 'NotRequired', cldBlocked: false, action: 'None' },
    requirements: [],
    evaluatedAt: EVALUADO,
    ...datos,
  };
}

const LINEA_FACTURADA: DemurrageLine = {
  id: 'd3000000-0000-4000-8000-000000000001',
  containerNumber: 'HLXU3034001',
  freeDays: 7,
  demurrageDays: 10,
  dailyRate: 51000,
  totalAmount: 510000,
  currency: 'CLP',
  startDate: '2026-09-21T03:00:00Z',
  endDate: '2026-10-01T03:00:00Z',
  status: 'Invoiced',
  isExempt: false,
  invoiceNumber: 'FAC-DEM-2026-0915',
  invoicedAt: '2026-10-01T15:00:00Z',
  invoiceDueDate: '2026-10-15T03:00:00Z',
};

const MHD: DemurrageConceptCharge = {
  chargeId: 'd3000000-0000-4000-8000-000000000101',
  conceptCode: 'MHD',
  conceptName: 'MHD',
  amount: 450,
  taxAmount: 0,
  totalAmount: 450,
  currency: 'USD',
  status: 'Pending',
  deduction: 0,
  payableTotal: 450,
  action: 'AddToCart',
};

const ENTRADAS_CALCULO = {
  dischargeDate: '2026-09-28T12:00:00Z',
  freeDays: 7,
  freeDaysSource: 'FIS',
  containers: [
    { containerNumber: 'HLXU3034002', containerType: '20DV', status: 'Discharged' },
    { containerNumber: 'HLXU3034003', containerType: '40HC', status: 'Discharged' },
  ],
  tariffAvailable: true,
  today: '2026-10-05',
};

const LINEAS_CALCULADAS: DemurrageLine[] = [
  {
    id: 'd3000000-0000-4000-8000-000000000011',
    containerNumber: 'HLXU3034002',
    freeDays: 7,
    demurrageDays: 5,
    dailyRate: 35000,
    totalAmount: 175000,
    currency: 'CLP',
    startDate: '2026-09-28T12:00:00Z',
    endDate: '2026-10-10T03:00:00Z',
    status: 'Pending',
    isExempt: false,
  },
];

const DEMURRAGE_OLA_C: Record<string, DemurrageStatus> = {
  [BL_DEM_FACTURADO]: estadoDemurrage(BL_DEM_FACTURADO, {
    state: 'InvoicedWithDebt',
    action: 'Pay',
    invoices: [{ invoiceNumber: 'FAC-DEM-2026-0915', amount: 510000, currency: 'CLP', dueDate: '2026-10-15T03:00:00Z', invoicedAt: '2026-10-01T15:00:00Z', lineIds: [LINEA_FACTURADA.id] }],
    lines: [LINEA_FACTURADA],
    otherConcepts: [MHD],
    mhd: { currency: 'USD', total: 450, advanceDeducted: 0, netPayable: 450 },
  }),
  [BL_DEM_CALCULADO]: estadoDemurrage(BL_DEM_CALCULADO, {
    state: 'CalculatedUnpaid',
    action: 'AddToCart',
    calculatorEnabled: true,
    lines: LINEAS_CALCULADAS,
  }),
  [BL_DEM_SIN_CALCULO]: estadoDemurrage(BL_DEM_SIN_CALCULO, {
    state: 'NotCalculated',
    action: 'Calculate',
    calculatorEnabled: true,
    calculationInputs: ENTRADAS_CALCULO,
  }),
  [BL_DEM_SIN_DEUDA]: estadoDemurrage(BL_DEM_SIN_DEUDA, { messageCode: 'NO_DEBT' }),
  // Bolivia: demoras anticipadas obligatorias, CLD bloqueado y MHD a descontar (M3-16).
  [BL_BOLIVIA]: estadoDemurrage(BL_BOLIVIA, {
    country: 'BO',
    timeZone: 'America/La_Paz',
    state: 'NotCalculated',
    action: 'Calculate',
    calculatorEnabled: true,
    calculationInputs: { ...ENTRADAS_CALCULO, freeDaysSource: 'PORTAL', containers: [ENTRADAS_CALCULO.containers[0]] },
    otherConcepts: [{ ...MHD, chargeId: 'd3000000-0000-4000-8000-000000000201' }],
    mhd: { currency: 'USD', total: 450, advanceDeducted: 0, netPayable: 450 },
    advance: {
      required: true,
      status: 'NotRequested',
      ruleId: 'f4000000-0000-4000-8000-000000000002',
      ruleReason: 'Cuenta sujeta a demoras anticipadas según reglas internas',
      amount: 150,
      currency: 'USD',
      chargeId: null,
      cldBlocked: true,
      cldBlockReason: 'ADVANCE_DEMURRAGE',
      action: 'AddToCart',
    },
    requirements: [{ code: 'ADVANCE_DEMURRAGE', status: 'Missing', blocksProcess: true, source: 'PORTAL' }],
  }),
};

/** Resultado de la calculadora (vista previa o guardado) para el BL sin cálculo. */
function calculoDemurrage(cuerpo: CalculateDemurrageRequest): DemurrageCalculation {
  const contenedores = cuerpo.containerNumbers ?? ENTRADAS_CALCULO.containers.map((c) => c.containerNumber);
  const lineas = contenedores.map((numero) => ({
    containerNumber: numero,
    containerType: '20DV',
    startDate: '2026-09-28T12:00:00Z',
    untilDate: cuerpo.untilDate ?? '2026-10-05',
    elapsedDays: 12,
    freeDays: 7,
    demurrageDays: 5,
    dailyRate: 35000,
    totalAmount: 175000,
    currency: 'CLP',
    tariffSource: 'PORTAL',
    tariffId: 'f3000000-0000-4000-8000-000000000004',
    breakdown: [{ fromUnit: 8, toUnit: 14, units: 5, unitAmount: 35000, amount: 175000 }],
  }));
  return {
    blNumber: BL_DEM_SIN_CALCULO,
    saved: cuerpo.save,
    lines: lineas,
    totals: [{ currency: 'CLP', amount: 175000 * lineas.length, taxAmount: 0, total: 175000 * lineas.length }],
    status: cuerpo.save
      ? estadoDemurrage(BL_DEM_SIN_CALCULO, { state: 'CalculatedUnpaid', action: 'AddToCart', calculatorEnabled: true, lines: LINEAS_CALCULADAS })
      : null,
  };
}

/** Tipo de cambio de Nexus por par de monedas (M5-05). */
function tipoDeCambio(url: URL): ExchangeRate {
  const from = url.searchParams.get('from') ?? 'USD';
  const to = url.searchParams.get('to') ?? 'CLP';
  return { fromCurrency: from, toCurrency: to, rate: to === 'BOB' ? 6.96 : 950, effectiveDate: '2026-10-05', source: 'NEXUS', approved: true };
}

const COTIZACION_TARIFAS: WarehouseChangeQuote = {
  blNumber: BL_PRUEBA.blNumber,
  country: 'CL',
  containerNumber: null,
  entitlement: { isFree: false, usedOnBl: 0 },
  tariffs: [
    { code: 'KTE', description: 'Cambio de almacén (KTE)', amount: 9940, currency: 'CLP', source: 'PORTAL', validFrom: '2026-10-01', validTo: null },
    { code: 'KTF', description: 'Cambio de almacén (KTF)', amount: 110910, currency: 'CLP', source: 'PORTAL', validFrom: '2026-10-01', validTo: null },
  ],
  defaultTariffCode: 'KTE',
  canRequest: true,
};

const COTIZACION_GRATIS: WarehouseChangeQuote = {
  blNumber: BL_CAMBIO_GRATIS,
  country: 'CL',
  containerNumber: null,
  entitlement: {
    isFree: true,
    source: 'PORTAL',
    reference: `RULE:${REGLA_CAMBIO_GRATIS}`,
    reason: 'Cliente de alto volumen con cambio gratuito',
    maxUsesPerBl: 1,
    usedOnBl: 0,
  },
  tariffs: [],
  defaultTariffCode: null,
  canRequest: true,
};

/** Solicitud individual: gratuita completa sin cobro; si no, queda pendiente de pago (M3-04). */
function solicitudCambio(cuerpo: WarehouseChangeRequest): WarehouseChangeDetail {
  const gratis = cuerpo.blNumber === BL_CAMBIO_GRATIS;
  return {
    id: 'w3000000-0000-4000-8000-000000000001',
    billOfLadingId: BL_PRUEBA.id,
    blNumber: cuerpo.blNumber,
    containerNumber: cuerpo.containerNumber,
    fromWarehouse: cuerpo.fromWarehouse ?? 'STI',
    toWarehouse: cuerpo.toWarehouse,
    amount: gratis ? 0 : 9940,
    currency: 'CLP',
    status: gratis ? 'Completed' : 'Pending',
    country: 'CL',
    isFree: gratis,
    requiresPayment: !gratis,
    tariffCode: gratis ? null : (cuerpo.tariffCode ?? 'KTE'),
    tariffSource: gratis ? null : 'PORTAL',
    entitlementSource: gratis ? 'PORTAL' : null,
    entitlementReference: gratis ? `RULE:${REGLA_CAMBIO_GRATIS}` : null,
    batchId: null,
    createdAt: '2026-10-05T15:00:00Z',
    completedAt: gratis ? '2026-10-05T15:00:00Z' : null,
  };
}

const LINEAS_LOTE: WarehouseChangeBatchItem[] = [
  { lineNumber: 1, blNumber: BL_CAMBIO_GRATIS, containerNumber: null, fromWarehouse: 'STI', toWarehouse: 'Bodega 1', tariffCode: null, status: 'Succeeded', warehouseChangeId: 'w3000000-0000-4000-8000-000000000011', processedAt: '2026-10-05T15:00:05Z' },
  { lineNumber: 2, blNumber: BL_PRUEBA.blNumber, containerNumber: null, fromWarehouse: 'STI', toWarehouse: 'Bodega 1', tariffCode: null, status: 'Succeeded', warehouseChangeId: 'w3000000-0000-4000-8000-000000000012', processedAt: '2026-10-05T15:00:07Z' },
  { lineNumber: 3, blNumber: 'HLCUXXX000000', containerNumber: null, fromWarehouse: null, toWarehouse: 'Bodega 1', tariffCode: null, status: 'Failed', errorCode: 'BillOfLading.NotFound', errorMessage: 'Not found', processedAt: '2026-10-05T15:00:01Z' },
];

/** Orden en que se procesan las líneas: la 3 falla al recibirla (sin acceso) y luego la 1 y la 2. */
const ORDEN_LOTE = [2, 0, 1];

/** Avance de la solicitud masiva según la consulta: 1 y 2 de 3 en proceso y luego terminada con errores. */
function loteEnAvance(consulta: number): WarehouseChangeBatch {
  const procesadas = Math.max(0, Math.min(consulta + 1, LINEAS_LOTE.length));
  const hechas = new Set(ORDEN_LOTE.slice(0, procesadas));
  const items: WarehouseChangeBatchItem[] = LINEAS_LOTE.map((l, i) =>
    hechas.has(i) ? l : { ...l, status: 'Pending', errorCode: null, errorMessage: null, warehouseChangeId: null, processedAt: null },
  );
  const final = procesadas === LINEAS_LOTE.length;
  return {
    id: LOTE_CAMBIO_ALMACEN,
    status: final ? 'CompletedWithErrors' : 'Processing',
    totalItems: LINEAS_LOTE.length,
    processedItems: procesadas,
    succeededItems: items.filter((i) => i.status === 'Succeeded').length,
    failedItems: items.filter((i) => i.status === 'Failed').length,
    progressPercent: Math.floor((procesadas * 100) / LINEAS_LOTE.length),
    createdAt: '2026-10-05T15:00:00Z',
    startedAt: '2026-10-05T15:00:01Z',
    completedAt: final ? '2026-10-05T15:00:07Z' : null,
    items,
  };
}

function tarifa(datos: Partial<Tariff> & Pick<Tariff, 'id' | 'conceptCode'>): Tariff {
  return {
    conceptName: null,
    code: null,
    country: 'CL',
    currency: 'CLP',
    containerType: null,
    description: null,
    amount: 0,
    tierUnit: 'None',
    tierMode: 'Flat',
    tiers: [],
    validFrom: '2026-10-01',
    validTo: null,
    isActive: true,
    createdAt: '2026-09-30T12:00:00Z',
    createdBy: 'admin@hapag-lloyd.cl',
    modifiedAt: null,
    modifiedBy: null,
    ...datos,
  };
}

const TARIFAS: Tariff[] = [
  tarifa({ id: 'f3000000-0000-4000-8000-000000000001', conceptCode: 'WAREHOUSE_CHANGE', conceptName: 'Cambio de almacén', code: 'KTE', amount: 9940, description: 'Cambio de almacén (KTE)' }),
  tarifa({ id: 'f3000000-0000-4000-8000-000000000002', conceptCode: 'WAREHOUSE_CHANGE', conceptName: 'Cambio de almacén', code: 'KTF', amount: 110910, description: 'Cambio de almacén (KTF)' }),
  tarifa({
    id: TARIFA_TRAMOS,
    conceptCode: 'LATE_ARRIVAL',
    conceptName: 'Llegada tardía',
    currency: 'USD',
    tierUnit: 'Hours',
    tierMode: 'Flat',
    tiers: [
      { fromUnit: 0, toUnit: 24, amount: 100 },
      { fromUnit: 25, toUnit: 48, amount: 200 },
      { fromUnit: 49, toUnit: null, amount: 350 },
    ],
    modifiedAt: '2026-10-03T14:00:00Z',
    modifiedBy: 'tarifas@hapag-lloyd.cl',
  }),
];

const CONCEPTOS: ChargeConcept[] = [
  { code: 'WAREHOUSE_CHANGE', name: 'Cambio de almacén', category: 'Service', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 13 },
  { code: 'LATE_ARRIVAL', name: 'Llegada tardía', category: 'Service', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 14 },
  { code: 'DEMURRAGE', name: 'Demurrage', category: 'Demurrage', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 11 },
];

/** Valor vigente de la tarifa con tramos, tal como lo guarda el registro de cambios (NF-15). */
const SNAPSHOT_TRAMOS: TariffSnapshot = {
  conceptCode: 'LATE_ARRIVAL',
  code: null,
  country: 'CL',
  currency: 'USD',
  containerType: null,
  description: null,
  amount: 0,
  tierUnit: 'Hours',
  tierMode: 'Flat',
  tiers: TARIFAS[2].tiers,
  validFrom: '2026-10-01',
  validTo: null,
  isActive: true,
};

const HISTORIAL_TARIFA: TariffChange[] = [
  {
    id: 'h3000000-0000-4000-8000-000000000002',
    tariffId: TARIFA_TRAMOS,
    action: 'Updated',
    changedAt: '2026-10-03T14:00:00Z',
    changedBy: 'tarifas@hapag-lloyd.cl',
    changedByUserId: 'u0000000-0000-4000-8000-000000000020',
    previous: { ...SNAPSHOT_TRAMOS, tiers: [{ fromUnit: 0, toUnit: 24, amount: 90 }, { fromUnit: 25, toUnit: null, amount: 180 }] },
    current: SNAPSHOT_TRAMOS,
  },
  {
    id: 'h3000000-0000-4000-8000-000000000001',
    tariffId: TARIFA_TRAMOS,
    action: 'Created',
    changedAt: '2026-09-30T12:00:00Z',
    changedBy: 'admin@hapag-lloyd.cl',
    changedByUserId: 'u0000000-0000-4000-8000-000000000099',
    previous: null,
    current: { ...SNAPSHOT_TRAMOS, tiers: [{ fromUnit: 0, toUnit: 24, amount: 90 }, { fromUnit: 25, toUnit: null, amount: 180 }] },
  },
];

const REGLAS: InternalChargeRule[] = [
  {
    id: REGLA_CAMBIO_GRATIS,
    ruleType: 'FreeWarehouseChange',
    country: 'CL',
    taxId: '76123456-7',
    matchCode: ORGANIZACION_PRUEBA.matchCode,
    accountName: ORGANIZACION_PRUEBA.name,
    reason: 'Cliente de alto volumen con cambio gratuito',
    maxUsesPerBl: 1,
    validFrom: '2026-01-01',
    validTo: null,
    isActive: true,
    createdAt: '2026-09-30T12:00:00Z',
    modifiedAt: null,
  },
  {
    id: 'f4000000-0000-4000-8000-000000000002',
    ruleType: 'AdvanceDemurrageRequired',
    country: 'BO',
    taxId: '1023456029',
    matchCode: 'BOALTI01',
    accountName: 'Altiplano Importaciones SRL',
    reason: 'Cuenta sujeta a demoras anticipadas según reglas internas',
    maxUsesPerBl: null,
    validFrom: '2026-01-01',
    validTo: '2026-12-31',
    isActive: true,
    createdAt: '2026-09-30T12:00:00Z',
    modifiedAt: null,
  },
];

const HISTORIAL_REGLA: InternalChargeRuleChange[] = [
  {
    id: 'h4000000-0000-4000-8000-000000000001',
    ruleId: REGLA_CAMBIO_GRATIS,
    action: 'Created',
    changedAt: '2026-09-30T12:00:00Z',
    changedBy: 'admin@hapag-lloyd.cl',
    changedByUserId: 'u0000000-0000-4000-8000-000000000099',
    previous: null,
    current: { ...REGLAS[0], isActive: true },
  },
];

/** Respuestas GET de la Ola C (se agregan a RESPUESTAS). */
const RESPUESTAS_OLA_C: Record<string, unknown> = {
  ...Object.fromEntries(Object.entries(CARGOS_OLA_C).map(([bl, c]) => [`charges/${bl}`, c])),
  ...Object.fromEntries(Object.entries(DEMURRAGE_OLA_C).map(([bl, d]) => [`demurrage/${bl}/status`, d])),
  'exchange-rates': tipoDeCambio,
  [`warehouse-changes/quote/${BL_PRUEBA.blNumber}`]: COTIZACION_TARIFAS,
  [`warehouse-changes/quote/${BL_CAMBIO_GRATIS}`]: COTIZACION_GRATIS,
  tariffs: TARIFAS,
  'tariffs/concepts': CONCEPTOS,
  ...Object.fromEntries(TARIFAS.map((t) => [`tariffs/${t.id}`, t])),
  [`tariffs/${TARIFA_TRAMOS}/history`]: HISTORIAL_TARIFA,
  'internal-charge-rules': REGLAS,
  [`internal-charge-rules/${REGLA_CAMBIO_GRATIS}/history`]: HISTORIAL_REGLA,
};

/** Escrituras de la Ola C con parámetros en la ruta o que dependen del cuerpo enviado. */
const ESCRITURAS_OLA_C: { metodo: string; patron: RegExp; responder: (cuerpo: unknown, m: RegExpMatchArray) => Escritura }[] = [
  {
    metodo: 'POST',
    patron: /^charges\/([^/]+)\/apply-rules$/,
    responder: (_c, m) => {
      if (m[1] !== BL_EXENTO) return { status: 400, body: { title: 'ChargeRules.NoChargesToApply', detail: 'No pending charges.' } };
      const aplicados = cargosExentos('2026-10-05T15:05:00Z');
      return { status: 200, body: { completed: true, requiresPayment: false, exemptedCharges: aplicados.charges, payableChargeIds: [], charges: aplicados } };
    },
  },
  {
    metodo: 'POST',
    patron: /^demurrage\/([^/]+)\/calculate$/,
    responder: (cuerpo, m) => {
      const estado = DEMURRAGE_OLA_C[m[1]];
      if (estado && !estado.calculatorEnabled) {
        return { status: 400, body: { title: 'Demurrage.InvoiceExists', detail: 'Invoice exists.' } };
      }
      return { status: 200, body: calculoDemurrage(cuerpo as CalculateDemurrageRequest) };
    },
  },
  {
    metodo: 'POST',
    patron: /^demurrage\/([^/]+)\/advance-demurrage$/,
    responder: (_c, m) => {
      const estado = DEMURRAGE_OLA_C[m[1]];
      if (!estado?.advance.required) return { status: 400, body: { title: 'Demurrage.AdvanceNotRequired', detail: 'Not required.' } };
      return {
        status: 200,
        body: { ...estado, advance: { ...estado.advance, status: 'Pending', chargeId: 'd3000000-0000-4000-8000-000000000301' } },
      };
    },
  },
  {
    metodo: 'POST',
    patron: /^warehouse-changes\/requests$/,
    responder: (cuerpo) => ({ status: 200, body: solicitudCambio(cuerpo as WarehouseChangeRequest) }),
  },
  {
    metodo: 'POST',
    patron: /^warehouse-changes\/bulk$/,
    responder: (cuerpo) => ({
      status: 202,
      body: {
        ...loteEnAvance(-1),
        status: 'Queued',
        totalItems: (cuerpo as { items: unknown[] }).items.length,
        processedItems: 0,
        succeededItems: 0,
        failedItems: 0,
        progressPercent: 0,
        items: null,
      },
    }),
  },
  { metodo: 'POST', patron: /^tariffs$/, responder: (cuerpo) => ({ status: 201, body: tarifa({ ...(cuerpo as TariffRequest), id: 'f3000000-0000-4000-8000-000000000009' }) }) },
  {
    metodo: 'PUT',
    patron: /^tariffs\/([^/]+)$/,
    responder: (cuerpo, m) => {
      const actual = TARIFAS.find((t) => t.id === m[1]) ?? TARIFAS[0];
      return { status: 200, body: { ...actual, ...(cuerpo as TariffRequest), modifiedAt: '2026-10-05T15:10:00Z', modifiedBy: 'admin@hapag-lloyd.cl' } };
    },
  },
  { metodo: 'DELETE', patron: /^tariffs\/([^/]+)$/, responder: () => ({ status: 204 }) },
  { metodo: 'POST', patron: /^internal-charge-rules$/, responder: (cuerpo) => ({ status: 200, body: { ...REGLAS[0], ...(cuerpo as object), id: 'f4000000-0000-4000-8000-000000000009' } }) },
  {
    metodo: 'PUT',
    patron: /^internal-charge-rules\/([^/]+)$/,
    responder: (cuerpo, m) => ({ status: 200, body: { ...(REGLAS.find((r) => r.id === m[1]) ?? REGLAS[0]), ...(cuerpo as object), modifiedAt: '2026-10-05T15:10:00Z' } }),
  },
  { metodo: 'DELETE', patron: /^internal-charge-rules\/([^/]+)$/, responder: () => ({ status: 204 }) },
];

/** Tarifarios oficiales que enlaza Tarifas locales (los mismos de appsettings del backend). */
export const URL_TARIFAS = {
  INLAND_CL: 'https://www.hapag-lloyd.com/en/services-information/offices-localinfo/latin-america/chile.html',
  DEMURRAGE_DETENTION: 'https://www.hapag-lloyd.com/es/online-business/quotation/detention-demurrage/latin-america.html',
  LOCAL_CHARGES: 'https://www.hapag-lloyd.com/es/online-business/quotation/tariffs/local-charges-service-fees.html',
} as const;

/**
 * GET /config/external-links: portal de devoluciones (por defecto sin URL, como hoy) y tarifarios oficiales. `refunds`
 * configura la URL del portal; `sinTarifa` deja un tarifario sin configurar.
 */
export function enlacesExternos(pais: string, opciones: { refunds?: string | null; sinTarifa?: string[] } = {}): unknown {
  const sinTarifa = new Set(opciones.sinTarifa ?? []);
  return {
    country: pais,
    refunds: opciones.refunds
      ? { code: 'REFUNDS', url: opciones.refunds, configured: true, source: 'Setting' }
      : { code: 'REFUNDS', url: null, configured: false, source: 'None' },
    localTariffs: Object.entries(URL_TARIFAS).map(([code, url]) =>
      sinTarifa.has(code)
        ? { code, url: null, configured: false, source: 'None' }
        : { code, url, configured: true, source: 'AppSettings' },
    ),
  };
}

type RespuestaGet = unknown | ((url: URL) => unknown);

/** Respuestas por ruta relativa a /api/v1/ (método GET); una función recibe la URL con sus query params. */
const RESPUESTAS: Record<string, RespuestaGet> = {
  'bills-of-lading/my': [BL_PRUEBA],
  [`bills-of-lading/${BL_PRUEBA.blNumber}`]: BL_PRUEBA,
  'payments/my': PAGOS,
  'notifications/unread-count': { count: NOTIFICACIONES.filter((n) => !n.isRead).length },
  notifications: NOTIFICACIONES,
  shipments: buscarEmbarques,
  ...Object.fromEntries(Object.entries(DETALLES).map(([bl, detalle]) => [`shipments/${bl}`, detalle])),
  'organizations/me': ORGANIZACION_PRUEBA,
  'organizations/me/users': USUARIOS_ORGANIZACION,
  'organizations/me/join-requests': SOLICITUDES,
  'organizations/me/documents': DOCUMENTOS,
  'organizations/me/operating-country': { country: 'CL', availableCountries: ['CL', 'BO'], canChange: true },
  'config/external-links': (url: URL) => enlacesExternos(url.searchParams.get('country') ?? 'CL'),
  'admin/organizations': (url: URL) => {
    const estado = url.searchParams.get('status');
    return paginar(
      ORGANIZACIONES_ADMIN.filter((o) => !estado || o.status === estado),
      url,
    );
  },
  [`admin/organizations/${ORGANIZACION_EN_REVISION}`]: DETALLE_ORGANIZACION_ADMIN,
  'access-matrix': MATRIZ,
  // Ola B
  'access/grants': buscarAccesos,
  'access/grantees': (url: URL) => {
    const texto = url.searchParams.get('search')?.trim().toLowerCase() ?? '';
    const tipo = url.searchParams.get('organizationType');
    return DESTINATARIOS.filter(
      (d) => (!tipo || d.organizationType === tipo) && (!texto || d.name.toLowerCase().includes(texto) || d.taxId.includes(texto)),
    );
  },
  'access/grantable-actions': ACCIONES_OTORGABLES,
  'access/mandate-terms': {
    version: 'MANDATO-2026-10',
    title: 'Mandato digital',
    summary: 'El mandatario actúa en representación del mandante dentro del alcance y la vigencia indicados.',
  },
  'access/defaults': DEFECTO,
  'access/open-access': ACCESO_ABIERTO,
  'access/audit': buscarAuditoria,
  [`shipments/${BL_PRUEBA.blNumber}/visibility-widenings`]: AMPLIACIONES,
  // Ola C
  ...RESPUESTAS_OLA_C,
};

type Escritura = { status: number; body?: unknown };

/** Respuestas de escritura por `MÉTODO ruta`; el resto responde 200 sin cuerpo. */
const ESCRITURAS: Record<string, Escritura> = {
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
  // Ola B
  'POST access/grants/revoke': { status: 200, body: { revoked: 2, cascadeRevoked: 0, wideningsRevoked: 0 } },
  'POST access/defaults': {
    status: 201,
    body: { ...DEFECTO[0], id: 'd1000000-0000-4000-8000-000000000002', grantee: referencia(TRANSPORTISTA), durationDays: 90 },
  },
  'POST access/grants/early-booking': {
    status: 200,
    body: acceso({ id: 'a3000000-0000-4000-8000-000000000001', billOfLadingId: null, blNumber: null, bookingNumber: 'BKG26039999', grantType: 'EarlyBooking', intendedRole: 'Shipper' }),
  },
};

/** Escrituras con parámetros en la ruta o que dependen del cuerpo enviado (Ola B). */
const ESCRITURAS_DINAMICAS: { metodo: string; patron: RegExp; responder: (cuerpo: unknown, m: RegExpMatchArray) => Escritura }[] = [
  {
    metodo: 'POST',
    patron: /^access\/grants(\/bulk)?$/,
    responder: (cuerpo) => ({ status: 200, body: resultadoOtorgamiento(cuerpo as { blNumbers?: string[] }) }),
  },
  {
    metodo: 'POST',
    patron: /^access\/grants\/([^/]+)\/revoke$/,
    responder: () => ({ status: 200, body: { revoked: 1, cascadeRevoked: 2, wideningsRevoked: 1 } }),
  },
  {
    metodo: 'POST',
    patron: /^access\/grants\/([^/]+)\/accept-terms$/,
    responder: (_c, m) => ({
      status: 200,
      body: { ...(ACCESOS.find((a) => a.id === m[1]) ?? ACCESO_AGENCIA), status: 'Active', isEffective: true, termsVersion: 'MANDATO-2026-10' },
    }),
  },
  {
    metodo: 'PUT',
    patron: /^access\/grants\/([^/]+)$/,
    responder: (cuerpo, m) => {
      const actual = ACCESOS.find((a) => a.id === m[1]) ?? ACCESO_AGENCIA;
      const cambios = cuerpo as { actionCodes?: string[]; validityType?: AccessGrant['validityType']; validTo?: string };
      return {
        status: 200,
        body: {
          ...actual,
          ...(cambios.validityType ? { validityType: cambios.validityType, validTo: cambios.validTo ?? null } : {}),
          ...(cambios.actionCodes ? { actionCodes: ['shipment.view', ...cambios.actionCodes], effectiveActionCodes: ['shipment.view', ...cambios.actionCodes] } : {}),
        },
      };
    },
  },
  {
    metodo: 'PUT',
    patron: /^access\/defaults\/([^/]+)$/,
    responder: (cuerpo) => ({ status: 200, body: { ...DEFECTO[0], ...(cuerpo as object), modifiedAt: '2026-10-05T12:00:00Z' } }),
  },
  { metodo: 'DELETE', patron: /^access\/defaults\/([^/]+)$/, responder: () => ({ status: 204 }) },
  {
    metodo: 'PUT',
    patron: /^access\/open-access$/,
    responder: (cuerpo) => {
      const c = cuerpo as { isEnabled: boolean; actionCodes?: string[] };
      return {
        status: 200,
        body: {
          ...ACCESO_ABIERTO,
          isEnabled: c.isEnabled,
          actionCodes: c.actionCodes ?? null,
          effectiveActionCodes: c.actionCodes ? ['shipment.view', ...c.actionCodes] : ACCESO_ABIERTO.effectiveActionCodes,
          changedAt: '2026-10-05T12:00:00Z',
        },
      };
    },
  },
  {
    metodo: 'POST',
    patron: /^shipments\/([^/]+)\/associate$/,
    responder: (_c, m) => ({
      status: 200,
      body: { billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000009', blNumber: m[1], associatedAt: '2026-10-05T12:00:00Z' },
    }),
  },
  {
    metodo: 'POST',
    patron: /^shipments\/([^/]+)\/visibility-widenings$/,
    responder: () => ({ status: 200, body: AMPLIACIONES }),
  },
  { metodo: 'POST', patron: /^shipments\/([^/]+)\/visibility-widenings\/([^/]+)\/revoke$/, responder: () => ({ status: 204 }) },
  // Ola C
  ...ESCRITURAS_OLA_C,
];

/**
 * Intercepta /api/v1/**: rutas conocidas con datos ficticios; cualquier otra GET responde [].
 * El avance de la solicitud masiva (Ola C) cambia con cada consulta de la página.
 * Ola D: el carro, los pagos, las facturas y la configuración de pagos los responde una simulación con
 * estado por página (ola-d-mocks.ts); `opciones` activa el crédito (M5-07), el bloqueo de pagos (M8-07)
 * o cierres sin respuesta (NF-01).
 * Ola E: los documentos del embarque los responde otra simulación con estado (ola-e-mocks.ts), que agrega
 * al carro de la Ola D el cargo del certificado de transbordo y levanta el bloqueo FFWW de los cargos al
 * emitirse la carta de responsabilidad.
 * Ola F: dashboard, emisión y TATC del BL, reglas de publicación, enlace de Dispute, asistente y buscador DG los
 * responde ola-f-mocks.ts, que además agrega al listado de embarques el estado de emisión y la publicación.
 * Fase 2, Ola G: servicios on demand, solicitudes, bandeja interna, mantenedor de definiciones e historial del cambio de
 * almacén los responde ola-g-mocks.ts, que registra en el carro de la Ola D los cargos que generan las solicitudes.
 * Fase 2, Ola H: estado de cuenta, cierre por ítem con crédito, comprobantes de depósito, anticipos, conceptos imputables,
 * refacturación IAO y los ajustes de la Ola G los responde ola-h-mocks.ts, que usa el carro y las facturas de la Ola D.
 * Fase 2, Ola I: bandeja con acciones y preferencias, comunicados, guías, área de administración, vista como cliente
 * (escrituras bloqueadas con su token), reportería, Counter, listas de contactos, transportistas pre-creados y empresa
 * matriz los responde ola-i-mocks.ts, que además agrega al listado los BL de la filial y el Counter al detalle interno.
 * Fase 2, Ola J: certificado de flete, carta de liberación y su revisión interna, entrega de documentos por el asistente y
 * clientes del canal Web Service los responde ola-j-mocks.ts, que publica los documentos emitidos en el repositorio de la Ola E.
 */
export async function simularApi(page: Page, opciones: OpcionesOlaD & OpcionesOlaI = {}): Promise<void> {
  let consultasLote = 0;
  const olaD = new SimulacionOlaD(opciones);
  const olaE = new SimulacionOlaE(olaD);
  const olaF = new SimulacionOlaF();
  const olaG = new SimulacionOlaG(olaD);
  const olaH = new SimulacionOlaH(olaD, opciones);
  const olaI = new SimulacionOlaI(opciones);
  const olaJ = new SimulacionOlaJ(olaE);
  await page.route('**/api/v1/**', async (route: Route) => {
    const request = route.request();
    const url = new URL(request.url());
    const ruta = url.pathname.replace(/^.*\/api\/v1\//, '').replace(/\/$/, '');
    const metodo = request.method();

    if (await olaJ.responder(route, ruta, metodo, url)) return;
    if (await olaI.responder(route, ruta, metodo, url)) return;
    if (await olaH.responder(route, ruta, metodo, url)) return;
    if (await olaG.responder(route, ruta, metodo, url)) return;
    if (await olaF.responder(route, ruta, metodo, url)) return;
    if (await olaE.responder(route, ruta, metodo)) return;
    if (await olaD.responder(route, ruta, metodo, url)) return;

    if (metodo !== 'GET') {
      let escritura: Escritura | undefined = ESCRITURAS[`${metodo} ${ruta}`];
      if (!escritura) {
        for (const d of ESCRITURAS_DINAMICAS) {
          const m = d.metodo === metodo ? ruta.match(d.patron) : null;
          if (m) {
            escritura = d.responder(request.postDataJSON(), m);
            break;
          }
        }
      }
      await route.fulfill({
        status: escritura?.status ?? 200,
        contentType: 'application/json',
        body: escritura?.body === undefined ? '' : JSON.stringify(escritura.body),
      });
      return;
    }

    const respuesta = ruta in RESPUESTAS ? RESPUESTAS[ruta] : [];
    const cuerpo = ruta === `warehouse-changes/bulk/${LOTE_CAMBIO_ALMACEN}`
      ? loteEnAvance(consultasLote++)
      : typeof respuesta === 'function' ? respuesta(url) : respuesta;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(olaI.ajustarDetalle(ruta, request, olaI.ajustar(ruta, url, olaG.ajustar(ruta, olaF.ajustar(ruta, olaE.ajustar(ruta, cuerpo)))))),
    });
  });
}
