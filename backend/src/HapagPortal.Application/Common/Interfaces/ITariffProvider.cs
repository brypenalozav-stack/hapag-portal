using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Montos fijos de recargos locales por país y concepto (CT-NEXUS, <c>GET /tariffs</c>).
/// Sin tarifas: lista vacía.
/// </summary>
public interface ITariffProvider
{
    Task<Result<IReadOnlyList<TariffItem>>> GetTariffsAsync(
        string countryCode,
        string concept,
        DateOnly at,
        CancellationToken cancellationToken = default);
}

/// <summary>Tarifa vigente de un concepto, opcionalmente por tipo ISO de contenedor.</summary>
public sealed record TariffItem(
    string Concept,
    string? ContainerType,
    decimal Amount,
    string Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
