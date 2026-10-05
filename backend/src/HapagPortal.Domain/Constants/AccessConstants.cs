namespace HapagPortal.Domain.Constants;

/// <summary>
/// Roles por embarque y columnas de la matriz base de M1-11. Customer, Shipper y Consignee se
/// obtienen del BL; ThirdParty queda reservado para los accesos otorgados (M1-12 en adelante);
/// CustomsAgency y Carrier aplican por el tipo de la organización.
/// </summary>
public static class ShipmentRoleCodes
{
    public const string Customer = "Customer";
    public const string Shipper = "Shipper";
    public const string Consignee = "Consignee";
    public const string ThirdParty = "ThirdParty";
    public const string CustomsAgency = "CustomsAgency";
    public const string Carrier = "Carrier";

    /// <summary>Columnas de la matriz, en el orden del documento.</summary>
    public static readonly string[] MatrixColumns = [Customer, Shipper, Consignee, ThirdParty, CustomsAgency, Carrier];

    /// <summary>Roles que se asignan desde los datos del embarque.</summary>
    public static readonly string[] ShipmentParties = [Customer, Shipper, Consignee];
}

/// <summary>Origen del vínculo entre una organización y un BL.</summary>
public static class ShipmentRoleSources
{
    public const string Import = "Import";
    public const string Seed = "Seed";
    public const string Manual = "Manual";
}

/// <summary>
/// Niveles de la matriz de M1-11: O = puede y se le puede retirar; X = no puede ni se le puede
/// otorgar; X (o) = no puede salvo otorgamiento expreso.
/// </summary>
public static class AccessLevels
{
    public const string Allowed = "Allowed";
    public const string Denied = "Denied";
    public const string OnGrant = "OnGrant";

    public static readonly string[] All = [Allowed, Denied, OnGrant];
}

public static class ShipmentActionCategories
{
    public const string Information = "Information";
    public const string Administration = "Administration";
}

/// <summary>View: consulta o descarga. Operate: requiere además un perfil de usuario que opere.</summary>
public static class ShipmentActionKinds
{
    public const string View = "View";
    public const string Operate = "Operate";
}

/// <summary>Alcance de la acción: sobre el BL/booking o sobre la propia organización.</summary>
public static class ShipmentActionScopes
{
    public const string Shipment = "Shipment";
    public const string Organization = "Organization";
}

/// <summary>Códigos estables de las acciones de la matriz base de M1-11.</summary>
public static class ShipmentActionCodes
{
    // Visibilidad de información y gestión de servicios
    public const string ViewShipment = "shipment.view";
    public const string ViewReleaseRequirements = "release-requirements.view";
    public const string ViewTracking = "tracking.view";
    public const string ViewBlIssuance = "bl-issuance.view";
    public const string RequestUnvaluedBlCopy = "bl-copy-unvalued.request";
    public const string RequestValuedBlCopy = "bl-copy-valued.request";
    public const string DownloadNoDebtCertificate = "no-debt-certificate.download";
    public const string DownloadTatc = "tatc.download";
    public const string PayFreight = "freight.pay";
    public const string PayMandatoryLocalCharges = "local-charges-mandatory.pay";
    public const string PayOnDemandLocalCharges = "local-charges-on-demand.pay";
    public const string GenerateReleaseLetter = "release-letter.generate";
    public const string GenerateTransshipmentCertificate = "transshipment-certificate.generate";
    public const string GenerateFreightCertificate = "freight-certificate.generate";
    public const string GenerateResponsibilityLetter = "responsibility-letter.generate";
    public const string PayImportDemurrage = "import-demurrage.pay";
    public const string RequestDropOff = "drop-off.request";
    public const string RequestWarehouseChange = "warehouse-change.request";
    public const string ViewAccountStatement = "account-statement.view";
    public const string ViewInvoicesAsBilled = "invoices-billed.view";
    public const string ViewInvoicesAsPayer = "invoices-payer.view";
    public const string DownloadCollectReceipt = "collect-receipt.download";
    public const string ViewImportDepot = "import-depot.view";
    public const string ViewExportDepot = "export-depot.view";

    // Administración de accesos y de la organización
    public const string GrantAccess = "access.grant";
    public const string RevokeAccess = "access.revoke";
    public const string SetAccessValidity = "access-validity.set";
    public const string ConfigureDefaultAgents = "default-agents.configure";
    public const string EnableOpenAccess = "open-access.enable";
    public const string SearchOpenAccess = "open-access.search";
    public const string SelfAssociate = "open-access.self-associate";
    public const string ReceiveThirdPartyQueryNotice = "third-party-query.notify";
    public const string ViewAccessAudit = "access-audit.view";
    public const string EnableParentCompanyVisibility = "parent-company-visibility.enable";
    public const string ExtendDataVisibility = "data-visibility.extend";
    public const string GrantEarlyBookingAccess = "early-booking-access.grant";
    public const string HaveOwnUsers = "organization-users.own";
    public const string ApproveJoinRequests = "join-requests.approve";
    public const string PreCreateCarrier = "carrier.pre-create";
    public const string UpdateDistributionList = "distribution-list.update";
    public const string AccessAdministrationArea = "administration-area.access";
    public const string UseAssistant = "assistant.use";
    public const string SelectCountry = "country.select";
}

/// <summary>
/// Permisos (claims) de la consola y de los perfiles de organización agregados en Fase 1 Ola A.
/// Los internos se asignan solo a roles de Hapag-Lloyd (Administrador/SuperAdmin por comodín).
/// </summary>
public static class AccessPermissions
{
    // Perfiles de organización (M1-02)
    public const string ManageOrganizationUsers = "org.users.manage";
    public const string ApproveJoinRequests = "org.requests.approve";
    public const string OperateShipments = "shipments.operate";

    // Internos (M8-04, M8-06, M1-11)
    public const string ViewAllShipments = "shipments.view-all";
    public const string ReviewOrganizations = "organizations.review";
    public const string CheckOrganizationsAr = "organizations.ar-check";
    public const string ManageAccessMatrix = "access-matrix.manage";
}
