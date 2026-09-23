import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-concurrency-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="concurrency-dialog-container" style="padding: 24px;">
      <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
        <mat-icon style="color: #f59e0b; font-size: 32px; width: 32px; height: 32px;">warning_amber</mat-icon>
        <h2 mat-dialog-title style="margin: 0; font-size: 1.25rem; font-weight: 600;">Data Concurrency Conflict</h2>
      </div>

      <mat-dialog-content style="padding: 0 0 16px 0; color: #475569; font-size: 0.95rem; line-height: 1.5;">
        <p>{{ data.message }}</p>
        <div style="background: #fffbeb; border: 1px solid #fde68a; border-radius: 6px; padding: 12px; margin-top: 12px;">
          <strong style="color: #92400e;">Why this happens:</strong>
          <p style="margin: 4px 0 0 0; font-size: 0.85rem; color: #78350f;">
            Another operator or background job has updated this entity since you opened it.
            To avoid overwriting their work, your changes were not applied.
          </p>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end" style="gap: 8px; padding: 0;">
        <button mat-button (click)="close(false)">Cancel</button>
        <button mat-flat-button color="primary" (click)="close(true)">
          <mat-icon style="margin-right: 4px;">refresh</mat-icon>
          Reload Latest Data
        </button>
      </mat-dialog-actions>
    </div>
  `
})
export class ConcurrencyDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConcurrencyDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { message: string }
  ) {}

  close(reload: boolean): void {
    this.dialogRef.close(reload);
  }
}
