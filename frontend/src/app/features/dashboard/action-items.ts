import {
  Dashboard,
  DashboardAmount,
  DashboardDemurrageItem,
  DashboardPayable,
  DashboardRequest,
  DashboardTarget,
} from '../../core/models/dashboard.model';

/**
 * Bandeja "Requiere su acción" del dashboard (cierre de Fase 1, UX): los pendientes de GET /dashboard (cargos,
 * facturas, demurrage en riesgo y solicitudes que esperan algo del cliente) agrupados por BL y ordenados por urgencia.
 * Cada grupo tiene sus motivos en texto y una sola acción principal. El servidor no los agrupa: se arman aquí.
 */

/** Urgencia del grupo: el texto del badge la nombra, el color solo la refuerza. */
export type ActionUrgency = 'overdue' | 'urgent' | 'pending';

const URGENCY_RANK: Record<ActionUrgency, number> = { overdue: 0, urgent: 1, pending: 2 };

/** Etiqueta y clase del badge de urgencia (claves literales para check:i18n). */
export const ACTION_URGENCY_KEYS: Record<ActionUrgency, string> = {
  overdue: 'dashboard.action.urgency.overdue',
  urgent: 'dashboard.action.urgency.urgent',
  pending: 'dashboard.action.urgency.pending',
};

export const ACTION_URGENCY_CLASS: Record<ActionUrgency, string> = {
  overdue: 'hl-badge--failed',
  urgent: 'hl-badge--pending',
  pending: 'hl-badge--processing',
};

/** Días antes del vencimiento en que una factura pasa a "Urgente". */
export const DUE_SOON_DAYS = 7;

/** Motivo del grupo: clave de texto con su número (plural ICU) o la referencia, y opcionalmente el estado. */
export interface ActionReason {
  key: string;
  count?: number;
  reference?: string;
  /** Código del estado que acompaña al motivo (estado del demurrage, M3-18). */
  state?: string;
}

/** Acción principal del grupo: agregar al carro (diálogo) o ir a la pantalla donde se resuelve. */
export type ActionPrimary =
  | { kind: 'addToCart'; labelKey: string; payables: DashboardPayable[] }
  | { kind: 'link'; labelKey: string; route: string[]; queryParams: Record<string, string> | null };

export interface ActionGroup {
  /** Clave estable del grupo (BL, factura o solicitud). */
  key: string;
  blNumber: string | null;
  /** Clave del título del grupo ("BL {{ reference }}", "Factura {{ reference }}"…) y su referencia. */
  titleKey: string;
  reference: string;
  urgency: ActionUrgency;
  /** Vencimiento más próximo (DateOnly) si lo hay. */
  dueDate: string | null;
  reasons: ActionReason[];
  amounts: DashboardAmount[];
  /** Todos los pendientes de pago del grupo ya están en el carro. */
  allInCart: boolean;
  primary: ActionPrimary;
}

/** Claves de los motivos y acciones (literales: check:i18n las encuentra). */
const REASON = {
  charges: 'dashboard.action.reason.charges',
  demurrageLines: 'dashboard.action.reason.demurrageLines',
  demurrageAtRisk: 'dashboard.action.reason.demurrageAtRisk',
  invoice: 'dashboard.action.reason.invoice',
  inCart: 'dashboard.action.reason.inCart',
  requestDraft: 'dashboard.action.reason.requestDraft',
  requestPendingPayment: 'dashboard.action.reason.requestPendingPayment',
  releaseLetterRejected: 'dashboard.action.reason.releaseLetterRejected',
} as const;

const DO = {
  addToCart: 'dashboard.action.do.addToCart',
  goToCart: 'dashboard.action.do.goToCart',
  payAccount: 'dashboard.action.do.payAccount',
  viewCharges: 'dashboard.action.do.viewCharges',
  reviewDemurrage: 'dashboard.action.do.reviewDemurrage',
  continueRequest: 'dashboard.action.do.continueRequest',
  payRequest: 'dashboard.action.do.payRequest',
  uploadLetter: 'dashboard.action.do.uploadLetter',
} as const;

const TITLE = {
  bl: 'dashboard.action.title.bl',
  invoice: 'dashboard.action.title.invoice',
  other: 'dashboard.action.title.other',
} as const;

/** Cómo paga la organización: carro de compra, pago desde la cuenta (crédito) o ninguno (usuario interno). */
export type PaymentMode = 'cart' | 'account' | 'none';

