import { Injectable, computed, signal } from '@angular/core';
import { Observable, defer, finalize } from 'rxjs';

/** Tiempo mínimo en pantalla: evita un destello cuando la operación termina casi al instante. */
export const OVERLAY_MIN_VISIBLE_MS = 450;

/**
 * Pantalla de carga que cubre todo el sitio (cambio de idioma y otras operaciones globales).
 * Lleva la cuenta de las operaciones en curso: se muestra con la primera y se oculta cuando termina
 * la última, respetando un tiempo mínimo visible. El texto es una clave de traducción.
 */
@Injectable({ providedIn: 'root' })
export class LoadingOverlayService {
  private readonly pending = signal(0);
  private readonly messageKey = signal('shared.globalLoader.loading');
  private shownAt = 0;

  readonly visible = computed(() => this.pending() > 0);
  readonly message = this.messageKey.asReadonly();

  /** Inicia una operación global; devuelve la función que la termina (llamarla más de una vez no tiene efecto). */
  begin(messageKey = 'shared.globalLoader.loading'): () => void {
    if (this.pending() === 0) this.shownAt = Date.now();
    this.messageKey.set(messageKey);
    this.pending.update((n) => n + 1);

    let ended = false;
    return () => {
      if (ended) return;
      ended = true;
      const wait = Math.max(0, OVERLAY_MIN_VISIBLE_MS - (Date.now() - this.shownAt));
      setTimeout(() => this.pending.update((n) => Math.max(0, n - 1)), wait);
    };
  }

  /** Envuelve un observable: la pantalla se muestra al suscribirse y se oculta al completar o fallar. */
  track<T>(source: Observable<T>, messageKey?: string): Observable<T> {
    return defer(() => {
      const end = this.begin(messageKey);
      return source.pipe(finalize(end));
    });
  }
}
