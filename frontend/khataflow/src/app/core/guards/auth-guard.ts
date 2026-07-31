import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../../services/auth-service';
import { TokenStorageService } from '../../services/token-storage-service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  if (!tokenStorage.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  const allowedRoles = route.data?.['roles'] as string[] | undefined;
  const userRole = authService.getRole();
  if (allowedRoles && (!userRole || !allowedRoles.includes(userRole))) {
    router.navigate(['/login']);
    return false;
  }

  if (!tokenStorage.isAccessTokenExpired()) {
    return true;
  }

  return authService.refresh().pipe(
    map(() => true),
    catchError(() => {
      tokenStorage.clear();
      router.navigate(['/login']);
      return of(false);
    })
  );
};