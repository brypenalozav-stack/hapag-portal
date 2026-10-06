import { Component, DOCUMENT, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from './core/services/auth.service';
import { LiveAnnouncerService } from './core/services/live-announcer.service';
import { NavbarComponent } from './shared/components/navbar/navbar';
import { SidebarComponent } from './shared/components/sidebar/sidebar';
import { AssistantComponent } from './shared/components/assistant/assistant';
import { ThemeService } from './core/services/theme.service';
import { GuideHostComponent } from './shared/components/guide/guide-host';
import { ImpersonationBannerComponent } from './shared/components/impersonation-banner/impersonation-banner';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, TranslocoPipe, NavbarComponent, SidebarComponent, AssistantComponent, GuideHostComponent, ImpersonationBannerComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class AppComponent {
  readonly auth = inject(AuthService);
  readonly announcer = inject(LiveAnnouncerService);
  private readonly document = inject(DOCUMENT);
  /** Tema claro u oscuro (M11-07): se aplica desde el arranque, con o sin sesión. */
  readonly theme = inject(ThemeService);
  sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  /** Enlace para saltar al contenido (WCAG 2.4.1): mueve el foco a <main> sin cambiar la URL. */
  skipToContent(event: Event): void {
    const main = this.document.getElementById('contenido-principal');
    if (!main) return;
    event.preventDefault();
    main.focus();
  }
}
