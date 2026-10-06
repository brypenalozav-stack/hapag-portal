import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { PERMISSIONS } from '../../../core/constants/app.constants';
import { DisputeLinkComponent } from '../dispute-link/dispute-link';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslocoPipe, DisputeLinkComponent],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  host: { '(document:keydown.escape)': 'onEscape()' },
})
export class SidebarComponent {
  readonly auth = inject(AuthService);
  /** Carro (M5-01) o pago desde la cuenta para clientes con crédito (M5-07). */
  readonly cart = inject(CartService);
  isOpen = input(false);

  /** Bandeja interna de organizaciones (M8-04). */
  canReviewOrganizations = computed(() =>
    this.auth.hasPermission(PERMISSIONS.REVIEW_ORGANIZATIONS, PERMISSIONS.CHECK_ORGANIZATIONS_AR),
  );
  /** Editor de la matriz base de accesos (M1-11). */
  canManageAccessMatrix = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_ACCESS_MATRIX));
  /** Mantenedores de tarifas (M8-01) y reglas internas de cobro (M3-04, M3-16). */
  canManageMaintainers = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_MAINTAINERS));
  /** Ventanas de bloqueo de pagos (M8-07). */
  canManagePaymentBlocks = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_PAYMENT_BLOCKS));
  /** Herramientas de Finanzas (M5-02, NF-03, NF-04). */
  canUseFinance = computed(() => this.auth.hasPermission(PERMISSIONS.PAYMENTS_FINANCE));
  /** Bandeja de solicitudes de servicios on demand (Fase 2, Ola G). */
  canProcessServiceRequests = computed(() => this.auth.hasPermission(PERMISSIONS.PROCESS_SERVICE_REQUESTS));
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
