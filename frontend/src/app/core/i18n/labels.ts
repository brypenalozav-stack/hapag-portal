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
  PreCreated: 'common.organizationStatus.preCreated',
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
  Grant: 'common.accessSource.grant',
  SelfAssociated: 'common.accessSource.selfAssociated',
  OpenAccess: 'common.accessSource.openAccess',
  Admin: 'common.accessSource.admin',
  Parent: 'common.accessSource.parent',
};

/** Estado de un acceso otorgado (M1-14, M1-22, M1-03, M1-20). */
export const ACCESS_GRANT_STATUS_KEYS: Record<string, string> = {
  PendingAcceptance: 'common.accessGrantStatus.pendingAcceptance',
  Active: 'common.accessGrantStatus.active',
  Expired: 'common.accessGrantStatus.expired',
  Revoked: 'common.accessGrantStatus.revoked',
  Reconciled: 'common.accessGrantStatus.reconciled',
  PendingActivation: 'common.accessGrantStatus.pendingActivation',
};

/** Origen del acceso: `grantType` y, si es mandato, la clave `Mandate` (M1-03). */
export const ACCESS_ORIGIN_KEYS: Record<string, string> = {
  Individual: 'common.accessOrigin.individual',
  Bulk: 'common.accessOrigin.bulk',
  Default: 'common.accessOrigin.default',
  EarlyBooking: 'common.accessOrigin.earlyBooking',
  Mandate: 'common.accessOrigin.mandate',
};

export const ACCESS_VALIDITY_TYPE_KEYS: Record<string, string> = {
  Indefinite: 'common.accessValidityType.indefinite',
  Duration: 'common.accessValidityType.duration',
  UntilDate: 'common.accessValidityType.untilDate',
};

export const ACCESS_END_REASON_KEYS: Record<string, string> = {
  Manual: 'common.accessEndReason.manual',
  Expired: 'common.accessEndReason.expired',
  Cascade: 'common.accessEndReason.cascade',
  Reconciled: 'common.accessEndReason.reconciled',
};

export const ACCESS_DIRECTION_KEYS: Record<string, string> = {
  Given: 'common.accessDirection.given',
  Received: 'common.accessDirection.received',
};

export const WIDENING_STATUS_KEYS: Record<string, string> = {
  Active: 'common.wideningStatus.active',
  Revoked: 'common.wideningStatus.revoked',
};

/** Tipo de evento de la auditoría de accesos (M1-23). */
export const ACCESS_AUDIT_EVENT_KEYS: Record<string, string> = {
  GrantCreated: 'common.accessAuditEvent.grantCreated',
  GrantPermissionsChanged: 'common.accessAuditEvent.grantPermissionsChanged',
  GrantValidityChanged: 'common.accessAuditEvent.grantValidityChanged',
  GrantRevoked: 'common.accessAuditEvent.grantRevoked',
  GrantExpired: 'common.accessAuditEvent.grantExpired',
  GrantRevokedByCascade: 'common.accessAuditEvent.grantRevokedByCascade',
  MandateTermsAccepted: 'common.accessAuditEvent.mandateTermsAccepted',
  BookingAccessLinked: 'common.accessAuditEvent.bookingAccessLinked',
  BookingAccessReconciled: 'common.accessAuditEvent.bookingAccessReconciled',
  OpenAccessEnabled: 'common.accessAuditEvent.openAccessEnabled',
  OpenAccessDisabled: 'common.accessAuditEvent.openAccessDisabled',
  OpenAccessPermissionsChanged: 'common.accessAuditEvent.openAccessPermissionsChanged',
  SelfAssociated: 'common.accessAuditEvent.selfAssociated',
  WideningCreated: 'common.accessAuditEvent.wideningCreated',
  WideningRevoked: 'common.accessAuditEvent.wideningRevoked',
  WideningRevokedByCascade: 'common.accessAuditEvent.wideningRevokedByCascade',
  DefaultGranteeAdded: 'common.accessAuditEvent.defaultGranteeAdded',
  DefaultGranteeUpdated: 'common.accessAuditEvent.defaultGranteeUpdated',
  DefaultGranteeRemoved: 'common.accessAuditEvent.defaultGranteeRemoved',
  // Fase 2, Ola I: transportistas pre-creados (M1-09) y empresa matriz (M1-21).
  CarrierPreCreated: 'common.accessAuditEvent.carrierPreCreated',
  CarrierActivated: 'common.accessAuditEvent.carrierActivated',
  GrantActivated: 'common.accessAuditEvent.grantActivated',
  ParentLinkRequested: 'common.accessAuditEvent.parentLinkRequested',
  ParentLinkApproved: 'common.accessAuditEvent.parentLinkApproved',
  ParentLinkRejected: 'common.accessAuditEvent.parentLinkRejected',
  ParentLinkRemoved: 'common.accessAuditEvent.parentLinkRemoved',
  ParentVisibilityEnabled: 'common.accessAuditEvent.parentVisibilityEnabled',
  ParentVisibilityDisabled: 'common.accessAuditEvent.parentVisibilityDisabled',
};

/**
 * Título traducido de las notificaciones por tipo (bandeja). Los tipos sin clave muestran el
 * título que envía el servidor.
 */
export const NOTIFICATION_TYPE_KEYS: Record<string, string> = {
  AccessGranted: 'notifications.type.accessGranted',
  AccessUpdated: 'notifications.type.accessUpdated',
  AccessRevoked: 'notifications.type.accessRevoked',
  AccessExpired: 'notifications.type.accessExpired',
  AccessRevokedByCascade: 'notifications.type.accessRevokedByCascade',
  DocumentIssued: 'notifications.type.documentIssued',
  // Fase 2, Ola G: cambios de estado de las solicitudes de servicios on demand.
  ServiceRequestPendingApproval: 'notifications.type.serviceRequestPendingApproval',
  ServiceRequestApproved: 'notifications.type.serviceRequestApproved',
  ServiceRequestRejected: 'notifications.type.serviceRequestRejected',
  ServiceRequestPendingPayment: 'notifications.type.serviceRequestPendingPayment',
  ServiceRequestPaid: 'notifications.type.serviceRequestPaid',
  ServiceRequestInProgress: 'notifications.type.serviceRequestInProgress',
  ServiceRequestCompleted: 'notifications.type.serviceRequestCompleted',
  ServiceRequestCancelled: 'notifications.type.serviceRequestCancelled',
  // Fase 2, Ola H: comprobantes de depósito, imputaciones a crédito y refacturación IAO.
  DepositProofSubmitted: 'notifications.type.depositProofSubmitted',
  DepositProofRejected: 'notifications.type.depositProofRejected',
  CreditImputationRegistered: 'notifications.type.creditImputationRegistered',
  ReinvoicingAccepted: 'notifications.type.reinvoicingAccepted',
  ReinvoicingDeclined: 'notifications.type.reinvoicingDeclined',
  InvoiceReissued: 'notifications.type.invoiceReissued',
  // Fase 2, Ola I: el resto del catálogo de la bandeja (M1-25), comunicados, empresa matriz y transportistas.
  DeadlineAtRisk: 'notifications.type.deadlineAtRisk',
  DeadlineOverdue: 'notifications.type.deadlineOverdue',
  TransmissionRejected: 'notifications.type.transmissionRejected',
  TransmissionError: 'notifications.type.transmissionError',
  JoinRequestReceived: 'notifications.type.joinRequestReceived',
  JoinRequestApproved: 'notifications.type.joinRequestApproved',
  JoinRequestRejected: 'notifications.type.joinRequestRejected',
  OrganizationApproved: 'notifications.type.organizationApproved',
  OrganizationRejected: 'notifications.type.organizationRejected',
  PaymentConfirmed: 'notifications.type.paymentConfirmed',
  AnnouncementPublished: 'notifications.type.announcementPublished',
  ParentLinkRequested: 'notifications.type.parentLinkRequested',
  ParentLinkApproved: 'notifications.type.parentLinkApproved',
  ParentLinkRejected: 'notifications.type.parentLinkRejected',
  ParentVisibilityChanged: 'notifications.type.parentVisibilityChanged',
  CarrierActivated: 'notifications.type.carrierActivated',
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

// ---------------------------------------------------------------------------
// Fase 1, Ola C: cargos con reglas de Nexus, demurrage, cambio de almacén y mantenedores.
// ---------------------------------------------------------------------------

/** Concepto de cobro del catálogo (las siglas de la industria no se traducen). */
export const CHARGE_CONCEPT_KEYS: Record<string, string> = {
  GATE_IN: 'common.chargeConcept.gateIn',
  EDS: 'common.chargeConcept.eds',
  GATE_OUT: 'common.chargeConcept.gateOut',
  IPO: 'common.chargeConcept.ipo',
  THC: 'common.chargeConcept.thc',
  THC_RF: 'common.chargeConcept.thcRf',
  BL_FEE: 'common.chargeConcept.blFee',
  ISPS: 'common.chargeConcept.isps',
  TRANSIT_FEE: 'common.chargeConcept.transitFee',
  MHD: 'common.chargeConcept.mhd',
  DEMURRAGE: 'common.chargeConcept.demurrage',
  ADVANCE_DEMURRAGE_BO: 'common.chargeConcept.advanceDemurrageBo',
  WAREHOUSE_CHANGE: 'common.chargeConcept.warehouseChange',
  LATE_ARRIVAL: 'common.chargeConcept.lateArrival',
  FREIGHT: 'common.chargeConcept.freight',
  INVOICE: 'common.chargeConcept.invoice',
  TRANSSHIPMENT_CERT: 'common.chargeConcept.transshipmentCert',
  // Fase 2, Ola G: conceptos de los servicios on demand (M3-07 a M3-14) y la homologación de M2-03.
  SEAL_MANAGEMENT: 'common.chargeConcept.sealManagement',
  EARLY_ARRIVAL: 'common.chargeConcept.earlyArrival',
  DROP_OFF: 'common.chargeConcept.dropOff',
  XOM: 'common.chargeConcept.xom',
  BL_CORRECTION: 'common.chargeConcept.blCorrection',
  BL_HOUSE_TRANSMISSION: 'common.chargeConcept.blHouseTransmission',
  MATRIX_LATE: 'common.chargeConcept.matrixLate',
  OPENING: 'common.chargeConcept.opening',
  VALUATION: 'common.chargeConcept.valuation',
  // Fase 2, Ola H: refacturación IAO y pérdida de IVA (M3-11).
  REINVOICING: 'common.chargeConcept.reinvoicing',
  VAT_LOSS: 'common.chargeConcept.vatLoss',
};

/** Resultado de las reglas sobre un cargo (M4-01 a M4-03). */
export const CHARGE_OUTCOME_KEYS: Record<string, string> = {
  Payable: 'common.chargeOutcome.payable',
  PartiallyExempt: 'common.chargeOutcome.partiallyExempt',
  Exempt: 'common.chargeOutcome.exempt',
  Paid: 'common.chargeOutcome.paid',
  CreditImputed: 'common.chargeOutcome.creditImputed',
};

/** Motivo por el que una acción no está disponible. */
export const CHARGE_BLOCKED_REASON_KEYS: Record<string, string> = {
  NO_PERMISSION: 'common.chargeBlockedReason.noPermission',
  ASSOCIATION_REQUIRED: 'common.chargeBlockedReason.associationRequired',
  RULES_UNAVAILABLE: 'common.chargeBlockedReason.rulesUnavailable',
};

/** Figura consultada en Nexus (M4-01). */
export const EXEMPTION_PARTY_KEYS: Record<string, string> = {
  MasterConsignee: 'common.exemptionParty.masterConsignee',
  FinalClient: 'common.exemptionParty.finalClient',
};

/** Origen de un dato: Nexus, el portal (mantenedores), FIS o la tarifa. */
export const DATA_SOURCE_KEYS: Record<string, string> = {
  NEXUS: 'common.dataSource.nexus',
  PORTAL: 'common.dataSource.portal',
  FIS: 'common.dataSource.fis',
  TARIFF: 'common.dataSource.tariff',
};

/** Estado del demurrage del BL (M3-18). */
export const DEMURRAGE_STATE_KEYS: Record<string, string> = {
  InvoicedWithDebt: 'common.demurrageState.invoicedWithDebt',
  CalculatedUnpaid: 'common.demurrageState.calculatedUnpaid',
  NotCalculated: 'common.demurrageState.notCalculated',
  NoDemurrage: 'common.demurrageState.noDemurrage',
};

/** Estado de una línea de demurrage o de un cargo. */
export const CHARGE_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.chargeStatus.pending',
  Invoiced: 'common.chargeStatus.invoiced',
  Paid: 'common.chargeStatus.paid',
  Exempt: 'common.chargeStatus.exempt',
  CreditImputed: 'common.chargeStatus.creditImputed',
};

