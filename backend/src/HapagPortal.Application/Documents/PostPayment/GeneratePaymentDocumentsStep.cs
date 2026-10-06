namespace HapagPortal.Application.Documents.PostPayment;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Qué ítems pagados emiten un documento al liberarse: el servicio de certificado de transbordo (M6-01),
/// el Gate Out (cupón de retiro, M6-03) y el flete de un BL Collect (comprobante para la agencia, M6-04).
/// El Gate Out pagado en exportación (la factura se emite después del zarpe) emite además el recibo del pago
/// anticipado (M3-19), salvo en una imputación a crédito, que no es un pago (M5-10).
/// </summary>
public static class PaymentDocumentRules
{
    public const string CollectFreightTerms = "Collect";

    public static bool IsCollect(BillOfLading billOfLading) =>
        string.Equals(billOfLading.FreightTerms, CollectFreightTerms, StringComparison.OrdinalIgnoreCase);

    public static bool IsExport(BillOfLading billOfLading) =>
        string.Equals(billOfLading.ShipmentType, "Export", StringComparison.OrdinalIgnoreCase);

    public static string? DocumentTypeFor(PaymentDetail detail, BillOfLading? billOfLading) => detail.ItemType switch
    {
        PayableItemTypes.LocalCharge when detail.ConceptType == ChargeConceptCodes.TransshipmentCertificate =>
            ShipmentDocumentTypes.TransshipmentCertificate,
        PayableItemTypes.LocalCharge when detail.ConceptType == ChargeConceptCodes.GateOut =>
            ShipmentDocumentTypes.GateOutCoupon,
        PayableItemTypes.Freight when billOfLading is not null && IsCollect(billOfLading) =>
            ShipmentDocumentTypes.CollectReceipt,
        _ => null
    };

    /// <summary>Documentos que emite el ítem liberado de un pago (o de una imputación a crédito).</summary>
    public static IReadOnlyList<string> DocumentTypesFor(Payment payment, PaymentDetail detail, BillOfLading? billOfLading)
    {
        var types = new List<string>();
        if (DocumentTypeFor(detail, billOfLading) is { } type)
            types.Add(type);

        if (detail.ItemType == PayableItemTypes.LocalCharge
            && detail.ConceptType == ChargeConceptCodes.GateOut
            && billOfLading is not null
            && IsExport(billOfLading)
            && payment.Origin != PaymentOrigins.CreditLine)
        {
            types.Add(ShipmentDocumentTypes.GateOutAdvanceReceipt);
        }

        return types;
    }

    /// <summary>BL de cada ítem: el del detalle o, en el flete, la fuente.</summary>
    public static Guid? BlIdOf(PaymentDetail detail) =>
        detail.BillOfLadingId ?? (detail.ItemType == PayableItemTypes.Freight ? detail.SourceId : null);

    public static async Task<IReadOnlyDictionary<Guid, BillOfLading>> LoadBlsAsync(
        IApplicationDbContext dbContext,
        IEnumerable<PaymentDetail> details,
        CancellationToken cancellationToken)
    {
        var ids = details.Select(BlIdOf).Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        return ids.Count == 0
            ? new Dictionary<Guid, BillOfLading>()
            : await dbContext.BillsOfLading.AsNoTracking().Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken);
    }

    /// <summary>Algún ítem liberado del pago emite un documento.</summary>
    public static async Task<bool> IssuesDocumentsAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        IReadOnlyList<PaymentDetail> details,
        CancellationToken cancellationToken)
    {
        var released = details.Where(d => d.ReleasedAt is not null).ToList();
        if (released.Count == 0)
            return false;

        var bls = await LoadBlsAsync(dbContext, released, cancellationToken);
        return released.Any(d => DocumentTypesFor(payment, d, BlIdOf(d) is { } id ? bls.GetValueOrDefault(id) : null).Count > 0);
    }
}

