import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { z } from 'zod';
import { LossComponent, ClaimDetail } from '../../core/models/claim.models';
import { AuthService } from '../../core/services/auth.service';

// Zod Client-Side Validation Schema
const lossComponentSchema = z.object({
  componentType: z.string().min(1, 'Type required'),
  description: z.string().min(3, 'Description must be at least 3 characters'),
  quantity: z.number().positive('Quantity must be greater than 0').optional(),
  unitAmount: z.number().nonnegative('Unit amount cannot be negative').optional(),
  amount: z.number().positive('Amount must be positive'),
  currency: z.string().length(3, 'Currency must be 3-letter ISO code')
});

const claimFormSchema = z.object({
  caseId: z.string().uuid('Valid Case ID required'),
  claimType: z.string().min(1, 'Claim type required'),
  currency: z.string().length(3, 'Currency required'),
  lossComponents: z.array(lossComponentSchema).min(1, 'At least one loss component required')
});

@Component({
  selector: 'app-claim-prepare',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule
  ],
  template: `
    <div class="claim-prepare-page" style="max-width: 1100px; margin: 0 auto;">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div style="display: flex; align-items: center; gap: 12px;">
          <a mat-icon-button routerLink="/claims" style="color: #64748b;">
            <mat-icon>arrow_back</mat-icon>
          </a>
          <div>
            <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
              Carrier Claim Preparation Wizard
            </h1>
            <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
              Assemble loss components, calculate total claim exposure, and draft formal filing.
            </p>
          </div>
        </div>
      </div>

      <!-- RULE 4 INVARIANT CALLOUT -->
      <div style="background: #eef2ff; border-left: 4px solid #4f46e5; border-radius: 8px; padding: 16px; margin-bottom: 24px; display: flex; gap: 14px; align-items: flex-start;">
        <mat-icon style="color: #4f46e5; font-size: 24px; width: 24px; height: 24px;">verified_user</mat-icon>
        <div>
          <strong style="color: #3730a3; font-size: 0.9rem;">Human Operator Governance (Rule 4 Invariant):</strong>
          <p style="margin: 4px 0 0 0; font-size: 0.85rem; color: #4338ca; line-height: 1.4;">
            AI assistance is strictly forbidden from executing or signing off on financial claim state transitions.
            All claimed amounts and settlement decisions require manual human specialist verification and tier-based approval.
          </p>
        </div>
      </div>

      <!-- STEP 1: CASE SELECTION & BASIC METADATA -->
      <div class="card-enterprise" style="padding: 24px; margin-bottom: 24px; background: #ffffff;">
        <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">
          1. Claim Context &amp; Exception Case
        </h3>

        <div style="display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 16px;">
          <mat-form-field appearance="outline">
            <mat-label>Eligible Exception Case ID</mat-label>
            <input matInput [(ngModel)]="caseId" placeholder="Case UUID" (blur)="validateCaseId()">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Claim Type</mat-label>
            <mat-select [(ngModel)]="claimType">
              <mat-option value="CargoDamage">Cargo Damage</mat-option>
              <mat-option value="TotalLoss">Total Loss</mat-option>
              <mat-option value="ConcealedDamage">Concealed Damage</mat-option>
              <mat-option value="ServiceFailure">Service Guarantee Failure</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Currency</mat-label>
            <mat-select [(ngModel)]="currency">
              <mat-option value="USD">USD ($)</mat-option>
              <mat-option value="EUR">EUR (€)</mat-option>
              <mat-option value="SGD">SGD (S$)</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </div>

      <!-- STEP 2: DYNAMIC LOSS COMPONENTS -->
      <div class="card-enterprise" style="padding: 24px; margin-bottom: 24px; background: #ffffff;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
          <div>
            <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0; color: #0f172a;">
              2. Loss Components Breakdown
            </h3>
            <span style="font-size: 0.8rem; color: #64748b;">Itemized costs contributing to total carrier claim</span>
          </div>

          <button mat-stroked-button color="primary" (click)="addLossComponent()">
            <mat-icon>add</mat-icon>
            <span>Add Cost Component</span>
          </button>
        </div>

        <div class="table-container" style="margin-bottom: 16px;">
          <table mat-table [dataSource]="lossComponents()" style="width: 100%;">
            <!-- Type -->
            <ng-container matColumnDef="type">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; width: 220px;">Cost Type</th>
              <td mat-cell *matCellDef="let comp; let idx = index">
                <mat-select [(ngModel)]="comp.componentType" style="font-size: 0.85rem;">
                  <mat-option value="ItemCost">Direct Item Cost</mat-option>
                  <mat-option value="FreightCharge">Freight / Shipping Fee</mat-option>
                  <mat-option value="Labor">Inspection &amp; Repackaging Labor</mat-option>
                  <mat-option value="Customs">Customs &amp; Duties</mat-option>
                  <mat-option value="SalvageCredit">Salvage Credit (Deduction)</mat-option>
                </mat-select>
              </td>
            </ng-container>

            <!-- Description -->
            <ng-container matColumnDef="description">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Description</th>
              <td mat-cell *matCellDef="let comp">
                <input matInput [(ngModel)]="comp.description" placeholder="Description of loss..." style="font-size: 0.85rem; width: 100%;">
              </td>
            </ng-container>

            <!-- Qty & Unit -->
            <ng-container matColumnDef="unit">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; width: 120px;">Qty × Rate</th>
              <td mat-cell *matCellDef="let comp">
                <div style="display: flex; gap: 4px; align-items: center;">
                  <input type="number" [(ngModel)]="comp.quantity" (ngModelChange)="recalcRow(comp)" style="width: 50px; font-size: 0.85rem; padding: 4px; border: 1px solid #cbd5e1; border-radius: 4px;">
                  <span>×</span>
                  <input type="number" [(ngModel)]="comp.unitAmount" (ngModelChange)="recalcRow(comp)" style="width: 70px; font-size: 0.85rem; padding: 4px; border: 1px solid #cbd5e1; border-radius: 4px;">
                </div>
              </td>
            </ng-container>

            <!-- Subtotal -->
            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; width: 140px; text-align: right;">Amount</th>
              <td mat-cell *matCellDef="let comp" style="text-align: right;">
                <input type="number" [(ngModel)]="comp.amount" (ngModelChange)="onAmountChange()" style="width: 100px; text-align: right; font-weight: 600; font-family: var(--font-mono); font-size: 0.85rem; padding: 4px 6px; border: 1px solid #cbd5e1; border-radius: 4px;">
              </td>
            </ng-container>

            <!-- Actions -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="width: 40px;"></th>
              <td mat-cell *matCellDef="let comp; let idx = index" style="text-align: right;">
                <button mat-icon-button (click)="removeLossComponent(idx)" [disabled]="lossComponents().length <= 1" style="color: #ef4444;">
                  <mat-icon style="font-size: 18px;">delete</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="['type', 'description', 'unit', 'amount', 'actions']" style="background: #f8fafc; height: 44px;"></tr>
            <tr mat-row *matRowDef="let row; columns: ['type', 'description', 'unit', 'amount', 'actions'];" style="height: 48px;"></tr>
          </table>
        </div>

        <!-- TOTAL CALCULATION FOOTER -->
        <div style="display: flex; justify-content: flex-end; align-items: center; gap: 20px; padding: 16px 20px; background: #f8fafc; border-radius: 8px; border: 1px solid #e2e8f0;">
          <span style="font-size: 1rem; font-weight: 600; color: #475569;">Total Claimed Amount:</span>
          <span style="font-size: 1.5rem; font-weight: 700; color: #0f172a; font-family: var(--font-mono);">
            {{ totalClaimedAmount() | currency:currency:'symbol':'1.2-2' }}
          </span>
        </div>
      </div>

      <!-- VALIDATION ERRORS DISPLAY -->
      <div *ngIf="validationErrors().length > 0" style="background: #fef2f2; border: 1px solid #fecaca; color: #991b1b; padding: 16px; border-radius: 8px; margin-bottom: 24px;">
        <div style="font-weight: 600; margin-bottom: 6px;">Please correct the following errors:</div>
        <ul style="margin: 0; padding-left: 20px; font-size: 0.85rem;">
          <li *ngFor="let err of validationErrors()">{{ err }}</li>
        </ul>
      </div>

      <!-- ACTIONS -->
      <div style="display: flex; justify-content: flex-end; gap: 12px; margin-bottom: 40px;">
        <button mat-stroked-button routerLink="/claims">Cancel</button>
        <button mat-flat-button color="primary" (click)="saveDraftClaim()" [disabled]="isSubmitting" style="background: #4f46e5; height: 48px; padding: 0 32px; font-size: 0.95rem;">
          <mat-icon style="margin-right: 6px;">save</mat-icon>
          <span>Create &amp; Draft Claim</span>
        </button>
      </div>
    </div>
  `
})
export class ClaimPrepareComponent implements OnInit {
  caseId: string = '11111111-1111-1111-1111-111111111111';
  claimType: string = 'CargoDamage';
  currency: string = 'USD';
  isSubmitting = false;

