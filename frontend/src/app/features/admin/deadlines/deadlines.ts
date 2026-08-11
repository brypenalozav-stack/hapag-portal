import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DeadlineService } from '../../../core/services/deadline.service';
import { DeadlineItem, DeadlineRule } from '../../../core/models/deadline.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

/**
 * Tablero de control de plazos aduaneros (semáforo). Muestra las instancias calculadas por el
 * motor a partir de las reglas configurables, y permite consultar el catálogo de reglas con su
 * fuente y nivel de certeza.
 */
@Component({
  selector: 'app-deadlines',
  standalone: true,
  imports: [FormsModule, DatePipe, LoadingSpinnerComponent],
  templateUrl: './deadlines.html',
  styles: [':host { display: block; }'],
})
export class DeadlinesComponent implements OnInit {
  private readonly service = inject(DeadlineService);
  private readonly destroyRef = inject(DestroyRef);

  items = signal<DeadlineItem[]>([]);
  rules = signal<DeadlineRule[]>([]);
  loading = signal(false);
  error = signal('');
  statusFilter = '';
  showRules = signal(false);

  counts = computed(() => {
    const c = { OnTrack: 0, AtRisk: 0, Overdue: 0, Met: 0 } as Record<string, number>;
    for (const i of this.items()) c[i.status] = (c[i.status] ?? 0) + 1;
    return c;
  });

  ngOnInit(): void {
    this.load();
    this.service.getRules().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => this.rules.set(r),
      error: () => { /* catálogo no crítico */ },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.getDashboard(this.statusFilter || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (i) => { this.items.set(i); this.loading.set(false); },
        error: () => { this.error.set('Error al cargar el tablero de plazos.'); this.loading.set(false); },
      });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Overdue': return 'bg-danger';
      case 'AtRisk': return 'bg-warning text-dark';
      case 'Met': return 'bg-success';
      default: return 'bg-secondary';
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'Overdue': return 'Vencido';
      case 'AtRisk': return 'En riesgo';
      case 'Met': return 'Cumplido';
      case 'OnTrack': return 'En plazo';
      default: return status;
    }
  }
}
