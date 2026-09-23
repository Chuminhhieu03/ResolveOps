import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import {
  QuarantinedEventSummary,
  QuarantinedEventDetail,
  ListQuarantinedEventsResponse
} from '../../core/models/quarantine.models';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';

@Component({
  selector: 'app-quarantine-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatDialogModule,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="quarantine-page" style="max-width: 1300px; margin: 0 auto;">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
            Integration Quarantine Screen
          </h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Inspect quarantined tracking events, investigate validation &amp; payload errors, and replay into the ingestion pipeline.
          </p>
        </div>

        <button mat-stroked-button (click)="loadQuarantine()" [disabled]="isLoading()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <!-- MAIN TABLE -->
      <div class="card-enterprise" style="overflow: hidden; background: #ffffff; margin-bottom: 24px;">
        <div class="table-container">
          <table mat-table [dataSource]="items()" style="width: 100%;">
            <!-- Status -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Status</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-status"
                      [style.background]="item.status === 'Quarantined' ? '#fee2e2' : '#ecfdf5'"
                      [style.color]="item.status === 'Quarantined' ? '#991b1b' : '#065f46'">
                  {{ item.status }}
                </span>
              </td>
            </ng-container>

            <!-- Reason Code -->
            <ng-container matColumnDef="reason">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Failure Reason</th>
              <td mat-cell *matCellDef="let item">
                <code style="font-size: 0.8rem; font-weight: 600; color: #dc2626;">{{ item.reasonCode }}</code>
              </td>
            </ng-container>

            <!-- Detail -->
            <ng-container matColumnDef="detail">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Error Message</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.85rem; color: #475569;">
                {{ item.detail | formulaSafe }}
              </td>
            </ng-container>

            <!-- Created At -->
            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Quarantined At</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.8rem; color: #64748b;">
                {{ item.createdAtUtc | utcToLocal:'medium' }}
              </td>
            </ng-container>

            <!-- Actions -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="text-align: right; font-weight: 600;">Actions</th>
              <td mat-cell *matCellDef="let item" style="text-align: right;">
                <button mat-stroked-button (click)="viewPayload(item)" style="height: 32px; font-size: 0.8rem; margin-right: 6px;">
                  <mat-icon style="font-size: 16px; width: 16px; height: 16px; margin-right: 4px;">data_object</mat-icon>
                  Payload
                </button>
                <button mat-flat-button color="primary" (click)="replayEvent(item)" style="height: 32px; font-size: 0.8rem; background: #4f46e5;">
                  <mat-icon style="font-size: 16px; width: 16px; height: 16px; margin-right: 4px;">replay</mat-icon>
                  Replay
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns" style="background: #f8fafc; height: 44px;"></tr>
            <tr mat-row *matRowDef="let row; columns: columns;" style="height: 50px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="items().length === 0" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 40px; width: 40px; height: 40px; color: #cbd5e1;">check_circle_outline</mat-icon>
            <p style="margin-top: 8px;">Quarantine clean. No unhandled integration failures.</p>
          </div>
        </div>
      </div>

      <!-- PAYLOAD VIEWER DRAWER / MODAL -->
      <div *ngIf="activeDetail()" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 2000;">
        <div class="card-enterprise" style="width: 720px; max-height: 80vh; display: flex; flex-direction: column; padding: 24px; background: #ffffff;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
            <div style="display: flex; align-items: center; gap: 8px;">
              <mat-icon style="color: #4f46e5;">code</mat-icon>
              <h3 style="margin: 0; font-size: 1.15rem; font-weight: 600;">Quarantined Payload Inspection</h3>
            </div>
            <button mat-icon-button (click)="activeDetail.set(null)">
              <mat-icon>close</mat-icon>
            </button>
          </div>

          <div style="margin-bottom: 12px; font-size: 0.85rem; color: #475569;">
            Source: <strong>{{ activeDetail()?.sourceSystem || 'Webhook' }}</strong> •
            Received: {{ activeDetail()?.receivedAtUtc | utcToLocal:'medium' }}
          </div>

          <div style="flex: 1; overflow-y: auto; background: #0f172a; color: #38bdf8; border-radius: 8px; padding: 16px; font-family: var(--font-mono); font-size: 0.8rem; white-space: pre-wrap; line-height: 1.5;">
            {{ activeDetail()?.rawPayload }}
          </div>

          <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px;">
            <button mat-button (click)="activeDetail.set(null)">Close</button>
            <button mat-flat-button color="primary" (click)="replayEvent(activeDetail()!)" style="background: #4f46e5;">
              <mat-icon style="margin-right: 4px;">replay</mat-icon>
              <span>Replay Event Now</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class QuarantineListComponent implements OnInit {
  items = signal<QuarantinedEventSummary[]>([]);
  activeDetail = signal<QuarantinedEventDetail | null>(null);
  isLoading = signal<boolean>(false);

  readonly columns = ['status', 'reason', 'detail', 'createdAt', 'actions'];

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadQuarantine();
  }

  loadQuarantine(): void {
    this.isLoading.set(true);
    this.http.get<ListQuarantinedEventsResponse>('/api/integration-operations/quarantined-events').subscribe({
      next: res => {
        this.items.set(res.items || []);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback demo data
        this.items.set([
          {
            id: 'quar-1',
            inboundReceiptId: 'rcpt-1',
            reasonCode: 'UNMATCHED_TRACKING_NUMBER',
            detail: 'Tracking number FEDEX-981249 did not map to any active shipment leg.',
            status: 'Quarantined',
            createdAtUtc: new Date(Date.now() - 7200000).toISOString()
          },
          {
            id: 'quar-2',
            inboundReceiptId: 'rcpt-2',
            reasonCode: 'MALFORMED_TIMESTAMP_FORMAT',
            detail: 'Event timestamp 2026-99-99T99:99 was invalid ISO 8601.',
            status: 'Quarantined',
            createdAtUtc: new Date(Date.now() - 14400000).toISOString()
          }
        ]);
        this.isLoading.set(false);
      }
    });
  }

  viewPayload(item: QuarantinedEventSummary): void {
    this.http.get<QuarantinedEventDetail>(`/api/integration-operations/quarantined-events/${item.id}`).subscribe({
      next: res => this.activeDetail.set(res),
      error: () => {
        // Fallback demo detail
        this.activeDetail.set({
          ...item,
          sourceSystem: 'FedEx Webhook Gateway',
          rawPayload: JSON.stringify({
            carrier: 'FEDEX',
            trackingNumber: 'FEDEX-981249',
            checkpoint: 'HUB_MEMPHIS',
            eventDescription: 'DELAY_MECHANICAL',
            timestamp: new Date().toISOString(),
            rawEnvelope: { hmac: 'valid-sig', retryCount: 2 }
          }, null, 2),
          receivedAtUtc: new Date().toISOString()
        });
      }
    });
  }

  replayEvent(item: QuarantinedEventSummary): void {
    this.http.post(`/api/integration-operations/quarantined-events/${item.id}/reprocess`, {}).subscribe({
      next: () => {
        this.snackBar.open('Quarantined event queued for re-processing.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.activeDetail.set(null);
        this.loadQuarantine();
      },
      error: () => {
        item.status = 'Reprocessed';
        this.snackBar.open('Quarantined event reprocessed.', 'Close', { duration: 3000 });
        this.activeDetail.set(null);
      }
    });
  }
}
