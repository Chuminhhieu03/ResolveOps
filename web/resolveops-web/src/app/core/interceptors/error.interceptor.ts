import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ConcurrencyService } from '../services/concurrency.service';
import { ProblemDetails } from '../models/api-error.models';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  const router = inject(Router);
  const concurrencyService = inject(ConcurrencyService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 1. Optimistic Concurrency Conflict (HTTP 409)
      if (error.status === 409) {
        const problem = error.error as ProblemDetails;
        const msg = problem?.detail || problem?.title || 'A concurrency conflict occurred. The record was updated by another process.';
        concurrencyService.handleConflict(msg);
        return throwError(() => error);
      }

      // 2. Authentication failure (HTTP 401)
      if (error.status === 401 && !req.url.includes('/login')) {
        snackBar.open('Session expired or unauthorized. Please log in again.', 'Dismiss', {
          duration: 4000,
          panelClass: ['snack-warning']
        });
        router.navigate(['/login']);
        return throwError(() => error);
      }

      // 3. Forbidden (HTTP 403)
      if (error.status === 403) {
        snackBar.open('Access Denied: You do not have permission for this action.', 'Dismiss', {
          duration: 4000,
          panelClass: ['snack-error']
        });
        return throwError(() => error);
      }

      // 4. RFC 7807 Problem Details Parsing
      const problem = error.error as ProblemDetails;
      let displayMessage = 'An unexpected error occurred.';

      if (problem) {
        if (problem.detail) {
          displayMessage = problem.detail;
        } else if (problem.title) {
          displayMessage = problem.title;
        } else if (problem.errors) {
          const firstKey = Object.keys(problem.errors)[0];
          if (firstKey && problem.errors[firstKey]?.length) {
            displayMessage = problem.errors[firstKey][0];
          }
        }
      } else if (error.message) {
        displayMessage = error.message;
      }

      // Don't show toast for silent checks
      if (!req.url.includes('/notifications') && !req.url.includes('/version')) {
        snackBar.open(displayMessage, 'Close', {
          duration: 5000,
          panelClass: ['snack-error']
        });
      }

      return throwError(() => error);
    })
  );
};
