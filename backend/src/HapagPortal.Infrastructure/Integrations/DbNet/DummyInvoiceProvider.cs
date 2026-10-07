using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.DbNet;

/// <summary>
/// Emisor simulado de documentos tributarios (CT-DBNET), en memoria. Asigna folios correlativos desde
/// 100001. Determinista: una <c>ExternalReference</c> que contiene "REJECT" queda REJECTED; el resto,
/// ACCEPTED. Emitir dos veces la misma referencia devuelve el documento ya emitido (idempotencia).
/// El PDF es una representación simulada de la factura sincronizada en el portal (M7-01), con el renderizador
/// documental del portal: marcada como no válida tributariamente hasta conectar CT-DBNET.
/// </summary>
public sealed class DummyInvoiceProvider(
    ILogger<DummyInvoiceProvider> logger,
    IServiceScopeFactory scopeFactory,
    IPdfDocumentRenderer renderer,
    DocumentSettings documentSettings) : IInvoiceProvider
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

    /// <summary>
    /// PDF simulado de la factura del portal con ese folio (M7-01). Antes devolvía un marcador de pocos bytes que
    /// ningún visor podía abrir. Sin factura en el portal, se usa el documento emitido por este Dummy.
    /// </summary>
    public async Task<Result<byte[]?>> GetPdfAsync(
        string folio,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return Result<byte[]?>.Success(null);

        var number = folio.Trim();
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var invoice = await dbContext.CustomerInvoices.AsNoTracking()
            .Where(i => i.SiiNumber == number)
            .OrderByDescending(i => i.IssueDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            if (!_byFolio.TryGetValue(number, out var issued))
                return Result<byte[]?>.Success(null);

            invoice = new Domain.Entities.CustomerInvoice
            {
                SiiNumber = issued.Folio,
                SourceNumber = issued.ExternalReference,
                DocumentType = issued.DocumentType switch
                {
                    34 => Domain.Constants.InvoiceDocumentTypes.ExemptInvoice,
                    56 => Domain.Constants.InvoiceDocumentTypes.DebitNote,
                    61 => Domain.Constants.InvoiceDocumentTypes.CreditNote,
                    _ => Domain.Constants.InvoiceDocumentTypes.Invoice
                },
                IssueDate = issued.IssueDate,
                LegalName = "-",
                TaxId = "-",
                NetAmount = issued.TotalAmount,
                TotalAmount = issued.TotalAmount,
                Currency = issued.Currency,
                Status = Domain.Constants.InvoiceStatus.Pending,
                Country = Domain.Constants.CountryCodes.Chile,
                Source = "DUMMY"
            };
        }

        var conceptName = invoice.ConceptCode is null
            ? null
            : await dbContext.ChargeConcepts.AsNoTracking()
                .Where(c => c.Code == invoice.ConceptCode)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);

        var model = PortalPdfs.SimulatedInvoice(
            invoice, documentSettings.IssuerFor(invoice.Country), conceptName, DateTime.UtcNow);
        return Result<byte[]?>.Success(renderer.Render(model));
    }
}
