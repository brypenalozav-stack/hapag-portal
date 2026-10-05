import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { PaymentService } from '../../core/services/payment.service';
import { BillOfLadingService } from '../../core/services/bl.service';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CountryBadgeComponent, StateMessageComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly paymentService = inject(PaymentService);
  private readonly blService = inject(BillOfLadingService);
  private readonly destroyRef = inject(DestroyRef);

  pendingPayments = signal(0);
  activeBLs = signal(0);
  recentActivity = signal(0);

  /** Consultas respondidas con éxito (pagos y BL). */
  private loadedSources = signal(0);
  /** NF-11: alguna consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);
  /** NF-11: ambas consultas respondieron 200 sin pagos ni BL. */
  noData = computed(() => this.loadedSources() === 2 && this.recentActivity() === 0 && this.activeBLs() === 0);

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loadFailed.set(false);
    this.loadedSources.set(0);

    this.paymentService.getAll().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (payments) => {
        this.pendingPayments.set(payments.filter((p) => p.status === 'PENDING').length);
        this.recentActivity.set(payments.length);
        this.loadedSources.update((n) => n + 1);
      },
      error: (err) => this.onLoadError(err),
    });

    this.blService.getMyBLs().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (bls) => {
        this.activeBLs.set(bls.length);
        this.loadedSources.update((n) => n + 1);
      },
      error: (err) => this.onLoadError(err),
    });
  }

  /** Las métricas no son críticas: solo se muestra el estado de error si el servicio no está disponible. */
  private onLoadError(err: unknown): void {
    if (isServiceUnavailable(err)) {
      this.loadFailed.set(true);
    }
  }
}
