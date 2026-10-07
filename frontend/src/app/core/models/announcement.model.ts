/** Fase 2, Ola I: comunicados masivos segmentados por país y operación (M1-26). */

export type AnnouncementOperation = 'Import' | 'Export' | 'Both';
export type AnnouncementSeverity = 'Info' | 'Important';
export type AnnouncementStatus = 'Draft' | 'Published' | 'Unpublished';

export const ANNOUNCEMENT_OPERATIONS: readonly AnnouncementOperation[] = ['Both', 'Import', 'Export'];
export const ANNOUNCEMENT_SEVERITIES: readonly AnnouncementSeverity[] = ['Info', 'Important'];
export const ANNOUNCEMENT_STATUSES: readonly AnnouncementStatus[] = ['Draft', 'Published', 'Unpublished'];

/** Comunicado vigente para el cliente (GET /announcements): publicado y dentro de su vigencia. */
export interface Announcement {
  id: string;
  titleEs: string;
  titleEn: string;
  bodyEs: string;
  bodyEn: string;
  countries: ('CL' | 'BO')[];
  operation: AnnouncementOperation;
  severity: AnnouncementSeverity;
  publishedAt: string;
  validFrom?: string | null;
  validTo?: string | null;
}

/** Comunicado del mantenedor interno (`announcements.manage`). */
export interface AnnouncementAdmin extends Omit<Announcement, 'publishedAt'> {
  status: AnnouncementStatus;
  isCurrent: boolean;
  publishedAt?: string | null;
  publishedBy?: string | null;
  unpublishedAt?: string | null;
  unpublishedBy?: string | null;
  notifyOnPublish: boolean;
  notifiedAt?: string | null;
  createdAt: string;
  createdBy?: string | null;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface AnnouncementRequest {
  titleEs: string;
  titleEn: string;
  bodyEs: string;
  bodyEn: string;
  countries: ('CL' | 'BO')[];
  operation: AnnouncementOperation;
  severity?: AnnouncementSeverity;
  validFrom?: string | null;
  validTo?: string | null;
  notifyOnPublish: boolean;
  /** Solo al crear: publica de inmediato. */
  publish?: boolean;
}

/** Valores guardados de un comunicado en su registro de cambios (NF-15). */
export interface AnnouncementSnapshot {
  titleEs?: string;
  titleEn?: string;
  countries?: string[];
  operation?: string;
  severity?: string;
  validFrom?: string | null;
  validTo?: string | null;
  status?: string;
  notifyOnPublish?: boolean;
}