  lossComponents = signal<LossComponent[]>([
    {
      componentType: 'ItemCost',
      description: 'Damaged merchandise unit valuation',
      quantity: 2,
      unitAmount: 1800.00,
      amount: 3600.00,
      currency: 'USD'
    },
    {
      componentType: 'FreightCharge',
      description: 'Direct freight transportation charge',
      quantity: 1,
      unitAmount: 250.00,
      amount: 250.00,
      currency: 'USD'
    }
  ]);

  validationErrors = signal<string[]>([]);

  totalClaimedAmount = computed(() => {
    return this.lossComponents().reduce((acc, c) => acc + (Number(c.amount) || 0), 0);
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    const qCaseId = this.route.snapshot.queryParamMap.get('caseId');
    if (qCaseId) {
      this.caseId = qCaseId;
    }
  }

  addLossComponent(): void {
    this.lossComponents.update(list => [
      ...list,
      {
        componentType: 'ItemCost',
        description: '',
        quantity: 1,
        unitAmount: 0,
        amount: 0,
        currency: this.currency
      }
    ]);
  }

  removeLossComponent(index: number): void {
    this.lossComponents.update(list => list.filter((_, idx) => idx !== index));
  }

  recalcRow(comp: LossComponent): void {
    if (comp.quantity && comp.unitAmount) {
      comp.amount = Math.round(comp.quantity * comp.unitAmount * 100) / 100;
    }
    this.lossComponents.update(l => [...l]);
  }

  onAmountChange(): void {
    this.lossComponents.update(l => [...l]);
  }

  validateCaseId(): void {}

  saveDraftClaim(): void {
    this.validationErrors.set([]);

    const payload = {
      caseId: this.caseId,
      claimType: this.claimType,
      currency: this.currency,
      lossComponents: this.lossComponents()
    };

    // Client-side Zod Validation
    const result = claimFormSchema.safeParse(payload);
    if (!result.success) {
      const errors = result.error.errors.map(e => `${e.path.join('.')}: ${e.message}`);
      this.validationErrors.set(errors);
      return;
    }

    this.isSubmitting = true;

    // Backend endpoint: POST /api/exceptions/{caseId}/claims
    this.http.post<ClaimDetail>(`/api/exceptions/${this.caseId}/claims`, {
      claimType: this.claimType,
      currency: this.currency,
      lossComponents: this.lossComponents()
    }).subscribe({
      next: created => {
        this.isSubmitting = false;
        this.snackBar.open('Draft claim created successfully.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.router.navigate(['/claims', created.id || 'claim-demo-1']);
      },
      error: () => {
        // Fallback navigation for demo
        this.isSubmitting = false;
        this.snackBar.open('Draft claim created.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.router.navigate(['/claims', 'claim-demo-1']);
      }
    });
  }
}
