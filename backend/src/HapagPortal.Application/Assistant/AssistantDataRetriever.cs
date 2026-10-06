namespace HapagPortal.Application.Assistant;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Dashboard;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Pedido de entrega de documentos por el asistente (M10-04): el mensaje (de donde salen los tipos pedidos), las
/// referencias escritas, la conversación y el mensaje de respuesta al que queda asociada cada entrega.
/// </summary>
public sealed record AssistantDeliveryRequest(
    string Message,
    IReadOnlyList<string> References,
    string Topic,
    Guid SessionId,
    Guid ReplyMessageId,
    string? UserEmail);

/// <summary>
/// Respuestas a consultas dinámicas (M10-03): estado del embarque, documentos, cargos pendientes, factura y TATC.
/// Todo sale de las consultas existentes del portal ejecutadas con la identidad y los permisos del usuario
/// (M1-11, NF-05), de modo que el asistente ve exactamente lo que vería en la pantalla correspondiente. Un BL,
/// booking o factura inexistente o sin acceso recibe la misma respuesta "no disponible", sin revelar si existe.
/// Los datos se presentan tal como están registrados; lo que falta se informa como "no disponible".
/// </summary>
public sealed class AssistantDataRetriever(
    ISender sender,
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
{
    private const int MaxReferences = 3;
    private const int MaxListedItems = 10;
    private const int MaxDeliveredItems = 5;

    public async Task<AssistantAnswer> AnswerAsync(
        string intent,
        IReadOnlyList<string> references,
        string topic,
        CancellationToken cancellationToken) => intent switch
    {
        AssistantIntents.ShipmentStatus => await ShipmentStatusAsync(references, topic, cancellationToken),
        AssistantIntents.ShipmentDocuments => await DocumentsAsync(references, topic, cancellationToken),
        AssistantIntents.PendingCharges when references.Count > 0 => await ShipmentChargesAsync(references, topic, cancellationToken),
        AssistantIntents.PendingCharges => await PendingSummaryAsync(topic, cancellationToken),
        AssistantIntents.InvoiceDetail => await InvoiceAsync(references, topic, cancellationToken),
        AssistantIntents.TatcStatus => await TatcAsync(references, topic, cancellationToken),
        AssistantIntents.DocumentDelivery => await DocumentsAsync(references, topic, cancellationToken),
        _ => NotAvailable(intent, references, topic),
    };

    /// <summary>
    /// Entrega de documentos puntuales (M10-04): identifica el embarque y los tipos pedidos, toma los documentos del
    /// repositorio (M6-09) con la misma consulta y los mismos permisos que la pantalla y la descarga directa (M1-11: el
    /// shipper solo ve la copia no valorada, el comprobante Collect solo la agencia de aduanas autorizada) y ofrece un
    /// enlace de descarga por documento. Cada entrega queda registrada (NF-14) con el usuario, su organización y el mandante,
    /// y en el registro del documento con el canal <c>Assistant</c>. Lo que no existe o no puede ver recibe la misma
    /// respuesta "no disponible".
    /// </summary>
    public async Task<AssistantAnswer> DeliverDocumentsAsync(AssistantDeliveryRequest request, CancellationToken cancellationToken)
    {
        const string intent = AssistantIntents.DocumentDelivery;
        var kinds = AssistantIntentRules.RequestedDocuments(request.Message);
        if (kinds.Count == 0)
            return await DocumentsAsync(request.References, request.Topic, cancellationToken);

        var detail = await ResolveShipmentAsync(request.References, cancellationToken);
        if (detail is null)
            return NotAvailable(intent, request.References, request.Topic);

        var result = await sender.Send(new GetShipmentDocumentsQuery(detail.BlNumber), cancellationToken);
        if (result.IsFailure)
            return NotAvailable(intent, request.References, request.Topic);

        var repository = result.Value;
        var requestedNames = string.Join(", ", kinds.Select(DeliveryKindName));
        var citation = new AssistantCitationDto(
            AssistantReferences.Documents, repository.BlNumber, null, $"/api/v1/documents/{Uri.EscapeDataString(repository.BlNumber)}");

        // Del repositorio, el vigente más reciente de cada tipo pedido (una carta reemplazada no se entrega).
        var documents = repository.Documents
            .Where(d => kinds.Contains(d.DocumentType) && d.Status == ShipmentDocumentStatus.Issued)
            .GroupBy(d => d.DocumentType)
            .Select(g => g.OrderByDescending(d => d.IssuedAt).First())
            .Take(MaxDeliveredItems)
            .ToList();
        var related = repository.Related
            .Where(r => (r.Kind == AssistantReferences.Invoice && kinds.Contains(AssistantDeliveryKinds.Invoice))
                || (r.Kind != AssistantReferences.Invoice && kinds.Contains(AssistantDeliveryKinds.Receipt)))
            .Take(MaxDeliveredItems)
            .ToList();

        if (documents.Count == 0 && related.Count == 0)
        {
            return new AssistantAnswer(
                intent,
                AssistantAnswerTypes.NotAvailable,
                $"No hay {requestedNames} disponible para su usuario en el BL {repository.BlNumber}. El asistente solo entrega los " +
                "documentos ya emitidos que usted puede descargar en el repositorio del embarque; si corresponde, puede solicitarlos " +
                "desde los documentos del embarque en el portal.",
                [],
                [citation],
                [new AssistantActionDto(AssistantReferences.OpenShipment, $"Ver el detalle del BL {repository.BlNumber}", null, repository.BlNumber, detail.Id)],
                request.Topic);
        }

        // Actor de la entrega (NF-14): usuario, su organización y el mandante cuando el permiso viene de un acceso otorgado.
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var bl = await dbContext.BillsOfLading.AsNoTracking().FirstAsync(b => b.Id == repository.BlId, cancellationToken);
        var permissions = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
        var now = DateTime.UtcNow;

        var facts = new List<AssistantFact>();
        var actions = new List<AssistantActionDto>();

        foreach (var document in documents)
        {
            var name = AssistantFormat.DocumentType(document.DocumentType);
            var delivery = Deliver(request, scope, permissions.GrantFor(ShipmentDocumentAccess.ActionFor(document.DocumentType)), repository,
                AssistantDeliveryKinds.ShipmentDocument, document.DocumentType, document.Id, document.DocumentNumber, now);

            dbContext.ShipmentDocumentEvents.Add(new ShipmentDocumentEvent
            {
                ShipmentDocumentId = document.Id,
                EventType = ShipmentDocumentEventTypes.Delivered,
                Channel = DocumentChannels.Assistant,
                OccurredAt = now,
                UserId = scope.UserId,
                UserEmail = delivery.UserEmail,
                OrganizationId = scope.OrganizationId,
                OnBehalfOfOrganizationId = delivery.OnBehalfOfOrganizationId,
                Details = $"{{\"sessionId\":\"{request.SessionId}\",\"deliveryId\":\"{delivery.Id}\"}}"
            });

            facts.Add(new AssistantFact(name, $"{document.DocumentNumber}, emitido el {AssistantFormat.LocalTime(document.IssuedAt, repository.Country)}"));
            actions.Add(new AssistantActionDto(
                AssistantReferences.DownloadDocument,
                $"Descargar {name} {document.DocumentNumber}",
                $"/api/v1/assistant/sessions/{request.SessionId}/deliveries/{delivery.Id}/download",
                repository.BlNumber,
                document.Id));
        }

        foreach (var item in related)
        {
            var isInvoice = item.Kind == AssistantReferences.Invoice;
            Deliver(request, scope, null, repository, isInvoice ? AssistantDeliveryKinds.Invoice : AssistantDeliveryKinds.Receipt,
                null, item.Id, item.Number, now);

            facts.Add(new AssistantFact(
                isInvoice ? "Factura" : "Recibo de pago",
                $"{item.Number}, emitido el {AssistantFormat.LocalTime(item.IssuedAt, repository.Country)}"));
            actions.Add(new AssistantActionDto(
                isInvoice ? AssistantReferences.DownloadInvoice : AssistantReferences.DownloadReceipt,
                $"Descargar {(isInvoice ? "factura" : "recibo")} {item.Number}",
                item.DownloadPath,
                repository.BlNumber,
                item.Id));
        }

        return new AssistantAnswer(
            intent,
            AssistantAnswerTypes.Data,
            Draft($"Documentos del BL {repository.BlNumber} listos para descargar ({requestedNames}):", facts),
            facts,
            [citation],
            actions,
            request.Topic);
    }

    private AssistantDocumentDelivery Deliver(
        AssistantDeliveryRequest request,
        AccessScope scope,
        ShipmentGrantAccess? grant,
        ShipmentDocumentsDto repository,
        string kind,
        string? documentType,
        Guid documentId,
        string documentNumber,
        DateTime now)
    {
        var delivery = new AssistantDocumentDelivery
        {
            SessionId = request.SessionId,
            MessageId = request.ReplyMessageId,
            UserId = scope.UserId ?? Guid.Empty,
            UserEmail = request.UserEmail,
            OrganizationId = scope.OrganizationId,
            OnBehalfOfOrganizationId = grant?.GrantorOrganizationId,
            BillOfLadingId = repository.BlId,
            BlNumber = repository.BlNumber,
            DocumentKind = kind,
            DocumentType = documentType,
            DocumentId = documentId,
            DocumentNumber = documentNumber,
            DeliveredAt = now
        };
        dbContext.AssistantDocumentDeliveries.Add(delivery);
        return delivery;
    }

    private static string DeliveryKindName(string kind) => kind switch
    {
        AssistantDeliveryKinds.Receipt => "recibos de pago",
        AssistantDeliveryKinds.Invoice => "facturas",
        _ => AssistantFormat.DocumentType(kind).ToLowerInvariant()
    };

    /// <summary>Respuesta única para lo que no existe o no es accesible (NF-05: no se distingue).</summary>
    public static AssistantAnswer NotAvailable(string intent, IReadOnlyList<string> references, string topic)
    {
        var reference = references.Count > 0 ? $" para «{references[0]}»" : string.Empty;
        return new AssistantAnswer(
            intent,
            AssistantAnswerTypes.NotAvailable,
            $"No encontré información disponible{reference} con su usuario. El asistente solo informa los embarques, " +
            "documentos, cargos y facturas a los que usted tiene acceso en el portal. Verifique el número ingresado.",
            [], [], [], topic);
    }

    private async Task<ShipmentDetailDto?> ResolveShipmentAsync(IReadOnlyList<string> references, CancellationToken cancellationToken)
    {
        foreach (var reference in references.Take(MaxReferences))
        {
            var detail = await sender.Send(new GetShipmentDetailQuery(reference), cancellationToken);
            if (detail.IsSuccess)
                return detail.Value;

            // Booking exacto entre los embarques accesibles (M1-21).
            var byBooking = await sender.Send(new SearchShipmentsQuery(BookingNumber: reference, PageSize: 5), cancellationToken);
            var match = byBooking.IsSuccess
                ? byBooking.Value.Items.FirstOrDefault(i => string.Equals(i.BookingNumber, reference, StringComparison.OrdinalIgnoreCase))
                : null;
            if (match is null)
                continue;

            detail = await sender.Send(new GetShipmentDetailQuery(match.BlNumber), cancellationToken);
            if (detail.IsSuccess)
                return detail.Value;
        }

        return null;
    }

    private async Task<AssistantAnswer> ShipmentStatusAsync(IReadOnlyList<string> references, string topic, CancellationToken cancellationToken)
    {
        var detail = await ResolveShipmentAsync(references, cancellationToken);
        if (detail is null)
            return NotAvailable(AssistantIntents.ShipmentStatus, references, topic);

        var facts = new List<AssistantFact>
        {
            new("BL", detail.BlNumber),
            new("Booking", AssistantFormat.Value(detail.BookingNumber)),
            new("Operación", AssistantFormat.Operation(detail.Operation)),
            new("Estado del embarque", AssistantFormat.ShipmentStatus(detail.Status)),
            new("Nave", AssistantFormat.Value(detail.Vessel)),
            new("Viaje", AssistantFormat.Value(detail.Voyage)),
            new("Puerto de carga", AssistantFormat.Value(detail.PortOfLoading)),
            new("Puerto de descarga", AssistantFormat.Value(detail.PortOfDischarge)),
            new("ETD", AssistantFormat.LocalTime(detail.Etd, detail.Country)),
            new("ETA", AssistantFormat.LocalTime(detail.Eta, detail.Country)),
        };

        if (detail.Issuance is { } issuance)
        {
            if (issuance.Available)
            {
                var type = issuance.DocumentType is null
                    ? AssistantFormat.NotAvailable
                    : issuance.EblPlatform is null ? issuance.DocumentType : $"{issuance.DocumentType} ({issuance.EblPlatform})";
                facts.Add(new AssistantFact("Documento de transporte", type));
                facts.Add(new AssistantFact(
                    "Estado de emisión",
                    issuance.Status is null
                        ? AssistantFormat.NotAvailable
                        : $"{AssistantFormat.IssuanceStatus(issuance.Status)} ({AssistantFormat.LocalTime(issuance.StatusAt, detail.Country)})"));
            }
            else
            {
                facts.Add(new AssistantFact("Estado de emisión", "no disponible: el sistema de origen no respondió"));
            }
        }

        if (detail.LocalCharges is not null)
        {
            facts.Add(new AssistantFact(
                "Recargos locales pendientes",
                detail.LocalCharges.Count(c => c.Status == ChargeStatus.Pending).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new AssistantAnswer(
            AssistantIntents.ShipmentStatus,
            AssistantAnswerTypes.Data,
            Draft($"Estado del BL {detail.BlNumber} registrado en el portal:", facts),
            facts,
            [new AssistantCitationDto(AssistantReferences.Shipment, detail.BlNumber, null, $"/api/v1/shipments/{Uri.EscapeDataString(detail.BlNumber)}")],
            [new AssistantActionDto(AssistantReferences.OpenShipment, $"Ver el detalle del BL {detail.BlNumber}", null, detail.BlNumber, detail.Id)],
            topic);
    }

    private async Task<AssistantAnswer> DocumentsAsync(IReadOnlyList<string> references, string topic, CancellationToken cancellationToken)
    {
        var detail = await ResolveShipmentAsync(references, cancellationToken);
        if (detail is null)
            return NotAvailable(AssistantIntents.ShipmentDocuments, references, topic);

        var result = await sender.Send(new GetShipmentDocumentsQuery(detail.BlNumber), cancellationToken);
        if (result.IsFailure)
            return NotAvailable(AssistantIntents.ShipmentDocuments, references, topic);

        var repository = result.Value;
        var citation = new AssistantCitationDto(
            AssistantReferences.Documents, repository.BlNumber, null, $"/api/v1/documents/{Uri.EscapeDataString(repository.BlNumber)}");

        if (repository.Documents.Count == 0 && repository.Related.Count == 0)
        {
            return new AssistantAnswer(
                AssistantIntents.ShipmentDocuments,
                AssistantAnswerTypes.Data,
                $"El BL {repository.BlNumber} no tiene documentos disponibles para descargar en el portal.",
                [new AssistantFact("Documentos disponibles", "0")],
                [citation],
                [],
                topic);
        }

        var facts = new List<AssistantFact>();
        var actions = new List<AssistantActionDto>();

        foreach (var document in repository.Documents.Take(MaxListedItems))
        {
            facts.Add(new AssistantFact(
                AssistantFormat.DocumentType(document.DocumentType),
                $"{document.DocumentNumber}, emitido el {AssistantFormat.LocalTime(document.IssuedAt, repository.Country)}"));
            actions.Add(new AssistantActionDto(
                AssistantReferences.DownloadDocument,
                $"Descargar {AssistantFormat.DocumentType(document.DocumentType)} {document.DocumentNumber}",
                $"/api/v1/documents/{Uri.EscapeDataString(repository.BlNumber)}/{document.Id}/download",
                repository.BlNumber,
                document.Id));
        }

        foreach (var related in repository.Related.Take(MaxListedItems))
        {
            var isInvoice = related.Kind == AssistantReferences.Invoice;
            facts.Add(new AssistantFact(
                isInvoice ? "Factura" : "Recibo de pago",
                $"{related.Number}, emitido el {AssistantFormat.LocalTime(related.IssuedAt, repository.Country)}"));
            actions.Add(new AssistantActionDto(
                isInvoice ? AssistantReferences.DownloadInvoice : AssistantReferences.DownloadReceipt,
                $"Descargar {(isInvoice ? "factura" : "recibo")} {related.Number}",
                related.DownloadPath,
                repository.BlNumber,
                related.Id));
        }

        return new AssistantAnswer(
            AssistantIntents.ShipmentDocuments,
            AssistantAnswerTypes.Data,
            Draft($"Documentos disponibles del BL {repository.BlNumber}:", facts),
            facts,
            [citation],
            actions,
            topic);
    }

    private async Task<AssistantAnswer> ShipmentChargesAsync(IReadOnlyList<string> references, string topic, CancellationToken cancellationToken)
    {
        var detail = await ResolveShipmentAsync(references, cancellationToken);
        if (detail is null)
            return NotAvailable(AssistantIntents.PendingCharges, references, topic);

        var facts = new List<AssistantFact>();
        var amounts = new List<(string Currency, decimal Total)>();

        if (detail.LocalCharges is null)
        {
            facts.Add(new AssistantFact("Recargos locales", "no disponibles para su perfil en este BL"));
        }
        else
        {
            foreach (var charge in detail.LocalCharges.Where(c => c.Status == ChargeStatus.Pending))
            {
                facts.Add(new AssistantFact(charge.ChargeCode, $"{AssistantFormat.Amount(charge.TotalAmount, charge.Currency)} ({AssistantFormat.Value(charge.Description)})"));
                amounts.Add((charge.Currency, charge.TotalAmount));
            }
        }

        if (detail.DemurrageCharges is not null)
        {
            foreach (var line in detail.DemurrageCharges.Where(l => !l.IsExempt && l.Status != DemurrageChargeStatus.Paid))
            {
                facts.Add(new AssistantFact($"Demurrage {line.ContainerNumber}", AssistantFormat.Amount(line.TotalAmount, line.Currency)));
                amounts.Add((line.Currency, line.TotalAmount));
            }
        }

        if (detail.Freight is { Status: "PENDING" } freight && freight.Amount > 0m)
        {
            facts.Add(new AssistantFact("Flete", AssistantFormat.Amount(freight.Amount, freight.Currency)));
            amounts.Add((freight.Currency, freight.Amount));
        }

        var citation = new AssistantCitationDto(
            AssistantReferences.Charges, detail.BlNumber, null, $"/api/v1/charges/{Uri.EscapeDataString(detail.BlNumber)}");

        if (amounts.Count == 0)
        {
            facts.Add(new AssistantFact("Cargos pendientes", "0"));
            return new AssistantAnswer(
                AssistantIntents.PendingCharges,
                AssistantAnswerTypes.Data,
                Draft($"El BL {detail.BlNumber} no registra cargos pendientes de pago visibles para su usuario en el portal.", facts),
                facts,
                [citation],
                [new AssistantActionDto(AssistantReferences.OpenShipment, $"Ver el detalle del BL {detail.BlNumber}", null, detail.BlNumber, detail.Id)],
                topic);
        }

        foreach (var total in amounts.GroupBy(a => a.Currency).OrderBy(g => g.Key))
            facts.Add(new AssistantFact($"Total {total.Key}", AssistantFormat.Amount(total.Sum(a => a.Total), total.Key)));

        return new AssistantAnswer(
            AssistantIntents.PendingCharges,
            AssistantAnswerTypes.Data,
            Draft($"Cargos pendientes de pago del BL {detail.BlNumber} registrados en el portal:", facts),
            facts,
            [citation],
            [
                new AssistantActionDto(AssistantReferences.OpenCharges, $"Ver los cargos del BL {detail.BlNumber}", null, detail.BlNumber, detail.Id),
                new AssistantActionDto(AssistantReferences.OpenCart, "Ir al carro de pagos", null, null, null),
            ],
            topic);
    }

    /// <summary>Resumen de los servicios pendientes de pago del dashboard (M1-05) cuando no se indica un BL.</summary>
    private async Task<AssistantAnswer> PendingSummaryAsync(string topic, CancellationToken cancellationToken)
    {
        var dashboard = await sender.Send(new GetDashboardQuery(), cancellationToken);
        if (dashboard.IsFailure)
            return NotAvailable(AssistantIntents.PendingCharges, [], topic);

        var pending = dashboard.Value.PendingPayments;
        var citation = new AssistantCitationDto(AssistantReferences.PendingPayments, "dashboard", null, "/api/v1/dashboard");

        if (pending.Count == 0)
        {
            return new AssistantAnswer(
                AssistantIntents.PendingCharges,
                AssistantAnswerTypes.Data,
                "No hay servicios pendientes de pago visibles para su usuario en el portal.",
                [new AssistantFact("Servicios pendientes", "0")],
                [citation],
                [],
                topic);
        }

        var facts = new List<AssistantFact> { new("Servicios pendientes", pending.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
        facts.AddRange(pending.Totals.Select(t => new AssistantFact($"Total {t.Currency}", AssistantFormat.Amount(t.Total, t.Currency))));
        facts.AddRange(pending.Items.Take(MaxListedItems).Select(i => new AssistantFact(
            $"{i.ConceptCode} {i.BlNumber ?? string.Empty}".Trim(),
            AssistantFormat.Amount(i.TotalAmount, i.Currency) + (i.DueDate is null ? string.Empty : $", vence el {AssistantFormat.Date(i.DueDate)}"))));

        return new AssistantAnswer(
            AssistantIntents.PendingCharges,
            AssistantAnswerTypes.Data,
            Draft("Servicios pendientes de pago registrados en el portal para su usuario:", facts),
            facts,
            [citation],
            [new AssistantActionDto(AssistantReferences.OpenCart, "Ir al carro de pagos", null, null, null)],
            topic);
    }

    /// <summary>Factura por número de origen o folio SII, solo si es visible según M7-01.</summary>
    private async Task<AssistantAnswer> InvoiceAsync(IReadOnlyList<string> references, string topic, CancellationToken cancellationToken)
    {
        var invoiceScope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        var organizationIds = invoiceScope.BlFilter.Keys.ToList();
        var candidates = references.Take(MaxReferences).ToList();

        List<CustomerInvoice> invoices = organizationIds.Count == 0
            ? []
            : await dbContext.CustomerInvoices.AsNoTracking()
                .Where(i => organizationIds.Contains(i.OrganizationId)
                    && (candidates.Contains(i.SourceNumber) || (i.SiiNumber != null && candidates.Contains(i.SiiNumber))))
                .ToListAsync(cancellationToken);

        var invoice = invoices.FirstOrDefault(i => InvoiceAccess.CanView(invoiceScope, i));
        if (invoice is null)
            return NotAvailable(AssistantIntents.InvoiceDetail, references, topic);

        var view = InvoiceView.ToDto(invoice, BusinessCalendar.LocalDate(invoice.Country, DateTime.UtcNow), inCart: false);
        var facts = new List<AssistantFact>
        {
            new("Número", view.SourceNumber),
            new("Folio SII", AssistantFormat.Value(view.SiiNumber)),
            new("Tipo", view.DocumentType),
            new("Fecha de emisión", AssistantFormat.Date(view.IssueDate)),
            new("Vencimiento", AssistantFormat.Date(view.DueDate)),
            new("BL", AssistantFormat.Value(view.BlNumber)),
            new("Razón social", view.LegalName),
            new("Neto", AssistantFormat.Amount(view.NetAmount, view.Currency)),
            new("Impuesto", AssistantFormat.Amount(view.TaxAmount, view.Currency)),
            new("Total", AssistantFormat.Amount(view.TotalAmount, view.Currency)),
            new("Estado", view.Status),
        };

        var actions = new List<AssistantActionDto>
        {
            new(AssistantReferences.OpenInvoices, "Ver mis facturas", null, view.BlNumber, view.Id),
        };
        if (view.CanDownload)
            actions.Add(new AssistantActionDto(AssistantReferences.DownloadInvoice, $"Descargar la factura {view.SiiNumber}", $"/api/v1/invoices/{view.Id}/pdf", view.BlNumber, view.Id));

        return new AssistantAnswer(
            AssistantIntents.InvoiceDetail,
            AssistantAnswerTypes.Data,
            Draft($"Detalle de la factura {view.SourceNumber} registrada en el portal:", facts),
            facts,
            [new AssistantCitationDto(AssistantReferences.Invoice, view.SourceNumber, null, $"/api/v1/invoices?blNumber={Uri.EscapeDataString(view.BlNumber ?? string.Empty)}")],
            actions,
            topic);
    }

    private async Task<AssistantAnswer> TatcAsync(IReadOnlyList<string> references, string topic, CancellationToken cancellationToken)
    {
        var detail = await ResolveShipmentAsync(references, cancellationToken);
        if (detail is null)
            return NotAvailable(AssistantIntents.TatcStatus, references, topic);

        var result = await sender.Send(new GetShipmentTatcQuery(detail.BlNumber), cancellationToken);
        var citation = new AssistantCitationDto(
            AssistantReferences.Tatc, detail.BlNumber, null, $"/api/v1/shipments/{Uri.EscapeDataString(detail.BlNumber)}/tatc");

        if (result.IsFailure)
        {
            var text = result.Error == Error.Forbidden
                ? $"La consulta del TATC del BL {detail.BlNumber} no está habilitada para su perfil."
                : $"El TATC no aplica al BL {detail.BlNumber}: corresponde solo a embarques de importación.";
            return new AssistantAnswer(AssistantIntents.TatcStatus, AssistantAnswerTypes.NotAvailable, text, [], [citation], [], topic);
        }

        var tatc = result.Value;
        if (!tatc.Available)
        {
            return new AssistantAnswer(
                AssistantIntents.TatcStatus,
                AssistantAnswerTypes.SourceUnavailable,
                $"El sistema de TATC no respondió en este momento, por lo que no puedo informar el estado del TATC del BL {detail.BlNumber}. " +
                "Intente nuevamente en unos minutos.",
                [], [citation], [], topic);
        }

        var facts = new List<AssistantFact>
        {
            new("Estado del BL", AssistantFormat.ShipmentStatus(tatc.BlStatus)),
            new("Estado del TATC", AssistantFormat.TatcStatus(tatc.Status)),
        };
        foreach (var container in tatc.Containers.Take(MaxListedItems))
        {
            var reasons = container.PendingReasons.Count == 0 ? string.Empty : $", pendiente por {string.Join(", ", container.PendingReasons)}";
            facts.Add(new AssistantFact(
                $"Contenedor {container.ContainerNumber}",
                $"{AssistantFormat.TatcStatus(container.Status)}{(container.TatcNumber is null ? string.Empty : $" (TATC {container.TatcNumber})")}{reasons}"));
        }

        facts.Add(new AssistantFact("Actualizado en el origen", AssistantFormat.LocalTime(tatc.SourceUpdatedAt, tatc.Country)));

        return new AssistantAnswer(
            AssistantIntents.TatcStatus,
            AssistantAnswerTypes.Data,
            Draft($"Estado del BL {detail.BlNumber} y de su TATC según el sistema de TATC:", facts),
            facts,
            [citation],
            [new AssistantActionDto(AssistantReferences.OpenShipment, $"Ver el detalle del BL {detail.BlNumber}", null, detail.BlNumber, detail.Id)],
            topic);
    }

    private static string Draft(string header, IEnumerable<AssistantFact> facts) =>
        header + "\n" + string.Join("\n", facts.Select(f => $"- {f.Label}: {f.Value}"));
}
