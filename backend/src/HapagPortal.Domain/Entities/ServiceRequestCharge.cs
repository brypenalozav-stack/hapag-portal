using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Cargo local que cobra una solicitud: el generado con la tarifa del mantenedor o el cargo del sistema de
/// origen vinculado (Gate In, M3-15). Cuando la liberación del pago lo marca pagado, la solicitud avanza.
/// </summary>
public sealed class ServiceRequestCharge : GuidEntity
{
    public Guid ServiceRequestId { get; set; }
    public Guid LocalChargeId { get; set; }

    /// <summary>Verdadero si el cargo lo creó la solicitud (se elimina si se anula antes de pagarlo).</summary>
    public bool Generated { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}
