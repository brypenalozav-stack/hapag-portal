import { Request, Route } from '@playwright/test';
import type { PagedResult } from '../../src/app/core/models/admin-user.model';
import type { ChargeConcept } from '../../src/app/core/models/tariff.model';
import type {
  AvailableService,
  AvailableServices,
  ServiceBillingData,
  ServiceDefinition,
  ServiceDefinitionChange,
  ServiceDefinitionInput,
  ServiceDefinitionSnapshot,
  ServiceInputField,
  ServiceInputValues,
  ServiceQuote,
  ServiceRequestDetail,
  ServiceRequestEvent,
  ServiceRequestStatus,
  ServiceRequestSummary,
  ShipmentContainerOption,
} from '../../src/app/core/models/service-request.model';
import type { WarehouseChangeHistoryItem, WarehouseChangeTrace } from '../../src/app/core/models/warehouse-change.model';
import { ORGANIZACION_PRUEBA, USUARIO_ADMIN, USUARIO_PRUEBA } from './session';
import { RUT_PROPIO, SimulacionOlaD } from './ola-d-mocks';

/**
 * Backend simulado de la Ola G (Fase 2): servicios on demand configurables (M2-03, M2-04), solicitudes de sellos,
 * Late Arrival y Early, Drop Off, XOM, correcciones de BL, BL hijo, matriz fuera de plazo y Gate In por devolución
 * (M3-07 a M3-15), bandeja interna, mantenedor de definiciones con historial (NF-15) e historial del cambio de almacén
 * (M3-06). Guarda estado por página: las solicitudes avanzan con las acciones del cliente y del equipo interno, y el
 * cargo generado se registra en la simulación del carro (Ola D) para poder agregarlo.
 */

const AHORA = '2026-10-06T12:00:00Z';
const ZONA_CL = 'America/Santiago';
const ZONA_BO = 'America/La_Paz';

/** BL de importación de Chile arribado (BL01): Drop Off con aprobación ED y correcciones de BL. */
export const BL_DROP_OFF = 'HLCUVAL250100123';
/** BL de exportación de Chile ya zarpado (BL16): BL hijo fuera de plazo y matriz fuera de plazo. */
export const BL_HIJO = 'HLCUSAI260901610';
export const BOOKING_HIJO = 'HLCUBKG2609161';
/** Booking de exportación de Chile antes del zarpe (BL06): sellos, Late Arrival, Early y Gate In por devolución. */
export const BOOKING_SELLOS = 'HLCUBKG2603061';
export const BL_SELLOS = 'HLCUSAI260300610';
/** BL de importación de Bolivia (BL04): XOM por contenedor en bolivianos, sin cobrar el contenedor SOC. */
export const BL_XOM = 'HLCUARI260100045';
/** BL de exportación de Bolivia sin contenedores aún (BL17): XOM no disponible con su motivo. */
export const BL_BO_SIN_UNIDADES = 'HLCUARI260901720';
/** BL de prueba con un servicio que usa todos los tipos de campo del formulario. */
export const BL_FORMULARIO = 'HLCUSAI260900001';
/** Código del servicio de prueba con todos los tipos de campo. */
export const SERVICIO_FORMULARIO = 'CARGO_INSPECTION';

/** Solicitudes sembradas de la organización de prueba. */
export const SOLICITUD = {
  DROP_OFF_PENDIENTE: 's9000000-0000-4000-8000-000000000001',
  SELLOS_POR_PAGAR: 's9000000-0000-4000-8000-000000000002',
  CORRECCION_EN_CURSO: 's9000000-0000-4000-8000-000000000003',
  BL_HIJO_COMPLETADA: 's9000000-0000-4000-8000-000000000004',
  DROP_OFF_RECHAZADA: 's9000000-0000-4000-8000-000000000005',
  LATE_ARRIVAL_ANULADA: 's9000000-0000-4000-8000-000000000006',
  MATRIZ_BORRADOR: 's9000000-0000-4000-8000-000000000008',
} as const;

/** Cargo local del servicio de sellos pendiente de pago (se agrega al carro). */
export const CARGO_SELLOS = 'c9000000-0000-4000-8000-000000000002';

/** Cambios de almacén del historial (M3-06). */
export const CAMBIO_PAGADO = 'w9000000-0000-4000-8000-000000000001';
export const CAMBIO_GRATIS = 'w9000000-0000-4000-8000-000000000002';
export const BL_CAMBIO_PAGADO = 'HLCUSAI260400910';

function definicionId(code: string): string {
  const n = DEFINICIONES_BASE.findIndex((d) => d.code === code) + 1;
  return `d9000000-0000-4000-8000-${String(n).padStart(12, '0')}`;
}

// ---------------------------------------------------------------------------
// Definiciones de servicio (las sembradas en el backend y una de prueba con todos los tipos de campo)
// ---------------------------------------------------------------------------

const CONTENEDORES: ServiceInputField = { key: 'containers', labelEs: 'Contenedores', labelEn: 'Containers', type: 'containers', required: true };
const OBSERVACIONES: ServiceInputField = { key: 'observations', labelEs: 'Observaciones', labelEn: 'Remarks', type: 'textarea', maxLength: 1000 };

function definicion(datos: Partial<ServiceDefinitionInput> & Pick<ServiceDefinitionInput, 'code' | 'nameEs' | 'nameEn'>): ServiceDefinitionInput {
  return {
    descriptionEs: null,
    descriptionEn: null,
    operations: ['EXPORT'],
    countries: ['CL'],
    referenceType: 'BL',
    requiredBlStatuses: null,
    availabilityWindow: 'Always',
    requiresContainers: false,
    allowMultiplePerBl: true,
    inputSchema: [],
    billingDataRequired: true,
    tariffAcceptanceRequired: false,
    pricingMode: 'Tariff',
    chargeConceptCode: null,
    tariffCode: null,
    lateTariffCode: null,
    quantityMode: 'PerRequest',
    measureFieldKey: null,
    milestone: 'None',
    milestoneOffsetHours: 0,
    deadlineRuleCode: null,
    timingRule: 'None',
    taxable: true,
    exemptionConcept: null,
    excludeShipperOwnedContainers: false,
    approvalTeam: 'None',
    fulfillmentTeam: 'None',
    requiresOutputDocument: false,
    actionCode: 'local-charges-on-demand.pay',
    displayOrder: 100,
    isActive: true,
    ...datos,
  };
}

