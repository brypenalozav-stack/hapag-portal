import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { PaymentService } from '../../core/services/payment.service';
import { BillOfLadingService } from '../../core/services/bl.service';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, CountryBadgeComponent],
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

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.paymentService.getAll().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (payments) => {
        this.pendingPayments.set(payments.filter((p) => p.status === 'PENDING').length);
        this.recentActivity.set(payments.length);
      },
      error: () => { /* Dashboard metrics are non-critical */ },
    });

    this.blService.getMyBLs().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (bls) => this.activeBLs.set(bls.length),
      error: () => { /* Dashboard metrics are non-critical */ },
    });
  }
}