/** Estado de las demoras anticipadas de Bolivia (M3-16). */
export const ADVANCE_DEMURRAGE_STATUS_KEYS: Record<string, string> = {
  NotRequired: 'common.advanceDemurrageStatus.notRequired',
  NotRequested: 'common.advanceDemurrageStatus.notRequested',
  Pending: 'common.advanceDemurrageStatus.pending',
  Paid: 'common.advanceDemurrageStatus.paid',
};

/** Estado de un requisito del proceso (carta FFWW, demoras anticipadas). */
export const REQUIREMENT_STATUS_KEYS: Record<string, string> = {
  Missing: 'common.requirementStatus.missing',
  Pending: 'common.requirementStatus.pending',
  Fulfilled: 'common.requirementStatus.fulfilled',
};

/** Estado de una solicitud de cambio de almacén (M3-04). */
export const WAREHOUSE_CHANGE_STATUS_KEYS: Record<string, string> = {
  Completed: 'common.warehouseChangeStatus.completed',
  Pending: 'common.warehouseChangeStatus.pending',
  Cancelled: 'common.warehouseChangeStatus.cancelled',
};

/** Estado de la solicitud masiva y de cada línea (M3-05). */
export const WAREHOUSE_BATCH_STATUS_KEYS: Record<string, string> = {
  Queued: 'common.warehouseBatchStatus.queued',
  Processing: 'common.warehouseBatchStatus.processing',
  Completed: 'common.warehouseBatchStatus.completed',
  CompletedWithErrors: 'common.warehouseBatchStatus.completedWithErrors',
};

export const WAREHOUSE_BATCH_ITEM_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.warehouseBatchItemStatus.pending',
  Succeeded: 'common.warehouseBatchItemStatus.succeeded',
  Failed: 'common.warehouseBatchItemStatus.failed',
};

/** Unidad y modo de los tramos de una tarifa (M8-01). */
export const TARIFF_TIER_UNIT_KEYS: Record<string, string> = {
  None: 'common.tariffTierUnit.none',
  Hours: 'common.tariffTierUnit.hours',
  CalendarDays: 'common.tariffTierUnit.calendarDays',
  BusinessDays: 'common.tariffTierUnit.businessDays',
  Units: 'common.tariffTierUnit.units',
};

export const TARIFF_TIER_MODE_KEYS: Record<string, string> = {
  Flat: 'common.tariffTierMode.flat',
  PerUnit: 'common.tariffTierMode.perUnit',
};

/** Tipo de regla interna de cobro (M3-04, M3-16). */
export const INTERNAL_RULE_TYPE_KEYS: Record<string, string> = {
  FreeWarehouseChange: 'common.internalRuleType.freeWarehouseChange',
  AdvanceDemurrageRequired: 'common.internalRuleType.advanceDemurrageRequired',
};

/** Acción registrada en el historial de un mantenedor (NF-15). */
export const MAINTAINER_ACTION_KEYS: Record<string, string> = {
  Created: 'common.maintainerAction.created',
  Updated: 'common.maintainerAction.updated',
  Deactivated: 'common.maintainerAction.deactivated',
  // Fase 2, Ola I: comunicados (M1-26).
  Published: 'common.maintainerAction.published',
  Unpublished: 'common.maintainerAction.unpublished',
  Deleted: 'common.maintainerAction.deleted',
};

/**
 * Códigos de error del backend de la Ola C (ProblemDetails `title`) → claves Transloco. Los textos
 * del servidor vienen en inglés y no se muestran tal cual.
 */
export const CHARGE_ERRORS: Record<string, string> = {
  'BillOfLading.NotFound': 'common.chargeErrors.blNotFound',
  'ChargeRules.ConditionsUnavailable': 'common.chargeErrors.conditionsUnavailable',
  'ChargeRules.NoChargesToApply': 'common.chargeErrors.noChargesToApply',
  'Demurrage.InvoiceExists': 'common.chargeErrors.invoiceExists',
  'Demurrage.NotImport': 'common.chargeErrors.notImport',
  'Demurrage.NotArrived': 'common.chargeErrors.notArrived',
  'Demurrage.AdvanceNotRequired': 'common.chargeErrors.advanceNotRequired',
  'Tariff.NotInForce': 'common.chargeErrors.tariffNotInForce',
  'Tariff.NotFound': 'common.chargeErrors.tariffNotFound',
  'Tariff.InvalidTiers': 'common.chargeErrors.invalidTiers',
  'Tariff.Overlaps': 'common.chargeErrors.tariffOverlaps',
  'ChargeConcept.NotFound': 'common.chargeErrors.conceptNotFound',
  'InternalChargeRule.NotFound': 'common.chargeErrors.ruleNotFound',
  'InternalChargeRule.MissingIdentifier': 'common.chargeErrors.ruleMissingIdentifier',
  'WarehouseChange.SameWarehouse': 'common.chargeErrors.sameWarehouse',
  'WarehouseChange.ContainerNotFound': 'common.chargeErrors.containerNotFound',
  'WarehouseChange.NotFound': 'common.chargeErrors.warehouseChangeNotFound',
  'ExchangeRate.NotFound': 'common.chargeErrors.exchangeRateNotFound',
  'ExchangeRate.NotApproved': 'common.chargeErrors.exchangeRateNotApproved',
  'Error.Forbidden': 'common.chargeErrors.forbidden',
};

// ---------------------------------------------------------------------------
// Fase 1, Ola D: carro, pagos, facturas, historial y configuración de pagos.
// ---------------------------------------------------------------------------

/** Tipo de servicio de un ítem del carro o de un pago (M5-01). */
export const PAYABLE_ITEM_TYPE_KEYS: Record<string, string> = {
  LocalCharge: 'common.payableItemType.localCharge',
  Freight: 'common.payableItemType.freight',
  Demurrage: 'common.payableItemType.demurrage',
  WarehouseChange: 'common.payableItemType.warehouseChange',
  Invoice: 'common.payableItemType.invoice',
};

/** Estado único de un pago (NF-02). */
export const PAYMENT_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.paymentStatus.pending',
  Processing: 'common.paymentStatus.processing',
  PendingVerification: 'common.paymentStatus.pendingVerification',
  Confirmed: 'common.paymentStatus.confirmed',
  Failed: 'common.paymentStatus.failed',
  Cancelled: 'common.paymentStatus.cancelled',
};

/** Variante de .hl-badge por estado de pago (el texto acompaña siempre al color). */
export const PAYMENT_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Processing: 'hl-badge--processing',
  PendingVerification: 'hl-badge--processing',
  Confirmed: 'hl-badge--confirmed',
  Failed: 'hl-badge--failed',
  Cancelled: 'hl-badge--failed',
};

export const PAYMENT_ORIGIN_KEYS: Record<string, string> = {
  Cart: 'common.paymentOrigin.cart',
  Account: 'common.paymentOrigin.account',
  Legacy: 'common.paymentOrigin.legacy',
  CreditLine: 'common.paymentOrigin.creditLine',
};

/** Motivo de un pago fallido (NF-12). */
export const PAYMENT_FAILURE_REASON_KEYS: Record<string, string> = {
  PROVIDER_UNAVAILABLE: 'common.paymentFailureReason.providerUnavailable',
  PROVIDER_REJECTED: 'common.paymentFailureReason.providerRejected',
};

