import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ShipmentDocumentsComponent } from './shipment-documents/shipment-documents';

/**
 * Repositorio documental de un embarque en su propia página (M6-09): el enlace directo
 * (/shipments/:blNumber/documents) se usa desde el resultado del pago y las notificaciones de
 * documentos emitidos. Es la misma sección "Documentos" del detalle del embarque.
 */
@Component({
  selector: 'app-shipment-documents-page',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, ShipmentDocumentsComponent],
  template: `
    <div class="hl-page-header">
      <nav [attr.aria-label]="'documents.page.breadcrumbLabel' | transloco">
        <ol class="breadcrumb">
          <li class="breadcrumb-item"><a routerLink="/shipments">{{ 'documents.page.breadcrumbList' | transloco }}</a></li>
          <li class="breadcrumb-item"><a [routerLink]="['/shipments', blNumber()]">{{ blNumber() }}</a></li>
          <li class="breadcrumb-item active" aria-current="page">{{ 'documents.page.breadcrumbCurrent' | transloco }}</li>
        </ol>
      </nav>
      <h1>{{ 'documents.page.title' | transloco: { bl: blNumber() } }}</h1>
    </div>

    <app-shipment-documents [blNumber]="blNumber()" />
  `,
  styles: [':host { display: block; }'],
})
export class ShipmentDocumentsPageComponent {
  blNumber = input.required<string>();
}