const DEFINICIONES_BASE: ServiceDefinitionInput[] = [
  definicion({
    code: 'SEAL_MANAGEMENT', nameEs: 'Gestión de sellos', nameEn: 'Seal management', displayOrder: 10,
    descriptionEs: 'Ingreso de los datos de sellos del booking; se presta tras el pago.',
    descriptionEn: 'Seal data entry for the booking; provided after payment.',
    referenceType: 'Booking', availabilityWindow: 'BeforeDeparture', requiresContainers: true, chargeConceptCode: 'SEAL_MANAGEMENT',
    quantityMode: 'PerContainer', fulfillmentTeam: 'CustomerService',
    inputSchema: [CONTENEDORES, { key: 'sealNumbers', labelEs: 'Números de sello', labelEn: 'Seal numbers', type: 'text', required: true, maxLength: 500 }, OBSERVACIONES],
  }),
  definicion({
    code: 'LATE_ARRIVAL', nameEs: 'Late Arrival', nameEn: 'Late Arrival', displayOrder: 20,
    descriptionEs: 'Ingreso de unidades después del cierre de recepción; tarifa por tramos de horas de atraso.',
    descriptionEn: 'Container delivery after the receiving cut-off; tiered by hours late.',
    referenceType: 'Booking', availabilityWindow: 'BeforeDeparture', requiresContainers: true, chargeConceptCode: 'LATE_ARRIVAL',
    measureFieldKey: 'hours', taxable: false, fulfillmentTeam: 'CustomerService',
    inputSchema: [CONTENEDORES, { key: 'hours', labelEs: 'Horas de atraso respecto del cierre', labelEn: 'Hours after the cut-off', type: 'number', required: true, min: 1, max: 240, integer: true }, OBSERVACIONES],
  }),
  definicion({
    code: 'EARLY_ARRIVAL', nameEs: 'Early', nameEn: 'Early arrival', displayOrder: 30,
    referenceType: 'Booking', availabilityWindow: 'BeforeDeparture', requiresContainers: true, chargeConceptCode: 'EARLY_ARRIVAL',
    measureFieldKey: 'days', taxable: false, fulfillmentTeam: 'CustomerService',
    inputSchema: [CONTENEDORES, { key: 'days', labelEs: 'Días de anticipación', labelEn: 'Days early', type: 'number', required: true, min: 1, max: 30, integer: true }, OBSERVACIONES],
  }),
  definicion({
    code: 'DROP_OFF_SCL', nameEs: 'Drop Off SCL', nameEn: 'Drop Off SCL', displayOrder: 40,
    descriptionEs: 'Devolución de contenedores en Santiago: el cliente elige las unidades, acepta la tarifa y el equipo ED aprueba o rechaza.',
    descriptionEn: 'Container return in Santiago: the customer selects the units, accepts the tariff and the ED team approves or rejects.',
    operations: ['IMPORT'], availabilityWindow: 'AfterArrival', requiresContainers: true, tariffAcceptanceRequired: true,
    chargeConceptCode: 'DROP_OFF', quantityMode: 'PerContainer', approvalTeam: 'ED', actionCode: 'drop-off.request',
    inputSchema: [
      CONTENEDORES,
      { key: 'returnDate', labelEs: 'Fecha de devolución', labelEn: 'Return date', type: 'date', required: true },
      {
        key: 'depot', labelEs: 'Depósito de devolución', labelEn: 'Return depot', type: 'select', required: true,
        options: [
          { value: 'SCL_PUDAHUEL', labelEs: 'Depósito Pudahuel', labelEn: 'Pudahuel depot' },
          { value: 'SCL_QUILICURA', labelEs: 'Depósito Quilicura', labelEn: 'Quilicura depot' },
          { value: 'SCL_SAN_BERNARDO', labelEs: 'Depósito San Bernardo', labelEn: 'San Bernardo depot' },
        ],
      },
      OBSERVACIONES,
    ],
  }),
  definicion({
    code: 'XOM', nameEs: 'Administración de contenedor (XOM)', nameEn: 'Container administration (XOM)', displayOrder: 50,
    descriptionEs: 'Cargo por unidad en bolivianos para la operación de Bolivia; no se cobra con una excepción vigente en Nexus ni a las unidades del embarcador (SOC).',
    descriptionEn: 'Per-unit charge in bolivianos for the Bolivia operation; waived by a Nexus exception and for shipper-owned units.',
    operations: ['IMPORT', 'EXPORT'], countries: ['BO'], requiresContainers: true, chargeConceptCode: 'XOM', quantityMode: 'PerContainer',
    taxable: false, exemptionConcept: 'XOM', excludeShipperOwnedContainers: true, inputSchema: [CONTENEDORES],
  }),
  definicion({
    code: 'BL_CORRECTION', nameEs: 'Corrección o aclaración de BL', nameEn: 'BL correction or clarification', displayOrder: 60,
    descriptionEs: 'Solicitud y pago de correcciones o aclaraciones del BL; Customer Service la atiende tras el pago.',
    descriptionEn: 'Request and payment of BL corrections or clarifications; handled by Customer Service after payment.',
    operations: ['IMPORT', 'EXPORT'], countries: ['CL', 'BO'], chargeConceptCode: 'BL_CORRECTION', fulfillmentTeam: 'CustomerService',
    inputSchema: [
      {
        key: 'requestType', labelEs: 'Tipo de solicitud', labelEn: 'Request type', type: 'select', required: true,
        options: [{ value: 'CORRECTION', labelEs: 'Corrección', labelEn: 'Correction' }, { value: 'CLARIFICATION', labelEs: 'Aclaración', labelEn: 'Clarification' }],
      },
      { key: 'description', labelEs: 'Detalle de la corrección o aclaración', labelEn: 'Correction or clarification details', type: 'textarea', required: true, maxLength: 2000 },
      { key: 'supportingDocument', labelEs: 'Documento de respaldo', labelEn: 'Supporting document', type: 'file' },
    ],
  }),
  definicion({
    code: 'BL_HOUSE_TRANSMISSION', nameEs: 'Transmisión de BL hijo', nameEn: 'House BL transmission', displayOrder: 70,
    descriptionEs: 'Transmisión de BL hijo de exportación dentro o fuera de plazo; fuera de plazo se cobra por tramos de horas.',
    descriptionEn: 'Export house BL transmission in time or late; late transmissions are tiered by hours.',
    countries: ['CL', 'BO'], availabilityWindow: 'AfterDeparture', chargeConceptCode: 'BL_HOUSE_TRANSMISSION', tariffCode: 'PLAZO',
    lateTariffCode: 'FUERA_PLAZO', milestone: 'CustomsDeadline', deadlineRuleCode: 'BL_EMPTY_OUT', milestoneOffsetHours: 72,
    timingRule: 'InTimeAndLate', taxable: false, fulfillmentTeam: 'CustomerService',
    inputSchema: [{ key: 'houseBlNumbers', labelEs: 'Números de BL hijo', labelEn: 'House BL numbers', type: 'text', required: true, maxLength: 500 }, OBSERVACIONES],
  }),
  definicion({
    code: 'MATRIX_LATE', nameEs: 'Matriz fuera de plazo', nameEn: 'Late matrix submission', displayOrder: 80,
    countries: ['CL', 'BO'], allowMultiplePerBl: false, chargeConceptCode: 'MATRIX_LATE', milestone: 'VesselDeparture',
    milestoneOffsetHours: -48, timingRule: 'LateOnly', taxable: false, inputSchema: [OBSERVACIONES],
  }),
  definicion({
    code: 'GATE_IN_RETURN', nameEs: 'Gate In por devolución de unidades', nameEn: 'Gate In for returned export units', displayOrder: 90,
    referenceType: 'Booking', requiresContainers: true, allowMultiplePerBl: false, billingDataRequired: false, pricingMode: 'SourceCharge',
    chargeConceptCode: 'GATE_IN', actionCode: 'local-charges-mandatory.pay',
    inputSchema: [{ ...CONTENEDORES, required: false }],
  }),
  definicion({
    code: 'OPENING', nameEs: 'Apertura', nameEn: 'Opening', operations: ['IMPORT'], displayOrder: 100, allowMultiplePerBl: false,
    pricingMode: 'SourceCharge', chargeConceptCode: 'OPENING', actionCode: 'local-charges-mandatory.pay', isActive: false,
  }),
  definicion({
    code: 'VALUATION', nameEs: 'Valorización', nameEn: 'Valuation', operations: ['IMPORT'], displayOrder: 110, allowMultiplePerBl: false,
    pricingMode: 'SourceCharge', chargeConceptCode: 'VALUATION', actionCode: 'local-charges-mandatory.pay', isActive: false,
  }),
  // Solo en la simulación: un servicio con todos los tipos de campo, para revisar el formulario dinámico.
  definicion({
    code: SERVICIO_FORMULARIO, nameEs: 'Inspección de carga', nameEn: 'Cargo inspection', operations: ['IMPORT'], displayOrder: 120,
    descriptionEs: 'Inspección física de la carga en el terminal.', descriptionEn: 'Physical cargo inspection at the terminal.',
    requiresContainers: true, chargeConceptCode: 'BL_CORRECTION', quantityMode: 'PerContainer', fulfillmentTeam: 'CustomerService',
    inputSchema: [
      CONTENEDORES,
      { key: 'contactName', labelEs: 'Nombre de contacto', labelEn: 'Contact name', type: 'text', required: true, maxLength: 100, helpEs: 'Persona que acompaña la inspección.', helpEn: 'Person attending the inspection.' },
      { key: 'inspectionDate', labelEs: 'Fecha de inspección', labelEn: 'Inspection date', type: 'date', required: true },
      { key: 'packages', labelEs: 'Bultos a revisar', labelEn: 'Packages to check', type: 'number', required: true, min: 1, max: 500, integer: true },
      {
        key: 'inspectionType', labelEs: 'Tipo de inspección', labelEn: 'Inspection type', type: 'select', required: true,
        options: [{ value: 'PARTIAL', labelEs: 'Parcial', labelEn: 'Partial' }, { value: 'FULL', labelEs: 'Completa', labelEn: 'Full' }],
      },
      { key: 'authorization', labelEs: 'Autorización firmada', labelEn: 'Signed authorization', type: 'file', required: true },
      { key: 'instructions', labelEs: 'Instrucciones', labelEn: 'Instructions', type: 'textarea', required: true, maxLength: 1000 },
    ],
  }),
];

function aDefinicion(input: ServiceDefinitionInput, id: string): ServiceDefinition {
  return { ...input, code: input.code.toUpperCase(), id, createdAt: '2026-10-01T12:00:00Z', createdBy: 'SYSTEM', modifiedAt: null, modifiedBy: null };
}

function instantanea(d: ServiceDefinitionInput): ServiceDefinitionSnapshot {
  return {
    ...d,
    operations: d.operations.join(','),
    countries: d.countries.join(','),
    requiredBlStatuses: d.requiredBlStatuses?.join(',') ?? null,
    inputSchemaJson: JSON.stringify(d.inputSchema),
  };
}

/** Conceptos de cobro nuevos de la Ola G (mantenedor de tarifas M8-01). */
const CONCEPTOS_OLA_G: ChargeConcept[] = [
  { code: 'SEAL_MANAGEMENT', name: 'Gestión de sellos', category: 'Service', countries: ['CL'], nexusTariff: false, nexusExemptible: false, displayOrder: 20 },
  { code: 'EARLY_ARRIVAL', name: 'Early', category: 'Service', countries: ['CL'], nexusTariff: false, nexusExemptible: false, displayOrder: 21 },
  { code: 'DROP_OFF', name: 'Drop Off', category: 'Service', countries: ['CL'], nexusTariff: false, nexusExemptible: false, displayOrder: 22 },
  { code: 'XOM', name: 'XOM', category: 'Service', countries: ['BO'], nexusTariff: false, nexusExemptible: true, displayOrder: 23 },
  { code: 'BL_CORRECTION', name: 'Corrección de BL', category: 'Service', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 24 },
  { code: 'BL_HOUSE_TRANSMISSION', name: 'Transmisión de BL hijo', category: 'Service', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 25 },
  { code: 'MATRIX_LATE', name: 'Matriz fuera de plazo', category: 'Service', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: false, displayOrder: 26 },
  { code: 'GATE_IN', name: 'Gate In', category: 'LocalCharge', countries: ['CL', 'BO'], nexusTariff: false, nexusExemptible: true, displayOrder: 1 },
];

