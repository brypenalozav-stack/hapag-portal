namespace HapagPortal.Application.Payments.DepositProofs;

using System.Security.Cryptography;
using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Comprobante de depósito adjuntado a un pago con boleta y su revisión por Finanzas (M5-06).</summary>
public sealed record DepositProofDto(
    Guid Id,
    Guid PaymentId,
    string Status,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? ContentHash,
    string? BankName,
    string? BankReference,
    DateOnly? DepositDate,
    decimal? DepositAmount,
    string? Notes,
    DateTime UploadedAt,
    string UploadedBy,
    DateTime? ReviewedAt,
    string? ReviewedBy,
    string? ReviewNotes,
    string? RejectionReason,
    string DownloadPath);

/// <summary>
/// Comprobantes de un pago con su estado: <c>AwaitingProof</c> verdadero cuando la boleta espera un comprobante
/// (ninguno enviado o el último rechazado); <c>CanUpload</c> si el usuario puede adjuntar uno ahora.
/// </summary>
public sealed record PaymentDepositProofsDto(
    PaymentStatusDto Payment,
    IReadOnlyList<DepositProofDto> Proofs,
    bool AwaitingProof,
    bool CanUpload);

/// <summary>Elemento de la bandeja de Finanzas: comprobante y datos del pago a verificar contra el abono.</summary>
public sealed record DepositProofQueueItemDto(
    DepositProofDto Proof,
    string PaymentNumber,
    string? SlipNumber,
    string PaymentStatus,
    string Country,
    string Currency,
    decimal TotalAmount,
    string? PayerTaxId,
    string? PayerName,
    IReadOnlyList<string> BlNumbers,
    DateTime PaymentCreatedAt,
    string TimeZone);

public sealed record DepositProofReviewResultDto(DepositProofDto Proof, PaymentStatusDto Payment);

