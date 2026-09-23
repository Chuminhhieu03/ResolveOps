import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { TenantService } from '../services/tenant.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const tenantService = inject(TenantService);

  const token = authService.getAccessToken();
  const correlationId = crypto.randomUUID();
  const tenantId = tenantService.tenantId;

  let headers = req.headers
    .set('X-Correlation-Id', correlationId);

  if (tenantId) {
    headers = headers.set('X-Tenant-Id', tenantId);
  }

  if (token && !req.headers.has('Authorization')) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  const cloned = req.clone({ headers });
  return next(cloned);
};
