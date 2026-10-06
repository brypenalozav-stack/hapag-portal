using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Exención aplicada a un cargo del embarque (M4-02): concepto, figura exenta y la condición informada
/// por Nexus (vigencia, monto), para que el concepto exento quede reflejado y sea trazable.
/// </summary>
public sealed class AppliedExemption : GuidEntity
{
    public Guid BillOfLadingId { get; set; }
    public Guid? LocalChargeId { get; set; }
    public required string ConceptCode { get; set; }
    public required string ExemptParty { get; set; }
    public required string PartyTaxId { get; set; }
    public string? PartyMatchCode { get; set; }
    public decimal ExemptAmount { get; set; }
    public required string Currency { get; set; }
    public decimal? ConditionAmount { get; set; }
    public string? ConditionCurrency { get; set; }
    public DateOnly ConditionValidFrom { get; set; }
    public DateOnly? ConditionValidTo { get; set; }
    public required string Source { get; set; }
    public Guid? PayerClientId { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public DateTime AppliedAt { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
