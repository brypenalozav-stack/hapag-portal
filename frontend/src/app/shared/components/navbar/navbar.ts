import { Component, DestroyRef, ElementRef, computed, inject, input, output, signal, viewChild, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { filter, switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { NotificationService } from '../../../core/services/notification.service';
import { OrganizationService } from '../../../core/services/organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { CartService } from '../../../core/services/cart.service';
import { THEME_PREFERENCES, ThemePreference, ThemeService } from '../../../core/services/theme.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class NavbarComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  readonly locale = inject(LocaleService);
  /** Carro de compra (M5-01) o, para clientes con crédito, pago desde la cuenta (M5-07). */
  readonly cart = inject(CartService);
  /** Tema claro, oscuro o del sistema (M11-07). */
  readonly themes = inject(ThemeService);
  readonly themeOptions = THEME_PREFERENCES;
  readonly themeKeys: Record<ThemePreference, string> = {
    auto: 'shared.navbar.theme.auto',
    light: 'shared.navbar.theme.light',
    dark: 'shared.navbar.theme.dark',
  };
  private readonly organizations = inject(OrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  /** Estado del menú lateral móvil, para `aria-expanded` del botón de menú. */
  sidebarOpen = input(false);
  toggleSidebar = output<void>();

  /** Selector de país (M1-04): solo si la organización opera en más de un país. */
  countries = computed(() => this.auth.operatingCountries());
  changingCountry = signal(false);

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.notifications.refreshUnreadCount();
    }
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  /**
   * Cambia el país de operación (M1-04) y renueva el token para que el claim `country` y el
   * usuario de la sesión lo reflejen; el locale (es-CL / es-BO) y el huso siguen al país.
   */
  setCountry(country: 'CL' | 'BO'): void {
    if (country === this.auth.getCountry() || this.changingCountry()) return;
    this.changingCountry.set(true);
    this.organizations.setOperatingCountry(country).pipe(
      switchMap(() => this.auth.refreshToken()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.changingCountry.set(false);
        const name = translate(country === 'BO' ? 'common.country.bo' : 'common.country.cl');
        this.toast.success(translate('shared.navbar.country.changed', { country: name }));
      },
      error: () => {
        this.changingCountry.set(false);
        this.announcer.announce(translate('shared.navbar.country.error'), 'assertive');
      },
    });
  }

  /** Menú del usuario (correo, organización, perfil y salir). */
  readonly userMenuOpen = signal(false);
  private readonly userMenuButton = viewChild<ElementRef<HTMLButtonElement>>('userMenuButton');
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    // Elegir una opción navega: el menú se cierra con la navegación.
    inject(Router)
      .events.pipe(filter((e) => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => this.userMenuOpen.set(false));
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  onDocumentClick(event: MouseEvent): void {
    if (this.userMenuOpen() && !this.host.nativeElement.querySelector('.hl-user-menu')?.contains(event.target as Node)) {
      this.userMenuOpen.set(false);
    }
  }

  /** Esc cierra el menú y devuelve el foco a su botón (WCAG 2.1.2). */
  onEscape(): void {
    if (!this.userMenuOpen()) return;
    this.userMenuOpen.set(false);
    this.userMenuButton()?.nativeElement.focus();
  }

  /** Cambia el tema y lo anuncia; la preferencia se conserva entre sesiones (hl_theme). */
  onTheme(event: Event): void {
    const preference = (event.target as HTMLSelectElement).value as ThemePreference;
    this.themes.setPreference(preference);
    this.toast.success(translate('shared.navbar.theme.changed', { theme: translate(this.themeKeys[preference]) }));
  }

  logout(): void {
    this.auth.logout();
  }
}
