import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { TenantService } from '../../core/services/tenant.service';
import { SignalRNotificationService } from '../../core/services/signalr-notification.service';
import { Tenant } from '../../core/models/tenant.models';
import { UtcToLocalPipe } from '../../shared/pipes/utc-to-local.pipe';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    UtcToLocalPipe
  ],
  template: `
    <div class="shell-container" style="display: flex; flex-direction: column; height: 100vh; overflow: hidden;">
      <!-- TOP HEADER -->
      <mat-toolbar color="default" style="background: #0f172a; color: #ffffff; border-bottom: 1px solid #1e293b; z-index: 1000; height: 64px; display: flex; justify-content: space-between; padding: 0 20px;">
        <div style="display: flex; align-items: center; gap: 16px;">
          <button mat-icon-button (click)="isSidebarCollapsed.set(!isSidebarCollapsed())" style="color: #94a3b8;" matTooltip="Toggle Sidebar">
            <mat-icon>menu</mat-icon>
          </button>
          
          <div style="display: flex; align-items: center; gap: 10px; cursor: pointer;" routerLink="/dashboard">
            <div style="background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); width: 36px; height: 36px; border-radius: 8px; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 8px rgba(79, 70, 229, 0.4);">
              <mat-icon style="color: #ffffff; font-size: 20px; width: 20px; height: 20px;">hub</mat-icon>
            </div>
            <div>
              <span style="font-weight: 700; font-size: 1.15rem; letter-spacing: -0.02em; color: #ffffff;">ResolveOps</span>
              <span style="font-size: 0.65rem; background: rgba(99, 102, 241, 0.2); color: #818cf8; padding: 2px 6px; border-radius: 4px; margin-left: 6px; font-weight: 600; text-transform: uppercase;">Enterprise</span>
            </div>
          </div>
        </div>

        <div style="display: flex; align-items: center; gap: 16px;">
          <!-- TENANT SWITCHER -->
          <div style="display: flex; align-items: center; background: rgba(30, 41, 59, 0.7); border: 1px solid #334155; border-radius: 8px; padding: 4px 12px; gap: 8px;">
            <mat-icon style="color: #38bdf8; font-size: 18px; width: 18px; height: 18px;">business</mat-icon>
            <mat-select [value]="tenantService.activeTenant().id" (selectionChange)="onTenantChange($event.value)" style="color: #f1f5f9; font-size: 0.85rem; width: 170px;">
              <mat-option *ngFor="let t of tenantService.availableTenants()" [value]="t.id">
                {{ t.name }} ({{ t.code }})
              </mat-option>
            </mat-select>
          </div>

          <!-- SIGNALR REAL-TIME NOTIFICATION BELL -->
          <button mat-icon-button (click)="drawerOpen.set(!drawerOpen())" [matBadge]="notificationService.unreadCount()" [matBadgeHidden]="notificationService.unreadCount() === 0" matBadgeColor="warn" style="color: #cbd5e1;" matTooltip="Notifications">
            <mat-icon>notifications</mat-icon>
          </button>

          <!-- USER PROFILE & ROLE BADGE -->
          <div style="display: flex; align-items: center; gap: 10px; cursor: pointer;" [matMenuTriggerFor]="userMenu">
            <div style="width: 34px; height: 34px; border-radius: 50%; background: #475569; display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 0.85rem; color: #f8fafc; border: 2px solid #6366f1;">
              {{ getInitials() }}
            </div>
            <div style="display: flex; flex-direction: column; text-align: left;">
              <span style="font-size: 0.85rem; font-weight: 600; color: #f1f5f9;">{{ authService.currentUser()?.email || 'User' }}</span>
              <span style="font-size: 0.7rem; color: #94a3b8;">{{ getPrimaryRole() }}</span>
            </div>
            <mat-icon style="color: #64748b; font-size: 18px;">arrow_drop_down</mat-icon>
          </div>

          <mat-menu #userMenu="matMenu" xPosition="before">
            <div style="padding: 12px 16px; border-bottom: 1px solid #e2e8f0;">
              <p style="margin: 0; font-size: 0.85rem; font-weight: 600; color: #0f172a;">{{ authService.currentUser()?.email }}</p>
              <span style="display: inline-block; margin-top: 4px; padding: 2px 8px; border-radius: 9999px; background: #e0f2fe; color: #0369a1; font-size: 0.7rem; font-weight: 600;">
                {{ getPrimaryRole() }}
              </span>
            </div>
            <button mat-menu-item routerLink="/dashboard">
              <mat-icon>dashboard</mat-icon>
              <span>Operational Dashboard</span>
            </button>
            <mat-divider></mat-divider>
            <button mat-menu-item (click)="logout()" style="color: #dc2626;">
              <mat-icon style="color: #dc2626;">logout</mat-icon>
              <span>Sign Out</span>
            </button>
          </mat-menu>
        </div>
      </mat-toolbar>

      <!-- BODY: SIDEBAR + CONTENT + DRAWER -->
      <mat-drawer-container style="flex: 1; display: flex; overflow: hidden; background: #f8fafc;">
        <!-- LEFT SIDEBAR -->
        <mat-drawer mode="side" opened style="background: #0f172a; color: #94a3b8; border-right: 1px solid #1e293b; width: 250px; transition: width 0.2s;" [style.width.px]="isSidebarCollapsed() ? 72 : 250">
          <div style="display: flex; flex-direction: column; justify-content: space-between; height: 100%; padding: 16px 8px;">
            <div style="display: flex; flex-direction: column; gap: 4px;">
              <a *ngFor="let item of navItems" [routerLink]="item.path" routerLinkActive="active-nav" [routerLinkActiveOptions]="{exact: item.exact}" class="nav-link" [matTooltip]="isSidebarCollapsed() ? item.label : ''" matTooltipPosition="right">
                <mat-icon style="font-size: 20px; width: 20px; height: 20px;">{{ item.icon }}</mat-icon>
                <span *ngIf="!isSidebarCollapsed()" style="font-size: 0.9rem; font-weight: 500;">{{ item.label }}</span>
              </a>

              <div *ngIf="authService.canManagePolicies()" style="margin-top: 16px;">
                <div *ngIf="!isSidebarCollapsed()" style="font-size: 0.7rem; font-weight: 700; color: #64748b; text-transform: uppercase; padding: 4px 12px; letter-spacing: 0.05em;">
                  Administration
                </div>
                <a routerLink="/admin/policies" routerLinkActive="active-nav" class="nav-link" [matTooltip]="isSidebarCollapsed() ? 'Policy Admin' : ''" matTooltipPosition="right">
                  <mat-icon style="font-size: 20px; width: 20px; height: 20px;">admin_panel_settings</mat-icon>
                  <span *ngIf="!isSidebarCollapsed()" style="font-size: 0.9rem; font-weight: 500;">Policy Admin</span>
                </a>
              </div>
            </div>

            <!-- FOOTER STATUS -->
            <div *ngIf="!isSidebarCollapsed()" style="padding: 12px; background: rgba(30, 41, 59, 0.4); border-radius: 8px; font-size: 0.75rem; color: #64748b;">
              <div style="display: flex; align-items: center; gap: 6px; margin-bottom: 4px;">
                <div style="width: 8px; height: 8px; border-radius: 50%;" [style.background]="notificationService.connectionState() === 'Connected' ? '#10b981' : '#f59e0b'"></div>
                <span style="color: #cbd5e1;">Live Stream: {{ notificationService.connectionState() }}</span>
              </div>
              <div>ResolveOps v1.0 • Phase 15</div>
            </div>
          </div>
        </mat-drawer>

        <!-- MAIN ROUTER CONTENT -->
        <mat-drawer-content style="flex: 1; overflow-y: auto; padding: 24px;">
          <router-outlet></router-outlet>
        </mat-drawer-content>

        <!-- RIGHT NOTIFICATION DRAWER -->
        <mat-drawer [opened]="drawerOpen()" (closed)="drawerOpen.set(false)" position="end" mode="over" style="width: 380px; background: #ffffff; box-shadow: -4px 0 16px rgba(0,0,0,0.15);">
          <div style="display: flex; flex-direction: column; height: 100%;">
            <div style="padding: 16px 20px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; background: #f8fafc;">
              <div style="display: flex; align-items: center; gap: 8px;">
                <mat-icon style="color: #4f46e5;">notifications_active</mat-icon>
                <h3 style="margin: 0; font-size: 1.1rem; font-weight: 600;">Live Notifications</h3>
              </div>
              <button mat-icon-button (click)="drawerOpen.set(false)">
                <mat-icon>close</mat-icon>
              </button>
            </div>

            <div style="padding: 8px 16px; display: flex; justify-content: space-between; align-items: center; background: #ffffff; border-bottom: 1px solid #f1f5f9;">
              <span style="font-size: 0.8rem; color: #64748b;">{{ notificationService.unreadCount() }} unread alerts</span>
              <button mat-button color="primary" (click)="notificationService.markAllAsRead()" style="font-size: 0.8rem; height: 32px; line-height: 32px;">
                Mark all as read
              </button>
            </div>

            <div style="flex: 1; overflow-y: auto; padding: 12px 16px;">
              <div *ngIf="notificationService.notifications().length === 0" style="text-align: center; padding: 40px 16px; color: #94a3b8;">
                <mat-icon style="font-size: 40px; width: 40px; height: 40px; color: #cbd5e1;">notifications_none</mat-icon>
                <p style="margin-top: 8px; font-size: 0.9rem;">No notifications yet</p>
              </div>

              <div *ngFor="let n of notificationService.notifications()" (click)="notificationService.markAsRead(n.id)" style="padding: 12px; border-radius: 8px; margin-bottom: 8px; cursor: pointer; transition: background 0.15s; border-left: 4px solid #4f46e5;" [style.background]="n.isRead ? '#ffffff' : '#f0fdf4'" [style.border-left-color]="n.isRead ? '#cbd5e1' : '#10b981'">
                <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 4px;">
                  <strong style="font-size: 0.85rem; color: #0f172a;">{{ n.title }}</strong>
                  <span style="font-size: 0.7rem; color: #94a3b8;">{{ n.createdAtUtc | utcToLocal:'short' }}</span>
                </div>
                <p style="margin: 0; font-size: 0.8rem; color: #475569; line-height: 1.4;">{{ n.message }}</p>
              </div>
            </div>
          </div>
        </mat-drawer>
      </mat-drawer-container>
    </div>
  `,
  styles: [`
    .nav-link {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 14px;
      border-radius: 8px;
      color: #94a3b8;
      text-decoration: none;
      transition: all 0.15s ease;

      &:hover {
        background: rgba(255, 255, 255, 0.05);
        color: #f1f5f9;
      }

      &.active-nav {
        background: linear-gradient(90deg, rgba(79, 70, 229, 0.25) 0%, rgba(79, 70, 229, 0.05) 100%);
        color: #818cf8;
        font-weight: 600;
        border-left: 3px solid #6366f1;
      }
    }
  `]
})
export class ShellComponent implements OnInit, OnDestroy {
  isSidebarCollapsed = signal<boolean>(false);
  drawerOpen = signal<boolean>(false);

