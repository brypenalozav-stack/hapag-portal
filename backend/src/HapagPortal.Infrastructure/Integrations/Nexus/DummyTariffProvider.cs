using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Tarifas simuladas de Nexus (CT-NEXUS). Determinista: Chile tiene WAREHOUSE_CHANGE en 9.940 CLP
/// (ejemplo del contrato), vigente desde el 01-10-2026; cualquier otra combinación devuelve una lista vacía.
/// </summary>
public sealed class DummyTariffProvider(ILogger<DummyTariffProvider> logger) : ITariffProvider
{
    private static readonly DateOnly ValidFrom = new(2026, 10, 1);

    private static readonly (string Country, TariffItem Item)[] Tariffs =
    [
        ("CL", new TariffItem("WAREHOUSE_CHANGE", null, 9940m, "CLP", ValidFrom, null)),
    ];

    public Task<Result<IReadOnlyList<TariffItem>>> GetTariffsAsync(
        string countryCode,
        string concept,
        DateOnly at,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TariffItem> items = Tariffs
            .Where(t => string.Equals(t.Country, countryCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.Item.Concept, concept, StringComparison.OrdinalIgnoreCase)
                && t.Item.ValidFrom <= at
                && (t.Item.ValidTo is null || t.Item.ValidTo >= at))
            .Select(t => t.Item)
            .ToList();

        logger.LogDebug(
            "Tarifas Nexus (dummy) - Country: {Country}, Concept: {Concept}, At: {At}, Items: {Count}",
            countryCode, concept, at, items.Count);

        return Task.FromResult(Result<IReadOnlyList<TariffItem>>.Success(items));
    }
}