/** Por qué el cliente no puede anular un pago (M5-02). */
export const CANCEL_DENIED_REASON_KEYS: Record<string, string> = {
  SLIP_ISSUED: 'common.cancelDeniedReason.slipIssued',
  IN_PROGRESS: 'common.cancelDeniedReason.inProgress',
  FINAL: 'common.cancelDeniedReason.final',
};

export const PAYMENT_METHOD_KIND_KEYS: Record<string, string> = {
  Online: 'common.paymentMethodKind.online',
  Deposit: 'common.paymentMethodKind.deposit',
};

/** Origen de un RUT de facturación habilitado (M5-09). */
export const BILLING_OPTION_SOURCE_KEYS: Record<string, string> = {
  Own: 'common.billingOptionSource.own',
  Grant: 'common.billingOptionSource.grant',
  Invoice: 'common.billingOptionSource.invoice',
};

export const INVOICE_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.invoiceStatus.pending',
  Overdue: 'common.invoiceStatus.overdue',
  Paid: 'common.invoiceStatus.paid',
  Cancelled: 'common.invoiceStatus.cancelled',
  Superseded: 'common.invoiceStatus.superseded',
};

export const INVOICE_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Overdue: 'hl-badge--failed',
  Paid: 'hl-badge--confirmed',
  Cancelled: 'hl-badge--processing',
  Superseded: 'hl-badge--processing',
};

export const INVOICE_DOCUMENT_TYPE_KEYS: Record<string, string> = {
  Invoice: 'common.invoiceDocumentType.invoice',
  ExemptInvoice: 'common.invoiceDocumentType.exemptInvoice',
  CreditNote: 'common.invoiceDocumentType.creditNote',
  DebitNote: 'common.invoiceDocumentType.debitNote',
};

/** Estado de una ventana de bloqueo de pagos (M8-07). */
export const BLOCK_WINDOW_STATUS_KEYS: Record<string, string> = {
  Scheduled: 'common.blockWindowStatus.scheduled',
  Active: 'common.blockWindowStatus.active',
  Ended: 'common.blockWindowStatus.ended',
  Cancelled: 'common.blockWindowStatus.cancelled',
};

/** Paso posterior a la confirmación y su estado en la cola recuperable (NF-03). */
export const PAYMENT_OPERATION_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.paymentOperationStatus.pending',
  Succeeded: 'common.paymentOperationStatus.succeeded',
  Stuck: 'common.paymentOperationStatus.stuck',
};

export const PAYMENT_OPERATION_JOB_KEYS: Record<string, string> = {
  Release: 'common.paymentOperationJob.release',
  Notify: 'common.paymentOperationJob.notify',
  Documents: 'common.paymentOperationJob.documents',
  Reinvoicing: 'common.paymentOperationJob.reinvoicing',
};

/** Estado de conciliación de un pago (NF-04). */
export const RECONCILIATION_STATUS_KEYS: Record<string, string> = {
  Matched: 'common.reconciliationStatus.matched',
  MissingReceipt: 'common.reconciliationStatus.missingReceipt',
  MissingTransactionReference: 'common.reconciliationStatus.missingTransactionReference',
  NotSettled: 'common.reconciliationStatus.notSettled',
};

/**
 * Códigos de error del backend de la Ola D → claves Transloco. Incluye los de la Ola C que pueden
 * llegar al agregar o pagar (Nexus caído, tipo de cambio). Los textos del servidor vienen en inglés.
 */
export const PAYMENT_ERRORS: Record<string, string> = {
  'Error.Forbidden': 'common.paymentErrors.forbidden',
  'ChargeRules.ConditionsUnavailable': 'common.paymentErrors.conditionsUnavailable',
  'Cart.CreditCustomer': 'common.paymentErrors.creditCustomer',
  'PayableItem.NotFound': 'common.paymentErrors.itemNotFound',
  'Cart.AssociationRequired': 'common.paymentErrors.associationRequired',
  'Cart.ResponsibilityLetterRequired': 'common.paymentErrors.responsibilityLetterRequired',
  'Cart.AlreadyPaid': 'common.paymentErrors.alreadyPaid',
  'Cart.ZeroValue': 'common.paymentErrors.zeroValue',
  'Cart.NotPayable': 'common.paymentErrors.notPayable',
  'Cart.PayDemurrageInvoice': 'common.paymentErrors.payDemurrageInvoice',
  'Cart.ItemInPayment': 'common.paymentErrors.itemInPayment',
  'Cart.NoPaymentCurrency': 'common.paymentErrors.noPaymentCurrency',
  'Cart.CurrencyNotAllowed': 'common.paymentErrors.currencyNotAllowed',
  'Cart.BillingTaxIdNotAllowed': 'common.paymentErrors.billingTaxIdNotAllowed',
  'CartItem.AlreadyExists': 'common.paymentErrors.duplicate',
  'CartItem.NotFound': 'common.paymentErrors.cartItemNotFound',
  'Cart.ItemLocked': 'common.paymentErrors.itemLocked',
  'Cart.Empty': 'common.paymentErrors.empty',
  'Cart.Conflict': 'common.paymentErrors.conflict',
  'ExchangeRate.NotFound': 'common.paymentErrors.exchangeRateNotFound',
  'ExchangeRate.NotApproved': 'common.paymentErrors.exchangeRateNotApproved',
  'Payment.Blocked': 'common.paymentErrors.blocked',
  'PaymentMethod.NotAvailable': 'common.paymentErrors.methodNotAvailable',
  'Payment.ProviderUnavailable': 'common.paymentErrors.providerUnavailable',
  'PaymentIdempotency.AlreadyExists': 'common.paymentErrors.idempotencyConflict',
  'Payment.SlipAlreadyIssued': 'common.paymentErrors.slipAlreadyIssued',
  'Payment.InProgress': 'common.paymentErrors.inProgress',
  'Payment.AlreadyConfirmed': 'common.paymentErrors.alreadyConfirmed',
  'Payment.AlreadyCancelled': 'common.paymentErrors.alreadyCancelled',
  'Payment.NotFound': 'common.paymentErrors.paymentNotFound',
  'Payment.ReceiptNotAvailable': 'common.paymentErrors.receiptNotAvailable',
  'AccountPayment.NotCreditCustomer': 'common.paymentErrors.notCreditCustomer',
  'AccountPayment.MixedCountries': 'common.paymentErrors.mixedCountries',
  'Invoice.PdfNotAvailable': 'common.paymentErrors.pdfNotAvailable',
  'Invoice.FolioRequired': 'common.paymentErrors.folioRequired',
  'Integration.Unavailable': 'common.paymentErrors.sourceUnavailable',
  'PaymentMethod.ProviderRequired': 'common.paymentErrors.providerRequired',
  'PaymentMethod.AlreadyExists': 'common.paymentErrors.methodExists',
  'PaymentBlockWindow.AlreadyEnded': 'common.paymentErrors.windowEnded',
  'PaymentBlockWindow.AlreadyStarted': 'common.paymentErrors.windowStarted',
  'PaymentOperation.NotRetryable': 'common.paymentErrors.operationNotRetryable',
  'Payment.InvalidTransition': 'common.paymentErrors.invalidTransition',
  'Payment.NotDeposit': 'common.paymentErrors.notDeposit',
  'Invoice.NotFound': 'common.paymentErrors.invoiceNotFound',
  // Fase 2, Ola H: estado de cuenta, imputación a crédito, comprobantes de depósito y anticipos.
  'Cart.CreditImputed': 'common.paymentErrors.creditImputed',
  'AccountPayment.CreditNotEligible': 'common.paymentErrors.creditNotEligible',
  'AccountPayment.PaymentDataRequired': 'common.paymentErrors.paymentDataRequired',
  'DepositProof.PaymentNotAwaitingProof': 'common.paymentErrors.proofNotAwaited',
  'DepositProof.PendingReview': 'common.paymentErrors.proofPendingReview',
  'DepositProof.NotPendingReview': 'common.paymentErrors.proofNotPendingReview',
  'DepositProof.NotFound': 'common.paymentErrors.proofNotFound',
  'Settlement.NotFound': 'common.paymentErrors.settlementNotFound',
  'Settlement.AlreadyMatched': 'common.paymentErrors.settlementAlreadyMatched',
  'Settlement.InvoiceNotMatchable': 'common.paymentErrors.settlementInvoiceNotMatchable',
  'CreditImputationRule.AlreadyExists': 'common.paymentErrors.creditRuleExists',
  'CreditImputationRule.NotFound': 'common.paymentErrors.creditRuleNotFound',
  'ChargeConcept.NotFound': 'common.paymentErrors.conceptNotFound',
};

// ---------------------------------------------------------------------------
// Fase 1, Ola E: documentos del embarque y repositorio documental (M6-01, M6-03 a M6-07, M6-09).
// ---------------------------------------------------------------------------

/** Tipo de documento del repositorio (términos del glosario). */
export const SHIPMENT_DOCUMENT_TYPE_KEYS: Record<string, string> = {
  TransshipmentCertificate: 'common.shipmentDocumentType.transshipmentCertificate',
  GateOutCoupon: 'common.shipmentDocumentType.gateOutCoupon',
  CollectReceipt: 'common.shipmentDocumentType.collectReceipt',
  BlCopyValued: 'common.shipmentDocumentType.blCopyValued',
  BlCopyNonValued: 'common.shipmentDocumentType.blCopyNonValued',
  ResponsibilityLetter: 'common.shipmentDocumentType.responsibilityLetter',
  NoDebtCertificate: 'common.shipmentDocumentType.noDebtCertificate',
  GateOutAdvanceReceipt: 'common.shipmentDocumentType.gateOutAdvanceReceipt',
  FreightCertificate: 'common.shipmentDocumentType.freightCertificate',
  ReleaseLetter: 'common.shipmentDocumentType.releaseLetter',
};

export const SHIPMENT_DOCUMENT_STATUS_KEYS: Record<string, string> = {
  Issued: 'common.shipmentDocumentStatus.issued',
  Superseded: 'common.shipmentDocumentStatus.superseded',
  Revoked: 'common.shipmentDocumentStatus.revoked',
};

