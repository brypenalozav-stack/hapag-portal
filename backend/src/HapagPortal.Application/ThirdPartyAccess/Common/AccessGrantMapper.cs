namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public static class AccessGrantMapper
{
    /// <summary>Estado visible: un acceso activo con la vigencia cumplida ya figura vencido (M1-14).</summary>
    public static string DisplayStatus(AccessGrant grant, DateTime now) =>
        grant.Status == AccessGrantStatus.Active && grant.ValidTo is not null && grant.ValidTo <= now
            ? AccessGrantStatus.Expired
            : grant.Status;

    public static OrganizationRefDto ToRef(Client client) =>
        new(client.Id, client.Name, client.OrganizationType, client.Country);

    public static async Task<Dictionary<Guid, Client>> LoadOrganizationsAsync(
        IApplicationDbContext dbContext,
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        return await dbContext.Clients
            .AsNoTracking()
            .Where(c => distinct.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }

    public static async Task<List<AccessGrantDto>> ToDtosAsync(
        IApplicationDbContext dbContext,
        AccessMatrixSnapshot matrix,
        IReadOnlyCollection<AccessGrant> grants,
        Guid organizationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (grants.Count == 0)
            return [];

        var organizations = await LoadOrganizationsAsync(
            dbContext, grants.SelectMany(g => new[] { g.GrantorClientId, g.GranteeClientId }), cancellationToken);

        var blIds = grants.Where(g => g.BillOfLadingId is not null).Select(g => g.BillOfLadingId!.Value).Distinct().ToList();
        var blNumbers = await dbContext.BillsOfLading
            .AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.BLNumber, cancellationToken);

        return grants.Select(g =>
        {
            var grantor = organizations.GetValueOrDefault(g.GrantorClientId);
            var grantee = organizations.GetValueOrDefault(g.GranteeClientId);
            var explicitActions = ActionCodeList.ParseNullable(g.ActionCodes);
            var status = DisplayStatus(g, now);

            return new AccessGrantDto(
                g.Id,
                g.GrantorClientId == organizationId ? AccessGrantDirections.Given : AccessGrantDirections.Received,
                grantor is null ? Unknown(g.GrantorClientId) : ToRef(grantor),
                grantee is null ? Unknown(g.GranteeClientId) : ToRef(grantee),
                g.GrantorRole,
                g.BillOfLadingId,
                g.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(g.BillOfLadingId.Value),
                g.BookingNumber,
                g.GrantType,
                g.IntendedRole,
                explicitActions is not null,
                explicitActions,
                matrix.AllowedByGrant(
                    grantee?.OrganizationType,
                    g.IntendedRole ?? ShipmentRoleCodes.ThirdParty,
                    explicitActions,
                    ActionCodeList.Parse(g.CeilingActionCodes)),
                g.ValidityType,
                g.ValidFrom,
                g.ValidTo,
                g.DurationDays,
                status,
                g.IsEffectiveAt(now),
                g.IsMandate,
                g.TermsVersion,
                g.TermsAcceptedAt,
                g.ParentGrantId,
                g.DefaultGranteeId,
                g.CreatedAt,
                g.EndedAt,
                g.EndReason,
                g.GrantorClientId == organizationId && AccessGrantStatus.Open.Contains(status));
        }).ToList();
    }

    private static OrganizationRefDto Unknown(Guid id) => new(id, string.Empty, string.Empty, string.Empty);
}