public sealed record DepositProofFileDto(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Adjunta el comprobante del depósito (PDF, PNG o JPEG hasta 10 MB) a un pago con boleta de la propia
/// organización (M5-06). Si la boleta aún no se emitió, adjuntar el comprobante la emite (pasa a verificación,
/// M5-02). Un solo comprobante en revisión a la vez; Finanzas lo verifica o lo rechaza con motivo.
/// </summary>
public sealed record UploadDepositProofCommand(
    Guid PaymentId,
    string FileName,
    string ContentType,
    byte[] Content,
    string? BankName,
    string? BankReference,
    DateOnly? DepositDate,
    decimal? DepositAmount,
    string? Notes) : ICommand<PaymentDepositProofsDto>;

public sealed record GetDepositProofsQuery(Guid PaymentId) : IQuery<PaymentDepositProofsDto>;

public sealed record GetDepositProofFileQuery(Guid PaymentId, Guid ProofId) : IQuery<DepositProofFileDto>;

/// <summary>Bandeja de Finanzas (por omisión, los comprobantes por revisar, el más antiguo primero).</summary>
public sealed record GetDepositProofQueueQuery(string? Status = null, string? Country = null) : IQuery<IReadOnlyList<DepositProofQueueItemDto>>;

/// <summary>Finanzas verifica el abono: el pago se confirma por la vía existente (comprobante, liberación, aviso).</summary>
public sealed record VerifyDepositProofCommand(Guid ProofId, string? Notes) : ICommand<DepositProofReviewResultDto>;

/// <summary>Finanzas rechaza el comprobante con motivo: el pago sigue a la espera de un comprobante válido.</summary>
public sealed record RejectDepositProofCommand(Guid ProofId, string Reason) : ICommand<DepositProofReviewResultDto>;

public sealed class UploadDepositProofCommandValidator : AbstractValidator<UploadDepositProofCommand>
{
    public const int MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes = ["application/pdf", "image/png", "image/jpeg"];

    public UploadDepositProofCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(c => AllowedContentTypes.Contains(c))
            .WithMessage("Only PDF, PNG or JPEG files are accepted.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("The file is empty.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("The file must not exceed 10 MB.");
        RuleFor(x => x.BankName).MaximumLength(100);
        RuleFor(x => x.BankReference).MaximumLength(60);
        RuleFor(x => x.DepositAmount).GreaterThan(0m).When(x => x.DepositAmount is not null);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class GetDepositProofQueueQueryValidator : AbstractValidator<GetDepositProofQueueQuery>
{
    public GetDepositProofQueueQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || DepositProofStatus.All.Contains(s))
            .WithMessage("Status must be Submitted, Verified or Rejected.");
        RuleFor(x => x.Country).Must(c => c is null || CountryCodes.ValidCountries.Contains(c)).WithMessage("Country must be CL or BO.");
    }
}

public sealed class VerifyDepositProofCommandValidator : AbstractValidator<VerifyDepositProofCommand>
{
    public VerifyDepositProofCommandValidator()
    {
        RuleFor(x => x.ProofId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RejectDepositProofCommandValidator : AbstractValidator<RejectDepositProofCommand>
{
    public RejectDepositProofCommandValidator()
    {
        RuleFor(x => x.ProofId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Vistas y reglas comunes de los comprobantes de depósito.</summary>
public static class DepositProofs
{
    public const string StorageContainer = "deposit-proofs";

    public static bool IsDeposit(Payment payment) => payment.SlipNumber is not null;

    public static DepositProofDto ToDto(DepositProof p) => new(
        p.Id, p.PaymentId, p.Status, p.FileName, p.ContentType, p.SizeBytes, p.ContentHash, p.BankName, p.BankReference,
        p.DepositDate, p.DepositAmount, p.Notes, p.UploadedAt, p.UploadedBy, p.ReviewedAt, p.ReviewedBy, p.ReviewNotes,
        p.RejectionReason, $"/api/v1/payments/{p.PaymentId}/deposit-proofs/{p.Id}/file");

    public static async Task<PaymentDepositProofsDto> ViewAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        bool owner,
        CancellationToken cancellationToken)
    {
        var proofs = await dbContext.DepositProofs.AsNoTracking()
            .Where(p => p.PaymentId == payment.Id)
            .OrderBy(p => p.UploadedAt)
            .ToListAsync(cancellationToken);

        var status = await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken);
        var open = IsDeposit(payment) && payment.Status is PaymentStatus.Pending or PaymentStatus.PendingVerification;
        var inReview = proofs.Any(p => p.Status == DepositProofStatus.Submitted);

        return new PaymentDepositProofsDto(
            status,
            proofs.Select(ToDto).ToList(),
            AwaitingProof: open && !inReview,
            CanUpload: owner && open && !inReview);
    }
}

public sealed class UploadDepositProofCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<UploadDepositProofCommand, PaymentDepositProofsDto>
{
    public async Task<Result<PaymentDepositProofsDto>> Handle(UploadDepositProofCommand request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.CanOperate)
            return Result<PaymentDepositProofsDto>.Failure(Error.Forbidden);

        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: true, tracking: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentDepositProofsDto>.Failure(loaded.Error);

        var payment = loaded.Value;
        if (!DepositProofs.IsDeposit(payment) || payment.Status is not (PaymentStatus.Pending or PaymentStatus.PendingVerification))
            return Result<PaymentDepositProofsDto>.Failure(DomainErrors.DepositProof.PaymentNotAwaitingProof);

        if (await dbContext.DepositProofs.AnyAsync(p => p.PaymentId == payment.Id && p.Status == DepositProofStatus.Submitted, cancellationToken))
            return Result<PaymentDepositProofsDto>.Failure(DomainErrors.DepositProof.PendingReview);

        var now = DateTime.UtcNow;
        var actor = PaymentActor.From(currentUserService);

        // M5-02: adjuntar el comprobante a una boleta aún no emitida la emite (operación de pago: respeta el bloqueo M8-07).
        if (payment.Status == PaymentStatus.Pending)
        {
            var open = await PaymentBlocks.EnsureOpenAsync(dbContext, payment.Country, now, cancellationToken);
            if (open.IsFailure)
                return Result<PaymentDepositProofsDto>.Failure(open.Error);

            var issued = PaymentLifecycle.Transition(
                dbContext, payment, PaymentStatus.PendingVerification, actor, "Deposit slip issued with the deposit proof (M5-06)", now);
            if (issued.IsFailure)
                return Result<PaymentDepositProofsDto>.Failure(issued.Error);
            payment.SlipIssuedAt ??= now;
        }

        using var stream = new MemoryStream(request.Content, writable: false);
        var stored = await fileStorage.SaveAsync(stream, request.FileName, request.ContentType, DepositProofs.StorageContainer, cancellationToken);
        if (stored.IsFailure)
            return Result<PaymentDepositProofsDto>.Failure(stored.Error);

        var proof = new DepositProof
        {
            PaymentId = payment.Id,
            Status = DepositProofStatus.Submitted,
            FileName = Path.GetFileName(request.FileName),
            ContentType = request.ContentType,
            SizeBytes = request.Content.LongLength,
            StorageKey = stored.Value,
            ContentHash = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant(),
            BankName = Trim(request.BankName),
            BankReference = Trim(request.BankReference),
            DepositDate = request.DepositDate,
            DepositAmount = request.DepositAmount,
            Notes = Trim(request.Notes),
            UploadedAt = now,
            UploadedByUserId = currentUserService.UserId,
            UploadedBy = actor.Name
        };

        dbContext.DepositProofs.Add(proof);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Aviso a Finanzas (consola interna) para verificar el abono.
        await notificationPublisher.PublishAsync(
            new NotificationRequest(
                NotificationTypes.DepositProofSubmitted,
                $"Comprobante de depósito {payment.SlipNumber}",
                $"{payment.PayerName ?? actor.Name} adjuntó el comprobante del depósito del pago {payment.PaymentNumber} " +
                $"({payment.TotalAmount:N2} {payment.Currency}). Verifique el abono en la bandeja de Finanzas.",
                RoleCode: RoleCodes.Administrador,
                DedupKey: $"deposit-proof:{proof.Id}"),
            cancellationToken);

        return Result<PaymentDepositProofsDto>.Success(await DepositProofs.ViewAsync(dbContext, payment, owner: true, cancellationToken));
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetDepositProofsQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetDepositProofsQuery, PaymentDepositProofsDto>
{
    public async Task<Result<PaymentDepositProofsDto>> Handle(GetDepositProofsQuery request, CancellationToken cancellationToken)
    {
        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentDepositProofsDto>.Failure(loaded.Error);

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var owner = loaded.Value.ClientId == scope.OrganizationId && scope.CanOperate;
        return Result<PaymentDepositProofsDto>.Success(await DepositProofs.ViewAsync(dbContext, loaded.Value, owner, cancellationToken));
    }
}

public sealed class GetDepositProofFileQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IPdfDocumentRenderer renderer,
    DocumentSettings settings)
    : IQueryHandler<GetDepositProofFileQuery, DepositProofFileDto>
{
    public async Task<Result<DepositProofFileDto>> Handle(GetDepositProofFileQuery request, CancellationToken cancellationToken)
    {
        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<DepositProofFileDto>.Failure(loaded.Error);

        var proof = await dbContext.DepositProofs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProofId && p.PaymentId == request.PaymentId, cancellationToken);
        if (proof is null)
            return Result<DepositProofFileDto>.Failure(DomainErrors.DepositProof.NotFound(request.ProofId));

        // Datos de demostración sin archivo: se entrega un PDF de muestra con los datos informados.
        if (proof.StorageKey is null)
        {
            var sample = renderer.Render(PortalPdfs.DepositProofSample(loaded.Value, proof, settings.IssuerFor(loaded.Value.Country)));
            return Result<DepositProofFileDto>.Success(new DepositProofFileDto(sample, ShipmentDocumentService.PdfContentType, proof.FileName));
        }

        var opened = await fileStorage.OpenReadAsync(proof.StorageKey, cancellationToken);
        if (opened.IsFailure)
            return Result<DepositProofFileDto>.Failure(opened.Error);
        if (opened.Value is null)
            return Result<DepositProofFileDto>.Failure(DomainErrors.DepositProof.ContentNotFound);

        await using var content = opened.Value;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return Result<DepositProofFileDto>.Success(new DepositProofFileDto(buffer.ToArray(), proof.ContentType, proof.FileName));
    }
}

public sealed class GetDepositProofQueueQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDepositProofQueueQuery, IReadOnlyList<DepositProofQueueItemDto>>
{
    private const int MaxRows = 500;

    public async Task<Result<IReadOnlyList<DepositProofQueueItemDto>>> Handle(GetDepositProofQueueQuery request, CancellationToken cancellationToken)
    {
        var status = request.Status ?? DepositProofStatus.Submitted;
        var proofs = await dbContext.DepositProofs.AsNoTracking()
            .Where(p => p.Status == status)
            .OrderBy(p => p.UploadedAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var paymentIds = proofs.Select(p => p.PaymentId).Distinct().ToList();
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(p => paymentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var bls = (await dbContext.PaymentDetails.AsNoTracking()
                .Where(d => paymentIds.Contains(d.PaymentId) && d.BlNumber != null)
                .Select(d => new { d.PaymentId, d.BlNumber })
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.PaymentId, d => d.BlNumber!);

        IReadOnlyList<DepositProofQueueItemDto> items = proofs
            .Where(p => payments.ContainsKey(p.PaymentId))
            .Select(p => (Proof: p, Payment: payments[p.PaymentId]))
            .Where(x => request.Country is null || x.Payment.Country == request.Country)
            .Select(x => new DepositProofQueueItemDto(
                DepositProofs.ToDto(x.Proof),
                x.Payment.PaymentNumber,
                x.Payment.SlipNumber,
                x.Payment.Status,
                x.Payment.Country,
                x.Payment.Currency,
                x.Payment.TotalAmount,
                x.Payment.PayerTaxId,
                x.Payment.PayerName,
                bls[x.Payment.Id].Distinct().Order(StringComparer.Ordinal).ToList(),
                x.Payment.CreatedAt == default ? x.Payment.PaymentDate : x.Payment.CreatedAt,
                BusinessCalendar.TimeZoneId(x.Payment.Country)))
            .ToList();

        return Result<IReadOnlyList<DepositProofQueueItemDto>>.Success(items);
    }
}

public sealed class VerifyDepositProofCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<VerifyDepositProofCommand, DepositProofReviewResultDto>
{
    public async Task<Result<DepositProofReviewResultDto>> Handle(VerifyDepositProofCommand request, CancellationToken cancellationToken)
    {
        var proof = await dbContext.DepositProofs.FirstOrDefaultAsync(p => p.Id == request.ProofId, cancellationToken);
        if (proof is null)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.DepositProof.NotFound(request.ProofId));
        if (proof.Status != DepositProofStatus.Submitted)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.DepositProof.NotPendingReview);

        var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == proof.PaymentId, cancellationToken);
        if (payment is null)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.Payment.NotFound(proof.PaymentId));

        var now = DateTime.UtcNow;
        var actor = PaymentActor.From(currentUserService);

        // Confirmación existente: comprobante, salida del carro, liberación y aviso por la cola (NF-03). La
        // referencia del abono queda como identificador de la transacción para la conciliación (NF-04).
        var confirmed = await PaymentLifecycle.ConfirmAsync(
            dbContext, payment, actor, proof.BankReference ?? $"DEP-{proof.Id:N}"[..16], now, cancellationToken);
        if (confirmed.IsFailure)
            return Result<DepositProofReviewResultDto>.Failure(confirmed.Error);

        proof.Status = DepositProofStatus.Verified;
        proof.ReviewedAt = now;
        proof.ReviewedByUserId = currentUserService.UserId;
        proof.ReviewedBy = actor.Name;
        proof.ReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        payment.DepositProofUrl = DepositProofs.ToDto(proof).DownloadPath;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DepositProofReviewResultDto>.Success(
            new DepositProofReviewResultDto(DepositProofs.ToDto(proof), await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken)));
    }
}

public sealed class RejectDepositProofCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RejectDepositProofCommand, DepositProofReviewResultDto>
{
    public async Task<Result<DepositProofReviewResultDto>> Handle(RejectDepositProofCommand request, CancellationToken cancellationToken)
    {
        var proof = await dbContext.DepositProofs.FirstOrDefaultAsync(p => p.Id == request.ProofId, cancellationToken);
        if (proof is null)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.DepositProof.NotFound(request.ProofId));
        if (proof.Status != DepositProofStatus.Submitted)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.DepositProof.NotPendingReview);

        var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == proof.PaymentId, cancellationToken);
        if (payment is null)
            return Result<DepositProofReviewResultDto>.Failure(DomainErrors.Payment.NotFound(proof.PaymentId));

        var now = DateTime.UtcNow;
        var reason = request.Reason.Trim();
        proof.Status = DepositProofStatus.Rejected;
        proof.RejectionReason = reason;
        proof.ReviewedAt = now;
        proof.ReviewedByUserId = currentUserService.UserId;
        proof.ReviewedBy = PaymentActor.From(currentUserService).Name;

        // El pago sigue en verificación esperando un comprobante válido (o la anulación de Finanzas, M5-02).
        await dbContext.SaveChangesAsync(cancellationToken);

        var title = $"Comprobante de depósito rechazado ({payment.PaymentNumber})";
        var body = $"Finanzas rechazó el comprobante del depósito del pago {payment.PaymentNumber} (boleta {payment.SlipNumber}). " +
                   $"Motivo: {reason}. Adjunte un nuevo comprobante desde el detalle del pago.";
        if (payment.CreatedByUserId is not null)
        {
            await notificationPublisher.PublishAsync(
                new NotificationRequest(NotificationTypes.DepositProofRejected, title, body,
                    UserId: payment.CreatedByUserId, DedupKey: $"deposit-proof-rejected:{proof.Id}"),
                cancellationToken);
        }

        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == payment.ClientId, cancellationToken);
        if (organization is not null)
        {
            await OrganizationNotifier.NotifyAdminsAsync(
                dbContext, notificationPublisher, organization, NotificationTypes.DepositProofRejected, title, body, cancellationToken,
                dedupKeyPrefix: $"deposit-proof-rejected:{proof.Id}");
        }

        return Result<DepositProofReviewResultDto>.Success(
            new DepositProofReviewResultDto(DepositProofs.ToDto(proof), await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken)));
    }
}
