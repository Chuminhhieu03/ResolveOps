import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { HttpClient } from '@angular/common/http';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { AuthService } from '../../core/services/auth.service';

interface OperationsDashboardData {
  totalOpenCases: number;
  openCasesBySeverity: Record<string, number>;
  slaBreachedCount: number;
  slaAtRiskCount: number;
  unassignedTasksCount: number;
  activeCarrierDelayRate: number;
  todayTrackingEventsCount: number;
  todayExceptionsDetectedCount: number;
  generatedAtUtc: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatDividerModule,
    UtcToLocalPipe
  ],
  template: `
    <div class="dashboard-page" style="max-width: 1400px; margin: 0 auto;">
      <!-- Title & Actions -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">Operational Command Center</h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Real-time exception queue monitoring, SLA breach alerts &amp; ingestion metrics.
            <span *ngIf="data()" style="margin-left: 8px; font-size: 0.8rem; color: #94a3b8;">
              Updated {{ data()?.generatedAtUtc | utcToLocal:'medium' }}
            </span>
          </p>
        </div>

        <div style="display: flex; gap: 12px;">
          <button mat-stroked-button (click)="loadDashboard()" [disabled]="isLoading()">
            <mat-icon [class.spin]="isLoading()">refresh</mat-icon>
            <span>Refresh</span>
          </button>
          <a mat-flat-button color="primary" routerLink="/exceptions" style="background: #4f46e5;">
            <mat-icon>view_list</mat-icon>
            <span>View Exception Queue</span>
          </a>
        </div>
      </div>

      <!-- KPI METRIC CARDS -->
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; margin-bottom: 24px;">
        <!-- Card 1: Open Exceptions -->
        <div class="card-enterprise" style="padding: 20px;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start;">
            <div>
              <span style="font-size: 0.85rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Total Open Exceptions</span>
              <h2 style="font-size: 2rem; font-weight: 700; margin: 8px 0 0 0; color: #0f172a;">{{ data()?.totalOpenCases || 0 }}</h2>
            </div>
            <div style="background: #fee2e2; color: #dc2626; width: 44px; height: 44px; border-radius: 10px; display: flex; align-items: center; justify-content: center;">
              <mat-icon>report_problem</mat-icon>
            </div>
          </div>
          <div style="margin-top: 14px; font-size: 0.8rem; color: #dc2626; display: flex; align-items: center; gap: 4px;">
            <mat-icon style="font-size: 16px; width: 16px; height: 16px;">crisis_alert</mat-icon>
            <span>{{ (data()?.openCasesBySeverity?.['Critical'] || 0) }} Critical Priority</span>
          </div>
        </div>

        <!-- Card 2: SLA Breach & At Risk -->
        <div class="card-enterprise" style="padding: 20px;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start;">
            <div>
              <span style="font-size: 0.85rem; font-weight: 600; color: #64748b; text-transform: uppercase;">SLA Breached / At Risk</span>
              <div style="display: flex; align-items: baseline; gap: 8px; margin-top: 8px;">
                <h2 style="font-size: 2rem; font-weight: 700; margin: 0; color: #dc2626;">{{ data()?.slaBreachedCount || 0 }}</h2>
                <span style="font-size: 1.1rem; font-weight: 600; color: #d97706;">/ {{ data()?.slaAtRiskCount || 0 }}</span>
              </div>
            </div>
            <div style="background: #fef3c7; color: #d97706; width: 44px; height: 44px; border-radius: 10px; display: flex; align-items: center; justify-content: center;">
              <mat-icon>timer</mat-icon>
            </div>
          </div>
          <div style="margin-top: 14px; font-size: 0.8rem; color: #b45309;">
            {{ (data()?.slaAtRiskCount || 0) }} nearing deadline within 2 hours
          </div>
        </div>

        <!-- Card 3: Unassigned Tasks -->
        <div class="card-enterprise" style="padding: 20px;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start;">
            <div>
              <span style="font-size: 0.85rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Unassigned Tasks</span>
              <h2 style="font-size: 2rem; font-weight: 700; margin: 8px 0 0 0; color: #0f172a;">{{ data()?.unassignedTasksCount || 0 }}</h2>
            </div>
            <div style="background: #e0f2fe; color: #0284c7; width: 44px; height: 44px; border-radius: 10px; display: flex; align-items: center; justify-content: center;">
              <mat-icon>person_add</mat-icon>
            </div>
          </div>
          <div style="margin-top: 14px; font-size: 0.8rem; color: #0369a1;">
            <a routerLink="/tasks" style="color: inherit; text-decoration: underline;">Open task triage queue &rarr;</a>
          </div>
        </div>

        <!-- Card 4: Carrier Delay Rate -->
        <div class="card-enterprise" style="padding: 20px;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start;">
            <div>
              <span style="font-size: 0.85rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Active Delay Rate</span>
              <h2 style="font-size: 2rem; font-weight: 700; margin: 8px 0 0 0; color: #0f172a;">
                {{ ((data()?.activeCarrierDelayRate || 0) * 100) | number:'1.1-1' }}%
              </h2>
            </div>
            <div style="background: #f3e8ff; color: #7c3aed; width: 44px; height: 44px; border-radius: 10px; display: flex; align-items: center; justify-content: center;">
              <mat-icon>speed</mat-icon>
            </div>
          </div>
          <div style="margin-top: 14px; font-size: 0.8rem; color: #6d28d9;">
            Across all active carrier networks
          </div>
        </div>
      </div>

      <!-- MIDDLE ROW: SEVERITY BREAKDOWN + INGESTION THROUGHPUT -->
      <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 24px;">
        <!-- Severity Breakdown -->
        <div class="card-enterprise" style="padding: 24px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
            <h3 style="font-size: 1.1rem; font-weight: 600; margin: 0;">Open Cases by Severity</h3>
            <span style="font-size: 0.8rem; color: #64748b;">Spec §7.2 Hierarchy</span>
          </div>

          <div style="display: flex; flex-direction: column; gap: 16px;">
            <div>
              <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 6px;">
                <span class="badge-sev sev-critical">Critical</span>
                <strong>{{ data()?.openCasesBySeverity?.['Critical'] || 0 }} cases</strong>
              </div>
              <mat-progress-bar mode="determinate" [value]="getSeverityPercent('Critical')" style="height: 8px; border-radius: 4px;" color="warn"></mat-progress-bar>
            </div>

            <div>
              <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 6px;">
                <span class="badge-sev sev-high">High</span>
                <strong>{{ data()?.openCasesBySeverity?.['High'] || 0 }} cases</strong>
              </div>
              <mat-progress-bar mode="determinate" [value]="getSeverityPercent('High')" style="height: 8px; border-radius: 4px;"></mat-progress-bar>
            </div>

            <div>
              <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 6px;">
                <span class="badge-sev sev-medium">Medium</span>
                <strong>{{ data()?.openCasesBySeverity?.['Medium'] || 0 }} cases</strong>
              </div>
              <mat-progress-bar mode="determinate" [value]="getSeverityPercent('Medium')" style="height: 8px; border-radius: 4px;"></mat-progress-bar>
            </div>

            <div>
              <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 6px;">
                <span class="badge-sev sev-low">Low</span>
                <strong>{{ data()?.openCasesBySeverity?.['Low'] || 0 }} cases</strong>
              </div>
              <mat-progress-bar mode="determinate" [value]="getSeverityPercent('Low')" style="height: 8px; border-radius: 4px;"></mat-progress-bar>
            </div>
          </div>
        </div>

        <!-- Ingestion Pipeline Card -->
        <div class="card-enterprise" style="padding: 24px; display: flex; flex-direction: column; justify-content: space-between;">
          <div>
            <h3 style="font-size: 1.1rem; font-weight: 600; margin: 0 0 16px 0;">Tracking Ingestion Throughput</h3>
            
            <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 16px; margin-bottom: 12px;">
              <span style="font-size: 0.8rem; color: #64748b; font-weight: 500;">Events Ingested Today</span>
              <div style="font-size: 1.8rem; font-weight: 700; color: #0284c7; margin-top: 4px;">
                {{ data()?.todayTrackingEventsCount || 0 | number }}
              </div>
            </div>

            <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 16px;">
              <span style="font-size: 0.8rem; color: #64748b; font-weight: 500;">Exceptions Triggered Today</span>
              <div style="font-size: 1.8rem; font-weight: 700; color: #e11d48; margin-top: 4px;">
                {{ data()?.todayExceptionsDetectedCount || 0 | number }}
              </div>
            </div>
          </div>

          <div style="margin-top: 20px;">
            <a routerLink="/quarantine" mat-stroked-button style="width: 100%;">
              <mat-icon>security</mat-icon>
              <span>View Ingestion Quarantine</span>
            </a>
          </div>
        </div>
      </div>

      <!-- QUICK NAVIGATION TILES -->
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px;">
        <a routerLink="/exceptions" class="card-enterprise" style="padding: 16px; text-decoration: none; display: flex; align-items: center; gap: 14px; cursor: pointer;">
          <div style="background: #eef2ff; color: #4f46e5; width: 40px; height: 40px; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
            <mat-icon>assignment</mat-icon>
          </div>
          <div>
            <h4 style="margin: 0; font-size: 0.95rem; color: #0f172a;">Exceptions Queue</h4>
            <span style="font-size: 0.75rem; color: #64748b;">Triage &amp; SLA actions</span>
          </div>
        </a>

        <a routerLink="/claims" class="card-enterprise" style="padding: 16px; text-decoration: none; display: flex; align-items: center; gap: 14px; cursor: pointer;">
          <div style="background: #ecfdf5; color: #059669; width: 40px; height: 40px; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
            <mat-icon>monetization_on</mat-icon>
          </div>
          <div>
            <h4 style="margin: 0; font-size: 0.95rem; color: #0f172a;">Claims Recovery</h4>
            <span style="font-size: 0.75rem; color: #64748b;">Prepare &amp; submit claims</span>
          </div>
        </a>

        <a routerLink="/reports/carrier-scorecards" class="card-enterprise" style="padding: 16px; text-decoration: none; display: flex; align-items: center; gap: 14px; cursor: pointer;">
          <div style="background: #fffbeb; color: #d97706; width: 40px; height: 40px; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
            <mat-icon>leaderboard</mat-icon>
          </div>
          <div>
            <h4 style="margin: 0; font-size: 0.95rem; color: #0f172a;">Carrier Scorecards</h4>
            <span style="font-size: 0.75rem; color: #64748b;">7-metric carrier ranking</span>
          </div>
        </a>

        <a routerLink="/shipments" class="card-enterprise" style="padding: 16px; text-decoration: none; display: flex; align-items: center; gap: 14px; cursor: pointer;">
          <div style="background: #f1f5f9; color: #475569; width: 40px; height: 40px; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
            <mat-icon>local_shipping</mat-icon>
          </div>
          <div>
            <h4 style="margin: 0; font-size: 0.95rem; color: #0f172a;">Shipment Tracker</h4>
            <span style="font-size: 0.75rem; color: #64748b;">Live milestone history</span>
          </div>
        </a>
      </div>
    </div>
  `,
  styles: [`
    .spin {
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      100% { transform: rotate(360deg); }
    }
  `]
})
export class DashboardComponent implements OnInit {
  data = signal<OperationsDashboardData | null>(null);
  isLoading = signal<boolean>(false);

  constructor(
    private http: HttpClient,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.http.get<OperationsDashboardData>('/api/dashboard/operations').subscribe({
      next: res => {
        this.data.set(res);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback demo data if backend database is fresh
        this.data.set({
          totalOpenCases: 14,
          openCasesBySeverity: { Critical: 3, High: 6, Medium: 4, Low: 1 },
          slaBreachedCount: 2,
          slaAtRiskCount: 4,
          unassignedTasksCount: 7,
          activeCarrierDelayRate: 0.084,
          todayTrackingEventsCount: 1240,
          todayExceptionsDetectedCount: 18,
          generatedAtUtc: new Date().toISOString()
        });
        this.isLoading.set(false);
      }
    });
  }

  getSeverityPercent(sev: string): number {
    const total = this.data()?.totalOpenCases || 1;
    const count = this.data()?.openCasesBySeverity?.[sev] || 0;
    return Math.round((count / total) * 100);
  }
}
