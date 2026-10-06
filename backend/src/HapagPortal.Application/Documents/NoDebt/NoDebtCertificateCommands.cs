namespace HapagPortal.Application.Documents.NoDebt;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Verificación previa del certificado de libre deuda (M6-07): motivos que lo bloquean hoy.</summary>
public sealed record GetNoDebtEligibilityQuery(string BlNumber) : IQuery<NoDebtEligibilityDto>;

/// <summary>
/// Solicitud del certificado de libre deuda (M6-07, BO-IMP-15), importación de Bolivia: sin deuda pendiente
/// del embarque y con las demoras anticipadas pagadas cuando la regla interna las exige (M3-16), genera el
/// PDF firmado y lo publica en el repositorio (M6-09); con deuda, el error enumera los motivos.
/// </summary>
public sealed record RequestNoDebtCertificateCommand(string BlNumber) : ICommand<ShipmentDocumentDto>;

public sealed class GetNoDebtEligibilityQueryValidator : AbstractValidator<GetNoDebtEligibilityQuery>
{
    public GetNoDebtEligibilityQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestNoDebtCertificateCommandValidator : AbstractValidator<RequestNoDebtCertificateCommand>
{
    public RequestNoDebtCertificateCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetNoDebtEligibilityQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    NoDebtEvaluator evaluator)
    : IQueryHandler<GetNoDebtEligibilityQuery, NoDebtEligibilityDto>
{
    public async Task<Result<NoDebtEligibilityDto>> Handle(GetNoDebtEligibilityQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<NoDebtEligibilityDto>.Failure(loaded.Error);

        var (bl, permissions, _, scope) = loaded.Value;
        if (!permissions.Can(ShipmentActionCodes.DownloadNoDebtCertificate))
            return Result<NoDebtEligibilityDto>.Failure(Error.Forbidden);

        var now = DateTime.UtcNow;
        var applicable = NoDebtEvaluator.IsApplicable(bl);
        IReadOnlyList<NoDebtBlockerDto> blockers = [];
        if (applicable)
        {
            // La deuda es del embarque; las condiciones de crédito, de la cuenta titular del BL.
            var account = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == bl.ClientId, cancellationToken);
            blockers = await evaluator.EvaluateAsync(bl, account, now, cancellationToken);
        }

        return Result<NoDebtEligibilityDto>.Success(new NoDebtEligibilityDto(
            bl.Id,
            bl.BLNumber,
            bl.Country,
            applicable,
            Eligible: applicable && blockers.Count == 0,
            CanRequest: applicable && !scope.IsAdmin && permissions.CanExecute(ShipmentActionCodes.DownloadNoDebtCertificate),
            blockers,
            now));
    }
}

public sealed class RequestNoDebtCertificateCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    NoDebtEvaluator evaluator,
    ShipmentDocumentService documents)
    : ICommandHandler<RequestNoDebtCertificateCommand, ShipmentDocumentDto>
{
    public async Task<Result<ShipmentDocumentDto>> Handle(RequestNoDebtCertificateCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentDocumentDto>.Failure(loaded.Error);

        var (bl, permissions, organization, scope) = loaded.Value;

        if (!NoDebtEvaluator.IsApplicable(bl))
            return Result<ShipmentDocumentDto>.Failure(DomainErrors.NoDebtCertificate.NotApplicable);
        if (!permissions.CanExecute(ShipmentActionCodes.DownloadNoDebtCertificate))
            return Result<ShipmentDocumentDto>.Failure(Error.Forbidden);

        var now = DateTime.UtcNow;
        var account = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == bl.ClientId, cancellationToken);
        var blockers = await evaluator.EvaluateAsync(bl, account, now, cancellationToken);
        if (blockers.Count > 0)
            return Result<ShipmentDocumentDto>.Failure(DomainErrors.NoDebtCertificate.DebtPending(NoDebtEvaluator.Describe(blockers)));

        var actor = DocumentActors.From(currentUserService, scope, permissions.GrantFor(ShipmentActionCodes.DownloadNoDebtCertificate));
        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(ShipmentDocumentTypes.NoDebtCertificate, bl, now);

        var issued = await documents.IssueAsync(
            new DocumentIssue(
                ShipmentDocumentTypes.NoDebtCertificate,
                bl,
                ShipmentDocumentTemplates.NoDebtCertificate(data, header, organization.Name, TaxIdNormalizer.Normalize(organization.TaxId)),
                ShipmentDocumentOrigins.Request,
                actor,
                organization.Id,
                data.Containers.Select(c => c.ContainerNumber).ToList()),
            cancellationToken);
        if (issued.IsFailure)
            return Result<ShipmentDocumentDto>.Failure(issued.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ShipmentDocumentDto>.Success(ShipmentDocumentService.ToDto(issued.Value, organization.Name));
    }
}
