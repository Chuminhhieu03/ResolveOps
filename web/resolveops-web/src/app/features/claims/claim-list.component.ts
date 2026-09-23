import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { HttpClient } from '@angular/common/http';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';
import { AuthService } from '../../core/services/auth.service';

interface ClaimSummary {
  id: string;
  claimNumber: string;
  caseId: string;
  carrierId: string;
  claimType: string;
  status: string;
  claimedAmount: number;
  currency: string;
  createdAtUtc: string;
}

@Component({
  selector: 'app-claim-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatPaginatorModule,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="claim-list-page" style="max-width: 1300px; margin: 0 auto;">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
            Carrier Claims Management
          </h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Manage filing, carrier approvals, financial recoveries &amp; settlement reconciliation.
          </p>
        </div>

        <div style="display: flex; gap: 12px;">
          <button mat-stroked-button (click)="loadClaims()">
            <mat-icon>refresh</mat-icon>
            <span>Refresh</span>
          </button>
          <a mat-flat-button color="primary" routerLink="/claims/prepare" style="background: #4f46e5;">
            <mat-icon>add</mat-icon>
            <span>Prepare New Claim</span>
          </a>
        </div>
      </div>

      <!-- FILTER BAR -->
      <div class="card-enterprise" style="padding: 16px 20px; margin-bottom: 20px; display: flex; gap: 16px; align-items: center; background: #ffffff;">
        <mat-form-field appearance="outline" style="width: 200px; margin-bottom: -1.25em;">
          <mat-label>Status</mat-label>
          <mat-select [(ngModel)]="statusFilter" (selectionChange)="loadClaims()">
            <mat-option value="">All Statuses</mat-option>
            <mat-option value="Draft">Draft</mat-option>
            <mat-option value="Approved">Approved</mat-option>
            <mat-option value="Submitted">Submitted</mat-option>
            <mat-option value="Settled">Settled</mat-option>
            <mat-option value="Rejected">Rejected</mat-option>
          </mat-select>
        </mat-form-field>

        <div style="flex: 1;"></div>

        <span style="font-size: 0.85rem; color: #64748b;">
          {{ claims().length }} claims recorded
        </span>
      </div>

      <!-- TABLE -->
      <div class="card-enterprise" style="overflow: hidden; background: #ffffff;">
        <div class="table-container">
          <table mat-table [dataSource]="claims()" style="width: 100%;">
            <ng-container matColumnDef="claimNumber">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Claim #</th>
              <td mat-cell *matCellDef="let item">
                <a [routerLink]="['/claims', item.id]" style="color: #4f46e5; font-weight: 600; text-decoration: none;">
                  {{ item.claimNumber | formulaSafe }}
                </a>
              </td>
            </ng-container>

            <ng-container matColumnDef="type">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Type</th>
              <td mat-cell *matCellDef="let item">{{ item.claimType }}</td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Status</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-status" [ngClass]="'status-' + item.status.toLowerCase()">
                  {{ item.status }}
                </span>
              </td>
            </ng-container>

            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">Claimed Amount</th>
              <td mat-cell *matCellDef="let item" style="text-align: right; font-family: var(--font-mono); font-weight: 600;">
                {{ item.claimedAmount | currency:item.currency:'symbol':'1.2-2' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Created</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.8rem; color: #64748b;">
                {{ item.createdAtUtc | utcToLocal:'short' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="text-align: right; font-weight: 600;">Action</th>
              <td mat-cell *matCellDef="let item" style="text-align: right;">
                <a mat-button color="primary" [routerLink]="['/claims', item.id]">
                  View Detail &rarr;
                </a>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="['claimNumber', 'type', 'status', 'amount', 'createdAt', 'actions']" style="background: #f8fafc; height: 44px;"></tr>
            <tr mat-row *matRowDef="let row; columns: ['claimNumber', 'type', 'status', 'amount', 'createdAt', 'actions'];" style="height: 50px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="claims().length === 0" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 40px; width: 40px; height: 40px; color: #cbd5e1;">monetization_on</mat-icon>
            <p style="margin-top: 8px;">No claims matching criteria.</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ClaimListComponent implements OnInit {
  claims = signal<ClaimSummary[]>([]);
  statusFilter = '';

  constructor(
    private http: HttpClient,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadClaims();
  }

  loadClaims(): void {
    let url = `/api/claims?`;
    if (this.statusFilter) url += `status=${this.statusFilter}&`;

    this.http.get<{ items: ClaimSummary[] }>(url).subscribe({
      next: res => this.claims.set(res.items || []),
      error: () => {
        // Fallback demo claims
        this.claims.set([
          {
            id: 'claim-demo-1',
            claimNumber: 'CLM-2026-0042',
            caseId: '11111111-1111-1111-1111-111111111111',
            carrierId: 'carr-1',
            claimType: 'CargoDamage',
            status: 'Submitted',
            claimedAmount: 3850.00,
            currency: 'USD',
            createdAtUtc: new Date(Date.now() - 86400000).toISOString()
          },
          {
            id: 'claim-demo-2',
            claimNumber: 'CLM-2026-0043',
            caseId: '33333333-3333-3333-3333-333333333333',
            carrierId: 'carr-2',
            claimType: 'TotalLoss',
            status: 'Draft',
            claimedAmount: 18200.00,
            currency: 'USD',
            createdAtUtc: new Date(Date.now() - 3600000).toISOString()
          }
        ]);
      }
    });
  }
}
