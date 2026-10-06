namespace HapagPortal.Application.ThirdPartyAccess.Common;

using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;

/// <summary>Quién ejecuta una decisión de acceso. Sin usuario = el sistema (vencimiento, carga de BL).</summary>
public sealed record AccessActor(Guid? UserId, string? Email, Guid? OrganizationId)
{
    public static readonly AccessActor System = new(null, "system", null);
}

/// <summary>
/// Escritura del registro append-only de auditoría de accesos (M1-23). Solo agrega entradas; las
/// entradas se guardan junto con el cambio que registran, en el mismo SaveChanges.
/// </summary>
public static class AccessAudit
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AccessAuditEntry ForGrant(
        IApplicationDbContext dbContext,
        string eventType,
        AccessActor actor,
        DateTime now,
        AccessGrant grant,
        string? blNumber,
        object? details = null) =>
        Add(dbContext, new AccessAuditEntry
        {
            OccurredAt = now,
            EventType = eventType,
            BillOfLadingId = grant.BillOfLadingId,
            BlNumber = blNumber,
            BookingNumber = grant.BookingNumber,
            AccessGrantId = grant.Id,
            GrantorClientId = grant.GrantorClientId,
            GranteeClientId = grant.GranteeClientId,
            ActorUserId = actor.UserId,
            ActorEmail = actor.Email,
            ActorClientId = actor.OrganizationId,
            Details = Serialize(details)
        });

    public static AccessAuditEntry ForWidening(
        IApplicationDbContext dbContext,
        string eventType,
        AccessActor actor,
        DateTime now,
        VisibilityWidening widening,
        string? blNumber,
        object? details = null) =>
        Add(dbContext, new AccessAuditEntry
        {
            OccurredAt = now,
            EventType = eventType,
            BillOfLadingId = widening.BillOfLadingId,
            BlNumber = blNumber,
            AccessGrantId = widening.OriginGrantId,
            VisibilityWideningId = widening.Id,
            GrantorClientId = widening.GrantorClientId,
            ActorUserId = actor.UserId,
            ActorEmail = actor.Email,
            ActorClientId = actor.OrganizationId,
            Details = Serialize(details)
        });

    /// <summary>Cambio de configuración de la organización (acceso abierto, terceros por defecto).</summary>
    public static AccessAuditEntry ForOrganization(
        IApplicationDbContext dbContext,
        string eventType,
        AccessActor actor,
        DateTime now,
        Guid organizationId,
        Guid? counterpartId = null,
        object? details = null) =>
        Add(dbContext, new AccessAuditEntry
        {
            OccurredAt = now,
            EventType = eventType,
            GrantorClientId = organizationId,
            GranteeClientId = counterpartId,
            ActorUserId = actor.UserId,
            ActorEmail = actor.Email,
            ActorClientId = actor.OrganizationId,
            Details = Serialize(details)
        });

    /// <summary>Evento sobre un BL sin acceso otorgado de por medio (autoasociación).</summary>
    public static AccessAuditEntry ForShipment(
        IApplicationDbContext dbContext,
        string eventType,
        AccessActor actor,
        DateTime now,
        BillOfLading billOfLading,
        Guid? grantorId,
        Guid? granteeId,
        object? details = null) =>
        Add(dbContext, new AccessAuditEntry
        {
            OccurredAt = now,
            EventType = eventType,
            BillOfLadingId = billOfLading.Id,
            BlNumber = billOfLading.BLNumber,
            BookingNumber = billOfLading.BookingNumber,
            GrantorClientId = grantorId,
            GranteeClientId = granteeId,
            ActorUserId = actor.UserId,
            ActorEmail = actor.Email,
            ActorClientId = actor.OrganizationId,
            Details = Serialize(details)
        });

    private static AccessAuditEntry Add(IApplicationDbContext dbContext, AccessAuditEntry entry)
    {
        dbContext.AccessAuditEntries.Add(entry);
        return entry;
    }

    private static string? Serialize(object? details) =>
        details is null ? null : JsonSerializer.Serialize(details, JsonOptions);
}
