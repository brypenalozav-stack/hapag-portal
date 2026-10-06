import { Request, Route } from '@playwright/test';
import type {
  DocumentActions,
  NoDebtEligibility,
  RelatedDocument,
  ResponsibilityLetterState,
  ShipmentDocument,
  ShipmentDocuments,
  TransshipmentRequest,
} from '../../src/app/core/models/document.model';
import type { PaymentStatusDetail } from '../../src/app/core/models/cart.model';
import type { ShipmentCharges } from '../../src/app/core/models/charges.model';
import { ORGANIZACION_PRUEBA, USUARIO_PRUEBA } from './session';
import type { SimulacionOlaD } from './ola-d-mocks';

/**
 * Backend simulado de la Ola E (documentos del embarque, M6-01, M6-03 a M6-07, M6-09). Guarda estado por
 * página: la primera descarga de un documento de demostración lo firma, la copia del BL, la carta y el CLD
 * se agregan al repositorio, la carta levanta el bloqueo FFWW de los cargos (M4-04) y el certificado de
 * transbordo crea un cargo que el carro de la Ola D puede agregar.
 */

/** BL de importación de Chile del cliente de prueba (customer y consignee). */
export const BL_DOCUMENTOS = 'HLCU0000001';
/** BL donde la organización es solo shipper: solo la copia no valorada (M6-05). */
export const BL_SOLO_SHIPPER = 'HLCUVAP260300720';
/** BL del FFWW sin carta de responsabilidad: bloqueado hasta emitirla (M4-04, M6-06). */
export const BL_CARTA = 'HLCUSAI260401199';
/** Bolivia, CLD bloqueado por deuda y demoras anticipadas sin solicitar (M6-07, M3-16). */
export const BL_CLD_BLOQUEADO = 'HLCUARI260100045';
/** Bolivia, sin deuda: el CLD se emite. */
export const BL_CLD_EMITIBLE = 'HLCUIQQ260200078';
/** El repositorio no responde (NF-11). */
export const BL_DOCUMENTOS_CAIDO = 'HLCUSAI260409999';

/** Números de los documentos de demostración. */
export const DOC = {
  TRANSBORDO: 'CTB-20261004-5A1B2C3D',
  CUPON: 'CGO-20261005-2B3C4D5E',
  COPIA: 'CBL-20261003-7E8F9A0B',
} as const;

/** Cargo del certificado de transbordo que crea la solicitud (M6-01). */
export const CARGO_TRANSBORDO = 'c5000000-0000-4000-8000-000000000001';
/** Pago confirmado que emite documentos (certificado de transbordo y cupón de retiro). */
export const PAGO_CON_DOCUMENTOS = 'p5000000-0000-4000-8000-000000000201';
export const BL_TRANSBORDO_PAGADO = 'HLCUSAI260401020';

const VERSION_TERMINOS = 'CARTA-RESP-2026-10';

const ZONA: Record<string, string> = { CL: 'America/Santiago', BO: 'America/La_Paz' };

function blId(bl: string): string {
  return `3f0c2a1e-0000-4000-8000-${bl.slice(-12).padStart(12, '0')}`;
}

function documento(bl: string, datos: Partial<ShipmentDocument> & Pick<ShipmentDocument, 'id' | 'documentType' | 'documentNumber'>): ShipmentDocument {
  return {
    status: 'Issued',
    billOfLadingId: blId(bl),
    blNumber: bl,
    bookingNumber: 'BKG26030010',
    country: 'CL',
    containerNumbers: [],
    issuedAt: '2026-10-04T15:00:00Z',
    origin: 'Request',
    issuedForOrganizationId: ORGANIZACION_PRUEBA.id,
    issuedForOrganizationName: ORGANIZACION_PRUEBA.name,
    fileName: `${datos.documentNumber}.pdf`,
    contentType: 'application/pdf',
    sizeBytes: 48213,
    contentHash: '9f2c4a6b8d0e1f3a5c7e9b1d3f5a7c9e1b3d5f7a9c1e3b5d7f9a1c3e5b7d9f1a',
    verificationCode: 'K7Q2-M9X4-P3L8-W6R1',
    signed: false,
    signatureId: null,
    signatureProvider: null,
    signatureLevel: null,
    signedAt: null,
    paymentId: null,
    recipientEmails: [],
    deliveredAt: null,
    termsVersion: null,
    termsAcceptedAt: null,
    validUntil: null,
    retainUntil: '2036-10-04T15:00:00Z',
    ...datos,
  };
}

