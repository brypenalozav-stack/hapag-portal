namespace HapagPortal.Application.ThirdPartyAccess.Grants;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Registra el vencimiento de los accesos y mandatos cuya vigencia se cumplió (M1-14, M1-03, NF-06):
/// pasan a Expired sin intervención manual, sus derivados se revocan en cadena (M1-22) y se avisa al
/// tercero y al otorgante. Idempotente. El evaluador ya los deniega por reloj; esto deja el registro.
/// Devuelve la cantidad de accesos vencidos.
/// </summary>
public sealed record ExpireAccessGrantsCommand : ICommand<int>;

public sealed class ExpireAccessGrantsCommandHandler(
    IApplicationDbContext dbContext,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<ExpireAccessGrantsCommand, int>
{
    private const int BatchSize = 500;

    public async Task<Result<int>> Handle(ExpireAccessGrantsCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var actor = AccessActor.System;

        var expired = await dbContext.AccessGrants
            .Where(g => AccessGrantStatus.Open.Contains(g.Status) && g.ValidTo != null && g.ValidTo <= now)
            .OrderBy(g => g.ValidTo)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return Result<int>.Success(0);

        var blIds = expired.Where(g => g.BillOfLadingId is not null).Select(g => g.BillOfLadingId!.Value).Distinct().ToList();
        var blNumbers = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.BLNumber, cancellationToken);

        foreach (var grant in expired)
        {
            AccessGrantCascade.End(grant, actor, now, AccessEndReasons.Expired);
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantExpired, actor, now, grant,
                grant.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(grant.BillOfLadingId.Value),
                new { validTo = grant.ValidTo, isMandate = grant.IsMandate });
        }

        var cascade = await AccessGrantCascade.RevokeDescendantsAsync(dbContext, expired, actor, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var grant in expired)
        {
            var reference = AccessNotifier.Describe(
                grant, grant.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(grant.BillOfLadingId.Value));

            await AccessNotifier.NotifyOrganizationsAsync(
                dbContext,
                notificationPublisher,
                [grant.GranteeClientId, grant.GrantorClientId],
                NotificationTypes.AccessExpired,
                grant.IsMandate ? "Mandato vencido" : "Acceso vencido",
                $"El acceso sobre {reference} venció el {grant.ValidTo:yyyy-MM-dd HH:mm} UTC y dejó de habilitar operaciones.",
                cancellationToken);
        }

        await AccessNotifier.NotifyCascadeAsync(dbContext, notificationPublisher, cascade, cancellationToken);

        return Result<int>.Success(expired.Count);
    }
}
