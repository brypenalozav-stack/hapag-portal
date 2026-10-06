import { Component, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LocaleService } from '../../../core/services/locale.service';
import {
  ServiceInputField,
  ServiceInputValues,
  ServiceRequestAttachment,
} from '../../../core/models/service-request.model';
import { displayValue, fieldLabel } from './service-text';

/**
 * Datos ingresados en el formulario de un servicio, leídos según su definición: selecciones con su texto en el idioma
 * activo, contenedores elegidos y, en los campos de archivo, los adjuntos de esa clave (o los archivos elegidos aún
 * sin subir, en la revisión antes de enviar).
 */
@Component({
  selector: 'app-service-input-values',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    @if (schema().length > 0) {
      <dl class="row g-3 mb-0" data-testid="service-input-values">
        @for (field of schema(); track field.key) {
          <div class="col-sm-6">
            <dt class="small fw-semibold text-muted">{{ label(field) }}</dt>
            <dd class="mb-0 text-break">
              @if (field.type === 'file') {
                @for (name of fileNames(field); track $index) {
                  <span class="d-block">{{ name }}</span>
                } @empty {
                  <span aria-hidden="true">—</span>
                  <span class="visually-hidden">{{ 'serviceRequests.values.noFile' | transloco }}</span>
                }
              } @else if (value(field); as text) {
                <span class="hl-pre-line">{{ text }}</span>
              } @else {
                <span aria-hidden="true">—</span>
                <span class="visually-hidden">{{ 'serviceRequests.values.empty' | transloco }}</span>
              }
            </dd>
          </div>
        }
      </dl>
    } @else {
      <p class="text-muted mb-0">{{ 'serviceRequests.values.noFields' | transloco }}</p>
    }
  `,
  styles: [':host { display: block; } .hl-pre-line { white-space: pre-line; }'],
})
export class ServiceInputValuesComponent {
  private readonly locale = inject(LocaleService);

  schema = input.required<ServiceInputField[]>();
  values = input.required<ServiceInputValues>();
  attachments = input<ServiceRequestAttachment[]>([]);
  /** Archivos elegidos y aún no subidos, por clave del campo. */
  pendingFiles = input<Record<string, File | null>>({});

  label(field: ServiceInputField): string {
    return fieldLabel(field, this.locale.lang());
  }

  value(field: ServiceInputField): string {
    return displayValue(field, this.values()[field.key], this.locale.lang());
  }

  fileNames(field: ServiceInputField): string[] {
    const pending = this.pendingFiles()[field.key];
    const uploaded = this.attachments().filter((a) => a.fieldKey === field.key).map((a) => a.fileName);
    return pending ? [...uploaded, pending.name] : uploaded;
  }
}