// ---------------------------------------------------------------------------
// Embarques y disponibilidad (el servidor decide; aquí se fija por BL)
// ---------------------------------------------------------------------------

interface Oferta {
  code: string;
  available: boolean;
  reasons?: string[];
  canRequest?: boolean;
  quoteErrorCode?: string;
}

interface Embarque {
  blNumber: string;
  bookingNumber: string | null;
  country: 'CL' | 'BO';
  operation: 'IMPORT' | 'EXPORT';
  status: string;
  etd: string;
  eta: string;
  containers: ShipmentContainerOption[];
  ofertas: Oferta[];
}

function unidad(containerNumber: string, containerType: string, isShipperOwned = false): ShipmentContainerOption {
  return { containerNumber, containerType, status: 'Discharged', isShipperOwned };
}

const EMBARQUES: Embarque[] = [
  {
    blNumber: BL_DROP_OFF, bookingNumber: 'BKG25010012', country: 'CL', operation: 'IMPORT', status: 'Arrived',
    etd: '2026-02-20T10:00:00Z', eta: '2026-04-04T21:00:00Z',
    containers: [unidad('HLXU3034002', '20DV'), unidad('HLXU3034003', '40HC')],
    ofertas: [{ code: 'DROP_OFF_SCL', available: true }, { code: 'BL_CORRECTION', available: true }],
  },
  {
    blNumber: BL_HIJO, bookingNumber: BOOKING_HIJO, country: 'CL', operation: 'EXPORT', status: 'Departed',
    etd: '2026-09-28T15:00:00Z', eta: '2026-10-20T10:00:00Z',
    containers: [unidad('HLXU2609161', '40RF')],
    ofertas: [
      { code: 'SEAL_MANAGEMENT', available: false, reasons: ['ALREADY_DEPARTED'] },
      { code: 'LATE_ARRIVAL', available: false, reasons: ['ALREADY_DEPARTED'] },
      { code: 'EARLY_ARRIVAL', available: false, reasons: ['ALREADY_DEPARTED'] },
      { code: 'BL_CORRECTION', available: true },
      { code: 'BL_HOUSE_TRANSMISSION', available: true },
      { code: 'MATRIX_LATE', available: true },
      { code: 'GATE_IN_RETURN', available: false, reasons: ['NO_SOURCE_CHARGE'] },
    ],
  },
  {
    blNumber: BL_SELLOS, bookingNumber: BOOKING_SELLOS, country: 'CL', operation: 'EXPORT', status: 'GateIn',
    etd: '2026-10-20T10:00:00Z', eta: '2026-11-10T10:00:00Z',
    containers: [unidad('HLXU2603061', '40HC'), unidad('HLXU2603062', '20DV')],
    ofertas: [
      { code: 'SEAL_MANAGEMENT', available: true },
      { code: 'LATE_ARRIVAL', available: true, quoteErrorCode: 'ServiceRequest.MeasureRequired' },
      { code: 'EARLY_ARRIVAL', available: true, quoteErrorCode: 'ServiceRequest.MeasureRequired' },
      { code: 'BL_CORRECTION', available: true },
      { code: 'BL_HOUSE_TRANSMISSION', available: false, reasons: ['NOT_DEPARTED'] },
      { code: 'MATRIX_LATE', available: false, reasons: ['NOT_OVERDUE'] },
      { code: 'GATE_IN_RETURN', available: true },
    ],
  },
  {
    blNumber: BL_XOM, bookingNumber: 'BKG26010045', country: 'BO', operation: 'IMPORT', status: 'Arrived',
    etd: '2026-01-10T10:00:00Z', eta: '2026-02-20T10:00:00Z',
    containers: [unidad('HLXU8899001', '40HC'), unidad('HLXU5566779', '20DV', true)],
    ofertas: [{ code: 'XOM', available: true }, { code: 'BL_CORRECTION', available: true }],
  },
  {
    blNumber: BL_BO_SIN_UNIDADES, bookingNumber: 'HLCUBKG2609172', country: 'BO', operation: 'EXPORT', status: 'Departed',
    etd: '2026-09-30T12:00:00Z', eta: '2026-10-25T10:00:00Z',
    containers: [],
    ofertas: [
      { code: 'XOM', available: false, reasons: ['NO_CONTAINERS'] },
      { code: 'BL_CORRECTION', available: true },
      { code: 'BL_HOUSE_TRANSMISSION', available: true },
      { code: 'MATRIX_LATE', available: true },
    ],
  },
  {
    blNumber: BL_FORMULARIO, bookingNumber: 'BKG26090001', country: 'CL', operation: 'IMPORT', status: 'Arrived',
    etd: '2026-08-20T10:00:00Z', eta: '2026-09-30T10:00:00Z',
    containers: [unidad('HLXU9000011', '20DV'), unidad('HLXU9000012', '40HC'), unidad('HLXU9000013', '40RF', true)],
    ofertas: [{ code: SERVICIO_FORMULARIO, available: true }],
  },
];

function zona(country: string): string {
  return country === 'BO' ? ZONA_BO : ZONA_CL;
}

function buscarEmbarque(bl: string | null, booking: string | null): Embarque | undefined {
  const b = (bl ?? '').toUpperCase();
  const k = (booking ?? '').toUpperCase();
  return EMBARQUES.find((e) => (b && e.blNumber === b) || (k && e.bookingNumber === k) || (b && e.bookingNumber === b));
}

// ---------------------------------------------------------------------------
// Cotización (tarifa vigente ahora, NF-22)
// ---------------------------------------------------------------------------

function redondear(monto: number, moneda: string): number {
  return moneda === 'CLP' ? Math.round(monto) : Math.round(monto * 100) / 100;
}

function cotizacionBase(datos: Partial<ServiceQuote>): ServiceQuote {
  return {
    pricingMode: 'Tariff', requiresPayment: true, amount: 0, taxAmount: 0, totalAmount: 0, currency: 'CLP', taxRate: 0, quantity: 1,
    timing: 'NotApplicable', isExempt: false, excludedContainers: [], lines: [], sourceCharges: [], timeZone: ZONA_CL, quotedAt: AHORA,
    tariffSource: 'PORTAL', ...datos,
  };
}

function porContenedor(e: Embarque, containers: string[], monto: number, moneda: string, tasa: number, concepto: string, tarifa: string): ServiceQuote {
  const soc = e.containers.filter((c) => c.isShipperOwned).map((c) => c.containerNumber);
  const cobrados = containers.filter((c) => !soc.includes(c));
  const lineas = cobrados.map((c) => ({
    containerNumber: c, containerType: e.containers.find((x) => x.containerNumber === c)?.containerType ?? null, amount: monto, tariffCode: null, breakdown: [],
  }));
  const neto = monto * cobrados.length;
  const impuesto = redondear(neto * tasa / 100, moneda);
  return cotizacionBase({
    chargeConceptCode: concepto, amount: neto, taxAmount: impuesto, totalAmount: neto + impuesto, currency: moneda, taxRate: tasa,
    quantity: cobrados.length, tariffCode: tarifa, tariffId: 't9000000-0000-4000-8000-000000000001', lines: lineas,
    excludedContainers: containers.filter((c) => soc.includes(c)), requiresPayment: neto > 0, timeZone: zona(e.country),
  });
}

/** Resultado de cotizar: la cotización o un error del servidor. */
type Cotizado = { quote: ServiceQuote } | { status: number; title: string; detail: string; errors?: Record<string, string[]> };

