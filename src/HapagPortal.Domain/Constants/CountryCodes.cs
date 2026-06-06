namespace HapagPortal.Domain.Constants;

public static class CountryCodes
{
    public const string Chile = "CL";
    public const string Bolivia = "BO";

    public static readonly string[] ValidCountries = [Chile, Bolivia];

    public static string GetCurrency(string country) => country == Chile ? "CLP" : "BOB";
    public static string GetTaxIdType(string country) => country switch
    {
        Chile => "RUT",
        Bolivia => "NIT",
        _ => "TAX_ID"
    };
}