/// <summary>
/// Paso posterior al pago (NF-03) que emite los documentos de los ítems liberados: certificado de
/// transbordo firmado y enviado al cliente o a UMAR (M6-01), cupón de retiro de Gate Out asociado al BL y
/// a sus unidades (M6-03), comprobante Collect (M6-04) y recibo del pago anticipado de Gate Out, vinculado al
/// anticipo para cruzarlo con la factura posterior (M3-19). Idempotente: cada ítem pagado emite un solo
/// documento de cada tipo (<c>GenerationKey</c>) y un certificado ya enviado no se reenvía; un fallo (firma,
/// almacenamiento) se reintenta con la cola sin duplicar lo ya emitido.
/// </summary>
public sealed class GeneratePaymentDocumentsStep(
    IApplicationDbContext dbContext,
    ShipmentDocumentService documents,
    INotificationPublisher notificationPublisher) : IPaymentPostStep
{
    public string JobType => PaymentOutboxJobTypes.Documents;

    public async Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        var released = details.Where(d => d.ReleasedAt is not null).ToList();
        var bls = await PaymentDocumentRules.LoadBlsAsync(dbContext, released, cancellationToken);
        var payer = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == payment.ClientId, cancellationToken)
            ?? throw new InvalidOperationException($"The payer organization '{payment.ClientId}' does not exist.");

        foreach (var detail in released)
        {
            var bl = PaymentDocumentRules.BlIdOf(detail) is { } blId ? bls.GetValueOrDefault(blId) : null;
            if (bl is null)
                continue;

            foreach (var type in PaymentDocumentRules.DocumentTypesFor(payment, detail, bl))
                await IssueOnceAsync(type, bl, payment, detail, payer, now, cancellationToken);
        }
    }

    private async Task IssueOnceAsync(
        string type,
        BillOfLading bl,
        Payment payment,
        PaymentDetail detail,
        Client payer,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var key = $"{type}:{detail.Id}";
        var document = await dbContext.ShipmentDocuments.FirstOrDefaultAsync(d => d.GenerationKey == key, cancellationToken);
        var recipients = type == ShipmentDocumentTypes.TransshipmentCertificate ? TransshipmentRecipients(bl, payer) : [];

        if (document is null)
        {
            document = await IssueAsync(type, key, bl, payment, detail, payer, recipients, now, cancellationToken);

            if (payment.CreatedByUserId is not null)
            {
                await notificationPublisher.PublishAsync(
                    new NotificationRequest(
                        NotificationTypes.DocumentIssued,
                        $"Documento emitido {document.DocumentNumber}",
                        $"Se emitió {ShipmentDocumentService.Title(type)} {document.DocumentNumber} del BL {bl.BLNumber}. " +
                        "Está disponible en el repositorio documental del embarque.",
                        UserId: payment.CreatedByUserId,
                        DedupKey: $"document-issued:{document.Id}",
                        Link: new NotificationLink(NotificationEntityTypes.ShipmentDocument, document.Id.ToString(), document.DocumentNumber, bl.BLNumber),
                        Action: new NotificationAction(NotificationActionTypes.OpenDocument, document.Id.ToString())),
                    cancellationToken);
            }
        }

        // M6-01: el certificado se envía automáticamente; un reintento no lo reenvía.
        if (recipients.Count > 0 && document.DeliveredAt is null)
        {
            var sent = await documents.SendAsync(document, recipients, DocumentChannels.Email, SystemActor(payment, detail), now, cancellationToken);
            if (sent.IsFailure)
                throw new InvalidOperationException(sent.Error.Message);
        }

        // M3-19: el recibo del anticipo queda vinculado al anticipo para mostrarlo con la factura posterior.
        if (type == ShipmentDocumentTypes.GateOutAdvanceReceipt)
        {
            var settlement = await dbContext.ChargeSettlements.FirstOrDefaultAsync(s => s.PaymentDetailId == detail.Id, cancellationToken);
            if (settlement is not null)
                settlement.ReceiptDocumentId ??= document.Id;
        }
    }

    private async Task<ShipmentDocument> IssueAsync(
        string type,
        string key,
        BillOfLading bl,
        Payment payment,
        PaymentDetail detail,
        Client payer,
        IReadOnlyList<string> recipients,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(type, bl, now);
        var payerName = payment.PayerName ?? payer.Name;
        var payerTaxId = payment.PayerTaxId ?? TaxIdNormalizer.Normalize(payer.TaxId);
        var reference = payment.ReceiptNumber is null ? payment.PaymentNumber : $"{payment.PaymentNumber} / {payment.ReceiptNumber}";

        var model = type switch
        {
            ShipmentDocumentTypes.TransshipmentCertificate =>
                ShipmentDocumentTemplates.TransshipmentCertificate(data, header, payerName, payerTaxId, reference),
            ShipmentDocumentTypes.GateOutCoupon =>
                ShipmentDocumentTemplates.GateOutCoupon(data, header, payerName, reference, detail.Description),
            ShipmentDocumentTypes.GateOutAdvanceReceipt =>
                ShipmentDocumentTemplates.GateOutAdvanceReceipt(
                    data, header, payerName, payerTaxId, payment.PaymentNumber, payment.ReceiptNumber,
                    payment.PaymentMethodCode ?? payment.PaymentMethod, detail, payment.ConfirmedAt),
            _ => ShipmentDocumentTemplates.CollectReceipt(
                data, header, payerName, payerTaxId, payment.PaymentNumber, payment.ReceiptNumber,
                detail.Amount + detail.TaxAmount, detail.Currency, payment.ConfirmedAt)
        };

        var issued = await documents.IssueAsync(
            new DocumentIssue(
                type,
                bl,
                model,
                ShipmentDocumentOrigins.Payment,
                SystemActor(payment, detail),
                payment.ClientId,
                data.Containers.Select(c => c.ContainerNumber).ToList(),
                payment.Id,
                detail.Id,
                key,
                recipients),
            cancellationToken);

        return issued.IsSuccess
            ? issued.Value
            : throw new InvalidOperationException($"{issued.Error.Code}: {issued.Error.Message}");
    }

    /// <summary>
    /// Destinatarios del certificado (M6-01): importación (CL-IMP-07) a UMAR con copia al cliente,
    /// exportación (CL-EXP-08) al cliente; configurable con <c>Documents:TransshipmentRecipient</c>.
    /// </summary>
    private IReadOnlyList<string> TransshipmentRecipients(BillOfLading bl, Client payer)
    {
        var settings = documents.Settings;
        var umar = string.IsNullOrWhiteSpace(settings.UmarEmail) ? null : settings.UmarEmail.Trim();
        var client = string.IsNullOrWhiteSpace(payer.Email) ? null : payer.Email.Trim();
        var import = string.Equals(bl.ShipmentType, "Import", StringComparison.OrdinalIgnoreCase);

        IEnumerable<string?> recipients = settings.TransshipmentRecipient switch
        {
            Domain.Constants.TransshipmentRecipients.Client => [client],
            Domain.Constants.TransshipmentRecipients.Umar => [umar ?? client],
            _ => import ? [umar, client] : [client]
        };

        return recipients.Where(r => r is not null).Select(r => r!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>La emisión automática la hace el sistema, por cuenta de la organización pagadora (y su mandante).</summary>
    private static DocumentActor SystemActor(Payment payment, PaymentDetail detail) =>
        new(null, null, payment.ClientId, detail.OnBehalfOfClientId ?? payment.OnBehalfOfClientId, detail.AccessGrantId ?? payment.AccessGrantId);
}
