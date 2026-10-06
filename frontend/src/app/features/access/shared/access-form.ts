import {
  ACCESS_DURATION_MAX_DAYS,
  ACCESS_DURATION_MIN_DAYS,
  AccessGrant,
  AccessValidity,
  AccessValidityType,
  SHIPMENT_VIEW_ACTION,
} from '../../../core/models/access.model';

/** Vigencia tal como se edita en el formulario (fechas `yyyy-MM-dd` del campo date). */
export interface ValidityFormValue {
  validityType: AccessValidityType;
  durationDays: number | null;
  validTo: string;
  validFrom: string;
}

/** Permisos del formulario: nivel base de M1-11 o un conjunto explícito (M1-15). */
export interface PermissionsFormValue {
  mode: 'base' | 'custom';
  codes: string[];
}

export function emptyValidity(): ValidityFormValue {
  return { validityType: 'Indefinite', durationDays: null, validTo: '', validFrom: '' };
}

export function basePermissions(): PermissionsFormValue {
  return { mode: 'base', codes: [] };
}

/** Fecha local de hoy en el formato del campo date. */
export function todayInput(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

/** Fecha ISO del servidor → valor del campo date (día local). */
export function toDateInput(iso: string | null): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Errores de la vigencia por campo, como claves Transloco (M1-14). */
export interface ValidityErrors {
  durationDays?: string;
  validTo?: string;
  validityType?: string;
}

/**
 * Valida la vigencia. `requireEnd`: el mandato exige un término (M1-03), por lo que no admite
 * `Indefinite`.
 */
export function validateValidity(value: ValidityFormValue, requireEnd = false): ValidityErrors {
  const errors: ValidityErrors = {};
  if (requireEnd && value.validityType === 'Indefinite') {
    errors.validityType = 'thirdPartyAccess.validity.errors.endRequired';
  }
  if (value.validityType === 'Duration') {
    const days = value.durationDays;
    if (days === null || !Number.isInteger(days) || days < ACCESS_DURATION_MIN_DAYS || days > ACCESS_DURATION_MAX_DAYS) {
      errors.durationDays = 'thirdPartyAccess.validity.errors.duration';
    }
  }
  if (value.validityType === 'UntilDate') {
    const start = value.validFrom || todayInput();
    if (!value.validTo) {
      errors.validTo = 'thirdPartyAccess.validity.errors.untilRequired';
    } else if (value.validTo < start) {
      errors.validTo = 'thirdPartyAccess.validity.errors.untilBeforeStart';
    }
  }
  return errors;
}

export function hasErrors(errors: object): boolean {
  return Object.values(errors).some((v) => !!v);
}

/**
 * Vigencia para el servidor. El término "hasta" cubre el día completo; un inicio igual a hoy se
 * omite (el servidor usa la hora actual y no admite inicios en el pasado).
 */
export function toValidityRequest(value: ValidityFormValue): AccessValidity {
  const request: AccessValidity = { validityType: value.validityType };
  if (value.validFrom && value.validFrom > todayInput()) {
    request.validFrom = new Date(`${value.validFrom}T00:00:00`).toISOString();
  }
  if (value.validityType === 'Duration' && value.durationDays !== null) {
    request.durationDays = value.durationDays;
  }
  if (value.validityType === 'UntilDate' && value.validTo) {
    request.validTo = new Date(`${value.validTo}T23:59:59`).toISOString();
  }
  return request;
}

/** Vigencia actual de un acceso, para editarla en el lugar (M1-24). */
export function validityOf(grant: AccessGrant): ValidityFormValue {
  return {
    validityType: grant.validityType,
    durationDays: grant.durationDays,
    validTo: grant.validityType === 'UntilDate' ? toDateInput(grant.validTo) : '',
    validFrom: '',
  };
}

export function sameValidity(a: ValidityFormValue, b: ValidityFormValue): boolean {
  if (a.validityType !== b.validityType) return false;
  if (a.validityType === 'Duration') return a.durationDays === b.durationDays;
  if (a.validityType === 'UntilDate') return a.validTo === b.validTo;
  return true;
}

/** Conjunto explícito sin `shipment.view`, que el servidor agrega siempre. */
export function explicitCodes(codes: string[]): string[] {
  return codes.filter((c) => c !== SHIPMENT_VIEW_ACTION);
}

export function permissionsOf(actionCodes: string[] | null): PermissionsFormValue {
  return actionCodes === null ? basePermissions() : { mode: 'custom', codes: [...actionCodes] };
}

export function samePermissions(a: PermissionsFormValue, b: PermissionsFormValue): boolean {
  if (a.mode !== b.mode) return false;
  if (a.mode === 'base') return true;
  const left = new Set(explicitCodes(a.codes));
  const right = new Set(explicitCodes(b.codes));
  return left.size === right.size && [...left].every((c) => right.has(c));
}

/** Error de un formulario, para el resumen de errores con enlace al campo (guía §3.2). */
export interface FormErrorItem {
  fieldId: string;
  key: string;
}
