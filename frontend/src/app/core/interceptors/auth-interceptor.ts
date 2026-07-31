import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { TokenStorageService } from '../../services/token-storage-service';
import { AuthService } from '../../services/auth-service';
import { SKIP_AUTH } from '../../services/customer-ledger-view-service';

let isRefreshing = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthCall = req.url.includes('/api/v1/Auth/');
  const skipAuth = req.context.get(SKIP_AUTH);

  const accessToken = tokenStorage.getAccessToken();
  const authReq = accessToken && !isAuthCall && !skipAuth
    ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      
      if (error.status !== 401 || isAuthCall || skipAuth) {
        return throwError(() => error);
      }

      if (!isRefreshing) {
        isRefreshing = true;
        refreshedToken$.next(null);

        return authService.refresh().pipe(
          switchMap((res) => {
            isRefreshing = false;
            refreshedToken$.next(res.data.accessToken);
            const retried = req.clone({
              setHeaders: { Authorization: `Bearer ${res.data.accessToken}` },
            });
            return next(retried);
          }),
          catchError((refreshError) => {
            isRefreshing = false;
            tokenStorage.clear();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }

      return refreshedToken$.pipe(
        filter((token): token is string => token !== null),
        take(1),
        switchMap((token) => {
          const retried = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
          return next(retried);
        })
      );
    })
  );
};