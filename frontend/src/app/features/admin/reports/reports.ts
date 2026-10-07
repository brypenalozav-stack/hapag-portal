import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReportService } from '../../../core/services/report.service';
import { ReportResult, ReportType } from '../../../core/models/report.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { FeatureService } from '../../../core/services/feature.service';

/** Reportes operativos con vista tabular y exportación a CSV (descarga directa). */
@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, LoadingSpinnerComponent],
  templateUrl: './reports.html',
  styles: [':host { display: block; }'],
})
export class ReportsComponent implements OnInit {
  private readonly service = inject(ReportService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly features = inject(FeatureService);

  private readonly allReports: { type: ReportType; labelKey: string }[] = [
    { type: 'transmissions', labelKey: 'admin.reports.types.transmissions' },
    { type: 'overdue-deadlines', labelKey: 'admin.reports.types.overdueDeadlines' },
  ];

  /** El reporte de plazos vencidos depende de los plazos documentales (M2-10), apagados por defecto. */
  get reports(): { type: ReportType; labelKey: string }[] {
    return this.features.enabled('DocumentaryDeadlines') ? this.allReports : this.allReports.filter((r) => r.type !== 'overdue-deadlines');
  }

  selectedType = signal<ReportType>('transmissions');
  result = signal<ReportResult | null>(null);
  loading = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  select(type: ReportType): void {
    this.selectedType.set(type);
    this.load();
  }

  private params(): Record<string, string | boolean> {
    return this.selectedType() === 'overdue-deadlines' ? { includeAtRisk: true } : {};
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.get(this.selectedType(), this.params())
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (r) => { this.result.set(r); this.loading.set(false); },
        error: () => { this.error.set(translate('admin.reports.errors.load')); this.loading.set(false); },
      });
  }

  exportCsv(): void {
    this.service.export(this.selectedType(), this.params())
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `reporte-${this.selectedType()}.csv`;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.error.set(translate('admin.reports.errors.export')),
      });
  }
}
