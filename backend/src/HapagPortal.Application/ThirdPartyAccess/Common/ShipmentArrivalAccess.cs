namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Rol de una organización sobre un BL recién incorporado al portal.</summary>
public sealed record ShipmentParty(Guid OrganizationId, string Role);

/// <summary>
/// Accesos que se resuelven cuando un BL o booking nuevo llega al portal con sus roles: los terceros
/// por defecto de cada parte (M1-13) y la reconciliación de los accesos anticipados por booking con
/// el rol oficial (M1-20). Agrega los cambios al contexto; quien llama guarda.
/// </summary>
public static class ShipmentArrivalAccess
{
    public static async Task<int> ApplyAsync(
        IApplicationDbContext dbContext,
        AccessMatrixSnapshot matrix,
        BillOfLading billOfLading,
        IReadOnlyCollection<ShipmentParty> parties,
        AccessActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var created = await ApplyDefaultsAsync(dbContext, matrix, billOfLading, parties, actor, now, cancellationToken);
        await ReconcileBookingAccessAsync(dbContext, billOfLading, parties, actor, now, cancellationToken);
        return created;
    }

    /// <summary>
    /// Crea un acceso por cada tercero por defecto de cada parte del BL (M1-13), con los permisos y la
    /// duración configurados, recortados a lo que esa parte posee sobre el BL (nunca más, M1-11).
    /// </summary>
    public static async Task<int> ApplyDefaultsAsync(
        IApplicationDbContext dbContext,
        AccessMatrixSnapshot matrix,
        BillOfLading billOfLading,
        IReadOnlyCollection<ShipmentParty> parties,
        AccessActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var partyIds = parties.Select(p => p.OrganizationId).Distinct().ToList();
        if (partyIds.Count == 0)
            return 0;

        var defaults = await dbContext.DefaultGrantees
            .AsNoTracking()
            .Where(d => d.IsActive && partyIds.Contains(d.GrantorClientId))
            .ToListAsync(cancellationToken);

        if (defaults.Count == 0)
            return 0;

        var organizationIds = partyIds.Concat(defaults.Select(d => d.GranteeClientId)).Distinct().ToList();
        var organizations = await dbContext.Clients
            .AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var shipmentScoped = matrix.ShipmentScopedActions.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
        var created = 0;

        foreach (var configured in defaults)
        {
            var grantor = organizations.GetValueOrDefault(configured.GrantorClientId);
            var grantee = organizations.GetValueOrDefault(configured.GranteeClientId);
            if (grantor is null || GrantRules.CheckGrantee(grantee, configured.GranteeClientId, configured.GrantorClientId) is not null)
                continue;

            var grantorRoles = parties
                .Where(p => p.OrganizationId == grantor.Id)
                .Select(p => p.Role)
                .Distinct()
                .ToList();

            var grantorActions = matrix.AllowedForRoles(grantor.OrganizationType, grantorRoles);
            if (!grantorActions.Contains(ShipmentActionCodes.GrantAccess))
                continue;

            var ceiling = grantorActions.Where(shipmentScoped.Contains).ToList();
            var grantable = matrix.GrantableTo(grantee!.OrganizationType, ShipmentRoleCodes.ThirdParty);
            var explicitActions = ActionCodeList.ParseNullable(configured.ActionCodes)?
                .Where(c => ceiling.Contains(c) && grantable.Contains(c))
                .ToList();

            var grant = new AccessGrant
            {
                GrantorClientId = grantor.Id,
                GrantorRole = ShipmentRoleCodes.MatrixColumns.First(grantorRoles.Contains),
                GranteeClientId = grantee.Id,
                BillOfLadingId = billOfLading.Id,
                BookingNumber = billOfLading.BookingNumber,
                GrantType = AccessGrantTypes.Default,
                DefaultGranteeId = configured.Id,
                ActionCodes = explicitActions is null ? null : ActionCodeList.Format(explicitActions),
                CeilingActionCodes = ActionCodeList.Format(ceiling),
                ValidityType = configured.DurationDays is null ? AccessValidityTypes.Indefinite : AccessValidityTypes.Duration,
                ValidFrom = now,
                ValidTo = configured.DurationDays is null ? null : now.AddDays(configured.DurationDays.Value),
                DurationDays = configured.DurationDays,
                Status = AccessGrantStatus.Active
            };

            dbContext.AccessGrants.Add(grant);
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantCreated, actor, now, grant, billOfLading.BLNumber, new
            {
                grantType = AccessGrantTypes.Default,
                defaultGranteeId = configured.Id,
                actionCodes = explicitActions,
                validTo = grant.ValidTo
            });
            created++;
        }

        return created;
    }

    /// <summary>
    /// Vincula al BL los accesos anticipados que su customer otorgó por el número de booking y los
    /// reconcilia con el rol oficial (M1-20): si el tercero quedó con el rol asignado, el acceso se da
    /// por reconciliado (sigue viendo el BL por su rol); si no, se mantiene vigente. Sin pérdida de
    /// visibilidad y con registro en M1-23.
    /// </summary>
    public static async Task ReconcileBookingAccessAsync(
        IApplicationDbContext dbContext,
        BillOfLading billOfLading,
        IReadOnlyCollection<ShipmentParty> parties,
        AccessActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(billOfLading.BookingNumber))
            return;

        var customers = parties
            .Where(p => p.Role == ShipmentRoleCodes.Customer)
            .Select(p => p.OrganizationId)
            .Append(billOfLading.ClientId)
            .ToHashSet();

        var booking = billOfLading.BookingNumber;
        var grants = await dbContext.AccessGrants
            .Where(g => g.GrantType == AccessGrantTypes.EarlyBooking
                && g.BookingNumber == booking
                && AccessGrantStatus.Open.Contains(g.Status)
                && (g.BillOfLadingId == null || g.BillOfLadingId == billOfLading.Id))
            .ToListAsync(cancellationToken);

        foreach (var grant in grants.Where(g => customers.Contains(g.GrantorClientId)))
        {
            if (grant.BillOfLadingId is null)
            {
                grant.BillOfLadingId = billOfLading.Id;
                AccessAudit.ForGrant(dbContext, AccessAuditEvents.BookingAccessLinked, actor, now, grant, billOfLading.BLNumber);
            }

            var officialRoles = parties
                .Where(p => p.OrganizationId == grant.GranteeClientId)
                .Select(p => p.Role)
                .Distinct()
                .ToList();

            if (officialRoles.Count == 0)
                continue;

            var replaced = grant.IntendedRole is not null && officialRoles.Contains(grant.IntendedRole);
            if (replaced)
                AccessGrantCascade.End(grant, actor, now, AccessEndReasons.Reconciled);

            AccessAudit.ForGrant(dbContext, AccessAuditEvents.BookingAccessReconciled, actor, now, grant, billOfLading.BLNumber, new
            {
                intendedRole = grant.IntendedRole,
                officialRoles,
                outcome = replaced ? "ReplacedByOfficialRole" : "Kept"
            });
        }
    }
}
