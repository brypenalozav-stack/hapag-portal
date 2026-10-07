namespace HapagPortal.Application.BillsOfLading.Import;

using HapagPortal.Application.Common.Messaging;

/// <summary>
/// Fila de importación (aplanada: una por contenedor/mercancía principal). <c>ClientId</c> es el
/// titular del BL (rol Customer); el consignatario con RUT/NIT de una organización registrada
/// queda vinculado como Consignee (M1-11). Los UN/LOCODE del puerto de descarga y del destino final y el DIFU
/// con su localidad (opcionales) alimentan las reglas de publicación por DIFU (M2-01).
/// </summary>
public sealed record ImportBillRow(
    Guid ClientId,
    string BLNumber,
    string ShipmentType,
    string Country,
    string? PortOfLoading,
    string? PortOfDischarge,
    string FreightCurrency,
    string? ConsigneeName,
    string? ConsigneeTaxId,
    string? HsCode,
    decimal? GrossWeight,
    string? ContainerNumber,
    string? ContainerIsoType,
    string? BookingNumber = null,
    string? PortOfDischargeCode = null,
    string? FinalDestinationCode = null,
    string? DifuCode = null,
    string? DifuLocationCode = null);

public sealed record ImportRowError(int Index, string? BLNumber, IReadOnlyList<string> Messages);

public sealed record ImportResult(int Created, int Failed, IReadOnlyList<ImportRowError> Errors);

public sealed record ImportBillsOfLadingCommand(IReadOnlyList<ImportBillRow> Rows) : ICommand<ImportResult>;
