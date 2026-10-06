namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Accesos y ampliaciones terminados en cadena, para notificar a los afectados.</summary>
public sealed record CascadeResult(IReadOnlyList<AccessGrant> Grants, IReadOnlyList<VisibilityWidening> Widenings)
{
    public static readonly CascadeResult Empty = new([], []);
}

/// <summary>
/// Revocación y ajuste en cadena (M1-22): cada acceso conoce su origen (<see cref="AccessGrant.ParentGrantId"/>)
/// y cada ampliación el acceso del que proviene (<see cref="VisibilityWidening.OriginGrantId"/>); terminar
/// o reducir un origen alcanza a todo lo que de él se derivó. Todo queda en la auditoría de M1-23.
/// </summary>
public static class AccessGrantCascade
{
    /// <summary>Revoca todos los accesos y ampliaciones derivados de <paramref name="roots"/>.</summary>
    public static async Task<CascadeResult> RevokeDescendantsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<AccessGrant> roots,
        AccessActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var revokedGrants = new List<AccessGrant>();
        var revokedWidenings = new List<VisibilityWidening>();
        var seen = roots.Select(r => r.Id).ToHashSet();
        var frontier = seen.ToList();

        while (frontier.Count > 0)
        {
            var widenings = await dbContext.VisibilityWidenings
                .Where(w => w.OriginGrantId != null
                    && frontier.Contains(w.OriginGrantId.Value)
                    && w.Status == VisibilityWideningStatus.Active)
                .ToListAsync(cancellationToken);

            var children = (await dbContext.AccessGrants
                    .Where(g => g.ParentGrantId != null
                        && frontier.Contains(g.ParentGrantId.Value)
                        && AccessGrantStatus.Open.Contains(g.Status))
                    .ToListAsync(cancellationToken))
                .Where(g => seen.Add(g.Id))
                .ToList();

            var blNumbers = await BlNumbersAsync(
                dbContext,
                widenings.Select(w => (Guid?)w.BillOfLadingId).Concat(children.Select(c => c.BillOfLadingId)),
                cancellationToken);

            foreach (var widening in widenings)
            {
                End(widening, actor, now, AccessEndReasons.Cascade);
                AccessAudit.ForWidening(dbContext, AccessAuditEvents.WideningRevokedByCascade, actor, now, widening,
                    blNumbers.GetValueOrDefault(widening.BillOfLadingId),
                    new { originGrantId = widening.OriginGrantId });
                revokedWidenings.Add(widening);
            }

            foreach (var child in children)
            {
                End(child, actor, now, AccessEndReasons.Cascade);
                AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantRevokedByCascade, actor, now, child,
                    child.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(child.BillOfLadingId.Value),
                    new { parentGrantId = child.ParentGrantId });
                revokedGrants.Add(child);
            }

            frontier = children.Select(c => c.Id).ToList();
        }

        return new CascadeResult(revokedGrants, revokedWidenings);
    }

    /// <summary>
    /// Ajusta los derivados de <paramref name="parent"/> a su nueva vigencia y a sus nuevas acciones:
    /// un derivado nunca dura más ni habilita más que su origen. Las ampliaciones que dependían de una
    /// acción retirada se revocan en cadena.
    /// </summary>
    public static async Task<CascadeResult> ClampDescendantsAsync(
        IApplicationDbContext dbContext,
        AccessGrant parent,
        IReadOnlyCollection<string> parentActions,
        AccessActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var revokedWidenings = new List<VisibilityWidening>();

        var widenings = await dbContext.VisibilityWidenings
            .Where(w => w.OriginGrantId == parent.Id && w.Status == VisibilityWideningStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var widening in widenings.Where(w => !parentActions.Contains(w.ActionCode)))
        {
            End(widening, actor, now, AccessEndReasons.Cascade);
            AccessAudit.ForWidening(dbContext, AccessAuditEvents.WideningRevokedByCascade, actor, now, widening, null,
                new { originGrantId = parent.Id, reason = "OriginPermissionsReduced" });
            revokedWidenings.Add(widening);
        }

        var children = await dbContext.AccessGrants
            .Where(g => g.ParentGrantId == parent.Id && AccessGrantStatus.Open.Contains(g.Status))
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            if (parent.ValidTo is not null && (child.ValidTo is null || child.ValidTo > parent.ValidTo))
            {
                var previous = child.ValidTo;
                child.ValidTo = parent.ValidTo;
                child.ValidityType = AccessValidityTypes.UntilDate;
                child.DurationDays = null;
                AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantValidityChanged, actor, now, child, null,
                    new { previousValidTo = previous, validTo = child.ValidTo, reason = "ClampedToOrigin", originGrantId = parent.Id });
            }

            var ceiling = ActionCodeList.Parse(child.CeilingActionCodes);
            var clampedCeiling = ceiling.Where(parentActions.Contains).ToList();
            var explicitActions = ActionCodeList.ParseNullable(child.ActionCodes);
            var clampedExplicit = explicitActions?.Where(parentActions.Contains).ToList();

            if (clampedCeiling.Count != ceiling.Count || clampedExplicit?.Count != explicitActions?.Count)
            {
                child.CeilingActionCodes = ActionCodeList.Format(clampedCeiling);
                if (clampedExplicit is not null)
                    child.ActionCodes = ActionCodeList.Format(clampedExplicit);

                AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantPermissionsChanged, actor, now, child, null,
                    new { removed = ceiling.Except(clampedCeiling).ToList(), reason = "ClampedToOrigin", originGrantId = parent.Id });
            }

            var nested = await ClampDescendantsAsync(
                dbContext, child, (IReadOnlyCollection<string>?)clampedExplicit ?? clampedCeiling, actor, now, cancellationToken);
            revokedWidenings.AddRange(nested.Widenings);
        }

        return new CascadeResult([], revokedWidenings);
    }

    public static void End(AccessGrant grant, AccessActor actor, DateTime now, string reason)
    {
        grant.Status = reason switch
        {
            AccessEndReasons.Expired => AccessGrantStatus.Expired,
            AccessEndReasons.Reconciled => AccessGrantStatus.Reconciled,
            _ => AccessGrantStatus.Revoked
        };
        grant.EndedAt = now;
        grant.EndedByUserId = actor.UserId;
        grant.EndReason = reason;
    }

    public static void End(VisibilityWidening widening, AccessActor actor, DateTime now, string reason)
    {
        widening.Status = VisibilityWideningStatus.Revoked;
        widening.EndedAt = now;
        widening.EndedByUserId = actor.UserId;
        widening.EndReason = reason;
    }

    private static async Task<Dictionary<Guid, string>> BlNumbersAsync(
        IApplicationDbContext dbContext,
        IEnumerable<Guid?> ids,
        CancellationToken cancellationToken)
    {
        var distinct = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        if (distinct.Count == 0)
            return [];

        return await dbContext.BillsOfLading
            .AsNoTracking()
            .Where(b => distinct.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.BLNumber, cancellationToken);
    }
}
