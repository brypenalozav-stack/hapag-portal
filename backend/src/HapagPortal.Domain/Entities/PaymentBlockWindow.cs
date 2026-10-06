using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Ventana programada de bloqueo de pagos (M8-07). Fechas y horas en el huso del país (NF-22); sin país,
/// aplica a todos, cada uno en su hora local. Se activa y desactiva sola: se evalúa en cada solicitud.
/// </summary>
public sealed class PaymentBlockWindow : BaseAuditableEntity
{
    public string? Country { get; set; }
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly EndTime { get; set; }
    public required string Reason { get; set; }
    public required string ClientMessage { get; set; }
    public bool IsActive { get; set; } = true;
}
