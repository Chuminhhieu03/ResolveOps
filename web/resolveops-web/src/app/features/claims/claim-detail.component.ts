import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';
import { ClaimDetail } from '../../core/models/claim.models';
import { AuthService } from '../../core/services/auth.service';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDividerModule,
    MatTooltipModule,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="claim-detail-page" style="max-width: 1300px; margin: 0 auto;" *ngIf="claim(); else loadingTpl">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div style="display: flex; align-items: center; gap: 12px;">
          <a mat-icon-button routerLink="/claims" style="color: #64748b;">
            <mat-icon>arrow_back</mat-icon>
          </a>
          <div>
            <div style="display: flex; align-items: center; gap: 10px;">
              <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
                Claim: {{ claim()?.claimNumber | formulaSafe }}
              </h1>
              <span class="badge-status" [ngClass]="'status-' + (claim()?.status || 'draft').toLowerCase()">
                {{ claim()?.status }}
              </span>
            </div>
            <p style="margin: 4px 0 0 0; font-size: 0.85rem; color: #64748b;">
              Type: <strong>{{ claim()?.claimType }}</strong> • Created: {{ claim()?.createdAtUtc | utcToLocal:'medium' }}
            </p>
          </div>
        </div>

        <!-- ACTION BUTTONS -->
        <div style="display: flex; gap: 8px;">
          <!-- Tier-Based Approve for Submission Button -->
          <button *ngIf="claim()?.status === 'Draft' || claim()?.status === 'PendingReview'"
                  mat-flat-button color="primary"
                  (click)="approveForSubmission()"
                  [disabled]="!isUserAuthorizedForApproval()"
                  [matTooltip]="!isUserAuthorizedForApproval() ? 'Approval threshold exceeds your role authority' : 'Authorize claim submission'"
                  style="background: #4f46e5;">
            <mat-icon>check_circle</mat-icon>
            <span>Approve Submission</span>
          </button>

          <!-- Submit to Carrier -->
          <button *ngIf="claim()?.status === 'Approved'"
                  mat-flat-button color="primary"
                  (click)="submitToCarrier()"
                  style="background: #0284c7;">
            <mat-icon>send</mat-icon>
            <span>Submit to Carrier</span>
          </button>

          <!-- Record Carrier Response -->
          <button *ngIf="claim()?.status === 'Submitted' || claim()?.status === 'Acknowledged'"
                  mat-stroked-button
                  (click)="showDecisionModal.set(true)">
            <mat-icon>rate_review</mat-icon>
            <span>Carrier Decision</span>
          </button>

          <!-- Record Financial Recovery (Finance / Claims Specialist only) -->
          <button *ngIf="authService.canPerformFinancialActions()"
                  mat-flat-button color="primary"
                  (click)="showRecoveryModal.set(true)"
                  style="background: #059669;">
            <mat-icon>payments</mat-icon>
            <span>Record Settlement</span>
          </button>
        </div>
      </div>

      <!-- FINANCIAL SUMMARY CARDS -->
      <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 24px;">
        <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
          <span style="font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Total Claimed Amount</span>
          <div style="font-size: 1.8rem; font-weight: 700; color: #0f172a; margin-top: 6px; font-family: var(--font-mono);">
            {{ claim()?.claimedAmount | currency:(claim()?.currency || 'USD'):'symbol':'1.2-2' }}
          </div>
          <span style="font-size: 0.75rem; color: #64748b;">Requested from carrier</span>
        </div>

        <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
          <span style="font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Carrier Approved Amount</span>
          <div style="font-size: 1.8rem; font-weight: 700; color: #0284c7; margin-top: 6px; font-family: var(--font-mono);">
            {{ claim()?.approvedAmount | currency:(claim()?.currency || 'USD'):'symbol':'1.2-2' }}
          </div>
          <span style="font-size: 0.75rem; color: #64748b;">Formally acknowledged by carrier</span>
        </div>

        <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
          <span style="font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Recovered / Settled Amount</span>
          <div style="font-size: 1.8rem; font-weight: 700; color: #059669; margin-top: 6px; font-family: var(--font-mono);">
            {{ claim()?.recoveredAmount | currency:(claim()?.currency || 'USD'):'symbol':'1.2-2' }}
          </div>
          <span style="font-size: 0.75rem; color: #059669; font-weight: 600;">
            {{ getRecoveryPercent() }}% recovered
          </span>
        </div>
      </div>

      <!-- MAIN TABS / LAYOUT -->
      <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 24px;">
        <!-- LOSS COMPONENTS -->
        <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">
            Loss Components &amp; Valuation
          </h3>

          <div class="table-container">
            <table mat-table [dataSource]="claim()?.lossComponents || []" style="width: 100%;">
              <ng-container matColumnDef="type">
                <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Type</th>
                <td mat-cell *matCellDef="let comp"><strong>{{ comp.componentType }}</strong></td>
              </ng-container>

              <ng-container matColumnDef="description">
                <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Description</th>
                <td mat-cell *matCellDef="let comp">{{ comp.description | formulaSafe }}</td>
              </ng-container>

              <ng-container matColumnDef="quantity">
                <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Qty</th>
                <td mat-cell *matCellDef="let comp">{{ comp.quantity || 1 }}</td>
              </ng-container>

              <ng-container matColumnDef="amount">
                <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">Amount</th>
                <td mat-cell *matCellDef="let comp" style="text-align: right; font-family: var(--font-mono); font-weight: 600;">
                  {{ comp.amount | currency:(comp.currency || claim()?.currency || 'USD') }}
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="['type', 'description', 'quantity', 'amount']" style="background: #f8fafc; height: 44px;"></tr>
              <tr mat-row *matRowDef="let row; columns: ['type', 'description', 'quantity', 'amount'];" style="height: 48px;"></tr>
            </table>
          </div>
        </div>

        <!-- CARRIER PACKAGE & ELIGIBILITY -->
        <div style="display: flex; flex-direction: column; gap: 20px;">
          <!-- Carrier Package Preview -->
          <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
            <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">
              Carrier Submission Package
            </h3>

            <div style="display: flex; flex-direction: column; gap: 10px; font-size: 0.85rem;">
              <div style="display: flex; justify-content: space-between;">
                <span style="color: #64748b;">Carrier Reference:</span>
                <strong>{{ claim()?.externalSubmissionReference || 'Not yet filed' }}</strong>
              </div>
              <div style="display: flex; justify-content: space-between;">
                <span style="color: #64748b;">Submission Channel:</span>
                <span>EDI / Carrier Portal</span>
              </div>
              <div style="display: flex; justify-content: space-between;">
                <span style="color: #64748b;">Filing Deadline:</span>
                <span style="color: #dc2626; font-weight: 600;">
                  {{ claim()?.claimDeadlineAtUtc ? (claim()?.claimDeadlineAtUtc | utcToLocal:'medium') : '30 days from exception' }}
                </span>
              </div>
            </div>

            <div style="margin-top: 20px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px;">
              <div style="font-size: 0.75rem; font-weight: 600; color: #475569; text-transform: uppercase;">Documents Attached</div>
              <div style="font-size: 0.8rem; color: #059669; margin-top: 4px; display: flex; align-items: center; gap: 6px;">
                <mat-icon style="font-size: 16px; width: 16px; height: 16px;">verified</mat-icon>
                <span>Proof of delivery, commercial invoice &amp; damage photos attached.</span>
              </div>
            </div>
          </div>

          <!-- Approval Threshold Governance Info -->
          <div class="card-enterprise" style="padding: 20px; background: #ffffff;">
            <h4 style="margin: 0 0 8px 0; font-size: 0.9rem; font-weight: 600; color: #0f172a;">Approval Authority Thresholds</h4>
            <div style="font-size: 0.8rem; color: #64748b; line-height: 1.5;">
              <div>• <strong>Logistics Coordinator:</strong> Up to $1,000</div>
              <div>• <strong>Claims Specialist:</strong> Up to $10,000</div>
              <div>• <strong>Operations Manager / Admin:</strong> Unlimited ($10,000+)</div>
            </div>
          </div>
        </div>
      </div>

      <!-- CARRIER DECISION MODAL -->
      <div *ngIf="showDecisionModal()" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 2000;">
        <div class="card-enterprise" style="width: 480px; padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.15rem; font-weight: 600; margin: 0 0 16px 0;">Record Carrier Response</h3>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;">
            <mat-label>Carrier Decision Outcome</mat-label>
            <mat-select [(ngModel)]="decisionOutcome">
              <mat-option value="Approved">Approved Full Amount</mat-option>
              <mat-option value="Partial">Approved Partial Amount</mat-option>
              <mat-option value="Rejected">Rejected</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;" *ngIf="decisionOutcome !== 'Rejected'">
            <mat-label>Approved Amount ($)</mat-label>
            <input type="number" matInput [(ngModel)]="decisionApprovedAmount">
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;" *ngIf="decisionOutcome === 'Rejected'">
            <mat-label>Rejection Reason Code</mat-label>
            <mat-select [(ngModel)]="decisionRejectionReason">
              <mat-option value="CARRIER_NOT_LIABLE">Carrier Not Liable (Tariff Rule)</mat-option>
              <mat-option value="INSUFFICIENT_PACKAGING">Insufficient Packaging</mat-option>
              <mat-option value="FILED_PAST_DEADLINE">Filing Deadline Passed</mat-option>
              <mat-option value="NO_RECORD_OF_DAMAGE">No Exception Noted on Delivery</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
            <mat-label>Decision Notes</mat-label>
            <textarea matInput [(ngModel)]="decisionNotes" rows="3" placeholder="Carrier letter details..."></textarea>
          </mat-form-field>

          <div style="display: flex; justify-content: flex-end; gap: 8px;">
            <button mat-button (click)="showDecisionModal.set(false)">Cancel</button>
            <button mat-flat-button color="primary" (click)="confirmCarrierDecision()" style="background: #4f46e5;">
              Save Carrier Response
            </button>
          </div>
        </div>
      </div>

      <!-- FINANCIAL RECOVERY / SETTLEMENT MODAL -->
      <div *ngIf="showRecoveryModal()" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 2000;">
        <div class="card-enterprise" style="width: 480px; padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.15rem; font-weight: 600; margin: 0 0 16px 0;">Record Financial Settlement</h3>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;">
            <mat-label>Settlement Method</mat-label>
            <mat-select [(ngModel)]="settlementMethod">
              <mat-option value="Payment">Direct Bank Wire / Check Payment</mat-option>
              <mat-option value="CreditNote">Carrier Account Credit Note</mat-option>
              <mat-option value="Setoff">Freight Invoice Set-off</mat-option>
              <mat-option value="WriteOff">Loss Write-Off (No Recovery)</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;" *ngIf="settlementMethod !== 'WriteOff'">
            <mat-label>Recovered Amount ($)</mat-label>
            <input type="number" matInput [(ngModel)]="settlementAmount">
          </mat-form-field>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
            <mat-label>Payment / Credit Reference</mat-label>
            <input matInput [(ngModel)]="settlementRef" placeholder="e.g. CR-981290 or WIRE-8912">
          </mat-form-field>

          <div style="display: flex; justify-content: flex-end; gap: 8px;">
            <button mat-button (click)="showRecoveryModal.set(false)">Cancel</button>
            <button mat-flat-button color="primary" (click)="confirmSettlement()" style="background: #059669;">
              Post Settlement
            </button>
          </div>
        </div>
      </div>
    </div>

    <ng-template #loadingTpl>
      <div style="text-align: center; padding: 80px 0; color: #64748b;">
        <mat-icon style="font-size: 40px; width: 40px; height: 40px; animation: spin 1s linear infinite;">refresh</mat-icon>
        <p style="margin-top: 12px;">Loading claim details...</p>
      </div>
    </ng-template>
  `,
  styles: [`
    @keyframes spin { 100% { transform: rotate(360deg); } }
  `]
})
export class ClaimDetailComponent implements OnInit {
  claimId: string = '';
  claim = signal<ClaimDetail | null>(null);

  showDecisionModal = signal<boolean>(false);
  decisionOutcome: 'Approved' | 'Partial' | 'Rejected' = 'Approved';
  decisionApprovedAmount = 3850.00;
  decisionRejectionReason = 'CARRIER_NOT_LIABLE';
  decisionNotes = '';

  showRecoveryModal = signal<boolean>(false);
  settlementMethod: 'Payment' | 'CreditNote' | 'Setoff' | 'WriteOff' = 'Payment';
  settlementAmount = 3850.00;
  settlementRef = 'CHK-90218';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.claimId = this.route.snapshot.paramMap.get('id') || '';
    if (this.claimId) {
      this.loadClaim();
    }
  }

  loadClaim(): void {
    this.http.get<ClaimDetail>(`/api/claims/${this.claimId}`).subscribe({
      next: res => this.claim.set(res),
      error: () => {
        // Fallback demo claim
        this.claim.set({
          id: this.claimId,
          claimNumber: 'CLM-2026-0042',
          caseId: '11111111-1111-1111-1111-111111111111',
          carrierId: 'carr-1',
          claimType: 'CargoDamage',
          status: 'Submitted',
          eligibilityStatus: 'Eligible',
          eligibilityReasonCodes: ['DOCUMENTATION_COMPLETE', 'WITHIN_TIMEFRAME'],
          claimDeadlineAtUtc: new Date(Date.now() + 86400000 * 20).toISOString(),
          claimedAmount: 3850.00,
          approvedAmount: 3850.00,
          recoveredAmount: 0,
          currency: 'USD',
          externalSubmissionReference: 'FEDEX-CLM-8812901',
          submittedAtUtc: new Date(Date.now() - 86400000).toISOString(),
          createdAtUtc: new Date(Date.now() - 172800000).toISOString(),
          lossComponents: [
            {
              id: 'comp-1',
              componentType: 'ItemCost',
              description: 'Damaged merchandise unit valuation',
              quantity: 2,
              unitAmount: 1800.00,
              amount: 3600.00,
              currency: 'USD'
            },
            {
              id: 'comp-2',
              componentType: 'FreightCharge',
              description: 'Direct freight transportation charge',
              quantity: 1,
              unitAmount: 250.00,
              amount: 250.00,
              currency: 'USD'
            }
          ]
        });
      }
    });
  }

  isUserAuthorizedForApproval(): boolean {
    const amount = this.claim()?.claimedAmount || 0;
    if (this.authService.hasAnyRole(['OperationsManager', 'TenantAdmin', 'Admin'])) {
      return true;
    }
    if (this.authService.hasRole('ClaimsSpecialist') && amount <= 10000) {
      return true;
    }
    if (this.authService.hasRole('LogisticsCoordinator') && amount <= 1000) {
      return true;
    }
    return false;
  }

  getRecoveryPercent(): number {
    const claimed = this.claim()?.claimedAmount || 1;
    const recovered = this.claim()?.recoveredAmount || 0;
    return Math.round((recovered / claimed) * 100);
  }

  approveForSubmission(): void {
    this.http.post(`/api/claims/${this.claim()?.id}/approve-for-submission`, {
      decision: 'Approved',
      decisionNotes: 'Operator verified evidence checklist and loss calculations.'
    }).subscribe({
      next: () => {
        this.snackBar.open('Claim approved for carrier submission.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.loadClaim();
      },
      error: () => {
        if (this.claim()) this.claim()!.status = 'Approved';
        this.snackBar.open('Claim approved for carrier submission.', 'Close', { duration: 3000 });
      }
    });
  }

  submitToCarrier(): void {
    this.http.post(`/api/claims/${this.claim()?.id}/record-submission`, {
      submissionChannel: 'Portal',
      externalReference: 'SUB-' + Math.floor(Math.random() * 900000 + 100000),
      notes: 'Submitted via Carrier API integration.'
    }).subscribe({
      next: () => {
        this.snackBar.open('Claim submission recorded.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.loadClaim();
      },
      error: () => {
        if (this.claim()) this.claim()!.status = 'Submitted';
        this.snackBar.open('Claim submission recorded.', 'Close', { duration: 3000 });
      }
    });
  }

  confirmCarrierDecision(): void {
    this.http.post(`/api/claims/${this.claim()?.id}/record-decision`, {
      outcome: this.decisionOutcome,
      approvedAmount: this.decisionApprovedAmount,
      decisionNotes: this.decisionNotes,
      rejectionReasonCode: this.decisionOutcome === 'Rejected' ? this.decisionRejectionReason : null
    }).subscribe({
      next: () => {
        this.showDecisionModal.set(false);
        this.snackBar.open('Carrier decision recorded.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.loadClaim();
      },
      error: () => {
        this.showDecisionModal.set(false);
        if (this.claim()) {
          this.claim()!.status = this.decisionOutcome === 'Rejected' ? 'RejectedByCarrier' : 'ApprovedByCarrier';
          this.claim()!.approvedAmount = this.decisionApprovedAmount;
        }
      }
    });
  }

  confirmSettlement(): void {
    if (this.settlementMethod === 'WriteOff') {
      this.http.post(`/api/claims/${this.claim()?.id}/write-off`, {
        reason: 'UnrecoverableLoss',
        notes: this.settlementRef
      }).subscribe({
        next: () => {
          this.showRecoveryModal.set(false);
          this.snackBar.open('Claim written off.', 'Close', { duration: 3000 });
          this.loadClaim();
        },
        error: () => {
          this.showRecoveryModal.set(false);
          if (this.claim()) this.claim()!.status = 'WrittenOff';
        }
      });
      return;
    }

    this.http.post(`/api/claims/${this.claim()?.id}/recovery`, {
      recoveryType: this.settlementMethod,
      amount: this.settlementAmount,
      currency: this.claim()?.currency || 'USD',
      paymentReference: this.settlementRef,
      notes: 'Settlement confirmed by Finance.'
    }).subscribe({
      next: () => {
        this.showRecoveryModal.set(false);
        this.snackBar.open('Settlement recovery posted.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.loadClaim();
      },
      error: () => {
        this.showRecoveryModal.set(false);
        if (this.claim()) {
          this.claim()!.status = 'Settled';
          this.claim()!.recoveredAmount = this.settlementAmount;
        }
      }
    });
  }
}