function acciones(datos: Partial<DocumentActions> = {}): DocumentActions {
  return {
    canRequestValuedCopy: false,
    canRequestNonValuedCopy: false,
    canIssueResponsibilityLetter: false,
    canRequestNoDebtCertificate: false,
    canRequestTransshipmentCertificate: false,
    ...datos,
  };
}

interface Repositorio {
  country: 'CL' | 'BO';
  documents: ShipmentDocument[];
  related: RelatedDocument[];
  actions: DocumentActions;
  responsibilityLetter?: ResponsibilityLetterState | null;
}

function repositorios(): Record<string, Repositorio> {
  return {
    [BL_DOCUMENTOS]: {
      country: 'CL',
      documents: [
        // Gate Out pagado: cupón de retiro asociado al BL y a sus unidades (M6-03).
        documento(BL_DOCUMENTOS, {
          id: 'd6000000-0000-4000-8000-000000000002', documentType: 'GateOutCoupon', documentNumber: DOC.CUPON,
          issuedAt: '2026-10-05T14:10:00Z', origin: 'Payment', containerNumbers: ['HLXU1234567'], paymentId: 'p5000000-0000-4000-8000-000000000101',
        }),
        // Certificado de transbordo de demostración: se genera y firma en la primera descarga (M6-01).
        documento(BL_DOCUMENTOS, {
          id: 'd6000000-0000-4000-8000-000000000001', documentType: 'TransshipmentCertificate', documentNumber: DOC.TRANSBORDO,
          issuedAt: '2026-10-04T15:00:00Z', origin: 'Seed', signed: true, contentHash: null, sizeBytes: 0,
        }),
        // Copia no valorada enviada al correo registrado (M6-05).
        documento(BL_DOCUMENTOS, {
          id: 'd6000000-0000-4000-8000-000000000003', documentType: 'BlCopyNonValued', documentNumber: DOC.COPIA,
          issuedAt: '2026-10-03T12:30:00Z', recipientEmails: [USUARIO_PRUEBA.email], deliveredAt: '2026-10-03T12:30:05Z',
        }),
      ],
      related: [
        {
          kind: 'Receipt', id: 'h5000000-0000-4000-8000-000000000001', number: 'RCP-20260925-0A1B2C3D', issuedAt: '2026-09-25T13:12:00Z',
          description: 'PAY-CL-2026-000123', downloadPath: '/api/v1/payment-history/h5000000-0000-4000-8000-000000000001/receipt',
        },
        {
          kind: 'Invoice', id: 'i4000000-0000-4000-8000-000000003987', number: '100245', issuedAt: '2026-09-15T12:00:00Z',
          description: 'HL-CL-2026-003987', downloadPath: '/api/v1/invoices/i4000000-0000-4000-8000-000000003987/pdf',
        },
      ],
      actions: acciones({ canRequestValuedCopy: true, canRequestNonValuedCopy: true, canRequestTransshipmentCertificate: true }),
    },
    [BL_SOLO_SHIPPER]: {
      country: 'CL',
      documents: [],
      related: [],
      actions: acciones({ canRequestNonValuedCopy: true, canRequestTransshipmentCertificate: true }),
    },
    [BL_CARTA]: {
      country: 'CL',
      documents: [],
      related: [],
      actions: acciones({ canRequestValuedCopy: true, canRequestNonValuedCopy: true, canIssueResponsibilityLetter: true }),
      responsibilityLetter: { required: true, status: 'Missing', blocksProcess: true },
    },
    // Fase 2, Ola J: la carta de liberación y desconsolidado (M6-08) y el certificado de flete (M6-02) de Bolivia.
    [BL_CLD_BLOQUEADO]: { country: 'BO', documents: [], related: [], actions: acciones({ canRequestNoDebtCertificate: true, canRequestReleaseLetter: true }) },
    [BL_CLD_EMITIBLE]: { country: 'BO', documents: [], related: [], actions: acciones({ canRequestNoDebtCertificate: true, canRequestFreightCertificate: true }) },
  };
}

