import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  await authService.waitForAdminStatus();

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/recipes']);
  authService.login();
  return false;
};
