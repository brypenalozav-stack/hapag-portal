import { Component, inject, signal, OnInit, input, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { PaymentService } from '../../../core/services/payment.service';
import { BillOfLadingService } from '../../../core/services/bl.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { BillOfLading } from '../../../core/models/bl.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';
import { TAX_RATES, REDIRECT_DELAY_MS } from '../../../core/constants/app.constants';

@Component({
  selector: 'app-payment-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe, HlCurrencyPipe, HlNumberPipe, LoadingSpinnerComponent],
  templateUrl: './payment-form.html',
  styleUrl: './payment-form.scss',
})
export class PaymentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly paymentService = inject(PaymentService);
  private readonly blService = inject(BillOfLadingService);
  private readonly announcer = inject(LiveAnnouncerService);
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
    const bankTransfer = {
      value: 'BankTransfer',
      labelKey: 'payments.form.methods.bankTransfer.label',
      hintKey: 'payments.form.methods.bankTransfer.hint',
    };
    const creditLine = {
      value: 'CreditLine',
      labelKey: 'payments.form.methods.creditLine.label',
      hintKey: 'payments.form.methods.creditLine.hint',
    };
    if (this.country() === 'CL') {
      return [
        bankTransfer,
        {
          value: 'CreditCard',
          labelKey: 'payments.form.methods.creditCard.label',
          hintKey: 'payments.form.methods.creditCard.hint',
        },
        creditLine,
      ];
    } else {
      return [
        bankTransfer,
        {
          value: 'WebPay',
          labelKey: 'payments.form.methods.qr.label',
          hintKey: 'payments.form.methods.qr.hint',
        },
        creditLine,
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
        this.error.set(translate('payments.form.errors.loadBl'));
        this.loading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
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
          this.announcer.announce(translate('payments.form.success.title'));
          setTimeout(() => this.router.navigate(['/payments']), REDIRECT_DELAY_MS);
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err.error?.message ?? translate('payments.form.errors.submit'));
          this.announcer.announce(this.error(), 'assertive');
        },
      });
  }
}
