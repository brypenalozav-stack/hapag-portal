import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationItem } from '../../core/models/notification.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { NOTIFICATION_TYPE_KEYS } from '../../core/i18n/labels';

/**
 * Centro de notificaciones del usuario: lista, marcar leída y marcar todas. Los tipos conocidos
 * (accesos otorgados, modificados, revocados, vencidos y revocados en cadena: M1-12, M1-14,
 * M1-22) muestran el título traducido; el resto, el título que envía el servidor.
 */
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [TranslocoPipe, HlDatePipe, LoadingSpinnerComponent],
  templateUrl: './notifications.html',
  styles: [':host { display: block; }'],
})
export class NotificationsComponent implements OnInit {
  private readonly service = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeKeys = NOTIFICATION_TYPE_KEYS;

  items = signal<NotificationItem[]>([]);
  loading = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.getMine().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (i) => { this.items.set(i); this.loading.set(false); },
      error: () => { this.error.set(translate('notifications.loadError')); this.loading.set(false); },
    });
  }

  markRead(item: NotificationItem): void {
    if (item.isRead) return;
    this.service.markRead(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
    });
  }

  markAll(): void {
    this.service.markAllRead().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
    });
  }
}
