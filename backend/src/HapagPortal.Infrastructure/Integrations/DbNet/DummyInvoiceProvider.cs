using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.DbNet;

/// <summary>
/// Emisor simulado de documentos tributarios (CT-DBNET), en memoria. Asigna folios correlativos desde
/// 100001. Determinista: una <c>ExternalReference</c> que contiene "REJECT" queda REJECTED; el resto,
/// ACCEPTED. Emitir dos veces la misma referencia devuelve el documento ya emitido (idempotencia).
/// </summary>
public sealed class DummyInvoiceProvider(ILogger<DummyInvoiceProvider> logger) : IInvoiceProvider
{
    private readonly ConcurrentDictionary<string, InvoiceDocument> _byFolio = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InvoiceDocument> _byReference = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private int _lastFolio = 100000;

    public Task<Result<InvoiceDocument>> IssueAsync(
        InvoiceIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        InvoiceDocument document;

        lock (_gate)
        {
            if (!_byReference.TryGetValue(request.ExternalReference, out document!))
            {
                var folio = (++_lastFolio).ToString(System.Globalization.CultureInfo.InvariantCulture);
                var reject = request.ExternalReference.Contains("REJECT", StringComparison.OrdinalIgnoreCase);

                document = new InvoiceDocument(
                    folio,
                    request.DocumentType,
                    request.ExternalReference,
                    reject ? "REJECTED" : "ACCEPTED",
                    $"DUMMY-{folio}",
                    request.IssueDate,
                    request.Totals.TotalAmount,
                    request.Currency,
                    PdfUrl: null,
                    XmlUrl: null);

                _byFolio[folio] = document;
                _byReference[request.ExternalReference] = document;
            }
        }

        logger.LogInformation(
            "Documento DBNet (dummy) - Type: {Type}, Ref: {Ref}, Folio: {Folio}, Status: {Status}",
            document.DocumentType, document.ExternalReference, document.Folio, document.Status);

        return Task.FromResult(Result<InvoiceDocument>.Success(document));
    }

    public Task<Result<InvoiceDocument?>> GetAsync(
        string folio,
        CancellationToken cancellationToken = default)
    {
        _byFolio.TryGetValue(folio, out var document);
        return Task.FromResult(Result<InvoiceDocument?>.Success(document));
    }
}
