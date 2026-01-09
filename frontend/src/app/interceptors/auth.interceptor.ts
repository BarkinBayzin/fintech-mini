import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenEndpoint = `${environment.keycloakBaseUrl}/realms/${environment.keycloakRealm}/protocol/openid-connect/token`;
  const isBackendRequest =
    req.url.startsWith('/api') ||
    req.url.startsWith('http://localhost:') ||
    req.url.startsWith('https://localhost:');

  if (!isBackendRequest || req.url.startsWith(tokenEndpoint)) {
    return next(req);
  }

  if (req.headers.has('Authorization')) {
    return next(req);
  }

  const authService = inject(AuthService);
  const token = authService.getToken();
  if (!token) {
    return next(req);
  }

  const authRequest = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authRequest);
};
