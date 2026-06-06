using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class CreditClient : BaseAuditableEntity
{
    public required string Country { get; set; }
    public decimal CreditLimit { get; set; }
    public required string CreditStatus { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;
}
