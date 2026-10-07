import { Component, computed, input, model } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GrantableAction, SHIPMENT_VIEW_ACTION } from '../../../core/models/access.model';
import { ACCESS_ACTION_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { PermissionsFormValue } from './access-form';

/**
 * Selector de permisos del acceso (M1-15): nivel base de M1-11 o un conjunto explícito. Solo se
 * pueden marcar las acciones que el otorgante posee y el tercero puede recibir (`grantable`); el
 * resto aparece deshabilitado con el motivo. La consulta del BL siempre se incluye.
 */
@Component({
  selector: 'app-permission-checklist',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent],
  templateUrl: './permission-checklist.html',
})
export class PermissionChecklistComponent {
  idPrefix = input.required<string>();
  value = model.required<PermissionsFormValue>();
  actions = input<GrantableAction[]>([]);
  loading = input(false);
  /** El mandato exige permisos explícitos (M1-03): sin opción de nivel base. */
  requireExplicit = input(false);
  /** Mensaje cuando aún no se puede consultar el catálogo (p. ej. sin destinatario). */
  pendingKey = input<string | null>(null);

  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly viewAction = SHIPMENT_VIEW_ACTION;

  mode = computed(() => (this.requireExplicit() ? 'custom' : this.value().mode));
  selected = computed(() => new Set(this.value().codes));
  baseLevel = computed(() => this.actions().filter((a) => a.includedInBaseLevel));
  information = computed(() => this.actions().filter((a) => a.category === 'Information'));
  administration = computed(() => this.actions().filter((a) => a.category === 'Administration'));

  setMode(mode: 'base' | 'custom'): void {
    this.value.update((v) => ({ ...v, mode }));
  }

  isChecked(action: GrantableAction): boolean {
    return action.code === SHIPMENT_VIEW_ACTION || this.selected().has(action.code);
  }

  isDisabled(action: GrantableAction): boolean {
    return action.code === SHIPMENT_VIEW_ACTION || !action.grantable;
  }

  toggle(action: GrantableAction, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.value.update((v) => {
      const codes = new Set(v.codes);
      if (checked) codes.add(action.code);
      else codes.delete(action.code);
      return { ...v, codes: [...codes] };
    });
  }

  /** Motivo por el que una acción no se puede marcar. */
  reasonKey(action: GrantableAction): string | null {
    if (action.code === SHIPMENT_VIEW_ACTION) return 'thirdPartyAccess.permissions.alwaysIncluded';
    if (!action.grantorHas) return 'thirdPartyAccess.permissions.notOwned';
    if (!action.grantable) return 'thirdPartyAccess.permissions.notGrantable';
    return null;
  }
}
