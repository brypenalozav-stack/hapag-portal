import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, catchError, switchMap, throwError, of } from 'rxjs';
import { ApiService } from './api.service';
import {
  Client,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../models/client.model';
import { ROLES, COUNTRIES, API_ENDPOINTS } from '../constants/app.constants';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private readonly TOKEN_KEY = 'hl_token';
  private readonly REFRESH_TOKEN_KEY = 'hl_refresh_token';
  private readonly USER_KEY = 'hl_user';

  currentUser = signal<Client | null>(this.loadUser());

  isAuthenticated = computed(() => !!this.currentUser() && !!this.getToken());

  /** Admin check: case-insensitive, accepts ADMIN and BA roles */
  isAdmin = computed(() => {
    const role = this.currentUser()?.role?.toUpperCase();
    return role === ROLES.ADMIN || role === ROLES.BA;
  });

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>(API_ENDPOINTS.AUTH_LOGIN, credentials).pipe(
      tap((response) => {
        localStorage.setItem(this.TOKEN_KEY, response.token);
        if (response.refreshToken) {
          localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
        }
        localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
        this.currentUser.set(response.user);
      }),
    );
  }

  register(data: RegisterRequest): Observable<Client> {
    return this.api.post<Client>(API_ENDPOINTS.AUTH_REGISTER, data);
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
      tap((response) => {
        localStorage.setItem(this.TOKEN_KEY, response.token);
        if (response.refreshToken) {
          localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
        }
        localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
        this.currentUser.set(response.user);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
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
}
