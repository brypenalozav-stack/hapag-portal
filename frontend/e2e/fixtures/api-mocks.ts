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
import { USUARIO_PRUEBA } from './session';

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

/** Respuestas por ruta relativa a /api/v1/ (método GET). */
const RESPUESTAS: Record<string, unknown> = {
  'bills-of-lading/my': [BL_PRUEBA],
  [`bills-of-lading/${BL_PRUEBA.blNumber}`]: BL_PRUEBA,
  [`bills-of-lading/${BL_PRUEBA.blNumber}/charges`]: CARGOS_BL,
  [`bills-of-lading/${BL_PRUEBA.blNumber}/demurrage`]: DEMURRAGE,
  'payments/my': PAGOS,
  'notifications/unread-count': { count: NOTIFICACIONES.filter((n) => !n.isRead).length },
  notifications: NOTIFICACIONES,
  'receipts/my': RECIBOS,
};

/** Intercepta /api/v1/**: rutas conocidas con datos ficticios; cualquier otra responde []. */
export async function simularApi(page: Page): Promise<void> {
  await page.route('**/api/v1/**', async (route: Route) => {
    const url = new URL(route.request().url());
    const ruta = url.pathname.replace(/^.*\/api\/v1\//, '').replace(/\/$/, '');
    const cuerpo = route.request().method() === 'GET' && ruta in RESPUESTAS ? RESPUESTAS[ruta] : [];
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(cuerpo),
    });
  });
}
