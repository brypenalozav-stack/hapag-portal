import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { BlImportService } from '../../../core/services/bl-import.service';
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
  imports: [FormsModule, LoadingSpinnerComponent],
  templateUrl: './bl-import.html',
  styles: [':host { display: block; }'],
})
export class BlImportComponent implements OnInit {
  private readonly service = inject(BlImportService);
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
      error: () => this.parseError.set('No se pudieron cargar los clientes.'),
    });
  }

  parse(): void {
    this.parseError.set('');
    this.result.set(null);

    if (!this.selectedClientId) {
      this.parseError.set('Selecciona el cliente al que pertenecen los BL.');
      return;
    }

    const lines = this.rawText.split('\n').map((l) => l.trim()).filter((l) => l.length > 0);
    if (lines.length === 0) {
      this.parseError.set('Pega al menos una fila de BL.');
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
      this.parseError.set('No se encontraron filas válidas para previsualizar.');
      return;
    }
    this.preview.set(rows);
  }

  submit(): void {
    if (this.preview().length === 0) return;
    this.submitting.set(true);
    this.parseError.set('');
    this.service.import(this.preview()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.result.set(res);
        this.submitting.set(false);
      },
      error: (err) => {
        this.submitting.set(false);
        this.parseError.set(err.error?.detail ?? err.error?.title ?? 'Error al importar los BL.');
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
