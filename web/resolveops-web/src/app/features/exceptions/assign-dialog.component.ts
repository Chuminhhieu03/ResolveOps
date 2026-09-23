import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ExceptionCaseSummary } from '../../core/models/exception.models';

@Component({
  selector: 'app-assign-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule
  ],
  template: `
    <div style="padding: 24px;">
      <div style="display: flex; align-items: center; gap: 10px; margin-bottom: 20px;">
        <mat-icon style="color: #0284c7;">person_pin</mat-icon>
        <h2 mat-dialog-title style="margin: 0; font-size: 1.25rem; font-weight: 600;">
          Assign Case: {{ data.caseItem.caseNumber }}
        </h2>
      </div>

      <mat-dialog-content style="padding: 0; display: flex; flex-direction: column; gap: 14px;">
        <mat-form-field appearance="outline">
          <mat-label>Assignee Team Code</mat-label>
          <input matInput [(ngModel)]="assignedTeamCode" placeholder="OPS-NORTH">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Assignee User ID (Optional GUID)</mat-label>
          <input matInput [(ngModel)]="assignedUserId" placeholder="Leave empty for unassigned team pool">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Assignment Note</mat-label>
          <textarea matInput [(ngModel)]="note" rows="2" placeholder="Instructions for assignee..."></textarea>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end" style="gap: 8px; padding-top: 16px;">
        <button mat-button (click)="dialogRef.close(false)">Cancel</button>
        <button mat-flat-button color="primary" (click)="saveAssignment()" [disabled]="isSubmitting" style="background: #0284c7;">
          Assign Case
        </button>
      </mat-dialog-actions>
    </div>
  `
})
export class AssignDialogComponent {
  assignedTeamCode: string = 'OPS-NORTH';
  assignedUserId: string = '';
  note: string = '';
  isSubmitting = false;

  constructor(
    public dialogRef: MatDialogRef<AssignDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { caseItem: ExceptionCaseSummary },
    private http: HttpClient,
    private snackBar: MatSnackBar
  ) {
    this.assignedTeamCode = data.caseItem.ownerTeamCode || 'OPS-NORTH';
  }

  saveAssignment(): void {
    this.isSubmitting = true;
    this.http.post(`/api/exception-cases/${this.data.caseItem.id}/assign`, {
      assignedTeamCode: this.assignedTeamCode,
      assignedUserId: this.assignedUserId ? this.assignedUserId : null,
      note: this.note,
      concurrencyStamp: this.data.caseItem.concurrencyStamp
    }).subscribe({
      next: () => {
        this.snackBar.open('Case assignment updated.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.dialogRef.close(true);
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
