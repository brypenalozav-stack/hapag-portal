import { Component, inject, output, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class NavbarComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  readonly locale = inject(LocaleService);
  toggleSidebar = output<void>();

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.notifications.refreshUnreadCount();
    }
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  logout(): void {
    this.auth.logout();
  }
}
