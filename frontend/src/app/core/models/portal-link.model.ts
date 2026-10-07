/**
 * Enlace al módulo de Dispute de productos digitales de Hapag-Lloyd (Fase 1, Ola F, M2-05). La URL se configura
 * por país sin desplegar; `configured: false` (sin `url`) oculta el acceso.
 */
export interface DisputeLink {
  country: 'CL' | 'BO';
  url?: string | null;
  configured: boolean;
  /** `Setting`, `AppSettings` o `None`. */
  source: string;
  opensInNewTab: boolean;
}

/** Tarifarios oficiales de Hapag-Lloyd que enlaza la página de Tarifas locales. */
export type LocalTariffCode = 'INLAND_CL' | 'DEMURRAGE_DETENTION' | 'LOCAL_CHARGES';

/** Enlace externo configurado por país; `configured: false` (sin `url`) muestra que aún no está disponible. */
export interface ExternalLink<TCode extends string = string> {
  code: TCode;
  url?: string | null;
  configured: boolean;
  /** `Setting`, `AppSettings` o `None`. */
  source: string;
}

/**
 * GET /config/external-links: portal de devoluciones de dinero (se muestra incrustado, con apertura en una ventana
 * nueva) y tarifarios oficiales (se abren en una pestaña nueva).
 */
export interface ExternalLinks {
  country: string;
  refunds: ExternalLink<'REFUNDS'>;
  localTariffs: ExternalLink<LocalTariffCode>[];
}
