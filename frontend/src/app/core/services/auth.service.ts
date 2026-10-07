import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, throwError, catchError, of, finalize } from 'rxjs';
import { ApiService } from './api.service';
import {
  Client,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../models/client.model';
import {
  JoinOrganizationRequest,
  JoinOrganizationResponse,
  OrganizationSummary,
} from '../models/organization.model';
import { ROLES, INTERNAL_ROLES, COUNTRIES, API_ENDPOINTS } from '../constants/app.constants';
import { ImpersonationSession, ImpersonationStarted } from '../models/administration.model';

/** Claim del JWT con los permisos del usuario (HasPermission en el servidor). */
const PERMISSION_CLAIM = 'permission';

/**
 * Permisos del claim `permission` del JWT. Solo sirven para mostrar u ocultar opciones:
 * el servidor vuelve a validar cada permiso (NF-05, M1-11).
 */
export function decodePermissions(token: string | null): string[] {
  const payload = token?.split('.')[1];
  if (!payload) return [];
  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
    const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
    const claims = JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>;
    const value = claims[PERMISSION_CLAIM];
    if (Array.isArray(value)) return value.filter((v): v is string => typeof v === 'string');
    return typeof value === 'string' ? [value] : [];
  } catch {
    return [];
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private readonly TOKEN_KEY = 'hl_token';
  private readonly REFRESH_TOKEN_KEY = 'hl_refresh_token';
  private readonly USER_KEY = 'hl_user';
  private readonly ORGANIZATION_KEY = 'hl_organization';
  /** Vista como cliente (M8-08): sesión en curso y la sesión del administrador guardada para restaurarla. */
  private readonly IMPERSONATION_KEY = 'hl_impersonation';
  private readonly ADMIN_SESSION_KEY = 'hl_admin_session';

  currentUser = signal<Client | null>(this.loadUser());

  /** Organización del usuario (M1-07, M1-08, M1-02, M1-04); null en sesiones previas a la Ola A. */
  organization = signal<OrganizationSummary | null>(this.loadOrganization());

  private readonly token = signal<string | null>(localStorage.getItem(this.TOKEN_KEY));

  /** Sesión de vista como cliente en curso (M8-08); null fuera de ella. */
  impersonation = signal<ImpersonationSession | null>(this.loadImpersonation());

  /** El administrador ve el portal como un cliente, en solo lectura (M8-08). */
  isImpersonating = computed(() => !!this.impersonation());

  isAuthenticated = computed(() => !!this.currentUser() && !!this.token());

  /** Admin check: el backend solo emite ADMIN/USER (BUG-14). */
  isAdmin = computed(() => this.currentUser()?.role?.toUpperCase() === ROLES.ADMIN);

  /** Perfil interno (consola operativa): Administrador/Coordinador/Supervisor/SuperAdmin. */
  isInternal = computed(() => {
    const role = this.currentUser()?.role?.toUpperCase();
    return !!role && (INTERNAL_ROLES as readonly string[]).includes(role);
  });

  /** Permisos del JWT, para mostrar u ocultar opciones; el servidor es quien decide. */
  permissions = computed(() => new Set(decodePermissions(this.token())));

  /** El perfil y el estado de la organización permiten pagar y solicitar servicios (M1-02, M1-07). */
  canOperate = computed(() => this.organization()?.canOperate ?? false);

  /** Países en los que opera la organización (M1-04). */
  operatingCountries = computed(() => this.organization()?.operatingCountries ?? []);

  /** Tiene al menos uno de los permisos indicados. */
  hasPermission(...permissions: string[]): boolean {
    const granted = this.permissions();
    return permissions.some((p) => granted.has(p));
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>(API_ENDPOINTS.AUTH_LOGIN, credentials).pipe(
      tap((response) => this.storeSession(response)),
    );
  }

  register(data: RegisterRequest): Observable<Client> {
    return this.api.post<Client>(API_ENDPOINTS.AUTH_REGISTER, data);
  }

  /** Solicitud para unirse a una organización ya registrada (M1-08). */
  requestMembership(data: JoinOrganizationRequest): Observable<JoinOrganizationResponse> {
    return this.api.post<JoinOrganizationResponse>(API_ENDPOINTS.AUTH_REGISTER_JOIN, data);
  }

  forgotPassword(data: ForgotPasswordRequest): Observable<unknown> {
    return this.api.post(API_ENDPOINTS.AUTH_FORGOT_PASSWORD, data);
  }

  resetPassword(data: ResetPasswordRequest): Observable<unknown> {
    return this.api.post(API_ENDPOINTS.AUTH_RESET_PASSWORD, data);
  }

  refreshToken(): Observable<LoginResponse> {
    const token = this.getToken();
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    if (!token || !refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.api.post<LoginResponse>(API_ENDPOINTS.AUTH_REFRESH_TOKEN, { token, refreshToken }).pipe(
      tap((response) => this.storeSession(response)),
    );
  }

  /** Actualiza la organización guardada (p. ej. tras GET /organizations/me). */
  setOrganization(organization: OrganizationSummary | null): void {
    if (organization) {
      localStorage.setItem(this.ORGANIZATION_KEY, JSON.stringify(organization));
    } else {
      localStorage.removeItem(this.ORGANIZATION_KEY);
    }
    this.organization.set(organization);
  }

  /**
   * Cierre de sesión (M1-10): invalida el refresh token en el servidor y después limpia la
   * sesión local, aunque el servidor no responda.
   */
  logout(): void {
    if (!this.getToken()) {
      this.clearSession();
      return;
    }
    if (this.isImpersonating()) {
      // Con el token de la vista como cliente, /auth/logout termina la sesión de impersonación (M8-08); después se
      // restaura la del administrador y se cierra también.
      this.api
        .post<void>(API_ENDPOINTS.AUTH_LOGOUT, {})
        .pipe(
          catchError(() => of(undefined)),
          finalize(() => {
            this.restoreAdminSession();
            this.logout();
          }),
        )
        .subscribe();
      return;
    }
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    this.api
      .post<void>(API_ENDPOINTS.AUTH_LOGOUT, refreshToken ? { refreshToken } : {})
      .pipe(
        catchError(() => of(undefined)),
        finalize(() => this.clearSession()),
      )
      .subscribe();
  }

  /** Limpia la sesión local y vuelve al ingreso (sin llamar al servidor). */
  clearSession(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.ORGANIZATION_KEY);
    localStorage.removeItem(this.IMPERSONATION_KEY);
    localStorage.removeItem(this.ADMIN_SESSION_KEY);
    this.token.set(null);
    this.currentUser.set(null);
    this.organization.set(null);
    this.impersonation.set(null);
    this.router.navigate(['/login']);
  }

  /**
   * Inicia la vista como cliente (M8-08): guarda la sesión del administrador y usa las credenciales del cliente que
   * entrega el servidor (sin refresh token; vencen con la sesión de impersonación).
   */
  beginImpersonation(started: ImpersonationStarted): void {
    if (!this.isImpersonating()) {
      const backup = {
        token: localStorage.getItem(this.TOKEN_KEY),
        refreshToken: localStorage.getItem(this.REFRESH_TOKEN_KEY),
        user: localStorage.getItem(this.USER_KEY),
        organization: localStorage.getItem(this.ORGANIZATION_KEY),
      };
      localStorage.setItem(this.ADMIN_SESSION_KEY, JSON.stringify(backup));
    }
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    this.storeSession({ ...started.auth, refreshToken: '' });
    this.setImpersonation(started.session);
  }

  /** Actualiza la sesión de vista como cliente (p. ej. tras GET /impersonation/current). */
  setImpersonation(session: ImpersonationSession | null): void {
    if (session) {
      localStorage.setItem(this.IMPERSONATION_KEY, JSON.stringify(session));
    } else {
      localStorage.removeItem(this.IMPERSONATION_KEY);
    }
    this.impersonation.set(session);
  }

  /** Termina la vista como cliente en el navegador y vuelve a la sesión guardada del administrador. */
  restoreAdminSession(): void {
    const raw = localStorage.getItem(this.ADMIN_SESSION_KEY);
    localStorage.removeItem(this.ADMIN_SESSION_KEY);
    this.setImpersonation(null);
    let backup: { token: string | null; refreshToken: string | null; user: string | null; organization: string | null } | null = null;
    try {
      backup = raw ? JSON.parse(raw) : null;
    } catch {
      backup = null;
    }
    if (!backup?.token || !backup.user) {
      this.clearSession();
      return;
    }
    const restore = (key: string, value: string | null) => (value ? localStorage.setItem(key, value) : localStorage.removeItem(key));
    restore(this.TOKEN_KEY, backup.token);
    restore(this.REFRESH_TOKEN_KEY, backup.refreshToken);
    restore(this.USER_KEY, backup.user);
    restore(this.ORGANIZATION_KEY, backup.organization);
    this.token.set(backup.token);
    this.currentUser.set(this.loadUser());
    this.organization.set(this.loadOrganization());
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getCountry(): 'CL' | 'BO' {
    return this.currentUser()?.country ?? COUNTRIES.CHILE;
  }

  getCurrentUser(): Client | null {
    return this.currentUser();
  }

  private storeSession(response: LoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    if (response.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    }
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    this.token.set(response.token);
    this.currentUser.set(response.user);
    this.setOrganization(response.organization ?? null);
  }

  private loadUser(): Client | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as Client;
    } catch (e: unknown) {
      console.warn('Failed to parse stored user data:', e instanceof Error ? e.message : e);
      return null;
    }
  }

  private loadImpersonation(): ImpersonationSession | null {
    const raw = localStorage.getItem(this.IMPERSONATION_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as ImpersonationSession;
    } catch {
      return null;
    }
  }

  private loadOrganization(): OrganizationSummary | null {
    const raw = localStorage.getItem(this.ORGANIZATION_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as OrganizationSummary;
    } catch {
      return null;
    }
  }
}
