using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class Client : BaseAuditableEntity
{
    public required string Name { get; set; }
    public required string TaxId { get; set; }
    public required string TaxIdType { get; set; }
    public required string Country { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public required string ClientType { get; set; }
    public string? AgentCode { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }

    public ICollection<BillOfLading> BillsOfLading { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
