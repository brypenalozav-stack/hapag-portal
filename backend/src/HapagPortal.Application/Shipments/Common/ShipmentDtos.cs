namespace HapagPortal.Application.Shipments.Common;

using HapagPortal.Application.Common.Dtos;

/// <summary>
/// Origen del acceso del usuario a un embarque: propio (rol en el BL), administración interna,
/// acceso otorgado (M1-12), autoasociado (M1-18) o acceso abierto por número de BL (M1-17).
/// </summary>
public static class ShipmentAccessSources
{
    public const string Own = "Own";
    public const string Admin = "Admin";
    public const string Grant = "Grant";
    public const string SelfAssociated = "SelfAssociated";
    public const string OpenAccess = "OpenAccess";
    public const string None = "None";
}

/// <summary>
/// Estado de emisión del documento de transporte (M2-02): tipo (BL, SWB, EBL), plataforma del EBL (p. ej.
/// WAVE), estado del portal (<c>BlIssuanceStatuses</c>) y su fecha en el origen. En el listado es el último
/// estado conocido; el detalle y <c>/issuance</c> lo leen del origen en el momento.
/// </summary>
public sealed record ShipmentIssuanceSummaryDto(
    string? DocumentType,
    string? EblPlatform,
    string? Status,
    DateTime? StatusAt);

/// <summary>
/// Publicación del BL por DIFU de destino final (M2-01), solo para el administrador interno: un BL no
/// publicado no llega a los clientes. <c>ReasonCode</c> en <c>ShipmentPublicationReasons</c>.
/// </summary>
public sealed record ShipmentPublicationDto(
    bool Published,
    string ReasonCode,
    Guid? RuleId,
    string? FinalDestinationCode,
    string? PortOfDischargeCode,
    string? DifuCode,
    string? DifuLocationCode);

/// <summary>Fila del listado único de embarques (M2-06, M2-07).</summary>
public sealed record ShipmentListItemDto(
    Guid Id,
    string BlNumber,
    string? BookingNumber,
    string? Vessel,
    string? Voyage,
    string Status,
    string Operation,
    string Country,
    string? PortOfLoading,
    string? PortOfDischarge,
    DateTime? Etd,
    DateTime? Eta,
    IReadOnlyList<string> Roles,
    string AccessSource,
    bool HasPendingCharges,
    ShipmentIssuanceSummaryDto? Issuance = null,
    ShipmentPublicationDto? Publication = null);

public sealed record ShipmentFreightDto(decimal Amount, string Currency, string Status);

public sealed record ShipmentServiceOrderDto(
    Guid Id,
    string OrderNumber,
    string OrderType,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt);

/// <summary>
/// Detalle del embarque según los permisos del usuario (M2-06, M1-11). Los bloques que la matriz no
/// habilita llegan en null; <see cref="AllowedActions"/> indica qué acciones mostrar y
/// <see cref="CanOperate"/> si el perfil del usuario puede ejecutarlas (pagar, solicitar).
/// <see cref="CanSelfAssociate"/>: visto por acceso abierto y sin asociar (M1-18).
/// <see cref="RequiresAssociationForPayment"/>: agencia o transportista que debe asociarse antes de
/// incorporar cargos al carro (M1-18). <see cref="Issuance"/>: estado de emisión leído del origen cuando la
/// matriz permite consultarlo (M2-02); <see cref="Publication"/>: solo para el administrador (M2-01).
/// </summary>
public sealed record ShipmentDetailDto(
    Guid Id,
    string BlNumber,
    string? BookingNumber,
    string Operation,
    string Status,
    string Country,
    string? Vessel,
    string? Voyage,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? PlaceOfDelivery,
    DateTime? Etd,
    DateTime? Eta,
    string? Shipper,
    string? Consignee,
    IReadOnlyList<string> Roles,
    string AccessSource,
    IReadOnlyList<string> AllowedActions,
    bool CanOperate,
    bool CanSelfAssociate,
    bool RequiresAssociationForPayment,
    ShipmentFreightDto? Freight,
    IReadOnlyList<BLContainerDto> Containers,
    IReadOnlyList<LocalChargeDto>? LocalCharges,
    IReadOnlyList<DemurrageChargeDto>? DemurrageCharges,
    IReadOnlyList<ShipmentServiceOrderDto> ServiceOrders,
    string? PortOfDischargeCode = null,
    string? FinalDestinationCode = null,
    ShipmentIssuanceDto? Issuance = null,
    ShipmentPublicationDto? Publication = null);

/// <summary>
/// Estado de emisión del documento de transporte leído del origen (M2-02, CL-IMP-14, BO-IMP-14).
/// <c>Available</c> = el origen respondió; si no, <c>ErrorCode</c> explica la falla (NF-11) y
/// <c>LastKnown</c> trae el último estado registrado en el portal, que no se presenta como vigente.
/// <c>SourceStatus</c> es el código tal como lo informa el origen.
/// </summary>
public sealed record ShipmentIssuanceDto(
    string BlNumber,
    string Country,
    string Operation,
    bool Available,
    string Source,
    string? DocumentType,
    string? EblPlatform,
    string? Status,
    string? SourceStatus,
    DateTime? StatusAt,
    string? IssuancePlace,
    DateTime RetrievedAt,
    string? ErrorCode,
    ShipmentIssuanceSummaryDto? LastKnown);

public static class ShipmentOperations
{
    public const string Import = "IMPORT";
    public const string Export = "EXPORT";

    public static readonly string[] All = [Import, Export];

    /// <summary>Normaliza el tipo de embarque (Import/IMPORT) al código de operación.</summary>
    public static string Normalize(string shipmentType) =>
        shipmentType.Trim().ToUpperInvariant();
}
