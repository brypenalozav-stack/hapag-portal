import { Injector, afterNextRender } from '@angular/core';

/**
 * Mueve el foco al elemento indicado después del próximo render (guía UI §3.2 y §3.5: resumen de
 * errores, resultado de un envío). Con la detección de cambios agrupada por eventos, un
 * `setTimeout` puede correr antes de que el elemento exista; `afterNextRender` espera al render.
 */
export function focusAfterRender(injector: Injector, target: () => HTMLElement | null | undefined): void {
  afterNextRender({ read: () => target()?.focus() }, { injector });
}
