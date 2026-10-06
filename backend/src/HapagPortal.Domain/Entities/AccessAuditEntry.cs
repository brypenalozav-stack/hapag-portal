using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Registro append-only de las decisiones de acceso (M1-23, NF-14): otorgamientos, cambios de
/// vigencia o permisos, revocaciones manuales, automáticas y en cadena, acceso abierto,
/// autoasociación, ampliaciones y reconciliaciones. Nunca se modifica ni se elimina.
/// <see cref="ActorUserId"/> nulo indica una acción del sistema (vencimiento, carga de BL).
/// </summary>
public sealed class AccessAuditEntry : GuidEntity
{
    public DateTime OccurredAt { get; set; }
    public required string EventType { get; set; }

    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public Guid? AccessGrantId { get; set; }
    public Guid? VisibilityWideningId { get; set; }

    /// <summary>Organización que otorga o cuya configuración cambia.</summary>
    public Guid? GrantorClientId { get; set; }

    /// <summary>Organización que recibe el acceso (o que se autoasocia).</summary>
    public Guid? GranteeClientId { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public Guid? ActorClientId { get; set; }

    /// <summary>Detalle del cambio en JSON (valores anteriores y nuevos, motivo).</summary>
    public string? Details { get; set; }
}
