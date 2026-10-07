using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Cargo cubierto antes de emitirse su factura (M7-03, NF-04): un pago anticipado de un recargo, una línea de
/// demurrage o un flete (por ejemplo el Gate Out que la agencia de aduanas paga antes del zarpe, M3-19), o su
/// imputación a la línea de crédito (M5-10). Lo registra la liberación del pago (NF-03), uno por ítem. Cuando
/// llega la factura que incluye el cargo se cruza con ella: el anticipo la deja cubierta (no vuelve a cobrarse)
/// y la imputación deja de presentarse como cargo no facturado.
/// </summary>
public sealed class ChargeSettlement : GuidEntity
{
    /// <summary><c>SettlementKinds</c>: Advance o CreditImputation.</summary>
    public required string Kind { get; set; }

    /// <summary><c>SettlementStatus</c>: Open o Matched.</summary>
    public required string Status { get; set; }

    public Guid PaymentId { get; set; }
    public Guid PaymentDetailId { get; set; }
    public required string PaymentNumber { get; set; }
    public string? ReceiptNumber { get; set; }

    /// <summary>Organización que pagó o imputó y, bajo mandato, el mandante (NF-14).</summary>
    public Guid PayerOrganizationId { get; set; }
    public string? PayerTaxId { get; set; }
    public string? PayerName { get; set; }
    public Guid? OnBehalfOfOrganizationId { get; set; }

    /// <summary>RUT de facturación elegido para el ítem (M5-09): la factura posterior se emite a él.</summary>
    public string? BillingTaxId { get; set; }
    public string? BillingName { get; set; }

    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string Country { get; set; }

    public required string ItemType { get; set; }
    public Guid SourceId { get; set; }
    public required string ConceptCode { get; set; }
    public string? Description { get; set; }

    /// <summary>Monto total en la moneda del cargo (el que tendrá la factura).</summary>
    public decimal Amount { get; set; }
    public required string Currency { get; set; }

    /// <summary>Monto total en la moneda del pago (con el tipo de cambio aplicado, M5-05).</summary>
    public decimal PaidAmount { get; set; }
    public required string PaidCurrency { get; set; }

    public DateTime SettledAt { get; set; }

    /// <summary>Recibo del pago anticipado emitido en el repositorio documental (M3-19).</summary>
    public Guid? ReceiptDocumentId { get; set; }

    public Guid? MatchedInvoiceId { get; set; }
    public DateTime? MatchedAt { get; set; }
    public string? MatchedBy { get; set; }
    public string? MatchNote { get; set; }

    public DateTime CreatedAt { get; set; }
}
