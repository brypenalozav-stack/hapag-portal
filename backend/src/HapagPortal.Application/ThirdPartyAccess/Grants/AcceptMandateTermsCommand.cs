namespace HapagPortal.Application.ThirdPartyAccess.Grants;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// El mandante acepta los términos vigentes de un mandato pendiente, que recién entonces queda activo
/// (M1-03). La versión aceptada, la fecha y el usuario quedan en el mandato y en la auditoría (M1-23).
/// </summary>
public sealed record AcceptMandateTermsCommand(Guid Id, string TermsVersion) : ICommand<AccessGrantDto>;

/// <summary>Términos vigentes del mandato digital.</summary>
public sealed record GetMandateTermsQuery : IQuery<MandateTermsDto>;

public sealed class AcceptMandateTermsCommandValidator : AbstractValidator<AcceptMandateTermsCommand>
{
    public AcceptMandateTermsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TermsVersion).NotEmpty().MaximumLength(50);
    }
}

public sealed class AcceptMandateTermsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<AcceptMandateTermsCommand, AccessGrantDto>
{
    public async Task<Result<AccessGrantDto>> Handle(AcceptMandateTermsCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<AccessGrantDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var now = DateTime.UtcNow;

        var grant = await dbContext.AccessGrants.FirstOrDefaultAsync(
            g => g.Id == request.Id && g.GrantorClientId == context.OrganizationId && g.IsMandate, cancellationToken);
        if (grant is null)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotFound(request.Id));

        if (grant.Status != AccessGrantStatus.PendingAcceptance)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotPendingAcceptance);

        if (request.TermsVersion != MandateTerms.CurrentVersion)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.TermsVersionMismatch(MandateTerms.CurrentVersion));

        if (grant.ValidTo is not null && grant.ValidTo <= now)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotOpen);

        grant.TermsVersion = MandateTerms.CurrentVersion;
        grant.TermsAcceptedAt = now;
        grant.TermsAcceptedByUserId = context.Actor.UserId;
        grant.Status = AccessGrantStatus.Active;

        var blNumber = grant.BillOfLadingId is null
            ? null
            : await dbContext.BillsOfLading.AsNoTracking()
                .Where(b => b.Id == grant.BillOfLadingId)
                .Select(b => b.BLNumber)
                .FirstOrDefaultAsync(cancellationToken);

        AccessAudit.ForGrant(dbContext, AccessAuditEvents.MandateTermsAccepted, context.Actor, now, grant, blNumber,
            new { termsVersion = grant.TermsVersion });

        await dbContext.SaveChangesAsync(cancellationToken);

        await AccessNotifier.NotifyOrganizationsAsync(
            dbContext,
            notificationPublisher,
            [grant.GranteeClientId],
            NotificationTypes.AccessGranted,
            "Mandato digital activo",
            $"{context.Membership.Organization.Name} aceptó los términos y activó su mandato sobre {AccessNotifier.Describe(grant, blNumber)}.",
            cancellationToken);

        var dto = await AccessGrantMapper.ToDtosAsync(dbContext, context.Matrix, [grant], context.OrganizationId, now, cancellationToken);
        return Result<AccessGrantDto>.Success(dto[0]);
    }
}

public sealed class GetMandateTermsQueryHandler : IQueryHandler<GetMandateTermsQuery, MandateTermsDto>
{
    public Task<Result<MandateTermsDto>> Handle(GetMandateTermsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<MandateTermsDto>.Success(
            new MandateTermsDto(MandateTerms.CurrentVersion, MandateTerms.Title, MandateTerms.Summary)));
}
