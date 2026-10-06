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
