import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Permite la ruta si el JWT trae al menos uno de los permisos indicados. Solo ordena la
 * navegación: el servidor vuelve a exigir el permiso en cada endpoint (NF-05).
 */
export function permissionGuard(...permissions: string[]): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.isAuthenticated() && authService.hasPermission(...permissions)) {
      return true;
    }

    router.navigate(['/dashboard']);
    return false;
  };
}
