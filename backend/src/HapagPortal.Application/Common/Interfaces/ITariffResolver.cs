namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Domain.Charges;

/// <summary>
/// Tarifa vigente de un concepto (M8-01, CT-NEXUS "FASE 1 conexión NEXUS"). Precedencia: 1) tarifa
/// vigente del mantenedor del portal (el valor del mantenedor es el que se aplica); 2) para los
/// conceptos cuya tarifa base publica Nexus (<c>ChargeConcept.NexusTariff</c>), la de
/// <see cref="ITariffProvider"/> cuando el mantenedor no tiene una vigente. Con <see cref="TariffLookup.AsOfUtc"/>
/// reconstruye el mantenedor tal como estaba en ese instante desde el registro de cambios (NF-15).
/// </summary>
public interface ITariffResolver
{
    /// <summary>Tarifas vigentes a la fecha local indicada; la más específica por tipo de contenedor primero.</summary>
    Task<IReadOnlyList<ResolvedTariff>> GetInForceAsync(TariffLookup lookup, CancellationToken cancellationToken = default);

    /// <summary>Feriados del calendario de negocio del país (NF-22).</summary>
    Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(string country, CancellationToken cancellationToken = default);
}

/// <summary>Consulta de tarifa: fecha de vigencia en el calendario local del país (NF-22).</summary>
public sealed record TariffLookup(
    string Country,
    string Concept,
    DateOnly Date,
    string? ContainerType = null,
    string? Code = null,
    DateTime? AsOfUtc = null);

/// <summary>Tarifa resuelta, con su origen (<c>RuleSources</c>: PORTAL o NEXUS).</summary>
public sealed record ResolvedTariff(
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
    IReadOnlyList<TierDefinition> Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string Source);
