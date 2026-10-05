using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Access;

/// <summary>Excepción de la matriz para un tipo de organización (p. ej. Freight Forwarder).</summary>
public sealed record BaselineOverride(string Role, string OrganizationType, string Level);

/// <summary>
/// Fila de la matriz base. <see cref="Levels"/> sigue el orden de
/// <see cref="ShipmentRoleCodes.MatrixColumns"/>: Customer, Shipper, Consignee, Tercero,
/// Ag. Aduanas, Transportista.
/// </summary>
public sealed record BaselineAction(
    string Code,
    string Name,
    string Category,
    string Kind,
    string Scope,
    IReadOnlyList<string> Levels,
    IReadOnlyList<BaselineOverride> Overrides);

/// <summary>
/// Las dos matrices de M1-11 (especificación v4, capítulo 4.3) como datos iniciales. Se siembran
/// en la base y desde ahí se administran sin desarrollo; este catálogo solo es el punto de partida.
/// </summary>
public static class AccessMatrixBaseline
{
    private const string O = AccessLevels.Allowed;
    private const string X = AccessLevels.Denied;
    private const string Xo = AccessLevels.OnGrant;

    private const string Info = ShipmentActionCategories.Information;
    private const string Admin = ShipmentActionCategories.Administration;
    private const string View = ShipmentActionKinds.View;
    private const string Operate = ShipmentActionKinds.Operate;
    private const string Shipment = ShipmentActionScopes.Shipment;
    private const string Organization = ShipmentActionScopes.Organization;

    private static readonly BaselineOverride[] None = [];

