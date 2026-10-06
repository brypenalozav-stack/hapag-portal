using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class Payment : BaseAuditableEntity
{
    public required string PaymentNumber { get; set; }
    public required string PaymentType { get; set; }
    public required string PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public required string Status { get; set; }
    public required string Country { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public string? ExternalReference { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? DepositProofUrl { get; set; }
    public Guid ClientId { get; set; }
    public Guid BillOfLadingId { get; set; }

    // NF-14 / M1-03: pago ejecutado bajo un acceso otorgado o mandato. ClientId es la organización
    // mandataria que pagó; OnBehalfOfClientId, el mandante; AccessGrantId, el acceso usado.
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    public Client Client { get; set; } = null!;
    public BillOfLading BillOfLading { get; set; } = null!;
    public ICollection<PaymentDetail> Details { get; set; } = [];
}
