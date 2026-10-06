using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Ítem del carro: fuente pagable validada al agregarla, su monto en la moneda del cargo y en la moneda
/// de pago elegida (con el tipo de cambio de M5-05 si hubo conversión) y el RUT de facturación elegido al
/// agregarlo (M5-09). Mientras un pago lo incluye queda bloqueado por ese pago; al confirmarse, sale del carro.
/// </summary>
public sealed class CartItem : GuidEntity
{
    public Guid CartId { get; set; }
    public required string ItemType { get; set; }
    public Guid SourceId { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string Country { get; set; }
    public required string ConceptCode { get; set; }
    public string? Description { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }

    public required string PaymentCurrency { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateOnly? RateEffectiveDate { get; set; }
    public string? RateSource { get; set; }

    public required string BillingTaxId { get; set; }
    public required string BillingName { get; set; }
    public Guid? BillingOrganizationId { get; set; }

    /// <summary>NF-14: mandante y acceso otorgado bajo el que se agregó el ítem.</summary>
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    public Guid AddedByUserId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid? LockedByPaymentId { get; set; }

    public Cart Cart { get; set; } = null!;
}
