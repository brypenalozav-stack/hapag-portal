using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Tipo de cambio y vigencia usados en una transacción (M5-05), tal como los informó Nexus, para
/// consulta y auditoría. <see cref="TransactionType"/> según <c>ExchangeRateTransactionTypes</c>.
/// </summary>
public sealed class ExchangeRateRecord : GuidEntity
{
    public required string TransactionType { get; set; }
    public Guid TransactionId { get; set; }
    public required string FromCurrency { get; set; }
    public required string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public required string Source { get; set; }
    public bool Approved { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public DateTime CapturedAt { get; set; }
}
