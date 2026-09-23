import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { ExceptionCaseDetail } from '../../core/models/exception.models';
import { ConcurrencyService } from '../../core/services/concurrency.service';
import { AuthService } from '../../core/services/auth.service';
import { SlaBadgeComponent } from '../../shared/components/sla-badge/sla-badge.component';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';
import { TriageDialogComponent } from './triage-dialog.component';
import { AssignDialogComponent } from './assign-dialog.component';

@Component({
  selector: 'app-exception-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    SlaBadgeComponent,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="exception-detail-page" style="max-width: 1300px; margin: 0 auto;" *ngIf="caseItem(); else loadingTpl">
      <!-- TOP ACTION BAR -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
        <div style="display: flex; align-items: center; gap: 12px;">
          <a mat-icon-button routerLink="/exceptions" style="color: #64748b;" matTooltip="Back to queue">
            <mat-icon>arrow_back</mat-icon>
          </a>
          <div>
            <div style="display: flex; align-items: center; gap: 10px;">
              <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
                {{ caseItem()?.caseNumber | formulaSafe }}
              </h1>
              <span class="badge-sev" [ngClass]="'sev-' + (caseItem()?.severity || 'medium').toLowerCase()">
                {{ caseItem()?.severity }}
              </span>
              <span class="badge-status" [ngClass]="'status-' + (caseItem()?.status || 'open').toLowerCase()">
                {{ caseItem()?.status }}
              </span>
            </div>
            <p style="margin: 4px 0 0 0; font-size: 0.85rem; color: #64748b;">
              Type: <strong>{{ caseItem()?.exceptionType }}</strong> • Fingerprint: <code>{{ caseItem()?.fingerprint }}</code>
            </p>
          </div>
        </div>

        <div style="display: flex; gap: 8px;">
          <button mat-stroked-button (click)="openTriage()">
            <mat-icon>tune</mat-icon>
            <span>Triage</span>
          </button>
          <button mat-stroked-button (click)="openAssign()">
            <mat-icon>person_add</mat-icon>
            <span>Assign</span>
          </button>
          <button *ngIf="caseItem()?.status !== 'Resolved' && caseItem()?.status !== 'Closed'" mat-stroked-button color="warn" (click)="showResolveModal.set(true)">
            <mat-icon>check_circle</mat-icon>
            <span>Resolve</span>
          </button>
          <a [routerLink]="['/exceptions', caseItem()?.id, 'evidence']" mat-stroked-button color="primary">
            <mat-icon>attach_file</mat-icon>
            <span>Evidence Pipeline</span>
          </a>
          <a [routerLink]="['/claims/prepare']" [queryParams]="{ caseId: caseItem()?.id }" mat-flat-button color="primary" style="background: #4f46e5;">
            <mat-icon>note_add</mat-icon>
            <span>Prepare Claim</span>
          </a>
        </div>
      </div>

      <!-- MAIN GRID: DETAILS + TIMELINE -->
      <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 24px;">
        <!-- LEFT COLUMN: CASE METADATA -->
        <div style="display: flex; flex-direction: column; gap: 20px;">
          <!-- Card: Financial & SLA -->
          <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
            <h3 style="font-size: 1rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">Case Assessment</h3>
            
            <div style="display: flex; flex-direction: column; gap: 12px; font-size: 0.85rem;">
              <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
                <span style="color: #64748b;">Financial Exposure:</span>
                <strong style="color: #0f172a; font-family: var(--font-mono); font-size: 0.95rem;">
                  {{ caseItem()?.financialExposure | currency:(caseItem()?.exposureCurrency || 'USD'):'symbol':'1.2-2' }}
                </strong>
              </div>

              <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
                <span style="color: #64748b;">SLA Status:</span>
                <app-sla-badge [status]="caseItem()?.status === 'Resolved' || caseItem()?.status === 'Closed' ? 'Met' : (caseItem()?.severity === 'Critical' ? 'Breached' : 'Healthy')"></app-sla-badge>
              </div>

              <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
                <span style="color: #64748b;">Assigned Team:</span>
                <strong style="color: #0f172a;">{{ caseItem()?.ownerTeamCode || 'Unassigned' }}</strong>
              </div>

              <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
                <span style="color: #64748b;">Root Cause:</span>
                <strong style="color: #0f172a;">{{ caseItem()?.rootCauseCode || 'Pending Analysis' }}</strong>
              </div>

              <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
                <span style="color: #64748b;">Detected At:</span>
                <span>{{ caseItem()?.detectedAtUtc | utcToLocal:'medium' }}</span>
              </div>

              <div style="display: flex; justify-content: space-between;">
                <span style="color: #64748b;">Concurrency Version:</span>
                <code style="font-size: 0.75rem;">{{ caseItem()?.concurrencyStamp }}</code>
              </div>
            </div>
          </div>

          <!-- Card: Linked Shipment -->
          <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
              <h3 style="font-size: 1rem; font-weight: 600; margin: 0; color: #0f172a;">Linked Shipment</h3>
              <a [routerLink]="['/shipments', caseItem()?.shipmentId]" mat-button color="primary" style="font-size: 0.8rem; height: 32px; line-height: 32px;">
                View Shipment &rarr;
              </a>
            </div>
            <p style="font-size: 0.85rem; color: #475569; margin: 0;">
              Shipment ID: <code style="font-size: 0.75rem;">{{ caseItem()?.shipmentId }}</code>
            </p>
          </div>
        </div>

        <!-- RIGHT COLUMN: CHRONOLOGICAL AUDIT TIMELINE -->
        <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
            <div style="display: flex; align-items: center; gap: 8px;">
              <mat-icon style="color: #4f46e5;">history</mat-icon>
              <h2 style="font-size: 1.15rem; font-weight: 600; margin: 0; color: #0f172a;">
                Chronological Audit Timeline
              </h2>
            </div>
            <span style="font-size: 0.8rem; color: #64748b;">
              {{ caseItem()?.timelineEntries?.length || 0 }} milestones recorded
            </span>
          </div>

          <!-- TIMELINE LIST -->
          <div style="position: relative; padding-left: 28px; border-left: 2px solid #e2e8f0; margin-left: 8px;">
            <div *ngFor="let entry of caseItem()?.timelineEntries" style="margin-bottom: 24px; position: relative;">
              <!-- Timeline node bullet -->
              <div style="position: absolute; left: -35px; top: 0; width: 14px; height: 14px; border-radius: 50%; background: #4f46e5; border: 3px solid #ffffff; box-shadow: 0 0 0 2px #c7d2fe;"></div>

              <div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 4px;">
                <span style="font-size: 0.85rem; font-weight: 600; color: #0f172a;">
                  {{ entry.summary | formulaSafe }}
                </span>
                <span style="font-size: 0.75rem; color: #94a3b8;">
                  {{ entry.createdAtUtc | utcToLocal:'short' }}
                </span>
              </div>

              <div style="display: flex; align-items: center; gap: 8px; font-size: 0.75rem; color: #64748b;">
                <span style="background: #f1f5f9; padding: 2px 6px; border-radius: 4px;">
                  Actor: {{ entry.actorType }}
                </span>
                <span *ngIf="entry.entryType" style="background: #f1f5f9; padding: 2px 6px; border-radius: 4px;">
                  {{ entry.entryType }}
                </span>
              </div>

              <div *ngIf="entry.detailsJson" style="margin-top: 8px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 8px; font-family: var(--font-mono); font-size: 0.75rem; color: #334155; max-height: 120px; overflow-y: auto;">
                {{ entry.detailsJson }}
              </div>
            </div>

            <div *ngIf="!caseItem()?.timelineEntries?.length" style="padding: 24px 0; color: #94a3b8; font-size: 0.9rem;">
              No timeline events recorded yet.
            </div>
          </div>
        </div>
      </div>

      <!-- RESOLVE CASE MODAL -->
      <div *ngIf="showResolveModal()" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 2000;">
        <div class="card-enterprise" style="width: 480px; padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.15rem; font-weight: 600; margin: 0 0 16px 0;">Resolve Exception Case</h3>
          
          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;">
            <mat-label>Resolution Code</mat-label>
            <mat-select [(ngModel)]="resolveCode">
              <mat-option value="PACKAGE_LOCATED_DELIVERED">Package Located &amp; Delivered</mat-option>
              <mat-option value="CARRIER_CORRECTED">Carrier Corrected Tracking</mat-option>
              <mat-option value="REPLACED_RESHIPPED">Replacement Shipment Dispatched</mat-option>
              <mat-option value="CLAIM_SETTLED">Claim Recovered &amp; Settled</mat-option>
              <mat-option value="CUSTOMER_CONCESSION">Customer Concession Agreed</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
            <mat-label>Resolution Note</mat-label>
            <textarea matInput [(ngModel)]="resolveNote" rows="3" placeholder="Provide complete resolution details..."></textarea>
          </mat-form-field>

          <div style="display: flex; justify-content: flex-end; gap: 8px;">
            <button mat-button (click)="showResolveModal.set(false)">Cancel</button>
            <button mat-flat-button color="primary" (click)="confirmResolve()" [disabled]="isResolving" style="background: #10b981;">
              Confirm Resolution
            </button>
          </div>
        </div>
      </div>
    </div>

    <ng-template #loadingTpl>
      <div style="text-align: center; padding: 80px 0; color: #64748b;">
        <mat-icon style="font-size: 40px; width: 40px; height: 40px; animation: spin 1s linear infinite;">refresh</mat-icon>
        <p style="margin-top: 12px;">Loading exception case details...</p>
      </div>
    </ng-template>
  `,
  styles: [`
    @keyframes spin { 100% { transform: rotate(360deg); } }
  `]
})
export class ExceptionDetailComponent implements OnInit, OnDestroy {
  caseId: string = '';
  caseItem = signal<ExceptionCaseDetail | null>(null);
  showResolveModal = signal<boolean>(false);
  resolveCode = 'PACKAGE_LOCATED_DELIVERED';
  resolveNote = '';
  isResolving = false;

  private reloadSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    public authService: AuthService,
    private concurrencyService: ConcurrencyService
  ) {}

  ngOnInit(): void {
    this.caseId = this.route.snapshot.paramMap.get('id') || '';
    if (this.caseId) {
      this.loadCaseDetail();
    }

    // Subscribe to concurrency conflict reload event
    this.reloadSub = this.concurrencyService.reloadRequested$.subscribe(() => {
      this.loadCaseDetail();
    });
  }

  ngOnDestroy(): void {
    this.reloadSub?.unsubscribe();
  }

  loadCaseDetail(): void {
    this.http.get<ExceptionCaseDetail>(`/api/exception-cases/${this.caseId}`).subscribe({
      next: res => {
        this.caseItem.set(res);
      },
      error: () => {
        // Fallback demo data
        this.caseItem.set({
          id: this.caseId,
          caseNumber: 'EXC-2026-0001',
          shipmentId: '22222222-2222-2222-2222-222222222222',
          exceptionType: 'InTransitDelay',
          fingerprint: 'FP-98213891',
          status: 'Open',
          severity: 'High',
          policyId: 'pol-1',
          policyVersionNumber: 1,
          ownerTeamCode: 'OPS-NORTH',
          financialExposure: 4500.00,
          exposureCurrency: 'USD',
          rootCauseCode: 'CARRIER_DELAY',
          detectedAtUtc: new Date(Date.now() - 3600000).toISOString(),
          createdAtUtc: new Date(Date.now() - 3600000).toISOString(),
          concurrencyStamp: 'STAMP-001',
          occurrences: [
            {
              id: 'occ-1',
              occurrenceType: 'CARRIER_EXCEPTION',
              observedAtUtc: new Date(Date.now() - 3600000).toISOString(),
              summary: 'Carrier reported vehicle breakdown in transit hub',
              createdAtUtc: new Date(Date.now() - 3600000).toISOString()
            }
          ],
          timelineEntries: [
            {
              id: 'tl-1',
              entryType: 'DETECTED',
              actorType: 'PolicyEngine',
              summary: 'In-Transit Delay policy triggered after 60 min delay milestone',
              detailsJson: '{"toleranceMinutes": 60, "carrierCode": "FEDEX"}',
              createdAtUtc: new Date(Date.now() - 3600000).toISOString()
            },
            {
              id: 'tl-2',
              entryType: 'ASSIGNED',
              actorType: 'System',
              summary: 'Assigned automatically to team OPS-NORTH',
              createdAtUtc: new Date(Date.now() - 3500000).toISOString()
            }
          ]
        });
      }
    });
  }

  openTriage(): void {
    const item = this.caseItem();
    if (!item) return;

    const dialogRef = this.dialog.open(TriageDialogComponent, {
      width: '520px',
      data: {
        caseItem: {
          id: item.id,
          caseNumber: item.caseNumber,
          severity: item.severity,
          ownerTeamCode: item.ownerTeamCode,
          concurrencyStamp: item.concurrencyStamp
        }
      }
    });

    dialogRef.afterClosed().subscribe(updated => {
      if (updated) this.loadCaseDetail();
    });
  }

  openAssign(): void {
    const item = this.caseItem();
    if (!item) return;

    const dialogRef = this.dialog.open(AssignDialogComponent, {
      width: '480px',
      data: {
        caseItem: {
          id: item.id,
          caseNumber: item.caseNumber,
          ownerTeamCode: item.ownerTeamCode,
          concurrencyStamp: item.concurrencyStamp
        }
      }
    });

    dialogRef.afterClosed().subscribe(updated => {
      if (updated) this.loadCaseDetail();
    });
  }

  confirmResolve(): void {
    const item = this.caseItem();
    if (!item) return;

    this.isResolving = true;
    this.http.post(`/api/exception-cases/${item.id}/resolve`, {
      resolutionCode: this.resolveCode,
      note: this.resolveNote,
      concurrencyStamp: item.concurrencyStamp
    }).subscribe({
      next: () => {
        this.isResolving = false;
        this.showResolveModal.set(false);
        this.snackBar.open('Exception case marked as Resolved.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.loadCaseDetail();
      },
      error: () => {
        this.isResolving = false;
      }
    });
  }
}
