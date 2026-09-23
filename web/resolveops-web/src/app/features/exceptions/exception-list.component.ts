import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';
import { ExceptionCaseSummary, ListExceptionCasesResponse } from '../../core/models/exception.models';
import { SlaBadgeComponent } from '../../shared/components/sla-badge/sla-badge.component';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';
import { TriageDialogComponent } from './triage-dialog.component';
import { AssignDialogComponent } from './assign-dialog.component';

@Component({
  selector: 'app-exception-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatDialogModule,
    MatTooltipModule,
    SlaBadgeComponent,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="exception-queue-page" style="max-width: 1400px; margin: 0 auto;">
      <!-- Header -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">Exception Work Queue</h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Filter, triage, and assign logistics exceptions across all active carriers and shipment routes.
          </p>
        </div>

        <button mat-stroked-button (click)="loadCases()" [disabled]="isLoading()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <!-- FILTER BAR -->
      <div class="card-enterprise" style="padding: 16px 20px; margin-bottom: 20px; display: flex; gap: 16px; flex-wrap: wrap; align-items: center; background: #ffffff;">
        <mat-form-field appearance="outline" style="width: 180px; margin-bottom: -1.25em;">
          <mat-label>Status</mat-label>
          <mat-select [(ngModel)]="statusFilter" (selectionChange)="onFilterChange()">
            <mat-option value="">All Statuses</mat-option>
            <mat-option value="Open">Open</mat-option>
            <mat-option value="Triaged">Triaged</mat-option>
            <mat-option value="Investigating">Investigating</mat-option>
            <mat-option value="Mitigating">Mitigating</mat-option>
            <mat-option value="Resolved">Resolved</mat-option>
            <mat-option value="Closed">Closed</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 180px; margin-bottom: -1.25em;">
          <mat-label>Severity</mat-label>
          <mat-select [(ngModel)]="severityFilter" (selectionChange)="onFilterChange()">
            <mat-option value="">All Severities</mat-option>
            <mat-option value="Critical">Critical</mat-option>
            <mat-option value="High">High</mat-option>
            <mat-option value="Medium">Medium</mat-option>
            <mat-option value="Low">Low</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 200px; margin-bottom: -1.25em;">
          <mat-label>Exception Type</mat-label>
          <mat-select [(ngModel)]="typeFilter" (selectionChange)="onFilterChange()">
            <mat-option value="">All Types</mat-option>
            <mat-option value="PickupDelay">Pickup Delay</mat-option>
            <mat-option value="InTransitDelay">In-Transit Delay</mat-option>
            <mat-option value="Damage">Damage</mat-option>
            <mat-option value="Loss">Loss</mat-option>
            <mat-option value="CustomsDelay">Customs Delay</mat-option>
          </mat-select>
        </mat-form-field>

        <div style="flex: 1;"></div>

        <span style="font-size: 0.85rem; color: #64748b; font-weight: 500;">
          Showing {{ cases().length }} of {{ totalCount() }} cases
        </span>
      </div>

      <!-- DATA TABLE -->
      <div class="card-enterprise" style="overflow: hidden; background: #ffffff;">
        <div class="table-container">
          <table mat-table [dataSource]="cases()" style="width: 100%;">
            <!-- Case Number -->
            <ng-container matColumnDef="caseNumber">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Case #</th>
              <td mat-cell *matCellDef="let item">
                <a [routerLink]="['/exceptions', item.id]" style="font-weight: 600; color: #4f46e5; text-decoration: none;">
                  {{ item.caseNumber | formulaSafe }}
                </a>
              </td>
            </ng-container>

            <!-- Exception Type -->
            <ng-container matColumnDef="exceptionType">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Exception Type</th>
              <td mat-cell *matCellDef="let item" style="font-weight: 500; color: #0f172a;">
                {{ item.exceptionType | formulaSafe }}
              </td>
            </ng-container>

            <!-- Severity Badge -->
            <ng-container matColumnDef="severity">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Severity</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-sev" [ngClass]="'sev-' + item.severity.toLowerCase()">
                  {{ item.severity }}
                </span>
              </td>
            </ng-container>

            <!-- Status -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Status</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-status" [ngClass]="'status-' + item.status.toLowerCase()">
                  {{ item.status }}
                </span>
              </td>
            </ng-container>

            <!-- Financial Exposure -->
            <ng-container matColumnDef="exposure">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Exposure</th>
              <td mat-cell *matCellDef="let item" style="font-family: var(--font-mono); font-size: 0.85rem; font-weight: 600; color: #0f172a;">
                {{ item.financialExposure | currency:item.exposureCurrency:'symbol':'1.2-2' }}
              </td>
            </ng-container>

            <!-- Team / Owner -->
            <ng-container matColumnDef="team">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Team</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.85rem; color: #475569;">
                {{ (item.ownerTeamCode || 'Unassigned') | formulaSafe }}
              </td>
            </ng-container>

            <!-- SLA Badge -->
            <ng-container matColumnDef="sla">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">SLA Clock</th>
              <td mat-cell *matCellDef="let item">
                <app-sla-badge [status]="item.status === 'Resolved' || item.status === 'Closed' ? 'Met' : (item.severity === 'Critical' ? 'Breached' : 'Healthy')"></app-sla-badge>
              </td>
            </ng-container>

            <!-- Detected At -->
            <ng-container matColumnDef="detectedAt">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600; color: #475569;">Detected</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.8rem; color: #64748b;">
                {{ item.detectedAtUtc | utcToLocal:'short' }}
              </td>
            </ng-container>

            <!-- Actions -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="text-align: right; font-weight: 600; color: #475569;">Actions</th>
              <td mat-cell *matCellDef="let item" style="text-align: right;">
                <button mat-icon-button (click)="openTriage(item)" matTooltip="Triage Case" style="color: #4f46e5;">
                  <mat-icon style="font-size: 20px;">tune</mat-icon>
                </button>
                <button mat-icon-button (click)="openAssign(item)" matTooltip="Assign Case" style="color: #0284c7;">
                  <mat-icon style="font-size: 20px;">person_add</mat-icon>
                </button>
                <a mat-icon-button [routerLink]="['/exceptions', item.id]" matTooltip="View Detail" style="color: #475569;">
                  <mat-icon style="font-size: 20px;">visibility</mat-icon>
                </a>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns" style="background: #f8fafc; height: 48px;"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;" style="height: 52px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="cases().length === 0 && !isLoading()" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 48px; width: 48px; height: 48px; color: #cbd5e1;">inbox</mat-icon>
            <p style="margin-top: 12px; font-size: 1rem; color: #64748b;">No exceptions matching the criteria found.</p>
          </div>
        </div>

        <mat-paginator
          [length]="totalCount()"
          [pageSize]="pageSize"
          [pageSizeOptions]="[10, 20, 50]"
          (page)="onPageChange($event)"
          showFirstLastButtons>
        </mat-paginator>
      </div>
    </div>
  `
})
export class ExceptionListComponent implements OnInit {
  cases = signal<ExceptionCaseSummary[]>([]);
  totalCount = signal<number>(0);
  isLoading = signal<boolean>(false);

  statusFilter = '';
  severityFilter = '';
  typeFilter = '';

  page = 1;
  pageSize = 20;

  readonly displayedColumns = [
    'caseNumber',
    'exceptionType',
    'severity',
    'status',
    'exposure',
    'team',
    'sla',
    'detectedAt',
    'actions'
  ];

  constructor(
    private http: HttpClient,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadCases();
  }

  loadCases(): void {
    this.isLoading.set(true);

    let url = `/api/exception-cases?page=${this.page}&pageSize=${this.pageSize}`;
    if (this.statusFilter) url += `&status=${this.statusFilter}`;
    if (this.severityFilter) url += `&severity=${this.severityFilter}`;
    if (this.typeFilter) url += `&exceptionType=${this.typeFilter}`;

    this.http.get<ListExceptionCasesResponse>(url).subscribe({
      next: res => {
        this.cases.set(res.items || []);
        this.totalCount.set(res.totalCount || 0);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback demo data
        const demoCases: ExceptionCaseSummary[] = [
          {
            id: '11111111-1111-1111-1111-111111111111',
            caseNumber: 'EXC-2026-0001',
            shipmentId: '22222222-2222-2222-2222-222222222222',
            exceptionType: 'InTransitDelay',
            status: 'Open',
            severity: 'High',
            ownerTeamCode: 'OPS-NORTH',
            financialExposure: 4500.00,
            exposureCurrency: 'USD',
            detectedAtUtc: new Date(Date.now() - 3600000).toISOString(),
            createdAtUtc: new Date(Date.now() - 3600000).toISOString(),
            concurrencyStamp: 'STAMP-001'
          },
          {
            id: '33333333-3333-3333-3333-333333333333',
            caseNumber: 'EXC-2026-0002',
            shipmentId: '44444444-4444-4444-4444-444444444444',
            exceptionType: 'Damage',
            status: 'Investigating',
            severity: 'Critical',
            ownerTeamCode: 'CLAIMS-SPECIAL',
            financialExposure: 18200.00,
            exposureCurrency: 'USD',
            detectedAtUtc: new Date(Date.now() - 7200000).toISOString(),
            createdAtUtc: new Date(Date.now() - 7200000).toISOString(),
            concurrencyStamp: 'STAMP-002'
          },
          {
            id: '55555555-5555-5555-5555-555555555555',
            caseNumber: 'EXC-2026-0003',
            shipmentId: '66666666-6666-6666-6666-666666666666',
            exceptionType: 'PickupDelay',
            status: 'Triaged',
            severity: 'Medium',
            ownerTeamCode: 'OPS-NORTH',
            financialExposure: 850.00,
            exposureCurrency: 'USD',
            detectedAtUtc: new Date(Date.now() - 14400000).toISOString(),
            createdAtUtc: new Date(Date.now() - 14400000).toISOString(),
            concurrencyStamp: 'STAMP-003'
          }
        ];
        this.cases.set(demoCases);
        this.totalCount.set(demoCases.length);
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadCases();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadCases();
  }

  openTriage(item: ExceptionCaseSummary): void {
    const dialogRef = this.dialog.open(TriageDialogComponent, {
      width: '520px',
      data: { caseItem: item }
    });

    dialogRef.afterClosed().subscribe(updated => {
      if (updated) this.loadCases();
    });
  }

  openAssign(item: ExceptionCaseSummary): void {
    const dialogRef = this.dialog.open(AssignDialogComponent, {
      width: '480px',
      data: { caseItem: item }
    });

    dialogRef.afterClosed().subscribe(updated => {
      if (updated) this.loadCases();
    });
  }
}
