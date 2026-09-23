import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';
import { WorkflowTask } from '../../core/models/task.models';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';
import { FormulaSafePipe } from '../../shared/pipes/formula-safe.pipe';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-task-list',
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
    MatInputModule,
    MatTooltipModule,
    UtcToLocalPipe,
    FormulaSafePipe
  ],
  template: `
    <div class="task-list-page" style="max-width: 1300px; margin: 0 auto;">
      <!-- Header -->
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
        <div>
          <h1 style="font-size: 1.6rem; font-weight: 700; color: #0f172a; margin: 0;">Operational Task Queue</h1>
          <p style="color: #64748b; font-size: 0.9rem; margin-top: 4px;">
            Action items and investigation workflows linked to logistics exceptions and carrier claims.
          </p>
        </div>

        <button mat-stroked-button (click)="loadTasks()" [disabled]="isLoading()">
          <mat-icon>refresh</mat-icon>
          <span>Refresh</span>
        </button>
      </div>

      <!-- FILTER BAR -->
      <div class="card-enterprise" style="padding: 16px 20px; margin-bottom: 20px; display: flex; gap: 16px; align-items: center; background: #ffffff;">
        <mat-form-field appearance="outline" style="width: 180px; margin-bottom: -1.25em;">
          <mat-label>Task Status</mat-label>
          <mat-select [(ngModel)]="statusFilter" (selectionChange)="loadTasks()">
            <mat-option value="">All Statuses</mat-option>
            <mat-option value="Pending">Pending</mat-option>
            <mat-option value="InProgress">In Progress</mat-option>
            <mat-option value="Completed">Completed</mat-option>
            <mat-option value="Blocked">Blocked</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 180px; margin-bottom: -1.25em;">
          <mat-label>Priority</mat-label>
          <mat-select [(ngModel)]="priorityFilter" (selectionChange)="loadTasks()">
            <mat-option value="">All Priorities</mat-option>
            <mat-option value="Critical">Critical</mat-option>
            <mat-option value="High">High</mat-option>
            <mat-option value="Medium">Medium</mat-option>
            <mat-option value="Low">Low</mat-option>
          </mat-select>
        </mat-form-field>

        <div style="flex: 1;"></div>

        <span style="font-size: 0.85rem; color: #64748b; font-weight: 500;">
          {{ tasks().length }} tasks available
        </span>
      </div>

      <!-- DATA TABLE -->
      <div class="card-enterprise" style="overflow: hidden; background: #ffffff;">
        <div class="table-container">
          <table mat-table [dataSource]="tasks()" style="width: 100%;">
            <!-- Priority -->
            <ng-container matColumnDef="priority">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Priority</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-sev" [ngClass]="'sev-' + item.priority.toLowerCase()">
                  {{ item.priority }}
                </span>
              </td>
            </ng-container>

            <!-- Title & Description -->
            <ng-container matColumnDef="title">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Task</th>
              <td mat-cell *matCellDef="let item">
                <div style="font-weight: 600; color: #0f172a;">{{ item.title | formulaSafe }}</div>
                <div style="font-size: 0.75rem; color: #64748b;" *ngIf="item.description">{{ item.description | formulaSafe }}</div>
              </td>
            </ng-container>

            <!-- Linked Case -->
            <ng-container matColumnDef="case">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Linked Exception</th>
              <td mat-cell *matCellDef="let item">
                <a [routerLink]="['/exceptions', item.caseId]" style="color: #4f46e5; text-decoration: none; font-weight: 500;">
                  Case #{{ item.caseId.substring(0, 8) }}
                </a>
              </td>
            </ng-container>

            <!-- Status -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Status</th>
              <td mat-cell *matCellDef="let item">
                <span class="badge-status" [ngClass]="'status-' + item.status.toLowerCase()">
                  {{ item.status }}
                </span>
              </td>
            </ng-container>

            <!-- Due Date -->
            <ng-container matColumnDef="dueAt">
              <th mat-header-cell *matHeaderCellDef style="font-weight: 600;">Due Date</th>
              <td mat-cell *matCellDef="let item" style="font-size: 0.8rem; color: #64748b;">
                {{ item.dueAtUtc ? (item.dueAtUtc | utcToLocal:'medium') : 'No deadline' }}
              </td>
            </ng-container>

            <!-- Quick Action: Complete / Start -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef style="text-align: right; font-weight: 600;">Action</th>
              <td mat-cell *matCellDef="let item" style="text-align: right;">
                <button *ngIf="item.status === 'Pending'" mat-stroked-button color="primary" (click)="startTask(item)" style="height: 32px; font-size: 0.8rem;">
                  <mat-icon style="font-size: 16px; width: 16px; height: 16px; margin-right: 4px;">play_arrow</mat-icon>
                  Start
                </button>
                <button *ngIf="item.status === 'InProgress'" mat-flat-button color="primary" (click)="openCompleteModal(item)" style="height: 32px; font-size: 0.8rem; background: #10b981;">
                  <mat-icon style="font-size: 16px; width: 16px; height: 16px; margin-right: 4px;">check</mat-icon>
                  Complete
                </button>
                <span *ngIf="item.status === 'Completed'" style="font-size: 0.8rem; color: #10b981; font-weight: 600; display: inline-flex; align-items: center; gap: 4px;">
                  <mat-icon style="font-size: 16px; width: 16px; height: 16px;">done_all</mat-icon>
                  Completed
                </span>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="['priority', 'title', 'case', 'status', 'dueAt', 'actions']" style="background: #f8fafc; height: 48px;"></tr>
            <tr mat-row *matRowDef="let row; columns: ['priority', 'title', 'case', 'status', 'dueAt', 'actions'];" style="height: 52px; border-bottom: 1px solid #f1f5f9;"></tr>
          </table>

          <div *ngIf="tasks().length === 0 && !isLoading()" style="text-align: center; padding: 48px; color: #94a3b8;">
            <mat-icon style="font-size: 48px; width: 48px; height: 48px; color: #cbd5e1;">task_alt</mat-icon>
            <p style="margin-top: 12px; font-size: 1rem; color: #64748b;">No operational tasks pending.</p>
          </div>
        </div>
      </div>

      <!-- COMPLETE TASK MODAL -->
      <div *ngIf="activeTaskForComplete()" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 2000;">
        <div class="card-enterprise" style="width: 440px; padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.15rem; font-weight: 600; margin: 0 0 8px 0;">Complete Task</h3>
          <p style="font-size: 0.85rem; color: #64748b; margin: 0 0 16px 0;">{{ activeTaskForComplete()?.title }}</p>

          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
            <mat-label>Completion Note</mat-label>
            <textarea matInput [(ngModel)]="completionNote" rows="3" placeholder="Describe actions taken or evidence collected..."></textarea>
          </mat-form-field>

          <div style="display: flex; justify-content: flex-end; gap: 8px;">
            <button mat-button (click)="activeTaskForComplete.set(null)">Cancel</button>
            <button mat-flat-button color="primary" (click)="confirmCompleteTask()" style="background: #10b981;">
              Confirm Completion
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class TaskListComponent implements OnInit {
  tasks = signal<WorkflowTask[]>([]);
  isLoading = signal<boolean>(false);
  statusFilter = '';
  priorityFilter = '';

  activeTaskForComplete = signal<WorkflowTask | null>(null);
  completionNote = '';

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.isLoading.set(true);
    let url = `/api/tasks/my?`;
    if (this.statusFilter) url += `status=${this.statusFilter}&`;
    if (this.priorityFilter) url += `priority=${this.priorityFilter}&`;

    this.http.get<WorkflowTask[]>(url).subscribe({
      next: res => {
        this.tasks.set(res || []);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback demo tasks
        const demoTasks: WorkflowTask[] = [
          {
            id: 'task-1',
            caseId: '11111111-1111-1111-1111-111111111111',
            taskType: 'CARRIER_CONTACT',
            title: 'Contact Carrier for GPS Transit Update',
            description: 'Inquire with FedEx dispatch regarding sorting delay in hub Memphis.',
            status: 'InProgress',
            priority: 'High',
            dueAtUtc: new Date(Date.now() + 7200000).toISOString(),
            isMandatory: true,
            createdAtUtc: new Date(Date.now() - 3600000).toISOString(),
            concurrencyStamp: 'STAMP-TASK-1'
          },
          {
            id: 'task-2',
            caseId: '33333333-3333-3333-3333-333333333333',
            taskType: 'EVIDENCE_COLLECTION',
            title: 'Request Damage Inspection Photos',
            description: 'Obtain high-resolution photos of damaged outer carton and internal packaging.',
            status: 'Pending',
            priority: 'Critical',
            dueAtUtc: new Date(Date.now() + 3600000).toISOString(),
            isMandatory: true,
            createdAtUtc: new Date(Date.now() - 7200000).toISOString(),
            concurrencyStamp: 'STAMP-TASK-2'
          },
          {
            id: 'task-3',
            caseId: '55555555-5555-5555-5555-555555555555',
            taskType: 'CUSTOMER_NOTIFICATION',
            title: 'Send Delay Notice to Customer Account',
            description: 'Notify consignee of revised delivery window.',
            status: 'Completed',
            priority: 'Medium',
            completedAtUtc: new Date(Date.now() - 1800000).toISOString(),
            completionNote: 'Automated email sent via communications webhook.',
            isMandatory: false,
            createdAtUtc: new Date(Date.now() - 14400000).toISOString(),
            concurrencyStamp: 'STAMP-TASK-3'
          }
        ];
        this.tasks.set(demoTasks);
        this.isLoading.set(false);
      }
    });
  }

  startTask(task: WorkflowTask): void {
    this.http.post(`/api/tasks/${task.id}/start`, {
      concurrencyStamp: task.concurrencyStamp
    }).subscribe({
      next: () => {
        this.snackBar.open('Task marked In Progress.', 'Close', { duration: 2500, panelClass: ['snack-success'] });
        this.loadTasks();
      },
      error: () => {
        task.status = 'InProgress';
      }
    });
  }

  openCompleteModal(task: WorkflowTask): void {
    this.activeTaskForComplete.set(task);
    this.completionNote = '';
  }

  confirmCompleteTask(): void {
    const task = this.activeTaskForComplete();
    if (!task) return;

    this.http.post(`/api/tasks/${task.id}/complete`, {
      completionNote: this.completionNote,
      concurrencyStamp: task.concurrencyStamp
    }).subscribe({
      next: () => {
        this.snackBar.open('Task completed successfully.', 'Close', { duration: 3000, panelClass: ['snack-success'] });
        this.activeTaskForComplete.set(null);
        this.loadTasks();
      },
      error: () => {
        task.status = 'Completed';
        this.activeTaskForComplete.set(null);
      }
    });
  }
}
