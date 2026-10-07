namespace HapagPortal.Application.Tariffs.Common;

using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Entities;

/// <summary>Tramo de tarifa (entrada y salida de la API).</summary>
public sealed record TariffTierDto(int FromUnit, int? ToUnit, decimal Amount);

/// <summary>Tarifa del mantenedor (M8-01).</summary>
public sealed record TariffDto(
    Guid Id,
    string ConceptCode,
    string? ConceptName,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string TierUnit,
    string TierMode,
    IReadOnlyList<TariffTierDto> Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>
/// Instantánea de una tarifa guardada en el registro de cambios (NF-15). Permite reconstruir el
/// mantenedor tal como estaba en una fecha.
/// </summary>
public sealed record TariffSnapshot(
    string ConceptCode,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string TierUnit,
    string TierMode,
    IReadOnlyList<TariffTierDto> Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive)
{
    public static TariffSnapshot From(Tariff tariff) => new(
        tariff.ConceptCode,
        tariff.Code,
        tariff.Country,
        tariff.Currency,
        tariff.ContainerType,
        tariff.Description,
        tariff.Amount,
        tariff.TierUnit,
        tariff.TierMode,
        tariff.Tiers.OrderBy(t => t.FromUnit).Select(t => new TariffTierDto(t.FromUnit, t.ToUnit, t.Amount)).ToList(),
        tariff.ValidFrom,
        tariff.ValidTo,
        tariff.IsActive);
}

/// <summary>Un cambio del mantenedor de tarifas: usuario, fecha, valor anterior y nuevo (NF-15).</summary>
public sealed record TariffChangeDto(
    Guid Id,
    Guid TariffId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    TariffSnapshot? Previous,
    TariffSnapshot? Current);

/// <summary>
/// Tarifa vigente resuelta (portal o Nexus) con su aplicación opcional a una medida: unidades
/// indicadas o tiempo transcurrido desde un hito, medido en UTC con el calendario del país (NF-22).
/// </summary>
public sealed record TariffInForceDto(
    Guid? TariffId,
    string Concept,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string TierUnit,
    string TierMode,
    IReadOnlyList<TariffTierDto> Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string Source,
    DateOnly LocalDate,
    string TimeZone,
    int? MeasuredUnits,
    decimal? ComputedAmount,
    bool? Covered,
    IReadOnlyList<TariffBreakdownLine> Breakdown);

/// <summary>Concepto del catálogo de cobros.</summary>
public sealed record ChargeConceptDto(
    string Code,
    string Name,
    string Category,
    IReadOnlyList<string> Countries,
    bool NexusTariff,
    bool NexusExemptible,
    int DisplayOrder);

public static class TariffMapper
{
    public static TariffDto ToDto(Tariff tariff, string? conceptName) => new(
        tariff.Id,
        tariff.ConceptCode,
        conceptName,
        tariff.Code,
        tariff.Country,
        tariff.Currency,
        tariff.ContainerType,
        tariff.Description,
        tariff.Amount,
        tariff.TierUnit,
        tariff.TierMode,
        tariff.Tiers.OrderBy(t => t.FromUnit).Select(t => new TariffTierDto(t.FromUnit, t.ToUnit, t.Amount)).ToList(),
        tariff.ValidFrom,
        tariff.ValidTo,
        tariff.IsActive,
        tariff.CreatedAt,
        tariff.CreatedBy,
        tariff.ModifiedAt,
        tariff.ModifiedBy);

    public static IReadOnlyList<TierDefinition> ToDefinitions(IEnumerable<TariffTierDto> tiers) =>
        tiers.Select(t => new TierDefinition(t.FromUnit, t.ToUnit, t.Amount)).ToList();
}
