using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Regla de plazo configurable (no hardcodeada): a partir de un evento base y un desfase en
/// horas se calcula la fecha límite. El signo del offset indica antes (-) o después (+) del evento.
/// Ver tabla de plazos de la Fase 4 del plan, con su fuente y nivel de certeza.
/// </summary>
public sealed class DeadlineRule : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string BaseEvent { get; set; }
    public int OffsetHours { get; set; }          // negativo = antes del evento; positivo = después
    public int AtRiskWindowHours { get; set; }    // margen para marcar "en riesgo" antes del vencimiento
    public string? Direction { get; set; }        // Ingreso | Salida | null (aplica a ambos)
    public string? Country { get; set; }
    public string? BLType { get; set; }           // Master | House | null
    public required string Severity { get; set; }
    public string? Source { get; set; }
    public required string Certainty { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DeadlineInstance> Instances { get; set; } = [];
}