function cotizar(e: Embarque, code: string, valores: ServiceInputValues): Cotizado {
  const todos = e.containers.map((c) => c.containerNumber);
  const elegidos = Array.isArray(valores['containers']) ? (valores['containers'] as string[]) : todos;
  switch (code) {
    case 'DROP_OFF_SCL':
      if (elegidos.length === 0) return { status: 400, title: 'ServiceRequest.ContainersRequired', detail: 'At least one container is required.' };
      return { quote: porContenedor(e, elegidos, 180000, 'CLP', 19, 'DROP_OFF', 'DROP_OFF') };
    case 'SEAL_MANAGEMENT':
      if (elegidos.length === 0) return { status: 400, title: 'ServiceRequest.ContainersRequired', detail: 'At least one container is required.' };
      return { quote: porContenedor(e, elegidos, 12000, 'CLP', 19, 'SEAL_MANAGEMENT', 'SELLOS') };
    case SERVICIO_FORMULARIO:
      return { quote: porContenedor(e, elegidos, 25000, 'CLP', 19, 'BL_CORRECTION', 'INSPECCION') };
    case 'XOM':
      return { quote: porContenedor(e, elegidos, 350, 'BOB', 0, 'XOM', 'XOM') };
    case 'BL_CORRECTION':
      return e.country === 'BO'
        ? { quote: cotizacionBase({ chargeConceptCode: 'BL_CORRECTION', amount: 245, totalAmount: 245, currency: 'BOB', tariffCode: 'CORRECCION', timeZone: ZONA_BO, lines: [{ amount: 245, breakdown: [] }] }) }
        : { quote: cotizacionBase({ chargeConceptCode: 'BL_CORRECTION', amount: 35000, taxAmount: 6650, totalAmount: 41650, taxRate: 19, tariffCode: 'CORRECCION', lines: [{ amount: 35000, breakdown: [] }] }) };
    case 'BL_HOUSE_TRANSMISSION':
      return {
        quote: cotizacionBase({
          chargeConceptCode: 'BL_HOUSE_TRANSMISSION', amount: 250, totalAmount: 250, currency: 'USD', tierUnit: 'Hours', measuredUnits: 117,
          timing: 'Late', milestoneAt: '2026-10-01T15:00:00Z', milestoneSource: 'ETD', tariffCode: 'FUERA_PLAZO', timeZone: zona(e.country),
          lines: [{ amount: 250, tariffCode: 'FUERA_PLAZO', breakdown: [{ fromUnit: 73, toUnit: 117, units: 1, unitAmount: 250, amount: 250 }] }],
        }),
      };
    case 'MATRIX_LATE':
      return {
        quote: cotizacionBase({
          chargeConceptCode: 'MATRIX_LATE', amount: 200, totalAmount: 200, currency: 'USD', tierUnit: 'CalendarDays', measuredUnits: 10,
          timing: 'Late', milestoneAt: '2026-09-26T15:00:00Z', milestoneSource: 'ETD', tariffCode: 'MATRIZ', timeZone: zona(e.country),
          lines: [{ amount: 200, breakdown: [{ fromUnit: 6, toUnit: 10, units: 1, unitAmount: 200, amount: 200 }] }],
        }),
      };
    case 'LATE_ARRIVAL':
    case 'EARLY_ARRIVAL': {
      const medida = Number(valores[code === 'LATE_ARRIVAL' ? 'hours' : 'days'] ?? NaN);
      if (!Number.isFinite(medida)) return { status: 400, title: 'ServiceRequest.MeasureRequired', detail: 'The measure is required.' };
      const monto = medida > 24 ? 300 : 150;
      return {
        quote: cotizacionBase({
          chargeConceptCode: code, amount: monto * elegidos.length, totalAmount: monto * elegidos.length, currency: 'USD', quantity: elegidos.length,
          tierUnit: code === 'LATE_ARRIVAL' ? 'Hours' : 'CalendarDays', measuredUnits: medida,
          lines: elegidos.map((c) => ({ containerNumber: c, amount: monto, breakdown: [{ fromUnit: medida > 24 ? 25 : 1, toUnit: medida, units: 1, unitAmount: monto, amount: monto }] })),
        }),
      };
    }
    case 'GATE_IN_RETURN':
      return {
        quote: cotizacionBase({
          pricingMode: 'SourceCharge', chargeConceptCode: 'GATE_IN', amount: 95000, taxAmount: 18050, totalAmount: 113050, taxRate: 19,
          tariffSource: 'FIS', sourceCharges: [{
            chargeId: 'c9000000-0000-4000-8000-000000000090', conceptCode: 'GATE_IN', description: 'Gate In', amount: 95000, taxAmount: 18050,
            totalAmount: 113050, currency: 'CLP', outcome: 'Payable', payableTotal: 113050,
          }],
        }),
      };
    default:
      return { status: 400, title: 'Tariff.NotInForce', detail: 'No tariff in force.' };
  }
}

function disponible(e: Embarque, o: Oferta, def: ServiceDefinitionInput): AvailableService {
  const cotizado = o.available && !o.quoteErrorCode ? cotizar(e, o.code, {}) : null;
  return {
    definitionId: definicionId(o.code), code: o.code, nameEs: def.nameEs, nameEn: def.nameEn,
    descriptionEs: def.descriptionEs, descriptionEn: def.descriptionEn, referenceType: def.referenceType, available: o.available,
    canRequest: o.canRequest ?? o.available, unavailableReasons: o.reasons ?? [], inputSchema: def.inputSchema,
    billingDataRequired: def.billingDataRequired, tariffAcceptanceRequired: def.tariffAcceptanceRequired,
    approvalTeam: def.approvalTeam, fulfillmentTeam: def.fulfillmentTeam,
    ...(cotizado && 'quote' in cotizado ? { quote: cotizado.quote } : {}),
    ...(o.quoteErrorCode ? { quoteErrorCode: o.quoteErrorCode } : {}),
  };
}

// ---------------------------------------------------------------------------
// Solicitudes sembradas
// ---------------------------------------------------------------------------

