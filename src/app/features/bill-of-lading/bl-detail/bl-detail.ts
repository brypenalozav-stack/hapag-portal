import { Component, inject, signal, OnInit, input, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { BillOfLadingService } from '../../../core/services/bl.service';
import { BillOfLading, LocalCharge, DemurrageCharge } from '../../../core/models/bl.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-bl-detail',
  standalone: true,
  imports: [RouterLink, DecimalPipe, DatePipe, StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './bl-detail.html',
  styleUrl: './bl-detail.scss',
})
export class BLDetailComponent implements OnInit {
  private readonly blService = inject(BillOfLadingService);
  private readonly destroyRef = inject(DestroyRef);

  blNumber = input.required<string>();

  bl = signal<BillOfLading | null>(null);
  charges = signal<LocalCharge[]>([]);
  demurrage = signal<DemurrageCharge[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.loadBL();
  }

  private loadBL(): void {
    this.loading.set(true);
    this.blService.getByNumber(this.blNumber()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (bl) => {
        this.bl.set(bl);
        this.loading.set(false);
        this.loadCharges(bl.blNumber);
        this.loadDemurrage(bl.blNumber);
      },
      error: () => {
        this.error.set('Error al cargar el BL.');
        this.loading.set(false);
      },
    });
  }

  private loadCharges(blNumber: string): void {
    this.blService.getCharges(blNumber).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (response) => this.charges.set(response.localCharges ?? []),
      error: () => this.error.set('Error al cargar los cargos.'),
    });
  }

  private loadDemurrage(blNumber: string): void {
    this.blService.getDemurrage(blNumber).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (dem) => this.demurrage.set(dem),
      error: () => this.error.set('Error al cargar demurrage.'),
    });
  }

  get totalCharges(): number {
    return this.charges().reduce((sum, c) => sum + c.totalAmount, 0);
  }

  get totalDemurrage(): number {
    return this.demurrage().reduce((sum, d) => sum + d.totalAmount, 0);
  }
}
