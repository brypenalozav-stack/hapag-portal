namespace HapagPortal.Application.Shipments.Common;

using HapagPortal.Application.Common.Dtos;

/// <summary>Origen del acceso del usuario a un embarque. Ola B agrega Granted y SelfAssociated.</summary>
public static class ShipmentAccessSources
{
    public const string Own = "Own";
    public const string Admin = "Admin";
}

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
    bool HasPendingCharges);

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
    ShipmentFreightDto? Freight,
    IReadOnlyList<BLContainerDto> Containers,
    IReadOnlyList<LocalChargeDto>? LocalCharges,
    IReadOnlyList<DemurrageChargeDto>? DemurrageCharges,
    IReadOnlyList<ShipmentServiceOrderDto> ServiceOrders);

public static class ShipmentOperations
{
    public const string Import = "IMPORT";
    public const string Export = "EXPORT";

    public static readonly string[] All = [Import, Export];

    /// <summary>Normaliza el tipo de embarque (Import/IMPORT) al código de operación.</summary>
    public static string Normalize(string shipmentType) =>
        shipmentType.Trim().ToUpperInvariant();
}
