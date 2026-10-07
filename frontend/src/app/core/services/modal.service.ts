import { Injectable, signal } from '@angular/core';

/** Textos del modal como claves de traducción; `params` alimenta la interpolación de título y mensaje. */
export interface ModalOptions {
  title: string;
  message?: string;
  params?: Record<string, unknown>;
  /** Detalle en lista (ya traducido o literal de datos, p. ej. números de BL). */
  details?: readonly string[];
  confirmLabel?: string;
  cancelLabel?: string;
  /** `danger` para acciones que borran, revocan o desactivan. */
  tone?: 'primary' | 'danger';
}

export interface ModalRequest extends ModalOptions {
  kind: 'confirm' | 'alert';
  resolve: (accepted: boolean) => void;
}

/**
 * Modal único del portal (confirmaciones y avisos), dibujado por ModalHostComponent con <dialog> nativo:
 * foco atrapado, Esc para cancelar y retorno del foco al control que lo abrió (WCAG 2.1.2, 2.4.3, 3.3.4).
 */
@Injectable({ providedIn: 'root' })
export class ModalService {
  private readonly current = signal<ModalRequest | null>(null);
  readonly request = this.current.asReadonly();

  /** Pide confirmación; resuelve `true` solo si la persona acepta. */
  confirm(options: ModalOptions): Promise<boolean> {
    return this.open('confirm', options);
  }

  /** Aviso con un único botón; resuelve cuando se cierra. */
  alert(options: ModalOptions): Promise<void> {
    return this.open('alert', options).then(() => undefined);
  }

  /** Lo llama el host al cerrarse el diálogo. */
  settle(accepted: boolean): void {
    const req = this.current();
    if (!req) return;
    this.current.set(null);
    req.resolve(accepted);
  }

  private open(kind: ModalRequest['kind'], options: ModalOptions): Promise<boolean> {
    // Un modal a la vez: si llega otro, el anterior se da por cancelado.
    this.settle(false);
    return new Promise<boolean>((resolve) => this.current.set({ ...options, kind, resolve }));
  }
}
