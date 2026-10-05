import { Injectable, signal } from '@angular/core';

export type Politeness = 'polite' | 'assertive';

/** Pausa entre vaciar la región y escribir el mensaje: así un mensaje repetido se vuelve a anunciar. */
const ANNOUNCE_DELAY_MS = 100;

/**
 * Anuncios para lectores de pantalla (guía UI/a11y/i18n §3.3, WCAG 4.1.3), sin CDK.
 * El shell (app.html) tiene dos regiones `visually-hidden`: `aria-live="polite"` y
 * `aria-live="assertive"`, que muestran `polite()` y `assertive()`.
 * - polite: resultados que no interrumpen (pago confirmado, importación terminada).
 * - assertive: errores de envío.
 * Los mensajes se pasan ya traducidos (translate()).
 */
@Injectable({ providedIn: 'root' })
export class LiveAnnouncerService {
  readonly polite = signal('');
  readonly assertive = signal('');

  private readonly timers: Record<Politeness, ReturnType<typeof setTimeout> | undefined> = {
    polite: undefined,
    assertive: undefined,
  };

  announce(message: string, politeness: Politeness = 'polite'): void {
    const region = politeness === 'assertive' ? this.assertive : this.polite;
    clearTimeout(this.timers[politeness]);
    region.set('');
    this.timers[politeness] = setTimeout(() => region.set(message), ANNOUNCE_DELAY_MS);
  }

  clear(): void {
    clearTimeout(this.timers.polite);
    clearTimeout(this.timers.assertive);
    this.polite.set('');
    this.assertive.set('');
  }
}
