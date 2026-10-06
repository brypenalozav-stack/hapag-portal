import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { ChargesService } from './charges.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { CartBatchItem, CartBatchResult } from '../models/account-statement.model';
import {
  AddCartItemRequest,
  Cart,
  CheckoutRequest,
  CheckoutResult,
  PayableItem,
  PayableItemRef,
} from '../models/cart.model';

const BASE = API_ENDPOINTS.CART;

/** Clave de idempotencia nueva para un intento de pago del usuario (NF-01). */
export function newIdempotencyKey(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  // Fuera de un contexto seguro (http sin localhost) no hay randomUUID: UUID v4 con getRandomValues.
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = [...bytes].map((b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

/**
 * Carro de compra persistente del usuario y su organización (M5-01), agrupado por país y moneda de pago
 * (M5-08). Reemplaza a la intención guardada en el navegador de la Ola C: todo se valida y se guarda en
 * el servidor. Comparte con el navbar la cantidad de ítems y si la organización es cliente con crédito
 * (M5-07): esos clientes no usan el carro sino "Pagar desde mi cuenta".
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly charges = inject(ChargesService);

  private readonly state = signal<Cart | null>(null);

  /** Carro vigente (null mientras no se ha consultado). */
  readonly cart = this.state.asReadonly();
  readonly count = computed(() => this.state()?.itemCount ?? 0);

  /**
   * Condición de crédito leída de Nexus (M8-02): true = cliente con crédito, false = sin crédito,
   * null = desconocida (no consultada o Nexus no respondió; el servidor decide al agregar).
   */
  readonly creditCustomer = signal<boolean | null>(null);

  /** Usuario de una organización cliente aprobada: el pago es de su propia organización (no del interno). */
  readonly isClient = computed(() => {
    const org = this.auth.organization();
    return this.auth.isAuthenticated() && !this.auth.isInternal()
      && !!org && org.status === 'Approved' && org.organizationType !== 'Internal';
  });

  /** Puede usar el carro: cliente sin crédito (o con la condición aún desconocida). */
  readonly cartEnabled = computed(() => this.isClient() && this.creditCustomer() !== true);

  /** Cliente con crédito: paga desde su cuenta (M5-07). */
  readonly accountPaymentsEnabled = computed(() => this.isClient() && this.creditCustomer() === true);

  constructor() {
    // Al iniciar o cambiar la sesión (usuario, organización o país) se vuelven a leer el carro y el crédito.
    effect(() => {
      const client = this.isClient();
      this.auth.organization()?.id;
      this.auth.currentUser()?.country;
      untracked(() => {
        this.state.set(null);
        this.creditCustomer.set(null);
        if (client) {
          this.loadConditions();
          this.refresh();
        }
      });
    });
  }

  /** Vuelve a leer el carro; los errores dejan el contador como estaba (no rompen el navbar). */
  refresh(): void {
    this.get().subscribe({ error: () => undefined });
  }

  get(): Observable<Cart> {
    return this.api.get<Cart>(BASE).pipe(tap((cart) => this.state.set(cart)));
  }

  /** Valida el ítem antes de agregarlo y devuelve los RUT y monedas habilitados (M5-09, M5-04). */
  getItemOptions(ref: PayableItemRef): Observable<PayableItem> {
    return this.api.get<PayableItem>(`${BASE}/item-options`, {
      itemType: ref.itemType,
      ...(ref.sourceId ? { sourceId: ref.sourceId } : {}),
      ...(ref.reference ? { reference: ref.reference } : {}),
    });
  }

  addItem(request: AddCartItemRequest): Observable<Cart> {
    return this.api.post<Cart>(`${BASE}/items`, request).pipe(tap((cart) => this.state.set(cart)));
  }

  /**
   * Agrega varios ítems de una vez (estado de cuenta, M7-03): cada uno se valida como en `addItem`; sin RUT se usa el
   * propio (en facturas, el facturado). Los que fallan se informan sin bloquear al resto.
   */
  addItems(items: CartBatchItem[]): Observable<CartBatchResult> {
    return this.api.post<CartBatchResult>(`${BASE}/items/batch`, { items }).pipe(tap((result) => this.state.set(result.cart)));
  }

  /** Convierte el ítem a otra moneda habilitada con el tipo de cambio del día (M5-08, M5-05). */
  changeCurrency(itemId: string, paymentCurrency: string): Observable<Cart> {
    return this.api.put<Cart>(`${BASE}/items/${itemId}/currency`, { paymentCurrency }).pipe(tap((cart) => this.state.set(cart)));
  }

  removeItem(itemId: string): Observable<Cart> {
    return this.api.delete<Cart>(`${BASE}/items/${itemId}`).pipe(tap((cart) => this.state.set(cart)));
  }

  /** Vacía el carro o solo un sub-carro (los ítems en un pago en curso se conservan). */
  clear(country?: string, paymentCurrency?: string): Observable<Cart> {
    return this.api.delete<Cart>(BASE, { ...(country ? { country } : {}), ...(paymentCurrency ? { paymentCurrency } : {}) })
      .pipe(tap((cart) => this.state.set(cart)));
  }

  /** Cierre de un sub-carro; la misma clave en un reintento devuelve el mismo resultado (NF-01). */
  checkout(request: CheckoutRequest, idempotencyKey: string): Observable<CheckoutResult> {
    return this.api.post<CheckoutResult>(`${BASE}/checkout`, request, { 'Idempotency-Key': idempotencyKey });
  }

  /** ¿Ya está en el carro? (para no ofrecer agregarlo otra vez). */
  contains(itemType: string, sourceId: string | null | undefined): boolean {
    if (!sourceId) return false;
    return (this.state()?.groups ?? []).some((g) => g.items.some((i) => i.itemType === itemType && i.sourceId === sourceId));
  }

  private loadConditions(): void {
    this.charges.getCommercialConditions().subscribe({
      next: (c) => this.creditCustomer.set(c.available ? c.hasCredit : null),
      error: () => this.creditCustomer.set(null),
    });
  }
}
