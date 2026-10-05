import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ApiService } from '../../core/services/api.service';
import { BillOfLadingService } from '../../core/services/bl.service';
import { API_ENDPOINTS } from '../../core/constants/app.constants';

@Component({
  selector: 'app-warehouse',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe],
  template: `
    <div class="hl-page-header">
      <h1>{{ 'warehouse.title' | transloco }}</h1>
      <p class="text-muted mb-0">{{ 'warehouse.subtitle' | transloco }}</p>
    </div>

    @if (success()) {
      <div class="hl-card p-5 text-center">
        <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="mb-3 text-hl-green" viewBox="0 0 16 16">
          <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0m-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
        </svg>
        <h4 class="text-hl-green">{{ 'warehouse.success.title' | transloco }}</h4>
        <p class="text-muted">{{ 'warehouse.success.message' | transloco }}</p>
        <a routerLink="/dashboard" class="btn btn-hl-blue mt-2">{{ 'warehouse.success.backToDashboard' | transloco }}</a>
      </div>
    } @else {
      <div class="row justify-content-center">
        <div class="col-lg-8">
          <div class="hl-card p-4">
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <div class="hl-form-group">
                <label for="blNumber">{{ 'warehouse.blNumber' | transloco }}</label>
                <input type="text" id="blNumber" class="form-control" formControlName="blNumber"
                       [placeholder]="'warehouse.blPlaceholder' | transloco"
                       [class.is-invalid]="form.controls.blNumber.touched && form.controls.blNumber.invalid" />
                @if (form.controls.blNumber.touched && form.controls.blNumber.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'warehouse.blRequired' | transloco }}</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="containerNumber">{{ 'warehouse.containerNumber' | transloco }}</label>
                <input type="text" id="containerNumber" class="form-control" formControlName="containerNumber"
                       [placeholder]="'warehouse.containerPlaceholder' | transloco"
                       [class.is-invalid]="form.controls.containerNumber.touched && form.controls.containerNumber.invalid" />
                @if (form.controls.containerNumber.touched && form.controls.containerNumber.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'warehouse.containerRequired' | transloco }}</div>
                }
              </div>

              <div class="row">
                <div class="col-md-6">
                  <div class="hl-form-group">
                    <label for="currentWarehouse">{{ 'warehouse.currentWarehouse' | transloco }}</label>
                    <input type="text" id="currentWarehouse" class="form-control" formControlName="currentWarehouse"
                           [placeholder]="'warehouse.currentWarehousePlaceholder' | transloco"
                           [class.is-invalid]="form.controls.currentWarehouse.touched && form.controls.currentWarehouse.invalid" />
                    @if (form.controls.currentWarehouse.touched && form.controls.currentWarehouse.errors?.['required']) {
                      <div class="invalid-feedback">{{ 'warehouse.fieldRequired' | transloco }}</div>
                    }
                  </div>
                </div>
                <div class="col-md-6">
                  <div class="hl-form-group">
                    <label for="requestedWarehouse">{{ 'warehouse.requestedWarehouse' | transloco }}</label>
                    <input type="text" id="requestedWarehouse" class="form-control" formControlName="requestedWarehouse"
                           [placeholder]="'warehouse.requestedWarehousePlaceholder' | transloco"
                           [class.is-invalid]="form.controls.requestedWarehouse.touched && form.controls.requestedWarehouse.invalid" />
                    @if (form.controls.requestedWarehouse.touched && form.controls.requestedWarehouse.errors?.['required']) {
                      <div class="invalid-feedback">{{ 'warehouse.fieldRequired' | transloco }}</div>
                    }
                  </div>
                </div>
              </div>

              <div class="hl-form-group">
                <label for="reason">{{ 'warehouse.reason' | transloco }}</label>
                <textarea id="reason" class="form-control" formControlName="reason" rows="3"
                          [placeholder]="'warehouse.reasonPlaceholder' | transloco"
                          [class.is-invalid]="form.controls.reason.touched && form.controls.reason.invalid"></textarea>
                @if (form.controls.reason.touched && form.controls.reason.errors?.['required']) {
                  <div class="invalid-feedback">{{ 'warehouse.reasonRequired' | transloco }}</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="contactPhone">{{ 'warehouse.contactPhone' | transloco }}</label>
                <input type="tel" id="contactPhone" class="form-control" formControlName="contactPhone"
                       [placeholder]="'warehouse.contactPhonePlaceholder' | transloco" />
              </div>

              <div class="hl-form-group">
                <label for="amount">{{ 'warehouse.amount' | transloco }}</label>
                <input type="number" id="amount" class="form-control" formControlName="amount" min="1"
                       [placeholder]="'warehouse.amountPlaceholder' | transloco"
                       [class.is-invalid]="form.controls.amount.touched && form.controls.amount.invalid" />
                @if (form.controls.amount.touched && form.controls.amount.invalid) {
                  <div class="invalid-feedback">{{ 'warehouse.amountInvalid' | transloco }}</div>
                }
              </div>

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="submitting()">
                @if (submitting()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                {{ 'warehouse.submit' | transloco }}
              </button>
            </form>
          </div>
        </div>
      </div>
    }
  `,
  styles: [':host { display: block; }'],
})
export class WarehouseComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly blService = inject(BillOfLadingService);
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group({
    blNumber: ['', Validators.required],
    containerNumber: ['', Validators.required],
    currentWarehouse: ['', Validators.required],
    requestedWarehouse: ['', Validators.required],
    reason: ['', Validators.required],
    contactPhone: [''],
    amount: [0, [Validators.required, Validators.min(1)]],
  });

  submitting = signal(false);
  error = signal('');
  success = signal(false);

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set('');

    // El backend espera fromWarehouse/toWarehouse/billOfLadingId (Guid)/country/amount;
    // se resuelve el BL a partir del número antes de enviar (BUG-13).
    const raw = this.form.getRawValue();
    this.blService.getByNumber(raw.blNumber).pipe(
      switchMap((bl) => this.api.post(API_ENDPOINTS.WAREHOUSE_CHANGES, {
        fromWarehouse: raw.currentWarehouse,
        toWarehouse: raw.requestedWarehouse,
        billOfLadingId: bl.id,
        country: bl.country,
        amount: raw.amount,
      })),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.message ?? translate('warehouse.submitError'));
      },
    });
  }
}
