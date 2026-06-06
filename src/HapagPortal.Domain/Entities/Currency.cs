using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class Currency : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public decimal ExchangeRateToUSD { get; set; }
    public DateTime LastUpdated { get; set; }
}
