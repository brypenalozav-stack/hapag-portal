using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Ítem de un pago. Los montos están en la moneda del pago; el monto de origen y el tipo de cambio,
/// cuando hubo conversión (M5-08, M5-05). El detalle por servicio identifica cada concepto pagado
/// dentro de la transacción consolidada (M5-01).
/// </summary>
public sealed class PaymentDetail : GuidEntity
{
    public required string ConceptType { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid PaymentId { get; set; }

    // Fase 1 Ola D: fuente del ítem, BL, RUT de facturación (M5-09) y monto de origen.
    public string? ItemType { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public string? BillingTaxId { get; set; }
    public string? BillingName { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? OriginalCurrency { get; set; }
    public decimal? ExchangeRate { get; set; }

    /// <summary>NF-14: mandante y acceso otorgado bajo el que se pagó el ítem.</summary>
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    /// <summary>Instante en que el paso de liberación marcó pagada la fuente (NF-03).</summary>
    public DateTime? ReleasedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
