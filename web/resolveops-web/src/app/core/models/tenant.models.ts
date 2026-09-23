export interface Tenant {
  id: string;
  code: string;
  name: string;
  status: string;
  defaultTimezone: string;
  defaultCurrency: string;
}

export interface ListTenantsResponse {
  tenants: Tenant[];
}
