import { Component, DOCUMENT, effect, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from './core/services/auth.service';
import { FeatureService } from './core/services/feature.service';
import { LiveAnnouncerService } from './core/services/live-announcer.service';
import { NavbarComponent } from './shared/components/navbar/navbar';
import { MainNavComponent } from './shared/components/main-nav/main-nav';
import { AssistantComponent } from './shared/components/assistant/assistant';
import { ThemeService } from './core/services/theme.service';
import { GuideHostComponent } from './shared/components/guide/guide-host';
import { ImpersonationBannerComponent } from './shared/components/impersonation-banner/impersonation-banner';
import { GlobalLoaderComponent } from './shared/components/global-loader/global-loader';
import { ModalHostComponent } from './shared/components/modal-host/modal-host';
import { ToastHostComponent } from './shared/components/toast-host/toast-host';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, TranslocoPipe, NavbarComponent, MainNavComponent, AssistantComponent, GuideHostComponent, ImpersonationBannerComponent, GlobalLoaderComponent, ModalHostComponent, ToastHostComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class AppComponent {
  readonly auth = inject(AuthService);
  /** Flags de funcionalidades (cierre de Fase 1): se leen al iniciar la aplicación. */
  readonly features = inject(FeatureService);
  readonly announcer = inject(LiveAnnouncerService);
  private readonly document = inject(DOCUMENT);
  /** Tema claro u oscuro (M11-07): se aplica desde el arranque, con o sin sesión. */
  readonly theme = inject(ThemeService);
  sidebarOpen = signal(false);
  /** Año del pie de página. */
  readonly year = new Date().getFullYear();

  constructor() {
    // Con sesión, el encabezado de escritorio suma la barra del menú principal (--navbar-height en styles.scss).
    effect(() => this.document.documentElement.classList.toggle('hl-has-mainnav', this.auth.isAuthenticated()));
  }

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