/** Variante de .hl-badge por estado del documento (el texto acompaña siempre al color). */
export const SHIPMENT_DOCUMENT_STATUS_CLASS: Record<string, string> = {
  Issued: 'hl-badge--confirmed',
  Superseded: 'hl-badge--processing',
  Revoked: 'hl-badge--failed',
};

export const SHIPMENT_DOCUMENT_ORIGIN_KEYS: Record<string, string> = {
  Payment: 'common.shipmentDocumentOrigin.payment',
  Request: 'common.shipmentDocumentOrigin.request',
  Seed: 'common.shipmentDocumentOrigin.seed',
};

/** Comprobante de pago (M7-02) o factura (M7-01) publicados en el repositorio. */
export const RELATED_DOCUMENT_KIND_KEYS: Record<string, string> = {
  Receipt: 'common.relatedDocumentKind.receipt',
  Invoice: 'common.relatedDocumentKind.invoice',
};

/** Motivo que impide el certificado de libre deuda (M6-07, M3-16). */
export const NO_DEBT_BLOCKER_KEYS: Record<string, string> = {
  PENDING_CHARGES: 'common.noDebtBlocker.pendingCharges',
  PENDING_DEMURRAGE: 'common.noDebtBlocker.pendingDemurrage',
  PENDING_INVOICES: 'common.noDebtBlocker.pendingInvoices',
  PENDING_FREIGHT: 'common.noDebtBlocker.pendingFreight',
  ADVANCE_DEMURRAGE: 'common.noDebtBlocker.advanceDemurrage',
};

/**
 * Códigos de error del backend de la Ola E → claves Transloco. Los textos del servidor vienen en
 * inglés y no se muestran tal cual.
 */
export const DOCUMENT_ERRORS: Record<string, string> = {
  'BillOfLading.NotFound': 'common.documentErrors.blNotFound',
  'ShipmentDocument.NotFound': 'common.documentErrors.notFound',
  'ShipmentDocumentContent.NotFound': 'common.documentErrors.contentNotFound',
  'ShipmentDocument.NoRecipient': 'common.documentErrors.noRecipient',
  'ShipmentDocument.NotAvailable': 'common.documentErrors.notAvailable',
  'ResponsibilityLetter.TermsNotAccepted': 'common.documentErrors.termsNotAccepted',
  'ResponsibilityLetter.TermsVersionMismatch': 'common.documentErrors.termsVersionMismatch',
  'NoDebtCertificate.NotApplicable': 'common.documentErrors.noDebtNotApplicable',
  'NoDebtCertificate.DebtPending': 'common.documentErrors.debtPending',
  'Tariff.NotInForce': 'common.documentErrors.tariffNotInForce',
  'ChargeRules.ConditionsUnavailable': 'common.documentErrors.conditionsUnavailable',
  'Integration.Unavailable': 'common.documentErrors.sourceUnavailable',
  'Error.Forbidden': 'common.documentErrors.forbidden',
  // Fase 2, Ola J: certificado de flete (M6-02) y carta de liberación y desconsolidado (M6-08).
  'FreightCertificate.NotApplicable': 'common.documentErrors.freightNotApplicable',
  'ReleaseLetter.NotApplicable': 'common.documentErrors.releaseLetterNotApplicable',
  'ReleaseLetter.CarrierRequired': 'common.documentErrors.carrierRequired',
  'ReleaseLetter.CarrierNotFound': 'common.documentErrors.carrierNotFound',
  'ReleaseLetter.AlreadyRequested': 'common.documentErrors.releaseLetterAlreadyRequested',
  'ServiceRequest.NotFound': 'common.documentErrors.requestNotFound',
  'ServiceRequest.InvalidTransition': 'common.documentErrors.invalidTransition',
};

// ---------------------------------------------------------------------------
// Fase 1, Ola F: dashboard, publicación por DIFU, emisión del BL, TATC, asistente y mercancías peligrosas.
// ---------------------------------------------------------------------------

/** Tipo de gestión del dashboard (M1-05), con los términos del glosario. */
export const DASHBOARD_REQUEST_KIND_KEYS: Record<string, string> = {
  Shipment: 'common.requestKind.shipment',
  Charges: 'common.requestKind.charges',
  Demurrage: 'common.requestKind.demurrage',
  Invoice: 'common.requestKind.invoice',
  Documents: 'common.requestKind.documents',
  WarehouseChange: 'common.requestKind.warehouseChange',
  WarehouseChangeBatch: 'common.requestKind.warehouseChangeBatch',
  ServiceOrder: 'common.requestKind.serviceOrder',
  TatcBatch: 'common.requestKind.tatcBatch',
  BlCopy: 'common.requestKind.blCopy',
  ResponsibilityLetter: 'common.requestKind.responsibilityLetter',
  ServiceRequest: 'common.requestKind.serviceRequest',
};

/** Estado de una gestión del dashboard: reúne los estados de las solicitudes del portal. */
export const DASHBOARD_REQUEST_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.requestStatus.pending',
  Queued: 'common.requestStatus.queued',
  Processing: 'common.requestStatus.processing',
  InProgress: 'common.requestStatus.inProgress',
  Requested: 'common.requestStatus.requested',
  Completed: 'common.requestStatus.completed',
  CompletedWithErrors: 'common.requestStatus.completedWithErrors',
  Failed: 'common.requestStatus.failed',
  Cancelled: 'common.requestStatus.cancelled',
  Issued: 'common.requestStatus.issued',
  Superseded: 'common.requestStatus.superseded',
  // Fase 2, Ola G: estados propios de las solicitudes de servicios on demand.
  Draft: 'common.serviceRequestStatus.draft',
  Submitted: 'common.serviceRequestStatus.submitted',
  PendingApproval: 'common.serviceRequestStatus.pendingApproval',
  Approved: 'common.serviceRequestStatus.approved',
  Rejected: 'common.serviceRequestStatus.rejected',
  PendingPayment: 'common.serviceRequestStatus.pendingPayment',
  Paid: 'common.serviceRequestStatus.paid',
};

/** Variante de .hl-badge por estado de la gestión (el texto acompaña siempre al color). */
export const DASHBOARD_REQUEST_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Queued: 'hl-badge--processing',
  Processing: 'hl-badge--processing',
  InProgress: 'hl-badge--processing',
  Requested: 'hl-badge--processing',
  Completed: 'hl-badge--confirmed',
  Issued: 'hl-badge--confirmed',
  CompletedWithErrors: 'hl-badge--pending',
  Failed: 'hl-badge--failed',
  Cancelled: 'hl-badge--failed',
  Superseded: 'hl-badge--processing',
  Draft: 'hl-badge--processing',
  Submitted: 'hl-badge--processing',
  PendingApproval: 'hl-badge--pending',
  Approved: 'hl-badge--confirmed',
  Rejected: 'hl-badge--failed',
  PendingPayment: 'hl-badge--pending',
  Paid: 'hl-badge--confirmed',
};

/** Estado de un pendiente de pago del dashboard. */
export const DASHBOARD_PAYABLE_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.payableStatus.pending',
  Overdue: 'common.payableStatus.overdue',
};

/** Estado de los embarques en los indicadores (los que informa el origen; el resto se muestra tal cual). */
export const SHIPMENT_STATUS_KEYS: Record<string, string> = {
  Booked: 'common.shipmentStatus.booked',
  GateIn: 'common.shipmentStatus.gateIn',
  OnBoard: 'common.shipmentStatus.onBoard',
  InTransit: 'common.shipmentStatus.inTransit',
  Arrived: 'common.shipmentStatus.arrived',
  Discharged: 'common.shipmentStatus.discharged',
  Released: 'common.shipmentStatus.released',
  Delivered: 'common.shipmentStatus.delivered',
  Closed: 'common.shipmentStatus.closed',
};

/** Tipo de documento de transporte (M2-02); las siglas no se traducen, la expansión sí. */
export const TRANSPORT_DOCUMENT_TYPE_KEYS: Record<string, string> = {
  BL: 'common.transportDocumentType.bl',
  SWB: 'common.transportDocumentType.swb',
  EBL: 'common.transportDocumentType.ebl',
};

/** Estado de emisión del documento de transporte (M2-02). */
export const BL_ISSUANCE_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.issuanceStatus.pending',
  Issued: 'common.issuanceStatus.issued',
  AuthorizedAtDestination: 'common.issuanceStatus.authorizedAtDestination',
  IssuedAtDestination: 'common.issuanceStatus.issuedAtDestination',
  Transferred: 'common.issuanceStatus.transferred',
  Surrendered: 'common.issuanceStatus.surrendered',
  TelexReleased: 'common.issuanceStatus.telexReleased',
  Cancelled: 'common.issuanceStatus.cancelled',
  Unknown: 'common.issuanceStatus.unknown',
};

export const BL_ISSUANCE_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Issued: 'hl-badge--confirmed',
  AuthorizedAtDestination: 'hl-badge--processing',
  IssuedAtDestination: 'hl-badge--confirmed',
  Transferred: 'hl-badge--confirmed',
  Surrendered: 'hl-badge--confirmed',
  TelexReleased: 'hl-badge--confirmed',
  Cancelled: 'hl-badge--failed',
  Unknown: 'hl-badge--processing',
};

/** Motivo de publicación por DIFU de destino final (M2-01). */
export const PUBLICATION_REASON_KEYS: Record<string, string> = {
  NO_RULE: 'common.publicationReason.noRule',
  SAME_AS_DISCHARGE: 'common.publicationReason.sameAsDischarge',
  DIFU_ASSOCIATED: 'common.publicationReason.difuAssociated',
  DIFU_MISSING: 'common.publicationReason.difuMissing',
  DIFU_OTHER_LOCATION: 'common.publicationReason.difuOtherLocation',
};

