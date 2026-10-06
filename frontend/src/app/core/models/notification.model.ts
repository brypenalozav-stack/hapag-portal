/** Módulo de origen de una notificación, para filtrar la bandeja (Fase 2, Ola I, M1-25). */
export type NotificationModule =
  | 'Organization'
  | 'Access'
  | 'Payments'
  | 'Documents'
  | 'Services'
  | 'Finance'
  | 'Customs'
  | 'Deadlines'
  | 'Announcements'
  | 'Administration'
  | 'General';

export const NOTIFICATION_MODULES: readonly NotificationModule[] = [
  'Organization',
  'Access',
  'Payments',
  'Documents',
  'Services',
  'Finance',
  'Customs',
  'Deadlines',
  'Announcements',
  'Administration',
  'General',
];

/** Gestión o elemento al que corresponde la notificación: identifica el embarque o la gestión (M1-25). */
export interface NotificationLink {
  entityType: string;
  entityId?: string | null;
  reference?: string | null;
  blNumber?: string | null;
}

/**
 * Acción que se toma desde la notificación (M1-25). `available` pasa a false cuando la gestión pendiente se resolvió por
 * cualquier vía; el servidor vuelve a autorizar cada acción.
 */
export interface NotificationAction {
  type: string;
  targetId?: string | null;
  available: boolean;
  resolvedAt?: string | null;
}

export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
  module?: NotificationModule | string;
  readAt?: string | null;
  link?: NotificationLink | null;
  action?: NotificationAction | null;
  emailSent?: boolean;
}

/** Filtros de la bandeja (GET /notifications). */
export interface NotificationFilters {
  onlyUnread?: boolean;
  type?: string;
  module?: string;
  blNumber?: string;
  onlyActionable?: boolean;
  limit?: number;
}

/** No leídas, por módulo y las que esperan una acción (GET /notifications/unread-count). */
export interface UnreadCount {
  count: number;
  byModule?: Record<string, number>;
  actionable?: number;
}

/** Preferencia de correo por tipo (M1-25): la bandeja siempre recibe la notificación. */
export interface NotificationPreference {
  type: string;
  module: string;
  emailAvailable: boolean;
  emailMandatory: boolean;
  emailDefault: boolean;
  emailEnabled: boolean;
  isCustomized: boolean;
}

/** `emailEnabled: null` vuelve al valor por defecto del tipo. */
export interface NotificationPreferenceUpdate {
  type: string;
  emailEnabled: boolean | null;
}
