import { Injectable, inject, signal } from '@angular/core';
import { LiveAnnouncerService } from './live-announcer.service';

export type ToastKind = 'success' | 'info' | 'warning';

export interface Toast {
  id: number;
  kind: ToastKind;
  /** Texto ya traducido. */
  message: string;
}

/** Tiempo en pantalla; se pausa mientras el puntero o el foco están sobre el aviso (WCAG 2.2.1). */
export const TOAST_DURATION_MS = 5000;
const MAX_VISIBLE = 4;

/**
 * Avisos breves en pantalla tras una acción (guardado, eliminado, revocado...). El texto se entrega además a
 * LiveAnnouncerService, que es la única región viva: el aviso visual no se anuncia dos veces.
 * Los errores siguen mostrándose junto al formulario o la tabla que los produjo, no como toast.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly items = signal<readonly Toast[]>([]);
  private nextId = 1;

  readonly toasts = this.items.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  warning(message: string): void {
    this.show('warning', message);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private show(kind: ToastKind, message: string): void {
    if (!message) return;
    this.announcer.announce(message);
    const toast: Toast = { id: this.nextId++, kind, message };
    this.items.update((list) => [...list, toast].slice(-MAX_VISIBLE));
  }
}
