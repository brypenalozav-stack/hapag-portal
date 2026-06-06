import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { Payment } from '../../../core/models/payment.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { FILTER_ALL } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-payment-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe, StatusBadgeComponent, LoadingSpinnerComponent],
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
        this.error.set('Error al cargar los pagos.');
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

  getTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      FREIGHT: 'Flete',
      LOCAL_CHARGES: 'Cargos Locales',
      DEMURRAGE: 'Demurrage',
      Freight: 'Flete',
      LocalCharges: 'Cargos Locales',
      Demurrage: 'Demurrage',
      Combined: 'Combinado',
    };
    return labels[type] ?? type;
  }

  getMethodLabel(method: string): string {
    const labels: Record<string, string> = {
      BANK_TRANSFER: 'Transferencia',
      CREDIT_CARD: 'Tarjeta de Crédito',
      CREDIT_LINE: 'Línea de Crédito',
      QR_PAYMENT: 'Pago QR',
      BankTransfer: 'Transferencia',
      CreditCard: 'Tarjeta de Crédito',
      DebitCard: 'Tarjeta de Débito',
      WebPay: 'WebPay',
      Cash: 'Efectivo',
      Check: 'Cheque',
      Khipu: 'Khipu',
      CreditLine: 'Línea de Crédito',
      Deposit: 'Depósito',
      BankDeposit: 'Depósito Bancario',
    };
    return labels[method] ?? method;
  }
}
