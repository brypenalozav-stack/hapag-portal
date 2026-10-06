/**
 * Fase 2, Ola I: listas de distribución de contactos (M1-06), transportistas pre-creados (M1-09) y visibilidad hacia la
 * empresa matriz (M1-21), en Mi organización; y la revisión interna de las vinculaciones con la matriz.
 */

export type ContactReportType = 'ARRIVAL_NOTICE' | 'BL_COPIES' | 'INVOICES' | 'FREE_TIME' | 'DEMURRAGE' | 'BOOKING_CONFIRMATION';

/** Máximo de correos por lista (M1-06). */
export const CONTACT_LIST_MAX_EMAILS = 20;

export interface ContactList {
  reportType: ContactReportType | string;
  emails: string[];
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/** GET /organizations/me/contact-lists: `available=false` si el sistema de origen no respondió. */
export interface ContactListsView {
  organizationId: string;
  matchCode?: string | null;
  available: boolean;
  errorCode?: string | null;
  canEdit: boolean;
  reportTypes: string[];
  lists: ContactList[];
}

export interface ContactListChange {
  id: string;
  reportType: string;
  previousEmails: string[];
  newEmails: string[];
  status: 'Propagated' | 'Failed' | string;
  sourceReference?: string | null;
  errorCode?: string | null;
  changedBy?: string | null;
  changedAt: string;
}

export interface CarrierAssignment {
  grantId: string;
  blNumber?: string | null;
  bookingNumber?: string | null;
  status: string;
  createdAt: string;
}

export interface CarrierPreRegistration {
  id: string;
  carrier: { id: string; name: string; organizationType: string; country: string };
  legalName: string;
  taxId: string;
  email: string;
  country: string;
  status: 'Pending' | 'Activated' | string;
  durationDays?: number | null;
  createdAt: string;
  requestedBy?: string | null;
  invitationSentAt?: string | null;
  activatedAt?: string | null;
  assignments: CarrierAssignment[];
}

export interface CarrierPreRegistrationRequest {
  legalName: string;
  taxId: string;
  email: string;
  country?: string;
  contactFirstName?: string;
  contactLastName?: string;
  blNumbers?: string[];
  bookingNumbers?: string[];
  durationDays?: number;
}

export interface CarrierSkipped {
  reference: string;
  code: string;
  message?: string | null;
}

export interface CarrierPreRegistrationResult {
  preRegistration: CarrierPreRegistration;
  created: boolean;
  invitationSent: boolean;
  assigned: number;
  skipped: CarrierSkipped[];
}

export interface ParentLinkOrganization {
  id: string;
  name: string;
  organizationType?: string | null;
  country?: string | null;
}

export type ParentLinkStatus = 'Pending' | 'Active' | 'Rejected' | 'Removed';

export const PARENT_LINK_STATUSES: readonly ParentLinkStatus[] = ['Pending', 'Active', 'Rejected', 'Removed'];

export interface ParentLink {
  id: string;
  organization: ParentLinkOrganization;
  parent: ParentLinkOrganization;
  status: ParentLinkStatus | string;
  visibilityEnabled: boolean;
  visibilityChangedAt?: string | null;
  visibilityChangedBy?: string | null;
  requestedAt: string;
  requestedBy?: string | null;
  notes?: string | null;
  decidedAt?: string | null;
  decidedBy?: string | null;
  decisionNotes?: string | null;
  endedAt?: string | null;
  endedBy?: string | null;
}

/** GET /organizations/me/parent-company: mi vínculo con la matriz y, si soy matriz, mis filiales activas. */
export interface ParentCompanyView {
  link?: ParentLink | null;
  subsidiaries: ParentLink[];
  canManage: boolean;
}

export interface ParentCandidate {
  id: string;
  name: string;
  taxId: string;
  organizationType: string;
  country: string;
}
