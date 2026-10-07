using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Datos propios de una refacturación IAO (M3-11) sobre la solicitud de servicio genérica (Ola G): la factura
/// original, el cobro de pérdida de IVA y de refacturación calculado al enviar, la aceptación del cobro por la
/// nueva razón social y la factura emitida. Los nuevos datos de facturación son los de la solicitud.
/// <para>
/// Aceptación verificable: al enviar la solicitud se genera un enlace de un solo uso (token aleatorio de 256
/// bits, del que solo se guarda el SHA-256) que se envía al correo de la nueva razón social; quien acepta
/// declara su nombre y RUT y queda registrado con fecha, hora y dirección de origen. Sin aceptación la factura
/// no se emite.
/// </para>
/// </summary>
public sealed class InvoiceReissue : GuidEntity
{
    public Guid ServiceRequestId { get; set; }

    public Guid OriginalInvoiceId { get; set; }
    public string? OriginalSiiNumber { get; set; }
    public required string OriginalSourceNumber { get; set; }
    public required string OriginalTaxId { get; set; }
    public required string OriginalLegalName { get; set; }

    /// <summary>Pérdida de IVA: el impuesto de la factura original, en la moneda del cobro.</summary>
    public decimal VatLossAmount { get; set; }

    /// <summary>Cargo por refacturación (neto, impuesto) de la tarifa vigente al enviar (M8-01).</summary>
    public decimal FeeAmount { get; set; }
    public decimal FeeTaxAmount { get; set; }
    public string? Currency { get; set; }

    /// <summary>Tipo de cambio aplicado a la pérdida de IVA cuando la factura está en otra moneda (M5-05).</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>Correo de la nueva razón social que debe aceptar el cobro.</summary>
    public required string AcceptorEmail { get; set; }

    /// <summary><c>ReinvoicingAcceptanceStatus</c>; nulo mientras la solicitud es un borrador.</summary>
    public string? AcceptanceStatus { get; set; }
    public string? AcceptanceTokenHash { get; set; }
    public DateTime? AcceptanceRequestedAt { get; set; }
    public DateTime? AcceptanceExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedByName { get; set; }
    public string? AcceptedByTaxId { get; set; }
    public string? AcceptedFromAddress { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }

    public Guid? NewInvoiceId { get; set; }
    public DateTime? IssuedAt { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}
