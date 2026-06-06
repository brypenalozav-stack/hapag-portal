using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class BillOfLading : BaseAuditableEntity
{
    public required string BLNumber { get; set; }
    public required string ShipmentType { get; set; }
    public string? Vessel { get; set; }
    public string? Voyage { get; set; }
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public string? PlaceOfDelivery { get; set; }
    public DateTime? ETD { get; set; }
    public DateTime? ETA { get; set; }
    public string? Consignee { get; set; }
    public string? Shipper { get; set; }
    public string? NotifyParty { get; set; }
    public decimal FreightAmount { get; set; }
    public required string FreightCurrency { get; set; }
    public required string Status { get; set; }
    public required string Country { get; set; }
    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;
    public ICollection<BLContainer> Containers { get; set; } = [];
    public ICollection<LocalCharge> LocalCharges { get; set; } = [];
    public ICollection<DemurrageCharge> DemurrageCharges { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