interface Builder {
  key: string;
  blNumber: string | null;
  titleKey: string;
  reference: string;
  payables: DashboardPayable[];
  demurrage: DashboardDemurrageItem | null;
  requests: DashboardRequest[];
}

/** Ruta de la pantalla donde se gestiona el elemento del dashboard. */
export function targetRoute(target: DashboardTarget): string[] {
  const bl = target.blNumber ?? '';
  switch (target.kind) {
    case 'Shipment':
      return bl ? ['/shipments', bl] : ['/shipments'];
    case 'Charges':
      return bl ? ['/charges', bl] : ['/charges'];
    case 'Demurrage':
      return bl ? ['/demurrage', bl] : ['/demurrage'];
    case 'Invoice':
      return ['/invoices'];
    case 'Documents':
    case 'BlCopy':
    case 'ResponsibilityLetter':
      return bl ? ['/shipments', bl, 'documents'] : ['/shipments'];
    case 'WarehouseChangeBatch':
      return target.id ? ['/warehouse/bulk', target.id] : ['/warehouse'];
    case 'WarehouseChange':
      return ['/warehouse'];
    case 'ServiceOrder':
      return ['/service-orders'];
    case 'TatcBatch':
      return ['/tatc'];
    case 'ServiceRequest':
      return target.id ? ['/service-requests', target.id] : ['/service-requests'];
    // Carta de liberación (M6-08): se sigue en su propia página, también con los servicios on demand apagados.
    case 'ReleaseLetter':
      return target.id ? ['/release-letters', target.id] : ['/release-letter'];
    default:
      return ['/dashboard'];
  }
}

/** La solicitud masiva de TATC se abre con su resultado (M2-09). */
export function targetQueryParams(target: DashboardTarget): Record<string, string> | null {
  return target.kind === 'TatcBatch' && target.id ? { batch: target.id } : null;
}

/** Carta de liberación rechazada sin otra más reciente para el mismo BL: hay que subir una nueva. */
function rejectedLetters(requests: DashboardRequest[]): DashboardRequest[] {
  const letters = requests.filter((r) => r.target.kind === 'ReleaseLetter');
  return letters.filter((r) => r.status === 'Rejected'
    && !letters.some((o) => o !== r && o.blNumber === r.blNumber && o.createdAt > r.createdAt));
}

/** Solicitudes que esperan algo del cliente: borradores, pago pendiente y cartas de liberación rechazadas. */
export function actionableRequests(requests: DashboardRequest[]): DashboardRequest[] {
  const rejected = new Set(rejectedLetters(requests));
  return requests.filter((r) => r.status === 'Draft' || r.status === 'PendingPayment' || rejected.has(r));
}

