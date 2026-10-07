/**
 * Carro de compra unificado, separado por país y moneda de pago (Fase 1, Ola D): ítems con su tipo de
 * servicio (M5-01), RUT de facturación elegido al agregar (M5-09), monedas habilitadas y conversión con el
 * tipo de cambio de Nexus (M5-04, M5-05, M5-08), cierre por sub-carro con clave de idempotencia (NF-01) y
 * estado único del pago (NF-02, NF-12). Las propiedades en null no llegan en el JSON (el backend las omite).
 */

import { CommercialConditions, CurrencyTotal } from './charges.model';

/** Tipo de ítem pagable: define qué se cobra y cómo se libera al confirmar el pago. */
export type PayableItemType = 'LocalCharge' | 'Freight' | 'Demurrage' | 'WarehouseChange' | 'Invoice';

/** Estado único de un pago (NF-02). */
export type PaymentStatus = 'Pending' | 'Processing' | 'PendingVerification' | 'Confirmed' | 'Failed' | 'Cancelled';

/** Estados en los que el pago ya no cambia sin intervención (el resultado queda definido). */
export const FINAL_PAYMENT_STATUSES: readonly PaymentStatus[] = ['Confirmed', 'Failed', 'Cancelled'];

/** Resultado que el usuario elige en el simulador de pago (modo de prueba). */
export type PaymentSimulatorOutcome = 'approved' | 'rejected' | 'pending' | 'cancelled';

/** `CreditLine`: imputación a la línea de crédito (Fase 2, Ola H, M5-10); no figura en el historial de pagos. */
export type PaymentOrigin = 'Cart' | 'Account' | 'Legacy' | 'CreditLine';

/** Motivo de un pago fallido: sin respuesta de la plataforma (sin cobro) o rechazado por ella. */
export type PaymentFailureReason = 'PROVIDER_UNAVAILABLE' | 'PROVIDER_REJECTED';

/** Qué hacer después del cierre: ir a la plataforma, emitir la boleta de depósito o nada. */
export type CheckoutNextAction = 'Redirect' | 'IssueSlip' | 'None';

/** Por qué el cliente no puede anular el pago (M5-02); null = puede anularlo. */
export type CancelDeniedReason = 'SLIP_ISSUED' | 'IN_PROGRESS' | 'FINAL';

export type PaymentMethodKind = 'Online' | 'Deposit';

/** Origen de un RUT de facturación habilitado (M5-09). */
export type BillingOptionSource = 'Own' | 'Grant' | 'Invoice';

/** Tipo de cambio aplicado a la conversión de un ítem (M5-05). */
export interface AppliedExchangeRate {
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  effectiveDate: string;
  source: string;
}

/** Medio de pago configurado (M5-03). */
export interface PaymentMethod {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  country: string;
  kind: PaymentMethodKind;
  providerKey?: string | null;
  currencies: string[];
  isEnabled: boolean;
  displayOrder: number;
  createdAt?: string | null;
  modifiedAt?: string | null;
}

/** Estado público del bloqueo de pagos de un país (M8-07). */
export interface PaymentBlockStatus {
  country: string;
  timeZone: string;
  blocked: boolean;
  windowId?: string | null;
  /** Mensaje configurado para el cliente. */
  message?: string | null;
  endDate?: string | null;
  endTime?: string | null;
  evaluatedAt: string;
}

export interface CartItem {
  id: string;
  itemType: PayableItemType;
  sourceId: string;
  blId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  country: string;
  conceptCode: string;
  conceptName?: string | null;
  description?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  /** Moneda del cargo. */
  currency: string;
  paymentCurrency: string;
  /** Monto en la moneda de pago. */
  paymentAmount: number;
  /** Solo si hubo conversión. */
  exchangeRate?: AppliedExchangeRate | null;
  allowedCurrencies: string[];
  billingTaxId: string;
  billingName: string;
  onBehalfOfOrganizationId?: string | null;
  /** El ítem está en un pago en curso: no se puede quitar ni convertir. */
  lockedByPaymentId?: string | null;
  addedAt: string;
}

/** Sub-carro: país + moneda de pago, con su subtotal, medios y bloqueo (M5-08). */
export interface CartGroup {
  country: string;
  paymentCurrency: string;
  /** Suma de los ítems no bloqueados por un pago. */
  subtotal: number;
  itemCount: number;
  lockedItemCount: number;
  items: CartItem[];
  paymentMethods: PaymentMethod[];
  block: PaymentBlockStatus;
}

