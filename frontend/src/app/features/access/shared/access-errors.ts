/**
 * Códigos de error del backend de accesos (ProblemDetails `title`) → claves Transloco. Los textos
 * del servidor vienen en inglés y no se muestran tal cual.
 */
export const GRANT_ERRORS: Record<string, string> = {
  'BillOfLading.NotFound': 'thirdPartyAccess.errors.blNotFound',
  'AccessGrant.ExceedsGrantorLevel': 'thirdPartyAccess.errors.exceedsGrantorLevel',
  'AccessGrant.NotGrantable': 'thirdPartyAccess.errors.notGrantable',
  'AccessGrant.NotAllowed': 'thirdPartyAccess.errors.notAllowed',
  'AccessGrant.SelfGrant': 'thirdPartyAccess.errors.selfGrant',
  'AccessGrant.GranteeNotFound': 'thirdPartyAccess.errors.granteeNotFound',
  'AccessGrant.InvalidValidity': 'thirdPartyAccess.errors.invalidValidity',
  'AccessGrant.TermsVersionMismatch': 'thirdPartyAccess.errors.termsVersionMismatch',
  'AccessGrant.UnknownActions': 'thirdPartyAccess.errors.unknownActions',
  'AccessGrant.NotFound': 'thirdPartyAccess.errors.grantNotFound',
  'AccessGrant.NotOpen': 'thirdPartyAccess.errors.notOpen',
  'AccessGrant.NotPendingAcceptance': 'thirdPartyAccess.errors.notPendingAcceptance',
  'AccessGrant.EarlyBookingNotAllowed': 'thirdPartyAccess.errors.earlyBookingNotAllowed',
  'AccessGrant.EarlyBookingRecipient': 'thirdPartyAccess.errors.earlyBookingRecipient',
  'DefaultGrantee.AlreadyExists': 'thirdPartyAccess.errors.defaultExists',
  'DefaultGrantee.NotFound': 'thirdPartyAccess.errors.defaultNotFound',
  'OpenAccess.NotAllowed': 'thirdPartyAccess.errors.openAccessNotAllowed',
  'OpenAccess.NotAvailable': 'thirdPartyAccess.errors.openAccessNotAvailable',
  'ShipmentAssociation.AlreadyExists': 'thirdPartyAccess.errors.alreadyAssociated',
  'VisibilityWidening.NotWidenable': 'thirdPartyAccess.errors.notWidenable',
  'VisibilityWidening.InvalidTargetRole': 'thirdPartyAccess.errors.invalidTargetRole',
  'VisibilityWidening.NotFound': 'thirdPartyAccess.errors.wideningNotFound',
  'Error.Forbidden': 'thirdPartyAccess.errors.forbidden',
};

/** Código de un omitido del otorgamiento masivo → clave Transloco; sin clave se muestra el código. */
export const SKIPPED_REASON_KEYS: Record<string, string> = GRANT_ERRORS;
