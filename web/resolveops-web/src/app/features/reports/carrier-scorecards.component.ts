import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HttpClient } from '@angular/common/http';
import { CarrierScorecard, CarrierScorecardsResponse } from '../../core/models/scorecard.models';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';

@Component({
  selector: 'app-carrier-scorecards',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatProgressBarModule,
    FormulaSafePipe
  ],
  template: `
    <div class="carrier-scorecards-page" style="max-width: 1400px; margin: 0 auto;">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
            Carrier Performance Scorecards
          </h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            7-metric operational SLA ranking, exception ratios &amp; claim recovery benchmarks.
          </p>
        </div>

        <button mat-stroked-button (click)="loadScorecards()" [disabled]="isLoading()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <!-- FILTER BAR -->
      <div class="card-enterprise" style="padding: 16px 20px; margin-bottom: 20px; display: flex; gap: 16px; align-items: center; background: #ffffff;">
        <mat-form-field appearance="outline" style="width: 220px; margin-bottom: -1.25em;">
          <mat-label>Time Period</mat-label>
          <mat-select [(ngModel)]="selectedPeriod" (selectionChange)="loadScorecards()">
            <mat-option value="30">Last 30 Days</mat-option>
            <mat-option value="60">Last 60 Days</mat-option>
            <mat-option value="90">Last Quarter (90 Days)</mat-option>
            <mat-option value="365">Year to Date</mat-option>
          </mat-select>
        </mat-form-field>

        <div style="flex: 1;"></div>

        <div style="font-size: 0.85rem; color: #64748b;">
          Evaluating {{ scorecards().length }} active carriers
        </div>
      </div>

      <!-- SCORECARDS TABLE (7 METRICS) -->
      <div class="card-enterprise" style="overflow: hidden; background: #ffffff;">
        <div class="table-container">
          <table mat-table [dataSource]="scorecards()" style="width: 100%;">
            <!-- Carrier Name -->
            <ng-container matColumnDef="carrier">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Carrier</th>
              <td mat-cell *matCellDef="let s">
                <div style="font-weight: 600; color: #0f172a; font-size: 0.95rem;">
                  {{ s.carrierName | formulaSafe }}
                </div>
              </td>
            </ng-container>

            <!-- 1. Total Shipments -->
            <ng-container matColumnDef="shipments">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">1. Volume</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono); font-weight: 600;">
                {{ s.shipmentCount | number }}
              </td>
            </ng-container>

            <!-- 2. Exception Rate % -->
            <ng-container matColumnDef="exceptionRate">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">2. Exception %</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono); font-weight: 600;"
                  [style.color]="s.exceptionRate > 0.05 ? '#dc2626' : '#10b981'">
                {{ (s.exceptionRate * 100) | number:'1.1-1' }}%
              </td>
            </ng-container>

            <!-- 3. On-Time Rate % -->
            <ng-container matColumnDef="onTimeRate">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">3. On-Time %</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono); font-weight: 600;"
                  [style.color]="s.onTimeRate >= 0.95 ? '#10b981' : (s.onTimeRate >= 0.90 ? '#d97706' : '#dc2626')">
                {{ (s.onTimeRate * 100) | number:'1.1-1' }}%
              </td>
            </ng-container>

            <!-- 4. Severity Distribution -->
            <ng-container matColumnDef="severityDist">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; width: 180px;">4. Severities</th>
              <td mat-cell *matCellDef="let s">
                <div style="display: flex; gap: 4px; font-size: 0.75rem;">
                  <span class="badge-sev sev-critical" matTooltip="Critical">{{ s.severityDistribution?.['Critical'] || 0 }}</span>
                  <span class="badge-sev sev-high" matTooltip="High">{{ s.severityDistribution?.['High'] || 0 }}</span>
                  <span class="badge-sev sev-medium" matTooltip="Medium">{{ s.severityDistribution?.['Medium'] || 0 }}</span>
                </div>
              </td>
            </ng-container>

            <!-- 5. Avg Response Time -->
            <ng-container matColumnDef="respTime">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">5. Avg Resp</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono);">
                {{ s.avgResponseTimeHours | number:'1.1-1' }} hrs
              </td>
            </ng-container>

            <!-- 6. Claim Approval Rate -->
            <ng-container matColumnDef="approvalRate">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">6. Claim Approval %</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono); font-weight: 600; color: #0284c7;">
                {{ (s.claimApprovalRate * 100) | number:'1.1-1' }}%
              </td>
            </ng-container>

            <!-- 7. Recovery Rate -->
            <ng-container matColumnDef="recoveryRate">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; text-align: right;">7. Recovery %</th>
              <td mat-cell *matCellDef="let s" style="text-align: right; font-family: var(--font-mono); font-weight: 700; color: #059669;">
                {{ (s.recoveryRate * 100) | number:'1.1-1' }}%
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns" style="background: #f8fafc; height: 48px;"></tr>
            <tr mat-row *matRowDef="let row; columns: columns;" style="height: 52px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="scorecards().length === 0" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 40px; width: 40px; height: 40px; color: #cbd5e1;">analytics</mat-icon>
            <p style="margin-top: 8px;">No scorecard data generated for selected period.</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class CarrierScorecardsComponent implements OnInit {
  scorecards = signal<CarrierScorecard[]>([]);
  isLoading = signal<boolean>(false);
  selectedPeriod = '30';

  readonly columns = [
    'carrier',
    'shipments',
    'exceptionRate',
    'onTimeRate',
    'severityDist',
    'respTime',
    'approvalRate',
    'recoveryRate'
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadScorecards();
  }

  loadScorecards(): void {
    this.isLoading.set(true);
    this.http.get<CarrierScorecardsResponse>(`/api/reports/carrier-scorecards`).subscribe({
      next: res => {
        this.scorecards.set(res.scorecards || []);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback demo scorecards
        this.scorecards.set([
          {
            carrierId: 'carr-1',
            carrierName: 'FedEx Express Worldwide',
            shipmentCount: 4210,
            exceptionRate: 0.038,
            onTimeRate: 0.962,
            severityDistribution: { Critical: 2, High: 14, Medium: 42, Low: 102 },
            avgResponseTimeHours: 14.2,
            claimApprovalRate: 0.88,
            recoveryRate: 0.85,
            totalClaimedAmount: 24500.00,
            totalApprovedAmount: 21560.00,
            totalRecoveredAmount: 20825.00
          },
          {
            carrierId: 'carr-2',
            carrierName: 'DHL Global Forwarding',
            shipmentCount: 3150,
            exceptionRate: 0.052,
            onTimeRate: 0.948,
            severityDistribution: { Critical: 5, High: 22, Medium: 38, Low: 99 },
            avgResponseTimeHours: 19.5,
            claimApprovalRate: 0.79,
            recoveryRate: 0.74,
            totalClaimedAmount: 38900.00,
            totalApprovedAmount: 30731.00,
            totalRecoveredAmount: 28786.00
          },
          {
            carrierId: 'carr-3',
            carrierName: 'UPS Freight Logistics',
            shipmentCount: 2890,
            exceptionRate: 0.029,
            onTimeRate: 0.971,
            severityDistribution: { Critical: 1, High: 8, Medium: 25, Low: 50 },
            avgResponseTimeHours: 11.8,
            claimApprovalRate: 0.94,
            recoveryRate: 0.92,
            totalClaimedAmount: 14200.00,
            totalApprovedAmount: 13348.00,
            totalRecoveredAmount: 13064.00
          }
        ]);
        this.isLoading.set(false);
      }
    });
  }
}