/** Estado del TATC de un contenedor y del BL (M2-09). */
export const TATC_STATUS_KEYS: Record<string, string> = {
  NotIssued: 'common.tatcStatus.notIssued',
  PreTatc: 'common.tatcStatus.preTatc',
  Issued: 'common.tatcStatus.issued',
  Cancelled: 'common.tatcStatus.cancelled',
  Unknown: 'common.tatcStatus.unknown',
  PartiallyIssued: 'common.tatcStatus.partiallyIssued',
  NotRegistered: 'common.tatcStatus.notRegistered',
};

export const TATC_STATUS_CLASS: Record<string, string> = {
  NotIssued: 'hl-badge--pending',
  PreTatc: 'hl-badge--processing',
  Issued: 'hl-badge--confirmed',
  Cancelled: 'hl-badge--failed',
  Unknown: 'hl-badge--processing',
  PartiallyIssued: 'hl-badge--pending',
  NotRegistered: 'hl-badge--processing',
};

/** Motivo por el que el TATC de un contenedor aún no se emite. */
export const TATC_PENDING_REASON_KEYS: Record<string, string> = {
  PAYMENT_PENDING: 'common.tatcPendingReason.paymentPending',
  MHD_PENDING: 'common.tatcPendingReason.mhdPending',
  DOCUMENT_PENDING: 'common.tatcPendingReason.documentPending',
  OTHER: 'common.tatcPendingReason.other',
};

/** Estado de la solicitud masiva de TATC y de cada BL. */
export const TATC_BATCH_STATUS_KEYS: Record<string, string> = {
  Completed: 'common.tatcBatchStatus.completed',
  CompletedWithErrors: 'common.tatcBatchStatus.completedWithErrors',
  Failed: 'common.tatcBatchStatus.failed',
};

export const TATC_BATCH_STATUS_CLASS: Record<string, string> = {
  Completed: 'hl-badge--confirmed',
  CompletedWithErrors: 'hl-badge--pending',
  Failed: 'hl-badge--failed',
};

export const TATC_BATCH_ITEM_STATUS_KEYS: Record<string, string> = {
  Accepted: 'common.tatcBatchItemStatus.accepted',
  Rejected: 'common.tatcBatchItemStatus.rejected',
  Failed: 'common.tatcBatchItemStatus.failed',
};

export const TATC_BATCH_ITEM_STATUS_CLASS: Record<string, string> = {
  Accepted: 'hl-badge--confirmed',
  Rejected: 'hl-badge--failed',
  Failed: 'hl-badge--failed',
};

/** Motivo de una línea de la solicitud masiva no aceptada (portal o sistema de TATC). */
export const TATC_BATCH_REASON_KEYS: Record<string, string> = {
  NOT_FOUND: 'common.tatcBatchReason.notFound',
  NO_PERMISSION: 'common.tatcBatchReason.noPermission',
  NOT_IMPORT: 'common.tatcBatchReason.notImport',
  OTHER_LOCATION: 'common.tatcBatchReason.otherLocation',
  OTHER_COUNTRY: 'common.tatcBatchReason.otherCountry',
  DUPLICATE: 'common.tatcBatchReason.duplicate',
  ALREADY_ISSUED: 'common.tatcBatchReason.alreadyIssued',
  SOURCE_UNAVAILABLE: 'common.tatcBatchReason.sourceUnavailable',
};

/** Tema de la base de conocimiento y de las casillas del asistente (M10-02). */
export const KNOWLEDGE_TOPIC_KEYS: Record<string, string> = {
  GENERAL: 'common.knowledgeTopic.general',
  SHIPPING: 'common.knowledgeTopic.shipping',
  PAYMENTS: 'common.knowledgeTopic.payments',
  DOCUMENTATION: 'common.knowledgeTopic.documentation',
  DEMURRAGE: 'common.knowledgeTopic.demurrage',
  COMMERCIAL: 'common.knowledgeTopic.commercial',
};

/** Tipo de respuesta del asistente que se señala junto al mensaje (falta un dato, no disponible, derivación, rechazo). */
export const ASSISTANT_ANSWER_TYPE_KEYS: Record<string, string> = {
  NeedsReference: 'shared.assistant.answerType.needsReference',
  NotAvailable: 'shared.assistant.answerType.notAvailable',
  SourceUnavailable: 'shared.assistant.answerType.sourceUnavailable',
  NoAnswer: 'shared.assistant.answerType.noAnswer',
  Refused: 'shared.assistant.answerType.refused',
};

/** Fuente citada por el asistente. */
export const ASSISTANT_CITATION_KIND_KEYS: Record<string, string> = {
  KnowledgeArticle: 'shared.assistant.citationKind.knowledgeArticle',
  Shipment: 'shared.assistant.citationKind.shipment',
  Documents: 'shared.assistant.citationKind.documents',
  Charges: 'shared.assistant.citationKind.charges',
  PendingPayments: 'shared.assistant.citationKind.pendingPayments',
  Invoice: 'shared.assistant.citationKind.invoice',
  Tatc: 'shared.assistant.citationKind.tatc',
};

/** Resultado del buscador de mercancías peligrosas (M10-06). */
export const DANGEROUS_GOOD_RESULT_KEYS: Record<string, string> = {
  CLASSIFIED: 'dangerousGoods.result.classified',
  NOT_CLASSIFIED: 'dangerousGoods.result.notClassified',
  NO_MATCH: 'dangerousGoods.result.noMatch',
};

/**
 * Códigos de error del backend de la Ola F → claves Transloco. Los textos del servidor vienen en inglés y no se
 * muestran tal cual.
 */
export const PORTAL_ERRORS: Record<string, string> = {
  'BillOfLading.NotFound': 'common.portalErrors.blNotFound',
  'Error.Forbidden': 'common.portalErrors.forbidden',
  'Tatc.NotApplicable': 'common.portalErrors.tatcNotApplicable',
  'Tatc.NoValidItems': 'common.portalErrors.tatcNoValidItems',
  'TatcBatch.NotFound': 'common.portalErrors.tatcBatchNotFound',
  'ShipmentPublicationRule.NotFound': 'common.portalErrors.publicationRuleNotFound',
  'ShipmentPublicationRule.AlreadyExists': 'common.portalErrors.publicationRuleExists',
  'KnowledgeArticle.NotFound': 'common.portalErrors.articleNotFound',
  'AssistantSession.NotFound': 'common.portalErrors.sessionNotFound',
  'AssistantSession.Ended': 'common.portalErrors.sessionEnded',
  'AssistantSession.Expired': 'common.portalErrors.sessionExpired',
  'AssistantSession.NoRecipient': 'common.portalErrors.noRecipient',
  'Assistant.RateLimited': 'common.portalErrors.rateLimited',
  'DangerousGood.InvalidImport': 'common.portalErrors.invalidImport',
  'Integration.Unavailable': 'common.portalErrors.sourceUnavailable',
  'Integration.Timeout': 'common.portalErrors.sourceUnavailable',
  // Fase 2, Ola J: entrega de documentos por el asistente (M10-04); los permisos se vuelven a validar al descargar.
  'AssistantDelivery.NotFound': 'common.portalErrors.deliveryNotFound',
  'ShipmentDocument.NotFound': 'common.portalErrors.deliveryAccessLost',
  'ShipmentDocumentContent.NotFound': 'common.portalErrors.deliveryContentNotFound',
};

// ---------------------------------------------------------------------------
// Fase 2, Ola G: servicios on demand configurables (M2-03, M2-04, M3-07 a M3-15) e historial del cambio de
// almacén (M3-06).
// ---------------------------------------------------------------------------

/** Estado de una solicitud de servicio. */
export const SERVICE_REQUEST_STATUS_KEYS: Record<string, string> = {
  Draft: 'common.serviceRequestStatus.draft',
  Submitted: 'common.serviceRequestStatus.submitted',
  PendingApproval: 'common.serviceRequestStatus.pendingApproval',
  Approved: 'common.serviceRequestStatus.approved',
  Rejected: 'common.serviceRequestStatus.rejected',
  PendingPayment: 'common.serviceRequestStatus.pendingPayment',
  Paid: 'common.serviceRequestStatus.paid',
  InProgress: 'common.serviceRequestStatus.inProgress',
  Completed: 'common.serviceRequestStatus.completed',
  Cancelled: 'common.serviceRequestStatus.cancelled',
};

/** Variante de .hl-badge por estado de la solicitud (el texto acompaña siempre al color). */
export const SERVICE_REQUEST_STATUS_CLASS: Record<string, string> = {
  Draft: 'hl-badge--processing',
  Submitted: 'hl-badge--processing',
  PendingApproval: 'hl-badge--pending',
  Approved: 'hl-badge--confirmed',
  Rejected: 'hl-badge--failed',
  PendingPayment: 'hl-badge--pending',
  Paid: 'hl-badge--confirmed',
  InProgress: 'hl-badge--processing',
  Completed: 'hl-badge--confirmed',
  Cancelled: 'hl-badge--failed',
};

/** Equipo interno que aprueba o atiende (ED, Customer Service). */
export const SERVICE_TEAM_KEYS: Record<string, string> = {
  None: 'common.serviceTeam.none',
  ED: 'common.serviceTeam.ed',
  CustomerService: 'common.serviceTeam.customerService',
};

/** Motivo por el que un servicio no está disponible para el embarque o el usuario. */
export const SERVICE_UNAVAILABLE_REASON_KEYS: Record<string, string> = {
  BL_STATUS: 'common.serviceUnavailableReason.blStatus',
  NOT_DEPARTED: 'common.serviceUnavailableReason.notDeparted',
  ALREADY_DEPARTED: 'common.serviceUnavailableReason.alreadyDeparted',
  NOT_ARRIVED: 'common.serviceUnavailableReason.notArrived',
  NO_CONTAINERS: 'common.serviceUnavailableReason.noContainers',
  NOT_OVERDUE: 'common.serviceUnavailableReason.notOverdue',
  MILESTONE_UNAVAILABLE: 'common.serviceUnavailableReason.milestoneUnavailable',
  ALREADY_REQUESTED: 'common.serviceUnavailableReason.alreadyRequested',
  NO_SOURCE_CHARGE: 'common.serviceUnavailableReason.noSourceCharge',
  NO_PERMISSION: 'common.serviceUnavailableReason.noPermission',
  TARIFF_NOT_IN_FORCE: 'common.serviceUnavailableReason.tariffNotInForce',
};

