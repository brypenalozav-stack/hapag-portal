namespace HapagPortal.Application.Documents.Common;

using System.Security.Cryptography;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Quién emite, descarga o envía un documento (NF-14). Sin usuario = el sistema. <c>Channel</c>: canal por el que actúa
/// cuando no es el portal (el Web Service de M3-17); nulo = portal o sistema.
/// </summary>
public sealed record DocumentActor(
    Guid? UserId,
    string? Email,
    Guid? OrganizationId,
    Guid? OnBehalfOfOrganizationId,
    Guid? AccessGrantId,
    string? Channel = null)
{
    public static readonly DocumentActor System = new(null, null, null, null, null);
}

/// <summary>Emisión de un documento ya armado (modelo con número y código de verificación).</summary>
public sealed record DocumentIssue(
    string DocumentType,
    BillOfLading BillOfLading,
    PdfDocumentModel Model,
    string Origin,
    DocumentActor Actor,
    Guid? IssuedForOrganizationId,
    IReadOnlyList<string> ContainerNumbers,
    Guid? PaymentId = null,
    Guid? PaymentDetailId = null,
    string? GenerationKey = null,
    IReadOnlyList<string>? RecipientEmails = null,
    string? TermsVersion = null,
    DateTime? TermsAcceptedAt = null,
    DateTime? ValidUntil = null);

