using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Factura local del cliente (M7-01) en la caché del portal, alimentada por la fuente de facturación
/// (<c>IInvoiceProvider</c>). <see cref="OrganizationId"/> es la organización facturada: la información
/// se segrega por ella. <see cref="SyncedAt"/> es la última actualización desde la fuente.
/// </summary>
public sealed class CustomerInvoice : BaseAuditableEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Folio SII (o número fiscal en Bolivia). Sin folio, el documento no se descarga.</summary>
    public string? SiiNumber { get; set; }
    public required string SourceNumber { get; set; }
    public required string DocumentType { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string LegalName { get; set; }
    public required string TaxId { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? SiiStatus { get; set; }
    public bool IsPayable { get; set; }
    public required string Country { get; set; }
    public string? ConceptCode { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime SyncedAt { get; set; }
    public required string Source { get; set; }

    public Client Organization { get; set; } = null!;
}
