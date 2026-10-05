/**
 * Claves Transloco de los códigos que llegan del backend (tipos, estados, perfiles, roles,
 * acciones de la matriz de M1-11). Las claves van escritas completas para que check:i18n las
 * valide; un código sin clave se muestra tal cual.
 */

export const ORGANIZATION_TYPE_KEYS: Record<string, string> = {
  Customer: 'common.organizationType.customer',
  FreightForwarder: 'common.organizationType.freightForwarder',
  CustomsAgency: 'common.organizationType.customsAgency',
  Carrier: 'common.organizationType.carrier',
  Internal: 'common.organizationType.internal',
};

export const ORGANIZATION_STATUS_KEYS: Record<string, string> = {
  PendingValidation: 'common.organizationStatus.pendingValidation',
  PendingArCheck: 'common.organizationStatus.pendingArCheck',
  Approved: 'common.organizationStatus.approved',
  Rejected: 'common.organizationStatus.rejected',
};

export const MEMBERSHIP_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.membershipStatus.pending',
  Active: 'common.membershipStatus.active',
  Rejected: 'common.membershipStatus.rejected',
};

export const ORGANIZATION_PROFILE_KEYS: Record<string, string> = {
  OrgAdmin: 'common.organizationProfile.orgAdmin',
  OrgOperator: 'common.organizationProfile.orgOperator',
  OrgViewer: 'common.organizationProfile.orgViewer',
};

export const ORGANIZATION_DOCUMENT_TYPE_KEYS: Record<string, string> = {
  RegistrationLetter: 'common.documentType.registrationLetter',
  CreditAuthorization: 'common.documentType.creditAuthorization',
  TaxCertificate: 'common.documentType.taxCertificate',
  Other: 'common.documentType.other',
};

export const SHIPMENT_ROLE_KEYS: Record<string, string> = {
  Customer: 'common.shipmentRole.customer',
  Shipper: 'common.shipmentRole.shipper',
  Consignee: 'common.shipmentRole.consignee',
  ThirdParty: 'common.shipmentRole.thirdParty',
  CustomsAgency: 'common.shipmentRole.customsAgency',
  Carrier: 'common.shipmentRole.carrier',
};

export const SHIPMENT_OPERATION_KEYS: Record<string, string> = {
  IMPORT: 'common.operation.import',
  EXPORT: 'common.operation.export',
};

export const SHIPMENT_ACCESS_SOURCE_KEYS: Record<string, string> = {
  Own: 'common.accessSource.own',
  Admin: 'common.accessSource.admin',
};

/** Texto corto del nivel (O, X, X (o)) y su descripción (lectura de las matrices de M1-11). */
export const ACCESS_LEVEL_KEYS: Record<string, string> = {
  Allowed: 'common.accessLevel.allowed',
  Denied: 'common.accessLevel.denied',
  OnGrant: 'common.accessLevel.onGrant',
};

export const ACCESS_LEVEL_HELP_KEYS: Record<string, string> = {
  Allowed: 'common.accessLevel.allowedHelp',
  Denied: 'common.accessLevel.deniedHelp',
  OnGrant: 'common.accessLevel.onGrantHelp',
};

/** Nombre de cada acción de la matriz de M1-11 por su código estable (ShipmentActionCodes). */
export const ACCESS_ACTION_KEYS: Record<string, string> = {
  'shipment.view': 'admin.accessMatrix.action.viewShipment',
  'release-requirements.view': 'admin.accessMatrix.action.viewReleaseRequirements',
  'tracking.view': 'admin.accessMatrix.action.viewTracking',
  'bl-issuance.view': 'admin.accessMatrix.action.viewBlIssuance',
  'bl-copy-unvalued.request': 'admin.accessMatrix.action.requestUnvaluedBlCopy',
  'bl-copy-valued.request': 'admin.accessMatrix.action.requestValuedBlCopy',
  'no-debt-certificate.download': 'admin.accessMatrix.action.downloadNoDebtCertificate',
  'tatc.download': 'admin.accessMatrix.action.downloadTatc',
  'freight.pay': 'admin.accessMatrix.action.payFreight',
  'local-charges-mandatory.pay': 'admin.accessMatrix.action.payMandatoryLocalCharges',
  'local-charges-on-demand.pay': 'admin.accessMatrix.action.payOnDemandLocalCharges',
  'release-letter.generate': 'admin.accessMatrix.action.generateReleaseLetter',
  'transshipment-certificate.generate': 'admin.accessMatrix.action.generateTransshipmentCertificate',
  'freight-certificate.generate': 'admin.accessMatrix.action.generateFreightCertificate',
  'responsibility-letter.generate': 'admin.accessMatrix.action.generateResponsibilityLetter',
  'import-demurrage.pay': 'admin.accessMatrix.action.payImportDemurrage',
  'drop-off.request': 'admin.accessMatrix.action.requestDropOff',
  'warehouse-change.request': 'admin.accessMatrix.action.requestWarehouseChange',
  'account-statement.view': 'admin.accessMatrix.action.viewAccountStatement',
  'invoices-billed.view': 'admin.accessMatrix.action.viewInvoicesAsBilled',
  'invoices-payer.view': 'admin.accessMatrix.action.viewInvoicesAsPayer',
  'collect-receipt.download': 'admin.accessMatrix.action.downloadCollectReceipt',
  'import-depot.view': 'admin.accessMatrix.action.viewImportDepot',
  'export-depot.view': 'admin.accessMatrix.action.viewExportDepot',
  'access.grant': 'admin.accessMatrix.action.grantAccess',
  'access.revoke': 'admin.accessMatrix.action.revokeAccess',
  'access-validity.set': 'admin.accessMatrix.action.setAccessValidity',
  'default-agents.configure': 'admin.accessMatrix.action.configureDefaultAgents',
  'open-access.enable': 'admin.accessMatrix.action.enableOpenAccess',
  'open-access.search': 'admin.accessMatrix.action.searchOpenAccess',
  'open-access.self-associate': 'admin.accessMatrix.action.selfAssociate',
  'third-party-query.notify': 'admin.accessMatrix.action.receiveThirdPartyQueryNotice',
  'access-audit.view': 'admin.accessMatrix.action.viewAccessAudit',
  'parent-company-visibility.enable': 'admin.accessMatrix.action.enableParentCompanyVisibility',
  'data-visibility.extend': 'admin.accessMatrix.action.extendDataVisibility',
  'early-booking-access.grant': 'admin.accessMatrix.action.grantEarlyBookingAccess',
  'organization-users.own': 'admin.accessMatrix.action.haveOwnUsers',
  'join-requests.approve': 'admin.accessMatrix.action.approveJoinRequests',
  'carrier.pre-create': 'admin.accessMatrix.action.preCreateCarrier',
  'distribution-list.update': 'admin.accessMatrix.action.updateDistributionList',
  'administration-area.access': 'admin.accessMatrix.action.accessAdministrationArea',
  'assistant.use': 'admin.accessMatrix.action.useAssistant',
  'country.select': 'admin.accessMatrix.action.selectCountry',
};
