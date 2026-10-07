import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { Observable, EMPTY, defer, switchMap, timer } from 'rxjs';

/** Pausa tras el arranque antes de precargar: la primera pantalla no compite por la red. */
const START_DELAY_MS = 2000;

/** Conexión lenta o con ahorro de datos activado: no se precarga nada. */
function shouldSkip(): boolean {
  const connection = (navigator as Navigator & { connection?: { saveData?: boolean; effectiveType?: string } }).connection;
  return !!connection?.saveData || /(^|-)2g$/.test(connection?.effectiveType ?? '');
}

/**
 * Precarga en segundo plano las pantallas marcadas con `data: { preload: true }` (las más usadas), cuando el
 * navegador está desocupado. El resto sigue cargándose al entrar, como hasta ahora.
 */
@Injectable({ providedIn: 'root' })
export class IdlePreloadingStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    if (!route.data?.['preload'] || shouldSkip()) return EMPTY;
    return timer(START_DELAY_MS).pipe(switchMap(() => defer(() => whenIdle()).pipe(switchMap(() => load()))));
  }
}

function whenIdle(): Promise<void> {
  return new Promise((resolve) => {
    if (typeof requestIdleCallback === 'function') requestIdleCallback(() => resolve(), { timeout: 5000 });
    else setTimeout(resolve, 200);
  });
}
