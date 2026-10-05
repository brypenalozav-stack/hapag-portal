import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReceiptService, Receipt } from '../../core/services/receipt.service';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [TranslocoPipe, HlCurrencyPipe, HlDatePipe, CountryBadgeComponent, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './receipts.html',
  styleUrl: './receipts.scss',
})
export class ReceiptsComponent implements OnInit {
  private readonly service = inject(ReceiptService);
  private readonly destroyRef = inject(DestroyRef);

  receipts = signal<Receipt[]>([]);
  loading = signal(false);
  error = signal('');
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  ngOnInit(): void {
    this.loadReceipts();
  }

  loadReceipts(): void {
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.getMyReceipts().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.receipts.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('receipts.loadError'));
        }
        this.loading.set(false);
      },
    });
  }
}
