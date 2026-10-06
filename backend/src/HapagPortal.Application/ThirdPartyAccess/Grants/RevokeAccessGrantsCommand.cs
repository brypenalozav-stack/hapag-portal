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
/// Revoca uno o varios accesos otorgados por la propia organización (M1-12, M1-22) y, en cadena, los
/// accesos y ampliaciones que dependían de ellos. Notifica a los terceros afectados, no solo a quien
/// revoca, y registra la revocación manual y la producida en cadena (M1-23).
/// </summary>
public sealed record RevokeAccessGrantsCommand(IReadOnlyList<Guid> GrantIds, string? Reason = null)
    : ICommand<RevokeAccessResultDto>;

public sealed class RevokeAccessGrantsCommandValidator : AbstractValidator<RevokeAccessGrantsCommand>
{
    public RevokeAccessGrantsCommandValidator()
    {
        RuleFor(x => x.GrantIds).NotEmpty().Must(ids => ids.Count <= 500);
        RuleForEach(x => x.GrantIds).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RevokeAccessGrantsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RevokeAccessGrantsCommand, RevokeAccessResultDto>
{
    public async Task<Result<RevokeAccessResultDto>> Handle(RevokeAccessGrantsCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<RevokeAccessResultDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var now = DateTime.UtcNow;

        // Matriz de M1-11: revocar requiere el permiso del rol con el que se otorgó.
        if (!context.Matrix.OrganizationCan(context.OrganizationType, ShipmentActionCodes.RevokeAccess))
            return Result<RevokeAccessResultDto>.Failure(DomainErrors.AccessGrant.NotAllowed);

        var ids = request.GrantIds.Distinct().ToList();
        var grants = await dbContext.AccessGrants
            .Where(g => ids.Contains(g.Id)
                && g.GrantorClientId == context.OrganizationId
                && AccessGrantStatus.Open.Contains(g.Status))
            .ToListAsync(cancellationToken);

        if (grants.Count == 0)
            return Result<RevokeAccessResultDto>.Failure(DomainErrors.AccessGrant.NotFound(ids[0]));

        var blIds = grants.Where(g => g.BillOfLadingId is not null).Select(g => g.BillOfLadingId!.Value).ToList();
        var blNumbers = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.BLNumber, cancellationToken);

        foreach (var grant in grants)
        {
            AccessGrantCascade.End(grant, context.Actor, now, AccessEndReasons.Manual);
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantRevoked, context.Actor, now, grant,
                grant.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(grant.BillOfLadingId.Value),
                new { reason = request.Reason });
        }

        var cascade = await AccessGrantCascade.RevokeDescendantsAsync(dbContext, grants, context.Actor, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var byGrantee in grants.GroupBy(g => g.GranteeClientId))
        {
            var references = byGrantee
                .Select(g => AccessNotifier.Describe(g, g.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(g.BillOfLadingId.Value)))
                .ToList();

            await AccessNotifier.NotifyOrganizationsAsync(
                dbContext,
                notificationPublisher,
                [byGrantee.Key],
                NotificationTypes.AccessRevoked,
                "Acceso revocado",
                $"{context.Membership.Organization.Name} revocó su acceso a {AccessNotifier.Describe(references)}.",
                cancellationToken);
        }

        await AccessNotifier.NotifyCascadeAsync(dbContext, notificationPublisher, cascade, cancellationToken);

        return Result<RevokeAccessResultDto>.Success(
            new RevokeAccessResultDto(grants.Count, cascade.Grants.Count, cascade.Widenings.Count));
    }
}
