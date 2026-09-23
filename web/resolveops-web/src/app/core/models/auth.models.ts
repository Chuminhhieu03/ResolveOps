export interface User {
  userId: string;
  email: string;
  tenantId: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
}

export interface DemoPreset {
  name: string;
  role: string;
  email: string;
  badgeColor: string;
  description: string;
}
