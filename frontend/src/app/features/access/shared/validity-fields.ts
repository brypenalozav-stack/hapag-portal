import { Component, computed, input, model } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import {
  ACCESS_DURATION_MAX_DAYS,
  ACCESS_DURATION_MIN_DAYS,
  ACCESS_VALIDITY_TYPES,
  AccessValidityType,
} from '../../../core/models/access.model';
import { ACCESS_VALIDITY_TYPE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { ValidityFormValue, todayInput, validateValidity } from './access-form';

/**
 * Vigencia de un acceso (M1-14): sin término, por cantidad de días o hasta una fecha, con inicio
 * opcional. El mandato (M1-03) exige un término (`requireEnd`). Los ids de los campos son
 * `<idPrefix>-validity-<tipo>`, `<idPrefix>-duration`, `<idPrefix>-valid-to` y `<idPrefix>-valid-from`.
 */
@Component({
  selector: 'app-validity-fields',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe],
  templateUrl: './validity-fields.html',
})
export class ValidityFieldsComponent {
  idPrefix = input.required<string>();
  value = model.required<ValidityFormValue>();
  /** El mandato no admite vigencia indefinida. */
  requireEnd = input(false);
  /** Muestra la fecha de inicio (al crear; al editar no se cambia). */
  showStart = input(true);
  /** Muestra los errores (después del primer envío). */
  showErrors = input(false);

  readonly types = ACCESS_VALIDITY_TYPES;
  readonly typeKeys = ACCESS_VALIDITY_TYPE_KEYS;
  readonly minDays = ACCESS_DURATION_MIN_DAYS;
  readonly maxDays = ACCESS_DURATION_MAX_DAYS;
  readonly today = todayInput();

  errors = computed(() => (this.showErrors() ? validateValidity(this.value(), this.requireEnd()) : {}));

  setType(validityType: AccessValidityType): void {
    this.value.update((v) => ({ ...v, validityType }));
  }

  setDuration(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const days = raw === '' ? null : Number(raw);
    this.value.update((v) => ({ ...v, durationDays: days === null || Number.isNaN(days) ? null : days }));
  }

  setValidTo(event: Event): void {
    const validTo = (event.target as HTMLInputElement).value;
    this.value.update((v) => ({ ...v, validTo }));
  }

  setValidFrom(event: Event): void {
    const validFrom = (event.target as HTMLInputElement).value;
    this.value.update((v) => ({ ...v, validFrom }));
  }
}
