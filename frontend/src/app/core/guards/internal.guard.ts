import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Permite el acceso a los módulos de la consola operativa interna. */
export const internalGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated() && authService.isInternal()) {
    return true;
  }

  router.navigate(['/dashboard']);
  return false;
};
