import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDividerModule } from '@angular/material/divider';
import { HttpClient } from '@angular/common/http';
import { ShipmentDetail } from '../../core/models/shipment.models';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';

@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatDividerModule,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="shipment-detail-page" style="max-width: 1200px; margin: 0 auto;" *ngIf="shipment(); else loadingTpl">
      <!-- HEADER -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div style="display: flex; align-items: center; gap: 12px;">
          <a mat-icon-button routerLink="/exceptions" style="color: #64748b;">
            <mat-icon>arrow_back</mat-icon>
          </a>
          <div>
            <div style="display: flex; align-items: center; gap: 10px;">
              <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">
                Shipment: {{ shipment()?.externalReference | formulaSafe }}
              </h1>
              <span class="badge-status" [ngClass]="'status-' + (shipment()?.status || 'open').toLowerCase()">
                {{ shipment()?.status }}
              </span>
            </div>
            <p style="margin: 4px 0 0 0; font-size: 0.85rem; color: #64748b;">
              Source: <strong>{{ shipment()?.sourceSystem }}</strong> • Service Level: <strong>{{ shipment()?.serviceLevel || 'Standard' }}</strong>
            </p>
          </div>
        </div>

        <button mat-stroked-button (click)="loadShipment()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <!-- VISUAL MILESTONE PROGRESSION TRACKER -->
      <div class="card-enterprise" style="padding: 24px; margin-bottom: 24px; background: #ffffff;">
        <h3 style="font-size: 1rem; font-weight: 600; margin: 0 0 20px 0; color: #0f172a;">Milestone Progression Tracker</h3>
        
        <div style="display: flex; align-items: center; justify-content: space-between; position: relative;">
          <!-- Progress Line Background -->
          <div style="position: absolute; top: 20px; left: 40px; right: 40px; height: 3px; background: #e2e8f0; z-index: 1;"></div>
          <!-- Progress Line Filled -->
          <div style="position: absolute; top: 20px; left: 40px; height: 3px; background: #4f46e5; z-index: 2;" [style.width]="getMilestoneProgressWidth()"></div>

          <!-- Milestones -->
          <div *ngFor="let m of milestones; let idx = index" style="position: relative; z-index: 3; display: flex; flex-direction: column; align-items: center; width: 100px;">
            <div style="width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 18px; margin-bottom: 8px; transition: all 0.2s;"
                 [style.background]="isMilestonePassed(idx) ? '#4f46e5' : (isMilestoneCurrent(idx) ? '#6366f1' : '#ffffff')"
                 [style.color]="isMilestonePassed(idx) || isMilestoneCurrent(idx) ? '#ffffff' : '#94a3b8'"
                 [style.border]="isMilestonePassed(idx) || isMilestoneCurrent(idx) ? '3px solid #c7d2fe' : '2px solid #cbd5e1'">
              <mat-icon style="font-size: 20px; width: 20px; height: 20px;">{{ m.icon }}</mat-icon>
            </div>
            <span style="font-size: 0.8rem; font-weight: 600;" [style.color]="isMilestonePassed(idx) || isMilestoneCurrent(idx) ? '#0f172a' : '#94a3b8'">
              {{ m.label }}
            </span>
          </div>
        </div>
      </div>

      <!-- SHIPMENT INFORMATION & LEGS -->
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
        <!-- Card 1: Route & Dates -->
        <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">Route &amp; Schedule</h3>
          
          <div style="display: flex; flex-direction: column; gap: 12px; font-size: 0.85rem;">
            <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
              <span style="color: #64748b;">Planned Pickup:</span>
              <strong>{{ shipment()?.plannedPickupAtUtc | utcToLocal:'medium' }}</strong>
            </div>

            <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
              <span style="color: #64748b;">Actual Pickup:</span>
              <span>{{ shipment()?.actualPickupAtUtc ? (shipment()?.actualPickupAtUtc | utcToLocal:'medium') : 'Pending departure' }}</span>
            </div>

            <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
              <span style="color: #64748b;">Planned Delivery:</span>
              <strong>{{ shipment()?.plannedDeliveryAtUtc | utcToLocal:'medium' }}</strong>
            </div>

            <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px;">
              <span style="color: #64748b;">Declared Value:</span>
              <strong style="color: #059669; font-family: var(--font-mono);">
                {{ shipment()?.declaredValue ? (shipment()?.declaredValue | currency:(shipment()?.declaredValueCurrency || 'USD')) : 'N/A' }}
              </strong>
            </div>

            <div style="display: flex; justify-content: space-between;">
              <span style="color: #64748b;">Packages / Weight:</span>
              <span>{{ shipment()?.expectedPackageCount || 1 }} pkgs • {{ shipment()?.expectedWeight || 0 }} {{ shipment()?.weightUnit || 'kg' }}</span>
            </div>
          </div>
        </div>

        <!-- Card 2: Carrier Legs -->
        <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">Carrier Transportation Legs</h3>

          <div *ngFor="let leg of shipment()?.legs" style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; margin-bottom: 10px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
              <strong style="font-size: 0.85rem; color: #0f172a;">Leg #{{ leg.sequenceNumber }}: {{ leg.trackingNumber || 'Unassigned tracking' }}</strong>
              <span class="badge-status" [ngClass]="'status-' + leg.status.toLowerCase()">{{ leg.status }}</span>
            </div>
            <div style="font-size: 0.8rem; color: #64748b;">
              Dep: {{ leg.plannedDepartureAtUtc | utcToLocal:'short' }} &rarr; Arr: {{ leg.plannedArrivalAtUtc | utcToLocal:'short' }}
            </div>
          </div>

          <div *ngIf="!shipment()?.legs?.length" style="color: #94a3b8; font-size: 0.85rem; text-align: center; padding: 20px;">
            No intermediate legs defined.
          </div>
        </div>
      </div>

      <!-- SHIPMENT ITEMS TABLE -->
      <div class="card-enterprise" style="padding: 24px; background: #ffffff;">
        <h3 style="font-size: 1.05rem; font-weight: 600; margin: 0 0 16px 0; color: #0f172a;">Manifest Line Items</h3>
        
        <div class="table-container">
          <table mat-table [dataSource]="shipment()?.items || []" style="width: 100%;">
            <ng-container matColumnDef="line">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Line</th>
              <td mat-cell *matCellDef="let item">{{ item.lineReference | formulaSafe }}</td>
            </ng-container>

            <ng-container matColumnDef="sku">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">SKU</th>
              <td mat-cell *matCellDef="let item"><code>{{ item.sku | formulaSafe }}</code></td>
            </ng-container>

            <ng-container matColumnDef="description">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Description</th>
              <td mat-cell *matCellDef="let item">{{ item.description | formulaSafe }}</td>
            </ng-container>

            <ng-container matColumnDef="quantity">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Quantity</th>
              <td mat-cell *matCellDef="let item">{{ item.expectedQuantity }} {{ item.quantityUnit }}</td>
            </ng-container>

            <ng-container matColumnDef="unitValue">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Unit Value</th>
              <td mat-cell *matCellDef="let item">{{ item.unitValue | currency:(item.currency || 'USD') }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="['line', 'sku', 'description', 'quantity', 'unitValue']" style="background: #f8fafc; height: 44px;"></tr>
            <tr mat-row *matRowDef="let row; columns: ['line', 'sku', 'description', 'quantity', 'unitValue'];" style="height: 48px;"></tr>
          </table>
        </div>
      </div>
    </div>

    <ng-template #loadingTpl>
      <div style="text-align: center; padding: 80px 0; color: #64748b;">
        <mat-icon style="font-size: 40px; width: 40px; height: 40px; animation: spin 1s linear infinite;">refresh</mat-icon>
        <p style="margin-top: 12px;">Loading shipment milestones...</p>
      </div>
    </ng-template>
  `,
  styles: [`
    @keyframes spin { 100% { transform: rotate(360deg); } }
  `]
})
export class ShipmentDetailComponent implements OnInit {
  shipmentId: string = '';
  shipment = signal<ShipmentDetail | null>(null);

  readonly milestones = [
    { label: 'Accepted', icon: 'inventory_2' },
    { label: 'In Transit', icon: 'local_shipping' },
    { label: 'Out for Delivery', icon: 'local_post_office' },
    { label: 'Delivered', icon: 'task_alt' }
  ];

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.shipmentId = this.route.snapshot.paramMap.get('id') || '';
    if (this.shipmentId) {
      this.loadShipment();
    }
  }

  loadShipment(): void {
    this.http.get<ShipmentDetail>(`/api/shipments/${this.shipmentId}`).subscribe({
      next: res => this.shipment.set(res),
      error: () => {
        // Fallback demo shipment
        this.shipment.set({
          id: this.shipmentId,
          externalReference: 'SHP-ORD-90214',
          sourceSystem: 'SAP-ERP',
          customerId: 'cust-1',
          originLocationId: 'loc-1',
          destinationLocationId: 'loc-2',
          status: 'InTransit',
          serviceLevel: 'ExpressPriority',
          plannedPickupAtUtc: new Date(Date.now() - 86400000).toISOString(),
          plannedDeliveryAtUtc: new Date(Date.now() + 86400000).toISOString(),
          actualPickupAtUtc: new Date(Date.now() - 80000000).toISOString(),
          declaredValue: 4500.00,
          declaredValueCurrency: 'USD',
          expectedPackageCount: 3,
          expectedWeight: 14.5,
          weightUnit: 'kg',
          concurrencyStamp: 'STAMP-SHP-001',
          createdAtUtc: new Date(Date.now() - 90000000).toISOString(),
          legs: [
            {
              id: 'leg-1',
              sequenceNumber: 1,
              carrierId: 'carr-1',
              trackingNumber: 'TRK-9821389182',
              originLocationId: 'loc-1',
              destinationLocationId: 'loc-2',
              plannedDepartureAtUtc: new Date(Date.now() - 80000000).toISOString(),
              plannedArrivalAtUtc: new Date(Date.now() + 86400000).toISOString(),
              actualDepartureAtUtc: new Date(Date.now() - 78000000).toISOString(),
              status: 'InTransit'
            }
          ],
          items: [
            {
              id: 'item-1',
              lineReference: 'LINE-1',
              sku: 'ELEC-GPU-4090',
              description: 'NVIDIA RTX 4090 Graphics Card 24GB',
              expectedQuantity: 2,
              quantityUnit: 'ea',
              unitValue: 1800.00,
              currency: 'USD'
            },
            {
              id: 'item-2',
              lineReference: 'LINE-2',
              sku: 'ELEC-PSU-1000W',
              description: 'Modular Power Supply 1000W Platinum',
              expectedQuantity: 2,
              quantityUnit: 'ea',
              unitValue: 450.00,
              currency: 'USD'
            }
          ]
        });
      }
    });
  }

  getMilestoneProgressWidth(): string {
    const status = this.shipment()?.status;
    if (status === 'Delivered') return '100%';
    if (status === 'OutForDelivery') return '66%';
    if (status === 'InTransit') return '33%';
    return '0%';
  }

  isMilestonePassed(idx: number): boolean {
    const status = this.shipment()?.status;
    if (status === 'Delivered') return true;
    if (status === 'OutForDelivery' && idx <= 2) return true;
    if (status === 'InTransit' && idx <= 1) return true;
    return idx === 0;
  }

  isMilestoneCurrent(idx: number): boolean {
    const status = this.shipment()?.status;
    if (status === 'Delivered') return idx === 3;
    if (status === 'OutForDelivery') return idx === 2;
    if (status === 'InTransit') return idx === 1;
    return idx === 0;
  }
}