/** Dentro o fuera de plazo respecto del hito del embarque (M3-13, M3-14). */
export const SERVICE_TIMING_KEYS: Record<string, string> = {
  NotApplicable: 'common.serviceTiming.notApplicable',
  InTime: 'common.serviceTiming.inTime',
  Late: 'common.serviceTiming.late',
};

export const SERVICE_TIMING_CLASS: Record<string, string> = {
  NotApplicable: 'hl-badge--processing',
  InTime: 'hl-badge--confirmed',
  Late: 'hl-badge--pending',
};

/** Hito desde el que se mide el plazo en la cotización. */
export const SERVICE_MILESTONE_SOURCE_KEYS: Record<string, string> = {
  CUSTOMS_DEADLINE: 'common.serviceMilestoneSource.customsDeadline',
  ETD: 'common.serviceMilestoneSource.etd',
  ETA: 'common.serviceMilestoneSource.eta',
};

export const SERVICE_PRICING_MODE_KEYS: Record<string, string> = {
  None: 'common.servicePricingMode.none',
  Tariff: 'common.servicePricingMode.tariff',
  SourceCharge: 'common.servicePricingMode.sourceCharge',
};

export const SERVICE_QUANTITY_MODE_KEYS: Record<string, string> = {
  PerRequest: 'common.serviceQuantityMode.perRequest',
  PerContainer: 'common.serviceQuantityMode.perContainer',
};

/** Hito del embarque que usa la definición. */
export const SERVICE_MILESTONE_KEYS: Record<string, string> = {
  None: 'common.serviceMilestone.none',
  VesselDeparture: 'common.serviceMilestone.vesselDeparture',
  VesselArrival: 'common.serviceMilestone.vesselArrival',
  CustomsDeadline: 'common.serviceMilestone.customsDeadline',
};

export const SERVICE_TIMING_RULE_KEYS: Record<string, string> = {
  None: 'common.serviceTimingRule.none',
  InTimeAndLate: 'common.serviceTimingRule.inTimeAndLate',
  LateOnly: 'common.serviceTimingRule.lateOnly',
};

export const SERVICE_AVAILABILITY_WINDOW_KEYS: Record<string, string> = {
  Always: 'common.serviceAvailabilityWindow.always',
  BeforeDeparture: 'common.serviceAvailabilityWindow.beforeDeparture',
  AfterDeparture: 'common.serviceAvailabilityWindow.afterDeparture',
  AfterArrival: 'common.serviceAvailabilityWindow.afterArrival',
};

export const SERVICE_REFERENCE_TYPE_KEYS: Record<string, string> = {
  BL: 'common.serviceReferenceType.bl',
  Booking: 'common.serviceReferenceType.booking',
};

/** Tipo de un campo del formulario del servicio. */
export const SERVICE_FIELD_TYPE_KEYS: Record<string, string> = {
  text: 'common.serviceFieldType.text',
  textarea: 'common.serviceFieldType.textarea',
  date: 'common.serviceFieldType.date',
  number: 'common.serviceFieldType.number',
  select: 'common.serviceFieldType.select',
  file: 'common.serviceFieldType.file',
  containers: 'common.serviceFieldType.containers',
};

/** Quién registró un evento de la línea de tiempo. */
export const SERVICE_ACTOR_KIND_KEYS: Record<string, string> = {
  Client: 'common.serviceActorKind.client',
  Internal: 'common.serviceActorKind.internal',
  System: 'common.serviceActorKind.system',
};

/** Evento de la trazabilidad de un cambio de almacén (M3-06). */
export const WAREHOUSE_HISTORY_EVENT_KEYS: Record<string, string> = {
  Requested: 'common.warehouseHistoryEvent.requested',
  FreeEntitlementApplied: 'common.warehouseHistoryEvent.freeEntitlementApplied',
  PaymentStatusChanged: 'common.warehouseHistoryEvent.paymentStatusChanged',
  Completed: 'common.warehouseHistoryEvent.completed',
  Cancelled: 'common.warehouseHistoryEvent.cancelled',
};

/**
 * Códigos de error del backend de la Ola G → claves Transloco. Los textos del servidor vienen en inglés y no se
 * muestran tal cual.
 */
export const SERVICE_REQUEST_ERRORS: Record<string, string> = {
  'BillOfLading.NotFound': 'common.serviceRequestErrors.blNotFound',
  'Error.Forbidden': 'common.serviceRequestErrors.forbidden',
  'Cart.AssociationRequired': 'common.serviceRequestErrors.associationRequired',
  'ServiceDefinition.NotFound': 'common.serviceRequestErrors.definitionNotFound',
  'ServiceDefinition.AlreadyExists': 'common.serviceRequestErrors.definitionExists',
  'ServiceDefinition.UnknownAction': 'common.serviceRequestErrors.unknownAction',
  'ServiceDefinition.Invalid': 'common.serviceRequestErrors.definitionInvalid',
  'ServiceDefinition.InvalidSchema': 'common.serviceRequestErrors.definitionInvalidSchema',
  'ChargeConcept.NotFound': 'common.serviceRequestErrors.conceptNotFound',
  'ServiceRequest.NotFound': 'common.serviceRequestErrors.notFound',
  'ServiceRequest.NotAvailable': 'common.serviceRequestErrors.notAvailable',
  'ServiceRequest.ContainersRequired': 'common.serviceRequestErrors.containersRequired',
  'ServiceRequest.MeasureRequired': 'common.serviceRequestErrors.measureRequired',
  'ServiceRequest.NoSourceCharge': 'common.serviceRequestErrors.noSourceCharge',
  'ServiceRequest.BillingTaxIdNotAllowed': 'common.serviceRequestErrors.billingTaxIdNotAllowed',
  'ServiceRequest.BillingDataRequired': 'common.serviceRequestErrors.billingDataRequired',
  'ServiceRequest.TariffNotAccepted': 'common.serviceRequestErrors.tariffNotAccepted',
  'ServiceRequest.TariffChanged': 'common.serviceRequestErrors.tariffChanged',
  'ServiceRequest.NotEditable': 'common.serviceRequestErrors.notEditable',
  'ServiceRequest.PaymentInProgress': 'common.serviceRequestErrors.paymentInProgress',
  'ServiceRequest.InvalidTransition': 'common.serviceRequestErrors.invalidTransition',
  'ServiceRequest.UnknownFileField': 'common.serviceRequestErrors.unknownFileField',
  'ServiceRequest.OutputDocumentRequired': 'common.serviceRequestErrors.outputDocumentRequired',
  'ServiceRequest.NotAssignedToTeam': 'common.serviceRequestErrors.notAssignedToTeam',
  'ServiceRequestAttachment.NotFound': 'common.serviceRequestErrors.attachmentNotFound',
  'Tariff.NotInForce': 'common.serviceRequestErrors.tariffNotInForce',
  'Tariff.NotCovered': 'common.serviceRequestErrors.tariffNotCovered',
  'ChargeRules.ConditionsUnavailable': 'common.serviceRequestErrors.conditionsUnavailable',
  'Integration.Unavailable': 'common.serviceRequestErrors.sourceUnavailable',
  'WarehouseChange.NotFound': 'common.serviceRequestErrors.warehouseChangeNotFound',
  'Reinvoicing.UseDedicatedFlow': 'common.serviceRequestErrors.useDedicatedFlow',
  // Fase 2, Ola J: aprobación de la carta de liberación con la regla de TATC emitido (M6-08, M2-09).
  'ServiceRequest.UseDedicatedFlow': 'common.serviceRequestErrors.useDedicatedFlow',
  'ReleaseLetter.TatcNotIssued': 'common.serviceRequestErrors.releaseLetterTatcNotIssued',
  'ReleaseLetter.TatcUnavailable': 'common.serviceRequestErrors.releaseLetterTatcUnavailable',
};

// ---------------------------------------------------------------------------
// Fase 2, Ola H: estado de cuenta (M7-03), pago por ítem con crédito (M5-10), comprobante de depósito (M5-06),
// refacturación IAO (M3-11) y pago anticipado de Gate Out (M3-19).
// ---------------------------------------------------------------------------

/** Facturado, calculado no facturado o imputado a crédito (M7-03). */
export const STATEMENT_LINE_KIND_KEYS: Record<string, string> = {
  Invoiced: 'common.statementLineKind.invoiced',
  Uninvoiced: 'common.statementLineKind.uninvoiced',
  CreditImputed: 'common.statementLineKind.creditImputed',
};

/** Estado de una línea del estado de cuenta y del filtro (incluye "por vencer"). */
export const STATEMENT_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.statementStatus.pending',
  Overdue: 'common.statementStatus.overdue',
  DueSoon: 'common.statementStatus.dueSoon',
  Covered: 'common.statementStatus.covered',
  Uninvoiced: 'common.statementStatus.uninvoiced',
  CreditImputed: 'common.statementStatus.creditImputed',
};

/** Variante de .hl-badge por estado de la línea (el texto acompaña siempre al color). */
export const STATEMENT_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Overdue: 'hl-badge--failed',
  Covered: 'hl-badge--confirmed',
  Uninvoiced: 'hl-badge--processing',
  CreditImputed: 'hl-badge--processing',
};

export const STATEMENT_DOCUMENT_TYPE_KEYS: Record<string, string> = {
  Invoice: 'common.statementDocumentType.invoice',
  ExemptInvoice: 'common.statementDocumentType.exemptInvoice',
  DebitNote: 'common.statementDocumentType.debitNote',
  LocalCharge: 'common.statementDocumentType.localCharge',
  ServiceCharge: 'common.statementDocumentType.serviceCharge',
  Demurrage: 'common.statementDocumentType.demurrage',
  Freight: 'common.statementDocumentType.freight',
};

