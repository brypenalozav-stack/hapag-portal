using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;

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

    /// <summary>BL del pago por BL anterior a la Ola D. Un pago del carro puede reunir varios BL: va en el detalle.</summary>
    public Guid? BillOfLadingId { get; set; }

    // NF-14 / M1-03: pago ejecutado bajo un acceso otorgado o mandato. ClientId es la organización
    // mandataria que pagó; OnBehalfOfClientId, el mandante; AccessGrantId, el acceso usado.
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    // Fase 1 Ola D (M5-01, M5-03, M5-07, M7-02, NF-01 a NF-04, NF-08, NF-12).
    public string Origin { get; set; } = PaymentOrigins.Legacy;

    /// <summary>Clave de idempotencia de la solicitud (NF-01): misma clave del mismo usuario, mismo resultado.</summary>
    public string? IdempotencyKey { get; set; }
    public string? RequestFingerprint { get; set; }
    public Guid? CreatedByUserId { get; set; }

    /// <summary>Medio de pago configurado (M5-03) y clave del proveedor que lo procesa.</summary>
    public string? PaymentMethodCode { get; set; }
    public string? ProviderKey { get; set; }

    /// <summary>
    /// Identificadores de conciliación (NF-04): <see cref="ExternalReference"/> es la referencia del portal
    /// enviada al proveedor; estos son los del proveedor. NF-08: no se guardan datos del instrumento de pago.
    /// </summary>
    public string? ProviderReference { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? RedirectUrl { get; set; }

    /// <summary>RUT del pagador (M7-02), distinto del RUT de facturación de cada ítem (M5-09).</summary>
    public string? PayerTaxId { get; set; }
    public string? PayerName { get; set; }

    public DateTime? StatusChangedAt { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Boleta de depósito bancario (M5-02): número y emisión. Emitida, el cliente ya no puede anularla.</summary>
    public string? SlipNumber { get; set; }
    public DateTime? SlipIssuedAt { get; set; }

    /// <summary>Trazabilidad de la anulación (M5-02, NF-14).</summary>
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelledByRole { get; set; }
    public string? CancellationReason { get; set; }

    public Client Client { get; set; } = null!;
    public BillOfLading? BillOfLading { get; set; }
    public ICollection<PaymentDetail> Details { get; set; } = [];
    public ICollection<PaymentStatusChange> StatusHistory { get; set; } = [];
}
