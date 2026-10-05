import { OrganizationType } from './organization.model';
import { ShipmentRole } from './shipment.model';

/** Niveles de la matriz de M1-11: O (Allowed), X (Denied) y X (o) (OnGrant). */
export type AccessLevel = 'Allowed' | 'Denied' | 'OnGrant';

export const ACCESS_LEVELS: readonly AccessLevel[] = ['Allowed', 'Denied', 'OnGrant'];

/** Nivel de una acción para un rol; con `organizationType` es una excepción por tipo de organización. */
export interface AccessMatrixLevel {
  role: ShipmentRole;
  organizationType: OrganizationType | null;
  level: AccessLevel;
}

/** Fila de la matriz base de M1-11. */
export interface AccessMatrixAction {
  code: string;
  name: string;
  category: 'Information' | 'Administration';
  kind: 'View' | 'Operate';
  scope: 'Shipment' | 'Organization';
  displayOrder: number;
  isActive: boolean;
  levels: AccessMatrixLevel[];
}

export interface UpdateAccessMatrixLevelRequest {
  role: ShipmentRole;
  organizationType?: OrganizationType | null;
  level: AccessLevel;
}