function addDays(date: string, days: number): string {
  const d = new Date(`${date}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

function sumByCurrency(items: { currency: string; total: number }[]): DashboardAmount[] {
  const totals = new Map<string, number>();
  for (const item of items) totals.set(item.currency, (totals.get(item.currency) ?? 0) + item.total);
  return [...totals.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([currency, total]) => ({ currency, total }));
}

function requestReason(request: DashboardRequest): ActionReason {
  if (request.status === 'Draft') return { key: REASON.requestDraft, reference: request.reference };
  if (request.status === 'PendingPayment') return { key: REASON.requestPendingPayment, reference: request.reference };
  return { key: REASON.releaseLetterRejected, reference: request.reference };
}

function requestAction(request: DashboardRequest): ActionPrimary {
  if (request.status === 'Rejected') {
    return {
      kind: 'link', labelKey: DO.uploadLetter, route: ['/release-letter'],
      queryParams: request.blNumber ? { bl: request.blNumber } : null,
    };
  }
  return {
    kind: 'link', labelKey: request.status === 'Draft' ? DO.continueRequest : DO.payRequest,
    route: targetRoute(request.target), queryParams: targetQueryParams(request.target),
  };
}

function primaryAction(b: Builder, pending: DashboardPayable[], payment: PaymentMode): ActionPrimary {
  if (b.payables.length > 0) {
    if (payment === 'account') return { kind: 'link', labelKey: DO.payAccount, route: ['/account-payments'], queryParams: null };
    if (payment === 'cart') {
      return pending.length > 0
        ? { kind: 'addToCart', labelKey: DO.addToCart, payables: pending }
        : { kind: 'link', labelKey: DO.goToCart, route: ['/cart'], queryParams: null };
    }
    const target = b.payables[0].target;
    return { kind: 'link', labelKey: DO.viewCharges, route: targetRoute(target), queryParams: targetQueryParams(target) };
  }
  if (b.requests.length > 0) return requestAction(b.requests[0]);
  return { kind: 'link', labelKey: DO.reviewDemurrage, route: ['/demurrage', b.blNumber ?? ''], queryParams: null };
}

/**
 * Agrupa los pendientes del dashboard por BL (las facturas y solicitudes sin BL quedan solas) y los ordena: vencidos,
 * urgentes (vencen en {@link DUE_SOON_DAYS} días o acumulan demurrage) y el resto; dentro de cada nivel, por
 * vencimiento y referencia. El demurrage en riesgo se suma al BL sin repetir sus líneas ya pendientes de pago.
 */
export function buildActionGroups(
  dashboard: Dashboard,
  payment: PaymentMode,
  inCart: (item: DashboardPayable) => boolean,
): ActionGroup[] {
  const groups = new Map<string, Builder>();
  const group = (key: string, init: () => Omit<Builder, 'key' | 'payables' | 'demurrage' | 'requests'>): Builder => {
    let b = groups.get(key);
    if (!b) {
      b = { key, ...init(), payables: [], demurrage: null, requests: [] };
      groups.set(key, b);
    }
    return b;
  };
  const byBl = (bl: string) => group(`bl:${bl}`, () => ({ blNumber: bl, titleKey: TITLE.bl, reference: bl }));

  for (const item of dashboard.pendingPayments.items) {
    const b = item.blNumber
      ? byBl(item.blNumber)
      : group(`${item.itemType}:${item.sourceId}`, () => ({
        blNumber: null,
        titleKey: item.itemType === 'Invoice' ? TITLE.invoice : TITLE.other,
        reference: item.description ?? item.bookingNumber ?? item.conceptCode,
      }));
    b.payables.push(item);
  }
  for (const item of dashboard.indicators.demurrageAtRisk.items) {
    byBl(item.blNumber).demurrage = item;
  }
  for (const request of actionableRequests(dashboard.requests.items)) {
    const b = request.blNumber
      ? byBl(request.blNumber)
      : group(`request:${request.id}`, () => ({ blNumber: null, titleKey: TITLE.other, reference: request.reference }));
    b.requests.push(request);
  }

  const today = dashboard.generatedAt.slice(0, 10);
  const soon = addDays(today, DUE_SOON_DAYS);

  const result = [...groups.values()].map((b): ActionGroup => {
    const dueDates = b.payables.map((p) => p.dueDate).filter((d): d is string => !!d).sort();
    const dueDate = dueDates[0] ?? null;
    const demurrageLines = b.payables.filter((p) => p.itemType === 'Demurrage').length;
    const invoices = b.payables.filter((p) => p.itemType === 'Invoice').length;
    const charges = b.payables.length - demurrageLines - invoices;
    const pending = b.payables.filter((p) => !inCart(p));

    const overdue = b.payables.some((p) => p.status === 'Overdue') || (dueDate !== null && dueDate < today);
    const urgent = (dueDate !== null && dueDate <= soon) || demurrageLines > 0 || b.demurrage !== null;
    const urgency: ActionUrgency = overdue ? 'overdue' : urgent ? 'urgent' : 'pending';

    const reasons: ActionReason[] = [];
    if (charges > 0) reasons.push({ key: REASON.charges, count: charges });
    if (invoices > 0) reasons.push({ key: REASON.invoice, count: invoices });
    if (demurrageLines > 0) {
      reasons.push({ key: REASON.demurrageLines, count: demurrageLines });
    } else if (b.demurrage) {
      reasons.push({ key: REASON.demurrageAtRisk, state: b.demurrage.state });
    }
    for (const request of b.requests) reasons.push(requestReason(request));
    const inCartCount = b.payables.length - pending.length;
    if (inCartCount > 0) reasons.push({ key: REASON.inCart, count: inCartCount });

    const amounts = b.payables.length > 0
      ? sumByCurrency(b.payables.map((p) => ({ currency: p.currency, total: p.totalAmount })))
      : (b.demurrage?.amounts ?? []);

    return {
      key: b.key,
      blNumber: b.blNumber,
      titleKey: b.titleKey,
      reference: b.reference,
      urgency,
      dueDate,
      reasons,
      amounts,
      allInCart: b.payables.length > 0 && pending.length === 0,
      primary: primaryAction(b, pending, payment),
    };
  });

  return result.sort((a, b) =>
    URGENCY_RANK[a.urgency] - URGENCY_RANK[b.urgency]
    || (a.dueDate ?? '9999-12-31').localeCompare(b.dueDate ?? '9999-12-31')
    || a.reference.localeCompare(b.reference));
}
