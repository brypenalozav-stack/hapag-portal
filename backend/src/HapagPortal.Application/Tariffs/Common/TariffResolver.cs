namespace HapagPortal.Application.Tariffs.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resolución de la tarifa vigente (M8-01). El mantenedor del portal tiene precedencia: su valor
/// vigente es el que se aplica al cobro. Si no tiene una tarifa vigente y el catálogo indica que Nexus
/// publica la tarifa base del concepto, se usa <see cref="ITariffProvider"/> (los montos que hoy están
/// fijos en procedimientos de Nexus). Un fallo de Nexus se trata como "sin tarifa" y el llamador
/// informa <c>Tariff.NotInForce</c>.
/// </summary>
public sealed class TariffResolver(
    IApplicationDbContext dbContext,
    ITariffProvider tariffProvider) : ITariffResolver
{
    public async Task<IReadOnlyList<ResolvedTariff>> GetInForceAsync(
        TariffLookup lookup,
        CancellationToken cancellationToken = default)
    {
        var portal = lookup.AsOfUtc is null
            ? await LoadCurrentAsync(lookup, cancellationToken)
            : await LoadAsOfAsync(lookup, cancellationToken);

        var selected = SelectForContainer(
            portal.Where(t => TariffCalculator.IsInForce(t.ValidFrom, t.ValidTo, lookup.Date)
                && (lookup.Code is null || string.Equals(t.Code, lookup.Code, StringComparison.OrdinalIgnoreCase))),
            lookup.ContainerType);

        if (selected.Count > 0)
            return selected;

        var nexusTariff = await dbContext.ChargeConcepts.AsNoTracking()
            .AnyAsync(c => c.Code == lookup.Concept && c.NexusTariff, cancellationToken);

        // Nexus no informa código de tarifa en el puerto: con un código pedido, solo responde el portal.
        if (!nexusTariff || lookup.Code is not null)
            return [];

        var nexus = await tariffProvider.GetTariffsAsync(lookup.Country, lookup.Concept, lookup.Date, cancellationToken);
        if (nexus.IsFailure)
            return [];

        return SelectForContainer(
            nexus.Value
                .Where(t => TariffCalculator.IsInForce(t.ValidFrom, t.ValidTo, lookup.Date))
                .Select(t => new ResolvedTariff(
                    null, t.Concept, null, lookup.Country, t.Currency, t.ContainerType, null, t.Amount,
                    TariffTierUnits.None, TariffTierModes.Flat, [], t.ValidFrom, t.ValidTo, RuleSources.Nexus)),
            lookup.ContainerType);
    }

    public async Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(string country, CancellationToken cancellationToken = default)
    {
        var dates = await dbContext.BusinessHolidays.AsNoTracking()
            .Where(h => h.Country == country)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        return dates.ToHashSet();
    }

    private async Task<IReadOnlyList<ResolvedTariff>> LoadCurrentAsync(TariffLookup lookup, CancellationToken cancellationToken)
    {
        var tariffs = await dbContext.Tariffs.AsNoTracking()
            .Include(t => t.Tiers)
            .Where(t => t.IsActive && t.Country == lookup.Country && t.ConceptCode == lookup.Concept)
            .ToListAsync(cancellationToken);

        return tariffs
            .Select(t => ToResolved(t.Id, TariffSnapshot.From(t)))
            .ToList();
    }

    /// <summary>Mantenedor reconstruido al instante <c>AsOfUtc</c> con la última instantánea de cada tarifa.</summary>
    private async Task<IReadOnlyList<ResolvedTariff>> LoadAsOfAsync(TariffLookup lookup, CancellationToken cancellationToken)
    {
        var asOf = lookup.AsOfUtc!.Value;

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.Tariff && c.ChangedAt <= asOf)
            .ToListAsync(cancellationToken);

        return changes
            .GroupBy(c => c.EntityId)
            .Select(g => (Id: g.Key, Snapshot: MaintainerChangeLogger.Read<TariffSnapshot>(
                g.OrderByDescending(c => c.ChangedAt).First().NewValue)))
            .Where(x => x.Snapshot is not null
                && x.Snapshot.IsActive
                && x.Snapshot.Country == lookup.Country
                && x.Snapshot.ConceptCode == lookup.Concept)
            .Select(x => ToResolved(x.Id, x.Snapshot!))
            .ToList();
    }

    /// <summary>
    /// Con tipo de contenedor, por cada código se prefiere la tarifa específica y si no existe la genérica
    /// (sin tipo). Sin tipo de contenedor se devuelven todas. Orden estable: código y luego monto.
    /// </summary>
    private static IReadOnlyList<ResolvedTariff> SelectForContainer(IEnumerable<ResolvedTariff> tariffs, string? containerType)
    {
        var list = tariffs.ToList();

        if (!string.IsNullOrWhiteSpace(containerType))
        {
            list = list
                .Where(t => t.ContainerType is null || string.Equals(t.ContainerType, containerType, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => (t.Code ?? string.Empty, t.Currency))
                .Select(g => g.OrderByDescending(t => t.ContainerType is not null).First())
                .ToList();
        }

        return list
            .OrderBy(t => t.Code ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(t => t.ContainerType ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(t => t.Amount)
            .ToList();
    }

    private static ResolvedTariff ToResolved(Guid id, TariffSnapshot snapshot) => new(
        id,
        snapshot.ConceptCode,
        snapshot.Code,
        snapshot.Country,
        snapshot.Currency,
        snapshot.ContainerType,
        snapshot.Description,
        snapshot.Amount,
        snapshot.TierUnit,
        snapshot.TierMode,
        TariffMapper.ToDefinitions(snapshot.Tiers),
        snapshot.ValidFrom,
        snapshot.ValidTo,
        RuleSources.Portal);
}
