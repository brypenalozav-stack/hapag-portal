namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Notificaciones de accesos a terceros (M1-12, M1-14, M1-22) a los administradores de cada
/// organización afectada, por la campana y por correo. Se publican después de guardar el cambio.
/// </summary>
public static class AccessNotifier
{
    public static async Task NotifyOrganizationsAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        IEnumerable<Guid> organizationIds,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken,
        NotificationLink? link = null)
    {
        var ids = organizationIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var organizations = await dbContext.Clients
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var organization in organizations)
        {
            await OrganizationNotifier.NotifyAdminsAsync(
                dbContext, notificationPublisher, organization, type, title, body, cancellationToken,
                link: link, action: new NotificationAction(NotificationActionTypes.OpenAccessGrants, link?.EntityId ?? organization.Id.ToString()));
        }
    }

    /// <summary>
    /// Avisa a los afectados por una revocación en cadena (M1-22): los terceros que pierden el acceso
    /// derivado, quienes lo habían otorgado y los roles que pierden una ampliación.
    /// </summary>
    public static async Task NotifyCascadeAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        CascadeResult cascade,
        CancellationToken cancellationToken)
    {
        if (cascade.Grants.Count == 0 && cascade.Widenings.Count == 0)
            return;

        var blIds = cascade.Grants.Where(g => g.BillOfLadingId is not null).Select(g => g.BillOfLadingId!.Value)
            .Concat(cascade.Widenings.Select(w => w.BillOfLadingId))
            .Distinct()
            .ToList();

        var blNumbers = await dbContext.BillsOfLading
            .AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .Select(b => new { b.Id, b.BLNumber })
            .ToListAsync(cancellationToken);

        foreach (var grant in cascade.Grants)
        {
            var reference = blNumbers.FirstOrDefault(b => b.Id == grant.BillOfLadingId)?.BLNumber
                ?? grant.BookingNumber
                ?? "(sin BL)";

            await NotifyOrganizationsAsync(
                dbContext,
                notificationPublisher,
                [grant.GranteeClientId, grant.GrantorClientId],
                NotificationTypes.AccessRevokedByCascade,
                "Acceso revocado en cadena",
                $"El acceso sobre {reference} fue revocado porque se revocó o venció el acceso del que dependía.",
                cancellationToken,
                ForGrant(grant, blNumbers.FirstOrDefault(b => b.Id == grant.BillOfLadingId)?.BLNumber));
        }

        foreach (var widening in cascade.Widenings)
        {
            var reference = blNumbers.FirstOrDefault(b => b.Id == widening.BillOfLadingId)?.BLNumber ?? "(sin BL)";

            var targets = await dbContext.ShipmentRoles
                .AsNoTracking()
                .Where(r => r.BillOfLadingId == widening.BillOfLadingId && r.Role == widening.TargetRole)
                .Select(r => r.ClientId)
                .ToListAsync(cancellationToken);

            await NotifyOrganizationsAsync(
                dbContext,
                notificationPublisher,
                targets.Append(widening.GrantorClientId),
                NotificationTypes.AccessRevokedByCascade,
                "Ampliación de visibilidad revocada en cadena",
                $"Se retiró la visibilidad ampliada de '{widening.ActionCode}' sobre {reference} porque se revocó el acceso del que provenía.",
                cancellationToken,
                new NotificationLink(NotificationEntityTypes.Shipment, reference, reference, reference));
        }
    }

    /// <summary>Referencia de la bandeja (M1-25) para un acceso: el acceso y su BL o booking.</summary>
    public static NotificationLink ForGrant(AccessGrant grant, string? blNumber) =>
        new(NotificationEntityTypes.AccessGrant, grant.Id.ToString(), blNumber ?? grant.BookingNumber, blNumber);

    /// <summary>Referencia de la bandeja para un aviso sobre varios BL: el primero identifica el embarque.</summary>
    public static NotificationLink? ForReferences(IReadOnlyCollection<string> blNumbers) =>
        blNumbers.Count == 0
            ? null
            : new(NotificationEntityTypes.Shipment, blNumbers.First(), Describe(blNumbers), blNumbers.Count == 1 ? blNumbers.First() : null);

    public static string Describe(AccessGrant grant, string? blNumber) =>
        blNumber ?? grant.BookingNumber ?? "(sin BL)";

    public static string Describe(IReadOnlyCollection<string> references) =>
        references.Count <= 5
            ? string.Join(", ", references)
            : $"{string.Join(", ", references.Take(5))} y {references.Count - 5} más";
}
