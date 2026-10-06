import { LoginResponse } from './client.model';

/** Fase 2, Ola I: área de administración (M8-05) y vista como cliente (M8-08). */

/** Código de cada sección del área de administración que el usuario puede usar. */
export type AdminSectionCode =
  | 'organizations'
  | 'parent-links'
  | 'access-matrix'
  | 'maintainers'
  | 'guides'
  | 'payment-blocks'
  | 'finance'
  | 'service-requests'
  | 'announcements'
  | 'counter'
  | 'reports'
  | 'impersonation'
  | 'audit'
  | 'users';

export interface AdminOverviewSection {
  code: AdminSectionCode | string;
  permission?: string | null;
  counters?: Record<string, number> | null;
}

/** GET /admin/overview: solo las secciones que el usuario puede usar, con sus contadores. */
export interface AdminOverview {
  generatedAt: string;
  sections: AdminOverviewSection[];
}

export type ImpersonationStatus = 'Active' | 'Ended' | 'Expired';
export type ImpersonationEndReason = 'Manual' | 'Logout' | 'Expired' | 'Replaced' | 'Admin';

export interface ImpersonationUser {
  userId: string;
  email: string;
  fullName?: string | null;
}

export interface ImpersonationOrganization {
  id: string;
  name: string;
  taxId?: string | null;
  organizationType?: string | null;
}

export interface ImpersonationSession {
  id: string;
  actor: ImpersonationUser;
  subject: ImpersonationUser;
  organization: ImpersonationOrganization;
  reason: string;
  status: ImpersonationStatus;
  startedAt: string;
  expiresAt: string;
  endedAt?: string | null;
  endReason?: ImpersonationEndReason | null;
  durationSeconds?: number | null;
  requestCount: number;
  blockedCount: number;
  readOnly: boolean;
  allowedActions: string[];
}

/** Usuario de la organización elegida y si puede verse como cliente. */
export interface ImpersonationTarget {
  userId: string;
  email: string;
  fullName?: string | null;
  profile?: string | null;
  isActive: boolean;
  membershipStatus?: string | null;
  eligible: boolean;
  lastLoginAt?: string | null;
}

/** Sesión iniciada: credenciales del cliente sin refresh token, que vencen con la sesión. */
export interface ImpersonationStarted {
  auth: Omit<LoginResponse, 'refreshToken'> & { refreshToken?: string | null };
  session: ImpersonationSession;
}

export interface ImpersonationRequestEntry {
  id: string;
  action: 'Started' | 'Request' | 'BlockedWrite' | 'Ended' | string;
  timestamp: string;
  method?: string | null;
  path?: string | null;
  statusCode?: number | null;
}

export interface ImpersonationSessionSearch {
  actorUserId?: string;
  organizationId?: string;
  status?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
