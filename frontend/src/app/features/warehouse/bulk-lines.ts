import { WarehouseChangeRequest } from '../../core/models/warehouse-change.model';

/** Línea de la lista pegada o cargada que no se pudo interpretar. */
export interface BulkLineIssue {
  lineNumber: number;
  kind: 'missingBl' | 'missingDestination';
}

export interface ParsedBulkLines {
  items: WarehouseChangeRequest[];
  issues: BulkLineIssue[];
}

/** Separadores admitidos entre campos: punto y coma, coma o tabulación (pegado desde una planilla). */
const SEPARATOR = /[;,\t]/;

/** Una primera línea que empieza con "BL" (sin dígitos) es encabezado y se omite. */
const HEADER = /^\s*bl\s*([;,\t]|$)/i;

/**
 * Interpreta la lista de la solicitud masiva (M3-05): una línea por solicitud con
 * `BL;almacén de destino;contenedor;almacén actual`. Solo el BL es obligatorio en la línea: el
 * destino puede venir del valor común del formulario; contenedor y almacén actual son opcionales.
 */
export function parseBulkLines(text: string, defaultDestination: string): ParsedBulkLines {
  const items: WarehouseChangeRequest[] = [];
  const issues: BulkLineIssue[] = [];
  const fallback = defaultDestination.trim();

  text.split(/\r?\n/).forEach((raw, index) => {
    if (raw.trim() === '' || (index === 0 && HEADER.test(raw))) return;
    const [bl, destination, container, from] = raw.split(SEPARATOR).map((f) => f.trim());
    const lineNumber = index + 1;
    if (!bl) {
      issues.push({ lineNumber, kind: 'missingBl' });
      return;
    }
    const toWarehouse = destination || fallback;
    if (!toWarehouse) {
      issues.push({ lineNumber, kind: 'missingDestination' });
      return;
    }
    items.push({
      blNumber: bl.toUpperCase(),
      containerNumber: container ? container.toUpperCase() : null,
      fromWarehouse: from || null,
      toWarehouse,
      tariffCode: null,
    });
  });

  return { items, issues };
}
