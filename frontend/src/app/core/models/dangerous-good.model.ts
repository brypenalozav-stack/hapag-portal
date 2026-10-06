/**
 * Buscador de clasificación de mercancías peligrosas (Fase 1, Ola F, M10-06): consulta libre por nombre,
 * descripción o número ONU, sin BL ni booking. El resultado es informativo y no constituye la aprobación
 * operacional del embarque. Las propiedades en null no llegan en el JSON (el backend las omite).
 */

/** `NOT_CLASSIFIED`: la carga figura como no peligrosa; `NO_MATCH`: sin clasificación en la base de referencia. */
export type DangerousGoodResultCode = 'CLASSIFIED' | 'NOT_CLASSIFIED' | 'NO_MATCH';

export interface DangerousGood {
  id: string;
  unNumber?: string | null;
  properShippingNameEs: string;
  properShippingNameEn: string;
  hazardClass?: string | null;
  hazardClassNameEs?: string | null;
  hazardClassNameEn?: string | null;
  subsidiaryRisk?: string | null;
  packingGroup?: string | null;
  notes?: string | null;
  classified: boolean;
  source: string;
}

export interface DangerousGoodSearchResult {
  query: string;
  resultCode: DangerousGoodResultCode;
  classified: boolean;
  /** Explicación del resultado (en español, del servidor). */
  message: string;
  items: DangerousGood[];
  disclaimer: string;
  dataSource: string;
}

/** Largo admitido de la consulta. */
export const DANGEROUS_GOOD_QUERY_MIN = 2;
export const DANGEROUS_GOOD_QUERY_MAX = 100;

export interface DangerousGoodImportError {
  line: number;
  reason: string;
}

/** Resultado de la carga del CSV de la base de referencia (mantenedor interno). */
export interface DangerousGoodImportResult {
  totalRows: number;
  created: number;
  updated: number;
  skipped: number;
  errors: DangerousGoodImportError[];
}
