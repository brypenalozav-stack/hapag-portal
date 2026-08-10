import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ServiceOrderService, ServiceOrder } from '../../core/services/service-order.service';
import { BillOfLadingService } from '../../core/services/bl.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-service-orders',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, StatusBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './service-orders.html',
  styleUrl: './service-orders.scss',
})
export class ServiceOrdersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ServiceOrderService);
  private readonly blService = inject(BillOfLadingService);
  private readonly destroyRef = inject(DestroyRef);

  orders = signal<ServiceOrder[]>([]);
  loading = signal(false);
  error = signal('');
  showForm = signal(false);
  submitting = signal(false);
  formError = signal('');
  formSuccess = signal('');

  form = this.fb.nonNullable.group({
    blNumber: ['', Validators.required],
    type: ['', Validators.required],
    description: ['', Validators.required],
  });

  orderTypes = [
    { value: 'RELEASE', label: 'Liberación de Carga' },
    { value: 'INSPECTION', label: 'Inspección de Contenedor' },
    { value: 'WEIGHING', label: 'Pesaje VGM' },
    { value: 'FUMIGATION', label: 'Fumigación' },
    { value: 'CONSOLIDATION', label: 'Consolidación' },
    { value: 'DECONSOLIDATION', label: 'Desconsolidación' },
    { value: 'OTHER', label: 'Otro' },
  ];

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.getMyOrders().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.orders.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar las órdenes de servicio.');
        this.loading.set(false);
      },
    });
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
    this.formError.set('');
    this.formSuccess.set('');
    if (this.showForm()) {
      this.form.reset();
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set('');
    this.formSuccess.set('');

    // El backend necesita el Guid del BL y el país; se resuelven a partir del
    // número de BL antes de crear la orden (BUG-13).
    const raw = this.form.getRawValue();
    this.blService.getByNumber(raw.blNumber).pipe(
      switchMap((bl) => this.service.create({
        orderType: raw.type,
        description: raw.description,
        billOfLadingId: bl.id,
        country: bl.country,
      })),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (order) => {
        this.submitting.set(false);
        this.formSuccess.set(`Orden ${order.orderNumber ?? order.id} creada exitosamente.`);
        this.form.reset();
        this.loadOrders();
        setTimeout(() => this.showForm.set(false), 2000);
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.message ?? 'Error al crear la orden de servicio.');
      },
    });
  }

  getTypeLabel(value: string): string {
    return this.orderTypes.find((t) => t.value === value)?.label ?? value;
  }
}
