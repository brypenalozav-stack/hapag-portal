import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLogItem } from '../../../core/models/audit.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

/** Consulta de auditoría sobre el registro de escrituras (AuditLog) con filtros y detalle old/new. */
@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [FormsModule, DatePipe, LoadingSpinnerComponent],
  templateUrl: './audit.html',
  styles: [':host { display: block; }'],
})
export class AuditComponent implements OnInit {
  private readonly service = inject(AuditService);
  private readonly destroyRef = inject(DestroyRef);

  items = signal<AuditLogItem[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = 20;
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
      pageSize: this.pageSize,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => { this.items.set(res.items); this.total.set(res.total); this.loading.set(false); },
      error: () => { this.error.set('Error al cargar la auditoría.'); this.loading.set(false); },
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  toggle(id: string): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.page.set(next);
    this.load();
  }
}