const ELEGIBILIDAD: Record<string, NoDebtEligibility> = {
  [BL_CLD_BLOQUEADO]: {
    blId: blId(BL_CLD_BLOQUEADO),
    blNumber: BL_CLD_BLOQUEADO,
    country: 'BO',
    applicable: true,
    eligible: false,
    canRequest: true,
    blockers: [
      { code: 'PENDING_CHARGES', references: ['THC', 'TRANSIT_FEE', 'MHD'], amounts: [{ currency: 'BOB', total: 2226.1 }, { currency: 'USD', total: 450 }] },
      { code: 'PENDING_DEMURRAGE', references: ['HLXU8899001'], amounts: [{ currency: 'USD', total: 380 }] },
      { code: 'PENDING_INVOICES', references: ['2026-000812'], amounts: [{ currency: 'BOB', total: 1392 }] },
      { code: 'ADVANCE_DEMURRAGE', references: ['NotRequested'], amounts: [] },
    ],
    evaluatedAt: '2026-10-05T15:00:00Z',
  },
  [BL_CLD_EMITIBLE]: {
    blId: blId(BL_CLD_EMITIBLE),
    blNumber: BL_CLD_EMITIBLE,
    country: 'BO',
    applicable: true,
    eligible: true,
    canRequest: true,
    blockers: [],
    evaluatedAt: '2026-10-05T15:00:00Z',
  },
};

const TERMINOS = {
  version: VERSION_TERMINOS,
  title: 'Carta de responsabilidad del freight forwarder',
  text: 'El freight forwarder declara que actúa por cuenta de su cliente y asume la responsabilidad por la información de la carga y por los cargos locales del BL.\nLa carta queda asociada al BL y vigente hasta su reemplazo.',
};

/** Pago confirmado cuyos ítems emiten documentos (M6-01, M6-03). */
function pagoConDocumentos(): PaymentStatusDetail {
  const item = (n: number, conceptCode: string, bl: string, total: number) => ({
    id: `p5000000-0000-4000-8000-00000000021${n}`, itemType: 'LocalCharge' as const, conceptCode, blNumber: bl, bookingNumber: null,
    amount: Math.round(total / 1.19), taxAmount: total - Math.round(total / 1.19), totalAmount: total, currency: 'CLP',
    originalAmount: total, originalCurrency: 'CLP', billingTaxId: '76123456-7', billingName: ORGANIZACION_PRUEBA.name,
    releasedAt: '2026-10-05T15:03:05Z',
  });
  return {
    payment: {
      id: PAGO_CON_DOCUMENTOS, paymentNumber: 'PAY-20261005-5D0C5D0C', status: 'Confirmed', origin: 'Cart', country: 'CL', currency: 'CLP',
      amount: 70000, taxAmount: 13300, totalAmount: 83300, method: 'KHIPU', paymentMethodCode: 'KHIPU', externalReference: 'PAY-20261005-5D0C5D0C',
      receiptNumber: 'RCP-20261005-5D0C5D0C', createdAt: '2026-10-05T15:00:00Z', confirmedAt: '2026-10-05T15:03:00Z',
      items: [item(1, 'TRANSSHIPMENT_CERT', BL_TRANSBORDO_PAGADO, 41650), item(2, 'GATE_OUT', BL_DOCUMENTOS, 41650)],
    },
    history: [
      { fromStatus: null, toStatus: 'Pending', changedAt: '2026-10-05T15:00:00Z', changedBy: USUARIO_PRUEBA.email, reason: null },
      { fromStatus: 'Pending', toStatus: 'Processing', changedAt: '2026-10-05T15:01:00Z', changedBy: 'SYSTEM', reason: null },
      { fromStatus: 'Processing', toStatus: 'Confirmed', changedAt: '2026-10-05T15:03:00Z', changedBy: 'Khipu', reason: null },
    ],
    canCancel: false,
    cancelDeniedReason: 'FINAL',
    canIssueSlip: false,
    releasePending: false,
  };
}

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

/** Estado del backend simulado de la Ola E para una página. */
export class SimulacionOlaE {
  private readonly repos = repositorios();
  private siguiente = 10;
  /** BL con carta de responsabilidad vigente: los cargos ya no se bloquean (M4-04). */
  private readonly conCarta = new Set<string>();

  constructor(private readonly olaD: SimulacionOlaD) {}

  private numero(prefijo: string): { id: string; numero: string } {
    const n = this.siguiente++;
    return { id: `d6000000-0000-4000-8000-${String(n).padStart(12, '0')}`, numero: `${prefijo}-20261006-${String(n).padStart(8, '0')}` };
  }

  private repo(bl: string): Repositorio {
    // Un BL sin datos propios tiene el repositorio vacío y sin solicitudes.
    this.repos[bl] ??= { country: 'CL', documents: [], related: [], actions: acciones() };
    return this.repos[bl];
  }

  private vista(bl: string): ShipmentDocuments {
    const r = this.repo(bl);
    return {
      blId: blId(bl),
      blNumber: bl,
      bookingNumber: 'BKG26030010',
      country: r.country,
      timeZone: ZONA[r.country],
      documents: r.documents,
      related: r.related,
      actions: r.actions,
      ...(r.responsibilityLetter ? { responsibilityLetter: r.responsibilityLetter } : {}),
    };
  }

