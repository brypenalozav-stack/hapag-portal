using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Emisión y consulta de documentos tributarios electrónicos (CT-DBNET). Folio inexistente:
/// <c>Success(null)</c>.
/// </summary>
public interface IInvoiceProvider
{
    Task<Result<InvoiceDocument>> IssueAsync(
        InvoiceIssueRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InvoiceDocument?>> GetAsync(
        string folio,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PDF del documento tributario por folio (M7-01). Folio inexistente o sin PDF: <c>Success(null)</c>.
    /// El Dummy entrega un PDF de marcador hasta la generación documental de la Ola E.
    /// </summary>
    Task<Result<byte[]?>> GetPdfAsync(
        string folio,
        CancellationToken cancellationToken = default);
}

/// <summary>Solicitud de emisión. <c>DocumentType</c> según SII: 33, 34, 39, 41, 61 o 110.</summary>
public sealed record InvoiceIssueRequest(
    int DocumentType,
    string ExternalReference,
    DateOnly IssueDate,
    string Currency,
    decimal? ExchangeRate,
    InvoiceReceiver Receiver,
    IReadOnlyList<InvoiceLine> Lines,
    InvoiceTotals Totals);

public sealed record InvoiceReceiver(
    string TaxId,
    string BusinessName,
    string? BusinessActivity,
    string? Address,
    string? Commune,
    string? City,
    string? Email);

public sealed record InvoiceLine(
    int LineNumber,
    string? Code,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    bool Exempt,
    decimal Amount);

public sealed record InvoiceTotals(
    decimal NetAmount,
    decimal ExemptAmount,
    decimal? VatRate,
    decimal VatAmount,
    decimal TotalAmount);

/// <summary>Documento emitido. <c>Status</c>: RECEIVED, ACCEPTED, ACCEPTED_WITH_OBJECTIONS o REJECTED.</summary>
public sealed record InvoiceDocument(
    string Folio,
    int DocumentType,
    string ExternalReference,
    string Status,
    string? SiiTrackId,
    DateOnly IssueDate,
    decimal TotalAmount,
    string Currency,
    string? PdfUrl,
    string? XmlUrl);