/** GET /cart. */
export interface Cart {
  id?: string | null;
  organizationId: string;
  itemCount: number;
  updatedAt?: string | null;
  groups: CartGroup[];
}

/** RUT de facturación habilitado para un ítem (M5-09). */
export interface BillingOption {
  taxId: string;
  name: string;
  organizationId?: string | null;
  source: BillingOptionSource;
  accessGrantId?: string | null;
}

/** GET /cart/item-options: lo que se valida y se elige antes de agregar. */
export interface PayableItem {
  itemType: PayableItemType;
  sourceId: string;
  blId?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  country: string;
  conceptCode: string;
  conceptName?: string | null;
  description?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  allowedCurrencies: string[];
  defaultPaymentCurrency: string;
  billingOptions: BillingOption[];
}

/** Ítem que se quiere agregar: por id de la fuente o, para facturas, por su número. */
export interface PayableItemRef {
  itemType: PayableItemType;
  sourceId?: string | null;
  reference?: string | null;
}

export interface AddCartItemRequest extends PayableItemRef {
  billingTaxId: string;
  paymentCurrency?: string | null;
}

export interface CheckoutRequest {
  country: string;
  paymentCurrency: string;
  paymentMethodCode: string;
}

/** Detalle de un pago: un ítem pagado (M5-01). */
export interface PaymentItem {
  id: string;
  itemType: PayableItemType;
  sourceId?: string | null;
  conceptCode: string;
  description?: string | null;
  blNumber?: string | null;
  bookingNumber?: string | null;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  /** Moneda del pago. */
  currency: string;
  originalAmount?: number | null;
  originalCurrency?: string | null;
  exchangeRate?: number | null;
  billingTaxId?: string | null;
  billingName?: string | null;
  releasedAt?: string | null;
}

export interface PaymentSummary {
  id: string;
  paymentNumber: string;
  status: PaymentStatus;
  statusChangedAt?: string | null;
  failureReason?: PaymentFailureReason | null;
  origin: PaymentOrigin;
  country: string;
  currency: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  method?: string | null;
  paymentMethodCode?: string | null;
  externalReference?: string | null;
  providerReference?: string | null;
  receiptNumber?: string | null;
  slipNumber?: string | null;
  slipIssuedAt?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  items: PaymentItem[];
}

/**
 * Formulario firmado que el navegador envía a la pasarela (botones bancarios por POST): el portal lo envía solo y,
 * si el envío automático no ocurre, ofrece el botón "Continuar al banco".
 */
export interface PaymentRedirectForm {
  method: string;
  action: string;
  fields: Record<string, string>;
}

/** POST /cart/checkout y POST /account-payments/checkout. */
export interface CheckoutResult {
  payment: PaymentSummary;
  nextAction: CheckoutNextAction;
  redirectUrl?: string | null;
  /** Viene en vez de una URL cuando la pasarela exige un formulario firmado por POST. */
  redirectForm?: PaymentRedirectForm | null;
  replayed: boolean;
}

export interface PaymentStatusChange {
  fromStatus?: PaymentStatus | null;
  toStatus: PaymentStatus;
  changedAt: string;
  changedBy: string;
  reason?: string | null;
}

/** GET /payments/{id}/status (NF-02, M5-02). */
export interface PaymentStatusDetail {
  payment: PaymentSummary;
  history: PaymentStatusChange[];
  canCancel: boolean;
  cancelDeniedReason?: CancelDeniedReason | null;
  canIssueSlip: boolean;
  /** Confirmado, con la liberación aún pendiente. */
  releasePending: boolean;
}

/** GET /account-payments (M5-07). */
export interface AccountPayables {
  organization: { id: string; name: string; taxId: string };
  conditions: CommercialConditions;
  items: PayableItem[];
  totals: CurrencyTotal[];
  evaluatedAt: string;
  /** Fase 2, Ola H: ítems que se pueden imputar a la línea de crédito desde el estado de cuenta (M5-10). */
  creditImputable?: PayableItemRef[];
}

export interface AccountCheckoutRequest {
  items: { itemType: PayableItemType; sourceId: string; billingTaxId?: string | null }[];
  paymentCurrency: string;
  paymentMethodCode: string;
}
