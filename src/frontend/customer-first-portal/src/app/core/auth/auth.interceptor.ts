import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

let authRecoveryInProgress = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const resolvedUrl = new URL(req.url, window.location.origin);
  const isApiRequest =
    resolvedUrl.pathname.startsWith('/api') ||
    req.url.startsWith(environment.apiUrl);

  if (!isApiRequest) {
    return next(req);
  }

  const authService = inject(AuthService);
  const router = inject(Router);

  return from(authService.getAccessToken()).pipe(
    switchMap((token) => {
      if (!token) {
        return next(req);
      }

      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });

      return next(authReq);
    }),
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && !authRecoveryInProgress) {
          authRecoveryInProgress = true;
          authService.login(window.location.href);

          // Avoid redirect storms when multiple API calls fail simultaneously.
          setTimeout(() => {
            authRecoveryInProgress = false;
          }, 2000);
        }

        if (error.status === 403) {
          void router.navigate(['/submissions']);
        }
      }

      return throwError(() => error);
    }),
  );
};
