import { Component, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslocoPipe],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  host: { '(document:keydown.escape)': 'onEscape()' },
})
export class SidebarComponent {
  readonly auth = inject(AuthService);
  isOpen = input(false);
  closed = output<void>();

  onLinkClick(): void {
    this.closed.emit();
  }

  /** Escape cierra el menú lateral móvil (WCAG 2.1.2). */
  onEscape(): void {
    if (this.isOpen()) {
      this.closed.emit();
    }
  }
}
