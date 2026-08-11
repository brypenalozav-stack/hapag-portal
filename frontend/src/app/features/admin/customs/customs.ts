import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { CustomsService } from '../../../core/services/customs.service';
import { Manifest, Transmission } from '../../../core/models/customs.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

/**
 * Transmisión a Aduana: crea manifiestos, transmite el encabezado y luego los B/L,
 * reintenta rechazos y envía Aclaraciones al Manifiesto (Anexo 4). El acuse (aceptado/
 * rechazado) proviene del backend, que hoy usa un transmisor simulado.
 */
@Component({
  selector: 'app-customs',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent],
  templateUrl: './customs.html',
  styles: [':host { display: block; }'],
})
export class CustomsComponent implements OnInit {
  private readonly service = inject(CustomsService);
  private readonly destroyRef = inject(DestroyRef);

  manifests = signal<Manifest[]>([]);
  transmissions = signal<Transmission[]>([]);
  selected = signal<Manifest | null>(null);
  loading = signal(false);
  busy = signal(false);
  error = signal('');

  // Alta de manifiesto
  newManifest = { vesselImo: '', voyage: '', port: '', direction: 'Ingreso' };
  // Transmisión de B/L y aclaración
  blId = '';
  amendReason = '';
  amendBlId = '';

  ngOnInit(): void {
    this.loadManifests();
  }

  loadManifests(): void {
    this.loading.set(true);
    this.service.getManifests().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (m) => { this.manifests.set(m); this.loading.set(false); },
      error: () => { this.error.set('Error al cargar manifiestos.'); this.loading.set(false); },
    });
  }

  createManifest(): void {
    if (!this.newManifest.vesselImo || !this.newManifest.voyage || this.newManifest.port.length !== 5) {
      this.error.set('IMO, viaje y puerto (UN/LOCODE de 5 caracteres) son obligatorios.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.service.createManifest(this.newManifest).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.newManifest = { vesselImo: '', voyage: '', port: '', direction: 'Ingreso' };
        this.loadManifests();
      },
      error: (e) => { this.busy.set(false); this.error.set(this.msg(e)); },
    });
  }

  select(m: Manifest): void {
    this.selected.set(m);
    this.transmissions.set([]);
    this.error.set('');
    this.service.getTransmissions(m.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (t) => this.transmissions.set(t),
      error: () => this.error.set('Error al cargar transmisiones.'),
    });
  }

  refresh(): void {
    const m = this.selected();
    if (m) this.select(m);
  }

  transmitHeader(): void {
    const m = this.selected();
    if (!m) return;
    this.run(this.service.transmitHeader(m.id));
  }

  transmitBL(): void {
    const m = this.selected();
    if (!m || !this.blId) return;
    this.run(this.service.transmitBL(m.id, this.blId.trim()), () => (this.blId = ''));
  }

  retry(t: Transmission): void {
    this.run(this.service.retry(t.id));
  }

  amend(): void {
    const m = this.selected();
    if (!m || !this.amendReason) return;
    this.run(this.service.amend(m.id, this.amendReason.trim(), this.amendBlId.trim() || undefined), () => {
      this.amendReason = '';
      this.amendBlId = '';
    });
  }

  isTerminal(t: Transmission): boolean {
    return t.status === 'Accepted';
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Accepted': return 'bg-success';
      case 'Rejected': return 'bg-danger';
      case 'Error': return 'bg-danger';
      case 'Sent': case 'Queued': return 'bg-info';
      default: return 'bg-secondary';
    }
  }

  private run(obs: ReturnType<CustomsService['retry']>, onOk?: () => void): void {
    this.busy.set(true);
    this.error.set('');
    obs.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); onOk?.(); this.refresh(); },
      error: (e) => { this.busy.set(false); this.error.set(this.msg(e)); },
    });
  }

  private msg(e: { error?: { detail?: string; title?: string } }): string {
    return e.error?.detail ?? e.error?.title ?? 'Ocurrió un error en la operación.';
  }
}
