import { OrganizationSummary, OrganizationType } from './organization.model';

export interface Client {
  id: string;
  name: string;
  email: string;
  taxId: string;
  phone: string;
  country: 'CL' | 'BO';
  type: 'CLIENT' | 'AGENT';
  agentCode?: string;
  role: 'USER' | 'ADMIN';
  isActive: boolean;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  user: Client;
  /** Organización del usuario y su situación (M1-07, M1-08, M1-02, M1-04). */
  organization?: OrganizationSummary | null;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  taxId: string;
  phone: string;
  country: 'CL' | 'BO';
  clientType: 'Client' | 'CustomsAgent';
  agentCode?: string;
  /** Tipo de organización declarado (M1-07); prevalece sobre `clientType`. */
  organizationType?: OrganizationType;
  contactFirstName?: string;
  contactLastName?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  email: string;
  newPassword: string;
}
