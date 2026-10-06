using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Mantenedor local heredado de exenciones. No es fuente de verdad (M4-01, M8-02): las exenciones
/// vigentes se leen de Nexus mediante <c>IExemptionReader</c> y ninguna regla de cobro consulta esta
/// tabla. Se conserva solo como referencia histórica hasta retirar su pantalla.
/// </summary>
public sealed class DemurrageExemption : BaseAuditableEntity
{
    public required string ClientName { get; set; }
    public required string TaxId { get; set; }
    public required string Country { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
}
