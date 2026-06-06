import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/app.constants';

@Component({
  selector: 'app-warehouse',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="hl-page-header">
      <h1>Solicitud de Cambio de Almacén</h1>
      <p class="text-muted mb-0">Solicite el cambio de almacén para sus contenedores</p>
    </div>

    @if (success()) {
      <div class="hl-card p-5 text-center">
        <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="#009840" class="mb-3" viewBox="0 0 16 16">
          <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0m-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
        </svg>
        <h4 class="text-hl-green">Solicitud Enviada</h4>
        <p class="text-muted">Su solicitud de cambio de almacén ha sido registrada. Recibirá una confirmación por correo.</p>
        <a routerLink="/dashboard" class="btn btn-hl-blue mt-2">Volver al Dashboard</a>
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
                <label for="blNumber">Número de BL</label>
                <input type="text" id="blNumber" class="form-control" formControlName="blNumber"
                       placeholder="Ej: HLCU1234567890"
                       [class.is-invalid]="form.controls.blNumber.touched && form.controls.blNumber.invalid" />
                @if (form.controls.blNumber.touched && form.controls.blNumber.errors?.['required']) {
                  <div class="invalid-feedback">El número de BL es obligatorio.</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="containerNumber">Número de Contenedor</label>
                <input type="text" id="containerNumber" class="form-control" formControlName="containerNumber"
                       placeholder="Ej: HLXU1234567"
                       [class.is-invalid]="form.controls.containerNumber.touched && form.controls.containerNumber.invalid" />
                @if (form.controls.containerNumber.touched && form.controls.containerNumber.errors?.['required']) {
                  <div class="invalid-feedback">El número de contenedor es obligatorio.</div>
                }
              </div>

              <div class="row">
                <div class="col-md-6">
                  <div class="hl-form-group">
                    <label for="currentWarehouse">Almacén Actual</label>
                    <input type="text" id="currentWarehouse" class="form-control" formControlName="currentWarehouse"
                           placeholder="Nombre del almacén actual"
                           [class.is-invalid]="form.controls.currentWarehouse.touched && form.controls.currentWarehouse.invalid" />
                    @if (form.controls.currentWarehouse.touched && form.controls.currentWarehouse.errors?.['required']) {
                      <div class="invalid-feedback">Este campo es obligatorio.</div>
                    }
                  </div>
                </div>
                <div class="col-md-6">
                  <div class="hl-form-group">
                    <label for="requestedWarehouse">Almacén Solicitado</label>
                    <input type="text" id="requestedWarehouse" class="form-control" formControlName="requestedWarehouse"
                           placeholder="Nombre del almacén destino"
                           [class.is-invalid]="form.controls.requestedWarehouse.touched && form.controls.requestedWarehouse.invalid" />
                    @if (form.controls.requestedWarehouse.touched && form.controls.requestedWarehouse.errors?.['required']) {
                      <div class="invalid-feedback">Este campo es obligatorio.</div>
                    }
                  </div>
                </div>
              </div>

              <div class="hl-form-group">
                <label for="reason">Motivo de la Solicitud</label>
                <textarea id="reason" class="form-control" formControlName="reason" rows="3"
                          placeholder="Describa el motivo del cambio de almacén"
                          [class.is-invalid]="form.controls.reason.touched && form.controls.reason.invalid"></textarea>
                @if (form.controls.reason.touched && form.controls.reason.errors?.['required']) {
                  <div class="invalid-feedback">El motivo es obligatorio.</div>
                }
              </div>

              <div class="hl-form-group">
                <label for="contactPhone">Teléfono de Contacto</label>
                <input type="tel" id="contactPhone" class="form-control" formControlName="contactPhone"
                       placeholder="Teléfono para coordinación" />
              </div>

              <button type="submit" class="btn btn-hl-orange w-100 py-2 mt-2" [disabled]="submitting()">
                @if (submitting()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                }
                Enviar Solicitud
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
  private readonly destroyRef = inject(DestroyRef);

  form = this.fb.nonNullable.group({
    blNumber: ['', Validators.required],
    containerNumber: ['', Validators.required],
    currentWarehouse: ['', Validators.required],
    requestedWarehouse: ['', Validators.required],
    reason: ['', Validators.required],
    contactPhone: [''],
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

    this.api.post(API_ENDPOINTS.WAREHOUSE_CHANGES, this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.message ?? 'Error al enviar la solicitud.');
      },
    });
  }
}
