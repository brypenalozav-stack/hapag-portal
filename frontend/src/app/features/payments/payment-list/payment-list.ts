import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { PaymentService } from '../../../core/services/payment.service';
import { Payment } from '../../../core/models/payment.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { FILTER_ALL } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-payment-list',
  standalone: true,
  imports: [
    RouterLink, FormsModule, TranslocoPipe, HlCurrencyPipe, HlDatePipe,
    StatusBadgeComponent, LoadingSpinnerComponent,
  ],
  templateUrl: './payment-list.html',
  styleUrl: './payment-list.scss',
})
export class PaymentListComponent implements OnInit {
  private readonly paymentService = inject(PaymentService);
  private readonly destroyRef = inject(DestroyRef);

  payments = signal<Payment[]>([]);
  filteredPayments = signal<Payment[]>([]);
  loading = signal(false);
  error = signal('');
  statusFilter = signal(FILTER_ALL);

  ngOnInit(): void {
    this.loadPayments();
  }

  loadPayments(): void {
    this.loading.set(true);
    this.error.set('');
    this.paymentService.getAll().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.payments.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.error.set(translate('payments.list.loadError'));
        this.loading.set(false);
      },
    });
  }

  onFilterChange(status: string): void {
    this.statusFilter.set(status);
    this.applyFilter();
  }

  private applyFilter(): void {
    const filter = this.statusFilter();
    if (filter === FILTER_ALL) {
      this.filteredPayments.set(this.payments());
    } else {
      this.filteredPayments.set(this.payments().filter((p) => p.status === filter));
    }
  }

  /** Clave de traducción del tipo de pago; null si el código no tiene texto (se muestra tal cual). */
  typeKey(type: string): string | null {
    const keys: Record<string, string> = {
      FREIGHT: 'payments.list.type.freight',
      LOCAL_CHARGES: 'payments.list.type.localCharges',
      DEMURRAGE: 'payments.list.type.demurrage',
      Freight: 'payments.list.type.freight',
      LocalCharges: 'payments.list.type.localCharges',
      Demurrage: 'payments.list.type.demurrage',
      Combined: 'payments.list.type.combined',
    };
    return keys[type] ?? null;
  }

  /** Clave de traducción del medio de pago; null si el código no tiene texto (se muestra tal cual). */
  methodKey(method: string): string | null {
    const keys: Record<string, string> = {
      BANK_TRANSFER: 'payments.list.method.bankTransfer',
      CREDIT_CARD: 'payments.list.method.creditCard',
      CREDIT_LINE: 'payments.list.method.creditLine',
      QR_PAYMENT: 'payments.list.method.qr',
      BankTransfer: 'payments.list.method.bankTransfer',
      CreditCard: 'payments.list.method.creditCard',
      DebitCard: 'payments.list.method.debitCard',
      WebPay: 'payments.list.method.webPay',
      Cash: 'payments.list.method.cash',
      Check: 'payments.list.method.check',
      Khipu: 'payments.list.method.khipu',
      CreditLine: 'payments.list.method.creditLine',
      Deposit: 'payments.list.method.deposit',
      BankDeposit: 'payments.list.method.bankDeposit',
    };
    return keys[method] ?? null;
  }
}
