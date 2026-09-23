import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { HttpClient } from '@angular/common/http';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';

interface ShipmentSummary {
  id: string;
  externalReference: string;
  sourceSystem: string;
  status: string;
  plannedPickupAtUtc: string;
  plannedDeliveryAtUtc: string;
  actualDeliveryAtUtc?: string;
  legCount: number;
  createdAtUtc: string;
}

@Component({
  selector: 'app-shipment-list',
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
    <div class="shipment-list-page" style="max-width: 1300px; margin: 0 auto;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
            Shipment Manifest &amp; Tracking
          </h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Active freight and parcel shipments onboarded for automated exception monitoring.
          </p>
        </div>

        <button mat-stroked-button (click)="loadShipments()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <div class="card-enterprise" style="overflow: hidden; background: #ffffff;">
        <div class="table-container">
          <table mat-table [dataSource]="shipments()" style="width: 100%;">
            <ng-container matColumnDef="ref">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Reference</th>
              <td mat-cell *matCellDef="let s">
                <a [routerLink]="['/shipments', s.id]" style="font-weight: 600; color: #4f46e5; text-decoration: none;">
                  {{ s.externalReference | formulaSafe }}
                </a>
              </td>
            </ng-container>

            <ng-container matColumnDef="source">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Source</th>
              <td mat-cell *matCellDef="let s">{{ s.sourceSystem }}</td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Status</th>
              <td mat-cell *matCellDef="let s">
                <span class="badge-status" [ngClass]="'status-' + s.status.toLowerCase()">
                  {{ s.status }}
                </span>
              </td>
            </ng-container>

            <ng-container matColumnDef="legs">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Legs</th>
              <td mat-cell *matCellDef="let s">{{ s.legCount || 1 }} legs</td>
            </ng-container>

            <ng-container matColumnDef="plannedDelivery">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Expected Delivery</th>
              <td mat-cell *matCellDef="let s" style="font-size: 0.85rem; color: #475569;">
                {{ s.plannedDeliveryAtUtc | utcToLocal:'medium' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="text-align: right; font-weight: 600;">Action</th>
              <td mat-cell *matCellDef="let s" style="text-align: right;">
                <a mat-button color="primary" [routerLink]="['/shipments', s.id]">
                  Milestones &rarr;
                </a>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="['ref', 'source', 'status', 'legs', 'plannedDelivery', 'actions']" style="background: #f8fafc; height: 44px;"></tr>
            <tr mat-row *matRowDef="let row; columns: ['ref', 'source', 'status', 'legs', 'plannedDelivery', 'actions'];" style="height: 50px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="shipments().length === 0" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 40px; width: 40px; height: 40px; color: #cbd5e1;">local_shipping</mat-icon>
            <p style="margin-top: 8px;">No shipments loaded.</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ShipmentListComponent implements OnInit {
  shipments = signal<ShipmentSummary[]>([]);

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadShipments();
  }

  loadShipments(): void {
    this.http.get<{ items: ShipmentSummary[] }>('/api/shipments?page=1&pageSize=20').subscribe({
      next: res => this.shipments.set(res.items || []),
      error: () => {
        // Fallback demo shipments
        this.shipments.set([
          {
            id: '22222222-2222-2222-2222-222222222222',
            externalReference: 'SHP-ORD-90214',
            sourceSystem: 'SAP-ERP',
            status: 'InTransit',
            plannedPickupAtUtc: new Date(Date.now() - 86400000).toISOString(),
            plannedDeliveryAtUtc: new Date(Date.now() + 86400000).toISOString(),
            legCount: 1,
            createdAtUtc: new Date(Date.now() - 90000000).toISOString()
          },
          {
            id: '44444444-4444-4444-4444-444444444444',
            externalReference: 'SHP-ORD-90215',
            sourceSystem: 'Shopify-Plus',
            status: 'Delivered',
            plannedPickupAtUtc: new Date(Date.now() - 172800000).toISOString(),
            plannedDeliveryAtUtc: new Date(Date.now() - 86400000).toISOString(),
            legCount: 2,
            createdAtUtc: new Date(Date.now() - 180000000).toISOString()
          }
        ]);
      }
    });
  }
}
