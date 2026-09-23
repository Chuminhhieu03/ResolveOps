import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { ConcurrencyDialogComponent } from '../../shared/components/concurrency-dialog/concurrency-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class ConcurrencyService {
  private reloadSubject = new Subject<void>();
  reloadRequested$ = this.reloadSubject.asObservable();

  constructor(private dialog: MatDialog) {}

  handleConflict(message?: string): void {
    const dialogRef = this.dialog.open(ConcurrencyDialogComponent, {
      width: '480px',
      disableClose: true,
      data: {
        message: message || 'This record was modified by another operator or background process. Please reload to review the latest state before retrying.'
      }
    });

    dialogRef.afterClosed().subscribe(reload => {
      if (reload) {
        this.reloadSubject.next();
      }
    });
  }
}
