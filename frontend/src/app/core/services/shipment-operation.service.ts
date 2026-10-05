import { Injectable, signal } from '@angular/core';
import { ShipmentOperationFilter } from '../models/shipment.model';

const OPERATION_KEY = 'hl_operation';

/**
 * Selección importación/exportación (M2-07). Se mantiene durante la navegación: vive en un
 * signal de la aplicación y en sessionStorage, y el listado la refleja en sus query params.
 */
@Injectable({ providedIn: 'root' })
export class ShipmentOperationService {
  readonly operation = signal<ShipmentOperationFilter>(this.load());

  set(operation: ShipmentOperationFilter): void {
    this.operation.set(operation);
    try {
      sessionStorage.setItem(OPERATION_KEY, operation);
    } catch {
      // Sin almacenamiento disponible: la selección vale mientras la aplicación siga abierta.
    }
  }

  private load(): ShipmentOperationFilter {
    try {
      const stored = sessionStorage.getItem(OPERATION_KEY);
      return stored === 'IMPORT' || stored === 'EXPORT' ? stored : '';
    } catch {
      return '';
    }
  }
}