/// <summary>
/// Emisión y entrega de los documentos del embarque (M6-01 a M6-09): genera el PDF con la plantilla, lo
/// firma si el tipo lo exige (M6-01, M6-07) por <see cref="IDocumentSigner"/>, lo guarda por
/// <see cref="IFileStorage"/>, lo registra en el repositorio con su huella y deja cada emisión, descarga y
/// envío en el registro de NF-14 con el canal (portal, correo o asistente de M10-04). Los permisos por tipo
/// de documento siguen M1-11 (<see cref="ShipmentDocumentAccess"/>). No guarda: lo hace el llamador.
/// </summary>
public sealed class ShipmentDocumentService(
    IApplicationDbContext dbContext,
    IPdfDocumentRenderer renderer,
    IDocumentSigner signer,
    IFileStorage fileStorage,
    IEmailService emailService,
    DocumentSettings settings)
{
    public const string PdfContentType = "application/pdf";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DocumentSettings Settings => settings;

    /// <summary>BL, partes, unidades, mercancía y cargos con que se arman las plantillas.</summary>
    public async Task<ShipmentDocumentData> LoadDataAsync(BillOfLading billOfLading, CancellationToken cancellationToken)
    {
        var id = billOfLading.Id;

        var parties = await dbContext.BLParties.AsNoTracking().Where(p => p.BillOfLadingId == id).ToListAsync(cancellationToken);
        var containers = await dbContext.BLContainers.AsNoTracking().Where(c => c.BillOfLadingId == id).ToListAsync(cancellationToken);
        var cargo = await dbContext.BLCargoItems.AsNoTracking().Where(c => c.BillOfLadingId == id).ToListAsync(cancellationToken);
        var charges = await dbContext.LocalCharges.AsNoTracking().Where(c => c.BillOfLadingId == id).ToListAsync(cancellationToken);

        return new ShipmentDocumentData(billOfLading, parties, containers, cargo, charges);
    }

    /// <summary>Número, instante de emisión, código de verificación y emisor de un documento nuevo.</summary>
    public DocumentHeader NewHeader(string documentType, BillOfLading billOfLading, DateTime now)
    {
        var number = PaymentLifecycle.NewNumber(DocumentPrefixes.ForDocument(documentType), now);
        return new DocumentHeader(
            number,
            now,
            ShipmentDocumentTemplates.VerificationCode(number, billOfLading.BLNumber, now),
            settings.IssuerFor(billOfLading.Country));
    }

    /// <summary>Genera, firma si corresponde, guarda y registra el documento. Lo agrega al contexto sin guardar.</summary>
    public async Task<Result<ShipmentDocument>> IssueAsync(DocumentIssue issue, CancellationToken cancellationToken)
    {
        var bl = issue.BillOfLading;
        var document = new ShipmentDocument
        {
            DocumentType = issue.DocumentType,
            DocumentNumber = issue.Model.DocumentNumber,
            Status = ShipmentDocumentStatus.Issued,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            BookingNumber = bl.BookingNumber,
            Country = bl.Country,
            ContainerNumbers = issue.ContainerNumbers.Count == 0 ? null : string.Join(',', issue.ContainerNumbers),
            IssuedAt = issue.Model.IssuedAt,
            IssuedForOrganizationId = issue.IssuedForOrganizationId,
            IssuedByUserId = issue.Actor.UserId,
            IssuedByEmail = issue.Actor.Email,
            OnBehalfOfOrganizationId = issue.Actor.OnBehalfOfOrganizationId,
            AccessGrantId = issue.Actor.AccessGrantId,
            Origin = issue.Origin,
            PaymentId = issue.PaymentId,
            PaymentDetailId = issue.PaymentDetailId,
            GenerationKey = issue.GenerationKey,
            FileName = ShipmentDocumentTemplates.FileName(issue.DocumentType, issue.Model.DocumentNumber),
            ContentType = PdfContentType,
            VerificationCode = issue.Model.VerificationCode ?? string.Empty,
            TemplateJson = JsonSerializer.Serialize(issue.Model, JsonOptions),
            RecipientEmails = issue.RecipientEmails is { Count: > 0 } recipients ? string.Join(',', recipients) : null,
            TermsVersion = issue.TermsVersion,
            TermsAcceptedAt = issue.TermsAcceptedAt,
            ValidUntil = issue.ValidUntil,
            RetainUntil = issue.Model.IssuedAt.AddYears(Math.Max(1, settings.RetentionYears)),
            CreatedAt = issue.Model.IssuedAt,
            CreatedBy = issue.Actor.Email ?? "SYSTEM"
        };

        var stored = await MaterializeAsync(document, issue.Model, cancellationToken);
        if (stored.IsFailure)
            return Result<ShipmentDocument>.Failure(stored.Error);

        dbContext.ShipmentDocuments.Add(document);
        Log(document, ShipmentDocumentEventTypes.Issued,
            issue.Actor.Channel ?? (issue.Actor.UserId is null ? DocumentChannels.System : DocumentChannels.Portal),
            issue.Actor, issue.Model.IssuedAt);

        return Result<ShipmentDocument>.Success(document);
    }

    /// <summary>
    /// Contenido del documento. Uno sembrado sin archivo (<c>StorageKey</c> nulo) se genera a partir de su
    /// modelo, se firma y se guarda en esta primera lectura; el documento debe estar en seguimiento.
    /// </summary>
    public async Task<Result<DocumentFileDto>> ReadAsync(ShipmentDocument document, CancellationToken cancellationToken)
    {
        if (document.StorageKey is null)
        {
            var model = JsonSerializer.Deserialize<PdfDocumentModel>(document.TemplateJson, JsonOptions)
                ?? throw new InvalidOperationException($"The document '{document.Id}' has no template.");

            var materialized = await MaterializeAsync(document, model, cancellationToken);
            return materialized.IsFailure
                ? Result<DocumentFileDto>.Failure(materialized.Error)
                : Result<DocumentFileDto>.Success(new DocumentFileDto(materialized.Value, document.ContentType, document.FileName));
        }

        var opened = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (opened.IsFailure)
            return Result<DocumentFileDto>.Failure(opened.Error);
        if (opened.Value is null)
            return Result<DocumentFileDto>.Failure(DomainErrors.ShipmentDocument.ContentNotFound);

        await using var stream = opened.Value;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return Result<DocumentFileDto>.Success(new DocumentFileDto(buffer.ToArray(), document.ContentType, document.FileName));
    }

    /// <summary>Envía el documento adjunto a cada destinatario y registra cada envío (NF-14).</summary>
    public async Task<Result<IReadOnlyList<string>>> SendAsync(
        ShipmentDocument document,
        IReadOnlyList<string> recipients,
        string channel,
        DocumentActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var to = recipients
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (to.Count == 0)
            return Result<IReadOnlyList<string>>.Failure(DomainErrors.ShipmentDocument.NoRecipient);

        var file = await ReadAsync(document, cancellationToken);
        if (file.IsFailure)
            return Result<IReadOnlyList<string>>.Failure(file.Error);

        var title = Title(document.DocumentType);
        var body =
            $"Adjuntamos {title} {document.DocumentNumber} del BL {document.BlNumber}.\n" +
            $"Código de verificación: {document.VerificationCode}.\n\n" +
            "Este documento también está disponible en el repositorio documental del embarque en el Portal de Clientes de Hapag-Lloyd.";
        var attachment = new EmailAttachment(document.FileName, document.ContentType, file.Value.Content);

        foreach (var recipient in to)
        {
            await emailService.SendEmailAsync(
                recipient, $"Hapag-Lloyd - {title} {document.DocumentNumber} - BL {document.BlNumber}", body, [attachment], cancellationToken);
            Log(document, ShipmentDocumentEventTypes.Sent, channel, actor, now, recipient);
        }

        document.DeliveredAt = now;
        return Result<IReadOnlyList<string>>.Success(to);
    }

    /// <summary>Registra un evento del documento (append-only, NF-14).</summary>
    public void Log(ShipmentDocument document, string eventType, string channel, DocumentActor actor, DateTime now, string? recipient = null) =>
        dbContext.ShipmentDocumentEvents.Add(new ShipmentDocumentEvent
        {
            ShipmentDocumentId = document.Id,
            EventType = eventType,
            Channel = channel,
            OccurredAt = now,
            UserId = actor.UserId,
            UserEmail = actor.Email,
            OrganizationId = actor.OrganizationId,
            OnBehalfOfOrganizationId = actor.OnBehalfOfOrganizationId,
            Recipient = recipient
        });

    /// <summary>El tipo de documento es visible para el usuario según M1-11 (el administrador interno ve todo).</summary>
    public static bool CanView(ShipmentPermissionSet permissions, string documentType) =>
        permissions.Can(ShipmentDocumentAccess.ActionFor(documentType));

    public static ShipmentDocumentDto ToDto(ShipmentDocument d, string? organizationName) => new(
        d.Id,
        d.DocumentType,
        d.DocumentNumber,
        d.Status,
        d.BillOfLadingId,
        d.BlNumber,
        d.BookingNumber,
        d.Country,
        Split(d.ContainerNumbers),
        d.IssuedAt,
        d.Origin,
        d.IssuedForOrganizationId,
        organizationName,
        d.FileName,
        d.ContentType,
        d.SizeBytes,
        d.ContentHash,
        d.VerificationCode,
        ShipmentDocumentTypes.Signed.Contains(d.DocumentType),
        d.SignatureId,
        d.SignatureProvider,
        d.SignatureLevel,
        d.SignedAt,
        d.PaymentId,
        Split(d.RecipientEmails),
        d.DeliveredAt,
        d.TermsVersion,
        d.TermsAcceptedAt,
        d.ValidUntil,
        d.RetainUntil);

    public static string Title(string documentType) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => "el certificado de transbordo",
        ShipmentDocumentTypes.GateOutCoupon => "el cupón de retiro de Gate Out",
        ShipmentDocumentTypes.CollectReceipt => "el comprobante Collect",
        ShipmentDocumentTypes.BlCopyValued => "la copia valorada del BL",
        ShipmentDocumentTypes.BlCopyNonValued => "la copia no valorada del BL",
        ShipmentDocumentTypes.ResponsibilityLetter => "la carta de responsabilidad",
        ShipmentDocumentTypes.NoDebtCertificate => "el certificado de libre deuda",
        ShipmentDocumentTypes.GateOutAdvanceReceipt => "el recibo del pago anticipado de Gate Out",
        ShipmentDocumentTypes.FreightCertificate => "el certificado de flete",
        ShipmentDocumentTypes.ReleaseLetter => "la carta de liberación y desconsolidado",
        _ => "el documento"
    };

    private static IReadOnlyList<string> Split(string? csv) =>
        string.IsNullOrWhiteSpace(csv) ? [] : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Genera el PDF, lo firma si el tipo lo exige y lo guarda; completa clave, tamaño, huella y firma.</summary>
    private async Task<Result<byte[]>> MaterializeAsync(ShipmentDocument document, PdfDocumentModel model, CancellationToken cancellationToken)
    {
        var content = renderer.Render(model);

        var signatureType = SignatureDocumentTypes.For(document.DocumentType);
        if (signatureType is not null)
        {
            var signed = await signer.SignAsync(
                new SignDocumentRequest(content, signatureType, $"HAPAG_LLOYD_{document.Country}"), cancellationToken);
            if (signed.IsFailure)
                return Result<byte[]>.Failure(signed.Error);

            content = signed.Value.SignedContent;
            document.SignatureId = signed.Value.SignatureId;
            document.SignatureLevel = signed.Value.SignatureLevel;
            document.SignatureProvider = signer.Provider;
            document.SignedAt = signed.Value.SignedAt;
        }

        using var stream = new MemoryStream(content, writable: false);
        var stored = await fileStorage.SaveAsync(stream, document.FileName, document.ContentType, settings.StorageContainer, cancellationToken);
        if (stored.IsFailure)
            return Result<byte[]>.Failure(stored.Error);

        document.StorageKey = stored.Value;
        document.SizeBytes = content.LongLength;
        document.ContentHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        return Result<byte[]>.Success(content);
    }
}