function evento(n: number, from: ServiceRequestStatus | null, to: ServiceRequestStatus, occurredAt: string, actorName: string, actorKind: ServiceRequestEvent['actorKind'], notes?: string): ServiceRequestEvent {
  return { id: `e9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, fromStatus: from, toStatus: to, occurredAt, actorName, actorKind, ...(notes ? { notes } : {}) };
}

const FACTURACION_DEMO: ServiceBillingData = {
  taxId: '76.123.456-7', name: ORGANIZACION_PRUEBA.name, address: 'Av. Apoquindo 4500, Las Condes', email: 'facturacion@andes.cl', activity: 'Importación',
};

function solicitud(definitionCode: string, datos: Partial<ServiceRequestDetail> & Pick<ServiceRequestDetail, 'id' | 'requestNumber' | 'status'>): ServiceRequestDetail {
  const def = DEFINICIONES_BASE.find((d) => d.code === definitionCode) as ServiceDefinitionInput;
  const e = EMBARQUES.find((x) => x.blNumber === datos.blNumber) ?? EMBARQUES[0];
  return {
    definitionId: definicionId(definitionCode), definitionCode, nameEs: def.nameEs, nameEn: def.nameEn,
    organizationId: ORGANIZACION_PRUEBA.id, organizationName: ORGANIZACION_PRUEBA.name, requestedByEmail: USUARIO_PRUEBA.email,
    billOfLadingId: `3f0c2a1e-0000-4000-8000-${e.blNumber.slice(-12).padStart(12, '0')}`, blNumber: e.blNumber, bookingNumber: e.bookingNumber,
    country: e.country, operation: e.operation, timeZone: zona(e.country), containerNumbers: [], inputSchema: def.inputSchema,
    inputValues: {}, billing: FACTURACION_DEMO, billingDataRequired: def.billingDataRequired, tariffAcceptanceRequired: def.tariffAcceptanceRequired,
    statusChangedAt: '2026-10-05T14:00:00Z', approvalTeam: def.approvalTeam, fulfillmentTeam: def.fulfillmentTeam,
    requiresOutputDocument: def.requiresOutputDocument, createdAt: '2026-10-05T13:00:00Z', charges: [], canEdit: false, canSubmit: false,
    canCancel: false, attachments: [], timeline: [],
    ...datos,
  };
}

function sembradas(): ServiceRequestDetail[] {
  const dropOff = (cotizar(EMBARQUES[0], 'DROP_OFF_SCL', { containers: ['HLXU3034002'] }) as { quote: ServiceQuote }).quote;
  const sellos = (cotizar(EMBARQUES[2], 'SEAL_MANAGEMENT', { containers: ['HLXU2603061'] }) as { quote: ServiceQuote }).quote;
  const correccion = (cotizar(EMBARQUES[0], 'BL_CORRECTION', {}) as { quote: ServiceQuote }).quote;
  const blHijo = cotizacionBase({
    chargeConceptCode: 'BL_HOUSE_TRANSMISSION', amount: 120, totalAmount: 120, currency: 'USD', tierUnit: 'Hours', measuredUnits: 36,
    timing: 'Late', milestoneAt: '2026-10-01T15:00:00Z', milestoneSource: 'ETD', tariffCode: 'FUERA_PLAZO', quotedAt: '2026-10-02T15:00:00Z',
    lines: [{ amount: 120, tariffCode: 'FUERA_PLAZO', breakdown: [{ fromUnit: 25, toUnit: 36, units: 1, unitAmount: 120, amount: 120 }] }],
  });
  return [
    solicitud('DROP_OFF_SCL', {
      id: SOLICITUD.DROP_OFF_PENDIENTE, requestNumber: 'SRV-20261005-5E1A0001', status: 'PendingApproval', blNumber: BL_DROP_OFF,
      assignedTeam: 'ED', containerNumbers: ['HLXU3034002'], quote: dropOff, tariffAcceptedAt: '2026-10-05T13:05:00Z', canCancel: true,
      inputValues: { containers: ['HLXU3034002'], returnDate: '2026-10-12', depot: 'SCL_PUDAHUEL' }, submittedAt: '2026-10-05T13:05:00Z',
      timeline: [
        evento(1, null, 'Draft', '2026-10-05T13:00:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(2, 'Draft', 'Submitted', '2026-10-05T13:05:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(3, 'Submitted', 'PendingApproval', '2026-10-05T13:05:00Z', 'SYSTEM', 'System'),
      ],
    }),
    solicitud('SEAL_MANAGEMENT', {
      id: SOLICITUD.SELLOS_POR_PAGAR, requestNumber: 'SRV-20261005-5E1A0002', status: 'PendingPayment', blNumber: BL_SELLOS,
      containerNumbers: ['HLXU2603061'], quote: sellos, canCancel: true, submittedAt: '2026-10-05T12:00:00Z',
      inputValues: { containers: ['HLXU2603061'], sealNumbers: 'SL-998877' },
      charges: [{ chargeId: CARGO_SELLOS, itemType: 'LocalCharge', conceptCode: 'SEAL_MANAGEMENT', amount: 12000, taxAmount: 2280, totalAmount: 14280, currency: 'CLP', status: 'Pending', generated: true }],
      timeline: [
        evento(11, null, 'Draft', '2026-10-05T11:58:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(12, 'Draft', 'Submitted', '2026-10-05T12:00:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(13, 'Submitted', 'PendingPayment', '2026-10-05T12:00:00Z', 'SYSTEM', 'System'),
      ],
    }),
    solicitud('BL_CORRECTION', {
      id: SOLICITUD.CORRECCION_EN_CURSO, requestNumber: 'SRV-20261005-5E1A0003', status: 'InProgress', blNumber: BL_DROP_OFF,
      assignedTeam: 'CustomerService', quote: correccion, submittedAt: '2026-10-04T15:00:00Z', paidAt: '2026-10-04T16:00:00Z',
      paymentId: 'p9000000-0000-4000-8000-000000000003', paymentNumber: 'PAY-20261004-1A2B3C4D', receiptNumber: 'RCP-20261004-1A2B3C4D',
      inputValues: { requestType: 'CORRECTION', description: 'Corregir la dirección del consignatario.' },
      charges: [{ chargeId: 'c9000000-0000-4000-8000-000000000003', itemType: 'LocalCharge', conceptCode: 'BL_CORRECTION', amount: 35000, taxAmount: 6650, totalAmount: 41650, currency: 'CLP', status: 'Paid', generated: true }],
      timeline: [
        evento(21, 'Draft', 'Submitted', '2026-10-04T15:00:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(22, 'Submitted', 'PendingPayment', '2026-10-04T15:00:00Z', 'SYSTEM', 'System'),
        evento(23, 'PendingPayment', 'Paid', '2026-10-04T16:00:00Z', 'PAYMENT PAY-20261004-1A2B3C4D', 'System'),
        evento(24, 'Paid', 'InProgress', '2026-10-04T16:00:00Z', 'SYSTEM', 'System'),
      ],
    }),
    solicitud('BL_HOUSE_TRANSMISSION', {
      id: SOLICITUD.BL_HIJO_COMPLETADA, requestNumber: 'SRV-20261002-5E1A0004', status: 'Completed', blNumber: BL_HIJO,
      assignedTeam: 'CustomerService', quote: blHijo, submittedAt: '2026-10-02T15:00:00Z', paidAt: '2026-10-02T15:30:00Z',
      completedAt: '2026-10-03T10:00:00Z', statusChangedAt: '2026-10-03T10:00:00Z', createdAt: '2026-10-02T14:50:00Z',
      paymentId: 'p9000000-0000-4000-8000-000000000004', paymentNumber: 'PAY-20261002-6F5E4D3C', receiptNumber: 'RCP-20261002-6F5E4D3C',
      resolutionNotes: 'BL hijo transmitido a Aduana.',
      inputValues: { houseBlNumbers: 'HB-2609-01, HB-2609-02', observations: 'Transmisión urgente.' },
      charges: [{ chargeId: 'c9000000-0000-4000-8000-000000000004', itemType: 'LocalCharge', conceptCode: 'BL_HOUSE_TRANSMISSION', amount: 120, taxAmount: 0, totalAmount: 120, currency: 'USD', status: 'Paid', generated: true }],
      attachments: [{
        id: 'a9000000-0000-4000-8000-000000000004', fieldKey: '_output', fileName: 'transmision-bl-hijo.pdf', contentType: 'application/pdf',
        sizeBytes: 182_400, uploadedAt: '2026-10-03T09:55:00Z', uploadedBy: USUARIO_ADMIN.email,
      }],
      timeline: [
        evento(31, null, 'Draft', '2026-10-02T14:50:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(32, 'Draft', 'Submitted', '2026-10-02T15:00:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(33, 'Submitted', 'PendingPayment', '2026-10-02T15:00:00Z', 'SYSTEM', 'System'),
        evento(34, 'PendingPayment', 'Paid', '2026-10-02T15:30:00Z', 'PAYMENT PAY-20261002-6F5E4D3C', 'System'),
        evento(35, 'Paid', 'InProgress', '2026-10-02T15:30:00Z', 'SYSTEM', 'System'),
        evento(36, 'InProgress', 'Completed', '2026-10-03T10:00:00Z', USUARIO_ADMIN.email, 'Internal', 'BL hijo transmitido a Aduana.'),
      ],
    }),
    solicitud('DROP_OFF_SCL', {
      id: SOLICITUD.DROP_OFF_RECHAZADA, requestNumber: 'SRV-20261005-5E1A0005', status: 'Rejected', blNumber: BL_DROP_OFF,
      assignedTeam: 'ED', containerNumbers: ['HLXU3034003'], quote: dropOff, rejectedAt: '2026-10-05T16:00:00Z',
      resolutionNotes: 'El depósito Quilicura no recibe unidades de 40 pies esta semana.',
      inputValues: { containers: ['HLXU3034003'], returnDate: '2026-10-08', depot: 'SCL_QUILICURA' },
      timeline: [
        evento(41, 'Draft', 'Submitted', '2026-10-05T10:00:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(42, 'Submitted', 'PendingApproval', '2026-10-05T10:00:00Z', 'SYSTEM', 'System'),
        evento(43, 'PendingApproval', 'Rejected', '2026-10-05T16:00:00Z', USUARIO_ADMIN.email, 'Internal', 'El depósito Quilicura no recibe unidades de 40 pies esta semana.'),
      ],
    }),
    solicitud('LATE_ARRIVAL', {
      id: SOLICITUD.LATE_ARRIVAL_ANULADA, requestNumber: 'SRV-20261005-5E1A0006', status: 'Cancelled', blNumber: BL_SELLOS,
      cancelledAt: '2026-10-05T09:00:00Z', inputValues: { containers: ['HLXU2603062'], hours: 6 },
      timeline: [
        evento(51, null, 'Draft', '2026-10-05T08:50:00Z', USUARIO_PRUEBA.email, 'Client'),
        evento(52, 'Draft', 'Cancelled', '2026-10-05T09:00:00Z', USUARIO_PRUEBA.email, 'Client'),
      ],
    }),
    solicitud('MATRIX_LATE', {
      id: SOLICITUD.MATRIZ_BORRADOR, requestNumber: 'SRV-20261005-5E1A0008', status: 'Draft', blNumber: BL_HIJO,
      canEdit: true, canSubmit: true, canCancel: true, inputValues: { observations: 'Matriz enviada por correo el 29-09.' },
      timeline: [evento(81, null, 'Draft', '2026-10-05T17:00:00Z', USUARIO_PRUEBA.email, 'Client')],
    }),
  ];
}

function resumen(r: ServiceRequestDetail): ServiceRequestSummary {
  const total = r.charges.reduce((s, c) => s + c.totalAmount, 0) || (r.quote?.totalAmount ?? 0);
  return {
    id: r.id, requestNumber: r.requestNumber, definitionCode: r.definitionCode, nameEs: r.nameEs, nameEn: r.nameEn, blNumber: r.blNumber,
    bookingNumber: r.bookingNumber, country: r.country, operation: r.operation, status: r.status, assignedTeam: r.assignedTeam ?? null,
    totalAmount: total, currency: r.quote?.currency ?? null, isExempt: !!r.quote?.isExempt, organizationName: r.organizationName,
    requestedByEmail: r.requestedByEmail, createdAt: r.createdAt, statusChangedAt: r.statusChangedAt,
  };
}

// ---------------------------------------------------------------------------
// Historial del cambio de almacén (M3-06)
// ---------------------------------------------------------------------------

const CAMBIOS: WarehouseChangeTrace[] = [
  {
    change: {
      id: CAMBIO_PAGADO, createdAt: '2026-10-02T13:20:00Z', status: 'Completed', billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000910',
      blNumber: BL_CAMBIO_PAGADO, bookingNumber: 'BKG26040091', country: 'CL', timeZone: ZONA_CL, containerNumber: null,
      fromWarehouse: 'ALM-SAI-01', toWarehouse: 'ALM-SCL-02', amount: 9940, currency: 'CLP', isFree: false, tariffCode: 'KTE',
      requestedByEmail: USUARIO_PRUEBA.email, requestedBy: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
      payer: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
      billingTaxId: RUT_PROPIO, billingName: ORGANIZACION_PRUEBA.name, paymentId: 'p9000000-0000-4000-8000-000000000910',
      paymentNumber: 'PAY-20261002-7A8B9C0D', paymentStatus: 'Confirmed', receiptNumber: 'RCP-20261002-7A8B9C0D',
      paidAt: '2026-10-02T13:40:00Z', completedAt: '2026-10-02T13:41:00Z',
    },
    timeline: [
      { occurredAt: '2026-10-02T13:20:00Z', event: 'Requested', status: 'Pending', actor: USUARIO_PRUEBA.email },
      { occurredAt: '2026-10-02T13:35:00Z', event: 'PaymentStatusChanged', status: 'Processing', actor: USUARIO_PRUEBA.email, reference: 'PAY-20261002-7A8B9C0D' },
      { occurredAt: '2026-10-02T13:40:00Z', event: 'PaymentStatusChanged', status: 'Confirmed', actor: 'SYSTEM', reference: 'PAY-20261002-7A8B9C0D' },
      { occurredAt: '2026-10-02T13:41:00Z', event: 'Completed', status: 'Completed', actor: 'SYSTEM', reference: 'RCP-20261002-7A8B9C0D' },
    ],
  },
  {
    change: {
      id: CAMBIO_GRATIS, createdAt: '2026-10-01T15:00:00Z', status: 'Completed', billOfLadingId: '3f0c2a1e-0000-4000-8000-000000001020',
      blNumber: 'HLCUSAI260401020', bookingNumber: 'BKG26040102', country: 'CL', timeZone: ZONA_CL, containerNumber: 'HLXU4010201',
      fromWarehouse: 'ALM-SAI-01', toWarehouse: 'Bodega 1', amount: 0, currency: 'CLP', isFree: true, entitlementSource: 'PORTAL',
      requestedByEmail: USUARIO_PRUEBA.email, requestedBy: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
      batchId: 'b2000000-0000-4000-8000-000000000001', batchLineNumber: 1, completedAt: '2026-10-01T15:00:05Z',
    },
    timeline: [
      { occurredAt: '2026-10-01T15:00:00Z', event: 'Requested', status: 'Pending', actor: USUARIO_PRUEBA.email, reference: 'BATCH:b2000000-0000-4000-8000-000000000001' },
      { occurredAt: '2026-10-01T15:00:04Z', event: 'FreeEntitlementApplied', status: 'Pending', actor: 'SYSTEM', reference: 'PORTAL:CAMBIO_GRATIS_SAI' },
      { occurredAt: '2026-10-01T15:00:05Z', event: 'Completed', status: 'Completed', actor: 'SYSTEM' },
    ],
  },
  {
    change: {
      id: 'w9000000-0000-4000-8000-000000000003', createdAt: '2026-10-05T18:30:00Z', status: 'Pending', billOfLadingId: '3f0c2a1e-0000-4000-8000-000000000123',
      blNumber: BL_DROP_OFF, bookingNumber: 'BKG25010012', country: 'CL', timeZone: ZONA_CL, containerNumber: null,
      fromWarehouse: 'ALM-VAP-03', toWarehouse: 'ALM-SCL-02', amount: 9940, currency: 'CLP', isFree: false, tariffCode: 'KTE',
      requestedByEmail: USUARIO_PRUEBA.email, requestedBy: { organizationId: ORGANIZACION_PRUEBA.id, name: ORGANIZACION_PRUEBA.name, taxId: RUT_PROPIO },
      billingTaxId: RUT_PROPIO, billingName: ORGANIZACION_PRUEBA.name,
    },
    timeline: [{ occurredAt: '2026-10-05T18:30:00Z', event: 'Requested', status: 'Pending', actor: USUARIO_PRUEBA.email }],
  },
];

function historialAlmacen(url: URL): PagedResult<WarehouseChangeHistoryItem> {
  const bl = (url.searchParams.get('blNumber') ?? '').toUpperCase();
  const status = url.searchParams.get('status');
  const from = url.searchParams.get('from');
  const to = url.searchParams.get('to');
  const items = CAMBIOS.map((c) => c.change)
    .filter((c) => !bl || c.blNumber === bl || c.bookingNumber === bl)
    .filter((c) => !status || c.status === status)
    .filter((c) => !from || c.createdAt.slice(0, 10) >= from)
    .filter((c) => !to || c.createdAt.slice(0, 10) <= to)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  return { items, total: items.length, page: 1, pageSize: 20 };
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

async function problema(route: Route, status: number, title: string, detail: string, errors?: Record<string, string[]>): Promise<void> {
  await route.fulfill({ status, contentType: 'application/problem+json', body: JSON.stringify({ title, detail, status, ...(errors ? { errors } : {}) }) });
}

/** PDF mínimo para las descargas de adjuntos y documentos de salida. */
const PDF = '%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF';

function pagina<T>(items: T[], url: URL): PagedResult<T> {
  const page = Number(url.searchParams.get('page') ?? 1);
  const pageSize = Number(url.searchParams.get('pageSize') ?? 20);
  return { items: items.slice((page - 1) * pageSize, page * pageSize), total: items.length, page, pageSize };
}

/** Estado del backend simulado de la Ola G para una página. */
export class SimulacionOlaG {
  private readonly solicitudes: ServiceRequestDetail[] = sembradas();
  private readonly definiciones: ServiceDefinition[] = DEFINICIONES_BASE.map((d) => aDefinicion(d, definicionId(d.code)));
  private readonly historial = new Map<string, ServiceDefinitionChange[]>();
  private siguiente = 20;

  constructor(private readonly olaD: SimulacionOlaD) {
    // El cargo de sellos pendiente de pago se puede agregar al carro (M5-01).
    this.registrarCargos(this.solicitudes.find((s) => s.id === SOLICITUD.SELLOS_POR_PAGAR) as ServiceRequestDetail);
    for (const d of this.definiciones) {
      this.historial.set(d.id, [{
        id: `h9000000-0000-4000-8000-${d.id.slice(-12)}`, definitionId: d.id, action: 'Created', changedAt: '2026-10-01T12:00:00Z',
        changedBy: 'SYSTEM', previous: null, current: instantanea(d),
      }]);
    }
  }

  /** Agrega a la lista de conceptos del mantenedor de tarifas los de la Ola G. */
  ajustar(ruta: string, respuesta: unknown): unknown {
    if (ruta !== 'tariffs/concepts' || !Array.isArray(respuesta)) return respuesta;
    const codigos = new Set((respuesta as ChargeConcept[]).map((c) => c.code));
    return [...(respuesta as ChargeConcept[]), ...CONCEPTOS_OLA_G.filter((c) => !codigos.has(c.code))];
  }

  private numero(): { id: string; numero: string; n: number } {
    const n = this.siguiente++;
    return { id: `s9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, numero: `SRV-20261006-5E1A${String(n).padStart(4, '0')}`, n };
  }

  private registrarCargos(r: ServiceRequestDetail): void {
    for (const c of r.charges.filter((x) => x.status === 'Pending')) {
      this.olaD.agregarPagable({
        itemType: 'LocalCharge', sourceId: c.chargeId, conceptCode: c.conceptCode, amount: c.amount, taxAmount: c.taxAmount,
        totalAmount: c.totalAmount, currency: c.currency, blNumber: r.blNumber, bookingNumber: r.bookingNumber, country: r.country,
        description: r.requestNumber, allowedCurrencies: [c.currency], defaultPaymentCurrency: c.currency,
      });
    }
  }

  private evento(r: ServiceRequestDetail, to: ServiceRequestStatus, actorName: string, actorKind: ServiceRequestEvent['actorKind'], notes?: string): void {
    const n = this.siguiente++;
    r.timeline = [...r.timeline, evento(1000 + n, r.status, to, AHORA, actorName, actorKind, notes)];
    r.status = to;
    r.statusChangedAt = AHORA;
  }

  /** Genera el cargo local de la solicitud con la tarifa fijada al enviar. */
  private generarCargo(r: ServiceRequestDetail): void {
    const q = r.quote;
    if (!q || !q.requiresPayment || q.pricingMode === 'None') return;
    const n = this.siguiente++;
    r.charges = [{
      chargeId: `c9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, itemType: 'LocalCharge', conceptCode: q.chargeConceptCode ?? 'SERVICE',
      amount: q.amount, taxAmount: q.taxAmount, totalAmount: q.totalAmount, currency: q.currency ?? 'CLP', status: 'Pending', generated: true,
    }];
    this.registrarCargos(r);
  }

  /** Envío: aprobación, pendiente de pago o en curso sin cobro (contrato de la Ola G). */
  private enviar(r: ServiceRequestDetail, body: { acceptTariff?: boolean; acceptedTotal?: number | null }): { status: number; title: string; detail: string } | null {
    const e = buscarEmbarque(r.blNumber, null) as Embarque;
    const def = this.definiciones.find((d) => d.code === r.definitionCode) as ServiceDefinition;
    const faltan = def.inputSchema.filter((f) => f.required && (f.type === 'file'
      ? !r.attachments.some((a) => a.fieldKey === f.key)
      : !(Array.isArray(r.inputValues[f.key]) ? (r.inputValues[f.key] as string[]).length : String(r.inputValues[f.key] ?? '').trim())));
    if (faltan.length > 0) return { status: 400, title: 'Validation', detail: 'Missing fields.' };
    const b = r.billing;
    if (def.billingDataRequired && (!b?.taxId || !b.name || !b.address || !b.email)) {
      return { status: 400, title: 'ServiceRequest.BillingDataRequired', detail: 'Billing data is required.' };
    }
    const cotizado = cotizar(e, r.definitionCode, r.inputValues);
    if (!('quote' in cotizado)) return cotizado;
    const quote = cotizado.quote;
    if (def.tariffAcceptanceRequired && quote.requiresPayment) {
      if (!body.acceptTariff) return { status: 400, title: 'ServiceRequest.TariffNotAccepted', detail: 'The tariff must be accepted.' };
      if (body.acceptedTotal !== quote.totalAmount) return { status: 400, title: 'ServiceRequest.TariffChanged', detail: `New total ${quote.totalAmount}.` };
      r.tariffAcceptedAt = AHORA;
    }
    r.quote = quote;
    r.submittedAt = AHORA;
    r.canEdit = false;
    r.canSubmit = false;
    this.evento(r, 'Submitted', USUARIO_PRUEBA.email, 'Client');
    if (def.approvalTeam !== 'None') {
      r.assignedTeam = def.approvalTeam;
      r.canCancel = true;
      this.evento(r, 'PendingApproval', 'SYSTEM', 'System');
    } else if (quote.requiresPayment) {
      this.generarCargo(r);
      r.canCancel = true;
      this.evento(r, 'PendingPayment', 'SYSTEM', 'System');
    } else if (def.fulfillmentTeam !== 'None') {
      r.assignedTeam = def.fulfillmentTeam;
      r.canCancel = false;
      this.evento(r, 'InProgress', 'SYSTEM', 'System');
    } else {
      r.canCancel = false;
      this.evento(r, 'Completed', 'SYSTEM', 'System');
    }
    return null;
  }

  private nueva(body: { definitionCode: string; blNumber?: string | null; bookingNumber?: string | null; inputValues: ServiceInputValues; billing: ServiceBillingData | null }): ServiceRequestDetail | null {
    const e = buscarEmbarque(body.blNumber ?? null, body.bookingNumber ?? null);
    const def = this.definiciones.find((d) => d.code === body.definitionCode);
    if (!e || !def) return null;
    const { id, numero } = this.numero();
    const r = solicitud(DEFINICIONES_BASE.some((d) => d.code === def.code) ? def.code : 'BL_CORRECTION', {
      id, requestNumber: numero, status: 'Draft', blNumber: e.blNumber, createdAt: AHORA, statusChangedAt: AHORA,
      inputValues: body.inputValues ?? {}, billing: body.billing ?? {}, canEdit: true, canSubmit: true, canCancel: true,
      containerNumbers: Array.isArray(body.inputValues?.['containers']) ? (body.inputValues['containers'] as string[]) : [],
    });
    // El formulario y las reglas son los de la definición vigente (incluye los cambios del mantenedor).
    Object.assign(r, {
      definitionId: def.id, nameEs: def.nameEs, nameEn: def.nameEn, inputSchema: def.inputSchema,
      billingDataRequired: def.billingDataRequired, tariffAcceptanceRequired: def.tariffAcceptanceRequired,
      approvalTeam: def.approvalTeam, fulfillmentTeam: def.fulfillmentTeam, requiresOutputDocument: def.requiresOutputDocument,
    });
    r.timeline = [evento(2000 + this.siguiente++, null, 'Draft', AHORA, USUARIO_PRUEBA.email, 'Client')];
    this.solicitudes.unshift(r);
    return r;
  }

  /** Responde la ruta si es de la Ola G; devuelve false si no le corresponde. */
  async responder(route: Route, ruta: string, metodo: string, url: URL): Promise<boolean> {
    const request = route.request();

    // Servicios disponibles y cotización (M2-03, M2-04).
    if (ruta === 'service-requests/available' && metodo === 'GET') {
      const bl = url.searchParams.get('blNumber');
      const booking = url.searchParams.get('bookingNumber');
      const incluir = url.searchParams.get('includeUnavailable') === 'true';
      const e = buscarEmbarque(bl, booking);
      if (!e) {
        // Embarque sin servicios on demand en la simulación (los de otras olas).
        await json(route, 200, {
          blId: '3f0c2a1e-0000-4000-8000-000000000000', blNumber: (bl ?? booking ?? '').toUpperCase(), country: 'CL', operation: 'IMPORT',
          status: 'Arrived', timeZone: ZONA_CL, containers: [], services: [], evaluatedAt: AHORA,
        } satisfies AvailableServices);
        return true;
      }
      const services = e.ofertas
        .filter((o) => incluir || o.available)
        .map((o) => ({ o, def: this.definiciones.find((d) => d.code === o.code && d.isActive) }))
        .filter((x): x is { o: Oferta; def: ServiceDefinition } => !!x.def)
        .map(({ o, def }) => disponible(e, o, def));
      await json(route, 200, {
        blId: `3f0c2a1e-0000-4000-8000-${e.blNumber.slice(-12).padStart(12, '0')}`, blNumber: e.blNumber, bookingNumber: e.bookingNumber,
        country: e.country, operation: e.operation, status: e.status, etd: e.etd, eta: e.eta, timeZone: zona(e.country),
        containers: e.containers, services, evaluatedAt: AHORA,
      } satisfies AvailableServices);
      return true;
    }
    if (ruta === 'service-requests/quote' && metodo === 'POST') {
      const body = cuerpo(request) as { definitionCode: string; blNumber?: string; bookingNumber?: string; inputValues: ServiceInputValues };
      const e = buscarEmbarque(body.blNumber ?? null, body.bookingNumber ?? null);
      if (!e) {
        await problema(route, 404, 'BillOfLading.NotFound', 'Not found.');
        return true;
      }
      const r = cotizar(e, body.definitionCode, body.inputValues ?? {});
      if ('quote' in r) await json(route, 200, r.quote);
      else await problema(route, r.status, r.title, r.detail, r.errors);
      return true;
    }

    // Solicitudes del cliente.
    if (ruta === 'service-requests' && metodo === 'POST') {
      const body = cuerpo(request) as Parameters<SimulacionOlaG['nueva']>[0] & { submit: boolean; acceptTariff?: boolean; acceptedTotal?: number | null };
      const r = this.nueva(body);
      if (!r) {
        await problema(route, 404, 'ServiceDefinition.NotFound', 'Not found.');
        return true;
      }
      if (body.submit) {
        const error = this.enviar(r, body);
        if (error) {
          this.solicitudes.splice(this.solicitudes.indexOf(r), 1);
          await problema(route, error.status, error.title, error.detail);
          return true;
        }
      }
      await json(route, 201, r);
      return true;
    }
    if (ruta === 'service-requests' && metodo === 'GET') {
      const status = url.searchParams.get('status');
      const code = url.searchParams.get('definitionCode');
      const bl = (url.searchParams.get('blNumber') ?? '').toUpperCase();
      const items = this.solicitudes
        .filter((r) => !status || r.status === status)
        .filter((r) => !code || r.definitionCode === code)
        .filter((r) => !bl || r.blNumber === bl || r.bookingNumber === bl)
        .map(resumen);
      await json(route, 200, pagina(items, url));
      return true;
    }
    let m = ruta.match(/^(admin\/)?service-requests\/([^/]+)\/attachments\/([^/]+)$/);
    if (m && metodo === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/pdf', body: PDF });
      return true;
    }
    m = ruta.match(/^(admin\/)?service-requests\/([^/]+)\/attachments$/);
    if (m && metodo === 'POST') {
      const r = this.solicitudes.find((x) => x.id === m?.[2]);
      if (!r) {
        await problema(route, 404, 'ServiceRequest.NotFound', 'Not found.');
        return true;
      }
      const datos = request.postDataBuffer()?.toString('latin1') ?? '';
      const campo = m[1] ? '_output' : (/name="fieldKey"\r\n\r\n([^\r\n]+)/.exec(datos)?.[1] ?? 'file');
      const nombre = /filename="([^"]+)"/.exec(datos)?.[1] ?? 'archivo.pdf';
      const n = this.siguiente++;
      const adjunto = {
        id: `a9000000-0000-4000-8000-${String(n).padStart(12, '0')}`, fieldKey: campo, fileName: nombre, contentType: 'application/pdf',
        sizeBytes: 2048, uploadedAt: AHORA, uploadedBy: m[1] ? USUARIO_ADMIN.email : USUARIO_PRUEBA.email,
      };
      r.attachments = [...r.attachments, adjunto];
      await json(route, 200, adjunto);
      return true;
    }
    m = ruta.match(/^service-requests\/([^/]+)\/submit$/);
    if (m && metodo === 'POST') {
      const r = this.solicitudes.find((x) => x.id === m?.[1]);
      if (!r || r.status !== 'Draft') {
        await problema(route, 400, 'ServiceRequest.NotEditable', 'Not a draft.');
        return true;
      }
      const error = this.enviar(r, (cuerpo(request) as { acceptTariff?: boolean; acceptedTotal?: number | null }) ?? {});
      if (error) await problema(route, error.status, error.title, error.detail);
      else await json(route, 200, r);
      return true;
    }
    m = ruta.match(/^service-requests\/([^/]+)\/cancel$/);
    if (m && metodo === 'POST') {
      const r = this.solicitudes.find((x) => x.id === m?.[1]);
      if (!r || !r.canCancel) {
        await problema(route, 400, 'ServiceRequest.InvalidTransition', 'Cannot cancel.');
        return true;
      }
      const reason = (cuerpo(request) as { reason?: string } | null)?.reason;
      r.canCancel = false;
      r.canEdit = false;
      r.canSubmit = false;
      r.cancelledAt = AHORA;
      r.charges = [];
      this.evento(r, 'Cancelled', USUARIO_PRUEBA.email, 'Client', reason);
      await json(route, 200, r);
      return true;
    }
    m = ruta.match(/^service-requests\/([^/]+)$/);
    if (m && (metodo === 'GET' || metodo === 'PUT')) {
      const r = this.solicitudes.find((x) => x.id === m?.[1]);
      if (!r) {
        await problema(route, 404, 'ServiceRequest.NotFound', 'Not found.');
        return true;
      }
      if (metodo === 'PUT') {
        if (r.status !== 'Draft') {
          await problema(route, 400, 'ServiceRequest.NotEditable', 'Not a draft.');
          return true;
        }
        const body = cuerpo(request) as { inputValues: ServiceInputValues | null; billing: ServiceBillingData | null };
        if (body.inputValues) r.inputValues = body.inputValues;
        if (body.billing) r.billing = body.billing;
      }
      await json(route, 200, r);
      return true;
    }

    // Bandeja interna (service-requests.process).
    if (ruta === 'admin/service-requests' && metodo === 'GET') {
      const status = url.searchParams.get('status');
      const team = url.searchParams.get('team');
      const code = url.searchParams.get('definitionCode');
      const country = url.searchParams.get('country');
      const bl = (url.searchParams.get('blNumber') ?? '').toUpperCase();
      const accionables = !status && !bl;
      const items = this.solicitudes
        .filter((r) => r.status !== 'Draft')
        .filter((r) => (accionables ? ['PendingApproval', 'InProgress'].includes(r.status) : !status || r.status === status))
        .filter((r) => !team || r.assignedTeam === team)
        .filter((r) => !code || r.definitionCode === code)
        .filter((r) => !country || r.country === country)
        .filter((r) => !bl || r.blNumber === bl || r.bookingNumber === bl)
        .sort((a, b) => a.statusChangedAt.localeCompare(b.statusChangedAt))
        .map(resumen);
      await json(route, 200, pagina(items, url));
      return true;
    }
    m = ruta.match(/^admin\/service-requests\/([^/]+)(\/(approve|reject|complete|notes))?$/);
    if (m) {
      const r = this.solicitudes.find((x) => x.id === m?.[1]);
      if (!r) {
        await problema(route, 404, 'ServiceRequest.NotFound', 'Not found.');
        return true;
      }
      const accion = m[3];
      const body = (cuerpo(request) as { notes?: string; reason?: string } | null) ?? {};
      if (!accion && metodo === 'GET') {
        await json(route, 200, { ...r, canEdit: false, canSubmit: false, canCancel: false });
        return true;
      }
      if (accion === 'approve' && r.status === 'PendingApproval') {
        r.approvedAt = AHORA;
        this.evento(r, 'Approved', USUARIO_ADMIN.email, 'Internal', body.notes);
        if (r.quote?.requiresPayment) {
          this.generarCargo(r);
          r.assignedTeam = null;
          this.evento(r, 'PendingPayment', 'SYSTEM', 'System');
        } else {
          this.evento(r, r.fulfillmentTeam !== 'None' ? 'InProgress' : 'Completed', 'SYSTEM', 'System');
        }
        if (body.notes) r.resolutionNotes = body.notes;
      } else if (accion === 'reject' && r.status === 'PendingApproval') {
        r.rejectedAt = AHORA;
        r.resolutionNotes = body.reason ?? null;
        r.canCancel = false;
        this.evento(r, 'Rejected', USUARIO_ADMIN.email, 'Internal', body.reason);
      } else if (accion === 'complete' && r.status === 'InProgress') {
        if (r.requiresOutputDocument && !r.attachments.some((a) => a.fieldKey === '_output')) {
          await problema(route, 400, 'ServiceRequest.OutputDocumentRequired', 'Output document required.');
          return true;
        }
        r.completedAt = AHORA;
        if (body.notes) r.resolutionNotes = body.notes;
        this.evento(r, 'Completed', USUARIO_ADMIN.email, 'Internal', body.notes);
      } else if (accion === 'notes' && body.notes) {
        r.timeline = [...r.timeline, evento(3000 + this.siguiente++, r.status, r.status, AHORA, USUARIO_ADMIN.email, 'Internal', body.notes)];
      } else {
        await problema(route, 400, 'ServiceRequest.InvalidTransition', 'Invalid transition.');
        return true;
      }
      await json(route, 200, { ...r, canEdit: false, canSubmit: false, canCancel: false });
      return true;
    }

    // Mantenedor de definiciones (maintainers.manage, NF-15).
    if (ruta === 'service-definitions') {
      if (metodo === 'POST') {
        const body = cuerpo(request) as ServiceDefinitionInput;
        if (this.definiciones.some((d) => d.code === body.code.toUpperCase())) {
          await problema(route, 409, 'ServiceDefinition.AlreadyExists', 'Already exists.');
          return true;
        }
        const n = this.siguiente++;
        const nueva = aDefinicion(body, `d9000000-0000-4000-8000-${String(900 + n).padStart(12, '0')}`);
        this.definiciones.push(nueva);
        this.historial.set(nueva.id, [{ id: `h9000000-0000-4000-8000-${String(900 + n).padStart(12, '0')}`, definitionId: nueva.id, action: 'Created', changedAt: AHORA, changedBy: USUARIO_ADMIN.email, previous: null, current: instantanea(nueva) }]);
        await json(route, 201, nueva);
      } else {
        const incluir = url.searchParams.get('includeInactive') === 'true';
        const country = url.searchParams.get('country');
        const operation = url.searchParams.get('operation');
        await json(route, 200, this.definiciones
          .filter((d) => d.code !== SERVICIO_FORMULARIO)
          .filter((d) => incluir || d.isActive)
          .filter((d) => !country || d.countries.includes(country))
          .filter((d) => !operation || d.operations.includes(operation as 'IMPORT' | 'EXPORT')));
      }
      return true;
    }
    m = ruta.match(/^service-definitions\/([^/]+)\/history$/);
    if (m && metodo === 'GET') {
      await json(route, 200, [...(this.historial.get(m[1]) ?? [])].reverse());
      return true;
    }
    m = ruta.match(/^service-definitions\/([^/]+)$/);
    if (m) {
      const indice = this.definiciones.findIndex((d) => d.id === m?.[1]);
      if (indice < 0) {
        await problema(route, 404, 'ServiceDefinition.NotFound', 'Not found.');
        return true;
      }
      const anterior = this.definiciones[indice];
      if (metodo === 'GET') {
        await json(route, 200, anterior);
        return true;
      }
      const actual = metodo === 'DELETE'
        ? { ...anterior, isActive: false }
        : { ...anterior, ...(cuerpo(request) as ServiceDefinitionInput), id: anterior.id, modifiedAt: AHORA, modifiedBy: USUARIO_ADMIN.email };
      actual.code = actual.code.toUpperCase();
      this.definiciones[indice] = actual;
      const lista = this.historial.get(anterior.id) ?? [];
      lista.push({
        id: `h9000000-0000-4000-8000-${String(800 + this.siguiente++).padStart(12, '0')}`, definitionId: anterior.id,
        action: metodo === 'DELETE' ? 'Deactivated' : 'Updated', changedAt: AHORA, changedBy: USUARIO_ADMIN.email,
        previous: instantanea(anterior), current: instantanea(actual),
      });
      this.historial.set(anterior.id, lista);
      await json(route, metodo === 'DELETE' ? 204 : 200, metodo === 'DELETE' ? undefined : actual);
      return true;
    }

    // Historial del cambio de almacén (M3-06).
    if (ruta === 'warehouse-changes/history' && metodo === 'GET') {
      await json(route, 200, historialAlmacen(url));
      return true;
    }
    m = ruta.match(/^warehouse-changes\/history\/([^/]+)$/);
    if (m && metodo === 'GET') {
      const traza = CAMBIOS.find((c) => c.change.id === m?.[1]);
      if (traza) await json(route, 200, traza);
      else await problema(route, 404, 'WarehouseChange.NotFound', 'Not found.');
      return true;
    }

    return false;
  }
}

/** Id de la definición sembrada por código (para abrir el editor). */
export function idDefinicion(code: string): string {
  return definicionId(code);
}
