using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class ServiceOrder : BaseAuditableEntity
{
    public required string OrderNumber { get; set; }
    public required string OrderType { get; set; }
    public required string Status { get; set; }
    public string? Description { get; set; }
    public required string Country { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid BillOfLadingId { get; set; }
    public Guid ClientId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
