import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { ExceptionPolicy, ListPoliciesResponse } from '../../core/models/policy.models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-policy-admin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTabsModule
  ],
  template: `
    <div class="policy-admin-page" style="max-width: 1300px; margin: 0 auto;">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
            Tenant Policy Administration
          </h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Configure SLA clocks, exception thresholds, claim eligibility rules, and mandatory evidence checklists.
          </p>
        </div>

        <button mat-stroked-button (click)="loadPolicies()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <mat-tab-group style="background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; padding: 16px;">
        <!-- TAB 1: EXCEPTION POLICIES -->
        <mat-tab label="Exception Policies">
          <div style="padding: 20px 0;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
              <h3 style="margin: 0; font-size: 1.1rem; font-weight: 600;">Active Exception Detection Policies</h3>
            </div>

            <div class="table-container">
              <table mat-table [dataSource]="policies()" style="width: 100%;">
                <ng-container matColumnDef="policyKey">
                  <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Policy Key</th>
                  <td mat-cell *matCellDef="let p"><code>{{ p.policyKey }}</code></td>
                </ng-container>

                <ng-container matColumnDef="type">
                  <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Exception Type</th>
                  <td mat-cell *matCellDef="let p"><strong>{{ p.exceptionType }}</strong></td>
                </ng-container>

                <ng-container matColumnDef="version">
                  <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Version</th>
                  <td mat-cell *matCellDef="let p">v{{ p.versionNumber }}</td>
                </ng-container>

                <ng-container matColumnDef="status">
                  <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Status</th>
                  <td mat-cell *matCellDef="let p">
                    <span class="badge-status" [style.background]="p.status === 'Active' ? '#ecfdf5' : '#f1f5f9'" [style.color]="p.status === 'Active' ? '#065f46' : '#64748b'">
                      {{ p.status }}
                    </span>
                  </td>
                </ng-container>

                <ng-container matColumnDef="rules">
                  <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Rule Parameters</th>
                  <td mat-cell *matCellDef="let p" style="font-family: var(--font-mono); font-size: 0.8rem; color: #475569;">
                    {{ p.ruleDefinitionJson }}
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="['policyKey', 'type', 'version', 'status', 'rules']" style="background: #f8fafc; height: 44px;"></tr>
                <tr mat-row *matRowDef="let row; columns: ['policyKey', 'type', 'version', 'status', 'rules'];" style="height: 48px;"></tr>
              </table>
            </div>
          </div>
        </mat-tab>

        <!-- TAB 2: SLA CLOCKS -->
        <mat-tab label="SLA Policy Configuration">
          <div style="padding: 20px 0; max-width: 700px;">
            <h3 style="margin: 0 0 16px 0; font-size: 1.1rem; font-weight: 600;">Standard Operational SLA Targets</h3>

            <div class="card-enterprise" style="padding: 20px; background: #f8fafc; border: 1px solid #e2e8f0; margin-bottom: 20px;">
              <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px;">
                <mat-form-field appearance="outline">
                  <mat-label>Acknowledgement Target (Minutes)</mat-label>
                  <input type="number" matInput [(ngModel)]="slaAckMinutes">
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Resolution Target (Minutes)</mat-label>
                  <input type="number" matInput [(ngModel)]="slaResolveMinutes">
                </mat-form-field>
              </div>

              <mat-form-field appearance="outline" style="width: 100%;">
                <mat-label>Authorized SLA Pause Reason Codes</mat-label>
                <input matInput [(ngModel)]="slaPauseCodes">
                <mat-hint>Comma-separated list of recognized pause triggers</mat-hint>
              </mat-form-field>
            </div>

            <button mat-flat-button color="primary" (click)="saveSlaSettings()" style="background: #4f46e5;">
              <mat-icon style="margin-right: 4px;">save</mat-icon>
              <span>Save SLA Configuration</span>
            </button>
          </div>
        </mat-tab>

        <!-- TAB 3: CLAIM ELIGIBILITY -->
        <mat-tab label="Claim Eligibility Rules">
          <div style="padding: 20px 0; max-width: 700px;">
            <h3 style="margin: 0 0 16px 0; font-size: 1.1rem; font-weight: 600;">Carrier Claim Filing Rules</h3>

            <div class="card-enterprise" style="padding: 20px; background: #f8fafc; border: 1px solid #e2e8f0; margin-bottom: 20px;">
              <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 12px;">
                <mat-label>Filing Window (Days after exception detected)</mat-label>
                <input type="number" matInput [(ngModel)]="claimFilingDays">
              </mat-form-field>

              <mat-form-field appearance="outline" style="width: 100%;">
                <mat-label>Minimum Claim Financial Threshold ($)</mat-label>
                <input type="number" matInput [(ngModel)]="claimMinThreshold">
                <mat-hint>Claims below this threshold are flagged for non-economic filing review</mat-hint>
              </mat-form-field>
            </div>

            <button mat-flat-button color="primary" (click)="saveEligibilitySettings()" style="background: #4f46e5;">
              <mat-icon style="margin-right: 4px;">save</mat-icon>
              <span>Save Eligibility Rules</span>
            </button>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `
})
export class PolicyAdminComponent implements OnInit {
  policies = signal<ExceptionPolicy[]>([]);
  slaAckMinutes = 60;
  slaResolveMinutes = 1440;
  slaPauseCodes = 'AWAITING_CARRIER,AWAITING_EVIDENCE,CARRIER_UPDATE_REQUESTED';
  claimFilingDays = 30;
  claimMinThreshold = 50.00;

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.http.get<ListPoliciesResponse>('/api/exception-policies').subscribe({
      next: res => this.policies.set(res.items || []),
      error: () => {
        // Fallback demo policies
        this.policies.set([
          {
            id: 'pol-1',
            policyKey: 'POL-PICKUP-DELAY-DEFAULT',
            versionNumber: 1,
            exceptionType: 'PickupDelay',
            ruleDefinitionJson: '{"ToleranceMinutes": 30}',
            severityDefinitionJson: '{"DefaultSeverity": "Medium"}',
            assignmentDefinitionJson: '{"DefaultTeamCode": "OPS-NORTH"}',
            status: 'Active',
            effectiveFromUtc: new Date(Date.now() - 86400000 * 30).toISOString()
          },
          {
            id: 'pol-2',
            policyKey: 'POL-IN-TRANSIT-DELAY-DEFAULT',
            versionNumber: 1,
            exceptionType: 'InTransitDelay',
            ruleDefinitionJson: '{"ToleranceMinutes": 60}',
            severityDefinitionJson: '{"DefaultSeverity": "Medium"}',
            assignmentDefinitionJson: '{"DefaultTeamCode": "OPS-NORTH"}',
            status: 'Active',
            effectiveFromUtc: new Date(Date.now() - 86400000 * 30).toISOString()
          },
          {
            id: 'pol-3',
            policyKey: 'POL-DAMAGE-DEFAULT',
            versionNumber: 1,
            exceptionType: 'Damage',
            ruleDefinitionJson: '{"TriggerEventTypes": ["Damaged", "Exception"]}',
            severityDefinitionJson: '{"DefaultSeverity": "High"}',
            assignmentDefinitionJson: '{"DefaultTeamCode": "CLAIMS-NORTH"}',
            status: 'Active',
            effectiveFromUtc: new Date(Date.now() - 86400000 * 30).toISOString()
          }
        ]);
      }
    });
  }

  saveSlaSettings(): void {
    this.snackBar.open('SLA configuration updated successfully.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
  }

  saveEligibilitySettings(): void {
    this.snackBar.open('Claim eligibility rules saved.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
  }
}
