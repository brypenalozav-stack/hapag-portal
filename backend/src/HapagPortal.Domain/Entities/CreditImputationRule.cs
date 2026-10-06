using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Concepto que un cliente con crédito vigente puede imputar a su línea de crédito en lugar de pagarlo en el
/// momento (M5-10). La ficha exige que Finanzas confirme los conceptos elegibles: este mantenedor interno los
/// administra (NF-15) y se siembra en forma conservadora (solo recargos locales de Chile). Un ítem es elegible
/// si su concepto tiene una regla habilitada en el país y el concepto de crédito de Nexus asociado
/// (<see cref="NexusCreditConcept"/>) figura en la condición de crédito del cliente (M8-02).
/// </summary>
public sealed class CreditImputationRule : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string ConceptCode { get; set; }

    /// <summary><c>CreditCoverageConcepts</c>: LOCAL_CHARGES, MHD, FREIGHT o STORAGE.</summary>
    public required string NexusCreditConcept { get; set; }

    public bool IsEnabled { get; set; } = true;
    public string? Notes { get; set; }
}
