import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ExceptionCaseSummary } from '../../core/models/exception.models';

@Component({
  selector: 'app-triage-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatIconModule
  ],
  template: `
    <div style="padding: 24px;">
      <div style="display: flex; align-items: center; gap: 10px; margin-bottom: 20px;">
        <mat-icon style="color: #4f46e5;">tune</mat-icon>
        <h2 mat-dialog-title style="margin: 0; font-size: 1.25rem; font-weight: 600;">
          Triage Exception: {{ data.caseItem.caseNumber }}
        </h2>
      </div>

      <mat-dialog-content style="padding: 0; display: flex; flex-direction: column; gap: 14px;">
        <mat-form-field appearance="outline">
          <mat-label>Adjust Severity</mat-label>
          <mat-select [(ngModel)]="severity">
            <mat-option value="Critical">Critical (Immediate escalation)</mat-option>
            <mat-option value="High">High</mat-option>
            <mat-option value="Medium">Medium</mat-option>
            <mat-option value="Low">Low</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Root Cause Code</mat-label>
          <mat-select [(ngModel)]="rootCauseCode">
            <mat-option value="CARRIER_DELAY">Carrier Transit Delay</mat-option>
            <mat-option value="WEATHER_DISRUPTION">Adverse Weather Disruption</mat-option>
            <mat-option value="DAMAGED_PACKAGE">Damaged in Handling</mat-option>
            <mat-option value="MISROUTED_HUB">Misrouted Sorting Facility</mat-option>
            <mat-option value="CUSTOMS_HOLD">Customs Clearance Hold</mat-option>
            <mat-option value="LABEL_UNREADABLE">Unreadable Barcode Label</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Assigned Team Code</mat-label>
          <input matInput [(ngModel)]="assignedTeam" placeholder="e.g. OPS-NORTH, CLAIMS-SPECIAL">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Triage Decision Note</mat-label>
          <textarea matInput [(ngModel)]="note" rows="3" placeholder="Provide investigation context..."></textarea>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end" style="gap: 8px; padding-top: 16px;">
        <button mat-button (click)="dialogRef.close(false)">Cancel</button>
        <button mat-flat-button color="primary" (click)="saveTriage()" [disabled]="isSubmitting" style="background: #4f46e5;">
          Confirm Triage
        </button>
      </mat-dialog-actions>
    </div>
  `
})
export class TriageDialogComponent {
  severity: string;
  rootCauseCode: string = 'CARRIER_DELAY';
  assignedTeam: string = 'OPS-NORTH';
  note: string = '';
  isSubmitting = false;

  constructor(
    public dialogRef: MatDialogRef<TriageDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { caseItem: ExceptionCaseSummary },
    private http: HttpClient,
    private snackBar: MatSnackBar
  ) {
    this.severity = data.caseItem.severity;
  }

  saveTriage(): void {
    this.isSubmitting = true;
    this.http.post(`/api/exception-cases/${this.data.caseItem.id}/triage`, {
      severity: this.severity,
      rootCauseCode: this.rootCauseCode,
      assignedTeam: this.assignedTeam,
      note: this.note,
      concurrencyStamp: this.data.caseItem.concurrencyStamp
    }).subscribe({
      next: () => {
        this.snackBar.open('Exception case triaged successfully.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.dialogRef.close(true);
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
