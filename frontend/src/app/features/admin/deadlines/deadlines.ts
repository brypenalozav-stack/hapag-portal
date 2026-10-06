import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DeadlineService } from '../../../core/services/deadline.service';
import { DeadlineItem, DeadlineRule } from '../../../core/models/deadline.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { ClientTable } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/**
 * Tablero de control de plazos aduaneros (semáforo). Muestra las instancias calculadas por el
 * motor a partir de las reglas configurables, y permite consultar el catálogo de reglas con su
 * fuente y nivel de certeza.
 */
@Component({
  selector: 'app-deadlines',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, HlDatePipe, LoadingSpinnerComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './deadlines.html',
  styles: [':host { display: block; }'],
})
export class DeadlinesComponent implements OnInit {
  private readonly service = inject(DeadlineService);
  private readonly destroyRef = inject(DestroyRef);

  items = signal<DeadlineItem[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre los plazos cargados. */
  readonly table = new ClientTable(this.items, {
    searchText: (i) => [i.ruleName, i.ruleCode, i.blNumber, i.severity, this.statusText(i.status)].join(' '),
    sortValues: {
      rule: (i) => i.ruleName,
      bl: (i) => i.blNumber,
      severity: (i) => i.severity,
      baseEvent: (i) => i.baseEventAt,
      due: (i) => i.dueAt,
      status: (i) => this.statusText(i.status),
    },
  });
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
        error: () => { this.error.set(translate('admin.deadlines.loadError')); this.loading.set(false); },
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

  /** Clave de traducción del estado; null si el estado no tiene texto (se muestra tal cual). */
  /** Texto del estado para buscar y ordenar. */
  statusText(status: string): string {
    const key = this.statusKey(status);
    return key ? translate(key) : status;
  }

  statusKey(status: string): string | null {
    switch (status) {
      case 'Overdue': return 'admin.deadlines.status.overdue';
      case 'AtRisk': return 'admin.deadlines.status.atRisk';
      case 'Met': return 'admin.deadlines.status.met';
      case 'OnTrack': return 'admin.deadlines.status.onTrack';
      default: return null;
    }
  }
}
