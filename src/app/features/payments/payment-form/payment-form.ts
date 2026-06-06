import { Component, inject, signal, OnInit, input, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { PaymentService } from '../../../core/services/payment.service';
import { BillOfLadingService } from '../../../core/services/bl.service';
import { BillOfLading } from '../../../core/models/bl.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { TAX_RATES, REDIRECT_DELAY_MS } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-payment-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './payment-form.html',
  styleUrl: './payment-form.scss',
})
export class PaymentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly paymentService = inject(PaymentService);
  private readonly blService = inject(BillOfLadingService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  blId = input.required<string>();

  bl = signal<BillOfLading | null>(null);
  loading = signal(true);
  submitting = signal(false);
  error = signal('');
  success = signal(false);

  form = this.fb.nonNullable.group({
    type: ['Freight' as string, Validators.required],
    method: ['' as string, Validators.required],
  });

  country = computed(() => this.auth.getCountry());

  paymentMethods = computed(() => {
    if (this.country() === 'CL') {
      return [
        { value: 'BankTransfer', label: 'Transferencia Bancaria' },
        { value: 'CreditCard', label: 'Tarjeta de Credito' },
        { value: 'CreditLine', label: 'Linea de Credito' },
      ];
    } else {
      return [
        { value: 'BankTransfer', label: 'Transferencia Bancaria' },
        { value: 'WebPay', label: 'Pago QR' },
        { value: 'CreditLine', label: 'Linea de Credito' },
      ];
    }
  });

  taxRate = computed(() => TAX_RATES[this.country()]);

  amount = computed(() => {
    const b = this.bl();
    if (!b) return 0;
    return b.freightAmount;
  });

  taxAmount = computed(() => this.amount() * this.taxRate());
  totalAmount = computed(() => this.amount() + this.taxAmount());

  ngOnInit(): void {
    const queryType = this.route.snapshot.queryParamMap.get('type');
    if (queryType) {
      this.form.controls.type.setValue(queryType);
    }

    this.loading.set(true);
    this.blService.getMyBLs().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (bls) => {
        const found = bls.find((b) => b.id === this.blId());
        if (found) {
          this.bl.set(found);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar la informacion del BL.');
        this.loading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set('');

    const formValue = this.form.getRawValue();
    this.paymentService
      .create({
        blId: this.blId(),
        type: formValue.type,
        method: formValue.method,
        country: this.country(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.success.set(true);
          setTimeout(() => this.router.navigate(['/payments']), REDIRECT_DELAY_MS);
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err.error?.message ?? 'Error al procesar el pago.');
        },
      });
  }
}
