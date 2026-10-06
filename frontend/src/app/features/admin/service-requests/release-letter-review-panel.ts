import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { AdminServiceRequestService } from '../../../core/services/service-request.service';
import { ReleaseLetterRequest } from '../../../core/models/document.model';
import { ServiceInputValue } from '../../../core/models/service-request.model';
import { LEGAL_ENTITY_TYPE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { ReleaseLetterTatcComponent } from '../../documents/release-letter/release-letter-tatc';
import { serviceErrorMessage } from '../../service-requests/shared/service-text';

/** Datos de la carta tal como los ingresó el cliente. */
interface ReleaseLetterFields {
  consigneeName: string;
  consigneeTaxId: string;
  consigneeAddress: string;
  legalRepresentativeName: string;
  legalRepresentativeId: string;
  carrierName: string;
  carrierTaxId: string;
  driverName: string;
  driverId: string;
  truckPlate: string;
}

/**
 * Carta de liberación y desconsolidado en la revisión de Customer Service (M6-08): consignatario según el tipo de
 * sociedad, transportista (registrado o datos libres), unidades, la regla de TATC vigente, el TATC al enviar, al aprobar
 * y consultado ahora (M2-09) y el registro de Counter del BL (M8-09: canje, HBL recibido, desconsolidado). Aprobar emite
 * la carta; con la regla activa exige TATC emitido para cada unidad.
 */
@Component({
  selector: 'app-release-letter-review-panel',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ReleaseLetterTatcComponent],
  templateUrl: './release-letter-review-panel.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class ReleaseLetterReviewPanelComponent {
  private readonly service = inject(AdminServiceRequestService);
  private readonly destroyRef = inject(DestroyRef);

  requestId = input.required<string>();
  /** Cambia con cada acción sobre la solicitud (estado o fecha): la carta se vuelve a consultar. */
  version = input<string | null>(null);

  readonly entityKeys = LEGAL_ENTITY_TYPE_KEYS;

  letter = signal<ReleaseLetterRequest | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  /** Datos ingresados por el cliente en el formulario de la carta (claves de ReleaseLetterFields del backend). */
  fields = computed<ReleaseLetterFields>(() => {
    const values = this.letter()?.request.inputValues ?? {};
    const text = (v: ServiceInputValue | undefined) => (Array.isArray(v) ? v.join(', ') : v === undefined || v === null ? '' : String(v));
    return {
      consigneeName: text(values['consigneeName']),
      consigneeTaxId: text(values['consigneeTaxId']),
      consigneeAddress: text(values['consigneeAddress']),
      legalRepresentativeName: text(values['legalRepresentativeName']),
      legalRepresentativeId: text(values['legalRepresentativeId']),
      carrierName: text(values['carrierName']),
      carrierTaxId: text(values['carrierTaxId']),
      driverName: text(values['driverName']),
      driverId: text(values['driverId']),
      truckPlate: text(values['truckPlate']),
    };
  });

  constructor() {
    effect(() => {
      this.requestId();
      this.version();
      untracked(() => this.load());
    });
  }

  load(): void {
    this.loading.set(this.letter() === null);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getReleaseLetter(this.requestId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (letter) => {
        this.letter.set(letter);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(serviceErrorMessage(err, 'admin.serviceRequests.releaseLetter.loadError'));
      },
    });
  }
}
