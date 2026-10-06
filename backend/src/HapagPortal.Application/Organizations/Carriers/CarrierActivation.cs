namespace HapagPortal.Application.Organizations.Carriers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Primer ingreso de un transportista pre-creado (M1-09): su organización queda aprobada y vinculada a lo que los
/// clientes le asignaron; los accesos pendientes pasan a <c>Active</c> con su vigencia contada desde ahora, y se avisa a
/// los clientes que lo pre-crearon. Todo queda en la auditoría de accesos (M1-23). No guarda: lo hace el llamador
/// (el login, en el mismo guardado del ingreso).
/// </summary>
public static class CarrierActivation
{
    public static async Task<bool> ActivateOnFirstLoginAsync(
        IApplicationDbContext dbContext,
        User user,
        Client organization,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (organization.RegistrationStatus != OrganizationStatus.PreCreated || organization.OrganizationType != OrganizationTypes.Carrier)
            return false;

        var actor = new AccessActor(user.Id, user.Email, organization.Id);
        organization.RegistrationStatus = OrganizationStatus.Approved;
        organization.ApprovedAt = now;
        organization.ReviewNotes = $"{organization.ReviewNotes} Activado en el primer ingreso de {user.Email}.".Trim();

        var registrations = await dbContext.CarrierPreRegistrations
            .Where(r => r.CarrierOrganizationId == organization.Id && r.Status == CarrierPreRegistrationStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var registration in registrations)
        {
            registration.Status = CarrierPreRegistrationStatus.Activated;
            registration.ActivatedAt = now;
            AccessAudit.ForOrganization(dbContext, AccessAuditEvents.CarrierActivated, actor, now,
                registration.RequestedByOrganizationId, organization.Id, new { preRegistrationId = registration.Id });
        }

        var grants = await dbContext.AccessGrants
            .Where(g => g.GranteeClientId == organization.Id && g.Status == AccessGrantStatus.PendingActivation)
            .ToListAsync(cancellationToken);
        var blIds = grants.Where(g => g.BillOfLadingId is not null).Select(g => g.BillOfLadingId!.Value).ToList();
        var blNumbers = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.BLNumber, cancellationToken);

        foreach (var grant in grants)
        {
            grant.Status = AccessGrantStatus.Active;
            grant.ValidFrom = now;
            grant.ValidTo = grant.DurationDays is { } days ? now.AddDays(days) : null;
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantActivated, actor, now, grant,
                grant.BillOfLadingId is null ? null : blNumbers.GetValueOrDefault(grant.BillOfLadingId.Value),
                new { validFrom = grant.ValidFrom, validTo = grant.ValidTo });
        }

        return true;
    }

    /// <summary>Aviso a los administradores de cada cliente que pre-creó al transportista (después de guardar).</summary>
    public static async Task NotifyRequestersAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Client carrier,
        CancellationToken cancellationToken)
    {
        var requesterIds = await dbContext.CarrierPreRegistrations.AsNoTracking()
            .Where(r => r.CarrierOrganizationId == carrier.Id)
            .Select(r => r.RequestedByOrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var requesters = await dbContext.Clients.AsNoTracking().Where(c => requesterIds.Contains(c.Id)).ToListAsync(cancellationToken);

        foreach (var requester in requesters)
        {
            await OrganizationNotifier.NotifyAdminsAsync(
                dbContext, notificationPublisher, requester, NotificationTypes.CarrierActivated,
                $"Transportista activo: {carrier.Name}",
                $"{carrier.Name} ingresó al portal por primera vez y ya ve los BL y bookings que le asignó.",
                cancellationToken,
                dedupKeyPrefix: $"carrier-activated:{carrier.Id}",
                link: new NotificationLink(NotificationEntityTypes.Organization, carrier.Id.ToString(), carrier.Name),
                action: new NotificationAction(NotificationActionTypes.OpenAccessGrants, carrier.Id.ToString()));
        }
    }
}
