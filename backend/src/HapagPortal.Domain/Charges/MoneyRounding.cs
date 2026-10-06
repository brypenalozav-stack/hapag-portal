namespace HapagPortal.Domain.Charges;

/// <summary>Redondeo por moneda: CLP sin decimales; USD, EUR y BOB con dos.</summary>
public static class MoneyRounding
{
    public static decimal Round(decimal amount, string currency) =>
        Math.Round(amount, string.Equals(currency, "CLP", StringComparison.OrdinalIgnoreCase) ? 0 : 2, MidpointRounding.AwayFromZero);
}
