import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLogItem } from '../../../core/models/audit.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/** Consulta de auditoría sobre el registro de escrituras (AuditLog) con filtros y detalle old/new. */
@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, HlDatePipe, LoadingSpinnerComponent, PaginatorComponent],
  templateUrl: './audit.html',
  styles: [':host { display: block; }'],
})
export class AuditComponent implements OnInit {
  private readonly service = inject(AuditService);
  private readonly destroyRef = inject(DestroyRef);

  items = signal<AuditLogItem[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = signal(20);
  loading = signal(false);
  error = signal('');
  expanded = signal<string | null>(null);

  entityName = '';
  userId = '';
  action = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.search({
      entityName: this.entityName || undefined,
      userId: this.userId || undefined,
      action: this.action || undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => { this.items.set(res.items); this.total.set(res.total); this.loading.set(false); },
      error: () => { this.error.set(translate('admin.audit.loadError')); this.loading.set(false); },
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  toggle(id: string): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  changePage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Otro tamaño de página vuelve a la primera página. */
  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }
}
