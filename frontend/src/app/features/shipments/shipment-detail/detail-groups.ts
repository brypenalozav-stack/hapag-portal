import { SectionNavItem } from '../../../shared/components/section-nav/section-nav';

/** Grupos del detalle del BL, en el orden del índice "Ir a": id del título h2 del grupo y clave de su texto. */
export const DETAIL_GROUPS = {
  summary: { id: 'bl-group-summary', labelKey: 'shipments.detail.groups.summary' },
  containers: { id: 'bl-group-containers', labelKey: 'shipments.detail.groups.containers' },
  charges: { id: 'bl-group-charges', labelKey: 'shipments.detail.groups.charges' },
  documents: { id: 'bl-group-documents', labelKey: 'shipments.detail.groups.documents' },
  access: { id: 'bl-group-access', labelKey: 'shipments.detail.groups.access' },
  internal: { id: 'bl-group-internal', labelKey: 'shipments.detail.groups.internal' },
} as const satisfies Record<string, SectionNavItem>;

export type DetailGroup = keyof typeof DETAIL_GROUPS;

export const DETAIL_GROUP_ORDER: DetailGroup[] = ['summary', 'containers', 'charges', 'documents', 'access', 'internal'];
