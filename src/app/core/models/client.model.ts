export interface Client {
  id: string;
  name: string;
  email: string;
  taxId: string;
  phone: string;
  country: 'CL' | 'BO';
  type: 'CLIENT' | 'AGENT';
  agentCode?: string;
  role: 'USER' | 'ADMIN' | 'BA' | 'AGENT';
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
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  taxId: string;
  phone: string;
  country: 'CL' | 'BO';
  type: 'CLIENT' | 'AGENT';
  agentCode?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  email: string;
  newPassword: string;
}
