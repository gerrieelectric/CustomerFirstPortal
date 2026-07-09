import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async (_, state) => {
  const authService = inject(AuthService);
  const isAuthenticated = await authService.isAuthenticatedAsync();

  if (isAuthenticated) {
    return true;
  }

  authService.login(`${window.location.origin}${state.url}`);
  return false;
};
