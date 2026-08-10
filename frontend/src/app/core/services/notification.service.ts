import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { NotificationItem } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);

  /** Contador de no leídas, compartido con la campana del navbar. */
  readonly unreadCount = signal(0);

  getMine(onlyUnread = false): Observable<NotificationItem[]> {
    return this.api.get<NotificationItem[]>('notifications', { onlyUnread });
  }

  refreshUnreadCount(): void {
    this.api.get<{ count: number }>('notifications/unread-count').subscribe({
      next: (r) => this.unreadCount.set(r.count),
      error: () => { /* silencioso: la campana no debe romper la UI */ },
    });
  }

  markRead(id: string): Observable<void> {
    return this.api.post<void>(`notifications/${id}/read`, {}).pipe(
      tap(() => this.refreshUnreadCount()),
    );
  }

  markAllRead(): Observable<{ marked: number }> {
    return this.api.post<{ marked: number }>('notifications/read-all', {}).pipe(
      tap(() => this.unreadCount.set(0)),
    );
  }
}
