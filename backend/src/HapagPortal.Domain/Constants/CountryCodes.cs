namespace HapagPortal.Domain.Constants;

public static class CountryCodes
{
    public const string Chile = "CL";
    public const string Bolivia = "BO";

    public static readonly string[] ValidCountries = [Chile, Bolivia];

    /// <summary>
    /// Países de operación a partir de la lista separada por coma; si está vacía, el país base.
    /// Solo se conservan códigos válidos y sin repetir.
    /// </summary>
    public static IReadOnlyList<string> ParseOperatingCountries(string? csv, string fallbackCountry)
    {
        var parsed = (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToUpperInvariant())
            .Where(c => ValidCountries.Contains(c))
            .Distinct()
            .ToList();

        return parsed.Count > 0 ? parsed : [fallbackCountry];
    }

    public static string GetCurrency(string country) => country == Chile ? "CLP" : "BOB";
    public static string GetTaxIdType(string country) => country switch
    {
        Chile => "RUT",
        Bolivia => "NIT",
        _ => "TAX_ID"
    };
}
