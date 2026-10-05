import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { BlImportService } from '../../../core/services/bl-import.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { ClientOption, ImportBillRow, ImportResult } from '../../../core/models/bl-import.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

/**
 * Carga masiva de Bill of Lading. El usuario pega filas (una por línea, campos
 * separados por punto y coma) y selecciona el cliente al que pertenecen. Las
 * validaciones aduaneras (RUT, HS, UN/LOCODE, país) las realiza el backend por
 * fila y se muestran los errores detallados.
 */
@Component({
  selector: 'app-bl-import',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, LoadingSpinnerComponent],
  templateUrl: './bl-import.html',
  styles: [':host { display: block; }'],
})
export class BlImportComponent implements OnInit {
  private readonly service = inject(BlImportService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  // Orden de columnas esperado en el texto pegado.
  readonly columns = [
    'blNumber', 'shipmentType', 'country', 'portOfLoading', 'portOfDischarge',
    'freightCurrency', 'consigneeName', 'consigneeTaxId', 'hsCode',
    'grossWeight', 'containerNumber', 'containerIsoType',
  ];

  clients = signal<ClientOption[]>([]);
  selectedClientId = '';
  rawText = '';

  preview = signal<ImportBillRow[]>([]);
  parseError = signal('');
  loading = signal(false);
  submitting = signal(false);
  result = signal<ImportResult | null>(null);

  ngOnInit(): void {
    this.service.getClients().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (c) => this.clients.set(c),
      error: () => this.parseError.set(translate('admin.blImport.errors.loadClients')),
    });
  }

  parse(): void {
    this.parseError.set('');
    this.result.set(null);

    if (!this.selectedClientId) {
      this.parseError.set(translate('admin.blImport.errors.selectClient'));
      this.announcer.announce(this.parseError(), 'assertive');
      return;
    }

    const lines = this.rawText.split('\n').map((l) => l.trim()).filter((l) => l.length > 0);
    if (lines.length === 0) {
      this.parseError.set(translate('admin.blImport.errors.noRows'));
      this.announcer.announce(this.parseError(), 'assertive');
      return;
    }

    const rows: ImportBillRow[] = [];
    for (const line of lines) {
      const parts = line.split(';').map((p) => p.trim());
      // Ignora una eventual fila de encabezado.
      if (parts[0]?.toLowerCase() === 'blnumber') continue;
      const grossWeight = parts[9] ? Number(parts[9]) : undefined;
      rows.push({
        clientId: this.selectedClientId,
        blNumber: parts[0] ?? '',
        shipmentType: parts[1] || 'Import',
        country: parts[2] || 'CL',
        portOfLoading: parts[3] || undefined,
        portOfDischarge: parts[4] || undefined,
        freightCurrency: parts[5] || 'USD',
        consigneeName: parts[6] || undefined,
        consigneeTaxId: parts[7] || undefined,
        hsCode: parts[8] || undefined,
        grossWeight: Number.isFinite(grossWeight) ? grossWeight : undefined,
        containerNumber: parts[10] || undefined,
        containerIsoType: parts[11] || undefined,
      });
    }

    if (rows.length === 0) {
      this.parseError.set(translate('admin.blImport.errors.noValidRows'));
      this.announcer.announce(this.parseError(), 'assertive');
      return;
    }
    this.preview.set(rows);
  }

  submit(): void {
    if (this.preview().length === 0) return;
    this.submitting.set(true);
    this.parseError.set('');
    this.announcer.announce(translate('admin.blImport.announce.started', { count: this.preview().length }));
    this.service.import(this.preview()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.result.set(res);
        this.submitting.set(false);
        this.announcer.announce(
          translate('admin.blImport.announce.finished', { created: res.created, failed: res.failed }),
        );
      },
      error: (err) => {
        this.submitting.set(false);
        this.parseError.set(err.error?.detail ?? err.error?.title ?? translate('admin.blImport.errors.import'));
        this.announcer.announce(this.parseError(), 'assertive');
      },
    });
  }

  reset(): void {
    this.rawText = '';
    this.preview.set([]);
    this.result.set(null);
    this.parseError.set('');
  }
}