/** Criterio de orden de las líneas. */
export const STATEMENT_SORT_KEYS: Record<string, string> = {
  dueDate: 'common.statementSort.dueDate',
  issueDate: 'common.statementSort.issueDate',
  amount: 'common.statementSort.amount',
  blNumber: 'common.statementSort.blNumber',
};

/** Por qué no se informa el crédito disponible. */
export const CREDIT_UNAVAILABLE_REASON_KEYS: Record<string, string> = {
  LIMIT_NOT_INFORMED: 'common.creditUnavailableReason.limitNotInformed',
  EXCHANGE_RATE_UNAVAILABLE: 'common.creditUnavailableReason.exchangeRateUnavailable',
};

/** Concepto de crédito de Nexus al que se imputa un concepto del portal (M5-10). */
export const NEXUS_CREDIT_CONCEPT_KEYS: Record<string, string> = {
  LOCAL_CHARGES: 'common.nexusCreditConcept.localCharges',
  MHD: 'common.nexusCreditConcept.mhd',
  FREIGHT: 'common.nexusCreditConcept.freight',
  STORAGE: 'common.nexusCreditConcept.storage',
};

/** Anticipo (pagado antes de la factura) o imputación a crédito. */
export const SETTLEMENT_KIND_KEYS: Record<string, string> = {
  Advance: 'common.settlementKind.advance',
  CreditImputation: 'common.settlementKind.creditImputation',
};

export const SETTLEMENT_STATUS_KEYS: Record<string, string> = {
  Open: 'common.settlementStatus.open',
  Matched: 'common.settlementStatus.matched',
};

export const SETTLEMENT_STATUS_CLASS: Record<string, string> = {
  Open: 'hl-badge--pending',
  Matched: 'hl-badge--confirmed',
};

/** Revisión de un comprobante de depósito por Finanzas (M5-06). */
export const DEPOSIT_PROOF_STATUS_KEYS: Record<string, string> = {
  Submitted: 'common.depositProofStatus.submitted',
  Verified: 'common.depositProofStatus.verified',
  Rejected: 'common.depositProofStatus.rejected',
};

export const DEPOSIT_PROOF_STATUS_CLASS: Record<string, string> = {
  Submitted: 'hl-badge--processing',
  Verified: 'hl-badge--confirmed',
  Rejected: 'hl-badge--failed',
};

/** Respuesta de la nueva razón social a la refacturación (M3-11). */
export const REINVOICING_ACCEPTANCE_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.reinvoicingAcceptanceStatus.pending',
  Accepted: 'common.reinvoicingAcceptanceStatus.accepted',
  Declined: 'common.reinvoicingAcceptanceStatus.declined',
};

export const REINVOICING_ACCEPTANCE_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Accepted: 'hl-badge--confirmed',
  Declined: 'hl-badge--failed',
};

/**
 * Códigos de error de la refacturación IAO (también los motivos de no elegibilidad de la cotización) → claves
 * Transloco. Los textos del servidor vienen en inglés y no se muestran tal cual.
 */
export const REINVOICING_ERRORS: Record<string, string> = {
  'Reinvoicing.InvoiceNotEligible': 'common.reinvoicingErrors.invoiceNotEligible',
  'Reinvoicing.AlreadyRequested': 'common.reinvoicingErrors.alreadyRequested',
  'Reinvoicing.ApprovalRequired': 'common.reinvoicingErrors.approvalRequired',
  'Reinvoicing.SameTaxId': 'common.reinvoicingErrors.sameTaxId',
  'Reinvoicing.AcceptanceExpired': 'common.reinvoicingErrors.acceptanceExpired',
  'Reinvoicing.AcceptanceClosed': 'common.reinvoicingErrors.acceptanceClosed',
  'Reinvoicing.AcceptorTaxIdRequired': 'common.reinvoicingErrors.acceptorTaxIdRequired',
  'Reinvoicing.AcceptanceNotPending': 'common.reinvoicingErrors.acceptanceNotPending',
  'Reinvoicing.InvoiceNotIssued': 'common.reinvoicingErrors.invoiceNotIssued',
  'ReinvoicingAcceptance.NotFound': 'common.reinvoicingErrors.acceptanceNotFound',
  'Invoice.NotFound': 'common.reinvoicingErrors.invoiceNotFound',
  'ServiceRequest.NotFound': 'common.reinvoicingErrors.notFound',
  'ServiceRequest.BillingDataRequired': 'common.reinvoicingErrors.billingDataRequired',
  'ServiceRequest.TariffNotAccepted': 'common.reinvoicingErrors.tariffNotAccepted',
  'ServiceRequest.TariffChanged': 'common.reinvoicingErrors.tariffChanged',
  'ServiceRequest.NotEditable': 'common.reinvoicingErrors.notEditable',
  'ServiceRequest.UnknownFileField': 'common.reinvoicingErrors.unknownFileField',
  'Tariff.NotInForce': 'common.reinvoicingErrors.tariffNotInForce',
  'ExchangeRate.NotFound': 'common.reinvoicingErrors.exchangeRateNotFound',
  'Integration.Unavailable': 'common.reinvoicingErrors.sourceUnavailable',
  'Error.Forbidden': 'common.reinvoicingErrors.forbidden',
};

// ---------------------------------------------------------------------------
// Fase 2, Ola I: bandeja de notificaciones (M1-25), comunicados (M1-26), modo guía (M1-27), área de administración
// (M8-05), vista como cliente (M8-08), reportería (M9-01), Counter (M8-09), contactos (M1-06), transportistas
// pre-creados (M1-09) y empresa matriz (M1-21).
// ---------------------------------------------------------------------------

/** Módulo de origen de una notificación (filtros de la bandeja). */
export const NOTIFICATION_MODULE_KEYS: Record<string, string> = {
  Organization: 'notifications.module.organization',
  Access: 'notifications.module.access',
  Payments: 'notifications.module.payments',
  Documents: 'notifications.module.documents',
  Services: 'notifications.module.services',
  Finance: 'notifications.module.finance',
  Customs: 'notifications.module.customs',
  Deadlines: 'notifications.module.deadlines',
  Announcements: 'notifications.module.announcements',
  Administration: 'notifications.module.administration',
  General: 'notifications.module.general',
};

/** Texto del botón o enlace de la acción de una notificación. */
export const NOTIFICATION_ACTION_KEYS: Record<string, string> = {
  ApproveJoinRequest: 'notifications.action.approveJoinRequest',
  ReviewOrganization: 'notifications.action.reviewOrganization',
  ReviewParentLink: 'notifications.action.reviewParentLink',
  VerifyDepositProof: 'notifications.action.verifyDepositProof',
  UploadDepositProof: 'notifications.action.uploadDepositProof',
  OpenPayment: 'notifications.action.openPayment',
  OpenServiceRequest: 'notifications.action.openServiceRequest',
  OpenShipment: 'notifications.action.openShipment',
  OpenDocument: 'notifications.action.openDocument',
  OpenAccessGrants: 'notifications.action.openAccessGrants',
  OpenAnnouncement: 'notifications.action.openAnnouncement',
  OpenInvoice: 'notifications.action.openInvoice',
};

/** Gestión a la que corresponde una notificación (M1-25: identifica el embarque o la gestión). */
export const NOTIFICATION_ENTITY_KEYS: Record<string, string> = {
  Shipment: 'notifications.entity.shipment',
  Organization: 'notifications.entity.organization',
  JoinRequest: 'notifications.entity.joinRequest',
  AccessGrant: 'notifications.entity.accessGrant',
  Payment: 'notifications.entity.payment',
  ShipmentDocument: 'notifications.entity.shipmentDocument',
  ServiceRequest: 'notifications.entity.serviceRequest',
  DepositProof: 'notifications.entity.depositProof',
  CustomsTransmission: 'notifications.entity.customsTransmission',
  Deadline: 'notifications.entity.deadline',
  Announcement: 'notifications.entity.announcement',
  ParentLink: 'notifications.entity.parentLink',
  CarrierPreRegistration: 'notifications.entity.carrierPreRegistration',
};

export const ANNOUNCEMENT_OPERATION_KEYS: Record<string, string> = {
  Import: 'common.announcementOperation.import',
  Export: 'common.announcementOperation.export',
  Both: 'common.announcementOperation.both',
};

export const ANNOUNCEMENT_SEVERITY_KEYS: Record<string, string> = {
  Info: 'common.announcementSeverity.info',
  Important: 'common.announcementSeverity.important',
};

export const ANNOUNCEMENT_STATUS_KEYS: Record<string, string> = {
  Draft: 'common.announcementStatus.draft',
  Published: 'common.announcementStatus.published',
  Unpublished: 'common.announcementStatus.unpublished',
};

export const ANNOUNCEMENT_STATUS_CLASS: Record<string, string> = {
  Draft: 'hl-badge--pending',
  Published: 'hl-badge--active',
  Unpublished: 'hl-badge--processing',
};

export const GUIDE_AUDIENCE_KEYS: Record<string, string> = {
  Client: 'common.guideAudience.client',
  Internal: 'common.guideAudience.internal',
  All: 'common.guideAudience.all',
};

export const IMPERSONATION_STATUS_KEYS: Record<string, string> = {
  Active: 'common.impersonationStatus.active',
  Ended: 'common.impersonationStatus.ended',
  Expired: 'common.impersonationStatus.expired',
};

export const IMPERSONATION_STATUS_CLASS: Record<string, string> = {
  Active: 'hl-badge--active',
  Ended: 'hl-badge--processing',
  Expired: 'hl-badge--pending',
};

export const IMPERSONATION_END_REASON_KEYS: Record<string, string> = {
  Manual: 'common.impersonationEndReason.manual',
  Logout: 'common.impersonationEndReason.logout',
  Expired: 'common.impersonationEndReason.expired',
  Replaced: 'common.impersonationEndReason.replaced',
  Admin: 'common.impersonationEndReason.admin',
};

export const IMPERSONATION_REQUEST_ACTION_KEYS: Record<string, string> = {
  Started: 'common.impersonationRequestAction.started',
  Request: 'common.impersonationRequestAction.request',
  BlockedWrite: 'common.impersonationRequestAction.blockedWrite',
  Ended: 'common.impersonationRequestAction.ended',
};

