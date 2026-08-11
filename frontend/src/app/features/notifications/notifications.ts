import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationItem } from '../../core/models/notification.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

/** Centro de notificaciones del usuario: lista, marcar leída y marcar todas. */
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [DatePipe, LoadingSpinnerComponent],
  templateUrl: './notifications.html',
  styles: [':host { display: block; }'],
})
export class NotificationsComponent implements OnInit {
  private readonly service = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

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
      error: () => { this.error.set('Error al cargar las notificaciones.'); this.loading.set(false); },
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
