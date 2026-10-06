using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Regla interna del portal para una cuenta o razón social, identificada por RUT/NIT o Match Code
/// (M3-04 cambio de almacén gratuito, M3-16 demoras anticipadas de Bolivia). Las condiciones que
/// administra Nexus (crédito, FFWW, exenciones) no se replican aquí (M8-02).
/// </summary>
public sealed class InternalChargeRule : BaseAuditableEntity
{
    public required string RuleType { get; set; }
    public required string Country { get; set; }
    public string? TaxId { get; set; }
    public string? MatchCode { get; set; }
    public string? AccountName { get; set; }
    public string? Reason { get; set; }

    /// <summary>Cambios gratuitos por BL (M3-04). Nulo = sin límite.</summary>
    public int? MaxUsesPerBl { get; set; }

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
