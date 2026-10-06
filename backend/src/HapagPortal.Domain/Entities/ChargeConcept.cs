using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Concepto del catálogo de cobros (Fase 1, Ola C). <see cref="NexusTariff"/> indica que Nexus publica
/// la tarifa base (CT-NEXUS <c>GET /tariffs</c>); <see cref="NexusExemptible"/>, que Nexus administra su
/// exención (M4-01). <see cref="Countries"/> es una lista separada por coma (CL, BO).
/// </summary>
public sealed class ChargeConcept : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string Countries { get; set; } = "CL,BO";
    public bool NexusTariff { get; set; }
    public bool NexusExemptible { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
