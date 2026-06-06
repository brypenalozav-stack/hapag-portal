import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ReceiptService, Receipt } from '../../core/services/receipt.service';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [DatePipe, DecimalPipe, CountryBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './receipts.html',
  styleUrl: './receipts.scss',
})
export class ReceiptsComponent implements OnInit {
  private readonly service = inject(ReceiptService);
  private readonly destroyRef = inject(DestroyRef);

  receipts = signal<Receipt[]>([]);
  loading = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getMyReceipts().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.receipts.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los comprobantes.');
        this.loading.set(false);
      },
    });
  }
}
