import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Tenant, ListTenantsResponse } from '../models/tenant.models';

const STORAGE_KEY = 'resolveops_active_tenant';

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly defaultTenant: Tenant = {
    id: '00000000-0000-0000-0000-000000000001',
    code: 'local-dev',
    name: 'Local Development',
    status: 'Active',
    defaultTimezone: 'UTC',
    defaultCurrency: 'USD'
  };

  activeTenant = signal<Tenant>(this.loadInitialTenant());
  availableTenants = signal<Tenant[]>([]);

  constructor(private http: HttpClient) {
    this.fetchTenants().subscribe();
  }

  get tenantId(): string {
    return this.activeTenant().id;
  }

  fetchTenants(): Observable<ListTenantsResponse> {
    return this.http.get<ListTenantsResponse>('/api/tenants').pipe(
      tap(res => {
        if (res && res.tenants && res.tenants.length > 0) {
          this.availableTenants.set(res.tenants);
          const current = this.activeTenant();
          const matched = res.tenants.find(t => t.id === current.id || t.code === current.code);
          if (matched) {
            this.activeTenant.set(matched);
          } else {
            this.activeTenant.set(res.tenants[0]);
          }
        }
      })
    );
  }

  switchTenant(tenant: Tenant): void {
    this.activeTenant.set(tenant);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tenant));
  }

  private loadInitialTenant(): Tenant {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // Fall back to default
      }
    }
    return this.defaultTenant;
  }
}
