import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of } from 'rxjs';
import { User, LoginResponse, DemoPreset } from '../models/auth.models';
import { TenantService } from './tenant.service';

const TOKEN_KEY = 'resolveops_access_token';
const USER_KEY = 'resolveops_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  currentUser = signal<User | null>(this.loadStoredUser());

  isAuthenticated = computed(() => !!this.token() && !!this.currentUser());
  userRoles = computed(() => this.currentUser()?.roles ?? []);

  readonly demoPresets: DemoPreset[] = [
    {
      name: 'Operations Manager',
      role: 'OperationsManager',
      email: 'ops.manager@resolveops.local',
      badgeColor: '#4f46e5',
      description: 'Oversees operational queue, SLAs, policy thresholds & exception escalations.'
    },
    {
      name: 'Claims Specialist',
      role: 'ClaimsSpecialist',
      email: 'claims.specialist@resolveops.local',
      badgeColor: '#059669',
      description: 'Prepares claims, builds loss component breakdowns, files carrier claims up to $10,000.'
    },
    {
      name: 'Logistics Coordinator',
      role: 'LogisticsCoordinator',
      email: 'logistics.coord@resolveops.local',
      badgeColor: '#0284c7',
      description: 'Frontline tracking exceptions, task execution, carrier updates & initial evidence collection.'
    },
    {
      name: 'Finance',
      role: 'Finance',
      email: 'finance@resolveops.local',
      badgeColor: '#b45309',
      description: 'Authorized to execute financial recoveries, settlement payments, credit notes & write-offs.'
    },
    {
      name: 'System Admin',
      role: 'TenantAdmin',
      email: 'admin@resolveops.local',
      badgeColor: '#7c3aed',
      description: 'Full tenant administration, policy rule configurations, integration replays & user roles.'
    }
  ];

  constructor(
    private http: HttpClient,
    private router: Router,
    private tenantService: TenantService
  ) {
    if (this.token() && !this.currentUser()) {
      this.fetchCurrentUser().subscribe();
    }
  }

  getAccessToken(): string | null {
    return this.token();
  }

  login(email: string, password: string, tenantId?: string): Observable<LoginResponse> {
    const targetTenantId = tenantId || this.tenantService.tenantId;
    return this.http.post<LoginResponse>('/api/identity/login', {
      email,
      password,
      tenantId: targetTenantId
    }).pipe(
      tap(res => {
        this.setSession(res.accessToken);
        this.decodeAndStoreUser(res.accessToken, email, targetTenantId);
      })
    );
  }

  fetchCurrentUser(): Observable<User | null> {
    return this.http.get<User>('/api/identity/me').pipe(
      tap(user => {
        this.currentUser.set(user);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
      }),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/identity/refresh', {}).pipe(
      tap(res => {
        this.setSession(res.accessToken);
      })
    );
  }

  logout(): void {
    this.http.post('/api/identity/logout', {}).subscribe({
      next: () => {},
      error: () => {}
    });
    this.clearSession();
    this.router.navigate(['/login']);
  }

  hasRole(role: string): boolean {
    const roles = this.userRoles();
    return roles.includes(role) || roles.includes('Admin') || roles.includes('TenantAdmin');
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some(r => this.hasRole(r));
  }

  canPerformFinancialActions(): boolean {
    return this.hasAnyRole(['Finance', 'ClaimsSpecialist', 'OperationsManager', 'TenantAdmin', 'Admin']);
  }

  canManagePolicies(): boolean {
    return this.hasAnyRole(['OperationsManager', 'TenantAdmin', 'Admin']);
  }

  private setSession(token: string): void {
    this.token.set(token);
    localStorage.setItem(TOKEN_KEY, token);
  }

  private clearSession(): void {
    this.token.set(null);
    this.currentUser.set(null);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  private decodeAndStoreUser(token: string, fallbackEmail: string, tenantId: string): void {
    try {
      const parts = token.split('.');
      if (parts.length === 3) {
        const payload = JSON.parse(atob(parts[1]));
        const roles = Array.isArray(payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
          ? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
          : payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
            ? [payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']]
            : (Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : []);

        const user: User = {
          userId: payload.sub || '',
          email: payload.email || fallbackEmail,
          tenantId: payload.tenant_id || tenantId,
          roles: roles.length > 0 ? roles : ['OperationsManager']
        };
        this.currentUser.set(user);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        return;
      }
    } catch {
      // Fallback
    }

    const fallbackUser: User = {
      userId: 'user-current',
      email: fallbackEmail,
      tenantId: tenantId,
      roles: ['OperationsManager']
    };
    this.currentUser.set(fallbackUser);
    localStorage.setItem(USER_KEY, JSON.stringify(fallbackUser));
  }

  private loadStoredUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    if (raw) {
      try {
        return JSON.parse(raw);
      } catch {
        return null;
      }
    }
    return null;
  }
}
