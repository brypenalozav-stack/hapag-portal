using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.DbNet;

/// <summary>
/// Cliente Real de DBNet/SII (CT-DBNET). La emisión envía <c>Idempotency-Key</c> = referencia externa,
/// así un reintento de la tubería de resiliencia no emite dos documentos (NF-01). Folio inexistente
/// (404): <c>Success(null)</c>.
/// </summary>
public sealed class HttpInvoiceProvider(HttpClient httpClient, ISecretResolver secretResolver) : IInvoiceProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.DbNet, SecretTypes.DbNetApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<InvoiceDocument>> IssueAsync(
        InvoiceIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new IssueRequestDto(
            request.DocumentType,
            request.ExternalReference,
            request.IssueDate,
            request.Currency,
            request.ExchangeRate,
            new ReceiverDto(
                request.Receiver.TaxId,
                request.Receiver.BusinessName,
                request.Receiver.BusinessActivity,
                request.Receiver.Address,
                request.Receiver.Commune,
                request.Receiver.City,
                request.Receiver.Email),
            request.Lines
                .Select(l => new LineDto(l.LineNumber, l.Code, l.Description, l.Quantity, l.UnitPrice, l.Exempt, l.Amount))
                .ToList(),
            new TotalsDto(
                request.Totals.NetAmount,
                request.Totals.ExemptAmount,
                request.Totals.VatRate,
                request.Totals.VatAmount,
                request.Totals.TotalAmount));

        var result = await IntegrationHttp.SendAsync<DocumentDto>(
            httpClient, secretResolver, Endpoint, "issueDocument", HttpMethod.Post, "documents", cancellationToken,
            body, idempotencyKey: request.ExternalReference);

        if (result.IsFailure)
            return Result<InvoiceDocument>.Failure(result.Error);

        // La emisión no tiene "sin datos": un 404 aquí es una respuesta fuera de contrato.
        return result.Value is { } dto
            ? Result<InvoiceDocument>.Success(ToDocument(dto))
            : Result<InvoiceDocument>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));
    }

    public async Task<Result<InvoiceDocument?>> GetAsync(
        string folio,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<DocumentDto>(
            httpClient, secretResolver, Endpoint, "getDocument", HttpMethod.Get,
            $"documents/{IntegrationHttp.Segment(folio)}", cancellationToken);

        if (result.IsFailure)
            return Result<InvoiceDocument?>.Failure(result.Error);

        return Result<InvoiceDocument?>.Success(result.Value is { } dto ? ToDocument(dto) : null);
    }

    private static InvoiceDocument ToDocument(DocumentDto dto) => new(
        dto.Folio,
        dto.DocumentType,
        dto.ExternalReference,
        dto.Status,
        dto.SiiTrackId,
        dto.IssueDate,
        dto.TotalAmount,
        dto.Currency,
        dto.PdfUrl,
        dto.XmlUrl);

    private sealed record IssueRequestDto(
        int DocumentType,
        string ExternalReference,
        DateOnly IssueDate,
        string Currency,
        decimal? ExchangeRate,
        ReceiverDto Receiver,
        IReadOnlyList<LineDto> Lines,
        TotalsDto Totals);

    private sealed record ReceiverDto(
        string TaxId,
        string BusinessName,
        string? BusinessActivity,
        string? Address,
        string? Commune,
        string? City,
        string? Email);

    private sealed record LineDto(
        int LineNumber,
        string? Code,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        bool Exempt,
        decimal Amount);

    private sealed record TotalsDto(
        decimal NetAmount,
        decimal ExemptAmount,
        decimal? VatRate,
        decimal VatAmount,
        decimal TotalAmount);

    private sealed record DocumentDto(
        string Folio,
        int DocumentType,
        string ExternalReference,
        string Status,
        DateOnly IssueDate,
        decimal TotalAmount,
        string Currency,
        string? SiiTrackId = null,
        string? PdfUrl = null,
        string? XmlUrl = null);
}
