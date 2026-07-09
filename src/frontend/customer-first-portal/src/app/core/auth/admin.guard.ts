import { Injectable, inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class AdminGuard {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  canActivate: CanActivateFn = async () => {
    const isAuthenticated = await this.authService.isAuthenticatedAsync();
    if (!isAuthenticated) {
      this.authService.login(window.location.href);
      return false;
    }

    const isAdmin = await this.authService.isAdminAsync(true);
    if (isAdmin) {
      return true;
    }

    return this.router.createUrlTree(['/submissions']);
  };
}

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticatedAsync().then(async (isAuthenticated) => {
    if (!isAuthenticated) {
      authService.login(window.location.href);
      return false;
    }

    const isAdmin = await authService.isAdminAsync(true);
    if (isAdmin) {
      return true;
    }

    return router.createUrlTree(['/submissions']);
  });
};

export const adminMatchGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticatedAsync().then(async (isAuthenticated) => {
    if (!isAuthenticated) {
      authService.login(window.location.href);
      return false;
    }

    const isAdmin = await authService.isAdminAsync(true);
    if (isAdmin) {
      return true;
    }

    return router.createUrlTree(['/submissions']);
  });
};
