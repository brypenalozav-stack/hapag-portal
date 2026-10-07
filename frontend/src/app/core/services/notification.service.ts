import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  NotificationFilters,
  NotificationItem,
  NotificationPreference,
  NotificationPreferenceUpdate,
  UnreadCount,
} from '../models/notification.model';

const BASE = API_ENDPOINTS.NOTIFICATIONS;

/**
 * Bandeja de notificaciones (Fase 2, Ola I, M1-25): filtros por módulo, tipo y BL, no leídas por módulo, acciones
 * pendientes y preferencias de correo por tipo. El contador se comparte con la campana del navbar.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);

  /** Contador de no leídas, compartido con la campana del navbar. */
  readonly unreadCount = signal(0);
  /** No leídas que esperan una acción del usuario. */
  readonly actionableCount = signal(0);
  /** No leídas por módulo (filtros de la bandeja). */
  readonly unreadByModule = signal<Record<string, number>>({});

  getMine(onlyUnread = false): Observable<NotificationItem[]> {
    return this.api.get<NotificationItem[]>(BASE, { onlyUnread });
  }

  search(filters: NotificationFilters): Observable<NotificationItem[]> {
    const query: Record<string, string | number | boolean> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '' && value !== false) query[key] = value as string | number | boolean;
    }
    return this.api.get<NotificationItem[]>(BASE, query);
  }

  refreshUnreadCount(): void {
    this.api.get<UnreadCount>(`${BASE}/unread-count`).subscribe({
      next: (r) => {
        this.unreadCount.set(r.count);
        this.actionableCount.set(r.actionable ?? 0);
        this.unreadByModule.set(r.byModule ?? {});
      },
      error: () => { /* silencioso: la campana no debe romper la UI */ },
    });
  }

  markRead(id: string): Observable<void> {
    return this.api.post<void>(`${BASE}/${id}/read`, {}).pipe(
      tap(() => this.refreshUnreadCount()),
    );
  }

  /** Marca todas como leídas; con `module`, solo las de ese módulo. */
  markAllRead(module?: string): Observable<{ marked: number }> {
    const path = module ? `${BASE}/read-all?module=${encodeURIComponent(module)}` : `${BASE}/read-all`;
    return this.api.post<{ marked: number }>(path, {}).pipe(
      tap(() => this.refreshUnreadCount()),
    );
  }

  getPreferences(): Observable<NotificationPreference[]> {
    return this.api.get<NotificationPreference[]>(`${BASE}/preferences`);
  }

  updatePreferences(items: NotificationPreferenceUpdate[]): Observable<NotificationPreference[]> {
    return this.api.put<NotificationPreference[]>(`${BASE}/preferences`, { items });
  }
}