  /** Fase 2, Ola J: publica en el repositorio del BL un documento emitido por otra ola (certificado de flete, carta). */
  publicar(bl: string, doc: ShipmentDocument): void {
    const repo = this.repo(bl);
    repo.documents = [doc, ...repo.documents.filter((d) => d.id !== doc.id)];
  }

  /** Ajusta una respuesta GET de otra ola: con la carta emitida, los cargos del BL ya no se bloquean. */
  ajustar(ruta: string, respuesta: unknown): unknown {
    const m = ruta.match(/^charges\/([^/]+)$/);
    if (!m || !this.conCarta.has(m[1])) return respuesta;
    const cargos = respuesta as ShipmentCharges;
    return {
      ...cargos,
      requirements: cargos.requirements.map((r) => (r.code === 'RESPONSIBILITY_LETTER' ? { ...r, status: 'Fulfilled', blocksProcess: false } : r)),
      canProceed: true,
    };
  }

  /** Responde la ruta si es de la Ola E; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodo: string): Promise<boolean> {
    const request = route.request();

    if (ruta === `payments/${PAGO_CON_DOCUMENTOS}/status`) {
      await json(route, 200, pagoConDocumentos());
      return true;
    }
    if (!ruta.startsWith('documents/')) return false;

    if (ruta === 'documents/responsibility-letter/terms') {
      await json(route, 200, TERMINOS);
      return true;
    }

    let m = ruta.match(/^documents\/([^/]+)$/);
    if (m && metodo === 'GET') {
      if (m[1] === BL_DOCUMENTOS_CAIDO) {
        await route.fulfill({ status: 503, contentType: 'text/plain', body: 'Service Unavailable' });
      } else {
        await json(route, 200, this.vista(m[1]));
      }
      return true;
    }

    // Descarga del PDF (M6-09): un documento de demostración se genera y firma en la primera descarga.
    m = ruta.match(/^documents\/([^/]+)\/([^/]+)\/download$/);
    if (m) {
      const repo = this.repo(m[1]);
      const doc = repo.documents.find((d) => d.id === m?.[2]);
      if (!doc) {
        await problema(route, 404, 'ShipmentDocument.NotFound', 'Not found.');
        return true;
      }
      if (!doc.contentHash) {
        repo.documents = repo.documents.map((d) => (d.id === doc.id
          ? { ...d, contentHash: '1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b', sizeBytes: 51200, signatureId: 'SIG-DUMMY-0001', signatureProvider: 'Dummy', signatureLevel: 'Advanced', signedAt: '2026-10-06T12:00:00Z' }
          : d));
      }
      await route.fulfill({ status: 200, contentType: 'application/pdf', body: Buffer.from('%PDF-1.4 documento de prueba') });
      return true;
    }

    // Reenvío al correo registrado de la organización (M6-05).
    m = ruta.match(/^documents\/([^/]+)\/([^/]+)\/send$/);
    if (m && metodo === 'POST') {
      const repo = this.repo(m[1]);
      const doc = repo.documents.find((d) => d.id === m?.[2]);
      if (!doc) {
        await problema(route, 404, 'ShipmentDocument.NotFound', 'Not found.');
        return true;
      }
      const enviado = { ...doc, recipientEmails: [USUARIO_PRUEBA.email], deliveredAt: '2026-10-06T12:05:00Z' };
      repo.documents = repo.documents.map((d) => (d.id === doc.id ? enviado : d));
      await json(route, 200, { document: enviado, sentTo: [USUARIO_PRUEBA.email] });
      return true;
    }

    // Copia del BL (M6-05): el shipper no puede pedir la valorada.
    m = ruta.match(/^documents\/([^/]+)\/bl-copy$/);
    if (m && metodo === 'POST') {
      const repo = this.repo(m[1]);
      const body = cuerpo(request) as { valued: boolean; sendEmail?: boolean };
      if (body.valued ? !repo.actions.canRequestValuedCopy : !repo.actions.canRequestNonValuedCopy) {
        await problema(route, 403, 'Error.Forbidden', 'The action is not allowed for the role.');
        return true;
      }
      const { id, numero } = this.numero('CBL');
      const enviar = body.sendEmail !== false;
      const doc = documento(m[1], {
        id, documentType: body.valued ? 'BlCopyValued' : 'BlCopyNonValued', documentNumber: numero, issuedAt: '2026-10-06T12:10:00Z',
        recipientEmails: enviar ? [USUARIO_PRUEBA.email] : [], deliveredAt: enviar ? '2026-10-06T12:10:05Z' : null,
      });
      repo.documents = [doc, ...repo.documents];
      await json(route, 200, { document: doc, sentTo: enviar ? [USUARIO_PRUEBA.email] : [] });
      return true;
    }

    // Carta de responsabilidad (M6-06): levanta el bloqueo de M4-04.
    m = ruta.match(/^documents\/([^/]+)\/responsibility-letter$/);
    if (m && metodo === 'POST') {
      const repo = this.repo(m[1]);
      const body = cuerpo(request) as { acceptTerms: boolean; termsVersion: string };
      if (!repo.actions.canIssueResponsibilityLetter) {
        await problema(route, 403, 'Error.Forbidden', 'Only a freight forwarder consignee can issue the letter.');
      } else if (!body.acceptTerms) {
        await problema(route, 400, 'ResponsibilityLetter.TermsNotAccepted', 'Terms not accepted.');
      } else if (body.termsVersion !== VERSION_TERMINOS) {
        await problema(route, 400, 'ResponsibilityLetter.TermsVersionMismatch', `The accepted terms are not the current version '${VERSION_TERMINOS}'.`);
      } else {
        const { id, numero } = this.numero('CRE');
        const doc = documento(m[1], {
          id, documentType: 'ResponsibilityLetter', documentNumber: numero, issuedAt: '2026-10-06T12:20:00Z',
          termsVersion: VERSION_TERMINOS, termsAcceptedAt: '2026-10-06T12:20:00Z',
        });
        repo.documents = [doc, ...repo.documents.map((d) => (d.documentType === 'ResponsibilityLetter' ? { ...d, status: 'Superseded' as const } : d))];
        repo.responsibilityLetter = { required: true, status: 'Fulfilled', blocksProcess: false };
        this.conCarta.add(m[1]);
        await json(route, 200, doc);
      }
      return true;
    }

    // Certificado de libre deuda de Bolivia (M6-07).
    m = ruta.match(/^documents\/([^/]+)\/no-debt-certificate$/);
    if (m) {
      const elegibilidad = ELEGIBILIDAD[m[1]];
      if (!elegibilidad) {
        await problema(route, 400, 'NoDebtCertificate.NotApplicable', 'Only Bolivia imports.');
      } else if (metodo === 'GET') {
        await json(route, 200, elegibilidad);
      } else if (!elegibilidad.eligible) {
        await problema(route, 400, 'NoDebtCertificate.DebtPending', 'The shipment has pending debt: PENDING_CHARGES (THC, TRANSIT_FEE, MHD)');
      } else {
        const { id, numero } = this.numero('CLD');
        const doc = documento(m[1], {
          id, documentType: 'NoDebtCertificate', documentNumber: numero, country: 'BO', issuedAt: '2026-10-06T12:30:00Z',
          signed: true, signatureId: 'SIG-DUMMY-0002', signatureProvider: 'Dummy', signatureLevel: 'Advanced', signedAt: '2026-10-06T12:30:00Z',
        });
        const repo = this.repo(m[1]);
        repo.documents = [doc, ...repo.documents];
        await json(route, 200, doc);
      }
      return true;
    }

    // Certificado de transbordo (M6-01): crea el cargo del servicio, que se paga con el carro.
    m = ruta.match(/^documents\/([^/]+)\/transshipment-certificate$/);
    if (m && metodo === 'POST') {
      const repo = this.repo(m[1]);
      if (!repo.actions.canRequestTransshipmentCertificate) {
        await problema(route, 400, 'ShipmentDocument.NotAvailable', 'Not available.');
        return true;
      }
      const solicitud: TransshipmentRequest = {
        chargeId: CARGO_TRANSBORDO, blId: blId(m[1]), blNumber: m[1], conceptCode: 'TRANSSHIPMENT_CERT',
        amount: 35000, taxAmount: 6650, totalAmount: 41650, currency: 'CLP', status: 'Pending',
      };
      this.olaD.agregarPagable({
        itemType: 'LocalCharge', sourceId: CARGO_TRANSBORDO, conceptCode: 'TRANSSHIPMENT_CERT', conceptName: 'Certificado de transbordo',
        blId: blId(m[1]), blNumber: m[1], amount: 35000, taxAmount: 6650, totalAmount: 41650,
      });
      await json(route, 200, solicitud);
      return true;
    }

    await problema(route, 404, 'ShipmentDocument.NotFound', 'Not found.');
    return true;
  }
}
