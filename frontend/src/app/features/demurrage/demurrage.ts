import { Component, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BillOfLadingService } from '../../core/services/bl.service';
import { AuthService } from '../../core/services/auth.service';
import { DemurrageCharge } from '../../core/models/bl.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-demurrage',
  standalone: true,
  imports: [RouterLink, DecimalPipe, FormsModule, StatusBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './demurrage.html',
  styleUrl: './demurrage.scss',
})
export class DemurrageComponent implements OnInit {
  private readonly blService = inject(BillOfLadingService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  searchBlNumber = '';
  activeBlNumber = signal('');
  charges = signal<DemurrageCharge[]>([]);
  loading = signal(false);
  error = signal('');
  searched = signal(false);

  country = computed(() => this.auth.getCountry());

  totalDemurrage = computed(() =>
    this.charges()
      .filter((c) => c.status === 'Pending')
      .reduce((sum, c) => sum + c.totalAmount, 0),
  );

  ngOnInit(): void {
    const blNumber = this.route.snapshot.paramMap.get('blNumber');
    if (blNumber) {
      this.searchBlNumber = blNumber;
      this.search();
    }
  }

  search(): void {
    const bl = this.searchBlNumber.trim();
    if (!bl) return;

    this.activeBlNumber.set(bl);
    this.loading.set(true);
    this.error.set('');
    this.searched.set(true);

    this.blService.getDemurrage(bl).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.charges.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar la información de demurrage. Verifique el número de BL.');
        this.loading.set(false);
      },
    });
  }
}
