using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Charges;

/// <summary>Tramo de tarifa: desde/hasta inclusive (hasta nulo = sin tope) y valor del tramo.</summary>
public sealed record TierDefinition(int FromUnit, int? ToUnit, decimal Amount);

/// <summary>Detalle de un tramo aplicado: unidades cobradas y su valor.</summary>
public sealed record TariffBreakdownLine(int FromUnit, int ToUnit, int Units, decimal UnitAmount, decimal Amount);

/// <summary>
/// Resultado de aplicar una tarifa a una medida. <see cref="Covered"/> es falso si la medida cae fuera
/// de los tramos definidos (la tarifa no la cubre).
/// </summary>
public sealed record TariffComputation(
    decimal Amount,
    int MeasuredUnits,
    bool Covered,
    IReadOnlyList<TariffBreakdownLine> Lines);

/// <summary>
/// Aplicación de tarifas con tramos del mantenedor (M8-01). Puro y determinista para pruebas.
/// <see cref="TariffTierModes.Flat"/>: valor del tramo que contiene la medida (por ejemplo, horas desde
/// el plazo). <see cref="TariffTierModes.PerUnit"/>: cada unidad desde 1 se cobra al valor de su tramo
/// (por ejemplo, días de demurrage); las primeras <c>freeUnits</c> no se cobran.
/// </summary>
public static class TariffCalculator
{
    public static bool IsInForce(DateOnly validFrom, DateOnly? validTo, DateOnly date) =>
        validFrom <= date && (validTo is null || validTo.Value >= date);

    public static TariffComputation Compute(
        decimal baseAmount,
        string tierMode,
        IReadOnlyList<TierDefinition> tiers,
        int measuredUnits,
        int freeUnits = 0)
    {
        measuredUnits = Math.Max(0, measuredUnits);

        if (tiers.Count == 0)
            return new TariffComputation(baseAmount, measuredUnits, true, []);

        var ordered = tiers.OrderBy(t => t.FromUnit).ToList();

        if (tierMode == TariffTierModes.PerUnit)
            return ComputePerUnit(ordered, measuredUnits, Math.Max(0, freeUnits));

        var tier = ordered.FirstOrDefault(t => t.FromUnit <= measuredUnits && (t.ToUnit is null || t.ToUnit >= measuredUnits));
        if (tier is null)
            return new TariffComputation(0m, measuredUnits, false, []);

        return new TariffComputation(
            tier.Amount,
            measuredUnits,
            true,
            [new TariffBreakdownLine(tier.FromUnit, tier.ToUnit ?? measuredUnits, 1, tier.Amount, tier.Amount)]);
    }

    /// <summary>
    /// Valida los tramos: desde no negativo, hasta mayor o igual que desde, sin solaparse, en orden y solo
    /// el último sin tope. Devuelve el motivo del error o nulo si son válidos.
    /// </summary>
    public static string? ValidateTiers(IReadOnlyList<TierDefinition> tiers)
    {
        var ordered = tiers.OrderBy(t => t.FromUnit).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var tier = ordered[i];

            if (tier.FromUnit < 0)
                return "Tier 'fromUnit' must be zero or greater.";

            if (tier.ToUnit is not null && tier.ToUnit < tier.FromUnit)
                return "Tier 'toUnit' must be greater than or equal to 'fromUnit'.";

            if (tier.Amount < 0)
                return "Tier amount must be zero or greater.";

            if (i < ordered.Count - 1)
            {
                if (tier.ToUnit is null)
                    return "Only the last tier can be open-ended.";

                if (ordered[i + 1].FromUnit <= tier.ToUnit)
                    return "Tiers must not overlap.";
            }
        }

        return null;
    }

    private static TariffComputation ComputePerUnit(IReadOnlyList<TierDefinition> tiers, int measuredUnits, int freeUnits)
    {
        var lines = new List<TariffBreakdownLine>();
        var total = 0m;
        var charged = 0;

        foreach (var tier in tiers)
        {
            var start = Math.Max(Math.Max(tier.FromUnit, 1), freeUnits + 1);
            var end = Math.Min(tier.ToUnit ?? measuredUnits, measuredUnits);

            if (end < start)
                continue;

            var units = end - start + 1;
            var amount = units * tier.Amount;
            total += amount;
            charged += units;
            lines.Add(new TariffBreakdownLine(start, end, units, tier.Amount, amount));
        }

        var billable = Math.Max(0, measuredUnits - freeUnits);
        return new TariffComputation(total, measuredUnits, charged >= billable, lines);
    }
}
