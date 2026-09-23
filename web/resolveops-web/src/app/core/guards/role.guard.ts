import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  const expectedRoles = route.data?.['roles'] as string[] | undefined;

  if (!expectedRoles || expectedRoles.length === 0) {
    return true;
  }

  if (authService.hasAnyRole(expectedRoles)) {
    return true;
  }

  snackBar.open('You do not have the required permissions for this area.', 'Close', {
    duration: 3500,
    panelClass: ['snack-warning']
  });

  router.navigate(['/dashboard']);
  return false;
};