export const TRANSACTION_CATEGORY_KEYS: Record<string, string> = {
  LocalCharge: 'common.transactionCategory.localCharge',
  OnDemandService: 'common.transactionCategory.onDemandService',
  Demurrage: 'common.transactionCategory.demurrage',
  Freight: 'common.transactionCategory.freight',
  WarehouseChange: 'common.transactionCategory.warehouseChange',
  Invoice: 'common.transactionCategory.invoice',
  Other: 'common.transactionCategory.other',
};

export const EXCEPTION_TYPE_KEYS: Record<string, string> = {
  GateInExemption: 'common.exceptionType.gateInExemption',
  EdsExemption: 'common.exceptionType.edsExemption',
  GateOutExemption: 'common.exceptionType.gateOutExemption',
  OtherExemption: 'common.exceptionType.otherExemption',
  IpoExclusion: 'common.exceptionType.ipoExclusion',
  FreeWarehouseChange: 'common.exceptionType.freeWarehouseChange',
  CreditImputation: 'common.exceptionType.creditImputation',
};

export const COUNTER_SYNC_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.counterSyncStatus.pending',
  Synced: 'common.counterSyncStatus.synced',
  Failed: 'common.counterSyncStatus.failed',
};

export const COUNTER_SYNC_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Synced: 'hl-badge--confirmed',
  Failed: 'hl-badge--failed',
};

export const CONTACT_REPORT_TYPE_KEYS: Record<string, string> = {
  ARRIVAL_NOTICE: 'common.contactReportType.arrivalNotice',
  BL_COPIES: 'common.contactReportType.blCopies',
  INVOICES: 'common.contactReportType.invoices',
  FREE_TIME: 'common.contactReportType.freeTime',
  DEMURRAGE: 'common.contactReportType.demurrage',
  BOOKING_CONFIRMATION: 'common.contactReportType.bookingConfirmation',
};

export const CONTACT_CHANGE_STATUS_KEYS: Record<string, string> = {
  Propagated: 'common.contactChangeStatus.propagated',
  Failed: 'common.contactChangeStatus.failed',
};

export const CARRIER_PRE_REGISTRATION_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.carrierPreRegistrationStatus.pending',
  Activated: 'common.carrierPreRegistrationStatus.activated',
};

export const PARENT_LINK_STATUS_KEYS: Record<string, string> = {
  Pending: 'common.parentLinkStatus.pending',
  Active: 'common.parentLinkStatus.active',
  Rejected: 'common.parentLinkStatus.rejected',
  Removed: 'common.parentLinkStatus.removed',
};

export const PARENT_LINK_STATUS_CLASS: Record<string, string> = {
  Pending: 'hl-badge--pending',
  Active: 'hl-badge--active',
  Rejected: 'hl-badge--rejected',
  Removed: 'hl-badge--processing',
};

/**
 * Códigos de error de la Ola I (Fase 2) → claves Transloco. Los textos del servidor vienen en inglés y no se muestran
 * tal cual.
 */
export const ADMINISTRATION_ERRORS: Record<string, string> = {
  'Error.Forbidden': 'common.adminErrors.forbidden',
  'Impersonation.ReadOnly': 'common.adminErrors.impersonationReadOnly',
  'Impersonation.Ended': 'common.adminErrors.impersonationEnded',
  'Impersonation.Nested': 'common.adminErrors.impersonationNested',
  'Impersonation.TargetNotAllowed': 'common.adminErrors.impersonationTargetNotAllowed',
  'Impersonation.TargetNotFound': 'common.adminErrors.impersonationTargetNotFound',
  'Impersonation.NotInternalActor': 'common.adminErrors.impersonationNotInternalActor',
  'Impersonation.NotActive': 'common.adminErrors.impersonationNotActive',
  'Notification.NotFound': 'common.adminErrors.notificationNotFound',
  'Notification.UnknownType': 'common.adminErrors.notificationUnknownType',
  'Notification.EmailNotAvailable': 'common.adminErrors.notificationEmailNotAvailable',
  'Notification.EmailMandatory': 'common.adminErrors.notificationEmailMandatory',
  'Announcement.NotFound': 'common.adminErrors.announcementNotFound',
  'Announcement.InvalidTransition': 'common.adminErrors.announcementInvalidTransition',
  'Announcement.Expired': 'common.adminErrors.announcementExpired',
  'Guide.NotFound': 'common.adminErrors.guideNotFound',
  'Guide.AlreadyExists': 'common.adminErrors.guideAlreadyExists',
  'Counter.AlreadySynced': 'common.adminErrors.counterAlreadySynced',
  'Counter.Invalid': 'common.adminErrors.counterInvalid',
  'BillOfLading.NotFound': 'common.adminErrors.blNotFound',
  'ContactList.NotAllowed': 'common.adminErrors.contactListNotAllowed',
  'ContactList.MatchCodeRequired': 'common.adminErrors.contactListMatchCodeRequired',
  'CarrierPreCreation.AlreadyActivated': 'common.adminErrors.carrierAlreadyActivated',
  'CarrierPreCreation.AlreadyRegistered': 'common.adminErrors.carrierAlreadyRegistered',
  'CarrierPreCreation.NotACarrier': 'common.adminErrors.carrierNotACarrier',
  'CarrierPreCreation.EmailInUse': 'common.adminErrors.carrierEmailInUse',
  'CarrierPreCreation.NotAllowed': 'common.adminErrors.carrierNotAllowed',
  'ParentLink.NotAllowed': 'common.adminErrors.parentLinkNotAllowed',
  'ParentLink.AlreadyExists': 'common.adminErrors.parentLinkAlreadyExists',
  'ParentLink.SelfLink': 'common.adminErrors.parentLinkSelfLink',
  'ParentLink.InvalidParent': 'common.adminErrors.parentLinkInvalidParent',
  'ParentLink.Cycle': 'common.adminErrors.parentLinkCycle',
  'ParentLink.NotPending': 'common.adminErrors.parentLinkNotPending',
  'ParentLink.NotActive': 'common.adminErrors.parentLinkNotActive',
  'Integration.Unavailable': 'common.adminErrors.sourceUnavailable',
  'Integration.Timeout': 'common.adminErrors.sourceUnavailable',
};

// ---------------------------------------------------------------------------
// Fase 2, Ola J: certificado de flete (M6-02), carta de liberación y desconsolidado (M6-08), entrega de documentos por el
// asistente (M10-04) y canal Web Service (M3-17).
// ---------------------------------------------------------------------------

/** Finalidad del certificado de flete. */
export const FREIGHT_PURPOSE_KEYS: Record<string, string> = {
  CUSTOMS: 'common.freightPurpose.customs',
  INSURANCE: 'common.freightPurpose.insurance',
  BANK: 'common.freightPurpose.bank',
  OTHER: 'common.freightPurpose.other',
};

/** Tipo de sociedad del consignatario de la carta de liberación. */
export const LEGAL_ENTITY_TYPE_KEYS: Record<string, string> = {
  COMPANY: 'common.legalEntityType.company',
  NATURAL_PERSON: 'common.legalEntityType.naturalPerson',
};

/** Origen del transportista registrado elegible para la carta. */
export const RELEASE_LETTER_CARRIER_SOURCE_KEYS: Record<string, string> = {
  Grant: 'common.releaseLetterCarrierSource.grant',
  PreCreated: 'common.releaseLetterCarrierSource.preCreated',
};

/** Proceso habilitado para un cliente del canal Web Service. */
export const API_CLIENT_SCOPE_KEYS: Record<string, string> = {
  'responsibility-letter': 'common.apiClientScope.responsibilityLetter',
  'warehouse-change': 'common.apiClientScope.warehouseChange',
};

export const API_CLIENT_STATUS_KEYS: Record<string, string> = {
  Active: 'common.apiClientStatus.active',
  Revoked: 'common.apiClientStatus.revoked',
};

export const API_CLIENT_STATUS_CLASS: Record<string, string> = {
  Active: 'hl-badge--confirmed',
  Revoked: 'hl-badge--failed',
};

/** Operación registrada en la bitácora del canal. */
export const WS_OPERATION_KEYS: Record<string, string> = {
  ResponsibilityLetter: 'common.wsOperation.responsibilityLetter',
  WarehouseChange: 'common.wsOperation.warehouseChange',
  WarehouseChangeBatch: 'common.wsOperation.warehouseChangeBatch',
  ResponsibilityLetterTerms: 'common.wsOperation.responsibilityLetterTerms',
  ListRequests: 'common.wsOperation.listRequests',
  GetRequest: 'common.wsOperation.getRequest',
  ClientInfo: 'common.wsOperation.clientInfo',
};

export const WS_OUTCOME_KEYS: Record<string, string> = {
  Processing: 'common.wsOutcome.processing',
  Accepted: 'common.wsOutcome.accepted',
  Rejected: 'common.wsOutcome.rejected',
  Failed: 'common.wsOutcome.failed',
};

export const WS_OUTCOME_CLASS: Record<string, string> = {
  Processing: 'hl-badge--processing',
  Accepted: 'hl-badge--confirmed',
  Rejected: 'hl-badge--rejected',
  Failed: 'hl-badge--failed',
};

/**
 * Códigos de error de la administración del canal Web Service → claves Transloco. Los textos del servidor vienen en
 * inglés y no se muestran tal cual.
 */
export const API_CLIENT_ERRORS: Record<string, string> = {
  'Error.Forbidden': 'common.apiClientErrors.forbidden',
  'Organization.NotFound': 'common.apiClientErrors.organizationNotFound',
  'ApiClient.OrganizationNotAllowed': 'common.apiClientErrors.organizationNotAllowed',
  'ApiClient.NotFound': 'common.apiClientErrors.notFound',
  'ApiClient.AlreadyRevoked': 'common.apiClientErrors.alreadyRevoked',
  'ApiClientKey.NotFound': 'common.apiClientErrors.keyNotFound',
};
