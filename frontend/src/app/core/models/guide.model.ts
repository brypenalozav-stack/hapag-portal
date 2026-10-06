/** Fase 2, Ola I: modo guía dentro del portal (M1-27). */

export type GuideAudience = 'Client' | 'Internal' | 'All';
export type GuideUserStatus = 'Completed' | 'Dismissed';

export const GUIDE_AUDIENCES: readonly GuideAudience[] = ['Client', 'Internal', 'All'];

/**
 * Paso de una guía: `elementKey` es el valor del atributo `data-guide-key` del elemento que el paso señala en la
 * pantalla de `route`.
 */
export interface GuideStep {
  order: number;
  route: string;
  elementKey: string;
  titleEs: string;
  titleEn: string;
  textEs: string;
  textEn: string;
}

/** Guía activa para el usuario; `status` null = se ofrece (nueva o con pasos cambiados desde la última vez). */
export interface Guide {
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs?: string | null;
  descriptionEn?: string | null;
  route: string;
  version: number;
  steps: GuideStep[];
  status?: GuideUserStatus | null;
  lastStep?: number | null;
  statusAt?: string | null;
}

/** Guía del mantenedor (`maintainers.manage`). */
export interface GuideDefinition extends Guide {
  id: string;
  audience: GuideAudience;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  createdBy?: string | null;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface GuideRequest {
  code?: string;
  nameEs: string;
  nameEn: string;
  descriptionEs?: string | null;
  descriptionEn?: string | null;
  route: string;
  audience: GuideAudience;
  isActive: boolean;
  displayOrder: number;
  steps: GuideStep[];
}

export interface GuideSnapshot {
  code?: string;
  nameEs?: string;
  nameEn?: string;
  route?: string;
  audience?: string;
  isActive?: boolean;
  displayOrder?: number;
  version?: number;
}
