import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { map } from 'rxjs';
import { FeatureName, FeatureService } from '../services/feature.service';

/**
 * Permite la ruta si alguno de los flags indicados está encendido (cierre de Fase 1: Fase 2 apagada por defecto); si
 * no, lleva al dashboard. Espera la lectura de los flags. El servidor responde 404 en los endpoints apagados.
 */
export function featureGuard(...names: FeatureName[]): CanActivateFn {
  return () => {
    const features = inject(FeatureService);
    const router = inject(Router);
    return features.ready().pipe(map(() => (features.enabled(...names) ? true : router.createUrlTree(['/dashboard']))));
  };
}
