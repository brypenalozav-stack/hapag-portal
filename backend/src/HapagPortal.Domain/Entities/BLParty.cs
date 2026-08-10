using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Parte estructurada de un BL (Shipper/Consignee/NotifyParty) con identificador fiscal,
/// alineada a DocumentParty de DCSA eBL v3. Reemplaza los strings libres del BL.
/// </summary>
public sealed class BLParty : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public required string Role { get; set; } // Shipper | Consignee | NotifyParty
    public required string Name { get; set; }
    public string? TaxId { get; set; }
    public string? TaxIdType { get; set; }
    public string? CountryCode { get; set; }
    public string? Address { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
