using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Registro append-only de un documento del embarque (NF-14): emisión, cada descarga y cada envío, con el
/// usuario, la organización en cuyo nombre actuó (y el mandante, si fue bajo un acceso otorgado), el canal
/// (portal, correo, asistente de M10-04) y la fecha. Nunca se modifica ni se elimina (NF-16).
/// </summary>
public sealed class ShipmentDocumentEvent : GuidEntity
{
    public Guid ShipmentDocumentId { get; set; }
    public required string EventType { get; set; }
    public required string Channel { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>Usuario que actuó; nulo = el sistema (emisión automática tras el pago).</summary>
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OnBehalfOfOrganizationId { get; set; }

    /// <summary>Correo de destino de un envío.</summary>
    public string? Recipient { get; set; }
    public string? Details { get; set; }

    public ShipmentDocument ShipmentDocument { get; set; } = null!;
}