  readonly navItems = [
    { label: 'Operations Dashboard', icon: 'dashboard', path: '/dashboard', exact: true },
    { label: 'Exception Work Queue', icon: 'report_problem', path: '/exceptions', exact: false },
    { label: 'Task List', icon: 'checklist', path: '/tasks', exact: false },
    { label: 'Claims Management', icon: 'assignment_turned_in', path: '/claims', exact: false },
    { label: 'Shipments', icon: 'local_shipping', path: '/shipments', exact: false },
    { label: 'Carrier Scorecards', icon: 'insights', path: '/reports/carrier-scorecards', exact: false },
    { label: 'Integration Quarantine', icon: 'security', path: '/quarantine', exact: false }
  ];

  constructor(
    public authService: AuthService,
    public tenantService: TenantService,
    public notificationService: SignalRNotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.notificationService.startConnection();
  }

  ngOnDestroy(): void {
    this.notificationService.stopConnection();
  }

  onTenantChange(tenantId: string): void {
    const found = this.tenantService.availableTenants().find(t => t.id === tenantId);
    if (found) {
      this.tenantService.switchTenant(found);
      // Re-login or reload current view with new tenant context
      window.location.reload();
    }
  }

  getInitials(): string {
    const email = this.authService.currentUser()?.email || 'U';
    return email.substring(0, 2).toUpperCase();
  }

  getPrimaryRole(): string {
    const roles = this.authService.userRoles();
    return roles.length > 0 ? roles[0] : 'OperationsManager';
  }

  logout(): void {
    this.authService.logout();
  }
}
