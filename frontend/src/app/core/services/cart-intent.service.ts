import { Injectable, computed, signal } from '@angular/core';

/** Cargo que el usuario pidió agregar al carro. */
export interface CartIntentItem {
  chargeId: string;
  blNumber: string;
  conceptCode: string;
  amount: number;
  currency: string;
}

const STORAGE_KEY = 'hl_cart_intent';

/**
 * Intención de agregar cargos al carro de compra. El carro unificado es de la Ola D (M5-01): por
 * ahora solo se guardan los cargos elegidos en la sesión del navegador para que la Ola D los
 * recoja; aquí no se arma el carro ni se paga nada.
 */
@Injectable({ providedIn: 'root' })
export class CartIntentService {
  private readonly state = signal<CartIntentItem[]>(this.load());

  readonly items = this.state.asReadonly();
  readonly count = computed(() => this.state().length);

  has(chargeId: string): boolean {
    return this.state().some((i) => i.chargeId === chargeId);
  }

  /** Agrega los cargos que no estaban; devuelve cuántos se agregaron. */
  add(items: CartIntentItem[]): number {
    const fresh = items.filter((i) => !this.has(i.chargeId));
    if (fresh.length === 0) return 0;
    this.state.update((current) => [...current, ...fresh]);
    this.save();
    return fresh.length;
  }

  private load(): CartIntentItem[] {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      const parsed: unknown = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed) ? (parsed as CartIntentItem[]) : [];
    } catch {
      return [];
    }
  }

  private save(): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.state()));
    } catch {
      // Sin almacenamiento disponible: la intención vale solo mientras la página siga abierta.
    }
  }
}