    public static readonly IReadOnlyList<BaselineAction> Actions =
    [
        // Nivel base de visibilidad de información y de gestión de servicios
        new(ShipmentActionCodes.ViewShipment, "Ver listado y detalle de BL o booking", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ViewReleaseRequirements, "Consultar estado de los requisitos de liberación", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ViewTracking, "Ver el seguimiento del embarque", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ViewBlIssuance, "Consultar el estado de emisión del BL", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.RequestUnvaluedBlCopy, "Solicitar copia de BL no valorada", Info, Operate, Shipment, [O, O, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.RequestValuedBlCopy, "Solicitar copia de BL valorada", Info, Operate, Shipment, [O, Xo, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.DownloadNoDebtCertificate, "Descargar certificado de libre deuda", Info, View, Shipment, [X, X, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.DownloadTatc, "Descargar documento TATC", Info, View, Shipment, [X, X, O, O, O, O], None),
        new(ShipmentActionCodes.PayFreight, "Visualizar y pagar montos de flete", Info, Operate, Shipment, [O, Xo, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.PayMandatoryLocalCharges, "Visualizar y pagar recargos locales mandatorios", Info, Operate, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.PayOnDemandLocalCharges, "Visualizar y pagar recargos locales on demand", Info, Operate, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.GenerateReleaseLetter, "Generar y descargar carta de liberación y desconsolidado", Info, Operate, Shipment, [X, X, O, Xo, X, X], None),
        new(ShipmentActionCodes.GenerateTransshipmentCertificate, "Generar y descargar certificado de transbordo", Info, Operate, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.GenerateFreightCertificate, "Generar y descargar certificado de flete", Info, Operate, Shipment, [X, X, O, Xo, X, Xo], None),
        new(ShipmentActionCodes.GenerateResponsibilityLetter, "Generar y descargar carta de responsabilidad", Info, Operate, Shipment, [X, X, X, X, X, X],
            [new(ShipmentRoleCodes.Consignee, OrganizationTypes.FreightForwarder, O)]),
        new(ShipmentActionCodes.PayImportDemurrage, "Consultar y pagar demurrage de importación", Info, Operate, Shipment, [X, X, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.RequestDropOff, "Solicitar y pagar Drop Off", Info, Operate, Shipment, [X, X, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.RequestWarehouseChange, "Solicitar cambio de almacén, individual o masivo", Info, Operate, Shipment, [X, X, O, Xo, Xo, Xo], None),
        new(ShipmentActionCodes.ViewAccountStatement, "Consultar el estado de cuenta en línea", Info, View, Shipment, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.ViewInvoicesAsBilled, "Ver facturas como cliente facturado", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ViewInvoicesAsPayer, "Ver facturas como pagador distinto del facturado", Info, View, Shipment, [X, X, X, X, O, O], None),
        new(ShipmentActionCodes.DownloadCollectReceipt, "Visualizar y descargar comprobante Collect", Info, View, Shipment, [X, X, X, X, O, X], None),
        new(ShipmentActionCodes.ViewImportDepot, "Ver depósito asignado en importación", Info, View, Shipment, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ViewExportDepot, "Ver depósito asignado en exportación", Info, View, Shipment, [X, X, X, X, X, X], None),

        // Nivel base de administración de accesos y de la organización
        new(ShipmentActionCodes.GrantAccess, "Otorgar acceso a un BL o booking, individual o masivo", Admin, Operate, Shipment, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.RevokeAccess, "Revocar un acceso ya otorgado", Admin, Operate, Shipment, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.SetAccessValidity, "Definir la vigencia de un acceso", Admin, Operate, Shipment, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.ConfigureDefaultAgents, "Configurar agencia de aduanas o transportista por defecto", Admin, Operate, Organization, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.EnableOpenAccess, "Activar el acceso abierto por número de BL", Admin, Operate, Shipment, [O, O, O, X, X, X], None),
        new(ShipmentActionCodes.SearchOpenAccess, "Buscar un BL con acceso abierto por su número", Admin, View, Organization, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.SelfAssociate, "Autoasociarse a un BL consultado con acceso abierto", Admin, Operate, Organization, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ReceiveThirdPartyQueryNotice, "Recibir notificación cuando un tercero consulta un BL", Admin, View, Shipment, [O, O, O, X, X, X], None),
        new(ShipmentActionCodes.ViewAccessAudit, "Consultar la auditoría de accesos de un BL o booking", Admin, View, Shipment, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.EnableParentCompanyVisibility, "Habilitar la visibilidad de los BL hacia la empresa matriz", Admin, Operate, Organization, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.ExtendDataVisibility, "Ampliar la visibilidad de un dato del BL a otro rol", Admin, Operate, Shipment, [O, O, O, O, X, X], None),
        // Excepción del documento: el tercero de un Freight Forwarder puede recibir acceso anticipado por booking.
        new(ShipmentActionCodes.GrantEarlyBookingAccess, "Otorgar acceso anticipado por booking a un futuro shipper", Admin, Operate, Shipment, [O, X, X, X, X, X],
            [new(ShipmentRoleCodes.ThirdParty, OrganizationTypes.FreightForwarder, O)]),
        new(ShipmentActionCodes.HaveOwnUsers, "Tener usuarios propios asociados a la organización", Admin, Operate, Organization, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.ApproveJoinRequests, "Revisar y aprobar solicitudes de registro a la organización", Admin, Operate, Organization, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.PreCreateCarrier, "Pre-crear el perfil de un transportista sin cuenta", Admin, Operate, Organization, [O, O, O, O, X, X], None),
        new(ShipmentActionCodes.UpdateDistributionList, "Actualizar la lista de distribución de correos propia", Admin, Operate, Organization, [O, O, O, O, O, X], None),
        new(ShipmentActionCodes.AccessAdministrationArea, "Acceder al área de administración del portal", Admin, Operate, Organization, [X, X, X, X, X, X], None),
        new(ShipmentActionCodes.UseAssistant, "Utilizar el asistente del portal y ver los comunicados", Admin, View, Organization, [O, O, O, O, O, O], None),
        new(ShipmentActionCodes.SelectCountry, "Seleccionar el país de operación y consultar la clasificación DG", Admin, View, Organization, [O, O, O, O, O, O], None),
    ];
}
